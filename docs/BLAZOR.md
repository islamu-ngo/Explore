ABOUTME: Contributor guide for the Blazor BFF host and client application boundaries.
ABOUTME: Keeps token handling, proxying, render policy, service state, and client generation source-grounded.

# Blazor Frontend Architecture

> **Audience:** Contributors | Frontend | AI agents
> **Status:** Implemented
> **Owner:** Frontend
> **Last Verified:** 2026-05-06
> **Source Anchors:** `Explore.Blazor/Program.cs`, `Explore.Blazor/Extensions/`, `Explore.Blazor.Client/Explore.Blazor.Client.csproj`, `Explore.Blazor.Client/Services/`, `Explore.Blazor.Client/Layout/`, `docs/RENDER_POLICIES.md`, `docs/DESIGN_SYSTEM.md`

## Scope

This document is the contributor-facing guide for `Explore.Blazor` and `Explore.Blazor.Client`. It focuses on the BFF trust boundary, proxy behavior, render-policy consumption, service/state patterns, and generated API client workflow.

Use the specialized docs for deep detail:

| Area | Canonical doc |
|---|---|
| API contract, HAL, idempotency | [API.md](API.md) |
| Task-first API usage | [API_COOKBOOK.md](API_COOKBOOK.md) |
| Auth/security model | [SECURITY.md](SECURITY.md) |
| Render policy and public SEO behavior | [RENDER_POLICIES.md](RENDER_POLICIES.md) |
| Design tokens, wrappers, CSS layers | [DESIGN_SYSTEM.md](DESIGN_SYSTEM.md) |
| Accessibility rules | [ACCESSIBILITY.md](ACCESSIBILITY.md) |
| Localization stack | [LOCALIZATION.md](LOCALIZATION.md) |

## Project Roles

| Project | Role | Must not own |
|---|---|---|
| `Explore.Blazor` | Server/BFF host: OIDC login, HttpOnly cookie session, YARP proxy, BFF endpoints, token forwarding, antiforgery, rate limiting. | Raw domain logic or browser token storage. |
| `Explore.Blazor.Client` | Razor UI, pages/components, scoped UI state, typed service layer, generated API DTO consumption. | API authorization decisions, raw access-token persistence, or direct controller logic. |

The browser never owns access tokens. Interactive UI calls go through the BFF or generated client services; API remains the hard authorization boundary.

## BFF Endpoint Families

BFF endpoints are split by concern in `Explore.Blazor/Extensions/` and wired through the server host.

| Endpoint family | Representative routes | Source |
|---|---|---|
| Authentication | `/auth/challenge`, `/auth/login`, `/auth/signout`, `/auth/status`, `/auth/providers`, `/auth/debug` | `BffAuthEndpoints.cs` |
| Auth refresh support | `/bff/auth/refresh-schemes`, `/bff/auth/refresh-session`, `/bff/auth/refresh-session/internal` | `BffAuthEndpoints.cs` |
| User/session view | `/bff/me` | `BffPreferenceEndpoints.cs` |
| Preferences and appearance | `/bff/theme`, `/bff/language`, `/bff/direction`, `/bff/ui-themes`, `/bff/appearance/*` | `BffPreferenceEndpoints.cs` |
| Setup secret | `GET/POST/DELETE /bff/setup-secret`, `/bff/setup-secret/sync` | `BffSetupSecretEndpoints.cs` |
| Storage upload proxy | `/bff/storage/upload-session`, `/bff/storage/upload-proxy` | `BffStorageEndpoints.cs` |

Keep new BFF endpoints in the smallest matching extension file. `BffEndpointExtensions.cs` should remain the facade/orchestrator, not a dumping ground for endpoint logic.

## Proxy And Token Forwarding

There are two related but separate transport paths:

1. **Browser-to-API proxy path** — `YarpProxyExtensions.cs` proxies `/api/*` calls and applies security-sensitive transforms:
   - forwards the server-side access token as `Authorization: Bearer ...`,
   - forwards trusted tenant context when route context is available,
   - strips any browser-supplied `X-Setup-Secret` value and forwards only the value returned by `ISetupSecretResolver`.
2. **Server-side typed client path** — `HttpClientExtensions.cs` registers outgoing clients with separate handlers:
   - `AccessTokenForwardingHandler`,
   - `TenantHeaderForwardingHandler`,
   - `SetupSecretForwardingHandler`,
   - `BffCookieForwardingHandler` for self/BFF calls that must preserve cookie/XSRF context.

