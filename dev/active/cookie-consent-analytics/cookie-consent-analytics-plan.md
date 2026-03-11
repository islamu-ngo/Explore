# Cookie Consent & Analytics Governance — Implementation Plan

Last Updated: 2026-03-10 (Rev 3 — Post Architect Review)

---

## Executive Summary

The ISLAMU Event platform has a mature analytics abstraction (PostHog, Plausible, Rybbit, RudderStack, Null) with consent modes governing **server-side identity hashing**. It completely lacks **browser-side cookie consent**.

This plan adds storage-mode-driven cookie consent with privacy-first defaults, PostHog native consent integration, tenant-scoped consent cookies, and a dedicated runtime profile resolver.

### Rev 3 Architectural Amendments (from Senior Architect Review)

| # | Amendment | Impact |
|---|-----------|--------|
| 1 | **Typed enums** replace stringly-typed policy contracts | Domain + Application + JS edge |
| 2 | **Dedicated resolver service** (`IAnalyticsRuntimeProfileResolver`) extracts consent computation from query handler | Application + Admin UI + Tests |
| 3 | **Slim public DTO** — effective runtime config only, no admin governance inputs | Application + Browser |
| 4 | **Explicit state machine** for consent flow in AnalyticsInitializer | Browser/JS |
| 5 | **Advisory auto-computation** — suggest, don't silently overwrite | Admin UI |
| 6 | **Global emergency kill switch** (`analytics.global_disable_client_tracking`) | Domain + Application + Browser |
| 7 | **Defer RudderStack parity** — document extension point, don't implement cookieless parity | Application + Docs |

### Design Principles (from Architect Review)

1. **Consent ≠ analytics enablement.** Two layers: operator configuration + end-user device choice. An operator setting alone must never force client-side tracking before consent when the storage mode requires it.
2. **Public API keys may go to the browser** if the provider expects them there. **Private/admin/personal API keys must NEVER go to the browser.**
3. **Consent cookies are preference artifacts, not identity artifacts.** Values: `accepted` | `declined` only. No timestamps, user IDs, or other tracking data.
4. **Provider capability is a first-class concept**, not scattered implicit knowledge.

---

## Current State Analysis

### What Exists
- **5 analytics providers** with runtime resolution via `AnalyticsConfigResolver` (tenant-cascading)
- **7 governance keys** under `analytics.*` for provider, enabled, consent_mode, transport_mode, api_key, endpoint_url, personal_api_key
- **`AnalyticsInitializer.razor`** in `MainLayout.razor` — bootstraps analytics on first render, tracks pageviews on navigation
- **`analytics-bridge.js`** — PostHog `posthog.init(apiKey, { api_host })` with NO feature controls
- **`PublicExperienceSettingsDto`** — carries analytics config to browser
- **`AnalyticsGovernanceService`** — sanitizes payloads, enforces consent-mode identity hashing
- **Relay transport** (`/api/a/t`) for cookieless server-side forwarding

### What's Missing
- ❌ Cookie consent banner, consent state, consent cookie
- ❌ PostHog feature controls (`posthog.init()` passes only `{ api_host }`)
- ❌ PostHog cookieless mode / person_profiles in init config
- ❌ Admin analytics settings UI
- ❌ Consent-gated initialization / state machine
- ❌ Storage-mode-aware consent computation
- ❌ Override with warnings
- ❌ Tenant-scoped consent
- ❌ Persistent consent withdrawal ("Cookie Settings" link)
- ❌ Global emergency kill switch
- ❌ Provider capability matrix
- ❌ Typed enums for policy contracts

### Provider Storage Behavior

| Provider | Config Mode | Writes to Browser? | Banner Needed? | Decline Behavior |
|---|---|---|---|---|
| **PostHog** | `cookieless_mode: 'always'` | NO | NO | N/A |
| **PostHog** | `cookieless_mode: 'on_reject'` | Only after consent | YES | Cookieless analytics |
| **PostHog** | No cookieless mode (legacy) | YES immediately | YES | Disable |
| **RudderStack** | Any (v1 scope) | YES | YES | Disable |
| **Plausible** | Always cookieless | NO | NO | N/A |
| **Rybbit** | Always cookieless | NO | NO | N/A |
| **None** | N/A | NO | NO | N/A |

> **RudderStack note (Amendment 7):** RudderStack JS v3 supports cookieless tracking, but we defer cookieless parity to a future iteration. For v1, RudderStack is treated as "full consent required". The resolver has an explicit extension point for future RudderStack cookieless support.

---

## Proposed Future State

### Domain Types (Amendment 1 — Typed Enums)

