import express from "express";
import { exec } from "child_process";
import crypto from "crypto";
import { createWriteStream } from "fs";
import { join, dirname } from "path";
import { fileURLToPath } from "url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const app = express();

// ================================
// Config
// ================================
const PORT = process.env.WEBHOOK_PORT || 3001;
const SECRET = process.env.WEBHOOK_SECRET;
const COMPOSE_DIR = process.env.COMPOSE_DIR || __dirname;
const LOG_FILE = join(__dirname, "deploy.log");

if (!SECRET) {
  console.error("❌ WEBHOOK_SECRET env variable is required");
  process.exit(1);
}

// ================================
// Logger
// ================================
const logStream = createWriteStream(LOG_FILE, { flags: "a" });

function log(msg) {
  const line = `[${new Date().toISOString()}] ${msg}`;
  console.log(line);
  logStream.write(line + "\n");
}

// ================================
// Signature Verification
// ================================
function verifySignature(req) {
  const signature = req.headers["x-hub-signature-256"];
  if (!signature) return false;

  const expected =
    "sha256=" +
    crypto
      .createHmac("sha256", SECRET)
      .update(req.rawBody)
      .digest("hex");

  return crypto.timingSafeEqual(
    Buffer.from(signature),
    Buffer.from(expected)
  );
}

// ================================
// Raw body capture (needed for HMAC)
// ================================
app.use((req, res, next) => {
  let data = "";
  req.on("data", (chunk) => (data += chunk));
  req.on("end", () => {
    req.rawBody = data;
    next();
  });
});

app.use(express.json());

// ================================
// Health check
// ================================
app.get("/health", (req, res) => {
  res.json({ status: "ok", uptime: process.uptime() });
});

// ================================
// Deploy webhook
// ================================
app.post("/webhook/deploy", (req, res) => {
  // 1. Verify signature
  if (!verifySignature(req)) {
    log("⚠️  Unauthorized webhook attempt");
    return res.status(401).json({ error: "Unauthorized" });
  }

  // 2. Only act on workflow_run completed + success events
  const event = req.headers["x-github-event"];
  const body = req.body;

  if (event === "workflow_run") {
    if (body.action !== "completed" || body.workflow_run?.conclusion !== "success") {
      log(`⏭️  Skipping — workflow_run event: action=${body.action} conclusion=${body.workflow_run?.conclusion}`);
      return res.json({ message: "Skipped — not a successful completion" });
    }
  }

  log(`🚀 Deploy triggered by GitHub event: ${event}`);

  // 3. Respond immediately — don't make GitHub wait for docker pull
  res.json({ message: "Deploy started" });

  // 4. Pull & restart containers
  const cmd = `cd ${COMPOSE_DIR} && docker compose pull && docker compose up -d`;

  exec(cmd, (error, stdout, stderr) => {
    if (error) {
      log(`❌ Deploy failed: ${error.message}`);
      log(`stderr: ${stderr}`);
      return;
    }
    log(`✅ Deploy succeeded`);
    log(`stdout: ${stdout}`);
  });
});

// ================================
// Start
// ================================
app.listen(PORT, () => {
  log(`🎯 Webhook server running on port ${PORT}`);
  log(`📁 Compose dir: ${COMPOSE_DIR}`);
  log(`📝 Log file: ${LOG_FILE}`);
});
