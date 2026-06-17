# JobVault

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet)
![Vue 3](https://img.shields.io/badge/Vue-3.x-4FC08D?style=flat-square&logo=vuedotjs)
![MongoDB](https://img.shields.io/badge/MongoDB-Atlas-47A248?style=flat-square&logo=mongodb)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-CloudAMQP-FF6600?style=flat-square&logo=rabbitmq)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat-square&logo=docker)
![GitHub Actions](https://img.shields.io/badge/CI%2FCD-GitHub_Actions-2088FF?style=flat-square&logo=githubactions)

**Live:** [Frontend](https://jobvault.kbilaluddin.dev) · [API / Swagger](https://api.kbilaluddin.dev/swagger/index.html)

---

## Motivation

Job hunting is repetitive work disguised as a decision-making process.

Every application follows the same pattern: find a role, read the JD, evaluate fit, rewrite a CV, craft a cover letter, send it, and then lose track of where it went. Multiply that by dozens of applications and the overhead becomes significant — not because the work is hard, but because most of it is mechanical.

The goal behind JobVault is simple: **remove everything that a machine can do better than you**, and leave only the part that requires human judgement — deciding whether a role is worth pursuing in the first place.

---

## What is JobVault

JobVault is a **personal job application automation platform**. You paste a job URL into Claude, and JobVault handles everything from that point forward: it generates a tailored CV and cover letter, converts them to PDF, commits all files to GitHub, updates a live dashboard, and sends you a real-time notification — all without touching your keyboard again.

The workflow looks like this:

```
You paste a job URL into Claude
        ↓
Claude evaluates the role, scores the match, and builds a structured payload
        ↓
Payload is sent to the JobVault API
        ↓
Worker picks it up, calls the Generation Service to produce DOCX files
        ↓
LibreOffice converts both documents to PDF
        ↓
All six files (CV + cover letter, both formats + two markdown reports)
are committed to a private GitHub vault
        ↓
The dashboard updates. Telegram notifies you. Done.
```

The only step that stays manual is deciding which jobs to look at. Everything else is automated.

---

## Architecture

![JobVault Architecture](docs/architecture.svg)

### Services

| Service | Technology | Responsibility |
|---|---|---|
| **API** | .NET 9 / ASP.NET Core | Ingestion endpoint, persistence, event publishing |
| **Worker** | .NET 9 Worker Service | Consumes RabbitMQ events, orchestrates processing pipeline |
| **Generation Service** | Node.js / TypeScript | Generates `.docx` CV and cover letter from structured payload |
| **Frontend** | Vue 3 / TypeScript / Pinia | Dashboard, pipeline board, interviews, skills gap, real-time SSE |
| **MongoDB** | Atlas | Stores application records |
| **RabbitMQ** | CloudAMQP | Async event bus with dead-letter queue |
| **GitHub** | REST API | Stores final application files (DOCX + PDF + reports) |
| **Telegram** | Bot API | Push notifications on application events |

---

## End-to-End Flow

1. **Claude** evaluates a job posting and POSTs a structured JSON payload to `POST /api/ingest/applications`
2. **API** validates the payload, persists the application to MongoDB with status `Processing`, and publishes a `received` event to RabbitMQ — returns `202 Accepted` immediately
3. **Worker** consumes the event, fetches the application, and calls the Generation Service in parallel for CV and cover letter
4. **Generation Service** renders Word documents from the payload (role bullets, skills, cover letter paragraphs)
5. **Worker** converts both DOCX files to PDF via LibreOffice
6. **Worker** commits all six files to the GitHub vault repository: `{CV}.docx`, `{CV}.pdf`, `{CoverLetter}.docx`, `{CoverLetter}.pdf`, `compatibility-report.md`, `tailoring-notes.md`
7. **MongoDB** status is updated to `Ready to Apply` with the commit URL
8. **RabbitMQ** fan-out notifies the Telegram bot and SSE stream simultaneously
9. **Frontend** receives the SSE event and updates in real time; **Telegram** delivers the push notification

**Failure handling:** transient failures (generation service down, GitHub network error) retry 3× with exponential backoff. After exhausting retries the message is dead-lettered and the application is marked `Failed`. Permanent failures (invalid payload, 4xx from generation service) skip retries and dead-letter immediately.

---

## Features

### Backend
- Async ingestion pipeline with immediate `202` response
- Event-driven processing via RabbitMQ topic exchange
- Dead-letter queue with retry/fast-fail distinction (4xx vs 5xx)
- W3C distributed trace propagation across services
- LibreOffice DOCX → PDF conversion in the Worker container
- GitHub vault commit via Git Trees API (6-file atomic commit)
- Telegram notifications with application details

### Frontend
- **Dashboard** — stats cards, applications-over-time chart, pipeline funnel, score distribution
- **Pipeline board** — Kanban across Processing → Ready to Apply → Applied → Interview → Offer → Rejected
- **Applications list** — searchable and filterable by stage
- **Company detail** — match score, role bullets, interview history, files
- **Interviews view** — all interviews across applications grouped by company
- **Skills gap** — identifies missing skills across job postings with severity indicators
- **Real-time SSE notifications** — bell icon, unread count, auto-reconnect with backoff
- **PWA** — installable, offline-capable with Workbox caching
- **Dark mode** — CSS variable theming via Tailwind

---

## Project Structure

```
jobvault/
├── backend/
│   ├── src/
│   │   ├── JobVault.API/               # Controllers, Program.cs, Swagger
│   │   ├── JobVault.Application/       # Interfaces, use cases, service contracts
│   │   ├── JobVault.Domain/            # Entities and value objects
│   │   ├── JobVault.Infrastructure/    # MongoDB, RabbitMQ, GitHub, Telegram, Generation client
│   │   ├── JobVault.Contracts/         # Request/response DTOs, events
│   │   └── JobVault.Worker/            # Background consumer, hosted services
│   └── tests/
│       └── JobVault.ArchitectureTests/ # Enforces Clean Architecture layer rules
├── frontend/
│   └── jobvault-ui/                    # Vue 3 / TypeScript / Pinia SPA
├── generation-service/                 # Node.js DOCX generation service
├── docker/
│   ├── api.Dockerfile
│   ├── worker.Dockerfile
│   └── fonts/                          # Calibri fonts for LibreOffice
├── docker-compose.yml
├── .github/
│   └── workflows/
│       └── ci-cd-with-webhook.yml
└── .env.example
```

---

## Tech Stack

| Layer | Technology |
|---|---|
| API | .NET 9, ASP.NET Core, Clean Architecture |
| Worker | .NET 9 Worker Service |
| Document Generation | Node.js, TypeScript, `docx` library |
| PDF Conversion | LibreOffice (headless, in Worker container) |
| Frontend | Vue 3, TypeScript, Pinia, Vue Router, Tailwind CSS |
| Real-time | Server-Sent Events (SSE) |
| PWA | vite-plugin-pwa, Workbox |
| Database | MongoDB Atlas |
| Message Broker | RabbitMQ (CloudAMQP) |
| Notifications | Telegram Bot API |
| File Vault | GitHub (Git Trees API) |
| Containers | Docker, Docker Compose |
| Registry | GitHub Container Registry (GHCR) |
| CI/CD | GitHub Actions |
| Hosting | Self-hosted (Windows → Hetzner), Cloudflare Tunnel |

---

## Local Development

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [LibreOffice](https://www.libreoffice.org/) (for local Worker PDF conversion)
- MongoDB Atlas account
- CloudAMQP account (or local RabbitMQ)
- Telegram Bot token + chat ID
- GitHub personal access token (`repo` scope) + target repository

### 1. Clone and configure

```bash
git clone https://github.com/k-bilaluddin/jobvault.git
cd jobvault
cp .env.example .env
# fill in your values
```

### 2. Create the Docker network (first time only)

```bash
docker network create jobvault-internal
```

### 3. Run with Docker Compose

```bash
docker compose up -d
```

Starts `jobvault-api` and `jobvault-worker`. The generation service runs separately on `jobvault-internal`.

### 4. Run services locally

```bash
# API
cd backend/src/JobVault.API && dotnet run

# Worker
cd backend/src/JobVault.Worker && dotnet run

# Generation service
cd generation-service && npm install && npm start

# Frontend
cd frontend/jobvault-ui && npm install && npm run dev
```

---

## Environment Variables

| Variable | Description |
|---|---|
| `MongoDb__ConnectionString` | MongoDB Atlas connection URI |
| `MongoDb__DatabaseName` | Database name (e.g. `jobvault`) |
| `MongoDb__JobApplicationsCollectionName` | Collection name |
| `RabbitMq__ConnectionString` | CloudAMQP / RabbitMQ AMQP URI |
| `Telegram__BotToken` | Telegram bot token |
| `Telegram__ChatId` | Destination Telegram chat ID |
| `GitHub__Token` | PAT with `repo` scope |
| `GitHub__Owner` | GitHub username |
| `GitHub__Repository` | Target vault repository name |
| `GitHub__Branch` | Branch to commit to (default: `master`) |
| `GitHub__CvFileName` | CV file base name (without extension) |
| `GitHub__CoverLetterFileName` | Cover letter file base name |
| `DocumentGeneration__BaseUrl` | Generation service URL (default: `http://jobvault-generation-service:3000`) |
| `LibreOffice__ExecutablePath` | LibreOffice binary path (default: `libreoffice`; override for Windows dev) |

---

## API Reference

Full spec: [Swagger UI](https://api.kbilaluddin.dev/swagger/index.html) · [Postman Collection](JobVault.postman_collection.json)

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/ingest/applications` | Ingest a job application payload (async, returns `202`) |
| `GET` | `/api/notifications` | Get recent notifications (last 50) |
| `GET` | `/api/notifications/stream` | SSE stream for real-time events |
| `POST` | `/api/notifications/read-all` | Mark all notifications as read |
| `POST` | `/api/notifications/{id}/read` | Mark a single notification as read |

---

## CI/CD

```
Push to master
      ↓
① Architecture tests
      ↓
② Build & push API image  ──┐
                             ├── parallel → GHCR
③ Build & push Worker image ┘
      ↓
④ Self-hosted runner: docker compose pull && up -d
      ↓
⑤ Telegram deployment notification
```

---

## Testing

```bash
# Architecture enforcement tests
cd backend/tests/JobVault.ArchitectureTests && dotnet test
```

---

## Roadmap

- [x] Async ingestion pipeline (API → RabbitMQ → Worker)
- [x] Document generation service (DOCX via Node.js)
- [x] LibreOffice PDF conversion in Worker container
- [x] GitHub vault commit (6-file atomic commit per application)
- [x] Dead-letter queue with retry/fast-fail distinction
- [x] W3C distributed trace propagation
- [x] Real-time SSE notifications
- [x] Vue 3 frontend with dashboard, pipeline, interviews, skills gap
- [x] PWA support
- [x] Telegram notifications
- [x] GitHub Actions CI/CD + self-hosted deployment
- [ ] DLQ management UI (list failed messages, retry button)
- [ ] Job discovery inside JobVault (remove the Claude manual step)
- [ ] Interview scheduling and tracking improvements
- [ ] Expanded dashboard analytics and history views
- [ ] Health checks and observability

---

## Author

**Khawaja Bilal Uddin** — Senior Full Stack Developer, Frankfurt am Main  
[kbilaluddin.dev](https://kbilaluddin.dev) · [GitHub](https://github.com/k-bilaluddin)
