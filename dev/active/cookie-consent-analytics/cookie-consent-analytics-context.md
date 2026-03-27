# Cookie Consent & Analytics Governance — Context

Last Updated: 2026-03-26 (Rev 4 — Post-Implementation Audit + Feedback Incorporation)

---

## SESSION PROGRESS

### ✅ COMPLETED
- Comprehensive codebase analysis of analytics system (Rev 1-3)
- Implementation plan Rev 1, Rev 2, Rev 3
- Architect review incorporated (7 amendments → Amendments 1-7)
- **Phase 1: Domain Layer** — 4 enums, capability matrix, 17 governance keys, 17 setting definitions
- **Phase 2: Application Layer** — AnalyticsSettingGroup (17 props), resolver + profile + options, DTOs, query handler, DI
- **Phase 3: Browser/JS Layer** — ConsentState enum, CookieConsentBanner, CookieConsentStateService, cookie-consent.js, analytics-bridge.js, AnalyticsInitializer state machine (311 lines)
- **Phase 4: Admin UI** — InstanceAnalyticsPrivacySection (261 lines), sidebar nav, settings model, save/load
- **80+ tests** across 13 test files (resolver: 20, setting group: 17, bootstrap DTO: 13, capabilities: 5, initializer: 5, consent state: 5, governance service: 3, etc.)
- Rev 4 plan update: codebase audit + feedback incorporation

### ⏳ REMAINING
- Phase 5: Hardening (3 code changes + tests from feedback)
- Phase 6: Documentation (3 docs updates)
- Phase 7: Testing gaps (4 test suites)

### ⚠️ ACTIVE RISKS
- ConsentCookieKey uses mutable tenant slug (HIGH — Amendment 9)
- No command-side validation (MEDIUM — Amendment 12)

---

## All Amendments (1-15)