All server-side handlers use `UseCookies = false` where applicable to avoid pooled `CookieContainer` leakage between requests.

## Setup Secret Boundary

Setup-secret handling is intentionally BFF-owned:

1. The browser sees only BFF-mediated cookie/session state, not the raw setup-secret persistence model.
2. Setup-secret forwarding uses `ISetupSecretResolver` with this trusted source order: BFF-owned setup handshake/session state, protected BFF-issued setup cookie, then explicit local/development/bootstrap configuration fallback. Inbound request headers are never a setup-secret source.
3. The setup cookie is protected with ASP.NET Core Data Protection, `HttpOnly`, short-lived, invalidated by the BFF setup-secret endpoints, and `Secure` outside local development.
4. `SameSite=Lax` is intentional because onboarding may cross top-level OIDC redirects before the first administrator completes setup.
5. Setup-secret validation is rate-limited at the BFF edge and again at the API edge.
6. The BFF limiter partitions requests by authenticated user when available, then antiforgery/session cookie state, then IP as the final fallback.

When debugging onboarding, check both BFF setup-secret endpoints and API setup-secret validation rather than adding client-side storage shortcuts.

## Auth Diagnostic Boundary

Authentication challenge and OIDC callback failures are intentionally safe by default:

1. Server redirects back to `/login` include only `challengeError=1`, a normalized `errorCode`, and a correlation ID.
2. Raw IdP exception text, token endpoint response bodies, client IDs, client-secret prefixes, client-secret lengths, and `errorDetail` query values are not browser-visible.
3. BFF logs use safe structured fields from `ISafeAuthDiagnosticsPolicy`; provider response bodies and secret-derived metadata stay out of production-path logs.
4. The login page renders a generic failure message and provider choices. It must not render legacy `errorDetail` query values if an old URL is reused.

## BFF Antiforgery Boundary

Cookie-authenticated BFF mutations use a double-submit-style antiforgery contract:

1. `UseAntiforgeryTokenMiddleware` issues a JavaScript-readable `XSRF-TOKEN` cookie on `GET` requests by calling `IAntiforgery.GetAndStoreTokens`.
2. `Program.cs` configures ASP.NET Core antiforgery to validate the `X-CSRF-TOKEN` request header.
3. `BrowserCredentialsMessageHandler` sends browser credentials and adds `X-CSRF-TOKEN` for `POST`, `PUT`, `PATCH`, and `DELETE` requests when the token cookie is present.
4. `BffCookieForwardingHandler` preserves cookie/XSRF context for InteractiveServer self-calls that legitimately call BFF endpoints from the server.
5. Unsafe preference and appearance BFF endpoints must call `.ValidateAntiforgery()`. Missing or invalid tokens return `400` with `Antiforgery validation failed`.
6. Positive protected examples include `/bff/auth/refresh-schemes`, `/bff/auth/refresh-session`, `/bff/storage/upload-proxy`, and the preference/appearance mutation endpoints.
7. Documented exceptions are setup-secret bootstrap endpoints and `/bff/auth/refresh-session/internal`; these use separate credentials/authorization constraints because initial setup and server-side onboarding calls cannot reliably satisfy browser antiforgery semantics.

## Storage Upload Proxy Boundary

Browser-mediated storage uploads use BFF-owned upload sessions rather than caller-supplied destination URLs:

1. The browser asks `/bff/storage/upload-session` for an upload session. The BFF calls the API generate-upload-url endpoint server-side, validates the returned presigned destination shape, and stores the exact approved destination in distributed cache.
2. The browser receives an opaque `uploadSessionId`, object key, view URL, and expiry. It does not need to send a raw presigned upload URL back to the BFF proxy.
3. The browser uploads bytes to `/bff/storage/upload-proxy` with `uploadSessionId`, `contentType`, and `file`. The proxy resolves the session, verifies the authenticated user and content type, and PUTs only to the exact server-issued destination.
4. `/bff/storage/upload-proxy` rejects arbitrary HTTPS or presigned-looking URLs because client-provided destinations are not trusted.
5. Upload sessions are short-lived, user-bound, content-type-bound, and consumed after successful proxy upload. Both storage BFF endpoints remain protected by authorization and antiforgery validation.
6. Non-browser/server paths may still use direct presigned upload URLs where the server code owns the trusted URL; browser paths must use the BFF upload-session flow.

## Auth-State Serialization Boundary

The browser authentication state is intentionally display-only:

1. `Explore.Blazor` configures `AddAuthenticationStateSerialization` with `AuthStateSerializationPolicy.SerializeDisplaySafeClaimsAsync`.
2. Serialized claims are limited to display/name hints: `name`, `preferred_username`, `given_name`, and `family_name`.
3. Serialized claims exclude email, `sub`, `sid`, `ClaimTypes.NameIdentifier`, `internal_user_id`, tenant identifiers, roles, permissions, admin claims, tokens, and any action-authority claims.
4. UI authorization, tenancy, feature access, and action affordances must come from BFF status endpoints, API/HAL `_links`, or other server-confirmed service responses.
5. Browser claim checks are not an authority source. If a UI action needs authority metadata and no HAL/status contract exists, record the missing affordance instead of synthesizing it from roles or cached claims.

## API Client Generation

`Explore.Blazor.Client/Explore.Blazor.Client.csproj` runs the NSwag generation target before `CoreCompile`:

1. API DTO/controller contract changes are made first.
2. The API build refreshes the checked-in build-time OpenAPI contract at `Explore.API/swagger.json`.
3. The Blazor client build regenerates `Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
4. Pages/components consume application services, not `EventApiClient` directly.

Generated DTOs preserve HAL `_links` through extension data. Per-resource UI affordances must be gated by HAL links from the API, not by duplicating role checks in Razor components.

## Render And Public Experience

Route rendering decisions are governed by runtime policy services and documented in [RENDER_POLICIES.md](RENDER_POLICIES.md). In Blazor code, the important contributor rules are:

1. Do not hardcode server/static/interactive route behavior in components.
2. Use `RuntimeRenderPolicyService` and the public-experience services instead of duplicating route-group logic.
3. `PublicExperienceService` caches public experience settings and shell data client-side for five minutes.
4. Tenant public-experience settings are source data; component code should treat them as inputs and degrade safely when optional sections are absent.

The broader hierarchical settings cascade belongs in configuration/render-policy docs, not in component code.

## Service And State Patterns

Pages should stay thin. They call scoped services that encapsulate generated-client calls, mapping, and UI-friendly error handling.

Use URL/query state for filters and pagination whenever it represents navigable state. Use scoped services for cross-component UI state only when URL state is insufficient.

Source-grounded examples:

| Pattern | Examples |
|---|---|
| Layout/shell state | `MainLayout.razor.cs`, `AppSideNav.razor.cs`, `AnnouncementBar.razor.cs` |
| Cross-component event bridge | `CookieConsentStateService` |
| Public-experience cache | `PublicExperienceService` |
| Render decisions | `RuntimeRenderPolicyService` |

Keep component lifecycle async and cancellation-aware for long-running loads. UI authorization is for affordance and navigation clarity only; API authorization remains authoritative.

## Styling, Accessibility, Localization, And Analytics

Do not duplicate the specialized docs in this guide.

| Topic | Blazor-specific rule | Deep doc |
|---|---|---|
| Styling | Use shared wrappers and CSS isolation; avoid ad hoc MudBlazor overrides. | [DESIGN_SYSTEM.md](DESIGN_SYSTEM.md) |
| Accessibility | Preserve semantic labels, keyboard flow, and testable states in components. | [ACCESSIBILITY.md](ACCESSIBILITY.md) |
| Localization | Use `LanguageProvider`, `LanguagePicker`, `MudBlazorLocalizer`, and RTL integration points. | [LOCALIZATION.md](LOCALIZATION.md) |
| Analytics | `AnalyticsInitializer` owns browser bootstrap and consent-sensitive pageview tracking; server business metrics stay in handlers. | [OPERATIONS.md](OPERATIONS.md) |

## Common Pitfalls

1. Calling `EventApiClient` directly from a Razor component instead of through a UI service.
2. Gating edit/delete buttons with client roles instead of API-emitted HAL links.
3. Storing tokens or setup secrets in browser storage.
4. Adding custom correlation headers where .NET/OpenTelemetry `Activity` flow already handles trace propagation.
5. Updating DTOs without refreshing `Explore.API/swagger.json` and rebuilding the generated client.
6. Repeating render-policy, design-system, localization, or accessibility reference tables inside Blazor-specific docs.

## Related Docs

- [API.md](API.md)
- [API_COOKBOOK.md](API_COOKBOOK.md)
- [SECURITY.md](SECURITY.md)
- [RENDER_POLICIES.md](RENDER_POLICIES.md)
- [DESIGN_SYSTEM.md](DESIGN_SYSTEM.md)
- [ACCESSIBILITY.md](ACCESSIBILITY.md)
- [LOCALIZATION.md](LOCALIZATION.md)
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
