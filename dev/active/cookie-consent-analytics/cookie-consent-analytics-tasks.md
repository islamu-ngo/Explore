# Cookie Consent & Analytics Governance — Task Checklist

Last Updated: 2026-03-10 (Rev 3 — Post Architect Review)

---

## Phase 1: Domain Layer ⏳ NOT STARTED

- [ ] **1.1** Create analytics enums: `DeclineBehavior`, `PosthogCookielessMode`, `PosthogPersonProfiles`, `AnalyticsStorageProfile` in `Explore.Domain/Enums/Analytics/`
- [ ] **1.2** Create `AnalyticsProviderCapabilities` record in `Explore.Domain/Analytics/` with static factory `For(AnalyticsProviderEnum)` — capability flags per provider (RudderStack `SupportsCookielessMode = false` per Amendment 7)
- [ ] **1.3** Add 10 governance keys to `GovernanceSettingKeys.Analytics`: cookie_consent_enabled, decline_behavior, consent_cookie_lifetime_days, global_disable_client_tracking (System scope), posthog_cookieless_mode, posthog_person_profiles, posthog_session_replay, posthog_autocapture, posthog_heatmaps, posthog_toolbar
- [ ] **1.4** Add 10 `SettingDefinition` entries in `AnalyticsSettingDefinitions.cs` — privacy-first defaults: cookieless `on_reject`, person_profiles `identified_only`, features OFF, consent lifetime 180 days, decline `cookieless`, global_disable `false` at System scope

## Phase 2: Application Layer ⏳ NOT STARTED

- [ ] **2.1** Extend `AnalyticsSettingGroup` with 10 new typed properties — string settings parsed to domain enums with fallback defaults
- [ ] **2.2** Create `IAnalyticsRuntimeProfileResolver` + `AnalyticsRuntimeProfile` + `PosthogClientOptions` + `AnalyticsRuntimeProfileResolver` — the core policy engine (Amendment 2). Checks global kill switch first, uses `AnalyticsProviderCapabilities`, computes storage profile, consent cookie key, PostHog options
- [ ] **2.3** Create `AnalyticsConsentBootstrap` + `PosthogClientBootstrap` slim public DTOs — effective runtime config only, no admin inputs, no tenantSlug (only consentCookieKey), no PersonalApiKey (Amendment 3)
- [ ] **2.4** Update `GetPublicExperienceSettingsQueryHandler` — inject resolver, delegate all policy logic, map `AnalyticsRuntimeProfile` → `AnalyticsConsentBootstrap` DTO (no inline computation)
- [ ] **2.5** Register `IAnalyticsRuntimeProfileResolver` in DI

## Phase 3: Browser/JS Layer ⏳ NOT STARTED

- [ ] **3.1** Create `ConsentState` enum in `Explore.Blazor.Client/Analytics/` — 7 states: Uninitialized, NoBannerImmediateInit, BannerPendingCookieless, BannerPendingBlocked, Accepted, DeclinedCookieless, DeclinedDisabled (Amendment 4)
- [ ] **3.2** Create `CookieConsentBanner.razor` + `.razor.css` — fixed bottom, equal Accept/Decline, MudBlazor, BEM, accessible, non-blocking
- [ ] **3.3** Create `ICookieConsentInterop` + `CookieConsentInterop` + `cookie-consent.js` + server no-op — tenant-scoped cookie via `consentCookieKey`, configurable lifetime, values `accepted`|`declined` only
- [ ] **3.4** Update `analytics-bridge.js` — PostHog init with `cookieless_mode`, `person_profiles`, `defaults: '2026-01-30'`, feature controls + expose `optInCapturing`, `optOutCapturing`, `getExplicitConsentStatus` JS methods
- [ ] **3.5** Update `IAnalyticsInterop` + `AnalyticsInterop` + `ServerAnalyticsInterop` — pass `PosthogClientBootstrap` options, add consent methods, enum→JS string mapping at interop edge (Amendment 1)
- [ ] **3.6** Rewrite `AnalyticsInitializer.razor` as ConsentState machine — state drives rendering + analytics lifecycle, no double init, handles first pageview, navigation during pending, "Cookie Settings" re-entry, global kill switch (Amendment 4)
- [ ] **3.7** Add persistent "Cookie Settings" link in footer/privacy area — visible when `CookieBannerEnabled`, triggers state machine re-entry

## Phase 4: Admin UI ⏳ NOT STARTED

