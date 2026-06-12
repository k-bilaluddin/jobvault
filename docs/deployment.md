# Deployment

## Local run with Docker Compose

1. Copy environment templates:
   - `cp .env.example .env`
2. Start services:
   - `docker compose up --build`

`docker-compose.yml` builds backend images using:
- `docker/api.Dockerfile`
- `docker/worker.Dockerfile`

## CI notes

The workflow in `.github/workflows/ci-cd-with-webhook.yml`:
- restores/builds/tests from `backend/src/JobVault.API/JobVault.sln`
- builds container images from `docker/api.Dockerfile` and `docker/worker.Dockerfile`
- deploys on the self-hosted Windows runner after successful image publish on `master` pushes by running `docker compose pull` and `docker compose up -d` from the checked-out runner workspace.
