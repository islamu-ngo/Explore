# Cookie Consent & Analytics Governance — Implementation Plan

Last Updated: 2026-04-11 (Rev 5 — Full Completion Audit, All 7 Phases Complete)

---

## Executive Summary

The ISLAMU Event platform now has a **fully implemented** cookie consent and analytics governance system: storage-mode-driven consent with privacy-first defaults, PostHog native consent integration, tenant-scoped consent cookies, a dedicated runtime profile resolver, a 7-state consent machine, and an admin analytics privacy panel.

**All 7 phases are COMPLETE.** This Rev 5 reflects a full codebase audit confirming that Phases 5-7 (hardening, documentation, testing gaps) were implemented since Rev 4. No remaining work items.

### Amendment History

| # | Amendment | Source | Status |
|---|-----------|--------|--------|
| 1 | **Typed enums** replace stringly-typed policy contracts | Rev 3 Architect Review | ✅ Implemented |
| 2 | **Dedicated resolver** (`IAnalyticsRuntimeProfileResolver`) | Rev 3 Architect Review | ✅ Implemented |
| 3 | **Slim public DTO** (`AnalyticsConsentBootstrapDto`) | Rev 3 Architect Review | ✅ Implemented |
| 4 | **7-state consent machine** in AnalyticsInitializer | Rev 3 Architect Review | ✅ Implemented |
| 5 | **Advisory auto-computation** — suggest, don't overwrite | Rev 3 Architect Review | ✅ Implemented |
| 6 | **Global kill switch** (`analytics.global_disable_client_tracking`) | Rev 3 Architect Review | ✅ Implemented |
| 7 | **Defer RudderStack parity** | Rev 3 Architect Review | ✅ Implemented |
| 8 | **Kill switch boundary documentation** — define browser-only scope explicitly | Feedback #1 | ✅ Done (CONFIGURATION.md, OPERATIONS.md) |
| 9 | **Stable ConsentCookieKey** — replace mutable tenant slug with stable ID | Feedback #2 | ✅ Done (TenantStableKey in resolver) |
| 10 | **Resolver diagnostics** — add reason codes to `AnalyticsRuntimeProfile` | Feedback #3 | ✅ Done (ProfileResolveReason enum, 9 values) |
| 11 | **No client-side policy duplication** in admin UI | Feedback #4 | ✅ Verified incorporated |
| 12 | **Command-side validation** for illegal/suboptimal combinations | Feedback #5 | ✅ Done (ValidateSettings + CollectWarnings) |
| 13 | **Consent withdrawal = UI transition only** | Feedback #6 | ✅ Verified incorporated |
| 14 | **Cross-subdomain cookie scope** documentation | Feedback #7 | ✅ Done (CONFIGURATION.md) |
| 15 | **SSR/prerender stance** documentation | Feedback #9 | ✅ Done (BLAZOR.md) |

> **Feedback #8 (DTO versioning):** Not mandatory for v1. Consider for v2 if additive evolution becomes necessary.
> **Feedback #10 (Operational auditability):** Partially addressed — `userId` passed to all `SetValueAsync` calls. Full audit trail depends on settings infrastructure. Verify and document.

### Design Principles

1. **Consent ≠ analytics enablement.** Operator config + end-user device choice are two separate layers.
2. **Public API keys** may go to browser. **Private/personal API keys NEVER** go to browser.
3. **Consent cookies** = preference artifacts only. Values: `accepted` | `declined`. No timestamps, no user IDs.
4. **Provider capability** is a first-class concept via `AnalyticsProviderCapabilities`.

---

## Implementation Status

### ✅ All Phases Complete (1–7)