| # | Amendment | Source | Status |
|---|-----------|--------|--------|
| 1 | Typed enums replace stringly-typed contracts | Rev 3 | ✅ Done |
| 2 | Dedicated `IAnalyticsRuntimeProfileResolver` | Rev 3 | ✅ Done |
| 3 | Slim public `AnalyticsConsentBootstrapDto` | Rev 3 | ✅ Done |
| 4 | 7-state `ConsentState` machine | Rev 3 | ✅ Done |
| 5 | Advisory auto-computation (suggest, don't overwrite) | Rev 3 | ✅ Done |
| 6 | Global kill switch | Rev 3 | ✅ Done |
| 7 | Defer RudderStack parity | Rev 3 | ✅ Done |
| 8 | Kill switch boundary documentation | Feedback #1 | ⏳ Phase 6 |
| 9 | Stable ConsentCookieKey (not mutable slug) | Feedback #2 | ⏳ Phase 5 |
| 10 | Resolver diagnostics / reason codes | Feedback #3 | ⏳ Phase 5 |
| 11 | No client-side policy duplication | Feedback #4 | ✅ Verified |
| 12 | Command-side validation | Feedback #5 | ⏳ Phase 5 |
| 13 | Consent withdrawal = UI transition only | Feedback #6 | ✅ Verified |
| 14 | Cross-subdomain cookie scope docs | Feedback #7 | ⏳ Phase 6 |
| 15 | SSR/prerender stance docs | Feedback #9 | ⏳ Phase 6 |

---

## Design Principles

1. **Consent ≠ analytics enablement.** Operator config + end-user device choice are two separate layers.
2. **Public API keys** may go to browser. **Private/personal API keys NEVER** go to browser.
3. **Consent cookies** = preference artifacts only. Values: `accepted` | `declined`. No timestamps, no user IDs.
4. **Provider capability** is a first-class concept via `AnalyticsProviderCapabilities`.

---

## Key Files (Actual Paths, Verified)

### Domain Layer
- `Explore.Domain/Enums/Analytics/DeclineBehavior.cs`
- `Explore.Domain/Enums/Analytics/PosthogCookielessMode.cs`
- `Explore.Domain/Enums/Analytics/PosthogPersonProfiles.cs`
- `Explore.Domain/Enums/Analytics/AnalyticsStorageProfile.cs`
- `Explore.Domain/Analytics/AnalyticsProviderCapabilities.cs`
- `Explore.Domain/Constants/GovernanceSettingKeys.cs` — 17 analytics keys
- `Explore.Domain/Settings/Definitions/AnalyticsSettingDefinitions.cs` — 17 definitions

### Application Layer
- `Explore.Application/Settings/Groups/AnalyticsSettingGroup.cs` — 17 typed properties, snake_case→PascalCase
- `Explore.Application/Contracts/Services/IAnalyticsRuntimeProfileResolver.cs`
- `Explore.Application/Analytics/AnalyticsRuntimeProfileResolver.cs` — 130 lines, core policy engine
- `Explore.Application/Analytics/AnalyticsRuntimeProfile.cs` — record (⚠️ needs reason codes, Amendment 10)
- `Explore.Application/Analytics/PosthogClientOptions.cs`
- `Explore.Application/DTOs/Onboarding/AnalyticsConsentBootstrap.cs` — `AnalyticsConsentBootstrapDto` + `PosthogClientBootstrapDto`
- `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs` — injects resolver, maps to DTO
- `Explore.Application/Features/Analytics/Handlers/Commands/UpdateAnalyticsGovernanceSettingsCommandHandler.cs` — ⚠️ pure write-through, no validation (Amendment 12)
- `Explore.Application/Features/Analytics/Handlers/Queries/GetAnalyticsGovernanceSettingsQueryHandler.cs` — injects resolver, maps advisory values

### Browser/JS Layer
- `Explore.Blazor.Client/Models/Analytics/ConsentState.cs` — 7-state enum
- `Explore.Blazor.Client/Models/Analytics/AnalyticsConsentBootstrapModel.cs` — client model
- `Explore.Blazor.Client/Shared/CookieConsentBanner.razor` — 33 lines, MudBlazor, equal Accept/Decline
- `Explore.Blazor.Client/Services/CookieConsentStateService.cs` — cross-component event bridge
- `Explore.Blazor.Client/Contracts/Interop/ICookieConsentInterop.cs`
- `Explore.Blazor.Client/Contracts/Interop/IAnalyticsInterop.cs` — 6 methods
- `Explore.Blazor.Client/Services/AnalyticsInterop.cs` — client-side implementation
- `Explore.Blazor/Services/ServerAnalyticsInterop.cs` — server-side no-ops
- `Explore.Blazor.Client/wwwroot/js/cookie-consent.js` — 23 lines, readConsent/writeConsent/clearConsent
- `Explore.Blazor.Client/wwwroot/js/analytics-bridge.js` — multi-provider adapter, cookieless_mode support
- `Explore.Blazor.Client/Shared/AnalyticsInitializer.razor` — 311 lines, full state machine

### Admin UI
- `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAnalyticsPrivacySection.razor` — 261 lines (NOTE: named `PrivacySection`, not `Section`)
- `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAdminSettingsLayout.razor` — sidebar nav, line 505
- `Explore.Blazor.Client/Models/Admin/AnalyticsGovernanceSettingsModel.cs` — 36 lines, advisory fields marked "from resolver"

### Tests (Existing)
- `Event.Application.UnitTests/Analytics/AnalyticsRuntimeProfileResolverTests.cs` — 20 tests
- `Event.Application.UnitTests/Settings/AnalyticsSettingGroupTests.cs` — 17 tests
- `Event.Application.UnitTests/DTOs/AnalyticsConsentBootstrapDtoTests.cs` — 13 tests
- `Event.Application.UnitTests/Analytics/AnalyticsProviderCapabilitiesTests.cs` — 5 tests
- `Explore.Blazor.Client.Tests/Shared/AnalyticsInitializerTests.cs` — 5 tests
- `Explore.Blazor.Client.Tests/Models/Analytics/ConsentStateTests.cs` — 5 tests
- `Event.Application.UnitTests/Services/AnalyticsGovernanceServiceTests.cs` — 3 tests

### Documentation
- `docs/BLAZOR.md` — Already has "Cookie Consent & Privacy State Machine" section (lines 80-115)
- `docs/CONFIGURATION.md` — Needs 17 keys + kill switch boundary + cookie scope
- `docs/OPERATIONS.md` — Needs provider table + kill switch ops guide

---

## Key Decisions

### Decision 1: Storage-Mode-Driven Consent ✅
Consent computed from provider's runtime storage behavior via `IAnalyticsRuntimeProfileResolver`.

### Decision 2: Graceful Decline via Cookieless ✅
PostHog `cookieless_mode: 'on_reject'` — declined users still counted. Configurable: `Cookieless` (default) or `Disable`.

### Decision 3: Tenant-Scoped Consent Cookie ⚠️ NEEDS HARDENING
Currently: `explore_cc_{tenantSlug}` from mutable subdomain. Amendment 9: change to stable identifier.

### Decision 4: Privacy-First PostHog Defaults ✅
Features OFF, `cookieless_mode: on_reject`, `person_profiles: identified_only`.

### Decision 5: Advisory Auto-Computation ✅
Admin UI shows "Computed advisory (read-only, from resolver)". Never silently overwrites.

### Decision 6: Consent Withdrawal = UI Transition Only ✅
`ReopenConsentAsync` clears cookie, returns to pending. Analytics state changes only on explicit accept/decline.

### Decision 7: No Client-Side Policy Duplication ✅
Admin query handler injects resolver, maps to advisory fields. Admin model doesn't re-implement logic.

### Decision 8: Global Kill Switch ✅
`analytics.global_disable_client_tracking` — System scope. Browser-only boundary (Amendment 8 documents this).

### Decision 9: Defer RudderStack Cookieless Parity ✅
Resolver has explicit extension point. `SupportsCookielessMode = false` for v1.

---

## Governance Keys (17 total, all implemented)

```csharp
// Original 7
analytics.provider, analytics.enabled, analytics.consent_mode,
analytics.transport_mode, analytics.api_key, analytics.endpoint_url, analytics.personal_api_key

// Consent & Storage (4 new)
analytics.cookie_consent_enabled     // bool, default false, Tenant scope
analytics.decline_behavior           // string, default "cookieless", Tenant scope
analytics.consent_cookie_lifetime_days // int, default 180, Tenant scope
analytics.global_disable_client_tracking // bool, default false, System scope

// PostHog Privacy & Features (6 new)
analytics.posthog_cookieless_mode    // string, default "on_reject", Tenant scope
analytics.posthog_person_profiles    // string, default "identified_only", Tenant scope
analytics.posthog_session_replay     // bool, default false, Tenant scope
analytics.posthog_autocapture        // bool, default false, Tenant scope
analytics.posthog_heatmaps           // bool, default false, Tenant scope
analytics.posthog_toolbar            // bool, default false, Tenant scope
```

---

## PostHog JS Config (Implemented)

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

// Consent methods (implemented in analytics-bridge.js)
posthog.opt_in_capturing();
posthog.opt_out_capturing();
posthog.get_explicit_consent_status(); // "pending"|"granted"|"denied"
```

---

## Quick Resume

To continue this work:
1. Read this context file for current state and all 15 amendments
2. Read the tasks file for remaining work (11 tasks across Phases 5-7)
3. Read the plan file for architecture reference and hardening details
4. **Start with Phase 5, Task 5.1** — change ConsentCookieKey from mutable `TenantSlug` to stable tenant identifier
5. Key files to change first: `AnalyticsRuntimeProfileResolver.cs` (line 16), `GetPublicExperienceSettingsQueryHandler.cs` (line 88)
