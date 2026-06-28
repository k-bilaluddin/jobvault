# API Reference

## Ingestion

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/ingest/applications` | Ingest a job application payload (async, returns `202`) |
| `POST` | `/api/ingest` | Ingest raw payload |

## Applications

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/applications` | List all applications |
| `GET` | `/api/applications/{name}/report` | Get compatibility report |
| `GET` | `/api/applications/{name}/notes` | Get application notes |
| `GET` | `/api/applications/{name}/pdf/{type}` | Download CV or cover letter PDF |
| `GET` | `/api/applications/{name}/content` | Get editable CV/cover letter content |
| `GET` | `/api/applications/skills-gap` | Get skills gap analysis across all applications |
| `GET` | `/api/applications/historical` | Get historical (past) applications |
| `POST` | `/api/applications/{name}/stage` | Update pipeline stage |
| `POST` | `/api/applications/{name}/personal-notes` | Update personal notes |
| `POST` | `/api/applications/{name}/interviews` | Add interview record |
| `PUT` | `/api/applications/{name}/interviews/{idx}` | Update interview record |
| `DELETE` | `/api/applications/{name}/interviews` | Delete all interviews |
| `POST` | `/api/applications/{name}/notes` | Add a note |
| `PUT` | `/api/applications/{name}/notes/{noteId}` | Update a note |
| `DELETE` | `/api/applications/{name}/notes/{noteId}` | Delete a note |
| `PATCH` | `/api/applications/{name}/content` | Edit CV/cover letter content |
| `POST` | `/api/applications/{name}/regenerate` | Regenerate DOCX/PDF and sync to vault |
| `POST` | `/api/applications/sync-vault` | Sync all files from GitHub vault |

## Job Queue

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/ingest/queue` | List all queued jobs |
| `GET` | `/api/ingest/queue/pending` | Get pending jobs (for Routine) |
| `POST` | `/api/ingest/queue` | Add URL to queue |
| `PUT` | `/api/ingest/queue/{id}` | Update job status |
| `DELETE` | `/api/ingest/queue/{id}` | Delete a queued job |
| `DELETE` | `/api/ingest/queue/cleanup/{status}` | Bulk delete by status |

## Notifications

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/notifications` | Get recent notifications (last 50) |
| `GET` | `/api/notifications/stream` | SSE stream for real-time events |
| `POST` | `/api/notifications/read-all` | Mark all notifications as read |
| `POST` | `/api/notifications/{id}/read` | Mark a single notification as read |

## Auth & Settings

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/auth/login` | Authenticate and receive JWT token |
| `GET` | `/api/settings` | Get application settings |
| `PUT` | `/api/settings` | Update application settings |
