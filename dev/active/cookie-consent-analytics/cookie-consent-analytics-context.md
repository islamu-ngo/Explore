# Cookie Consent & Analytics Governance — Context

Last Updated: 2026-04-11 (Rev 5 — Full Completion Audit)

---

## SESSION PROGRESS

### ✅ ALL PHASES COMPLETE
- Comprehensive codebase analysis of analytics system (Rev 1-3)
- Implementation plan Rev 1, Rev 2, Rev 3, Rev 4 (feedback incorporation)
- Architect review incorporated (7 amendments → Amendments 1-7)
- **Phase 1: Domain Layer** — 4 enums + `ProfileResolveReason` (9 values), capability matrix, 17 governance keys, 17 setting definitions
- **Phase 2: Application Layer** — AnalyticsSettingGroup (17 props + TenantStableKey), resolver + profile (with PosthogClientOptions inline) + resolve reasons, DTOs, query handler, DI
- **Phase 3: Browser/JS Layer** — ConsentState enum, CookieConsentBanner, CookieConsentStateService, cookie-consent.js, analytics-bridge.js, AnalyticsInitializer state machine (311 lines)
- **Phase 4: Admin UI** — InstanceAnalyticsPrivacySection (261 lines), sidebar nav, settings model, save/load
- **Phase 5: Hardening** — Stable ConsentCookieKey (TenantStableKey), resolver diagnostics (ResolveReasons), command-side validation (ValidateSettings + CollectWarnings)
- **Phase 6: Documentation** — CONFIGURATION.md (17 keys + kill switch boundary + cookie scope), BLAZOR.md (state machine + SSR/prerender stance), OPERATIONS.md (provider table + kill switch ops + resolve reasons)
- **Phase 7: Testing Gaps** — CookieConsentBanner bUnit (133 lines), AnalyticsInterop contracts (123 lines), CookieConsentStateService (98 lines), CookieConsentInterop contracts (97 lines), command handler validation (310 lines), query handler (213 lines)
- **100+ tests** across 15+ test files

### ⚠️ NO ACTIVE RISKS — All mitigated

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
| 8 | Kill switch boundary documentation | Feedback #1 | ✅ Done (CONFIGURATION.md line 117, OPERATIONS.md lines 277-282) |
| 9 | Stable ConsentCookieKey (not mutable slug) | Feedback #2 | ✅ Done (TenantStableKey in resolver) |
| 10 | Resolver diagnostics / reason codes | Feedback #3 | ✅ Done (ProfileResolveReason enum, 9 values, every path) |
| 11 | No client-side policy duplication | Feedback #4 | ✅ Verified |
| 12 | Command-side validation | Feedback #5 | ✅ Done (ValidateSettings + CollectWarnings) |
| 13 | Consent withdrawal = UI transition only | Feedback #6 | ✅ Verified |
| 14 | Cross-subdomain cookie scope docs | Feedback #7 | ✅ Done (CONFIGURATION.md line 142) |
| 15 | SSR/prerender stance docs | Feedback #9 | ✅ Done (BLAZOR.md lines 116-125) |

---

## Design Principles

1. **Consent ≠ analytics enablement.** Operator config + end-user device choice are two separate layers.
2. **Public API keys** may go to browser. **Private/personal API keys NEVER** go to browser.
3. **Consent cookies** = preference artifacts only. Values: `accepted` | `declined`. No timestamps, no user IDs.
4. **Provider capability** is a first-class concept via `AnalyticsProviderCapabilities`.

---

## Key Files (Actual Paths, Verified 2026-04-11)

### Domain Layer
- `Explore.Domain/Enums/Analytics/DeclineBehavior.cs`
- `Explore.Domain/Enums/Analytics/PosthogCookielessMode.cs`
- `Explore.Domain/Enums/Analytics/PosthogPersonProfiles.cs`
- `Explore.Domain/Enums/Analytics/AnalyticsStorageProfile.cs`
- `Explore.Domain/Enums/Analytics/ProfileResolveReason.cs` — 9 diagnostic reason codes
- `Explore.Domain/Analytics/AnalyticsProviderCapabilities.cs`
- `Explore.Domain/Constants/GovernanceSettingKeys.cs` — 17 analytics keys
- `Explore.Domain/Settings/Definitions/AnalyticsSettingDefinitions.cs` — 17 definitions

