## Technical Insights
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
- [2026-02-15 21:28 Europe/Brussels] Attempted to filter TUnit tests via standard `--filter` flow; this runner uses different option handling and rejected the argument. Use project runs and targeted suite partitioning instead.
- [2026-02-23 18:12 Europe/Brussels] Tried using `rg --files dev/active` from shell for inventory, but local `rg` shim failed with permission error in this environment. Switched to `glob`/`grep` + Python file append script.
- [2026-02-23 18:47 Europe/Brussels] Initial category edit dialog wiring used `CategoryDto` from API details fetch, but dialog parameter expects `CategoryListDto`; this caused compile errors and was corrected by passing the list DTO directly.

## Deferred Fixes
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

## Key Decisions
- [2026-02-23 18:12 Europe/Brussels] For context-reset safety, append a standardized session update block to every `dev/active/*-context.md` and `dev/active/*-tasks.md`, then add detailed track-specific handoff only in the active track (`navbar-customization`).
- [2026-02-23 18:12 Europe/Brussels] Keep admin consolidation implementation plan in the existing `navbar-customization` track rather than creating a new active track folder to avoid split ownership before code changes begin.
- [2026-02-23 18:47 Europe/Brussels] Keep SMTP settings persistence in instance governance `SystemSetting` keys (`GovernanceSettingKeys.Email*`) with explicit CQRS handlers and service abstraction, mirroring storage settings architecture.
- [2026-02-23 18:47 Europe/Brussels] Remove legacy standalone admin pages/routes now that their functionality is consolidated into panel sections to avoid duplicate admin surfaces.

## Technical Insights
- [2026-02-23 18:12 Europe/Brussels] Admin claims/UI integration in this codebase is claim-driven (`IsInstanceAdmin`, `IsTenantAdmin`, `HasAnyAdminAuthority` in `NavMenu.razor.cs`), so navbar/admin visibility changes should stay claim-based instead of introducing new role checks in UI components.

## Technical Insights
- [2026-02-16 01:55 Europe/Brussels] In Blazor `InteractiveAuto`, components in client assembly can be instantiated during server prerender; any injected service must exist in server DI too. Added server no-op `IAnalyticsInterop` implementation to prevent prerender resolution failures.