```csharp
// Explore.Domain/Enums/Analytics/
public enum DeclineBehavior { Disable = 0, Cookieless = 1 }
public enum PosthogCookielessMode { Off = 0, Always = 1, OnReject = 2 }
public enum PosthogPersonProfiles { Always = 0, IdentifiedOnly = 1, Never = 2 }

// Computed by resolver, not stored — represents effective runtime behavior
public enum AnalyticsStorageProfile { Cookieless = 0, ConsentManaged = 1, FullConsent = 2 }
```

### Provider Capability Matrix (Amendment 5)

```csharp
// Explore.Domain/Analytics/AnalyticsProviderCapabilities.cs
public sealed record AnalyticsProviderCapabilities
{
    public bool SupportsCookielessMode { get; init; }
    public bool SupportsNativeConsentTransition { get; init; }
    public bool SupportsPersonProfiles { get; init; }
    public bool RequiresClientApiKey { get; init; }
    public bool InherentlyCookieless { get; init; }

    public static AnalyticsProviderCapabilities For(AnalyticsProviderEnum provider) => provider switch
    {
        AnalyticsProviderEnum.Posthog => new()
        {
            SupportsCookielessMode = true,
            SupportsNativeConsentTransition = true,
            SupportsPersonProfiles = true,
            RequiresClientApiKey = true,
            InherentlyCookieless = false
        },
        AnalyticsProviderEnum.Plausible => new()
        {
            InherentlyCookieless = true,
            RequiresClientApiKey = false
        },
        AnalyticsProviderEnum.Rybbit => new()
        {
            InherentlyCookieless = true,
            RequiresClientApiKey = false
        },
        AnalyticsProviderEnum.RudderStack => new()
        {
            SupportsCookielessMode = false, // deferred to future iteration
            RequiresClientApiKey = true,
            InherentlyCookieless = false
        },
        _ => new() { InherentlyCookieless = true }
    };
}
```

### Runtime Profile Resolver (Amendment 2)

```csharp
// Explore.Application/Contracts/Services/IAnalyticsRuntimeProfileResolver.cs
public interface IAnalyticsRuntimeProfileResolver
{
    AnalyticsRuntimeProfile Resolve(AnalyticsSettingGroup settings);
}

// Explore.Application/Analytics/AnalyticsRuntimeProfile.cs
public sealed record AnalyticsRuntimeProfile
{
    public AnalyticsStorageProfile StorageProfile { get; init; }
    public bool CookieBannerEnabled { get; init; }
    public bool CanRunBeforeConsent { get; init; }
    public DeclineBehavior DeclineBehavior { get; init; }
    public string ConsentCookieKey { get; init; }     // computed: "explore_cc_{tenantSlug}"
    public int ConsentCookieLifetimeDays { get; init; }
    public PosthogClientOptions? Posthog { get; init; } // null when provider != PostHog
}

// Explore.Application/Analytics/PosthogClientOptions.cs
public sealed record PosthogClientOptions
{
    public PosthogCookielessMode CookielessMode { get; init; }
    public PosthogPersonProfiles PersonProfiles { get; init; }
    public bool SessionReplay { get; init; }
    public bool Autocapture { get; init; }
    public bool Heatmaps { get; init; }
    public bool Toolbar { get; init; }
}
```

### Resolver Logic (the core policy engine)

```
Resolve(settings):
  // Amendment 6: global kill switch checked first
  if settings.GlobalDisableClientTracking:
      return profile with CookieBannerEnabled=false, CanRunBeforeConsent=false (no analytics at all)

  if !settings.AnalyticsEnabled:
      return profile with CookieBannerEnabled=false (no analytics)

  capabilities = AnalyticsProviderCapabilities.For(settings.Provider)

  if capabilities.InherentlyCookieless:
      storageProfile = Cookieless
      bannerEnabled = false
      canRunBefore = true
      declineBehavior = DeclineBehavior.Disable  // N/A, no banner
  elif settings.Provider is PostHog:
      switch settings.PosthogCookielessMode:
          Always:
              storageProfile = Cookieless
              bannerEnabled = false
              canRunBefore = true
          OnReject:
              storageProfile = ConsentManaged
              bannerEnabled = settings.CookieConsentEnabled  // admin can override
              canRunBefore = true
              declineBehavior = settings.DeclineBehavior  // default: Cookieless
          Off:
              storageProfile = FullConsent
              bannerEnabled = settings.CookieConsentEnabled
              canRunBefore = false
              declineBehavior = DeclineBehavior.Disable
  elif settings.Provider is RudderStack:
      // Amendment 7: defer cookieless parity
      storageProfile = FullConsent
      bannerEnabled = settings.CookieConsentEnabled
      canRunBefore = false
      declineBehavior = DeclineBehavior.Disable

  consentCookieKey = $"explore_cc_{settings.TenantSlug ?? "default"}"
  consentCookieLifetimeDays = settings.ConsentCookieLifetimeDays

  posthog = null
  if settings.Provider is PostHog:
      posthog = new PosthogClientOptions(...)

  return new AnalyticsRuntimeProfile(...)
```

