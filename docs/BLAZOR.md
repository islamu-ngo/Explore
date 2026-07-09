ABOUTME: Contributor guide for the Blazor BFF host and client application boundaries.
ABOUTME: Keeps token handling, proxying, render policy, service state, and client generation source-grounded.

# Blazor Frontend Architecture

> **Audience:** Contributors | Frontend | AI agents
> **Status:** Implemented
> **Owner:** Frontend
> **Last Verified:** 2026-07-05
> **Source Anchors:** `Explore.Blazor/Program.cs`, `Explore.Blazor/Extensions/`, `Explore.Blazor.Client/Explore.Blazor.Client.csproj`, `Explore.Blazor.Client/Services/`, `Explore.Blazor.Client/Layout/`, `Event.ControlPlane.Blazor/Program.cs`, `Event.ControlPlane.Blazor/Components/App.razor`, `Event.ControlPlane.Client/`, `docs/RENDER_POLICIES.md`, `docs/DESIGN_SYSTEM.md`

## Scope

This document is the contributor-facing guide for `Explore.Blazor`, `Explore.Blazor.Client`, `Event.ControlPlane.Blazor`, and `Event.ControlPlane.Client`. It focuses on the BFF trust boundary, proxy behavior, render-policy consumption, service/state patterns, and generated API client workflow.

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
| `Event.ControlPlane.Blazor` | Separate self-hostable control-plane BFF host: Keycloak OIDC confidential-client login, HttpOnly cookie session, shared BFF hosting, control-plane proxy, and protected root shell. | Public/tenant shell logic, browser token storage, setup-secret/API-key operator login, or WebAssembly/Auto render-mode hosting. |
| `Event.ControlPlane.Client` | Host-neutral control-plane Razor class library shared by embedded and separate shells. | Host render-mode selection, BFF security primitives, local authorization decisions, or direct public Blazor shell dependencies. |

The browser never owns access tokens. Interactive UI calls go through the BFF or generated client services; API remains the hard authorization boundary.

## Render Mode Boundary

`Explore.Blazor` may keep its render-policy-controlled public app behavior, including the existing configurable Interactive Server, WebAssembly, or Auto paths documented in [RENDER_POLICIES.md](RENDER_POLICIES.md).

`Event.ControlPlane.Blazor` is different: the separate control-plane host is Interactive Server-only. Its `Program.cs` registers `AddInteractiveServerComponents()` and maps `AddInteractiveServerRenderMode()`, while `Components/App.razor` applies `@rendermode="InteractiveServer"` to both `HeadOutlet` and `Routes`. Do not add InteractiveAuto or InteractiveWebAssembly hosting to the separate control-plane app.

`Event.ControlPlane.Client` stays render-mode neutral. Shared control-plane components must not declare `@rendermode` or depend on host-specific render-mode services; the embedding host chooses how those components are activated.

The shared control-plane RCL also owns its own small `ControlPlane*` UI primitives. It may use MudBlazor and the Event design tokens directly, but it must not reference `Explore.Blazor.Client` or its `App*` wrapper components. This keeps embedded and separate control-plane shells on the same component implementation without creating a public-client dependency cycle.