### Application Layer
- `Explore.Application/Settings/Groups/AnalyticsSettingGroup.cs` — 17 typed properties, `TenantStableKey` for cookie scoping
- `Explore.Application/Contracts/Services/IAnalyticsRuntimeProfileResolver.cs`
- `Explore.Application/Analytics/AnalyticsRuntimeProfileResolver.cs` — 140 lines, core policy engine with reason codes
- `Explore.Application/Analytics/AnalyticsRuntimeProfile.cs` — record + `PosthogClientOptions` (inline, not separate file) + `ResolveReasons`
- `Explore.Application/DTOs/Onboarding/AnalyticsConsentBootstrap.cs` — `AnalyticsConsentBootstrapDto` + `PosthogClientBootstrapDto`
- `Explore.Application/DTOs/Analytics/AnalyticsGovernanceSettingsDto.cs` — admin settings DTO with advisory fields + ResolveReasons
- `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs` — injects resolver, maps to DTO
- `Explore.Application/Features/InstanceOnboarding/Handlers/Commands/UpdateAnalyticsGovernanceSettingsCommandHandler.cs` — ValidateSettings + CollectWarnings + persist
- `Explore.Application/Features/InstanceOnboarding/Handlers/Queries/GetAnalyticsGovernanceSettingsQueryHandler.cs` — injects resolver, maps advisory values + ResolveReasons

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
- `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAnalyticsPrivacySection.razor` — 261 lines
- `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAdminSettingsLayout.razor` — sidebar nav
- `Explore.Blazor.Client/Models/Analytics/AnalyticsGovernanceSettingsModel.cs` — advisory fields marked "from resolver"

### Tests
- `Event.Application.UnitTests/Analytics/AnalyticsRuntimeProfileResolverTests.cs` — 383 lines, all paths + reason codes + stable key
- `Event.Application.UnitTests/Analytics/UpdateAnalyticsGovernanceSettingsCommandHandlerTests.cs` — 310 lines, validation + warnings
- `Event.Application.UnitTests/Analytics/GetAnalyticsGovernanceSettingsQueryHandlerTests.cs` — 213 lines
- `Event.Application.UnitTests/Analytics/AnalyticsConsentBootstrapDtoTests.cs` — 140 lines
- `Event.Application.UnitTests/Settings/Groups/AnalyticsSettingGroupTests.cs` — 102 lines (also `Settings/AnalyticsSettingGroupTests.cs`)
- `Event.Application.UnitTests/Services/AnalyticsGovernanceServiceTests.cs` — 80 lines
- `Event.Domain.UnitTests/Analytics/AnalyticsProviderCapabilitiesTests.cs` — 62 lines
- `Explore.Blazor.Client.Tests/Components/AnalyticsInitializerTests.cs` — state machine tests
- `Explore.Blazor.Client.Tests/Components/CookieConsentBannerTests.cs` — 133 lines, bUnit tests
- `Explore.Blazor.Client.Tests/Models/Analytics/ConsentStateTests.cs` — 50 lines
- `Explore.Blazor.Client.Tests/Services/AnalyticsInteropContractTests.cs` — 123 lines
- `Explore.Blazor.Client.Tests/Services/CookieConsentStateServiceTests.cs` — 98 lines
- `Explore.Blazor.Client.Tests/Services/CookieConsentInteropContractTests.cs` — 97 lines

### Documentation
- `docs/CONFIGURATION.md` — 17 keys + kill switch boundary + cookie scope
- `docs/BLAZOR.md` — Consent state machine + SSR/prerender stance
- `docs/OPERATIONS.md` — Provider table + kill switch ops + resolve reasons + save-time validation

---

## Key Decisions

### Decision 1: Storage-Mode-Driven Consent ✅
Consent computed from provider's runtime storage behavior via `IAnalyticsRuntimeProfileResolver`.

### Decision 2: Graceful Decline via Cookieless ✅
PostHog `cookieless_mode: 'on_reject'` — declined users still counted. Configurable: `Cookieless` (default) or `Disable`.

### Decision 3: Tenant-Scoped Consent Cookie ✅
Uses `TenantStableKey` (not mutable slug): `explore_cc_{settings.TenantStableKey ?? "default"}`

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

All 7 phases are complete. This feature is fully implemented and tested.
No remaining work items. If revisiting, run build + tests to verify green state:
```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```
