# CI/CD Pipeline

JobVault uses GitHub Actions with three workflows across two environments. All secrets are managed via GitHub Secrets — nothing is hardcoded.

## Workflows

| Workflow | Trigger | Environment |
|---|---|---|
| **CI** (`ci.yml`) | Push/PR to `master` or `develop` | Ubuntu (GitHub-hosted) |
| **Deploy Production** (`cd.yml`) | CI passes on `master` | Self-hosted Windows runner |
| **Deploy Staging** (`cd-staging.yml`) | CI passes on `develop` | Self-hosted Windows runner |

## CI — Continuous Integration

Runs on every push and pull request. Uses [dorny/paths-filter](https://github.com/dorny/paths-filter) to skip unchanged layers:

```
Push / PR to master or develop
          ↓
    Detect changed paths
     ┌─────┴──────┐
     ↓            ↓
  Backend?     Frontend?
     ↓            ↓
  .NET 9       Node.js 22
  Restore      npm ci
  Build        npm test
     ↓
  Unit Tests (129 tests)
  Architecture Tests (19 tests)
```

Backend and frontend jobs run **in parallel**. If only frontend files changed, backend tests are skipped entirely — and vice versa.

### What the Architecture Tests enforce

These aren't just unit tests — they verify Clean Architecture layer rules at build time:

- Domain has zero external dependencies
- Application layer cannot reference Infrastructure
- Controllers cannot use repositories directly
- Controllers cannot access the file system or process APIs
- All interfaces have implementations
- Naming conventions are enforced (Services, Interfaces, Entities)

If someone breaks a layer boundary, CI fails before the code reaches `develop`.

## CD — Continuous Deployment

Triggered automatically when CI passes on the target branch. Production and staging follow the same pattern:

```
CI passes on master (or develop for staging)
          ↓
  ┌───────┴────────┐
  ↓                ↓
Build API        Build Worker        ← parallel, Docker Buildx
Push to GHCR     Push to GHCR        ← ghcr.io/k-bilaluddin/jobvault-*
  └───────┬────────┘
          ↓
  Self-hosted runner (Windows)
          ↓
  Write .env from GitHub Secrets
          ↓
  Ensure Docker network exists
          ↓
  docker compose pull && up -d
          ↓
  Telegram deployment notification
```

### Key details

**Docker images** are built with Buildx and pushed to GitHub Container Registry (GHCR). Each image gets two tags: `latest` (or `staging`) and `sha-<commit>`.

**GitHub Actions cache** (`cache-from: type=gha`) is used for Docker layer caching, so rebuilds only process changed layers.

**Secrets management**: The deploy step writes all environment variables from GitHub Secrets to a persistent `.env` file on the runner. The file lives outside the workspace (`$USERPROFILE/.jobvault/.env`) so it survives across deployments.

**Self-hosted runner** runs on Windows with Docker Desktop. The runner pulls fresh images from GHCR and restarts containers with zero-downtime (Docker Compose handles graceful shutdown).

**Telegram notification** fires after every deployment with the commit SHA and actor — so I know immediately when a deploy lands.

## Environment Isolation

| | Production | Staging |
|---|---|---|
| **Branch** | `master` | `develop` |
| **Docker tag** | `latest` | `staging` |
| **Compose file** | `docker-compose.yml` | `docker-compose.staging.yml` |
| **Env file** | `.env` | `.env.staging` |
| **Database** | `jobvault` | `jobvault-staging` |
| **Secrets prefix** | `AUTH_*`, `MONGODB_*` | `STG_AUTH_*`, `STG_MONGODB_*` |
| **Frontend** | Cloudflare Pages (prod) | Cloudflare Pages (staging) |

Both environments share the same self-hosted runner but run completely isolated containers, databases, and configurations.

## Testing

```bash
# Backend tests (unit + architecture)
cd backend/src/JobVault.API && dotnet test JobVault.sln

# Frontend tests
cd frontend/jobvault-ui && npm test
```
