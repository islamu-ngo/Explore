# Cookie Consent & Analytics Governance — Task Checklist

Last Updated: 2026-04-11 (Rev 5 — Full Completion Audit)

---

## Phase 1: Domain Layer ✅ COMPLETE

- [x] **1.1** Create analytics enums: `DeclineBehavior`, `PosthogCookielessMode`, `PosthogPersonProfiles`, `AnalyticsStorageProfile` in `Explore.Domain/Enums/Analytics/`
- [x] **1.2** Create `AnalyticsProviderCapabilities` record in `Explore.Domain/Analytics/` with static factory — capability flags per provider (RudderStack `SupportsCookielessMode = false`)
- [x] **1.3** Add 17 governance keys to `GovernanceSettingKeys.Analytics` (7 original + 10 new)
- [x] **1.4** Add 17 `SettingDefinition` entries in `AnalyticsSettingDefinitions.cs` — privacy-first defaults
- [x] **1.5** Create `ProfileResolveReason` enum in `Explore.Domain/Enums/Analytics/` — 9 reason codes for resolver diagnostics

## Phase 2: Application Layer ✅ COMPLETE

- [x] **2.1** Extend `AnalyticsSettingGroup` with 17 typed properties — string settings parsed to domain enums with fallback defaults
- [x] **2.2** Create `IAnalyticsRuntimeProfileResolver` + `AnalyticsRuntimeProfile` (with `PosthogClientOptions` inline) + `AnalyticsRuntimeProfileResolver` — core policy engine
- [x] **2.3** Create `AnalyticsConsentBootstrapDto` + `PosthogClientBootstrapDto` slim public DTOs
- [x] **2.4** Update `GetPublicExperienceSettingsQueryHandler` — delegates to resolver, maps profile → DTO
- [x] **2.5** Register `IAnalyticsRuntimeProfileResolver` in DI (`ApplicationServicesRegistration.cs`)

## Phase 3: Browser/JS Layer ✅ COMPLETE

- [x] **3.1** Create `ConsentState` enum in `Explore.Blazor.Client/Models/Analytics/` — 7 states
- [x] **3.2** Create `CookieConsentBanner.razor` — fixed bottom, equal Accept/Decline, MudBlazor, BEM, accessible
- [x] **3.3** Create `ICookieConsentInterop` + `cookie-consent.js` + `CookieConsentStateService` — tenant-scoped cookie, 180-day default, SameSite=Lax, Secure
- [x] **3.4** Update `analytics-bridge.js` — PostHog init with `cookieless_mode`, `person_profiles`, feature controls + consent methods
- [x] **3.5** Update `IAnalyticsInterop` + `AnalyticsInterop` (6 methods: Init, Track, Identify, PageView, OptIn, OptOut) + `ServerAnalyticsInterop` (no-ops)
- [x] **3.6** Rewrite `AnalyticsInitializer.razor` as 7-state ConsentState machine (311 lines) — handles bootstrap, pageview tracking, navigation, re-entry, kill switch
- [x] **3.7** Add "Cookie Settings" link — triggers `CookieConsentStateService.RequestReopenConsent()`

## Phase 4: Admin UI ✅ COMPLETE

- [x] **4.1** Create `InstanceAnalyticsPrivacySection.razor` (261 lines) — provider controls, PostHog privacy section, cookie consent section, advisory chips from resolver, legal/incompatibility warnings, tenant delegation lock
- [x] **4.2** Add "Analytics & Privacy" to `InstanceAdminSettingsLayout.razor` sidebar navigation
- [x] **4.3** Create `AnalyticsGovernanceSettingsModel.cs` with typed properties + computed advisory fields (CookieBannerRequired, CanRunBeforeConsent, StorageProfile)
- [x] **4.4** Wire save/load via `InstanceOnboardingService.GetAnalyticsGovernanceSettingsAsync()` / `UpdateAnalyticsGovernanceSettingsAsync()`

## Phase 5: Hardening (Feedback Code Changes) ✅ COMPLETE

