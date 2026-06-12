# JobVault

## Repository layout

- `backend/src/` - .NET production projects
- `backend/tests/` - test projects (`JobVault.ArchitectureTests` and planned Unit/Integration tests)
- `services/webhook-server/` - deploy webhook service
- `docker/` - Dockerfiles for API and worker
- `docs/` - architecture, deployment, and environment setup docs
- `frontend/` - reserved placeholder for future UI

## Run locally

1. Copy env files:
   - `cp .env.example .env`
   - `cp services/webhook-server/.env.example services/webhook-server/.env`
2. Start with compose:
   - `docker compose up --build`
