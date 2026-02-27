ABOUTME: Practical troubleshooting guide for this repository's API, BFF, and tenant-aware runtime.
ABOUTME: Prioritizes repeat incidents and non-obvious checks over generic .NET advice.

# Troubleshooting

## Quick Triage Order

1. Check `https://localhost:7039/health` and `/alive`.
2. Check API startup logs for migration/seed failures.
3. Verify deployment mode and tenant resolution behavior.
4. Verify auth session (`/auth/status`) and token forwarding through BFF.
5. Check rate limiting (`429`) and request timeout (`504`) before deeper debugging.

## Build And Test Failures

Run from solution root:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity normal
```

Important:
- run tests with `--project` (not solution-level `dotnet test`).
- if failures are unclear, generate TRX:
  `dotnet test --project <project.csproj> --configuration Release -- --report-trx --report-trx-filename results.trx`

## OpenAPI / NSwag Drift

Symptoms:
- Blazor compile errors after DTO changes.
- Missing/old generated client types.

Checks:
1. Ensure API starts in `Development` so `OpenApiExportService` refreshes `Explore.API/swagger.json`.
2. Rebuild `Explore.Blazor.Client`; its `GenerateApiClient` target regenerates `Clients/EventApiClient.g.cs`.
3. Confirm `swagger.json` timestamp changed.

## Auth And BFF Issues

### 401 on write endpoints

Checks:
- user is authenticated in BFF (`GET /auth/status`).
- YARP is forwarding bearer token to `/api/*`.
- API token contains expected audience (`explore-api` or `explore-blazor-server`) and valid issuer.

### OIDC redirect URI errors behind proxy

Cause:
- forwarded proto/host not propagated, so app computes wrong callback URL.

Check:
- proxy sends `X-Forwarded-Proto` and `X-Forwarded-Host`.
- forwarded headers middleware is active in Blazor server pipeline.

## Setup Secret Failures

Symptoms:
- onboarding blocked at `/setup`.
- setup calls return `410`, `400`, or `502/503`.

Checks:
1. API logs: setup mode active vs completed.
2. BFF endpoints:
   - `POST /bff/setup-secret`
   - `POST /bff/setup-secret/sync`
3. ensure secret is not being injected directly by client headers; proxy strips and re-resolves trusted value.
4. auto-generated setup secrets expire after 60 minutes from API startup.

## Tenant Resolution Problems

Symptoms:
- wrong tenant branding/data.
- tenant-scoped data appears empty.

Resolution order in API:
1. `X-Tenant-Id` header
2. custom domain
3. subdomain
4. default tenant fallback (`018e4e5c-7f00-7000-8000-000000000001`)

Checks:
- host headers (`X-Forwarded-Host` / host).
- `deployment.mode` value (`SingleTenant` vs `MultiTenant`).

## Missing HAL Links

If `_links` are missing:
- confirm request did not include `Prefer: return=minimal`.
- link pruning can be authorization-driven (user lacks action permission).

## 429 / 504 Responses

`429`:
- triggered by API rate limiting policies (`Global`, `Authenticated`, `Write`, `SetupSecret`).
- inspect `Retry-After` header and caller behavior.

`504`:
- request timeout policy exceeded (`Default`, `Lookup`, `Complex`).
- verify endpoint timeout category and long-running query behavior.

## Local URLs

- API: `https://localhost:7039`
- Swagger: `https://localhost:7039/swagger`
- Scalar: `https://localhost:7039/scalar/v1`
- Blazor (dotnet): `https://localhost:7177`
- Blazor (docker compose): `http://localhost:7002`
