
# Troubleshooting

This guide focuses on common issues when working on the **backend** projects:

- `Explore.API`
- `Explore.Application`
- `Explore.Domain`
- `Explore.Persistence`
- `Explore.Infrastructure`
- `Event.MigrationService`

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

In Development, `Explore.API` runs `OpenApiExportService` which exports a `swagger.json` file at startup.

If it doesn’t update:

- Ensure `Explore.API` starts successfully in Development.
- Check startup logs for the hosted service.
- Confirm the file is not locked by another process.

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

`Explore.API` exposes a Development-only endpoint:

```http
POST /admin/migrate
```

It is protected with `.RequireAuthorization()`.

### Migration worker

`Event.MigrationService` is responsible for background migration/maintenance tasks (see `Event.MigrationService/Program.cs`).

### Duplicate key violations

`GenericRepository.Create` catches PostgreSQL unique constraint violations (`SqlState == "23505"`) and rethrows with more context.

If you see duplicate key errors:

- Check unique constraints in EF configurations/migrations.
- Verify seed data and id generation.
