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
7. Avoid deleting seemingly unused `using` statements blindly.
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
21. **HAL links are the single source of truth for UI**: Clients must gate action affordances (Edit, Delete, etc.) by checking for the presence of the corresponding link in the `_links` object, never by local role/claim inspection.
22. Normalized lookup DTOs expose `*Id`, `*Code`, and `*Name`; do not expose persisted enum wrappers in API contracts.
23. **Blazor is fully isolated from API implementation layers**: `Explore.Blazor`, `Explore.Blazor.Client`, and their tests must not reference Domain, Application, Infrastructure, or Persistence. Backend communication and backend/domain models come only from the generated `IEventApiClient` contract.
24. **EF Core migrations and model snapshots are generated artifacts**: Never hand-edit them. Correct the entity/configuration or migration-generation extension, delete the unapplied development migration, and regenerate it with `dotnet ef migrations`.
25. **External behavior research is clean-room only**: Implementation context may contain neutral functional requirements and repository-native design material, never third-party source, snippets, ASTs, SQL, migrations, tests, comments, or assets. Independently design the implementation's structure, sequence, and organization and record provenance under [`docs/legal/IP_GOVERNANCE.md`](legal/IP_GOVERNANCE.md).
26. **Dependencies must preserve outbound licensing options**: Do not add a library, package, image, asset, or generated component whose terms prevent ISLAMU-owned material from being offered under any outbound license the Project Steward may select under the CLA. Third-party material always retains its own terms; commercial or exceptional use requires documented approval for each distribution mode.

## Multi-Tenancy Reminder
Runtime tenant resolution:
1. trusted `X-Tenant-Slug` header from the BFF
2. custom domain
3. subdomain
4. unresolved multi-tenant request fails closed (`404`)

Single-tenant fallback default tenant ID: `018e4e5c-7f00-7000-8000-000000000001`.

Governance settings resolution uses a **5-tier cascade**: User → Group → Organization → Tenant → Instance. Instance-level locks prevent higher-tier overrides unless in single-tenant mode.

## Auditing And Soft Delete
1. Auditable entities use `CreatedAt/By` and `UpdatedAt/By`.
2. Soft-deletable entities use `IsDeleted` (and often `DeletedAt/By`).
3. Named query filter `SoftDelete` is used so deleted rows stay hidden unless explicitly requested.

## Build And Test Baseline

1. Build: `dotnet build --configuration Release --verbosity quiet`
2. Run test projects individually with `dotnet test --project <path>.csproj` (not solution-level test).
3. Use `AGENTS.md` for the exact current project list.

## Common Failure Patterns
1. DTO changed but NSwag client not regenerated.
2. Wrong tenant context while debugging list/query results.
3. Repository returning DTOs and leaking app-layer concerns.
4. Assuming one command-response pattern across all legacy features without checking local feature conventions.
5. Changing middleware pipeline order in Program.cs without understanding dependencies (e.g., rate limiting after auth).
6. Adding a normalized lookup table without stable IDs/codes in `LookupTableSeeder` and matching DTO metadata projection.

## Controller-Authoring Standard (Forward Policy)

Every new controller action MUST have:

1. **Explicit route template** — `[HttpGet("resource/{id:guid}", ...)]` not bare `[HttpGet]`
2. **Explicit route name** — `Name = RouteNames.Xxx` on every `[Http*]` attribute
3. **Explicit endpoint class** — one of `[EndpointClassification(EndpointClass.Public)]`, `Authenticated`, `Admin`, or `PublicTransactional` on controller or action
4. **Explicit response typing** — `[ProducesResponseType<T>]` for success + error responses
5. **No overloaded semantics** — one action per HTTP verb + route template combination

Enforced by: `ApiContractArchitectureTests`, `EndpointClassificationArchitectureTests`, `ContractInvariantsTests`.

## API Rate Limiting Quick Reference

| Policy | Key | Limit | Window |
|---|---|---|---|
| `global` | IP | 200 tokens | Refill 40/10s |
| `authenticated` | User ID | 200 requests | 60s sliding |
| `write` | User ID | 30 requests | 60s fixed |
| `setup_secret` | IP | 5 requests | 60s fixed |
| `public_transactional` | Effective remote IP | 10 requests | 60s fixed, queue 0 |

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

## Correct vs Wrong (Top 10 Violations)

| # | Rule | Correct | Wrong |
|---|---|---|---|
| 1 | Repository return type | `Task<Event?> GetByIdAsync(Guid id)` returning the aggregate | `Task<EventDto?> GetByIdAsync(Guid id)` (leaks mapping into the repo) |
| 2 | Validator lifetime | `var validator = new CreateEventValidator(); var result = await validator.ValidateAsync(cmd, ct);` | Constructor-injected `IValidator<T>` (DI-bound) |
| 3 | ID types | `public int CountryId` (lookup), `public Guid Id` (aggregate), `public long Size` (bytes/cursor) | `public long CountryId` for a lookup FK |
| 4 | GET auth attribute | `[HttpGet("events", Name = RouteNames.ListEvents)] [AllowAnonymous]` | `[HttpGet]` with no explicit route template or name |
| 5 | User ID extraction | Helper that falls back `sub` → `nameidentifier` → `sid` (exact order) | `User.FindFirst(ClaimTypes.NameIdentifier)?.Value` alone |
| 6 | UI action gating | `@if (dto.HasHalLink("edit")) { <AppButton /> }` driven by API `_links` | `@if (authState.User.IsInRole("Admin"))` (local claim check) |
| 7 | Tenant filter override | `ctx.Events.IgnoreQueryFilters([QueryFilterNames.SoftDelete])` (named filter only) | `ctx.Events.IgnoreQueryFilters()` (drops Tenant filter — security bug) |
| 8 | Specification builder | `var spec = new EventQuerySpecification().WithTitle(x).WithDate(y);` returns new instances | `spec.Title = x; spec.Date = y;` (mutates existing instance) |
| 9 | Command response | `Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand cmd, CancellationToken ct)` | `Task<EventDto> Handle(CreateEventCommand cmd)` (missing wrapper + no CT) |
| 10 | HAL link policy | `yield return new LinkDefinition("edit", Url.Link(RouteNames.EditEvent, new { id })!, HttpMethods.Put);` | `links.Add(new LinkDefinition(...))` (list mutation instead of `yield return`) |

These are enforced by `Event.Architecture.Tests` and the `.agents/rules/` path-scoped rule files — see [`.agents/rules/README.md`](../.agents/rules/README.md).
