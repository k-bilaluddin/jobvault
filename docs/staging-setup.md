# Staging Environment Setup Guide

## Architecture Overview

```
Feature Branch → PR → develop → CI → CD Staging → stg-api.kbilaluddin.dev
                                                    stg.kbilaluddin.dev (Cloudflare Pages)
                                                         ↓ (verified)
                      develop → PR → master → CI → CD Production → api.kbilaluddin.dev
                                                                    jobvault.kbilaluddin.dev (Cloudflare Pages)
```

### Infrastructure Map

| Component | Production | Staging |
|-----------|-----------|---------|
| Frontend | `jobvault.kbilaluddin.dev` (Cloudflare Pages, master) | `stg.kbilaluddin.dev` (Cloudflare Pages, develop) |
| API | `api.kbilaluddin.dev` (port 5000) | `stg-api.kbilaluddin.dev` (port 5002) |
| API Container | `jobvault-api` | `jobvault-api-staging` |
| Worker Container | `jobvault-worker` | `jobvault-worker-staging` |
| Docker Image Tag | `:latest` | `:staging` |
| Docker Compose | `docker-compose.yml` | `docker-compose.staging.yml` |
| Env File | `.env` | `.env.staging` |
| MongoDB | Atlas (`jobvault` database) | Local Docker (`jobvault-staging` database) |
| RabbitMQ | CloudAMQP | Local Docker (`amqp://jobvault:jobvault@localhost:5672`) |
| CD Workflow | `cd.yml` (triggers on master) | `cd-staging.yml` (triggers on develop) |
| GitHub Secrets | `MONGODB_*`, `RABBITMQ_*`, etc. | `STG_MONGODB_*`, `STG_RABBITMQ_*`, etc. |

---

## Prerequisites (Manual Steps)

### 1. MongoDB — Staging Database

Staging uses the local Docker MongoDB instance (same one used for development), not Atlas.
The database `jobvault-staging` is created automatically on first write — no manual setup needed.

- Connection: `mongodb://jobvault:jobvault@localhost:27017/`
- Database: `jobvault-staging`

### 2. RabbitMQ — Staging Queues

Staging uses the local Docker RabbitMQ instance, not CloudAMQP.
Queues are created automatically by the application on startup.

- Connection: `amqp://jobvault:jobvault@localhost:5672`
- Queue prefix: `stg.` to avoid any overlap with local development

### 3. Cloudflare Tunnel — Add Staging Route

Update your tunnel config (`~/.cloudflared/config.yml`):

```yaml
tunnel: <your-tunnel-id>
credentials-file: <your-credentials-file>
ingress:
  - hostname: api.kbilaluddin.dev
    service: http://localhost:5000
  - hostname: stg-api.kbilaluddin.dev
    service: http://localhost:5002
  - service: http_status:404
```

Then:
1. Add DNS CNAME record: `stg-api` → `<tunnel-id>.cfargotunnel.com`
2. Restart cloudflared: `cloudflared tunnel run <tunnel-name>`

### 4. Cloudflare Pages — Map Staging Domain

1. Go to Cloudflare Pages → jobvault project → Custom domains
2. Add `stg.kbilaluddin.dev` mapped to `develop` branch
3. Go to Settings → Environment variables → Preview environment
4. Set `VITE_API_BASE` = `https://stg-api.kbilaluddin.dev`

### 5. GitHub Secrets — Add Staging Secrets

All staging secrets use the `STG_` prefix. Run these commands:

```bash
# Auth (can share with production or use separate credentials)
gh secret set STG_AUTH_EMAIL --repo k-bilaluddin/jobvault --body "<email>"
gh secret set STG_AUTH_PASSWORD_HASH --repo k-bilaluddin/jobvault --body "<bcrypt-hash>"
gh secret set STG_AUTH_JWT_SECRET --repo k-bilaluddin/jobvault --body "<min-32-chars>"

# CORS
gh secret set STG_CORS_ALLOWED_ORIGINS --repo k-bilaluddin/jobvault --body "https://stg.kbilaluddin.dev,http://localhost:5173"

# MongoDB (local Docker instance, separate database)
gh secret set STG_MONGODB_CONNECTION_STRING --repo k-bilaluddin/jobvault --body "mongodb://jobvault:jobvault@host.docker.internal:27017/"
gh secret set STG_MONGODB_DATABASE_NAME --repo k-bilaluddin/jobvault --body "jobvault-staging"
gh secret set STG_MONGODB_JOB_APPLICATIONS_COLLECTION --repo k-bilaluddin/jobvault --body "job_applications"
gh secret set STG_MONGODB_NOTIFICATIONS_COLLECTION --repo k-bilaluddin/jobvault --body "notifications"

# RabbitMQ (local Docker instance, prefixed queues)
gh secret set STG_RABBITMQ_CONNECTION_STRING --repo k-bilaluddin/jobvault --body "amqp://jobvault:jobvault@host.docker.internal:5672"
gh secret set STG_RABBITMQ_EXCHANGE_NAME --repo k-bilaluddin/jobvault --body "stg.job.applications"
gh secret set STG_RABBITMQ_DEAD_LETTER_EXCHANGE_NAME --repo k-bilaluddin/jobvault --body "stg.job.applications.dead"
gh secret set STG_RABBITMQ_DEAD_LETTER_QUEUE_NAME --repo k-bilaluddin/jobvault --body "stg.job.applications.dlq"
gh secret set STG_RABBITMQ_JOB_APPLICATION_CREATED_QUEUE --repo k-bilaluddin/jobvault --body "stg.job.applications.created"
gh secret set STG_RABBITMQ_JOB_APPLICATION_UPDATED_QUEUE --repo k-bilaluddin/jobvault --body "stg.job.applications.updated"
gh secret set STG_RABBITMQ_JOB_APPLICATION_RECEIVED_QUEUE --repo k-bilaluddin/jobvault --body "stg.job.applications.received"
gh secret set STG_RABBITMQ_SSE_NOTIFICATIONS_QUEUE --repo k-bilaluddin/jobvault --body "stg.job.applications.notifications"
```

Shared secrets (same for both environments — no `STG_` prefix needed):
- `TELEGRAM_BOT_TOKEN`, `TELEGRAM_CHAT_ID` — same bot, messages distinguish staging vs production
- `APP_GH_TOKEN`, `APP_GH_OWNER`, `APP_GH_REPOSITORY`, `APP_GH_BRANCH` — same vault
- `APP_VAULT_ROOT_DIR` — same vault folder on host

---

## Developer Workflow

### Daily development

```bash
# 1. Create feature branch from develop
git checkout develop && git pull
git checkout -b feature/my-feature

# 2. Develop and commit
# ...

# 3. Push and create PR targeting develop
git push -u origin feature/my-feature
gh pr create --base develop

# 4. CI runs, PR merged to develop
# 5. CD Staging deploys automatically
# 6. Verify on stg.kbilaluddin.dev
```

### Promoting to production

```bash
# 1. Create PR from develop to master
gh pr create --base master --head develop --title "Release: <description>"

# 2. CI runs, review, merge
# 3. CD Production deploys automatically
# 4. Verify on jobvault.kbilaluddin.dev
```

### Hotfixes (production emergencies)

```bash
# 1. Branch from master
git checkout master && git pull
git checkout -b hotfix/critical-fix

# 2. Fix, push, PR to master
# 3. After merge to master, also merge master back to develop:
git checkout develop && git pull
git merge master
git push
```

---

## Troubleshooting

### Staging containers conflict with production
- Staging uses separate container names (`jobvault-api-staging`, `jobvault-worker-staging`)
- Staging uses port 5002 (production uses 5000)
- They share the `jobvault-internal` Docker network for the generation service

### Wrong database in staging
- Staging uses local Docker MongoDB, NOT Atlas
- Check `.env.staging` on the runner: `MONGODB_DATABASE_NAME` should be `jobvault-staging`
- Verify: `docker exec jobvault-api-staging env | findstr MONGODB`
- Connection should show `localhost` or `host.docker.internal`, NOT `mongodb+srv://`

### CORS errors on staging frontend
- Check `STG_CORS_ALLOWED_ORIGINS` includes `https://stg.kbilaluddin.dev`
- Verify: `docker exec jobvault-api-staging env | findstr CORS`

### Cloudflare Pages not deploying staging
- Check Cloudflare Pages → Deployments → filter by `develop` branch
- Ensure custom domain `stg.kbilaluddin.dev` is mapped to `develop` branch

### RabbitMQ queue cross-talk
- Staging uses local Docker RabbitMQ with `stg.` prefix, production uses CloudAMQP — no cross-talk possible
- Verify: `docker exec jobvault-api-staging env | findstr RABBITMQ`
- Connection should show `localhost` or `host.docker.internal`, NOT `amqps://`