Hosts that render `Event.ControlPlane.Client` components must provide MudBlazor host services, providers, and static assets. The separate `Event.ControlPlane.Blazor` host registers `AddMudServices`, includes the MudBlazor CSS/JS static assets, and renders `MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, and `MudSnackbarProvider` in its operator layout.

## BFF Endpoint Families

BFF endpoints are split by concern in `Explore.Blazor/Extensions/` and wired through the server host.

| Endpoint family | Representative routes | Source |
|---|---|---|
| Authentication | `/auth/challenge`, `/auth/login`, `/auth/signout`, `/auth/status`, `/auth/providers`, `/auth/debug` | `BffAuthEndpoints.cs` |
| Auth refresh support | `/bff/auth/refresh-schemes`, `/bff/auth/refresh-session`, `/bff/auth/refresh-session/internal` | `BffAuthEndpoints.cs` |
| User/session view | `/bff/me` | `BffPreferenceEndpoints.cs` |
| Preferences and appearance | `/bff/theme`, `/bff/language`, `/bff/direction`, `/bff/ui-themes`, `/bff/appearance/*` | `BffPreferenceEndpoints.cs` |
| White-label manifest | `/manifest.webmanifest` | `BffManifestEndpoints.cs` |
| Setup secret | `GET/POST/DELETE /bff/setup-secret`, `/bff/setup-secret/sync` | `BffSetupSecretEndpoints.cs` |
| Storage upload proxy | `/bff/storage/upload-session`, `/bff/storage/upload-proxy` | `BffStorageEndpoints.cs` |
| Support access | `/bff/support-access/current`, `/bff/support-access/sessions`, `/bff/support-access/sessions/current/stop`, `/bff/support-access/tenants/{targetTenantId}/sessions`, `/bff/support-access/tenants/{targetTenantId}/sessions/{sessionId}/audit-events`, `/bff/support-access/sessions/{sessionId}/force-stop` | `BffSupportAccessEndpoints.cs` |

Keep new BFF endpoints in the smallest matching extension file. `BffEndpointExtensions.cs` should remain the facade/orchestrator, not a dumping ground for endpoint logic. The manifest endpoint resolves public-experience branding through the server-side API client and falls back to generic install metadata when branding cannot be read; do not reintroduce a static tenant-branded manifest file.

## Proxy And Token Forwarding

There are two related but separate transport paths:

1. **Browser-to-API proxy path** — `YarpProxyExtensions.cs` proxies `/api/*` calls and applies security-sensitive transforms:
   - forwards the server-side access token as `Authorization: Bearer ...`,
   - forwards trusted tenant context when route context is available,
   - strips any browser-supplied `X-Setup-Secret` value and forwards only the value returned by `ISetupSecretResolver`,
   - strips browser-supplied `X-Support-Access-*` values and forwards only the BFF-owned support-access session id when an active actor-bound session is stored server-side.
2. **Server-side typed client path** — `HttpClientExtensions.cs` registers outgoing clients with separate handlers:
   - `AccessTokenForwardingHandler`,
   - `TenantHeaderForwardingHandler`,
   - `SetupSecretForwardingHandler`,
   - `SupportAccessForwardingHandler`,
   - `BffCookieForwardingHandler` for self/BFF calls that must preserve cookie/XSRF context.

All server-side handlers use `UseCookies = false` where applicable to avoid pooled `CookieContainer` leakage between requests.

## Dedicated Admin Host Classification

Dedicated control-plane hostnames are static BFF configuration, not tenant database routing state. Configure exact hosts or origins under `Bff:AdminHosts`, for example `admin.example.org` or `https://admin.example.org`. Wildcards are invalid because admin hosts must not overlap tenant subdomains or tenant custom domains.

Host classification runs against `HttpContext.Request.Host` after `UseEventBffForwardedHeaders()` applies trusted forwarded headers. The tenant subdomain and custom-domain resolvers skip configured admin hosts, so `admin.example.org` is not resolved as tenant slug `admin` under `example.org` and is not looked up as a tenant-owned custom domain.

Optional admin-host IP allowlisting is configured with `Bff:AdminHostAllowedIpRanges` using exact IP addresses or CIDR ranges, for example `203.0.113.10` or `203.0.113.0/24`. When the allowlist is set, admin-host requests fail closed with `403` if the remote address is missing or outside the configured ranges. Public and tenant hosts are not affected by this allowlist.

Supported now: exact admin-host classification, optional IP/CIDR allowlisting, server-side BFF cookies, and the existing CSP/frame protections. Deferred: per-admin-host cookie domain/name switching, a separate mutation rate-limit policy, and per-admin-host CSP variants. MFA for instance administrators should be enforced by the identity provider policy rather than by browser-side checks.

## Setup Secret Boundary

Setup-secret handling is intentionally BFF-owned:

1. The browser sees only BFF-mediated cookie/session state, not the raw setup-secret persistence model.
2. Setup-secret forwarding uses `ISetupSecretResolver` with this trusted source order: BFF-owned setup handshake/session state, protected BFF-issued setup cookie, then explicit local/development/bootstrap configuration fallback. Inbound request headers are never a setup-secret source.
3. The setup cookie is protected with ASP.NET Core Data Protection, `HttpOnly`, short-lived, invalidated by the BFF setup-secret endpoints, and `Secure` outside local development.
4. `SameSite=Lax` is intentional because onboarding may cross top-level OIDC redirects before the first administrator completes setup.
5. Setup-secret validation is rate-limited at the BFF edge and again at the API edge.
6. The BFF limiter partitions requests by authenticated user when available, then antiforgery/session cookie state, then IP as the final fallback.

When debugging onboarding, check both BFF setup-secret endpoints and API setup-secret validation rather than adding client-side storage shortcuts.

## Support Access Boundary

Admin support access follows the same BFF-owned trust model as tokens, tenant hints, and setup secrets:

1. Browser code never owns support-access authority claims, tenant role grants, target-tenant tokens, or a durable impersonated identity.
2. `POST /bff/support-access/sessions` starts a session through the API and stores only an opaque active-session reference in `IBffSupportAccessSessionStore`, keyed to the authenticated user and OIDC `sid`.
3. `GET /bff/support-access/current` rechecks the API current-session endpoint and refreshes or clears the BFF store. This lets the shell recover after a refresh without making persisted active sessions ambient authority for ordinary API calls.
4. `POST /bff/support-access/sessions/current/stop` resolves the active owned session from the store or API current-session endpoint, stops it through the API, and clears the BFF store on success.
5. `GET /bff/support-access/tenants/{targetTenantId}/sessions` and `GET /bff/support-access/tenants/{targetTenantId}/sessions/{sessionId}/audit-events` expose bounded history and audit evidence through the same API authorization/HAL pipeline used by the operator console and tenant evidence view.
6. `POST /bff/support-access/sessions/{sessionId}/force-stop` is an antiforgery-protected emergency revocation path. On success it clears the current BFF store entry when the revoked session is the actor's active session.
7. `BffProxyHeaderSanitizer`, YARP transforms, and `SupportAccessForwardingHandler` remove all inbound `X-Support-Access-*` values before adding a trusted `X-Support-Access-Session-Id` from server-side state.
8. The API validates that forwarded session id against persisted support-access state, actor identity, resolved tenant, expiry, mode, and governance settings on each request that asks for support context.
9. Blazor UI affordances still come from BFF-confirmed state and API/HAL `_links`, not from local roles or serialized claims. The global shell banner is a visibility/safety control, not an authorization source.
10. Tenant admins review support-access evidence from tenant settings through `TenantSupportAccessEvidenceSection`. The view is read-only, resolves the current tenant through the tenant onboarding status endpoint, and renders audit drill-in only when a session resource contains the `audit-events` HAL link.

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
6. Positive protected examples include `/bff/auth/refresh-schemes`, `/bff/auth/refresh-session`, `/bff/support-access/sessions`, `/bff/support-access/sessions/current/stop`, `/bff/support-access/sessions/{sessionId}/force-stop`, `/bff/storage/upload-session`, `/bff/storage/upload-proxy`, and the preference/appearance mutation endpoints.
7. InteractiveServer storage-upload self-calls use a short-lived Data Protection protected `X-ISLAMU-BFF-SELF-CALL` token bound to the method, path, host, and authenticated user so they can pass the same endpoint filter without exposing browser tokens or setup secrets.
8. Documented exceptions are setup-secret bootstrap endpoints and `/bff/auth/refresh-session/internal`; these use separate credentials/authorization constraints because initial setup and server-side onboarding calls cannot reliably satisfy browser antiforgery semantics.

## Storage Upload Proxy Boundary

Browser-mediated storage uploads use BFF-owned upload sessions rather than caller-supplied destination URLs:

1. The browser asks `/bff/storage/upload-session` for an upload session. The BFF calls the provider-neutral API upload-session endpoint server-side and stores the approved session metadata in distributed cache.
2. The browser receives an opaque `uploadSessionId`, metadata-backed view URL, and expiry. It does not receive a raw provider object key, local path, or trusted upload destination.
3. The browser uploads bytes to `/bff/storage/upload-proxy` with `uploadSessionId`, `contentType`, and `file`. The proxy resolves the session, verifies the authenticated user, content type, and expected size, then streams bytes to the API upload-session content endpoint.
4. `/bff/storage/upload-proxy` rejects arbitrary HTTPS URLs, local filesystem paths, provider object keys, or presigned-looking values because client-provided destinations are not trusted.
5. Upload sessions are short-lived, user-bound, content-type-bound, and consumed after successful proxy upload. Both storage BFF endpoints remain protected by authorization and antiforgery validation.
6. Non-browser/server paths may still use direct provider upload URLs where the server code owns the trusted URL; browser paths must use the BFF upload-session flow.

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
2. The API build refreshes the checked-in build-time OpenAPI contract at `schemas/openapi.json`.
3. The Blazor client build regenerates `Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
4. Pages/components consume application services, not `EventApiClient` directly.

Generated DTOs preserve HAL `_links` through extension data. Per-resource UI affordances must be gated by HAL links from the API, not by duplicating role checks in Razor components. Admin authorization-provider setup/sync UI surfaces server-confirmed status, sync, and manual-package download affordances; the browser never owns Cerbos Admin API credentials or access tokens.

### Localization Admin Service Boundary

Localization administration in `Explore.Blazor.Client` goes through
`ILocalizationAdminService` and the BFF/API proxy path. The client can request
configuration, test provider connectivity, rotate/write-only TMS secrets through
server endpoints, export from TMS, and import/export static bundles. It never
calls Tolgee/Weblate directly and never receives plaintext TMS API keys.

Static bundle import/export service methods are client hooks over
`/api/admin/localization/bundle`; UI components should keep raw bundle JSON out
of logs/snackbars and rely on the service result message for safe feedback.

### Webhook Management UI

Webhook management uses generated API clients plus client service wrappers. Components must preserve API HAL resources and render create/update/delete/test/retry/rotate/open-portal actions only when the matching `_links` relation is present.

LocalProvider endpoint management is handled in ISLAMU Event. Svix advanced endpoint management is delegated to the backend-generated App Portal route, exposed in the UI only when the API emits the provider portal affordance. The browser never receives the Svix API token, endpoint signing secrets, secret refs, raw payload JSON, or full delivery response bodies.

### Registration Client Outcome Handling

Registration components consume `IEventRegistrationService` and shared workflow helpers rather than calling `EventApiClient` directly. The generated create-registration client contract still returns `BaseCommandResponseOfGuid`; no NSwag regeneration is required when only the server message semantics change and the response shape stays stable.

Blazor registration flows must classify that command response through `EventListRegistrationWorkflow.ResolveOutcome` so the modal, event list, and preview workspace share the same user-facing states:

1. `Confirmed` for normal successful create responses.
2. `Waitlisted` when the API reports that one or more selected sessions were waitlisted.
3. `AlreadyRegistered` when an idempotent repeat submit returns the existing registration.
4. `Failed` for safe service-layer failures.

`EventRegistrationService` converts generated-client `ApiException` values into bounded `FailureCode`, `Message`, and `Errors` values. Components should display those safe messages and log exceptions with structured context; they must not show raw exception text, provider response bodies, database details, bearer-token errors, or generated-client stack details. Event detail registration buttons remain HAL-gated: render the registration action only when the event detail resource contains the `register` link relation.

### Event Management And Moderation UI

Event detail and profile surfaces are HAL-driven management views:

1. `EventService.GetEventByIdAsync` tries public detail first. If the public route returns `404` for a hidden/moderated event, it falls back to authenticated `GetEventManagementDetailsAsync`; `401`, `403`, and `404` on the management route still fail closed.
2. Actor profile event lists merge public actor events with `GetManagedEventsByActorAsync` results when the current principal is authorized. Managed duplicates win so status and HAL data stay authoritative.
3. Event topbar actions must read link relations from the event DTO. Light moderation uses `moderate-light`, heavy redaction uses `moderate-heavy`, reversible restore uses `unmoderate`, and safe audit history uses `moderation-history`.
4. Event session reads default to the anonymous-safe public program route. Management views may pass `includeManagedSessions: true` to `EventService.GetSessionsByEventAsync` only from a server-provided HAL management context; the service then merges public sessions with the authorized management collection and falls back to public data on `401`, `403`, or `404`.
5. The Blazor service exposes explicit `ModerateEventLightAsync`, `ModerateEventHeavyAsync`, and `UnmoderateEventAsync` methods. Do not reintroduce a generic `ModerateEventAsync` compatibility alias; the API contract intentionally split light and heavy semantics.
6. Moderated event cards and sidebars may show a red/error "Moderated" state only after the API returned the authorized management DTO. Public visitors should not receive moderated event data at all.
7. Heavy redaction is destructive and must require explicit user confirmation in UI before calling the API. The API remains authoritative for redaction, deletion, notification, and authorization outcomes.

### AI Assistant Client Surface

The product assistant follows the same BFF and service-layer boundary:

1. Razor components use `IAiAssistantClientService`, not `IEventApiClient` directly.
2. `AiAssistantClientService` calls generated API client methods through the BFF/YARP path and returns safe command results or HAL resources.
3. `AiAssistantConversationState` owns selected conversation, reference search results, selected references, loading/error state, and HAL affordance helpers.
4. Confirm/Reject proposal buttons are rendered only when the proposed action has `confirm-action` / `reject-action` links.
5. Reference event affordances are rendered only when the reference HAL resource has an `event` link.
6. AI run progress uses polling through the run-status endpoint; no Blazor streaming transport is active for assistant runs.
7. Browser code must not display or log raw provider payloads, full prompt content, API keys, endpoint URLs, model secrets, or local authorization claims.

### Notification Preference Matrix

Notification preference UI consumes the generated API client through `INotificationService` and renders the reusable `Components/Notifications/NotificationPreferenceMatrix` component.

1. `/settings?section=notifications` renders the current-user matrix.
2. Organization and group profile pages render scoped notification-preference tabs using the same component with organization or group scope ids.
3. Save and global-mute controls render only when the HAL resource includes `save` and `set-mute` links. Components must not inspect roles or claims to decide whether preference cells are editable.
4. The matrix sends only generated DTOs through the service layer; Razor components do not call `EventApiClient` directly.
5. The component announces load, save, mute, and error states through the accessibility announcer service and uses scoped CSS with logical properties.

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
| AI assistant state | `AiAssistantState`, `AiAssistantConversationState`, `IAiAssistantClientService` |
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
5. Updating DTOs without refreshing `schemas/openapi.json` and rebuilding the generated client.
6. Repeating render-policy, design-system, localization, or accessibility reference tables inside Blazor-specific docs.
7. Enabling an AI assistant button without the corresponding HAL link from the API.

## Related Docs

- [API.md](API.md)
- [API_COOKBOOK.md](API_COOKBOOK.md)
- [SECURITY.md](SECURITY.md)
- [RENDER_POLICIES.md](RENDER_POLICIES.md)
- [DESIGN_SYSTEM.md](DESIGN_SYSTEM.md)
- [ACCESSIBILITY.md](ACCESSIBILITY.md)
- [LOCALIZATION.md](LOCALIZATION.md)
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
