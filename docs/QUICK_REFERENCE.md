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
13. Exception handling uses chained `IExceptionHandler` (not middleware); all errors return RFC 7807 ProblemDetails.
14. Rate limiting is disabled in `Testing` environment (all policies replaced with `NoLimiter`).
15. Middleware pipeline order in `Program.cs` is critical — do not rearrange without understanding dependencies.
16. HATEOAS link policies use `yield return` pattern; each entity has separate detail and collection policies.
17. `EventQuerySpecification` is an immutable builder — every `With*()` call returns a new instance.
18. Module-conditional filters (Islamic, Tech) are silently ignored when the module is disabled for the tenant.
19. ETag middleware uses weak ETags (SHA256) — only on `application/json` and `application/hal+json` responses.
20. Named route constants in `RouteNames` must match `[HttpGet(Name = "...")]` attribute values on controller actions.

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
5. Changing middleware pipeline order in Program.cs without understanding dependencies (e.g., rate limiting after auth).
6. Adding a new lookup table without matching enum and HasData() seed — both must be synchronized.

## API Rate Limiting Quick Reference

| Policy | Key | Limit | Window |
|---|---|---|---|
| `global` | IP | 200 tokens | Refill 40/10s |
| `authenticated` | User ID | 200 requests | 60s sliding |
| `write` | User ID | 30 requests | 60s fixed |
| `setup_secret` | IP | 5 requests | 60s fixed |

All disabled in `Testing` environment.

## Caching Quick Reference

| Layer | Scope | Duration | Applied At |
|---|---|---|---|
| Output Cache `LookupData` | HTTP response | 1 hour | Controller `[OutputCache]` |
| Output Cache `ListData` | HTTP response | 30 seconds | Controller `[OutputCache]` |
| Output Cache `DetailData` | HTTP response | 60 seconds | Controller `[OutputCache]` |
| HybridCache | Domain entity/DTO | 30 min (5 min local) | MediatR handler |
| ETag | Conditional request | N/A (304 check) | Middleware |

## Request Timeout Quick Reference

| Policy | Duration |
|---|---|
| `Default` | 30 seconds |
| `Lookup` | 10 seconds |
| `Complex` | 60 seconds |
