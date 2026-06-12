# Deployment

## Local run with Docker Compose

1. Copy environment templates:
   - `cp .env.example .env`
   - `cp services/webhook-server/.env.example services/webhook-server/.env`
2. Start services:
   - `docker compose up --build`

`docker-compose.yml` builds backend images using:
- `docker/api.Dockerfile`
- `docker/worker.Dockerfile`

and builds the webhook service from `services/webhook-server/`.

## CI notes

The workflow in `.github/workflows/ci-cd-with-webhook.yml`:
- restores/builds/tests from `backend/src/JobVault.API/JobVault.sln`
- builds container images from `docker/api.Dockerfile` and `docker/worker.Dockerfile`
- triggers the deploy webhook after successful image publish on `master` pushes.
