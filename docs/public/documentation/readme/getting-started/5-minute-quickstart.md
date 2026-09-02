---
description: Start the complete local development topology with .NET Aspire.
---

# 5-Minute Quickstart

This path is for a local evaluation workstation, not a production deployment.

## Prerequisites

Install:

* .NET 10 SDK;
* Docker;
* the .NET Aspire CLI.

## Start the platform

From a clean checkout:

```bash
git clone <repository-url>
cd Event
cp .env.example .env
aspire run --apphost Explore.AppHost/Explore.AppHost.csproj
```

Treat `.env.example` as a documented configuration schema, not production values. Secrets must come from approved external authorities and must never be committed.

Aspire starts declared infrastructure, injects service discovery and configuration, runs the migration service before the API and Blazor application, and exposes dynamic endpoints in its dashboard. Do not assume Aspire ports match Docker Compose ports.

## Acceptance checks

Before evaluating features, confirm that:

1. the migration resource exits successfully;
2. API and browser resources report healthy;
3. the setup flow is reachable from the dashboard endpoint;
4. Keycloak authentication completes;
5. a public read succeeds;
6. an authenticated write shows only actions present in HAL `_links`;
7. health responses and logs contain no secrets, connection strings, private storage paths, or PII.

Use `/alive` for process liveness, `/health` for readiness, and `/metrics` for bounded operational measurements. These surfaces must remain free of credentials and tenant/user data.

## Next step

Read [Architecture & Request Flows](architecture-and-request-flows.md), then choose a deployment under [Self-Hosting](../self-hosting/).
