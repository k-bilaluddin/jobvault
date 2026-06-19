# Environment Variables Reference

## Naming convention

All environment variables use `SCREAMING_SNAKE_CASE`. At startup, both the API and Worker map these to .NET's internal `Section:Key` config paths via an `AddInMemoryCollection` block in `Program.cs` — no double-underscore names needed.

---

## Auth

| Variable | Required | Default | Description |
|---|---|---|---|
| `AUTH_EMAIL` | yes | — | Owner account email |
| `AUTH_PASSWORD_HASH` | yes | — | bcrypt hash of owner password. Generate: `cd tools/HashPassword && dotnet run` |
| `AUTH_JWT_SECRET` | yes | — | Signing key, min 32 chars. Generate: `openssl rand -hex 32` |
| `AUTH_TOKEN_EXPIRY_DAYS` | no | `7` | JWT validity in days |
| `DEMO_EMAIL` | no | `demo@jobvault.dev` | Demo account email (leave empty to disable) |
| `DEMO_PASSWORD_HASH` | no | — | bcrypt hash of demo password |

---

## CORS

| Variable | Required | Default | Description |
|---|---|---|---|
| `CORS_ALLOWED_ORIGINS` | yes | — | Comma-separated allowed origins |

---

## MongoDB

| Variable | Required | Default | Description |
|---|---|---|---|
| `MONGODB_CONNECTION_STRING` | yes | — | Atlas connection URI |
| `MONGODB_DATABASE_NAME` | no | `jobvault` | Database name |
| `MONGODB_JOB_APPLICATIONS_COLLECTION` | no | `JobApplications` | Applications collection |
| `MONGODB_NOTIFICATIONS_COLLECTION` | no | `notifications` | Notifications collection |

---

## RabbitMQ

| Variable | Required | Default | Description |
|---|---|---|---|
| `RABBITMQ_CONNECTION_STRING` | yes | — | CloudAMQP / RabbitMQ AMQP URI |
| `RABBITMQ_EXCHANGE_NAME` | no | `job.applications` | Topic exchange name |
| `RABBITMQ_DEAD_LETTER_EXCHANGE_NAME` | no | `job.applications.dead` | DLX name |
| `RABBITMQ_DEAD_LETTER_QUEUE_NAME` | no | `job.applications.dlq` | Dead-letter queue |
| `RABBITMQ_JOB_APPLICATION_CREATED_QUEUE` | no | `job.applications.created` | Created events queue |
| `RABBITMQ_JOB_APPLICATION_UPDATED_QUEUE` | no | `job.applications.updated` | Updated events queue |
| `RABBITMQ_JOB_APPLICATION_RECEIVED_QUEUE` | no | `job.applications.received` | Async ingestion queue |
| `RABBITMQ_SSE_NOTIFICATIONS_QUEUE` | no | `job.applications.notifications` | SSE fan-out queue |

---

## Telegram

| Variable | Required | Default | Description |
|---|---|---|---|
| `TELEGRAM_BOT_TOKEN` | yes | — | Telegram bot token |
| `TELEGRAM_CHAT_ID` | yes | — | Destination chat ID |

---

## GitHub

`APP_GH_*` prefix avoids collision with GitHub Actions' built-in `GITHUB_TOKEN`.

| Variable | Required | Default | Description |
|---|---|---|---|
| `APP_GH_TOKEN` | yes | — | PAT with `repo` scope |
| `APP_GH_OWNER` | no | `k-bilaluddin` | GitHub username |
| `APP_GH_REPOSITORY` | no | `job-applications-vault` | Target vault repository |
| `APP_GH_BRANCH` | no | `master` | Branch to commit to |
| `APP_GH_CV_FILE_NAME` | no | `KhawajaBilal_Uddin_CV` | CV file base name (no extension) |
| `APP_GH_COVER_LETTER_FILE_NAME` | no | `KhawajaBilal_Uddin_CoverLetter` | Cover letter base name |

---

## Document Generation

| Variable | Required | Default | Description |
|---|---|---|---|
| `DOCUMENT_GENERATION_BASE_URL` | no | `http://jobvault-generation-service:3000` | Generation service URL |

---

## LibreOffice

| Variable | Required | Default | Description |
|---|---|---|---|
| `LIBREOFFICE_EXECUTABLE_PATH` | no | `libreoffice` | Path to soffice binary. Override on Windows dev: `C:\Program Files\LibreOffice\program\soffice.exe` |

---

## Auth Setup Quickstart

```bash
# 1. Generate a bcrypt hash for your password
cd tools/HashPassword && dotnet run

# 2. Generate a JWT secret
openssl rand -hex 32

# 3. Add to your .env file
AUTH_EMAIL=you@example.com
AUTH_PASSWORD_HASH=<paste hash>
AUTH_JWT_SECRET=<paste secret>

# 4. Restart the API
docker compose restart api
```