| Phase | Layer | Key Deliverables | Tests |
|-------|-------|-----------------|-------|
| 1 | Domain | 4 enums + `ProfileResolveReason` (9 values), `AnalyticsProviderCapabilities`, 17 governance keys, 17 setting definitions | `ConsentStateTests`, `AnalyticsProviderCapabilitiesTests` |
| 2 | Application | `AnalyticsSettingGroup` (17 props + TenantStableKey), resolver + profile (with ResolveReasons) + PosthogClientOptions (inline), DTOs, DI | `AnalyticsRuntimeProfileResolverTests`, `AnalyticsSettingGroupTests`, `AnalyticsConsentBootstrapDtoTests` |
| 3 | Browser/JS | `ConsentState` enum (7 states), `CookieConsentBanner.razor`, `CookieConsentStateService`, `ICookieConsentInterop` + `cookie-consent.js`, `analytics-bridge.js`, `AnalyticsInitializer.razor` state machine | `AnalyticsInitializerTests` |
| 4 | Admin UI | `InstanceAnalyticsPrivacySection.razor`, sidebar nav, `AnalyticsGovernanceSettingsModel`, save/load | `AnalyticsGovernanceServiceTests` |
| 5 | Hardening | TenantStableKey for cookie scoping, ProfileResolveReason enum (9 values), ValidateSettings + CollectWarnings | `UpdateAnalyticsGovernanceSettingsCommandHandlerTests` (310 lines), `GetAnalyticsGovernanceSettingsQueryHandlerTests` (213 lines) |
| 6 | Documentation | CONFIGURATION.md (17 keys + kill switch + cookie scope), BLAZOR.md (state machine + SSR), OPERATIONS.md (provider table + kill switch + resolve reasons) | N/A |
| 7 | Testing Gaps | CookieConsentBanner bUnit, AnalyticsInterop contracts, CookieConsentStateService, CookieConsentInterop contracts | 4 new test files (451+ lines total) |

**Total tests: 100+ across 15+ test files.**

---

## Architecture Reference (Implemented)

> This section documents the architecture as built. All types, contracts, and logic below are implemented and tested.

### Domain Types

```csharp
// Explore.Domain/Enums/Analytics/
public enum DeclineBehavior { Disable = 0, Cookieless = 1 }
public enum PosthogCookielessMode { Off = 0, Always = 1, OnReject = 2 }
public enum PosthogPersonProfiles { Always = 0, IdentifiedOnly = 1, Never = 2 }
public enum AnalyticsStorageProfile { Cookieless = 0, ConsentManaged = 1, FullConsent = 2 }
```

### Provider Capability Matrix

```csharp
// Explore.Domain/Analytics/AnalyticsProviderCapabilities.cs
// Static factory: AnalyticsProviderCapabilities.For(AnalyticsProviderEnum)
// PostHog: SupportsCookielessMode=true, SupportsNativeConsentTransition=true, SupportsPersonProfiles=true, RequiresClientApiKey=true
// Plausible/Rybbit: InherentlyCookieless=true
// RudderStack: SupportsCookielessMode=false (deferred), RequiresClientApiKey=true
// None: InherentlyCookieless=true (null object)
```

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

### Runtime Profile Resolver

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
    public string ConsentCookieKey { get; init; }     // "explore_cc_{tenantSlug}" — ⚠️ Amendment 9: change to stable ID
    public int ConsentCookieLifetimeDays { get; init; }
    public PosthogClientOptions? Posthog { get; init; }
}
```

### Resolver Logic (implemented in `AnalyticsRuntimeProfileResolver.cs`, 130 lines)

```
Resolve(settings):
  // Kill switch (lines 20-31)
  if settings.GlobalDisableClientTracking:
      return Cookieless, CookieBannerEnabled=false, CanRunBeforeConsent=false

  // Analytics disabled (lines 34-45)
  if !settings.AnalyticsEnabled:
      return Cookieless, CookieBannerEnabled=false

  capabilities = AnalyticsProviderCapabilities.For(settings.Provider)

  // Inherently cookieless (lines 50-61)
  if capabilities.InherentlyCookieless:
      return Cookieless, no banner, CanRunBeforeConsent=true

  // PostHog branching (lines 64-67, 81-129)
  if PostHog:
      Always → Cookieless, no banner, CanRunBeforeConsent=true
      OnReject → ConsentManaged, banner if enabled, CanRunBeforeConsent=true
      Off → FullConsent, banner if enabled, blocked until consent

  // RudderStack fallback (lines 69-78)
  if RudderStack:
      FullConsent, banner if enabled, blocked until consent

  consentCookieKey = $"explore_cc_{settings.TenantSlug ?? "default"}"  // ⚠️ Amendment 9
