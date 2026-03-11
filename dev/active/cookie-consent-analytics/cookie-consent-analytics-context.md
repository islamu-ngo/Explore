# Cookie Consent & Analytics Governance — Context

Last Updated: 2026-03-10 (Rev 3 — Post Architect Review)

---

## SESSION PROGRESS (2026-03-10)

### ✅ COMPLETED
- Comprehensive codebase analysis of analytics system
- Implementation plan Rev 1, Rev 2, Rev 3
- Architect review incorporated (7 amendments)

### 🟡 IN PROGRESS
- Plan review with user (Rev 3 delivered)

### ⚠️ BLOCKERS
- None

---

## Rev 3 Amendments (from Senior Architect Review)

| # | Amendment | Summary |
|---|-----------|---------|
| 1 | Typed enums | Replace stringly-typed contracts with domain enums. JS string mapping only at interop edge. |
| 2 | Dedicated resolver | `IAnalyticsRuntimeProfileResolver` — single source of policy truth. Admin UI + query handler + tests all use it. |
| 3 | Slim public DTO | `AnalyticsConsentBootstrap` — effective runtime config only. No admin inputs, no tenantSlug, no PersonalApiKey. |
| 4 | Consent state machine | 7-state `ConsentState` enum drives AnalyticsInitializer. No imperative if/else branching. |
| 5 | Advisory auto-computation | Suggest defaults on provider/mode change, show hint. Never silently overwrite admin's manual choices. |
| 6 | Global kill switch | `analytics.global_disable_client_tracking` — System-scope, disables all browser analytics immediately. |
| 7 | Defer RudderStack parity | RudderStack = "full consent required" for v1. Explicit extension point in resolver. |

---

## Design Principles

1. **Consent ≠ analytics enablement.** Operator config + end-user device choice are two separate layers.
2. **Public API keys** may go to browser. **Private/personal API keys NEVER** go to browser.
3. **Consent cookies** = preference artifacts only. Values: `accepted` | `declined`. No timestamps, no user IDs.
4. **Provider capability** is a first-class concept via `AnalyticsProviderCapabilities`.

---

## Key Files

### Domain Layer
- **`Explore.Domain/Constants/GovernanceSettingKeys.cs`** — Analytics keys (lines 177-186), needs 10 new keys
- **`Explore.Domain/Settings/Definitions/AnalyticsSettingDefinitions.cs`** — Needs 10 new definitions
- **`Explore.Domain/Enums/AnalyticsProviderEnum.cs`** — `None=0, Posthog=1, Plausible=2, Rybbit=3, RudderStack=4`
- **`Explore.Domain/Enums/Analytics/`** — NEW: DeclineBehavior, PosthogCookielessMode, PosthogPersonProfiles, AnalyticsStorageProfile
- **`Explore.Domain/Analytics/AnalyticsProviderCapabilities.cs`** — NEW: Provider capability matrix

### Application Layer
- **`Explore.Application/Settings/Groups/AnalyticsSettingGroup.cs`** — Needs 10 new properties (typed enums)
- **`Explore.Application/Contracts/Services/IAnalyticsRuntimeProfileResolver.cs`** — NEW: Resolver contract
- **`Explore.Application/Analytics/AnalyticsRuntimeProfileResolver.cs`** — NEW: Core policy engine
- **`Explore.Application/Analytics/AnalyticsRuntimeProfile.cs`** — NEW: Computed profile record
- **`Explore.Application/Analytics/PosthogClientOptions.cs`** — NEW: PostHog typed options
- **`Explore.Application/DTOs/Onboarding/AnalyticsConsentBootstrap.cs`** — NEW: Slim public DTO
- **`Explore.Application/DTOs/Onboarding/PublicExperienceSettingsDto.cs`** — Extends with AnalyticsConsentBootstrap
- **`Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs`** — Delegates to resolver, maps to DTO

### Browser/JS Layer
- **`Explore.Blazor.Client/Analytics/ConsentState.cs`** — NEW: 7-state consent state machine enum
- **`Explore.Blazor.Client/wwwroot/js/analytics-bridge.js`** — PostHog init + consent methods
- **`Explore.Blazor.Client/wwwroot/js/cookie-consent.js`** — NEW: Tenant-scoped cookie utility
- **`Explore.Blazor.Client/Shared/AnalyticsInitializer.razor`** — State machine driven
- **`Explore.Blazor.Client/Shared/CookieConsentBanner.razor`** — NEW: Banner component
- **`Explore.Blazor.Client/Contracts/Interop/ICookieConsentInterop.cs`** — NEW
- **`Explore.Blazor.Client/Contracts/Interop/IAnalyticsInterop.cs`** — Extended with consent methods
- **`Explore.Blazor.Client/Services/AnalyticsInterop.cs`** — Extended
- **`Explore.Blazor/Services/ServerAnalyticsInterop.cs`** — Extended (no-ops)

### Admin UI Layer
- **`Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAdminSettingsLayout.razor`** — Sidebar
- **`Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAnalyticsSection.razor`** — NEW
- **`Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceGovernanceSection.razor`** — Reference pattern

### Tests
- **`Event.Application.UnitTests/Analytics/AnalyticsRuntimeProfileResolverTests.cs`** — NEW: KEY test class
- **`Event.Application.UnitTests/Features/PublicExperience/...`** — Handler tests (delegates to resolver)
- **`Event.Application.UnitTests/Settings/AnalyticsSettingGroupTests.cs`** — New property tests
- **`Explore.Blazor.Client.Tests/...`** — State machine, banner, JS interop contract tests