### Slim Public DTO (Amendment 3)

```csharp
// Only effective runtime config — no admin governance inputs
public sealed class AnalyticsConsentBootstrap
{
    // Consent UX
    public bool CookieBannerEnabled { get; set; }
    public bool CanRunBeforeConsent { get; set; }
    public string DeclineBehavior { get; set; }        // JS string: "none"|"cookieless"|"disable"
    public string ConsentCookieKey { get; set; }       // "explore_cc_{slug}" — computed server-side
    public int ConsentCookieLifetimeDays { get; set; }

    // Provider runtime config (public keys only)
    public string AnalyticsProvider { get; set; }      // "posthog"|"plausible"|"rybbit"|"rudderstack"|"none"
    public PosthogClientBootstrap? Posthog { get; set; } // null when not PostHog
}

public sealed class PosthogClientBootstrap
{
    public string CookielessMode { get; set; }         // JS string: "off"|"always"|"on_reject"
    public string PersonProfiles { get; set; }         // JS string: "always"|"identified_only"|"never"
    public bool SessionReplay { get; set; }
    public bool Autocapture { get; set; }
    public bool Heatmaps { get; set; }
    public bool Toolbar { get; set; }
}
```

> **Note:** `tenantSlug` is NOT exposed publicly. The server computes `consentCookieKey` and sends only the result. `PersonalApiKey` NEVER crosses to public bootstrap.

### Consent State Machine (Amendment 4)

```csharp
// Explore.Blazor.Client/Analytics/ConsentState.cs
public enum ConsentState
{
    Uninitialized,              // Initial state, no settings fetched yet
    NoBannerImmediateInit,      // No banner needed, init analytics immediately
    BannerPendingCookieless,    // Banner shown, analytics running in cookieless mode
    BannerPendingBlocked,       // Banner shown, analytics blocked until consent
    Accepted,                   // User accepted, full analytics active
    DeclinedCookieless,         // User declined, cookieless analytics running
    DeclinedDisabled            // User declined, no analytics
}
```

**State transitions:**

```
Uninitialized
  ├─ settings.CookieBannerEnabled = false
  │   └→ NoBannerImmediateInit → init analytics → TERMINAL
  │
  ├─ settings.CookieBannerEnabled = true, settings.CanRunBeforeConsent = true
  │   ├─ consent cookie = "accepted" → Accepted (init full) → TERMINAL
  │   ├─ consent cookie = "declined" → DeclinedCookieless → TERMINAL
  │   └─ no cookie → BannerPendingCookieless (init cookieless, show banner)
  │       ├─ user accepts → Accepted (opt_in_capturing)
  │       └─ user declines → DeclinedCookieless (opt_out_capturing)
  │
  └─ settings.CookieBannerEnabled = true, settings.CanRunBeforeConsent = false
      ├─ consent cookie = "accepted" → Accepted (init analytics) → TERMINAL
      ├─ consent cookie = "declined" → DeclinedDisabled → TERMINAL
      └─ no cookie → BannerPendingBlocked (show banner, no init)
          ├─ user accepts → Accepted (init analytics)
          └─ user declines → DeclinedDisabled

"Cookie Settings" link:
  Accepted → BannerPendingCookieless or BannerPendingBlocked (re-show banner)
  DeclinedCookieless → BannerPendingCookieless (re-show banner)
  DeclinedDisabled → BannerPendingBlocked (re-show banner)
```

### Consent Flow Architecture Diagram

```
┌─────────────────────────────────────────────────────┐
│                  Server Side                         │
│                                                      │
│  AnalyticsSettingGroup ──→ IAnalyticsRuntimeProfile  │
│        (raw settings)       Resolver                 │
│                              │                       │
│                              ▼                       │
│                    AnalyticsRuntimeProfile            │
│                              │                       │
│                              ▼                       │
│          GetPublicExperienceSettingsQueryHandler      │
│                    (maps to slim DTO)                 │
│                              │                       │
│                              ▼                       │
│              AnalyticsConsentBootstrap                │
│              (public, runtime-only)                   │
└──────────────────────┬──────────────────────────────┘
                       │ API response
                       ▼
┌─────────────────────────────────────────────────────┐
│                  Browser Side                        │
│                                                      │
│  AnalyticsInitializer ──→ ConsentState machine       │
│        │                     │                       │
│        ▼                     ▼                       │
│  ICookieConsentInterop   CookieConsentBanner         │
│  (tenant-scoped cookie)  (Accept / Decline)          │
│        │                     │                       │
│        ▼                     ▼                       │
│  IAnalyticsInterop ──→ analytics-bridge.js           │
│  (PostHog consent      (posthog.init + opt_in/out)   │
│   methods)                                           │
└─────────────────────────────────────────────────────┘
```

