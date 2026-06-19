# Environment Variables Reference

## Precedence

Configuration is resolved in this order (highest to lowest):

1. **Environment variables** (set in docker-compose or shell)
2. **`appsettings.{Environment}.json`**
3. **`appsettings.json`** (base defaults, in source)

.NET maps `SCREAMING__SNAKE__CASE` env vars to `Section:Key` config paths automatically (double underscore `__` = `:` separator).

---

## Auth

| Variable | Required | Default | Description |
|---|---|---|---|
| `Auth__Email` | yes | — | Owner account email |
| `Auth__PasswordHash` | yes | — | bcrypt hash of owner password. Generate: `cd tools/HashPassword && dotnet run` |
| `Auth__JwtSecret` | yes | — | Signing key, min 32 chars. Generate: `openssl rand -hex 32` |
| `Auth__TokenExpiryDays` | no | `7` | JWT validity in days |
| `Demo__Email` | no | `demo@jobvault.dev` | Demo account email (leave empty to disable) |
| `Demo__PasswordHash` | no | — | bcrypt hash of demo password |

---

## CORS

| Variable | Required | Default | Description |
|---|---|---|---|
| `CORS_ALLOWED_ORIGINS` | yes | — | Comma-separated allowed origins. Takes priority over `Cors:AllowedOrigins` config key. |

---

## MongoDB

| Variable | Required | Default | Description |
|---|---|---|---|
| `MongoDb__ConnectionString` | yes | — | Atlas connection URI |
| `MongoDb__DatabaseName` | no | `jobvault` | Database name |
| `MongoDb__JobApplicationsCollectionName` | no | `JobApplications` | Applications collection |
| `MongoDb__NotificationsCollectionName` | no | `notifications` | Notifications collection |

---

## RabbitMQ

| Variable | Required | Default | Description |
|---|---|---|---|
| `RabbitMq__ConnectionString` | yes | — | CloudAMQP / RabbitMQ AMQP URI |
| `RabbitMq__ExchangeName` | no | `job.applications` | Topic exchange name |
| `RabbitMq__DeadLetterExchangeName` | no | `job.applications.dead` | DLX name |
| `RabbitMq__DeadLetterQueueName` | no | `job.applications.dlq` | Dead-letter queue |
| `RabbitMq__JobApplicationCreatedQueueName` | no | `job.applications.created` | Created events queue |
| `RabbitMq__JobApplicationUpdatedQueueName` | no | `job.applications.updated` | Updated events queue |
| `RabbitMq__JobApplicationReceivedQueueName` | no | `job.applications.received` | Async ingestion queue |
| `RabbitMq__SseNotificationsQueueName` | no | `job.applications.notifications` | SSE fan-out queue |

---

## Telegram

| Variable | Required | Default | Description |
|---|---|---|---|
| `Telegram__BotToken` | yes | — | Telegram bot token |
| `Telegram__ChatId` | yes | — | Destination chat ID |

---

## GitHub

| Variable | Required | Default | Description |
|---|---|---|---|
| `GitHub__Token` | yes | — | PAT with `repo` scope |
| `GitHub__Owner` | no | `k-bilaluddin` | GitHub username |
| `GitHub__Repository` | no | `job-applications-vault` | Target vault repository |
| `GitHub__Branch` | no | `master` | Branch to commit to |
| `GitHub__CvFileName` | no | `KhawajaBilal_Uddin_CV` | CV file base name (no extension) |
| `GitHub__CoverLetterFileName` | no | `KhawajaBilal_Uddin_CoverLetter` | Cover letter base name |

---

## Document Generation

| Variable | Required | Default | Description |
|---|---|---|---|
| `DocumentGeneration__BaseUrl` | no | `http://jobvault-generation-service:3000` | Generation service URL |

---

## LibreOffice

| Variable | Required | Default | Description |
|---|---|---|---|
| `LibreOffice__ExecutablePath` | no | `libreoffice` | Path to soffice binary. Override on Windows dev: `C:\Program Files\LibreOffice\program\soffice.exe` |

---

## Auth Setup Quickstart

```bash
# 1. Generate a bcrypt hash for your password
cd tools/HashPassword && dotnet run

# 2. Generate a JWT secret
openssl rand -hex 32

# 3. Add to your .env file
Auth__Email=you@example.com
Auth__PasswordHash=<paste hash>
Auth__JwtSecret=<paste secret>

# 4. Restart the API
docker compose restart api
```