### Documentation
- **`docs/CONFIGURATION.md`** — Analytics settings table, needs 10 new keys
- **`docs/BLAZOR.md`** — Analytics bootstrap, needs state machine docs
- **`docs/OPERATIONS.md`** — Provider table, needs storage mode + consent columns

---

## Key Decisions

### Decision 1: Storage-Mode-Driven Consent
Consent is computed from the provider's **runtime storage behavior**, not just the provider name. Implemented via `IAnalyticsRuntimeProfileResolver` which returns an `AnalyticsRuntimeProfile` — the single source of policy truth.

### Decision 2: Graceful Decline via Cookieless Analytics
PostHog `cookieless_mode: 'on_reject'` — declined users still counted via privacy-preserving server-side hash. Configurable: `Cookieless` (default) or `Disable`.

### Decision 3: Tenant-Scoped Consent Cookie
`explore_cc_{tenantSlug}` — computed server-side as `ConsentCookieKey` (raw slug not exposed). 180 days configurable. Values: `accepted` | `declined` only.

### Decision 4: Privacy-First PostHog Defaults
Features OFF, `cookieless_mode: on_reject`, `person_profiles: identified_only`. Explicit admin action required to enable.

### Decision 5: Self-Hoster Override with Warnings (Advisory, Not Destructive)
Auto-suggest defaults on provider/mode change. Show "recommended settings" hint. Never silently overwrite admin's manual choices.

### Decision 6: No Page Blocking + Persistent Withdrawal
Banner non-blocking. "Cookie Settings" link in footer for consent withdrawal. State machine supports re-entry.

### Decision 7: Global Emergency Kill Switch
`analytics.global_disable_client_tracking` — System scope. One flag disables all browser analytics immediately.

### Decision 8: Defer RudderStack Cookieless Parity
RudderStack treated as "full consent required" for v1. Resolver has explicit extension point.

---

## Interface Signatures

### Domain Enums
```csharp
public enum DeclineBehavior { Disable = 0, Cookieless = 1 }
public enum PosthogCookielessMode { Off = 0, Always = 1, OnReject = 2 }
public enum PosthogPersonProfiles { Always = 0, IdentifiedOnly = 1, Never = 2 }
public enum AnalyticsStorageProfile { Cookieless = 0, ConsentManaged = 1, FullConsent = 2 }
```

### Consent State Machine
```csharp
public enum ConsentState
{
    Uninitialized,
    NoBannerImmediateInit,
    BannerPendingCookieless,
    BannerPendingBlocked,
    Accepted,
    DeclinedCookieless,
    DeclinedDisabled
}
```

### Resolver Contract
```csharp
public interface IAnalyticsRuntimeProfileResolver
{
    AnalyticsRuntimeProfile Resolve(AnalyticsSettingGroup settings);
}
```

### Runtime Profile
```csharp
public sealed record AnalyticsRuntimeProfile
{
    public AnalyticsStorageProfile StorageProfile { get; init; }
    public bool CookieBannerEnabled { get; init; }
    public bool CanRunBeforeConsent { get; init; }
    public DeclineBehavior DeclineBehavior { get; init; }
    public string ConsentCookieKey { get; init; }
    public int ConsentCookieLifetimeDays { get; init; }
    public PosthogClientOptions? Posthog { get; init; }
}
```

### Slim Public DTO
```csharp
public sealed class AnalyticsConsentBootstrap
{
    public bool CookieBannerEnabled { get; set; }
    public bool CanRunBeforeConsent { get; set; }
    public string DeclineBehavior { get; set; }        // JS: "none"|"cookieless"|"disable"
    public string ConsentCookieKey { get; set; }       // "explore_cc_{slug}"
    public int ConsentCookieLifetimeDays { get; set; }
    public string AnalyticsProvider { get; set; }
    public PosthogClientBootstrap? Posthog { get; set; }
}
```

### Governance Keys (10 total)
```csharp
// Consent & Storage
public const string CookieConsentEnabled = "analytics.cookie_consent_enabled";
public const string DeclineBehavior = "analytics.decline_behavior";
public const string ConsentCookieLifetimeDays = "analytics.consent_cookie_lifetime_days";
public const string GlobalDisableClientTracking = "analytics.global_disable_client_tracking";

// PostHog Privacy & Features
public const string PosthogCookielessMode = "analytics.posthog_cookieless_mode";
public const string PosthogPersonProfiles = "analytics.posthog_person_profiles";
public const string PosthogSessionReplay = "analytics.posthog_session_replay";
public const string PosthogAutocapture = "analytics.posthog_autocapture";
public const string PosthogHeatmaps = "analytics.posthog_heatmaps";
public const string PosthogToolbar = "analytics.posthog_toolbar";
```

### PostHog JS Init (Rev 3)
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

// Consent methods
posthog.opt_in_capturing();
posthog.opt_out_capturing();
posthog.get_explicit_consent_status(); // "pending"|"granted"|"denied"
```

---

## Quick Resume

To continue this work:
1. Read this context file for current state and all 7 amendments
2. Read the tasks file for remaining work (31 tasks across 6 phases)
3. Read the plan file for full architecture: resolver, state machine, capability matrix
4. Start with Phase 1 (Domain Layer) — enums, capability matrix, governance keys, setting definitions
5. Work inward through architecture layers
6. Key PostHog docs: https://posthog.com/docs/privacy/data-collection