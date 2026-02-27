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
2. `X-Tenant-Id` is forwarded when present.
3. incoming `X-Setup-Secret` is stripped first, then replaced using trusted sources (header, cookie, server session).

## Render Strategy
1. Interactive rendering is configured through server + WASM render modes.
2. Route-level rendering policy decisions are governed by runtime policy services (see `docs/RENDER_POLICIES.md`).
3. Public experience settings are fetched from `api/PublicExperience/settings` and cached client-side for 5 minutes.

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