- [x] **5.1** **Stable ConsentCookieKey** (Amendment 9) — Resolver uses `settings.TenantStableKey` (first 8 hex chars of tenant GUID). `AnalyticsSettingGroup.TenantStableKey` set externally by query handler. Default fallback `explore_cc_default`.
- [x] **5.2** **Resolver diagnostics** (Amendment 10) — `ProfileResolveReason` enum (9 values) in `Explore.Domain/Enums/Analytics/`. `ResolveReasons` collection on `AnalyticsRuntimeProfile`. Every resolver path produces exactly 2 reasons (primary + banner). Exposed in admin query handler via `ResolveReasons` list; NOT in public DTO.
- [x] **5.3** **Command-side validation** (Amendment 12) — `ValidateSettings()` rejects: out-of-range cookie lifetime (1-730), cookieless decline behavior for non-supporting providers. `CollectWarnings()` advises: PostHog features on non-PostHog provider, session replay in always-cookieless mode, unnecessary consent banner for inherently cookieless provider.
- [x] **5.4** **Hardening tests** — Resolver tests (383 lines) include reason code assertions on every test. Command handler tests (310 lines) cover validation rejection + warning scenarios. Cookie key tests verify stable key derivation + default fallback + uniqueness.

## Phase 6: Documentation ✅ COMPLETE

- [x] **6.1** `docs/CONFIGURATION.md` — 17 governance keys table, kill switch boundary (browser-only scope, line 117), cross-subdomain cookie scope (per-host/tenant, SameSite=Lax, line 142), provider capability matrix
- [x] **6.2** `docs/BLAZOR.md` — Consent state machine (lines 80-115), SSR/prerender stance section (lines 116-125: all final decisions post-hydration), PostHog consent methods, Cookie Settings re-entry
- [x] **6.3** `docs/OPERATIONS.md` — Provider table with storage mode + consent columns (lines 287-293), kill switch operational guidance (lines 277-282), resolve reasons documented (line 316), save-time validation documented (line 317)

## Phase 7: Testing Gap Coverage ✅ COMPLETE

- [x] **7.1** CookieConsentBanner bUnit tests (133 lines) — `Explore.Blazor.Client.Tests/Components/CookieConsentBannerTests.cs`
- [x] **7.2** JS interop contract tests (123 lines) — `Explore.Blazor.Client.Tests/Services/AnalyticsInteropContractTests.cs`
- [x] **7.3** CookieConsentStateService tests (98 lines) — `Explore.Blazor.Client.Tests/Services/CookieConsentStateServiceTests.cs`
- [x] **7.4** ICookieConsentInterop contract tests (97 lines) — `Explore.Blazor.Client.Tests/Services/CookieConsentInteropContractTests.cs`

---

## Effort Summary

| Phase | Tasks | Status |
|-------|-------|--------|
| Phase 1: Domain | 5 | ✅ Complete |
| Phase 2: Application | 5 | ✅ Complete |
| Phase 3: Browser/JS | 7 | ✅ Complete |
| Phase 4: Admin UI | 4 | ✅ Complete |
| Phase 5: Hardening | 4 | ✅ Complete |
| Phase 6: Documentation | 3 | ✅ Complete |
| Phase 7: Testing Gaps | 4 | ✅ Complete |
| **Total** | **32** | **32 done / 0 remaining** |

## Acceptance Gates

### Gate 1: Resolver Correctness ✅ PASSED
- [x] 20+ tests cover all provider paths, modes, kill switch, banner combinations
- [x] Reason code assertions on every test (2 reasons per profile)
- [x] Stable cookie key tests (derivation, default fallback, uniqueness)

### Gate 2: Browser State Machine Correctness ✅ PASSED
- [x] AnalyticsInitializer tests cover bootstrap, degradation, pageview
- [x] CookieConsentBanner bUnit tests (133 lines)
- [x] JS interop contract tests (123 lines)
- [x] CookieConsentStateService tests (98 lines)

## Status

**ALL PHASES COMPLETE.** Feature is fully implemented, tested, and documented.
