# Environment Variables

## Precedence and ownership

1. Root `.env` (from `.env.example`) is system-wide and loaded by API/worker services in `docker-compose.yml`.

Deployment-specific secrets for the self-hosted GitHub Actions runner stay in the repository's GitHub Actions secrets instead of repo `.env` files.

## Templates

- Root template: `.env.example`