---

## Implementation Phases

### Phase 1: Domain Layer (Effort: S)

#### Task 1.1: Add Analytics Enums
- **Files (new):**
  - `Explore.Domain/Enums/Analytics/DeclineBehavior.cs`
  - `Explore.Domain/Enums/Analytics/PosthogCookielessMode.cs`
  - `Explore.Domain/Enums/Analytics/PosthogPersonProfiles.cs`
  - `Explore.Domain/Enums/Analytics/AnalyticsStorageProfile.cs`
- **Acceptance Criteria:**
  - [ ] All 4 enums created with correct values
  - [ ] File-scoped namespaces
  - [ ] `AnalyticsStorageProfile` is computed (not stored), placed in Domain for shared reference
- **Effort:** S
- **Skill:** `clean-architecture-rules`

#### Task 1.2: Add Provider Capability Matrix
- **File (new):** `Explore.Domain/Analytics/AnalyticsProviderCapabilities.cs`
- **Acceptance Criteria:**
  - [ ] Record with capability flags per provider
  - [ ] Static factory `For(AnalyticsProviderEnum)` with correct capabilities
  - [ ] PostHog: all capabilities true, inherently cookieless false
  - [ ] Plausible/Rybbit: inherently cookieless true
  - [ ] RudderStack: `SupportsCookielessMode = false` (deferred, Amendment 7)
  - [ ] None: inherently cookieless true
- **Effort:** S

#### Task 1.3: Add Governance Keys
- **File:** `Explore.Domain/Constants/GovernanceSettingKeys.cs`
- **Changes:** Add to `Analytics` nested class:
  - `analytics.cookie_consent_enabled` — master switch for cookie consent banner
  - `analytics.decline_behavior` — enum string: `disable` or `cookieless`
  - `analytics.consent_cookie_lifetime_days` — int, consent cookie TTL
  - `analytics.posthog_cookieless_mode` — enum string: `off`, `always`, `on_reject`
  - `analytics.posthog_person_profiles` — enum string: `always`, `identified_only`, `never`
  - `analytics.posthog_session_replay` — bool
  - `analytics.posthog_autocapture` — bool
  - `analytics.posthog_heatmaps` — bool
  - `analytics.posthog_toolbar` — bool
  - `analytics.global_disable_client_tracking` — bool (Amendment 6, System scope)
- **Acceptance Criteria:**
  - [ ] 10 key constants added
  - [ ] Follows existing naming pattern
- **Effort:** S
- **Skill:** `clean-architecture-rules`

#### Task 1.4: Add Setting Definitions
- **File:** `Explore.Domain/Settings/Definitions/AnalyticsSettingDefinitions.cs`
- **Changes:** Add `SettingDefinition` entries for 10 new keys:
  - `cookie_consent_enabled`: boolean, default `false`, scope Tenant
  - `decline_behavior`: string, default `cookieless`, scope Tenant
  - `consent_cookie_lifetime_days`: integer, default `180`, scope Tenant
  - `posthog_cookieless_mode`: string, default `on_reject`, scope Tenant
  - `posthog_person_profiles`: string, default `identified_only`, scope Tenant
  - `posthog_session_replay`: boolean, default `false`, scope Tenant
  - `posthog_autocapture`: boolean, default `false`, scope Tenant
  - `posthog_heatmaps`: boolean, default `false`, scope Tenant
  - `posthog_toolbar`: boolean, default `false`, scope Tenant
  - `global_disable_client_tracking`: boolean, default `false`, scope **System** (not Tenant)
- **Acceptance Criteria:**
  - [ ] All 10 definitions follow existing pattern
  - [ ] Privacy-first defaults throughout
  - [ ] `global_disable_client_tracking` scoped to System level (Amendment 6)
  - [ ] All others scoped to Tenant
- **Effort:** S
- **Skill:** `clean-architecture-rules`

---

### Phase 2: Application Layer (Effort: M)

#### Task 2.1: Extend AnalyticsSettingGroup
- **File:** `Explore.Application/Settings/Groups/AnalyticsSettingGroup.cs`
- **Changes:** Add 10 new properties with typed enum parsing:
  - `CookieConsentEnabled` (bool)
  - `DeclineBehavior` (DeclineBehavior enum)
  - `ConsentCookieLifetimeDays` (int)
  - `PosthogCookielessMode` (PosthogCookielessMode enum)
  - `PosthogPersonProfiles` (PosthogPersonProfiles enum)
  - `PosthogSessionReplay` (bool)
  - `PosthogAutocapture` (bool)
  - `PosthogHeatmaps` (bool)
  - `PosthogToolbar` (bool)
  - `GlobalDisableClientTracking` (bool)
