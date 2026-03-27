# Cookie Consent & Analytics Governance — Task Checklist

Last Updated: 2026-03-26 (Rev 4 — Post-Implementation Audit + Feedback Incorporation)

---

## Phase 1: Domain Layer ✅ COMPLETE

- [x] **1.1** Create analytics enums: `DeclineBehavior`, `PosthogCookielessMode`, `PosthogPersonProfiles`, `AnalyticsStorageProfile` in `Explore.Domain/Enums/Analytics/`
- [x] **1.2** Create `AnalyticsProviderCapabilities` record in `Explore.Domain/Analytics/` with static factory — capability flags per provider (RudderStack `SupportsCookielessMode = false`)
- [x] **1.3** Add 17 governance keys to `GovernanceSettingKeys.Analytics` (7 original + 10 new: cookie_consent_enabled, decline_behavior, consent_cookie_lifetime_days, global_disable_client_tracking, posthog_cookieless_mode, posthog_person_profiles, posthog_session_replay, posthog_autocapture, posthog_heatmaps, posthog_toolbar)
- [x] **1.4** Add 17 `SettingDefinition` entries in `AnalyticsSettingDefinitions.cs` — privacy-first defaults

## Phase 2: Application Layer ✅ COMPLETE

- [x] **2.1** Extend `AnalyticsSettingGroup` with 17 typed properties — string settings parsed to domain enums with fallback defaults
- [x] **2.2** Create `IAnalyticsRuntimeProfileResolver` + `AnalyticsRuntimeProfile` + `PosthogClientOptions` + `AnalyticsRuntimeProfileResolver` — core policy engine
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
- [x] **4.2** Add "Analytics & Privacy" to `InstanceAdminSettingsLayout.razor` sidebar navigation (line 505)
- [x] **4.3** Create `AnalyticsGovernanceSettingsModel.cs` with typed properties + computed advisory fields (CookieBannerRequired, CanRunBeforeConsent, StorageProfile)
- [x] **4.4** Wire save/load via `InstanceOnboardingService.GetAnalyticsGovernanceSettingsAsync()` / `UpdateAnalyticsGovernanceSettingsAsync()`

---

## Phase 5: Hardening (Feedback Code Changes) ⏳ NOT STARTED

- [ ] **5.1** **Stable ConsentCookieKey** (Amendment 9) — Replace `settings.TenantSlug` in resolver with stable tenant identifier derivation. Update `GetPublicExperienceSettingsQueryHandler` to provide stable key instead of subdomain. Ensure default fallback `explore_cc_default` for single-tenant mode.
- [ ] **5.2** **Resolver diagnostics** (Amendment 10) — Add `ProfileResolveReason` enum + `ResolveReasons` collection to `AnalyticsRuntimeProfile`. Populate in resolver for every code path. Expose in admin query handler (internal only, NOT in public DTO). Update admin UI advisory display.
- [ ] **5.3** **Command-side validation** (Amendment 12) — Add validation to `UpdateAnalyticsGovernanceSettingsCommandHandler`: reject invalid combinations (PostHog without API key, cookieless for non-supporting provider), save-with-warning for suboptimal combinations. Use `AnalyticsProviderCapabilities` for constraint checks. Validators manually instantiated.
- [ ] **5.4** **Hardening tests** — Update all 20 resolver tests with reason code assertions. Add command validation tests (invalid → reject, suboptimal → warn). Add stable cookie key survival tests.

## Phase 6: Documentation ⏳ NOT STARTED

- [ ] **6.1** Update `docs/CONFIGURATION.md` — 17 governance keys table, kill switch boundary semantics (Amendment 8: browser-only scope, future full-system option), cross-subdomain cookie scope (Amendment 14: per-host/tenant, conservative), provider capability matrix
- [ ] **6.2** Update `docs/BLAZOR.md` — Verify existing consent state machine docs (lines 80-115), add SSR/prerender stance (Amendment 15: all final decisions post-hydration), document PostHog consent methods, "Cookie Settings" re-entry
- [ ] **6.3** Update `docs/OPERATIONS.md` — Provider table with storage mode + consent columns, kill switch operational guidance, RudderStack deferred parity, auditability note (userId in SetValueAsync)

## Phase 7: Testing Gap Coverage ⏳ NOT STARTED

- [ ] **7.1** CookieConsentBanner bUnit tests — rendering, accept/decline callbacks, equal button prominence, accessibility
- [ ] **7.2** JS interop contract tests — exact JS payload assertions for `InitAsync`, privacy-first defaults, no PostHog options for non-PostHog, consent method function names, non-PostHog no-ops
- [ ] **7.3** CookieConsentStateService tests — banner reopen event, cross-component bridge, state isolation
- [ ] **7.4** ICookieConsentInterop contract tests — correct JS function calls, cookie key parameter, `accepted`/`declined` values only, configurable lifetime

---

## Dependencies

```
Phase 5 (Hardening): 5.1 and 5.2 are independent. 5.3 depends on capabilities (already done). 5.4 depends on 5.1-5.3.
    ↓
Phase 6 (Documentation): Can start after 5.1-5.2 (cookie key + diagnostics documented)
Phase 7 (Testing Gaps): Independent of Phase 5-6 (tests existing behavior)
```

> **Recommended order:** 5.1 → 5.2 → 5.3 (parallel where possible) → 5.4 → 7.1-7.4 (parallel) → 6.1-6.3 (parallel)

## Effort Summary

| Phase | Tasks | Status | Effort | Key Complexity |
|-------|-------|--------|--------|----------------|
| Phase 1: Domain | 4 | ✅ Complete | S | — |
| Phase 2: Application | 5 | ✅ Complete | M | — |
| Phase 3: Browser/JS | 7 | ✅ Complete | L | — |
| Phase 4: Admin UI | 4 | ✅ Complete | L | — |
| Phase 5: Hardening | 4 | ⏳ Not Started | M | Stable cookie key, resolver diagnostics, command validation |
| Phase 6: Documentation | 3 | ⏳ Not Started | S | Kill switch boundary, SSR stance, cookie scope |
| Phase 7: Testing Gaps | 4 | ⏳ Not Started | M | Banner bUnit, JS interop contracts, state service |
| **Total** | **31** | **20 done / 11 remaining** | | |

## Acceptance Gates

### Gate 1: Resolver Correctness
- [x] 20 existing tests cover all provider paths, modes, kill switch, banner combinations
- [ ] Reason code assertions added (Phase 5.4)
- [ ] Stable cookie key tests added (Phase 5.4)

### Gate 2: Browser State Machine Correctness
- [x] 5 existing tests cover bootstrap, degradation, pageview
- [ ] Banner bUnit tests (Phase 7.1)
- [ ] JS interop contract tests (Phase 7.2)
- [ ] CookieConsentStateService tests (Phase 7.3)

## Quick Resume

**Next action:** Start Phase 5, Task 5.1 — change ConsentCookieKey derivation from mutable `TenantSlug` to stable tenant identifier in `AnalyticsRuntimeProfileResolver.cs`
