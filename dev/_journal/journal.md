## Technical Insights
- [2026-03-05 Europe/Brussels] ASP.NET Core minimal API bug: handlers with signature `async Task<IResult>(HttpContext ctx)` silently return empty 200 responses. `Task<IResult>` coerces to `Task` (RequestDelegate), so `IResult.ExecuteAsync` never runs. Fixed 4 handlers in `BffEndpointExtensions.cs` by switching to `async Task` with direct `ctx.Response.WriteAsJsonAsync()`. This ONLY affects handlers where `HttpContext` is the sole parameter; extra DI params prevent coercion.
- [2026-02-18 Europe/Brussels] ASP.NET Core rate limiting `AddPolicy` lambda returns `RateLimitPartition<string>` (not `RateLimitPartition<HttpContext, string>`). The `GetNoLimiter` generic type param must be the partition key type only.
- [2026-02-18 Europe/Brussels] `WebApplicationFactory` test clients have `null` or non-loopback `RemoteIpAddress`, so IP-based loopback bypass in rate limiters doesn't help tests. Must disable rate limiting entirely via environment check for "Testing".
- [2026-02-18 Europe/Brussels] `Asp.Versioning.Mvc` 8.1.0 media type versioning: configure with `AddApiVersionReader(new MediaTypeApiVersionReader("v"))` and `AssumeDefaultVersionWhenUnspecified = true`. Clients use `Accept: application/json;v=1.0`.
- [2026-02-18 Europe/Brussels] ETag middleware must buffer response body (using `MemoryStream` swap on `Response.Body`) to compute SHA256 hash before sending. Only applies to GET/HEAD with `application/json` or `application/hal+json` content types and 200 OK status.
- [2026-02-15 21:28 Europe/Brussels] Runtime provider selection should be centralized in `RuntimeAnalyticsProvider`; concrete provider `IsActive(...)` checks should validate local prerequisites (enabled/key presence) but avoid provider-id coupling to prevent stale-cache/provider mismatch behavior.
- [2026-02-15 21:28 Europe/Brussels] Public bootstrap payload must compute analytics readiness (`enabled && providerId > 0 && apiKey present`) to prevent first-load UI script churn and no-op/fail races.
- [2026-02-23 18:47 Europe/Brussels] When deleting Razor pages, Blazouter route registrations in `Explore.Blazor.Client/Routes.razor` must be cleaned in the same change-set or build fails with missing component type errors.

## Architectural Decisions
- [2026-02-18 Europe/Brussels] Chose media type versioning over URL versioning for API (user request). Accept header `application/json;v=1.0`. Keeps URLs clean, no route template changes, no `/v2/` path proliferation.
- [2026-02-18 Europe/Brussels] Rate limiting uses token bucket for global (better burst tolerance) and sliding window for authenticated (smoother distribution). Write operations use fixed window (simple, predictable).
- [2026-02-18 Europe/Brussels] All rate limit values are configurable via `appsettings.json` `RateLimiting:*` section with sensible code-level defaults. Testing environment gets no-op limiters.
- [2026-02-18 Europe/Brussels] Middleware pipeline order: SecurityHeaders -> CorrelationId -> RequestLogging -> ResponseCompression -> HTTPS -> Hateoas -> Routing -> Auth -> RateLimiter -> Authorization -> OutputCache -> ETag -> Controllers. ETag must be after OutputCache to hash final body.
- [2026-02-15 21:28 Europe/Brussels] Keep analytics provider abstraction thin (`Identify`, `Track`, `PageView`, `GroupIdentify`) and isolate feature flags via a separate capability interface with safe defaults.
- [2026-02-15 21:28 Europe/Brussels] JS analytics bridge enforces no-op initialization when API key is empty, independent of provider flag, to preserve graceful degradation.