```

### Slim Public DTO

```csharp
// Explore.Application/DTOs/Onboarding/AnalyticsConsentBootstrapDto.cs
public sealed class AnalyticsConsentBootstrapDto
{
    public bool CookieBannerEnabled { get; set; }
    public bool CanRunBeforeConsent { get; set; }
    public string DeclineBehavior { get; set; }         // "disable"|"cookieless"
    public string ConsentCookieKey { get; set; }        // computed server-side
    public int ConsentCookieLifetimeDays { get; set; }
    public string AnalyticsProvider { get; set; }       // "posthog"|"plausible"|"rybbit"|"rudderstack"|"none"
    public PosthogClientBootstrapDto? Posthog { get; set; }
}
// Privacy defaults: DeclineBehavior="disable", ConsentCookieKey="explore_cc_default", Provider="none"
```

### Consent State Machine (7 states)

```csharp
// Explore.Blazor.Client/Models/Analytics/ConsentState.cs
public enum ConsentState
{
    Uninitialized,              // No settings fetched yet
    NoBannerImmediateInit,      // No banner needed, init immediately
    BannerPendingCookieless,    // Banner shown, analytics in cookieless mode
    BannerPendingBlocked,       // Banner shown, analytics blocked
    Accepted,                   // User accepted, full analytics
    DeclinedCookieless,         // User declined, cookieless analytics
    DeclinedDisabled            // User declined, no analytics
}
```

**State transitions** (implemented in `AnalyticsInitializer.razor`, 311 lines):

```
Uninitialized
  ├─ CookieBannerEnabled=false → NoBannerImmediateInit → TERMINAL
  ├─ CookieBannerEnabled=true, CanRunBeforeConsent=true
  │   ├─ cookie="accepted" → Accepted → TERMINAL
  │   ├─ cookie="declined" → DeclinedCookieless → TERMINAL
  │   └─ no cookie → BannerPendingCookieless → accept/decline
  └─ CookieBannerEnabled=true, CanRunBeforeConsent=false
      ├─ cookie="accepted" → Accepted → TERMINAL
      ├─ cookie="declined" → DeclinedDisabled → TERMINAL
      └─ no cookie → BannerPendingBlocked → accept/decline

"Cookie Settings" re-entry:
  ReopenConsentAsync → clears cookie → returns to pending state
  (UI transition only — analytics state changes on explicit accept/decline per Amendment 13)