- **Acceptance Criteria:**
  - [ ] String setting values parsed to typed enums
  - [ ] Invalid enum strings fall back to defaults
  - [ ] Batch resolution includes new keys
- **Effort:** S

#### Task 2.2: Create AnalyticsRuntimeProfileResolver (Amendment 2)
- **Files (new):**
  - `Explore.Application/Contracts/Services/IAnalyticsRuntimeProfileResolver.cs`
  - `Explore.Application/Analytics/AnalyticsRuntimeProfile.cs`
  - `Explore.Application/Analytics/PosthogClientOptions.cs`
  - `Explore.Application/Analytics/AnalyticsRuntimeProfileResolver.cs`
- **Logic:** See "Resolver Logic" section above
- **Acceptance Criteria:**
  - [ ] Global kill switch checked first (Amendment 6)
  - [ ] Inherently cookieless providers → no banner
  - [ ] PostHog `always` → no banner, runs before consent
  - [ ] PostHog `on_reject` → banner (if enabled), runs before consent, decline = configurable
  - [ ] PostHog `off` → banner (if enabled), blocks until consent, decline = Disable
  - [ ] RudderStack → full consent required (Amendment 7)
  - [ ] `ConsentCookieKey` computed from tenant slug (never exposes raw slug)
  - [ ] `PosthogClientOptions` only populated for PostHog
  - [ ] Uses `AnalyticsProviderCapabilities` for provider checks
  - [ ] Registered in DI
- **Effort:** M
- **Skill:** `clean-architecture-rules`

#### Task 2.3: Create Slim Public DTO (Amendment 3)
- **File (new or extend):** `Explore.Application/DTOs/Onboarding/AnalyticsConsentBootstrap.cs`
- **Changes:** Create the slim public-facing model:
  - `AnalyticsConsentBootstrap` — consent UX + provider runtime config
  - `PosthogClientBootstrap` — PostHog-specific client options (only if PostHog)
  - Enum → JS string mapping happens here (enum-to-string at the serialization edge)
- **Acceptance Criteria:**
  - [ ] No admin governance inputs exposed
  - [ ] No `tenantSlug` — only `ConsentCookieKey`
  - [ ] No `PersonalApiKey` — ever
  - [ ] PosthogClientBootstrap null when provider ≠ PostHog
  - [ ] All enum values mapped to JS-friendly strings
- **Effort:** S

#### Task 2.4: Update GetPublicExperienceSettingsQueryHandler
- **File:** `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs`
- **Changes:**
  - Inject `IAnalyticsRuntimeProfileResolver`
  - Call resolver to get `AnalyticsRuntimeProfile`
  - Map profile to `AnalyticsConsentBootstrap` (the slim DTO)
  - Attach to `PublicExperienceSettingsDto` (as a nested object or flattened)
- **Acceptance Criteria:**
  - [ ] Handler delegates all policy logic to resolver (no inline computation)
  - [ ] Only maps resolver output → DTO
  - [ ] Existing non-analytics DTO fields unchanged
- **Effort:** S
- **Skill:** `cqrs-mediatr-guidelines`

#### Task 2.5: Register Resolver in DI
- **File:** Application layer DI registration
- **Changes:** Register `IAnalyticsRuntimeProfileResolver` → `AnalyticsRuntimeProfileResolver`
- **Effort:** S

---

### Phase 3: Browser/JS Layer (Effort: L)

#### Task 3.1: Add Consent State Machine
- **File (new):** `Explore.Blazor.Client/Analytics/ConsentState.cs`
- **Acceptance Criteria:**
  - [ ] `ConsentState` enum with 7 states (see Amendment 4)
  - [ ] File-scoped namespace
- **Effort:** S

#### Task 3.2: Add Cookie Consent Banner Component
- **File (new):** `Explore.Blazor.Client/Shared/CookieConsentBanner.razor` + `.razor.css`
- **Design:**
  - Fixed position at bottom of viewport (non-blocking)
  - Equal "Accept" and "Decline" buttons (GDPR, no dark patterns)
  - Brief text with privacy/cookie policy link
  - MudBlazor, BEM CSS, accessible (ARIA, keyboard nav)
  - Slides in/out with CSS animation
- **Acceptance Criteria:**
  - [ ] Accept and Decline equally prominent
  - [ ] Banner dismissed after choice
  - [ ] Does not appear if consent cookie already exists
  - [ ] Accessible