## Failed Approaches
- [2026-03-22 Europe/Brussels] Considered storing the tenant theme catalog as JSON in a generic hierarchical setting (`appearance.available_themes`). Rejected because it would turn theme management into an accidental mini-database with weak concurrency, poor fallback semantics, and brittle admin editing behavior.
- [2026-03-16 Europe/Brussels] Avoid describing the target registration model as “EventRegistration becomes the abstract parent.” In this codebase, `EventRegistration` is already mentally attached to session-scoped access rows, so that wording increases migration ambiguity and risks muddling attendance/capacity semantics.
- [2026-03-13 Europe/Brussels] Attempted to recover completed handoff explore-agent results via `background_output(...)` after the system reminder, but the task IDs were no longer retrievable in this session context. For context-reset handoff, write the synthesis into dev docs immediately instead of depending on later retrieval.
- [2026-03-13 Europe/Brussels] Tried standard `dotnet test --filter ...` again while validating the Blazor setup-secret slice; this repo's TUnit runner still rejects or ignores the standard filter flow. Use full project runs or runner-compatible partitioning instead.
- [2026-02-15 21:28 Europe/Brussels] Attempted to filter TUnit tests via standard `--filter` flow; this runner uses different option handling and rejected the argument. Use project runs and targeted suite partitioning instead.
- [2026-02-23 18:12 Europe/Brussels] Tried using `rg --files dev/active` from shell for inventory, but local `rg` shim failed with permission error in this environment. Switched to `glob`/`grep` + Python file append script.
- [2026-02-23 18:47 Europe/Brussels] Initial category edit dialog wiring used `CategoryDto` from API details fetch, but dialog parameter expects `CategoryListDto`; this caused compile errors and was corrected by passing the list DTO directly.
- [2026-02-27 Europe/Brussels] Attempted PowerShell-style orphan CSS check in Bash shell; command failed due to shell syntax mismatch. Switched to POSIX loop over `git ls-files` and completed orphan check successfully.

## Deferred Fixes
- [2026-03-22 Europe/Brussels] Create the appearance architecture ADR before any code implementation for hierarchical settings preferences. The plan is updated, but the ADR file itself still needs to be written.
- [2026-03-16 Europe/Brussels] When implementation starts for event scheduling, decide the exact parent-table name (`EventRegistrationIntent` vs `EventRegistrationGroup`) before touching migrations or DTOs so API and DB naming do not drift.
- [2026-03-13 Europe/Brussels] Do not enable explicit antiforgery validation on state-changing BFF minimal API endpoints yet. There is currently no discoverable `X-CSRF-TOKEN` / `XSRF-TOKEN` request-header propagation path in the client-side codebase, so enforcement would likely break existing browser flows.
- [2026-03-13 Europe/Brussels] Replace the reflection-based `CircuitAccessTokenService.SetToken(...)` workaround in `Explore.Blazor/Extensions/MiddlewareExtensions.cs` with a clean direct contract before finishing the middleware pass.
- [2026-03-13 Europe/Brussels] Finish the controller-wide anonymous error payload sweep (`ActorKeyStoreController`, `ModuleController`, `EventRegistrationController`, and any remaining `BadRequest(new { error = ... })` / `NotFound(new { error = ... })` paths) so Phase 2 can be closed honestly.
- [2026-03-13 Europe/Brussels] Complete the repo-wide `CancellationToken` and `HttpResponseMessage` disposal audit across the remaining `Explore.Blazor.Client/Services/*` implementations; the highest-risk slice is done, but the broad audit is not.
- [2026-02-18 Europe/Brussels] Add `RateLimiting:*` and `Cors:AllowedOrigins` config sections to `appsettings.json` with explicit default values.
- [2026-02-18 Europe/Brussels] Document media type versioning, rate limiting headers, ETag support in `docs/API.md`.
- [2026-02-18 Europe/Brussels] Add architecture test enforcing `[ApiVersion]` on all controllers (Phase 6.3).
- [2026-02-18 Europe/Brussels] Implement idempotency keys (Phase 3.3) and cursor-based pagination (Phase 3.4) in future session.
- [2026-02-18 Europe/Brussels] BusinessMetrics counters defined but not yet wired into command handlers. Wire `events.created`, `registrations.created`, `organizations.created` counters into respective handlers.
- [2026-02-15 21:28 Europe/Brussels] Add CSP documentation and validation for analytics script hosts (PostHog/Plausible/RudderStack) before production rollout.
- [2026-02-15 21:28 Europe/Brussels] Add integration tests for runtime provider switch SLA (within 60s cache window) and UI-level graceful degradation checks.
- [2026-02-23 18:12 Europe/Brussels] Implement admin consolidation code changes tracked in `dev/active/navbar-customization/*` (tenant panel Organizations/Lookup sections, instance panel SMTP section, NavMenu updates, remove `/admin` and standalone lookup pages), then run diagnostics/build/tests.
- [2026-02-23 18:47 Europe/Brussels] Perform manual browser smoke verification for tenant/instance admin pages (organizations actions, lookup CRUD dialogs, SMTP save/test flow, role-based navbar entries).
- [2026-02-23 18:47 Europe/Brussels] Address pre-existing MudBlazor analyzer/nullability warnings repository-wide in a separate quality pass.
- [2026-02-27 Europe/Brussels] Update every checkbox in `dev/active/blazor-folder-restructure/blazor-folder-restructure-tasks.md` to exactly mirror completed migration actions (currently summarized in execution-status section).
- [2026-02-27 Europe/Brussels] Run the full mandatory multi-project test matrix from `CLAUDE.md` before final release/merge gate for this large restructure.

