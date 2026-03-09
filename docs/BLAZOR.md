ABOUTME: Blazor frontend architecture and operational patterns for this repository.
ABOUTME: Emphasizes non-obvious BFF, rendering, client-generation, and styling constraints.

# Blazor Frontend Architecture

## Scope
This document covers `Explore.Blazor` (BFF/server host) and `Explore.Blazor.Client` (UI), including auth flow, proxy behavior, render policy, and API client generation.

## Architecture Roles
1. `Explore.Blazor`:
   - hosts OIDC auth flow and BFF middleware,
   - proxies `/api/*` calls to `Explore.API` via YARP,
   - manages server-side token/session handling.
2. `Explore.Blazor.Client`:
   - contains pages/components,
   - uses service layer + generated API client,
   - does not own raw token persistence.

## BFF Endpoints (Non-Obvious)

Auth endpoints:
- `GET /auth/challenge`
- `GET /auth/login`
- `GET /auth/signout`
- `GET /auth/status`
- `GET /auth/debug` (development only, authorized)

Other BFF endpoints:
- `POST /bff/theme`
- `POST /bff/storage/upload-proxy`
- `POST /bff/setup-secret`
- `POST /bff/setup-secret/sync`
- `DELETE /bff/setup-secret`
- `GET /bff/me`

## Proxy Security Behavior

YARP transform behavior is security-sensitive:

1. access token is forwarded from server-side auth session to API request.
2. `X-Tenant-Slug` is forwarded when route context is available; the API resolves tenant identity authoritatively.
3. incoming `X-Setup-Secret` is stripped first, then replaced using trusted sources (header, cookie, server session).

## Render Strategy
1. Interactive rendering is configured through server + WASM render modes.
2. Route-level rendering policy decisions are governed by runtime policy services (see `docs/RENDER_POLICIES.md`).
3. Public experience settings are fetched from `api/PublicExperience/settings` and cached client-side for 5 minutes.

## Analytics Bootstrap And Degradation
1. `AnalyticsInitializer` reads `PublicExperienceSettingsDto` once after first render and initializes the JS bridge through `IAnalyticsInterop`.
2. Public bootstrap now carries provider, consent mode, transport mode, identify allowance, public API key, and endpoint URL.
3. Readiness rules are explicit: disabled analytics or provider `none` still initialize a safe no-op bridge, and `relay` mode stays eligible even when the browser has no public API key.
4. `direct` and `proxy` modes use the browser bridge to load provider-specific scripts; failures degrade to a no-op adapter.
5. `relay` mode does not require a browser API key and posts first-party pageview/custom events to `POST /api/a/t` instead of loading a vendor script.
6. `AnalyticsInitializer` owns browser pageview tracking: it emits one initial pageview after successful initialization, subscribes to `NavigationManager.LocationChanged`, tracks normalized path-only routes, and includes `navigation_source`, `tenant_id`, and prior-path `page_referrer` when available.
7. Client analytics should stay limited to pageviews and low-risk UI interaction signals. Server-side business events remain the responsibility of application handlers so they can be emitted exactly once from authoritative flows.
8. Relay transport currently supports pageview and constrained client custom events; identify remains policy-gated and is not emitted by the relay bridge.
9. Analytics bootstrap must never interfere with route rendering, startup navigation, or authenticated BFF flows.

## API Client Generation (Non-Obvious)
1. `Explore.Blazor.Client.csproj` runs target `GenerateApiClient` before `CoreCompile`.
2. NSwag input is `../Explore.API/swagger.json`; output is `Clients/EventApiClient.g.cs`.
3. Build also patches a known NSwag `void` return generation issue (`return default(void)!;` -> `return;`).
4. Development API startup exports/refreshes OpenAPI via `OpenApiExportService`.
5. DTO updates should use API-first workflow:
   - change API DTOs/handlers,
   - refresh exported OpenAPI,
   - rebuild Blazor client to regenerate types,
   - then update UI/services.

## Service Layer Pattern
1. Pages call application services, not generated client classes directly.
2. Services encapsulate API error handling and mapping for UI-friendly behavior.
3. Keep auth and tenant concerns centralized in shared handlers/services.

## State Management
1. URL/query state is the source of truth for filters/pagination where possible.
2. Use scoped services for cross-component UI state only when URL-based state is insufficient.
3. Keep page lifecycle async and cancellation-aware for long-running loads.

## Authentication And Access
1. Use standard authorize patterns at page/route level.
2. UI authorization is for UX gating; API remains the hard security boundary.
3. Do not store access tokens in browser storage as an application design pattern.

## Styling Rules
1. Prefer CSS isolation (`.razor.css`) per component.
2. Use clear BEM-style class names to keep scoped CSS readable.
3. Use `::deep` only when integration with third-party component internals requires it.
4. Avoid global CSS except variables, reset, and shared utilities.

## Common Pitfalls
1. Updating UI before regenerating NSwag client after API contract changes.
2. Treating client-side route guards as security enforcement.
3. Reintroducing direct token handling in WASM components.
4. Forgetting setup-secret forwarding rules when debugging onboarding.

## Related Docs
- `docs/API.md`
- `docs/CONTRIBUTING.md`
- `docs/RENDER_POLICIES.md`
- `docs/TROUBLESHOOTING.md`
