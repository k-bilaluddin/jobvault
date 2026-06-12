# Environment Variables

## Precedence and ownership

1. Root `.env` (from `.env.example`) is system-wide and loaded by API/worker services in `docker-compose.yml`.
2. Service-level `.env` files (for example `services/webhook-server/.env`) are loaded for that specific service only.

If the same variable exists in both files, the value from the service-level file applies only to that service container, while other services continue using root `.env`.

## Templates

- Root template: `.env.example`
- Webhook template: `services/webhook-server/.env.example`

Webhook service also supports deploy endpoint throttling via `DEPLOY_RATE_WINDOW_MS` and `DEPLOY_RATE_MAX`.