## Key Decisions
- [2026-03-22 Europe/Brussels] For hierarchical settings preferences, keep the settings engine responsible for precedence and references only; model tenant theme catalogs as first-class relational entities and keep user theme selection as sparse `UserPreference` overrides.
- [2026-03-22 Europe/Brussels] Do not let `MainLayout` or `SetupLayout` own theme precedence or palette mapping rules. Introduce a dedicated runtime service boundary (`IThemeCompositionService` / `IAppearanceRuntimeService`) before implementation.
- [2026-03-16 Europe/Brussels] For event scheduling refactor planning, keep registration modeling as: parent rows preserve intent/policy semantics, child rows remain concrete session entitlements/access records. This is easier to migrate from the current session-only `EventRegistration` table and keeps attendance/capacity logic understandable.
- [2026-03-16 Europe/Brussels] For the first scheduling rollout, same-room overlapping sessions should be rejected in create/update session DTO validators using async FluentValidation repository-backed checks. Stronger persistence/database hardening can be layered later if needed.
- [2026-03-13 Europe/Brussels] For the HTTP resilience refactor, API auth scheme selection should be treated as not tenant-sensitive in this repo: `Explore.API/Program.cs` chooses API-key vs JWT auth based on `X-API-Key`, so tenant resolution does not need to move ahead of authentication for scheme selection.
- [2026-03-13 Europe/Brussels] Forwarded-header trust must be explicit in both hosts via `ForwardedHeaders:KnownProxies` / `KnownNetworks`, with development-only trust-all fallback when config is empty; do not keep the previous BFF behavior that blindly cleared trust lists.
- [2026-03-13 Europe/Brussels] BFF setup-secret throttling should be keyed by authenticated user first, then antiforgery/session cookie, and only fall back to IP as a last resort. This is now implemented in `Explore.Blazor/Extensions/RateLimitingExtensions.cs` as policy `BffSetupSecret`.
- [2026-03-13 Europe/Brussels] Do not treat minimal-API antiforgery as a safe toggle in this tree yet. The middleware/token cookie are present, but there is no discoverable client-side request-header propagation for `X-CSRF-TOKEN` / `XSRF-TOKEN`, so endpoint enforcement must wait for an explicit client path and render-mode verification.
- [2026-03-13 Europe/Brussels] The client antiforgery path is now centralized in `Explore.Blazor.Client/Services/Http/BrowserCredentialsMessageHandler.cs`, which injects `X-CSRF-TOKEN` for mutating same-origin requests by reading the existing `XSRF-TOKEN` cookie. Use this shared handler path instead of ad hoc antiforgery token reads in individual services.
- [2026-02-23 18:12 Europe/Brussels] For context-reset safety, append a standardized session update block to every `dev/active/*-context.md` and `dev/active/*-tasks.md`, then add detailed track-specific handoff only in the active track (`navbar-customization`).
- [2026-02-23 18:12 Europe/Brussels] Keep admin consolidation implementation plan in the existing `navbar-customization` track rather than creating a new active track folder to avoid split ownership before code changes begin.
- [2026-02-23 18:47 Europe/Brussels] Keep SMTP settings persistence in instance governance `SystemSetting` keys (`GovernanceSettingKeys.Email*`) with explicit CQRS handlers and service abstraction, mirroring storage settings architecture.
- [2026-02-23 18:47 Europe/Brussels] Remove legacy standalone admin pages/routes now that their functionality is consolidated into panel sections to avoid duplicate admin surfaces.
- [2026-02-27 Europe/Brussels] For Blazor dialog migrations, enforce static `ShowAsync(...)` in `.razor.cs` partials and keep `.razor` markup-only where possible; this prevents call-site drift and preserves testable invocation patterns.
- [2026-02-27 Europe/Brussels] For context-limit handoff, update every active task folder with an explicit session checkpoint entry, then maintain deep implementation state in the actually active track file.
- [2026-02-27 Europe/Brussels] Adopt root `Explore.Blazor.Client/Contracts` as the client public API boundary; keep `Services/` for implementations only and split contracts by `Services`, `Providers`, and `Interop`.