- **Effort:** M
- **Skill:** `blazor-ui-conventions`, `blazor-css-isolation`

#### Task 3.3: Add Cookie Consent JS Interop (Tenant-Scoped)
- **Files (new):**
  - `Explore.Blazor.Client/Contracts/Interop/ICookieConsentInterop.cs`
  - `Explore.Blazor.Client/Services/CookieConsentInterop.cs`
  - `Explore.Blazor.Client/wwwroot/js/cookie-consent.js`
- **Methods:**
  - `GetConsentStatusAsync(string consentCookieKey)` → `string?`
  - `SetConsentAsync(string consentCookieKey, bool accepted, int lifetimeDays)` → sets cookie
- **Acceptance Criteria:**
  - [ ] Cookie name: `consentCookieKey` (server-computed, e.g., `explore_cc_default`)
  - [ ] Values: `accepted` | `declined` only (no timestamps, no IDs)
  - [ ] Configurable TTL (default 180 days)
  - [ ] Path `/`, SameSite=Lax, Secure in production
  - [ ] Server-side no-op for prerender
- **Effort:** S

#### Task 3.4: Update analytics-bridge.js — PostHog Init Options
- **File:** `Explore.Blazor.Client/wwwroot/js/analytics-bridge.js`
- **Changes:**
  ```javascript
  window.posthog.init(apiKey, {
    api_host: host,
    cookieless_mode: options.cookielessMode ?? 'on_reject',
    person_profiles: options.personProfiles ?? 'identified_only',
    autocapture: options.autocapture ?? false,
    capture_pageview: false,
    capture_pageleave: false,
    disable_session_recording: !(options.sessionReplay ?? false),
    enable_heatmaps: options.heatmaps ?? false,
    advanced_disable_toolbar_metrics: !(options.toolbar ?? false),
    defaults: '2026-01-30'
  });
  ```
  - Add PostHog consent methods:
    - `optInCapturing()` → `posthog.opt_in_capturing()`
    - `optOutCapturing()` → `posthog.opt_out_capturing()`
    - `getExplicitConsentStatus()` → `posthog.get_explicit_consent_status()`
- **Acceptance Criteria:**
  - [ ] All PostHog config options passed with privacy-first defaults
  - [ ] `defaults: '2026-01-30'` set for version pinning
  - [ ] Consent methods exposed for Blazor interop
  - [ ] Non-PostHog providers unaffected
- **Effort:** M

#### Task 3.5: Update IAnalyticsInterop + Implementations
- **Files:**
  - `Explore.Blazor.Client/Contracts/Interop/IAnalyticsInterop.cs`
  - `Explore.Blazor.Client/Services/AnalyticsInterop.cs`
  - `Explore.Blazor/Services/ServerAnalyticsInterop.cs`
- **Changes:**
  - Extend `InitAsync` to accept `PosthogClientBootstrap?` options
  - Add `OptInCapturingAsync()`, `OptOutCapturingAsync()`, `GetExplicitConsentStatusAsync()`
  - Enum → JS string mapping at the interop edge (Amendment 1)
- **Acceptance Criteria:**
  - [ ] PostHog options forwarded to JS bridge
  - [ ] Consent methods available
  - [ ] Non-PostHog providers: consent methods no-op
  - [ ] Server no-op implementation updated
  - [ ] No PostHog-specific options sent for other providers
- **Effort:** M

#### Task 3.6: Update AnalyticsInitializer as State Machine (Amendment 4)
- **File:** `Explore.Blazor.Client/Shared/AnalyticsInitializer.razor`
- **Changes:**
  - Replace imperative if/else with `ConsentState` machine
  - State drives rendering (banner visibility) and analytics lifecycle
  - Transitions:
    - `Uninitialized` → fetch settings → determine initial state
    - `NoBannerImmediateInit` → init analytics → terminal
    - `BannerPendingCookieless` → init PostHog in cookieless mode, show banner
    - `BannerPendingBlocked` → show banner, block init
    - Accept → `Accepted` (opt_in or init)
    - Decline → `DeclinedCookieless` (opt_out) or `DeclinedDisabled` (no-op)
  - "Cookie Settings" link triggers re-entry into pending state
- **Acceptance Criteria:**
  - [ ] State machine drives all behavior
  - [ ] No double init (state prevents re-initialization)
  - [ ] First pageview not lost in ConsentManaged mode (init is cookieless, pageview captured)
  - [ ] Navigation during pending state handled correctly
  - [ ] "Cookie Settings" re-entry works from Accepted, DeclinedCookieless, DeclinedDisabled
  - [ ] Global kill switch state → no analytics, no banner
- **Effort:** L