- [ ] **4.1** Create `InstanceAnalyticsSection.razor` + `.razor.css` — provider selector, PostHog privacy section (cookieless mode, person profiles, features), cookie consent section (toggle, decline, lifetime), contextual warnings per storage profile, advisory auto-suggestions (Amendment 5), global kill switch (Amendment 6)
- [ ] **4.2** Add "Analytics & Privacy" to `InstanceAdminSettingsLayout.razor` sidebar navigation
- [ ] **4.3** Create/extend settings model with all analytics properties (typed enums)
- [ ] **4.4** Wire save/load logic for analytics governance settings via API

## Phase 5: Documentation ⏳ NOT STARTED

- [ ] **5.1** Update `docs/CONFIGURATION.md` — 10 new governance keys, storage-mode consent, provider capabilities, global kill switch
- [ ] **5.2** Update `docs/BLAZOR.md` — consent state machine, three modes, PostHog consent integration, "Cookie Settings" link
- [ ] **5.3** Update `docs/OPERATIONS.md` — provider table with storage mode + consent columns, global kill switch, RudderStack deferred parity

## Phase 6: Testing ⏳ NOT STARTED

- [ ] **6.1** Domain unit tests — 10 governance keys, 4 enums, `AnalyticsProviderCapabilities.For()` per provider
- [ ] **6.2** **Runtime profile resolver tests** (KEY test class) — global kill switch, analytics disabled, PostHog always/on_reject/off, Plausible, Rybbit, RudderStack, None, consent cookie key computation, PosthogClientOptions only for PostHog, admin override
- [ ] **6.3** AnalyticsSettingGroup tests — 10 new properties, enum parsing with fallback defaults
- [ ] **6.4** Query handler tests — delegates to resolver, maps correctly, no inline policy logic
- [ ] **6.5** JS interop contract tests — exact JS payload assertions, privacy-first defaults, no PostHog options for non-PostHog, consent method JS function names
- [ ] **6.6** AnalyticsInitializer state machine tests — all 7 states reachable, transitions correct, no double init, first pageview preserved, navigation during pending, "Cookie Settings" re-entry, global kill switch
- [ ] **6.7** CookieConsentBanner tests — rendering, accept/decline, tenant-scoped cookie key, configurable lifetime
- [ ] **6.8** PostHog consent method tests — OptIn/OptOut call correct JS, GetExplicitConsentStatus returns state, non-PostHog no-op

---

## Dependencies

```
Phase 1 (Domain: enums, capabilities, keys, definitions)
    ↓
Phase 2 (Application: setting group, resolver, slim DTO, handler, DI)
    ↓
Phase 3 (Browser/JS: state machine, banner, cookie interop, JS bridge, analytics interop, initializer, footer link)
Phase 4 (Admin UI: analytics section, layout, model, save/load)
    ↓
Phase 5 (Documentation)
Phase 6 (Testing — can start per-phase as each phase completes)
```

## Effort Summary

| Phase | Tasks | Effort | Key Complexity |
|-------|-------|--------|----------------|
| Phase 1: Domain | 4 | S | Enums + capability matrix + 10 keys |
| Phase 2: Application | 5 | M | `AnalyticsRuntimeProfileResolver` is the core policy engine |
| Phase 3: Browser/JS | 7 | L | ConsentState machine in AnalyticsInitializer |
| Phase 4: Admin UI | 4 | L | Storage-profile-aware section + advisory auto-computation |
| Phase 5: Documentation | 3 | S | Straightforward updates |
| Phase 6: Testing | 8 | L | Resolver tests + state machine tests + JS contract tests |
| **Total** | **31** | | |

## Rev 3 Changes from Rev 2

| Area | Rev 2 | Rev 3 |
|------|-------|-------|
| Policy contracts | Stringly-typed (`"cookieless"`, `"on_reject"`) | **Typed enums** (Amendment 1) |
| Policy computation | Inside query handler | **Dedicated `IAnalyticsRuntimeProfileResolver`** (Amendment 2) |
| Public DTO | Flat with admin inputs exposed | **Slim `AnalyticsConsentBootstrap`** (Amendment 3) |
| Consent flow | Imperative if/else in component | **7-state `ConsentState` machine** (Amendment 4) |
| Admin auto-computation | Silent mutation | **Advisory hints** (Amendment 5) |
| Emergency stop | None | **`analytics.global_disable_client_tracking`** (Amendment 6) |
| RudderStack | Implicit "future" | **Explicit deferred parity** (Amendment 7) |
| Provider knowledge | Scattered in plan text | **`AnalyticsProviderCapabilities`** first-class record |
| Governance keys | 9 | **10** (+global kill switch) |
| Total tasks | 27 | **31** |

## Quick Resume

**Next action:** Start Phase 1, Task 1.1 — create analytics enums in `Explore.Domain/Enums/Analytics/`