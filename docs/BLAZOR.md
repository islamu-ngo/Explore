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

## BFF Runtime Hardening

1. Split BFF endpoint families live in dedicated files:
   - `Explore.Blazor/Extensions/BffAuthEndpoints.cs`
   - `Explore.Blazor/Extensions/BffPreferenceEndpoints.cs`
   - `Explore.Blazor/Extensions/BffSetupSecretEndpoints.cs`
   - `Explore.Blazor/Extensions/BffStorageEndpoints.cs`
   - `Explore.Blazor/Extensions/BffEndpointExtensions.cs` is only the facade/orchestrator.
2. Outbound server-side API clients use a three-handler chain in `Explore.Blazor/Extensions/HttpClientExtensions.cs`:
   - `AccessTokenForwardingHandler` for bearer token forwarding only
   - `TenantHeaderForwardingHandler` for tenant + forwarded-host headers
   - `SetupSecretForwardingHandler` for setup-secret propagation on onboarding endpoints only
3. Outbound server-side handlers set `UseCookies = false` to avoid pooled `CookieContainer` leakage between BFF HTTP clients.
4. Server-side resilience profiles are scoped by usage class:
   - interactive: short timeout, safe-method retries only
   - admin: medium timeout, safe-method retries only
   - background/upload: longer timeout, safe-method retries only
5. The BFF setup-secret surface has its own named rate-limit policy (`BffSetupSecret`) keyed by authenticated user when available, then antiforgery/session cookie, and IP only as the last fallback.
6. `traceparent` propagation relies on .NET/OpenTelemetry `Activity` flow for `HttpClient`; no custom correlation header should be introduced.

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

## Cookie Consent & Privacy State Machine

`AnalyticsInitializer` implements a 7-state consent state machine that governs analytics initialization based on the computed `AnalyticsConsentBootstrap` from the server:

States: `Uninitialized` → `NoBannerImmediateInit` | `BannerPendingCookieless` | `BannerPendingBlocked` → `Accepted` | `DeclinedCookieless` | `DeclinedDisabled`

State transitions:

1. **Uninitialized**: Initial state. Reads `AnalyticsConsentBootstrap` and tenant-scoped consent cookie.
2. **NoBannerImmediateInit**: No banner needed (cookieless provider or banner disabled). Analytics initializes immediately.
3. **BannerPendingCookieless**: Banner shown; analytics can run in cookieless mode before consent. PostHog initialized with `opt_out_capturing_by_default: true`.
4. **BannerPendingBlocked**: Banner shown; analytics blocked until consent. No PostHog initialization.
5. **Accepted**: User accepted. PostHog `posthog.opt_in_capturing()` called if SDK was pre-initialized; otherwise full init.
6. **DeclinedCookieless**: User declined but `decline_behavior=cookieless`. PostHog continues in cookieless mode.
7. **DeclinedDisabled**: User declined and `decline_behavior=disable`. Analytics fully disabled.

Key components:

- `CookieConsentBanner.razor`: Non-blocking fixed-bottom banner with equal Accept/Decline buttons. Uses MudBlazor.
- `CookieConsentStateService`: Cross-component event bridge. Footer "Cookie Settings" link triggers `RequestReopenAsync()`, which `AnalyticsInitializer` subscribes to.
- `ICookieConsentInterop`: JS module interop for tenant-scoped consent cookies (`cookie-consent.js`). Server-side no-op via `ServerCookieConsentInterop`.
- `analytics-bridge.js`: PostHog adapter accepts `posthogOptions` (cookielessMode, personProfiles, sessionReplay, autocapture, heatmaps, toolbar, optOutByDefault). Exposes `optInCapturing`, `optOutCapturing` for consent transitions.

Consent cookie design:

- Name: `explore_cc_{stableShortKey}` where `stableShortKey` is the first 8 hex characters of the tenant's immutable GUID (not the mutable subdomain slug).
- Values: `accepted` or `declined` only (minimal, no tracking data).
- Lifetime: configurable, default 180 days.

Privacy-first defaults:

- `opt_out_capturing_by_default: true` when consent is required.
- PostHog `person_profiles: 'identified_only'` (or `'never'` for anonymous-only).
- Session replay, autocapture, heatmaps, and toolbar all disabled by default.
- PostHog `defaults` version pin recommended for SDK stability.

## SSR / Prerender Stance

All consent-sensitive decisions happen **post-hydration** in the browser. During server-side rendering (SSR) and prerendering:

1. `AnalyticsInitializer` renders no markup server-side — it is a client-only component that initializes in `OnAfterRenderAsync`.
2. No consent cookies are read or written during SSR. `ServerCookieConsentInterop` is a no-op that returns `null`.
3. No analytics pageview events are emitted during SSR. The first pageview fires after the client-side state machine reaches a terminal or immediate-init state.
4. The cookie consent banner is never visible during the initial server-rendered HTML. It appears only after the client-side bootstrap determines banner visibility from `AnalyticsConsentBootstrap`.

This ensures zero consent-sensitive data is processed before the user's browser has had the opportunity to display and collect consent.

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
4. For the current BFF/onboarding model, setup-secret persistence remains server-controlled and the browser only receives the secure cookie/session view of that state.

## Setup Secret Cookie And Onboarding Notes
1. The setup-secret flow is a BFF-owned trust boundary, not a client storage workflow.
2. Setup-secret persistence currently uses an `HttpOnly` cookie plus server-side `SetupSecretSessionService` fallback for authenticated bootstrap flows.
3. `SameSite=Lax` is intentional for the setup-secret cookie because onboarding may bridge through top-level auth/login redirects before the first administrator completes setup; `Strict` would be riskier for that flow.
4. Setup-secret validation requests are rate limited at the BFF edge and again at the API edge; this is deliberate defense in depth.
5. The antiforgery cookie (`XSRF-TOKEN`) is also used as the anonymous-session partition key for the BFF setup-secret limiter when no authenticated user identity exists.

