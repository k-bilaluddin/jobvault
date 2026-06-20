# JobVault

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet)
![Vue 3](https://img.shields.io/badge/Vue-3.x-4FC08D?style=flat-square&logo=vuedotjs)
![MongoDB](https://img.shields.io/badge/MongoDB-Atlas-47A248?style=flat-square&logo=mongodb)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-CloudAMQP-FF6600?style=flat-square&logo=rabbitmq)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat-square&logo=docker)
![GitHub Actions](https://img.shields.io/badge/CI%2FCD-GitHub_Actions-2088FF?style=flat-square&logo=githubactions)

**Live:** [Frontend](https://jobvault.kbilaluddin.dev) · [API / Swagger](https://api.kbilaluddin.dev/swagger/index.html)

---

## The Problem

We all use AI to generate tailored CVs and cover letters now, but the workflow around it is still manual and messy.

**Generating the documents** is the first friction point. You either copy-paste AI output into your CV template by hand, or ask the AI to generate the entire file, which burns tokens and rarely gets the formatting right. Either way, you're spending time on something a machine should handle.

**Tracking what you sent** is the second. After 20+ applications, you can't remember which version of your CV went to which company. Most of us end up maintaining a spreadsheet, manually updating it after every application, hoping we remember to do it.

**Accuracy** is the third. AI will happily invent experience you don't have. If you let it write freely, you end up reviewing every bullet point to make sure it's real, which defeats the purpose of automating in the first place.

I hit all three problems and built JobVault to close the gap.

---

## How It Works

The idea is simple: **AI generates the content, machines handle everything else.**

**1. A curated bullet-point library**
As a software engineer, I work across different tech stacks, but not every stack belongs on every CV. So I maintain `.md` files that list all my real accomplishments, grouped by technology and domain. When Claude sees a job description, it picks the relevant bullet points from this library instead of inventing them. Nothing fabricated, nothing generic.

**2. Content-only handoff**
Claude doesn't generate files. It evaluates the JD, selects the right bullets, writes a tailored cover letter, and sends the **content as structured data** to the JobVault API. No tokens wasted on formatting or file generation.

**3. Automated document pipeline**
The API takes over from there:
- Generates properly formatted CV and cover letter (DOCX)
- Converts both to PDF
- Commits all files to a private GitHub repository (your vault)
- Updates the database and live dashboard
- Sends a Telegram notification

**4. Everything is tracked**
Every application, the company, the role, match score, documents, status, all of it lives in the dashboard. When a recruiter calls, you pull up the company and see exactly what you sent. No spreadsheet required.

```
Paste a job URL into Claude
        ↓
Claude picks bullets from your library, builds a structured payload
        ↓
Payload hits the JobVault API
        ↓
Worker generates DOCX → converts to PDF → commits to GitHub vault
        ↓
Dashboard updates. Telegram notifies you. Done.
```

The only manual step is deciding which jobs to apply to. Everything after pasting the JD runs on its own.

---

## Architecture

![JobVault Architecture](docs/architecture.svg)

### Performance

The API handles 20 concurrent users at **11ms average response time** with **0% error rate** under sustained load (k6 load test, 2,069 requests over 2 minutes).

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
├── generation-service/ → [jobvault-generation-service](https://github.com/k-bilaluddin/jobvault-generation-service)  # Node.js DOCX generation service (separate repo)
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

## Engineering Challenges

**Preventing AI hallucinations**
Claude doesn't write bullet points from scratch. It selects from a curated `.md` library of real accomplishments grouped by technology and domain. The prompt constrains it to pick, not invent, so every line on the CV maps to actual experience.

**Atomic 6-file GitHub commits**
Each application produces six files (CV + cover letter in DOCX and PDF, plus two markdown reports). These are committed in a single atomic operation using the Git Trees API, so the vault never contains a partial application.

**DOCX-to-PDF consistency in Linux containers**
LibreOffice renders fonts differently depending on what's installed. The Worker container bundles native Calibri font files so PDFs match the DOCX output exactly, regardless of the host environment.

**Dead-letter queue with retry/fast-fail distinction**
Not all failures deserve retries. Transient errors (network timeout, generation service down) retry 3x with exponential backoff. Permanent failures (invalid payload, 4xx responses) skip retries and dead-letter immediately, so the queue doesn't waste time on requests that will never succeed.

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

# Generation service (clone from https://github.com/k-bilaluddin/jobvault-generation-service)
cd jobvault-generation-service && npm install && npm start

# Frontend
cd frontend/jobvault-ui && npm install && npm run dev
```

---

## Environment Variables

All variables use `SCREAMING_SNAKE_CASE`. Copy `.env.example` and fill in your values. See [docs/env.md](docs/env.md) for the full reference with descriptions.

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
[kbilaluddin.dev](https://kbilaluddin.dev) · [GitHub](https://github.com/k-bilaluddin) · [LinkedIn](https://www.linkedin.com/in/kbilaluddin/)