#### Task 3.7: Add Persistent "Cookie Settings" Link
- **File:** MainLayout or footer component
- **Changes:** Add visible "Cookie Settings" link when banner is enabled
- **Acceptance Criteria:**
  - [ ] Link visible in footer/privacy area when `CookieBannerEnabled = true`
  - [ ] Clicking link triggers state machine re-entry
  - [ ] Hidden when no banner needed
- **Effort:** S

---

### Phase 4: Admin UI (Effort: L)

#### Task 4.1: Create InstanceAnalyticsSection Component
- **Files (new):**
  - `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAnalyticsSection.razor`
  - `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAnalyticsSection.razor.css`
- **Design:**
  - **Analytics Provider Section:** Provider selector, enable/disable, API key, endpoint, consent mode, transport
  - **PostHog Privacy Section (conditional):**
    - Cookieless Mode selector with contextual help per mode
    - Person Profiles selector with privacy explanation
    - Feature toggles (Session Replay, Autocapture, Heatmaps, Toolbar) — all OFF by default
  - **Cookie Consent Section:**
    - Cookie consent toggle (advisory auto-computed, not silently overwritten — Amendment 5)
    - Decline behavior selector
    - Consent cookie lifetime
    - "Recommended settings" hint shown after provider/mode change
    - Admin can override; manual choices not silently reset
  - **Contextual warnings per storage profile** (uses same resolver as backend)
  - **Global kill switch** (Amendment 6) — System-level, separate from tenant settings
- **Acceptance Criteria:**
  - [ ] Uses `IAnalyticsRuntimeProfileResolver` (or equivalent client-side logic) for warnings
  - [ ] Auto-suggestions shown as hints, not silent overwrites (Amendment 5)
  - [ ] PostHog section only when PostHog selected
  - [ ] Contextual warnings per storage profile
  - [ ] Global kill switch visible to system admins
  - [ ] MudBlazor components, existing admin patterns
- **Effort:** L
- **Skill:** `blazor-ui-conventions`, `blazor-css-isolation`

#### Task 4.2: Add Analytics Section to Admin Layout
- **File:** `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAdminSettingsLayout.razor`
- **Changes:** Add "Analytics & Privacy" navigation item
- **Effort:** S

#### Task 4.3: Create/Extend Settings Model
- **Changes:** Add all analytics properties to admin settings model with typed enum properties
- **Effort:** S

#### Task 4.4: Wire Save/Load Logic
- **Changes:** Wire analytics settings save/load via API
- **Acceptance Criteria:**
  - [ ] Settings load on page init
  - [ ] Settings save on action
  - [ ] Validation (PostHog requires API key, etc.)
  - [ ] Success/error feedback
- **Effort:** M

---

### Phase 5: Documentation (Effort: S)

#### Task 5.1: Update docs/CONFIGURATION.md
- Add 10 new governance keys
- Document storage-mode consent behavior
- Document provider capability matrix
- Document tenant-scoped cookie naming
- Document global kill switch

#### Task 5.2: Update docs/BLAZOR.md
- Add consent state machine documentation
- Document three consent modes
- Document PostHog consent integration methods
- Document "Cookie Settings" link behavior

#### Task 5.3: Update docs/OPERATIONS.md
- Update provider table with storage mode + consent columns
- Add operational guidance for global kill switch
- Document RudderStack deferred parity (Amendment 7)

---

### Phase 6: Testing (Effort: L)

#### Task 6.1: Domain Unit Tests
- All 10 new governance keys have correct defaults
- All 4 enums have correct values
- `AnalyticsProviderCapabilities.For()` returns correct capabilities per provider
- **Effort:** S

#### Task 6.2: Runtime Profile Resolver Tests (Amendment 2 — KEY TEST CLASS)
- **File (new):** `Event.Application.UnitTests/Analytics/AnalyticsRuntimeProfileResolverTests.cs`
- **Tests — the core policy engine:**
  - Global kill switch ON → no analytics, no banner
  - Analytics disabled → no banner
  - PostHog `always` → Cookieless profile, no banner
  - PostHog `on_reject` + consent enabled → ConsentManaged, banner, canRunBefore, decline=Cookieless
  - PostHog `on_reject` + consent disabled → ConsentManaged, no banner (admin override)
  - PostHog `off` + consent enabled → FullConsent, banner, can't run before, decline=Disable
  - Plausible → Cookieless, no banner
  - Rybbit → Cookieless, no banner
  - RudderStack → FullConsent, banner, can't run before
  - None → Cookieless, no banner
  - ConsentCookieKey computed correctly from tenant slug
  - PosthogClientOptions only for PostHog
- **Effort:** M
- **Skill:** `cqrs-mediatr-guidelines`

#### Task 6.3: AnalyticsSettingGroup Tests
- 10 new properties resolve correctly from settings
- Enum parsing with fallback defaults
- **Effort:** S

