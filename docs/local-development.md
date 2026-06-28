# Local Development

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [LibreOffice](https://www.libreoffice.org/) (for local Worker PDF conversion)
- MongoDB Atlas account
- CloudAMQP account (or local RabbitMQ)
- Telegram Bot token + chat ID
- GitHub personal access token (`repo` scope) + target repository

## 1. Clone and configure

```bash
git clone https://github.com/k-bilaluddin/jobvault.git
cd jobvault
cp .env.example .env
# fill in your values
```

## 2. Create the Docker network (first time only)

```bash
docker network create jobvault-internal
```

## 3. Run with Docker Compose

```bash
docker compose up -d
```

Starts `jobvault-api` and `jobvault-worker`. The generation service runs separately on `jobvault-internal`.

## 4. Run services locally

```bash
# API
cd backend/src/JobVault.API && dotnet run

# Worker
cd backend/src/JobVault.Worker && dotnet run

# Generation service (clone from https://github.com/k-bilaluddin/jobvault-generation-service)
cd jobvault-generation-service && npm install && npm start

# Frontend
cd frontend/jobvault-ui && npm install && npm run dev
```

## Environment Variables

All variables use `SCREAMING_SNAKE_CASE`. Copy `.env.example` and fill in your values. See [env.md](env.md) for the full reference with descriptions.

## Testing

```bash
# Backend tests (unit + architecture)
cd backend/src/JobVault.API && dotnet test JobVault.sln

# Frontend tests
cd frontend/jobvault-ui && npm test
```

