# Architecture

JobVault is organized around a .NET backend plus Docker-based deployment driven by GitHub Actions:

- `backend/src/` contains production .NET projects (`JobVault.API`, `JobVault.Worker`, `JobVault.Application`, `JobVault.Domain`, `JobVault.Contracts`, `JobVault.Infrastructure`).
- `backend/tests/` contains test projects (`JobVault.ArchitectureTests`) and reserves space for `JobVault.UnitTests` and `JobVault.IntegrationTests`.
- `docker/` contains Dockerfiles for backend API and worker images.
- `.github/workflows/ci-cd-with-webhook.yml` builds images in GitHub Actions and deploys them from the self-hosted Windows runner.

This layout separates deployable services, backend source, and tests for easier maintenance and CI targeting.
