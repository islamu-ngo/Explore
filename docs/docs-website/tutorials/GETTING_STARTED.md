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
2. Create local environment defaults:
   `cp .env.example .env`
3. Validate the resolved Compose model:
   `docker compose config`
4. Start core services:
   `docker compose up -d postgres redis keycloak-db keycloak keycloak-init islamu-event-api islamu-event-ui`
5. Open:
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
- Coop moderation: `docker compose --profile moderation up -d`
- Osprey signal provider: `docker compose --profile osprey up -d` after setting an accessible `OSPREY_IMAGE`

## Option 2: Local Aspire Orchestration

Run:
- `aspire run --apphost Explore.AppHost/Explore.AppHost.csproj`

This starts full local infrastructure, migration, API, and Blazor with local development wiring.

Expected local URLs:
- Use the Aspire dashboard output for exact dynamic Blazor and API endpoints.
- Keycloak local full mode uses `http://localhost:8080/auth`.

## Development Test User (Keycloak Realm Import)

- Username: `demo`
- Password: `demo1234`

## Common Startup Issues

1. API contract/client mismatch after DTO change:
   start API in Development and rebuild `Explore.Blazor.Client` to regenerate NSwag client.
2. Auth redirect problems behind proxy:
   verify `X-Forwarded-Proto`/`X-Forwarded-Host` forwarding.
3. Tenant mismatch:
   confirm `deployment.mode`, trusted `X-Tenant-Slug` forwarding, and host-based tenant resolution behavior.
