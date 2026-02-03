# Troubleshooting

> **Project-Agnostic .NET Backend Troubleshooting Guide**
>
> Placeholders use `{Placeholder}` syntax - see [TEMPLATE_GLOSSARY.md](TEMPLATE_GLOSSARY.md).

**Last Updated**: January 2026

---

## Placeholder Substitutions

| Placeholder | Replace With | Example (ISLAMU Event) |
|-------------|--------------|------------------------|
| `{Project}` | Your solution name | `Explore` |
| `{Project}.API` | API project | `Explore.API` |
| `{Project}.Application` | Application project | `Explore.Application` |
| `{Project}.Domain` | Domain project | `Explore.Domain` |
| `{Project}.Persistence` | Persistence project | `Explore.Persistence` |
| `{Project}.Infrastructure` | Infrastructure project | `Explore.Infrastructure` |

---

This guide focuses on common issues when working on the **backend** projects:

- `{Project}.API`
- `{Project}.Application`
- `{Project}.Domain`
- `{Project}.Persistence`
- `{Project}.Infrastructure`
- Migration/worker services (if applicable)

### Implementation Example: ISLAMU Event

```
- Explore.API
- Explore.Application
- Explore.Domain
- Explore.Persistence
- Explore.Infrastructure
- Event.MigrationService
```

## Build & Restore

```powershell
dotnet restore
dotnet build
dotnet test
```

### Common issues

#### Target framework / SDK mismatch

All backend projects target `net10.0`. If your SDK is older, install the .NET 10 SDK.

#### "The type or namespace name 'X' could not be found"

Typical causes:

- Missing `using` (do not remove existing usings)
- Missing project reference (check `.csproj`)
- Wrong layer dependency (see `docs/ARCHITECTURE.md` and `docs/QUICK_REFERENCE.md`)

## OpenAPI / Swagger / Scalar

### Swagger UI loads but endpoints are missing metadata

Controllers should annotate actions with:

- `[EndpointSummary]`
- `[EndpointDescription]`
- `[ProducesResponseType]`

See `docs/API.md`.

### swagger.json file is stale

In Development, `{Project}.API` runs `OpenApiExportService` which exports a `swagger.json` file at startup.

If it doesn't update:

- Ensure `{Project}.API` starts successfully in Development.
- Check startup logs for the hosted service.
- Confirm the file is not locked by another process.

**Example (ISLAMU Event)**: `Explore.API` exports `swagger.json` to `Explore.API/swagger.json`

## Authentication / Authorization

### 401 Unauthorized on write endpoints

- Ensure you are sending `Authorization: Bearer <token>`.
- Confirm Keycloak settings in configuration match your environment.

### UserId missing in token

Controllers that require a user id MUST use the fallback claim extraction:

`sub` → `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` → `sid`

If all are missing, return `401`.

## Database / EF Core / Migrations

### Apply migrations (Development)

`{Project}.API` exposes a Development-only endpoint:

```http
POST /admin/migrate
```

It is protected with `.RequireAuthorization()`.

**Example (ISLAMU Event)**: `Explore.API` provides `POST /admin/migrate`

### Migration worker (if applicable)

If your project has a dedicated migration service, it's responsible for background migration/maintenance tasks.

**Example (ISLAMU Event)**: `Event.MigrationService` handles background migrations (see `Event.MigrationService/Program.cs`)

### Duplicate key violations

`GenericRepository.Create` catches PostgreSQL unique constraint violations (`SqlState == "23505"`) and rethrows with more context.

If you see duplicate key errors:

- Check unique constraints in EF configurations/migrations.
- Verify seed data and id generation.


### Development URLs (Default)

| Service | URL |
|---------|-----|
| Aspire Dashboard | `https://localhost:17225` |
| API | `https://localhost:7001` |
| Blazor | `https://localhost:7002` |
| Scalar API Docs | `https://localhost:7001/scalar/v1` |
| Swagger UI | `https://localhost:7001/swagger` |