## Token Lifecycle Constraints
1. `CircuitAccessTokenService` captures access tokens from the authenticated BFF request/circuit and can also resolve persisted tokens for the current user.
2. `AccessTokenForwardingHandler` must remain token-only; tenant and setup-secret forwarding belong in separate handlers.
3. Any future token lifecycle work must explicitly consider render-mode transitions and circuit reconnection behavior before changing handler responsibilities.

## Styling Architecture

### Global CSS (@layer)
Global styles use `@layer` cascade ordering via `Explore.Blazor/wwwroot/css/layers.css`:
- **Layers** (cascade order): `reset → base → tokens → mudblazor-overrides → components → utilities`
- **Token system**: 3-tier (Primitives → Semantic → Component) in `tokens.css`
- **Colors**: Use `oklch()` and `color-mix(in oklch, ...)` for perceptual uniformity
- **Typography**: H1-H5 use `clamp()` for fluid responsive sizing

### Component CSS Isolation
1. Prefer CSS isolation (`.razor.css`) per component.
2. Use clear BEM-style class names to keep scoped CSS readable.
3. Use native CSS nesting (`&`) for pseudo-classes, modifiers, and nested media/container queries. Max 3 levels deep.
4. Use `::deep` only when integration with third-party component internals requires it.

### Wrapper Components
MudBlazor wrapper components in `Explore.Blazor.Client/Components/Common/` provide consistent defaults:
- `AppButton` (Filled/Primary/Elevation=0), `AppCard` (Elevation=0/border), `AppTextField<T>` (Outlined), `AppIconButton`, `AppDialogShell`
- `DialogOptionsFactory` in `Services/` — static presets: `Small()`, `Medium()`, `Confirmation()`, `Editor()`

### MudBlazor Override Policy
- Global `.mud-*` overrides tracked in `css/mudblazor-overrides.css` with whitelist header
- Each override requires a `JUSTIFICATION` comment
- Approved exceptions: drawer/portal overrides (render outside Blazor scope), overlay z-index

### AppearanceStyleBuilder
Generates inline CSS for actor/event appearance customization (`Explore.Blazor.Client/Helpers/`):

| Method | Purpose |
|---|---|
| `BuildStyle(settings, fallbackHex, additionalCss?)` | General background with optional overlay effect |
| `BuildHeroStyle(settings, fallbackHex)` | Hero sections with `aspect-ratio: 16/9` |
| `BuildBannerStyle(settings, fallbackHex)` | Banner-specific styling |

**AppearanceSettings model**: `BackgroundColor`, `ImageUri`, `BackgroundEffect` (None, SoftOverlay 0.24, StrongOverlay 0.40, Blur 0.18), `IsEmpty`.

### AppearanceEditor Component
Two-way bindable editor (`Explore.Blazor.Client/Shared/`): `BackgroundColor`, `BackgroundEffect`, `ImageUri` with `ShowImageField`, `ShowPreview`, `FallbackColor`, `PreviewAdditionalCss` parameters.

### DialogOptionsFactory
Static presets in `Explore.Blazor.Client/Services/`:
- `Small()` — MaxWidth.Small, FullWidth, CloseOnEscape
- `Medium()` — MaxWidth.Medium, FullWidth, CloseOnEscape
- `Confirmation()` — Small + DialogPosition.Center
- `Editor()` — Medium + CloseButton + BackdropClick

## Localization

The localization stack bridges server-side TMS providers with the Blazor client:

- **`LanguageProvider.razor`** — cascading provider that reads language from `PersistentComponentState` (server→WASM hand-off) then cookie; cascades `LanguageContext`
- **`ITranslationService`** / `TranslationService` — client-side fetch + cache + `T(key)` lookup
- **`LanguagePicker.razor`** — language selector with kill-switch (`ClientPickerEnabled`), a11y (`aria-label`, keyboard nav)
- **`MudBlazorLocalizer`** — bridges MudBlazor's `MudLocalizer` to `ITranslationService` with `mudblazor.{key}` prefix
- **`CultureRegistry`** — compile-time allowlist in `Explore.Domain/Common/Localization/` (NOT Application — see decision D16)
- **`MudRTLProvider`** — cascaded via `App.razor` using `RtlLanguages.IsRtl(code)`
- **Admin UI** — `InstanceLocalizationSection.razor` for TMS config, kill-switches, enabled languages, bundle export, secret lifecycle

CultureInfo is set on WASM startup via cookie → `CultureRegistry` validation → `CultureInfo.DefaultThreadCurrentCulture`.

Full architecture: `docs/LOCALIZATION.md`

## Common Pitfalls
1. Updating UI before regenerating NSwag client after API contract changes.
2. Treating client-side route guards as security enforcement.
3. Reintroducing direct token handling in WASM components.
4. Forgetting setup-secret forwarding rules when debugging onboarding.
5. Adding metrics to the `T(key)` hot path — it must stay allocation-free (see D8).

## Related Docs
- `docs/API.md`
- `docs/CONTRIBUTING.md`
- `docs/RENDER_POLICIES.md`
- `docs/TROUBLESHOOTING.md`
- `docs/DESIGN_SYSTEM.md` — CSS layers, tokens, wrapper components, typography
- `docs/ACCESSIBILITY.md` — WCAG AA compliance, service contracts, testing
- `docs/LOCALIZATION.md` — TMS providers, offline bundles, governance model, cache variation
