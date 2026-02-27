ABOUTME: Minimal getting-started path for running the stack locally with Docker or Aspire.
ABOUTME: Uses current repository paths, service names, ports, and seed/dev auth details.

# Tutorial: Getting Started

## Prerequisites

- Docker Desktop
- .NET 10 SDK (preview, aligned with `global.json`)
- Git

## Option 1: Docker Compose

1. Clone and enter the repo:
   `git clone https://github.com/islamu-ngo/Event.git && cd Event`
2. Start core services:
   `docker compose up -d`
3. Open:
   - Blazor UI: `http://localhost:7002`
   - API: `http://localhost:7039`
   - Swagger: `http://localhost:7039/swagger`
   - Keycloak: `http://localhost:8080` (admin: `admin` / `admin`)

First run note:
- if instance onboarding is incomplete, `/` redirects to `/setup`.
- when `SETUP_SECRET` is not provided, API generates one at startup, prints it in API logs, and keeps it valid for 60 minutes.

Optional profiles:
- Storage: `docker compose --profile storage up -d`
- Cerbos: `docker compose --profile authz up -d`

## Option 2: Local Aspire Orchestration

Run:
- `dotnet run --project Explore.AppHost`

This starts migration, API, and Blazor with local development wiring.

Expected local URLs:
- Blazor: `https://localhost:7177`
- API: `https://localhost:7039`

## Development Test User (Keycloak Realm Import)

- Username: `demo`
- Password: `demo1234`

## Common Startup Issues

1. API contract/client mismatch after DTO change:
   start API in Development and rebuild `Explore.Blazor.Client` to regenerate NSwag client.
2. Auth redirect problems behind proxy:
   verify `X-Forwarded-Proto`/`X-Forwarded-Host` forwarding.
3. Tenant mismatch:
   confirm `deployment.mode` and `X-Tenant-Id` behavior.