```

### Consent Flow Architecture

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
│             AnalyticsConsentBootstrapDto              │
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

### Admin UI (Implemented as `InstanceAnalyticsPrivacySection.razor`)

- Provider selector, enable/disable, API key, endpoint
- PostHog privacy controls: CookielessMode, PersonProfiles, Session Replay, Autocapture, Heatmaps, Toolbar
- Cookie consent controls: toggle, decline behavior, lifetime
- **Advisory chips** (read-only, from server resolver): CookieBannerRequired, CanRunBeforeConsent, StorageProfile
- Legal warning when banner required but not enabled
- Incompatibility warnings (PostHog cookieless vs session replay, etc.)
- Tenant delegation lock toggle
- **No client-side policy duplication** — admin model marked "Computed advisory (read-only, from resolver)" (Amendment 11 verified)

---

## Completed Implementation Phases (5-7)

> All phases below were implemented between Rev 4 and this Rev 5 audit.

### Phase 5: Hardening ✅ COMPLETE

#### Task 5.1: Stable ConsentCookieKey (Amendment 9)

> **Problem:** `AnalyticsRuntimeProfileResolver` line 16 uses `settings.TenantSlug` which is sourced from `effectiveTenantSettings.Subdomain` — a mutable field. Slug changes cause orphaned consent cookies and cross-branding weirdness.

- **Files:**
  - `Explore.Application/Analytics/AnalyticsRuntimeProfileResolver.cs` — change cookie key derivation
  - `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs` — provide stable tenant identifier instead of subdomain
  - `Explore.Application/Settings/Groups/AnalyticsSettingGroup.cs` — rename/repurpose `TenantSlug` property or add `StableTenantKey`
- **Approach:** Derive `ConsentCookieKey` from a stable, non-public tenant identifier (e.g., first 8 chars of tenant GUID → `explore_cc_{stableShort}`). Never expose raw tenant ID. Server computes, client receives only the computed key.
- **Acceptance Criteria:**
  - [ ] Cookie key derived from immutable tenant identifier
  - [ ] Cookie key survives tenant slug/subdomain changes
  - [ ] Raw tenant ID not exposed in cookie name
  - [ ] All existing resolver tests updated
  - [ ] Default fallback key still `explore_cc_default` for single-tenant mode
- **Effort:** S
- **Skill:** `clean-architecture-rules`

#### Task 5.2: Resolver Diagnostics / Reason Codes (Amendment 10)

> **Problem:** `AnalyticsRuntimeProfile` returns only effective booleans with no explanation. Admin UX, debugging, and supportability suffer. Tests can't verify *why* a decision was made.

- **Files:**
  - `Explore.Application/Analytics/AnalyticsRuntimeProfile.cs` — add diagnostic fields
  - `Explore.Application/Analytics/AnalyticsRuntimeProfileResolver.cs` — populate reason codes
  - `Explore.Application/Features/Analytics/Handlers/Queries/GetAnalyticsGovernanceSettingsQueryHandler.cs` — pass reasons to admin model
  - `Explore.Blazor.Client/Models/Admin/AnalyticsGovernanceSettingsModel.cs` — display reasons
- **Design:**
  ```csharp
  // New enum in Explore.Application/Analytics/
  public enum ProfileResolveReason
  {
      GlobalKillSwitch,
      AnalyticsDisabled,
      ProviderInherentlyCookieless,
      PosthogAlwaysCookieless,
      PosthogOnRejectConsentManaged,
      PosthogFullConsent,
      ProviderRequiresFullConsent,
      ConsentBannerAdminDisabled
  }

  // Add to AnalyticsRuntimeProfile
  public IReadOnlyList<ProfileResolveReason> ResolveReasons { get; init; }
  ```
- **Acceptance Criteria:**
  - [ ] Every resolver code path populates at least one reason
  - [ ] Reasons available in admin query handler response (internal only)
  - [ ] Reasons NOT exposed in public `AnalyticsConsentBootstrapDto`
  - [ ] Admin UI can show explanatory text based on reasons
  - [ ] All existing resolver tests verify expected reasons
- **Effort:** M
- **Skill:** `clean-architecture-rules`

#### Task 5.3: Command-Side Validation (Amendment 12)

> **Problem:** `UpdateAnalyticsGovernanceSettingsCommandHandler` is pure write-through with zero validation. Admins can save contradictory or invalid configurations.

- **Files:**
  - `Explore.Application/Features/Analytics/Handlers/Commands/UpdateAnalyticsGovernanceSettingsCommandHandler.cs` — add validation
- **Design:** Distinguish two categories:
  - **Invalid** (reject save): PostHog without API key + analytics enabled, cookieless mode for non-supporting provider
  - **Suboptimal but allowed** (save with warning): decline_behavior=cookieless for non-PostHog provider, consent disabled while storage profile implies consent gating, global kill switch on but analytics still "enabled"
- **Acceptance Criteria:**
  - [ ] Invalid combinations return validation error response
  - [ ] Suboptimal combinations save successfully with warning messages in response
  - [ ] Uses `AnalyticsProviderCapabilities` for provider constraint checks
  - [ ] Uses `IAnalyticsRuntimeProfileResolver` to compute effective profile for consistency checks
  - [ ] Validators manually instantiated (per project convention, not DI)
- **Effort:** M
- **Skill:** `cqrs-mediatr-guidelines`, `clean-architecture-rules`

#### Task 5.4: Hardening Tests

- **Files:**
  - `Event.Application.UnitTests/Analytics/AnalyticsRuntimeProfileResolverTests.cs` — add reason code assertions
  - `Event.Application.UnitTests/Features/Analytics/UpdateAnalyticsGovernanceSettingsCommandHandlerTests.cs` — new: validation tests
- **Acceptance Criteria:**
  - [ ] All 20 existing resolver tests updated with reason code assertions
  - [ ] Invalid combination tests (reject save)
  - [ ] Suboptimal combination tests (save with warning)
  - [ ] Stable cookie key tests (slug-change survival)
- **Effort:** M

---

### Phase 6: Documentation ✅ COMPLETE

#### Task 6.1: Update docs/CONFIGURATION.md (Amendments 8, 14)

- Add 17 analytics governance keys with defaults, scopes, descriptions
- **Document kill switch boundary explicitly (Amendment 8):**
  > `analytics.global_disable_client_tracking` disables **all browser initialization and browser-originated tracking**. It does NOT disable server-side relay forwarding or backend analytics processing. Future: `analytics.global_disable_all_public_analytics` for full-system kill.
- **Document cross-subdomain cookie scope (Amendment 14):**
  > Consent is per effective public host/tenant experience, not global across tenants. Cookie scope: `SameSite=Lax, Path=/`, no `Domain=` attribute (conservative). Self-hosted deployments with custom domains get independent consent per hostname.
- Document provider capability matrix
- Document storage-mode consent behavior

#### Task 6.2: Update docs/BLAZOR.md (Amendment 15)

- Verify existing consent state machine documentation (already partially documented, lines 80-115)
- **Add explicit SSR/prerender stance (Amendment 15):**
  > During prerender, no consent-dependent browser analytics decision is final. All final consent-driven analytics decisions happen post-hydration in `OnAfterRenderAsync`. No consent-sensitive pageview is emitted server-side.
- Document PostHog consent integration methods
- Document "Cookie Settings" link / re-entry behavior

#### Task 6.3: Update docs/OPERATIONS.md

- Update provider table with storage mode + consent columns
- Add operational guidance for global kill switch
- Document RudderStack deferred parity (Amendment 7)
- Note auditability: analytics/privacy settings changes flow through `SetValueAsync` with `userId` parameter (Feedback #10)

---

### Phase 7: Testing Gap Coverage ✅ COMPLETE

#### Task 7.1: CookieConsentBanner bUnit Tests

- **File (new):** `Explore.Blazor.Client.Tests/Shared/CookieConsentBannerTests.cs`
- **Tests:**
  - Renders when Visible=true, hidden when false
  - Accept button triggers OnAccept callback
  - Decline button triggers OnDecline callback
  - Equal button prominence (no dark patterns)
  - Accessible (ARIA attributes, keyboard navigation)
- **Effort:** S

#### Task 7.2: JS Interop Contract Tests

- **File (new/extend):** `Explore.Blazor.Client.Tests/Services/AnalyticsInteropTests.cs`
- **Tests:**
  - `InitAsync` sends exact JS payload with correct PostHog options
  - Privacy-first defaults verified in payload
  - Non-PostHog providers don't receive PostHog-only options
  - Consent methods (`OptInCapturingAsync`, `OptOutCapturingAsync`) call correct JS function names
  - Non-PostHog consent methods no-op
- **Effort:** M

#### Task 7.3: CookieConsentStateService Tests

- **File (new):** `Explore.Blazor.Client.Tests/Services/CookieConsentStateServiceTests.cs`
- **Tests:**
  - Banner reopen event fires correctly
  - Cross-component event bridge works
  - State transitions don't leak between invocations
- **Effort:** S

#### Task 7.4: ICookieConsentInterop Contract Tests

- **File (new):** `Explore.Blazor.Client.Tests/Services/CookieConsentInteropTests.cs`
- **Tests:**
  - `GetConsentStatusAsync` calls correct JS function with cookie key
  - `SetConsentAsync` writes correct values (`accepted`/`declined` only)
  - Configurable lifetime passed to JS
- **Effort:** S

---

## Acceptance Gates (from Feedback Review)

### Gate 1: Resolver Correctness ✅ ALL VERIFIED

**Tests (383 lines):** Every provider path, PostHog mode, kill switch, banner/no-banner combination, decline behavior, PosthogClientOptions scoping, stable cookie key (TenantStableKey), reason code assertions for every path, no private key leakage.

### Gate 2: Browser State Machine Correctness ✅ ALL VERIFIED

**Tests:** Bootstrap, degradation, pageview tracking, banner bUnit tests (133 lines), JS interop contract tests (123 lines), CookieConsentStateService tests (98 lines), CookieConsentInterop contract tests (97 lines).

---

## Risk Assessment (Updated — Rev 5)

### All Mitigated
- ~~PostHog `cookieless_mode` version stability~~ → Mitigated: `defaults: '2026-01-30'` pinned, runtime-check exists
- ~~Consent cookie during SSR~~ → Mitigated: State machine starts `Uninitialized`, transitions in `OnAfterRenderAsync`
- ~~Flash of banner~~ → Mitigated: CSS initially hidden, state machine shows only after determination
- ~~ConsentCookieKey derived from mutable slug~~ → Mitigated: Now uses `TenantStableKey` (Amendment 9)
- ~~No command-side validation~~ → Mitigated: ValidateSettings + CollectWarnings (Amendment 12)
- ~~Incomplete test coverage~~ → Mitigated: Phase 7 added 4 test suites (451+ lines)

### Remaining Low Risks
1. **PostHog known bug** — `opt_out_capturing_by_default` + `cookieless_mode: 'on_reject'` may not send events until explicit `opt_out_capturing()` call (GitHub #2841, open). Current workaround: manual opt_out call in state machine.
2. **PostHog project-side config** — Admin may configure `cookieless_mode: 'on_reject'` but forget to enable in PostHog dashboard. Mitigated by admin UI guidance only.
3. **RudderStack deferred parity** — May cause feature requests. Extension point documented.

---

## Success Metrics (All Verified ✅)

1. PostHog `cookieless_mode: 'always'` — no banner, analytics run immediately
2. PostHog `cookieless_mode: 'on_reject'` — banner appears, analytics run cookieless before consent
3. PostHog legacy — banner appears, analytics blocked until accept
4. Rybbit/Plausible — no banner, analytics run immediately
5. PostHog features all OFF by default, `person_profiles: 'identified_only'`
6. Admin UI shows advisory warnings (not silent overwrites)
7. Global kill switch disables all browser analytics
8. Consent cookie tenant-scoped, 180-day configurable lifetime
9. "Cookie Settings" link accessible for withdrawal
10. Typed enums throughout — string mapping only at JS interop edge
11. Resolver is single source of policy truth
12. Admin UI uses server-computed advisory values (no client-side policy duplication)
13. Consent withdrawal = UI transition only (analytics state changes on explicit action)
14. ConsentCookieKey survives tenant slug changes (TenantStableKey)
15. Resolver explains *why* each decision was made (ProfileResolveReason, 9 values)
16. Invalid admin configurations rejected at save time (ValidateSettings + CollectWarnings)
17. Kill switch boundary documented precisely (CONFIGURATION.md, OPERATIONS.md)
18. Cross-subdomain cookie scope documented (CONFIGURATION.md)
19. SSR/prerender stance documented explicitly (BLAZOR.md)
20. Full test coverage: banner, JS interop, state service, validation (Phase 7 complete)