## Technical Insights
- [2026-02-27 Europe/Brussels] Bulk namespace refactors in C# must include Razor `@using` directives (`_Imports.razor` + feature dialogs/pages); otherwise builds fail with repetitive `Services.Contracts` resolution errors despite C# files being updated.
- [2026-02-27 Europe/Brussels] Adding a dedicated `Explore.Blazor.Client/GlobalUsings.Contracts.cs` significantly reduces churn during contract namespace migrations and avoids per-file import drift.

## Technical Insights
- [2026-02-23 18:12 Europe/Brussels] Admin claims/UI integration in this codebase is claim-driven (`IsInstanceAdmin`, `IsTenantAdmin`, `HasAnyAdminAuthority` in `NavMenu.razor.cs`), so navbar/admin visibility changes should stay claim-based instead of introducing new role checks in UI components.

## Technical Insights
- [2026-02-16 01:55 Europe/Brussels] In Blazor `InteractiveAuto`, components in client assembly can be instantiated during server prerender; any injected service must exist in server DI too. Added server no-op `IAnalyticsInterop` implementation to prevent prerender resolution failures.

## Notification System Implementation (2026-03-03 → 2026-03-04)

### Technical Insights
- [2026-03-03 Europe/Brussels] `ExecuteUpdateAsync` with timestamp cutoff is the correct pattern for bulk mark-all-read — prevents race condition where new notifications arrive during operation.
- [2026-03-03 Europe/Brussels] Project uses a SINGLE `MappingProfile.cs` file for all AutoMapper mappings (not per-feature profiles). Add a `CreateXxxMappings()` private method and call from constructor.
- [2026-03-03 Europe/Brussels] `required` keyword on navigation properties (e.g., `required User User`) means test initializers MUST include `= null!` assignments or compilation fails with CS9035.
- [2026-03-03 Europe/Brussels] NSubstitute mock calls must match EXACT parameter count including new optional params. Adding `int? notificationScopeId = null` to repository method requires updating ALL mock `.Received()` calls to include the extra `null` argument.
- [2026-03-03 Europe/Brussels] EF Core named query filters: `.HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted)` — uses `QueryFilterNames` constants.
- [2026-03-04 Europe/Brussels] LookupTableSeeder uses runtime seeding (not HasData) due to EF Core 10 bug #36682 with circular FKs.
- [2026-03-04 Europe/Brussels] Actor has Pii (1:1 extension table) with `DisplayName`. Repo queries need `.ThenInclude(a => a!.Pii)` for display names.
- [2026-03-04 Europe/Brussels] FK to ActorType uses `DeleteBehavior.Restrict` (scope must not be orphaned), FK to Actor uses `DeleteBehavior.SetNull` (actor deletion shouldn't cascade to notifications).

### Failed Approaches
- [2026-03-04 Europe/Brussels] Considered replacing `UserId` with `ActorId` for notification targeting (Option A). Rejected because it kills the hot read path — would require JOIN through Actor→User to query "my notifications", and complicates the most frequent access pattern.
- [2026-03-04 Europe/Brussels] Considered NotificationRecipient junction table (Option C) for multi-recipient support. Rejected as over-engineered — materialized fan-out gives the same result with simpler queries and no N+1 risks.

### Key Decisions
- [2026-03-03 Europe/Brussels] Notification endpoints are ALL `[Authorize]` — deviates from project default where GET is `[AllowAnonymous]`, but notifications are personal data.
- [2026-03-03 Europe/Brussels] Lookup entities (NotificationType, NotificationEntityType) follow ApprovalStatus pattern exactly: int Id, MasterCode, FullName, Description + companion enum.
- [2026-03-04 Europe/Brussels] Materialized fan-out: notifications always per-user (UserId stays), org/group notifications fan out at write time → N rows per member. Read path stays O(1).
- [2026-03-04 Europe/Brussels] Reuse existing ActorType entity as notification scope (User=1→Personal, Organization=2, Group=4, System=5) instead of creating new NotificationScope lookup.
- [2026-03-04 Europe/Brussels] Bots/System are senders not receivers — they should consume domain events directly, not notifications. But they CAN be SourceActorId.

### Deferred Fixes
- [2026-03-04 Europe/Brussels] EF migration for notification system not yet created. All schema changes are in EF configs but no migration generated.
- [2026-03-04 Europe/Brussels] Notification dispatch handlers (domain events → fan-out logic) not yet implemented — will need OrganizationMember/GroupMember queries.
- [2026-03-04 Europe/Brussels] Push delivery (WebSocket/SSE) for real-time notifications not yet implemented.
- [2026-03-04 Europe/Brussels] Per-user notification preferences not yet implemented.