#### Task 6.4: Query Handler Tests
- Handler delegates to resolver (verify resolver called)
- Maps resolver output to slim DTO correctly
- No inline policy logic
- **Effort:** S

#### Task 6.5: JS Interop Contract Tests (NEW — from Architect Review)
- **Assert exact JS payload** sent from Blazor to JS bridge
- Feature defaults remain privacy-first
- Non-PostHog providers do not receive PostHog-only options
- PostHog consent methods called with correct JS function names
- **Effort:** M

#### Task 6.6: AnalyticsInitializer State Machine Tests
- All 7 states reachable
- State transitions correct per consent flow diagram
- No double init
- First pageview not lost in ConsentManaged mode
- "Cookie Settings" re-entry works
- Global kill switch → no analytics, no banner
- **Effort:** M

#### Task 6.7: CookieConsentBanner Tests
- Rendering, accept/decline actions
- Tenant-scoped cookie key
- Configurable lifetime
- **Effort:** S

#### Task 6.8: PostHog Consent Method Tests
- OptInCapturing calls correct JS method
- OptOutCapturing calls correct JS method
- GetExplicitConsentStatus returns correct state
- Non-PostHog: methods no-op
- **Effort:** S

---

## Risk Assessment

### High Risk
1. **PostHog `cookieless_mode` version stability** — `cookieless_mode`, `get_explicit_consent_status()`, and `opt_in/opt_out_capturing()` are documented but relatively recent API surfaces. Behavior may shift between SDK versions.
   - **Mitigation:** Pin `defaults: '2026-01-30'`. Runtime-check `posthog.get_explicit_consent_status` existence before calling. JS interop contract tests assert exact payloads.

### Medium Risk
2. **Consent cookie during SSR** — Blazor prerender cannot read JS cookies.
   - **Mitigation:** State machine starts in `Uninitialized`, transitions only in `OnAfterRenderAsync`.
3. **Flash of banner** — Brief moment before consent state is checked.
   - **Mitigation:** CSS initially hides banner; state machine shows only after determination.
4. **PostHog project-side cookieless config** — `cookieless_mode: 'on_reject'` requires project-level enablement in PostHog dashboard.
   - **Mitigation:** Admin UI guidance: "Ensure cookieless mode is enabled in your PostHog project settings."

### Low Risk
5. **Tenant-scoped cookie naming** — URL-safe slug required for cookie name.
   - **Mitigation:** Validate slug is URL-safe during `ConsentCookieKey` computation.
6. **RudderStack deferred parity** — May cause feature requests.
   - **Mitigation:** Document extension point in resolver. Architecture supports future addition.

---

## Success Metrics

1. PostHog `cookieless_mode: 'always'` — no banner, analytics run immediately
2. PostHog `cookieless_mode: 'on_reject'` — banner appears, analytics run cookieless before consent, accept upgrades, decline keeps cookieless
3. PostHog legacy (no cookieless) — banner appears, analytics blocked until accept
4. Rybbit/Plausible — no banner, analytics run immediately
5. PostHog features all OFF by default, `person_profiles: 'identified_only'`
6. Admin UI shows contextual warnings per storage profile, advisory suggestions (not silent overwrites)
7. Global kill switch disables all browser analytics immediately
8. Consent cookie tenant-scoped, 180-day configurable lifetime
9. "Cookie Settings" link always accessible for withdrawal
10. Runtime profile resolver is the single source of policy truth
11. Typed enums throughout — no string-based policy logic except at JS interop edge
12. All existing tests pass

---

## Potential Risks & Unknowns

The **most complex component** is the **ConsentState machine in AnalyticsInitializer** (Task 3.6). The `BannerPendingCookieless` state is the most intricate: PostHog must be initialized, analytics must be capturing in cookieless mode, the banner must be visible, and the consent transition (opt_in/opt_out) must happen without re-initialization. The state machine design (Amendment 4) mitigates the complexity by making each state explicit and transitions declarative, but the integration between Blazor component lifecycle (`OnAfterRenderAsync`), JS interop calls, and PostHog SDK state is inherently async and requires careful ordering.

The **second area** is the **AnalyticsRuntimeProfileResolver** (Task 2.2). This is the policy engine, and getting the computation right for all provider × mode × override combinations requires thorough test coverage. The resolver test suite (Task 6.2) is the single most important test class in this feature.

The **third area** is **PostHog project-side configuration**. An admin may configure `cookieless_mode: 'on_reject'` in our platform but forget to enable cookieless mode in their PostHog project settings. Cookieless events would be silently dropped. We can only mitigate this with admin UI guidance — we cannot verify PostHog project settings from our side.