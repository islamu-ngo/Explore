ABOUTME: Developer onboarding guide covering prerequisites, setup, and first-run workflow.
ABOUTME: From clone to running application in under 15 minutes.

# Getting Started

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| .NET SDK | 10.0+ | Runtime and build toolchain |
| Docker Desktop | Latest | Infrastructure services (PostgreSQL, Keycloak, MinIO) |
| .NET Aspire workload | Latest | Service orchestration and discovery |
| IDE | VS 2022+ / Rider / VS Code | Development environment |

Install the Aspire workload:

```bash
dotnet workload install aspire
```

## Clone And Build

```bash
git clone https://github.com/ISLAMU/Event.git
cd Event
dotnet build --configuration Release --verbosity quiet
```

Build must succeed with zero errors before proceeding. If it fails, see [TROUBLESHOOTING.md](TROUBLESHOOTING.md).

## Infrastructure

Start required services via Docker Compose:

```bash
docker compose up -d
```

This provisions:

| Service | Port | Purpose |
|---------|------|---------|
| PostgreSQL | 5432 | Primary database |
| Keycloak | 8080 | OIDC identity provider |
| MinIO | 9000/9001 | S3-compatible object storage |

## Run The Application

The application uses .NET Aspire for orchestration. All services start from a single command:

```bash
dotnet run --project Explore.AppHost
```

Aspire launches the API, Blazor BFF, and all dependent services with automatic configuration and service discovery.

### Local URLs

| Service | URL |
|---------|-----|
| Blazor UI | `https://localhost:7177` |
| API (direct) | `https://localhost:7039` |
| Swagger UI | `https://localhost:7039/swagger` |
| Scalar API docs | `https://localhost:7039/scalar` |
| Aspire Dashboard | `https://localhost:15888` |

## Running Tests

Run each test project individually — never use solution-level `dotnet test`:

```bash
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

All tests must pass before submitting changes. See [TESTING.md](TESTING.md) for project roles and detailed conventions.

## First Change Walkthrough

1. **Check active tasks** — read `dev/active/README.md` for current work items
2. **Create a branch** — use prefix conventions: `feat/`, `fix/`, `refactor/`, `docs/`
3. **Build first** — verify clean build before any changes
4. **Read relevant docs** — open the [docs index](index.md) for files related to your change area
5. **Write tests first** — TDD is the default; write failing test, then implement
6. **Implement** — follow [ARCHITECTURE.md](ARCHITECTURE.md) layer rules and [QUICK_REFERENCE.md](QUICK_REFERENCE.md) constraints
7. **Validate** — build + all 7 test projects must pass
8. **Submit PR** — follow [CONTRIBUTING.md](../CONTRIBUTING.md) checklist

## Key Conventions

- Every file starts with a two-line `ABOUTME:` header
- File-scoped namespaces for all new C# files
- Repositories return entities, not DTOs — mapping happens in handlers
- Validators are manually instantiated, not injected
- `GET` endpoints use `[AllowAnonymous]`; write endpoints use `[Authorize]`
- Commands return `BaseCommandResponse<Guid>` for creates

See [QUICK_REFERENCE.md](QUICK_REFERENCE.md) for the complete constraint list.

## Project Structure Overview

The solution follows Clean Architecture with two product families:

- **Event.\*** — domain, application, persistence, API (core business logic)
- **Explore.\*** — Blazor BFF, secrets management, infrastructure, AppHost

See [CODEBASE_STRUCTURE.md](CODEBASE_STRUCTURE.md) for the full directory map.

## Related

- [ARCHITECTURE.md](ARCHITECTURE.md) — system design and layer boundaries
- [CONTRIBUTING.md](../CONTRIBUTING.md) — PR workflow and validation
- [TESTING.md](TESTING.md) — test framework and project roles
- [CONFIGURATION.md](CONFIGURATION.md) — application settings and secrets
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) — common failure fixes
