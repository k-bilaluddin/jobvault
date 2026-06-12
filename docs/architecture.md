# Architecture

JobVault is organized around a .NET backend and a lightweight deploy webhook service:

- `backend/src/` contains production .NET projects (`JobVault.API`, `JobVault.Worker`, `JobVault.Application`, `JobVault.Domain`, `JobVault.Contracts`, `JobVault.Infrastructure`).
- `backend/tests/` contains test projects (`JobVault.ArchitectureTests`) and reserves space for `JobVault.UnitTests` and `JobVault.IntegrationTests`.
- `services/webhook-server/` contains the Node.js webhook service used to trigger deployments.
- `docker/` contains Dockerfiles for backend API and worker images.

This layout separates deployable services, backend source, and tests for easier maintenance and CI targeting.
