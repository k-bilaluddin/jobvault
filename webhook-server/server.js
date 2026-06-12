import express from "express";
import { exec } from "child_process";
import crypto from "crypto";
import { createWriteStream } from "fs";
import { join, dirname } from "path";
import { fileURLToPath } from "url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const app = express();

const PORT = process.env.WEBHOOK_PORT || 3001;
const SECRET = process.env.WEBHOOK_SECRET;
const COMPOSE_DIR = process.env.COMPOSE_DIR || __dirname;
const LOG_FILE = join(__dirname, "deploy.log");

if (!SECRET) {
  console.error("❌ WEBHOOK_SECRET env variable is required");
  process.exit(1);
}

const logStream = createWriteStream(LOG_FILE, { flags: "a" });
function log(msg) {
  const line = `[${new Date().toISOString()}] ${msg}`;
  console.log(line);
  logStream.write(line + "\n");
}

// Use raw buffer — captures body once, used for both HMAC and JSON parse
app.use(express.raw({ type: "*/*" }));

app.get("/health", (req, res) => {
  res.json({ status: "ok", uptime: process.uptime() });
});

app.post("/webhook/deploy", (req, res) => {
  const signature = req.headers["x-hub-signature-256"];
  if (!signature) {
    log("⚠️  Missing signature");
    return res.status(401).json({ error: "Unauthorized" });
  }

  const expected =
    "sha256=" +
    crypto
      .createHmac("sha256", SECRET)
      .update(req.body)
      .digest("hex");

  if (!crypto.timingSafeEqual(Buffer.from(signature), Buffer.from(expected))) {
    log("⚠️  Invalid signature");
    return res.status(401).json({ error: "Unauthorized" });
  }

  log("🚀 Deploy triggered");
  res.json({ message: "Deploy started" });

  const cmd = `cd /app && docker compose pull && docker compose up -d`;
  exec(cmd, (error, stdout, stderr) => {
    if (error) {
      log(`❌ Deploy failed: ${error.message}`);
      return;
    }
    log(`✅ Deploy succeeded`);
  });
});

app.listen(PORT, () => {
  log(`🎯 Webhook server running on port ${PORT}`);
  log(`📁 Compose dir: ${COMPOSE_DIR}`);
  log(`📝 Log file: ${LOG_FILE}`);
});