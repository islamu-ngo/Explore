ABOUTME: Short runnable onboarding path for contributors and local evaluators.
ABOUTME: Links deeper docs instead of duplicating self-hosting, testing, and contribution guides.

# Getting Started

> **Audience:** Contributors | Evaluators | AI agents
> **Status:** Implemented
> **Owner:** Contributor Experience
> **Last Verified:** 2026-05-06
> **Source Anchors:** `global.json`, `Explore.AppHost/AppHost.cs`, `docker-compose.yml`, `docs/SELF_HOSTING.md`, `docs/TESTING.md`

Use this page for the shortest safe local path. For production-style hosting, start with [SELF_HOSTING.md](SELF_HOSTING.md) instead.

## Prerequisites

| Tool | Version | Purpose |
|---|---|---|
| .NET SDK | `10.0.200-preview.0.26103.119` or compatible SDK from `global.json` | Build and run the solution. |
| Docker / Docker Compose v2 | Current | Local infrastructure and self-hosting stack. |
| .NET Aspire workload | Current compatible workload | Local AppHost orchestration. |

Install the Aspire workload if it is not already installed:

```bash
dotnet workload install aspire
```

## Clone And Build

```bash
git clone https://github.com/islamu-ngo/Event.git
cd Event
dotnet build --configuration Release --verbosity quiet
```

Build errors must be fixed before continuing. For recurring failures, use [TROUBLESHOOTING.md](TROUBLESHOOTING.md).

## Option A: Run The Aspire Development Loop

Aspire is the preferred local development loop because `Explore.AppHost` starts the migration service before API and Blazor, then wires API discovery into Blazor without hardcoded ports.

```bash
dotnet run --project Explore.AppHost
```

Use the Aspire dashboard output to find exact dynamic endpoints. Do not assume the AppHost uses the same ports as Docker Compose.

## Option B: Run The Compose Stack

Use Compose when you want the self-hosting topology locally:

```bash
API_ENDPOINT=http://explore-api:8080/ docker compose up -d postgres redis keycloak-db keycloak explore-api explore-blazor
```

Default Compose endpoints:

| Service | URL |
|---|---|
| Blazor BFF/UI | `http://localhost:7002` |
| API | `http://localhost:7039` |

Swagger, Scalar, and OpenAPI JSON endpoints are mapped only for Development/Testing API runs. The Compose stack uses `ASPNETCORE_ENVIRONMENT=Production`; use [API.md](API.md) and [API_COOKBOOK.md](API_COOKBOOK.md) for API guidance unless you intentionally run the API in a development profile.

Optional profiles:

```bash
docker compose --profile storage up -d
docker compose --profile authz up -d
```

For setup-secret behavior, Keycloak, reverse proxy, migrations, storage, and backups, use [SELF_HOSTING.md](SELF_HOSTING.md).

## First Contribution Path

1. Read [FIRST_CONTRIBUTION.md](FIRST_CONTRIBUTION.md) for the shortest docs-only or small-bug PR path.
2. Read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a PR.
3. Read the source-owned doc for your area from [docs/index.md](index.md).
4. Make the smallest safe change.
5. Record docs impact: `Updated`, `Not needed`, or `Deferred`.

## Validation

Run the build and affected test projects individually. Do not run solution-level `dotnet test`.

Minimum documentation/architecture check:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Typical related-project examples:

```bash
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

See [TESTING.md](TESTING.md) for the full per-project validation matrix, TUnit conventions, and E2E expectations.

## Related

- [README.md](../README.md) — product overview and role-based docs map.
- [SELF_HOSTING.md](SELF_HOSTING.md) — production-style Compose topology and operator guidance.
- [CONFIGURATION.md](CONFIGURATION.md) — runtime settings and secret-provider mappings.
- [FIRST_CONTRIBUTION.md](FIRST_CONTRIBUTION.md) — first PR path.
- [CONTRIBUTING.md](CONTRIBUTING.md) — contribution workflow and PR checklist.
- [TESTING.md](TESTING.md) — test project roles and commands.
