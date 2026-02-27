ABOUTME: High-signal implementation rules that are easy to miss when reading code quickly.
ABOUTME: Focuses on non-inferable constraints and project-specific behavior.

# Quick Reference

## Critical Rules
1. Repositories return entities, not DTOs; mapping happens in handlers.
2. Validators are manually instantiated in handlers/services (not injected as `IValidator<T>`).
3. Link/junction writes go through repositories, not direct navigation collection mutation.
4. Use `Guid` for core aggregates, `int` for most lookup IDs, `long` only for size/cursor style fields.
5. Do not rely on implicit business defaults in entities; set values in handler or EF config.
6. Keep file-scoped namespaces for new C# files.
7. Avoid deleting seemingly unused `using` statements blindly; verify build/test impact first.
8. `GET` endpoints are typically `[AllowAnonymous]`; write endpoints are protected with `[Authorize]`.
9. User ID fallback order is `sub` -> `nameidentifier` -> `sid`.
10. HAL responses are default; `Prefer: return=minimal` can remove link-heavy payloads.
11. Most create/update commands use `BaseCommandResponse<TId>`; many delete commands currently use `bool`.
12. Tenant isolation is enforced centrally by global query filters in `ExploreDbContext`; do not bypass casually.

## Multi-Tenancy Reminder
Runtime tenant resolution:
1. `X-Tenant-Id` header
2. custom domain
3. subdomain
4. default tenant fallback

Default fallback tenant ID: `018e4e5c-7f00-7000-8000-000000000001`.

## Auditing And Soft Delete
1. Auditable entities use `CreatedAt/By` and `UpdatedAt/By`.
2. Soft-deletable entities use `IsDeleted` (and often `DeletedAt/By`).
3. Named query filter `SoftDelete` is used so deleted rows stay hidden unless explicitly requested.

## Build And Test Baseline

1. Build: `dotnet build --configuration Release --verbosity quiet`
2. Run test projects individually with `dotnet test --project <path>.csproj` (not solution-level test).
3. Use `CLAUDE.md` for the exact current project list.

## Common Failure Patterns
1. DTO changed but NSwag client not regenerated.
2. Wrong tenant context while debugging list/query results.
3. Repository returning DTOs and leaking app-layer concerns.
4. Assuming one command-response pattern across all legacy features without checking local feature conventions.
