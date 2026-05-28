# Blazor Localization — Implementation Plan (Enterprise-Grade)

Last Updated: 2026-04-11

## Executive Summary

The ISLAMU Event platform ships with a **dual-provider translation abstraction** (Tolgee ↔ Weblate ↔ Offline ↔ Null) that lets every self-hoster pick how their instance sources translations. The abstraction, backend providers, REST API, governance settings, and a first-draft Blazor client layer are **already implemented**. This plan takes that first draft to **enterprise-grade quality**: it closes concrete gaps, adds the missing **Localization Admin UI**, wires the user-facing **language picker + RTL** through WCAG 2.2 AA accessibility services, plugs in native .NET culture formatting and MudBlazor's built-in localizer, and hardens the TMS HTTP clients with resilience, observability, and tests.

The north star is threefold:

1. **Dual-provider abstraction as the product surface** — instance admins pick **Tolgee** OR **Weblate** (both self-hostable) from a Blazor admin UI, or stay fully offline with bundled JSON files. The backend already routes through `RuntimeTranslationProvider`; this plan makes that choice visible, testable, and operable.
2. **Offline bundles as Tier 1 default** — every self-hoster gets working translations shipped inside the DLL (`Explore.Infrastructure.dll` embeds `Localization/Bundles/*.json`). No TMS = no infrastructure = still localized. Connected TMS is an upgrade path, not a precondition.
3. **UI language picker** — users pick their language from a MudBlazor picker in the navbar. Selection persists via BFF cookie, activates RTL for Arabic, drives native .NET `CultureInfo`, translates MudBlazor's internal strings, and survives Server↔WASM hydration without flicker.

Everything must obey the repo's Clean Architecture + CQRS + BFF conventions, hit WCAG 2.2 AA, emit Prometheus + Loki observability, use Polly resilience, and carry test coverage equal to the rest of the codebase.

---

## Current State (audited 2026-04-11)

This plan's previous revision (2026-03-26) asserted *"Blazor — Nothing"*. That was **incorrect**. A careful audit of the repository shows the following reality:

### Backend Localization Stack — Complete

| Layer | File(s) | Status |
|---|---|---|
| Abstraction contracts | `Explore.Application/Contracts/Infrastructure/ITranslationManagementProvider.cs`, `ITranslationResolver.cs`, `ITranslationConfigResolver.cs` | Complete |
| Runtime routing | `Explore.Infrastructure/Localization/RuntimeTranslationProvider.cs` (Scoped, graceful fallback to Offline on error) | Complete |
| Tolgee provider | `Explore.Infrastructure/Localization/TolgeeTranslationProvider.cs` (`X-API-Key`, 10s timeout, converts nested→flat) | Complete |
| Weblate provider | `Explore.Infrastructure/Localization/WeblateTranslationProvider.cs` (`Authorization: Token`, 10s timeout) | Complete |
| Offline provider | `Explore.Infrastructure/Localization/OfflineTranslationProvider.cs` (Singleton, `ConcurrentDictionary`, assembly manifest scanning) | Complete |
| Null provider | `Explore.Infrastructure/Localization/NullTranslationProvider.cs` (no-op safe fallback) | Complete |
| Config resolver | `Explore.Infrastructure/Localization/TranslationConfigResolver.cs` (5-min cache keyed by `TranslationConfig:{tenantId}`) | Complete |
| Translation resolver | `Explore.Infrastructure/Localization/TranslationResolver.cs` (30-min live / 24h offline, preloads full language) | Complete |
| Public API | `Explore.API/Controllers/TranslationController.cs` → `GET /api/translation/{lang}`, `GET /api/translation/languages` (AllowAnonymous) | Complete |
| Admin API | `Explore.API/Controllers/LocalizationAdminController.cs` → `POST test-connection`, `GET configuration`, `POST export-from-tms` (Authorize) | Complete with one gap (see below) |
| CQRS | `Explore.Application/Features/Localization/{Requests,Handlers}/{Queries,Commands}/*.cs` | Complete |
| DTO | `Explore.Application/DTOs/Localization/LocalizationConfigDto.cs` | Complete |
| Governance seeds | `Explore.Persistence/Seed/LookupTableSeeder.cs` lines 350-355 (IDs 560-564: `localization.default_language`, `tms_provider`, `tms_api_url`, `tms_project_id`, `tms_component`) | Complete |
| Enum | `Explore.Domain/Enums/TranslationManagementProviderEnum.cs` (None=0, Tolgee=1, Weblate=2) | Complete |
| Offline bundles | `Explore.Infrastructure/Localization/Bundles/{en,fr,ar}.json` | **Present but EMPTY (`{}`)** |
| Unit tests | `Event.Application.UnitTests/Infrastructure/Localization/*.cs` | Present (5 files) |

### Blazor Client Stack — First Draft Exists

| Layer | File | Status |
|---|---|---|
| Language model | `Explore.Blazor.Client/Models/LanguageContext.cs` (91 lines, 11 hardcoded languages + flags + names + RTL set) | Exists, hardcoded |
| Translation contract | `Explore.Blazor.Client/Contracts/Services/ITranslationService.cs` (46 lines) | Exists |
| Translation service | `Explore.Blazor.Client/Services/TranslationService.cs` (142 lines, `SemaphoreSlim`+`CacheEntry<T>`, 30-min TTL, calls `_apiClient.TranslationAsync`) | Exists |
| Cascading provider | `Explore.Blazor.Client/Providers/LanguageProvider.razor` (84 lines, reads `lang` cookie via JS, calls `setDirection`) | Exists, has quality gaps |
| Language picker | `Explore.Blazor.Client/Shared/LanguagePicker.razor` (89 lines, MudMenu with flag) | Exists, has a11y gaps |
| JS interop | `Explore.Blazor.Client/wwwroot/js/localization.js` (23 lines, global `window.localization`) | Exists |
| Server SSR lang/dir | `Explore.Blazor/Components/App.razor` lines 40-70 (reads `lang` cookie → sets `<html lang dir>`) | Exists, hardcoded RTL list |
| BFF language endpoint | `Explore.Blazor/Extensions/BffPreferenceEndpoints.cs` lines 105-126 → `POST /bff/language?lang=xx` (cookie only, NOT persisted to API) | Exists, incomplete persistence |
| BFF direction endpoint | Same file lines 152-195 → `POST /bff/direction?dir=auto|ltr|rtl` (persists to `PUT api/user/appearance`) | Complete |
| Layout RTL | `Explore.Blazor.Client/Layout/MainLayout.razor` → `<MudRTLProvider RightToLeft="@_isRtl">` already wrapping content | Wired (needs `_isRtl` fed from `LanguageContext`) |
| Picker placement | `Explore.Blazor.Client/Layout/NavMenu.razor` line 90 → `<LanguagePicker />` in `.navbar__actions` | Placed |
| DI registration | `Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs` line 78 → `services.AddScoped<ITranslationService, TranslationService>()` | Registered |
| Router wrap | `Explore.Blazor.Client/Routes.razor` lines 26-41 → `<LanguageProvider>...</LanguageProvider>` wraps router | Wired |
| Tests | `Explore.Blazor.Client.Tests/Services/TranslationServiceTests.cs`, `Components/LanguagePickerTests.cs`, `LanguagePicker.razor.css` | **NONE exist** |

### Supporting Infrastructure — Already Available

| Concern | Artifact | Status |
|---|---|---|
| Accessibility announce | `Explore.Blazor.Client/Contracts/Services/Accessibility/IAccessibilityAnnouncerService.cs` (`AnnouncePoliteAsync`, `AnnounceAssertiveAsync`) | Available (not used by picker yet) |
| Accessibility focus | `IAccessibilityFocusService` (Save/Restore/Focus helpers) | Available |
| Observability skill | `.claude/skills/error-tracking/SKILL.md` (Prometheus + Loki patterns) | Available |
| WCAG docs | `docs/ACCESSIBILITY.md` (WCAG 2.2 AA target, CSS logical-properties ban on physical properties) | Published |
| Appearance DTO | `Explore.Application/DTOs/Appearance/UpdateUserAppearancePreferencesDto.cs` → `{ ThemeMode, Direction }` (no `Language` field yet) | Needs extension |
| Admin layout | `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAdminSettingsLayout.razor` (MudGrid xs=3 sidebar + xs=9 content sections switch) | Available for admin UI docking |
| NSwag generation | `Explore.Blazor.Client/nswag.json` → input `../schemas/openapi.json`, runs before `CoreCompile` | Complete (translation endpoints already present in `_apiClient.TranslationAsync` and `_apiClient.LanguagesAsync`) |

### Confirmed Gaps (Priority-Sorted)

**P0 — Blocker**

1. **Empty starter bundles.** `en.json`, `fr.json`, `ar.json` are literally `{}`. A self-hoster who leaves `localization.tms_provider = none` sees NO translations. Either populate them with the core UI key set, or auto-seed from the first TMS export (see Phase 5).
2. **`ExportFromTmsCommandHandler` does not persist to bundle files.** It counts exports, logs, and returns. The injected `ITranslationResolver` dependency is never called. This makes "Export from TMS" a broken promise — the admin clicks, hears success, but the offline fallback is unchanged.

**P1 — Biggest Feature Gaps**

3. **No Localization Admin UI.** Instance admins cannot pick Tolgee vs Weblate, test the connection, enter API URL/project/component, or trigger an export from the Blazor shell. The backend API is there; the UI is not.
4. **`/bff/language` does not persist to API for authenticated users.** `/bff/direction` persists via `PUT api/user/appearance`; `/bff/language` only sets a cookie. Authenticated users lose their language preference between devices.
5. **`UpdateUserAppearancePreferencesDto`** has `ThemeMode` + `Direction` only. Needs a `Language` field and matching handler in the API.
6. **MudBlazor `MudLocalizer` is not wired.** MudBlazor emits its own strings (DataGrid headers, Pagination labels, Dialog OK/Cancel, date picker). They stay in English regardless of user language unless a custom `MudLocalizer` is registered.
7. **Zero tests** for `TranslationService`, `LanguagePicker`, `LanguageProvider`.

**P2 — Accessibility & Code Quality**

8. **Picker button has no `aria-label`.** WCAG 2.1.1 violation (icon-only button). `IAccessibilityAnnouncerService` exists but is not called.
9. **No CSS isolation** for `LanguagePicker` (`.razor.css` file missing).
10. **`async void HandleLanguageChanged`** in `LanguageProvider` — should be `async Task` wrapped via `InvokeAsync`.
11. **`LanguageProvider` does not implement `IDisposable`** interface (has a `Dispose()` method but no interface declaration → unsubscription may not fire).
12. **Hardcoded RTL list duplicated** in `App.razor` (`_rtlLanguages`) AND `LanguageContext.cs` (`RtlLanguages`). Single source of truth needed.
13. **`LanguageContext` hardcodes 11 languages** (en, fr, ar, he, fa, ur, tr, id, ms, de, es) while backend bundles ship 3. Discovery should come from `GetAvailableLanguagesAsync()`.

**P3 — Enterprise Features**

14. **No native .NET `CultureInfo` integration.** Date/number formatting stays in `en-US` even when the user picks Arabic. `UseRequestLocalization()` middleware is not configured.
15. **No `.AspNetCore.Culture` cookie.** Our `lang` cookie is non-standard and bypasses `CookieRequestCultureProvider`.
16. **No `PersistentComponentState`** for culture → possible flicker on Server→WASM handoff.
17. **No Polly resilience** on `TolgeeTranslationProvider` / `WeblateTranslationProvider` HttpClients. No retry, no circuit breaker, no timeout policy beyond a single 10s.
18. **No rate-limit handling.** Tolgee sends `retryAfter` in 429 body (no headers). Weblate sends `X-RateLimit-*` headers. Both are ignored.
19. **No Prometheus metrics / Loki correlation** for TMS lookups, cache hit rate, fallback activations.

**P4 — Nice-to-Have (post-v1)**

20. Webhook subscription for cache invalidation (Weblate 5.11 free, Tolgee paid).
21. ICU pluralization / interpolation helper (`T("greeting", new { name = "Alice" })`).
22. Fallback chain per user language (e.g., `ar → en` if key missing).
23. WASM-side persistence (localStorage / IndexedDB) across sessions.
24. Architecture tests enforcing "components must not call `IEventApiClient` directly".
25. Integration tests with Testcontainers-backed Tolgee / Weblate.
26. Source-generated `TranslationKeys.g.cs` for compile-time safety.

---

## Guiding Principles

- **Enhance, don't rewrite.** The existing Blazor stack is ~500 lines of working code. Rip-and-replace wastes effort; surgical enhancements align with project scale.
- **Offline first, TMS optional.** Self-hosters should get a working instance from `docker run` with zero TMS configuration. Connected TMS is an ops upgrade for teams that want live translator workflows.
- **Abstraction as product.** "Choose Tolgee or Weblate" is a published capability, not an implementation detail. The admin UI and docs must treat it that way.
- **Startup must not depend on runtime TMS discovery.** A transient TMS failure must never change which cultures the app considers valid. Culture registry is local and trusted; TMS discovery is a runtime concern layered on top.
- **Accessibility is non-negotiable.** WCAG 2.2 AA. Every new component carries `aria-label`, focus ring, announce, keyboard path, and logical-property CSS.
- **Observability is non-negotiable.** Every TMS *fetch/fallback/admin* call emits a span + metric + log. Hot-path dictionary lookups are NOT instrumented — too noisy, too expensive. Fallbacks are alertable events, not silent recoveries.
- **No type suppression, no broken tests, no shotgun debugging.** Repo rules apply here too.

---

## Language Governance Model

One of the biggest design mistakes in localization systems is collapsing three distinct concepts into a single list. This plan keeps them strictly separated.

### The Three Concepts

| Concept | Who Owns It | When It Changes | Authority |
|---|---|---|---|
| **Culture Registry** | The codebase (compile-time) | Only on code release | `Explore.Application/Common/Localization/CultureRegistry.cs` — a static list of every culture code the platform knows how to render (UI strings, date/number formatting, RTL mirroring, MudBlazor wiring). If a culture is not in the registry, the app cannot use it, period. |
| **Enabled Languages** | Instance admin (runtime governance) | Via admin UI without redeploy | `localization.enabled_languages` governance setting — a comma-separated subset of the culture registry. Drives the language picker contents, `UseRequestLocalization` supported cultures, and validation of user preferences. An instance admin can disable `ar` without changing the registry or rebuilding the image. |
| **Available Translations** | The active TMS / offline bundles (runtime discovery) | Whenever TMS content changes | `ITranslationManagementProvider.GetAvailableLanguagesAsync()` — what the current provider actually has strings for *right now*. May be a superset (TMS has more than enabled), a subset (TMS is missing what admin enabled), or equal. This is **reporting data**, not config. |

### Governance Settings Introduced

Three new settings are seeded alongside the existing `localization.*` keys (IDs 560-564):

| Key | Type | Default | Purpose |
|---|---|---|---|
| `localization.enabled_languages` | string (CSV) | `"en,fr,ar"` | Subset of Culture Registry exposed to users / picker / admin UI |
| `localization.fallback_language` | string | `"en"` | Language used if a user's selected language is disabled, missing from the provider, or invalid. Falls back in the order: **user pref → fallback → default** |
| `localization.client_picker_enabled` | bool | `true` | Kill-switch: hides the `LanguagePicker` everywhere without redeploy |
| `localization.force_offline_mode` | bool | `false` | Kill-switch: `RuntimeTranslationProvider` bypasses the active TMS and always returns the offline bundle — used during TMS outages |

`localization.default_language` stays as the cold-start default when no cookie / user preference / fallback applies.

### Resolution Order (runtime language decision)

```
1. Check `localization.client_picker_enabled`.
     if false → force `default_language` for everyone (no picker)

2. Check `localization.force_offline_mode`.
     if true → route all TMS calls to OfflineTranslationProvider regardless of `tms_provider`

3. Resolve the user's desired language:
     a. authenticated user preference (api/user/appearance.Language)
     b. `lang` cookie
     c. `Accept-Language` header (best match against enabled_languages)
     d. `default_language`

4. Validate desired language:
     if ∉ Culture Registry → reject, use fallback_language
     if ∉ enabled_languages → reject, use fallback_language
     else → use desired language

5. Fetch translations:
     try enabled language from runtime provider (Tolgee | Weblate | Offline)
     on exception → OfflineTranslationProvider fallback (RuntimeTranslationProvider does this already)
     on missing key → return key itself (client-side `T()` fallback) OR fallback_language lookup (post-v1)
```

### What This Prevents

- **A misconfigured TMS cannot break startup.** `UseRequestLocalization` is configured from `CultureRegistry ∩ enabled_languages`, both of which are synchronous and local.
- **A TMS project with 20 extra languages does not inflate the navbar picker.** The admin controls exactly which languages are exposed.
- **A typo in a user preference (`"zh"` when `zh` is not in the registry) does not poison the cache or break the page.** Validation rejects it before it reaches the resolver.
- **Instance admins stay in control** without needing to know or touch the codebase.

### Implementation Touchpoints

- **Seed** `enabled_languages`, `fallback_language`, `client_picker_enabled`, `force_offline_mode` in `LookupTableSeeder.cs` alongside the existing localization keys.
- **Extend** `TranslationConfigResolver` to parse the new keys and expose them on `TranslationConfiguration`.
- **Enforce** in `RuntimeTranslationProvider` (respect `force_offline_mode`) and in both `HandleLanguagePreference` and the API user-preference handler (validate against `enabled_languages`).
- **Expose in admin UI** — the Localization section of `InstanceAdminSettingsLayout` has a "Language Set" card listing the culture registry and letting the admin toggle which are enabled, plus a "Fallback language" dropdown.
- **Source of truth** for the culture registry is `Explore.Application/Common/Localization/CultureRegistry.cs` — NOT `Explore.Blazor.Client`, so both server (`Explore.Blazor/Components/App.razor`) and client (`Explore.Blazor.Client/Models/LanguageContext.cs`) reference it without a server→client project dependency.

---

## Target Architecture

```
                      ┌──────────────────────────────────────────┐
                      │     Instance Admin (Blazor Admin UI)     │
                      │   /admin/instance → Localization tab     │
                      │   ┌────────────────────────────────────┐ │
                      │   │ Provider: [ None | Tolgee | Weblate]│ │
                      │   │ API URL / Project / Component       │ │
                      │   │ [Test Connection] [Save]            │ │
                      │   │ [Export & Persist Bundle]           │ │
                      │   └────────────────────────────────────┘ │
                      └────────────┬─────────────────────────────┘
                                   │ NSwag IEventApiClient
                                   │ via BFF ("BffClient")
                  ┌────────────────▼───────────────────┐
                  │  LocalizationAdminController (API) │
                  │   POST test-connection              │
                  │   GET  configuration                │
                  │   POST export-from-tms (+persist)   │
                  └────────────────┬───────────────────┘
                                   │ MediatR
         ┌─────────────────────────▼──────────────────────────┐
         │               Application Layer                    │
         │  TestTmsConnectionCommandHandler                    │
         │  ExportFromTmsCommandHandler (FIXED: persists)      │
         │  GetTranslationsQueryHandler                        │
         │  GetAvailableLanguagesQueryHandler                  │
         └─────────────────────────┬──────────────────────────┘
                                   │
      ┌────────────────────────────▼──────────────────────────┐
      │             RuntimeTranslationProvider                 │
      │  reads governance → dispatches to concrete provider    │
      │  catches all → falls back to OfflineTranslationProvider│
      └──┬────────────┬────────────┬─────────────┬────────────┘
         │            │            │             │
   ┌─────▼──┐  ┌──────▼─────┐ ┌────▼────────┐ ┌─▼───────────┐
   │ Tolgee │  │  Weblate   │ │   Offline   │ │    Null     │
   │ (HTTP) │  │  (HTTP)    │ │  (.json DLL)│ │   (no-op)   │
   └────────┘  └────────────┘ └─────────────┘ └─────────────┘
         ▲            ▲            ▲
         │ Polly      │ Polly      │ always-on
         │ Retry+CB   │ Retry+CB   │ ConcurrentDictionary
         │ 429-aware  │ 429-aware  │

───────────────────────────────────────────────────────────────

                          USER SIDE

 <html lang="@pageLang" dir="@pageDir">   ← App.razor reads cookie server-side
 │
 └── Routes.razor
     └── <LanguageProvider>                ← CascadingValue "Language"
         └── <MudRTLProvider RightToLeft="@ctx.EffectiveIsRtl">
             └── <MainLayout>
                 └── <NavMenu>
                     └── <LanguagePicker>  ← MudMenu, aria-label, announces change
                         └── POST /bff/language?lang=xx
                             ├─ cookie "lang" (custom, 365d)
                             ├─ cookie ".AspNetCore.Culture" (native .NET format)
                             └─ PUT /api/user/appearance (authenticated users)

             │
             ▼ inject
       ┌────────────────────────────────────┐
       │      ITranslationService           │
       │  T(key, fallback?)                 │
       │  GetTranslationsAsync(lang)        │
       │  ChangeLanguageAsync(lang)         │
       │  OnLanguageChanged event           │
       └──────────────┬─────────────────────┘
                      │ NSwag
       ┌──────────────▼─────────────────────┐
       │  _apiClient.TranslationAsync(lang) │
       └──────────────┬─────────────────────┘
                      │ YARP
       ┌──────────────▼─────────────────────┐
       │  TranslationController (API)       │
       │  GET /api/translation/{lang}       │
       └────────────────────────────────────┘

 Plus: MudBlazorLocalizer : MudLocalizer → ITranslationService  (new, DI-wired)
```

---

## Key Design Decisions

### D1 — Keep the custom `ITranslationService`; do not migrate to `IStringLocalizer<T>`

Microsoft docs explicitly allow any data source behind `IStringLocalizer` ("*By implementing IStringLocalizer, any data source can be used.*"). However `IStringLocalizer` is built around **synchronous** `.resx` access. Our translations come from a REST API; blocking in an indexer would stall render. A dedicated async service is the correct shape. **Microsoft's own Blazor docs validate this approach for custom data sources.**

### D2 — Use **both** the custom `lang` cookie AND `.AspNetCore.Culture`

Two cookies, two purposes, one user action:

- `lang` (existing, 365d, `SameSite=Lax`, `HttpOnly=false`, `Secure=!isDev`) drives our custom `ITranslationService` and is readable by JS on the WASM side.
- `.AspNetCore.Culture` (`CookieRequestCultureProvider.DefaultCookieName`, set via `CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture))`) drives native .NET `CultureInfo` so `UseRequestLocalization()` middleware works, date/number formatting follows the user, and MudBlazor time-pickers pick up the locale.

Writing both cookies in the same BFF handler is cheap and avoids a breaking migration.

### D3 — Extend `UpdateUserAppearancePreferencesDto` with `Language` (acceptable technical debt)

The API side already owns `PUT api/user/appearance`. Adding `Language` to the DTO and plumbing it into the existing handler gives authenticated users cross-device persistence with zero new endpoints, no new routes, and no NSwag regen surprises.

**Acknowledged debt**: semantically, language/locale is not "appearance." The correct long-term shape is a broader `UserPreferences` contract that composes appearance + language + notification + accessibility preferences. For v1 we accept the name mismatch to ship faster. A dedicated tech-debt ticket (`REFACTOR: split UserPreferences from UserAppearancePreferences`) is added to `dev/backlog/` as part of Phase 10 documentation. Same comment applies to the `InstanceOnboardingController` name — see D15.

### D4 — Admin UI lives inside the existing `InstanceAdminSettingsLayout` sidebar

New sidebar item: **"Localization"**, new section component: `InstanceLocalizationSection.razor`. Matches the existing pattern (`InstanceGovernanceSection`, `InstanceBrandingSection`, `InstanceAnalyticsSection`, …). No new routes, no new layouts.

### D5 — Conditional Admin UI form fields per provider

- **Tolgee** form: `API URL`, `Project ID` (numeric), `API Key` (stored via `SecretProvider`).
- **Weblate** form: `API URL`, `Project Slug`, **`Component Slug` (mandatory)**, `API Token`.
- **None** selection: shows an informational card explaining that offline bundles will be used and lists the languages currently shipped in the DLL.

The existing `localization.tms_component` governance key stays; it is simply hidden from the form when Tolgee is selected.

### D6 — Fix `ExportFromTmsCommandHandler` to actually persist (v1 default: writable directory, clearly scoped)

Current behaviour counts exports and returns. Target behaviour:

1. Call `ITranslationManagementProvider.ExportTranslationsAsync(languageCode)` (already done).
2. Serialize `IEnumerable<TranslationExport>` → flat `Dictionary<string, string>` → UTF-8 JSON (default serializer options — **no `UnsafeRelaxedJsonEscaping`**; the default is safe for Arabic/Unicode).
3. **Write bundle via the `IBundleFileWriter` abstraction** — never directly to disk. The v1 implementation writes to `{ContentRootPath}/App_Data/Localization/Bundles/{lang}.json` (outside the embedded-resource DLL, which is read-only at runtime). A future `S3BundleFileWriter` or `SharedVolumeBundleFileWriter` can drop in without touching the handler.
4. `OfflineTranslationProvider` is updated to check the writable directory FIRST, then fall back to the embedded resource.
5. Invalidate the translation cache: `ITranslationResolver.InvalidateLanguage(languageCode)` (add method if missing).
6. Return count + file path in `BaseCommandResponse<Guid>`.

**High-availability constraint (explicitly documented, not hidden)**: the local-filesystem implementation is correct for single-instance deployments and deployments where all replicas mount the same persistent volume. **It is not inherently HA-safe behind a horizontally-scaled load balancer without shared storage.** Two replicas running local disks will diverge after an "Export & Persist" click. This is acceptable for v1 because:

- Tier 1 deployments are typically single-instance.
- Tier 2 (Connected) deployments with HA are expected to use Kubernetes PVCs / Docker volumes / NFS mounts.
- The `IBundleFileWriter` abstraction lets us add a `DistributedBundleFileWriter` (backed by S3 / Azure Blob / Redis) post-v1 without touching the command handler.

The Localization admin UI surfaces **writable-path health** — it checks the directory exists, is writable, and whether the running process has write permission. If not, the "Export & Persist" button is disabled with an explanatory tooltip and a link to the deployment docs. Do not ship this as "enterprise-grade HA-safe"; ship it as "works for 95% of self-hosters and is abstractable for the other 5%."

This closes the loop between Tier 2 (Connected) and Tier 1 (Offline) — instance admins "pull" from a connected TMS into a file the offline provider can serve.

### D7 — Single resilience pipeline per provider, with a 429-body reader, not a separate retry handler

Each HttpClient gets **one** `AddResilienceHandler` pipeline using `Microsoft.Extensions.Http.Resilience`. The pipeline owns retry execution; there are **no overlapping custom DelegatingHandlers that also retry**. The only custom code is a small *reader* invoked by the pipeline's `DelayGenerator` to extract provider-specific retry hints from the response:

- **Tolgee**: when the pipeline decides to retry on 429, a `TolgeeRetryAfterReader` parses the response body JSON for `retryAfter` (milliseconds) and feeds that into `DelayGenerator`. No retry happens inside the reader — it only tells the pipeline how long to wait next time.
- **Weblate**: a `WeblateRateLimitReader` parses the `X-RateLimit-Reset` header (Unix seconds) and feeds the remaining delta to `DelayGenerator`.

Both readers are pure functions. Both pipelines share the same structure:

```csharp
services
  .AddHttpClient<TolgeeTranslationProvider>(c => c.Timeout = TimeSpan.FromSeconds(30))
  .AddResilienceHandler("tolgee", pipeline =>
  {
      pipeline
        .AddTimeoutPerAttempt(TimeSpan.FromSeconds(10))
        .AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = args => ValueTask.FromResult(/* 5xx, 408, 429, HttpRequestException */),
            DelayGenerator = async args => args.Outcome.Result is { } r && r.StatusCode == HttpStatusCode.TooManyRequests
                ? await TolgeeRetryAfterReader.ReadDelayAsync(r, args.CancellationToken)
                : null  // null = use built-in exponential backoff
        })
        .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(30)
        })
        .AddTimeout(TimeSpan.FromSeconds(60));  // total attempt budget
  });
```

Weblate is the same shape with a different reader. Failures short-circuit to `RuntimeTranslationProvider.Fallback` which returns the offline bundle. This gives us **one retry source of truth**, clean semantics, and no "did I retry twice?" debugging on 3am pages.

Caps on delay: 60 s (never wait longer than the total pipeline timeout).

### D8 — Observability via the `error-tracking` skill (instrument edges, not hot paths)

**Rule**: instrument TMS *fetches*, *fallbacks*, *admin operations*, and *change-language events* — NOT every `T(key)` lookup. UI render paths call `T(key)` constantly; every lookup would flood the metric registry and log stream for zero operational value.

- **Metrics** (Prometheus, via existing `IMetricsCollector`):
  - `islamu_translation_fetch_total{provider, language, result}` (counter) — incremented **only** on `GetTranslationsAsync` (= one fetch per language-change, not per component render). Result values: `hit_cache` | `hit_tms` | `hit_offline` | `error`.
  - `islamu_translation_fetch_duration_seconds{provider, language}` (histogram) — observed only on actual HTTP fetches.
  - `islamu_translation_change_language_total{from, to}` (counter) — fires on user language-change events, exposes the language-switching funnel.
  - `islamu_tms_connection_test_total{provider, result}` (counter) — only on admin "Test Connection" clicks.
  - `islamu_tms_fallback_activated_total{provider, reason}` (counter) — **alertable**: if this ticks > 0 in 5 minutes, page on-call.
  - `islamu_translation_cache_entries{scope}` (gauge: scope = `config|language|bundle`).
- **Logs** (Loki, structured JSON, correlation-ID via existing middleware):
  - Every TMS failure logs at `Warning` with `TenantId`, `Provider`, `Endpoint`, `StatusCode`, `RetryCount`, `CorrelationId`.
  - Fallback activations log at `Error` (NOT `Warning`) so the existing alert rules already know how to react.
  - Hot-path `T(key)` misses are **debug-level at most**, gated by a sampling rate, because a flood of missing-key warnings from a single render pass is noise.
- **Traces** (OpenTelemetry): `ITranslationManagementProvider.ExportTranslationsAsync`, `TranslationService.GetTranslationsAsync`, and `TranslationService.ChangeLanguageAsync` start child spans tagged with provider + language. **`T(key)` does NOT start a span** — too hot.

This approach gives us the operationally useful signal (is the TMS up? are we falling back? are users switching languages?) without drowning the system in per-render metrics.

### D9 — Accessibility baked in

Every component touched must satisfy:

- `aria-label` on icon-only triggers (picker button gets e.g. `"Change language, current: English"`).
- `IAccessibilityAnnouncerService.AnnouncePoliteAsync($"Language changed to {Name}")` after a successful change.
- Keyboard path: Tab focus, Enter/Space activation, Escape closes menu.
- `:focus-visible` ring via existing `--isl-focus-ring-*` tokens.
- Target size ≥ 24×24 CSS px.
- All custom CSS uses **logical properties only** (`margin-inline-start`, `inset-inline-end`, `text-align: start`) — physical properties are banned per `docs/ACCESSIBILITY.md` PR-4.
- The `<html lang="">` attribute always matches the content language (already wired in `App.razor` and `localization.js`; audit for edge cases).

### D10 — Wire `MudLocalizer` via a repo `MudBlazorLocalizer`

New class `MudBlazorLocalizer : MudLocalizer` living in `Explore.Blazor.Client/Services/` that delegates to `ITranslationService.T(key)` using the MudBlazor key convention. Registered via `builder.Services.AddTransient<MudLocalizer, MudBlazorLocalizer>()` in `ServiceCollectionExtensions`. This single wire translates all MudBlazor built-in strings (DataGrid headers, Dialog buttons, DatePicker labels, Pagination controls, etc.).

### D11 — `PersistentComponentState` for culture code (not full dictionary)

During Server prerender, persist the **culture code only** (tiny payload). WASM hydration reads it back and avoids a double-fetch cycle. Translation dictionaries are NOT persisted — they are fetched on the WASM side using the same cached response the Server used (HTTP output caching gives us this for free).

### D12 — Three sources of truth, not one — and none of them live in `Explore.Blazor.Client`

See the full design in **Language Governance Model** above. Summary of the code locations:

1. **Culture Registry** (compile-time authority on what the platform can render):
   - `Explore.Application/Common/Localization/CultureRegistry.cs` (NEW) — static list of `CultureEntry { Code, DisplayName, NativeName, Flag, IsRtl }`. Both `Explore.Blazor` (server SSR) and `Explore.Blazor.Client` (WASM) reference `Explore.Application` directly for DTOs, so this lives in a layer both already depend on — no new dependencies, no layering smell.
   - `Explore.Application/Common/Localization/RtlLanguages.cs` (NEW) — tiny helper `IsRtl(string code)` backed by the culture registry. Replaces both `_rtlLanguages` in `App.razor` and `RtlLanguages` in `LanguageContext.cs`.
2. **Enabled Languages** (governance-controlled runtime authority on what's exposed):
   - `localization.enabled_languages` governance key (NEW, seeded).
   - Parsed by `TranslationConfigResolver.ResolveAsync` and exposed on `TranslationConfiguration.EnabledLanguages`.
   - Validates `CultureRegistry ∩ enabled` to prevent typos from enabling unsupported cultures.
3. **Available Translations** (runtime discovery, reporting only):
   - `ITranslationService.GetAvailableLanguagesAsync()` still wraps `GET /api/translation/languages` — but it is treated as **reporting data for the admin UI** (e.g., "the active TMS has 20 languages; only 3 are enabled"), NOT as the allowlist for the picker or startup culture wiring.

`LanguageContext.cs` retires its hardcoded 11-language discovery dictionary. It keeps only a **display lookup** (flag + native name) with graceful unknown-code fallback (`code.ToUpperInvariant()` + 🌐). All authoritative decisions move to `CultureRegistry` + `enabled_languages`.

### D13 — Kill-switches are first-class governance, not afterthought

Two new governance keys introduced in the Language Governance Model section have explicit implementation tasks, not just mentions:

- `localization.client_picker_enabled` (default `true`): seeded in `LookupTableSeeder.cs`, parsed by `TranslationConfigResolver`, exposed on `TranslationConfiguration.ClientPickerEnabled`, consumed by `LanguagePicker.razor` (hides itself if false), surfaced in the Localization admin UI as a toggle.
- `localization.force_offline_mode` (default `false`): seeded, parsed, exposed on `TranslationConfiguration.ForceOfflineMode`, **enforced inside `RuntimeTranslationProvider`** (short-circuits TMS dispatch and returns Offline provider directly when true), surfaced in admin UI as an emergency toggle with a "use during TMS outages" helper text.

These are not decorative — ops needs them to turn off the picker or bypass the TMS without a redeploy if something breaks post-release.

### D14 — Explicit translation cache variation

Translation caches key on `(tenant, language, provider_mode)`:

- **Tenant** — each tenant can have different governance settings (`tms_provider`, `enabled_languages`, etc.). `TranslationConfig:{tenantId}` cache key already isolates this at the config layer; translation-result caches must do the same.
- **Language** — obvious; `Translation:{tenantId}:{languageCode}` for fetched dictionaries.
- **Provider mode** — whether `force_offline_mode` is in effect matters for cache keys because a tenant flipping from `tolgee` → offline should NOT serve stale tolgee-cached dictionaries. Cache keys include a provider-mode suffix: `Translation:{tenantId}:{languageCode}:{mode}` where mode = `"live"` or `"offline"`.

Cache invalidation walks tenant + language prefixes (via `IMemoryCache` `CancelAfter` tokens or explicit key tracking).

### D15 — Controller naming drift is acknowledged technical debt

Adding `PUT /api/InstanceOnboarding/localization-governance` to the existing `InstanceOnboardingController` is the pragmatic v1 choice — it matches the existing `analytics-governance` precedent and avoids NSwag / route changes. The name is starting to drift, though: the controller is no longer just about onboarding.

**Acknowledged debt**: medium-term, `InstanceOnboardingController` should be split into `InstanceSetupController` (one-time onboarding) and `InstanceSettingsController` (ongoing governance). A tech-debt ticket is added to `dev/backlog/` in Phase 10. For v1: keep the existing controller.

---

## Deployment Tiers (User-Facing Story)

| Tier | Name | TMS Config | Translator Workflow | Ops Footprint |
|---|---|---|---|---|
| **1** | **Offline** | `localization.tms_provider = none` | Translators edit JSON files in source control, rebuild the image, redeploy | Zero additional infra — just the DLL |
| **2** | **Connected** | `tms_provider = tolgee` or `weblate` + URL + Project [+ Component] + Secret | Translators use the TMS UI live; instance admin clicks "Export & Persist" to refresh offline fallback | +1 Docker container (Tolgee or Weblate) + PostgreSQL |
| **3** | **ISLAMU Global** (post-v1) | Connected tier + CDN distribution of bundles + community governance | Same as Tier 2 but bundles are centrally published | Tier 2 + CDN |

**Both Tolgee and Weblate are self-hostable and licensed permissively enough for community use** (Tolgee Apache 2.0 core, Weblate GPLv3 — copyleft but allows self-hosting without triggering distribution obligations for internal use). The admin UI surfaces a license note on each option so self-hosters can make an informed choice.

---

## Delivery Slices (How We Ship)

Phases are a *thinking* tool; slices are a *shipping* tool. Each slice is an independently mergeable, independently testable, independently deployable release. **Tests live inside the slice that introduces the code being tested** — there is no "Phase 9: Testing" dumping ground at the end. Phase 9 as written in this document is the **test registry**, organized by code surface; the task list (`blazor-localization-tasks.md`) assigns each test to the slice where it is written.

### Slice A — "Stabilize What Exists" (≈2.5 days)

**Goal**: fix the existing Blazor client stack to enterprise quality, introduce the Language Governance Model, populate starter bundles, and make BFF language persistence symmetric with theme/direction. Ship-ready on its own — the product is immediately better after this slice even if Slices B and C never land.

Includes:
- **Phase 0** — Audit + architecture anchors
- **Phase 1** — Enterprise quality pass on `TranslationService`, `LanguageProvider`, `LanguagePicker`, `App.razor`, `localization.js`
- **Phase 2** — BFF language persistence + `UseRequestLocalization` middleware
- **Phase 5** — Populate starter bundles (manually curated) — critical because Slice A ends with users able to pick a language that renders correctly offline
- **Language Governance Model seeding** — `enabled_languages`, `fallback_language`, `client_picker_enabled`, `force_offline_mode` settings seeded + wired into `TranslationConfigResolver` + enforced in `RuntimeTranslationProvider`
- **Critical tests for the above** — `TranslationServiceTests`, `LanguagePickerTests`, `LanguageProviderTests`, `BffPreferenceEndpointsTests` (Slice A subset), `TranslationConfigResolverTests` for governance keys, `RuntimeTranslationProviderFallbackTests`

**Slice A exit criteria**: picker + cookie + BFF persistence + offline bundles all work end-to-end in en/fr/ar, with a clean test suite, architecture tests in place, and the kill-switches verified to turn off the picker and force offline mode.

### Slice B — "Make the Product Operable" (≈3.5 days)

**Goal**: make the dual-provider abstraction operable for instance admins. Ship the Admin UI, fix the bundle export gap, wire native .NET `CultureInfo` + `MudBlazorLocalizer`.

Includes:
- **Phase 3** — Bundle export persistence fix (`IBundleFileWriter`, `ExportFromTmsCommandHandler` rewrite, `OfflineTranslationProvider` writable-dir check)
- **Phase 4** — Localization Admin UI (the biggest phase — section, service, form, test-connection, per-language export)
- **Phase 6** — Native `CultureInfo` + `MudBlazorLocalizer` + `PersistentComponentState` wiring
- **Tests for the above** — `ExportFromTmsCommandHandlerTests` (persistence + cache invalidation + `BundleWriteException`), `UpdateLocalizationGovernanceCommandHandlerTests`, `LocalizationAdminServiceTests`, `InstanceLocalizationSectionTests`, `MudBlazorLocalizerTests`, integration tests for `PUT localization-governance` and writable-path health

**Slice B exit criteria**: an instance admin can open `/admin/instance/settings`, pick Tolgee or Weblate, enter credentials (via SecretProvider), test the connection, save, and click "Export & Persist" per language to refresh the offline bundle — all without leaving the Blazor shell.

### Slice C — "Enterprise Hardening" (≈2 days)

**Goal**: resilience, observability, documentation, and the integration-test suite against real Tolgee/Weblate containers.

Includes:
- **Phase 7** — TMS provider resilience (single-pipeline Polly, Tolgee/Weblate 429 readers)
- **Phase 8** — Observability (Prometheus metrics, Loki correlation, OpenTelemetry spans — instrumented at *edges*, not hot paths, per D8)
- **Phase 9 (integration + architecture tests only)** — Testcontainers-backed Tolgee/Weblate tests, fallback behavior under pod kill, architecture tests for layering + service layer enforcement
- **Phase 10** — Documentation + rollout (`docs/LOCALIZATION.md`, `docs/BLAZOR.md`, `docs/ACCESSIBILITY_ARTIFACTS.md`, `docs/OPERATIONS.md`, tech-debt tickets for D3 and D15 renames)

**Slice C exit criteria**: killing the Tolgee container mid-request does not crash any page, Grafana dashboard plots live metrics, alerts fire on `islamu_tms_fallback_activated_total > 0` in 5m, and documentation reflects shipped reality.

### Slice-to-Phase Matrix

| Slice | Phases | Tests In-Slice | Duration | Cumulative |
|---|---|---|---|---|
| A — Stabilize | 0, 1, 2, 5, governance seeding | 9.1, 9.2, 9.3, plus BFF + config-resolver unit tests | 2.5 days | 2.5 |
| B — Operable | 3, 4, 6 | 9.4, 9.5, 9.6, 9.7, 9.8 (non-Testcontainer parts), plus writable-path health test | 3.5 days | 6.0 |
| C — Harden | 7, 8, 10 | Testcontainers integration tests, architecture tests, kill-container fallback test | 2 days | 8.0 |

Slice A is mergeable independently. Slice B depends on Slice A for the governance model and service-layer wrapper. Slice C depends on Slice B for the code it instruments + tests.

---

## Implementation Phases

Phases are ordered for minimum disruption: low-risk quality fixes first, then persistence gaps, then the big new admin UI, then enterprise hardening. **Tests for each phase live in the slice that owns the phase, not in a terminal test phase.**

### Phase 0 — Audit & Architecture Anchors (0.5 day) — Slice A

Lock in the reality documented above and add guard rails so the codebase cannot drift.

- Architecture test that pages/components in `Explore.Blazor.Client/Pages/**` and `Explore.Blazor.Client/Shared/**` may not directly depend on `IEventApiClient` (must go through a service).
- Architecture test that `ITranslationService` implementations live in `Explore.Blazor.Client/Services/`.
- Smoke test confirming `RuntimeTranslationProvider` falls back to `OfflineTranslationProvider` when Tolgee/Weblate HttpClients throw.
- Populate this phase's status in `blazor-localization-tasks.md`.

### Phase 1 — Enterprise Quality Pass on Existing Components (1 day) — Slice A

Fix code-quality gaps in files that already exist.

- **Culture Registry + RtlLanguages** (the source-of-truth work — done in `Explore.Application` per D12):
  - New `Explore.Application/Common/Localization/CultureRegistry.cs` with `CultureEntry` record and a static readonly list seeded with `en`, `fr`, `ar` (plus display metadata: `NativeName`, `Flag`, `IsRtl`).
  - New `Explore.Application/Common/Localization/RtlLanguages.cs` (or an `IsRtl(code)` static on `CultureRegistry` itself).
  - Both `App.razor`'s `_rtlLanguages` set AND `LanguageContext.RtlLanguages` are removed. The former references `CultureRegistry.IsRtl(code)`; the latter delegates the same way.
  - Note: `Explore.Blazor` and `Explore.Blazor.Client` both already reference `Explore.Application` for DTOs, so there is no new layering dependency. **We deliberately do NOT put shared primitives in `Explore.Blazor.Client/Constants/` — that would create a Server→Client dependency just for a constant.**
- `TranslationService.cs`:
  - Add language-code validation (allowlist check against `CultureRegistry ∩ enabled_languages`) before using as cache key → prevents cache-key poisoning.
  - Instrument **only** the fetch and change-language operations (per D8): `islamu_translation_fetch_total`, `islamu_translation_fetch_duration_seconds`, `islamu_translation_change_language_total`. Do NOT add metrics to `T(key)`.
  - Add `ILogger` scopes with `{TenantId, Language}` for Loki correlation at the fetch points.
- `LanguageProvider.razor`:
  - Convert `async void HandleLanguageChanged` → `async Task HandleLanguageChangedAsync` invoked via `InvokeAsync` from a thin event-handler shim.
  - Declare `@implements IDisposable` on the `@code` block.
  - Announce via `IAccessibilityAnnouncerService.AnnouncePoliteAsync` after a successful language change.
  - Guard initialization against `client_picker_enabled = false` (no-op cascade if kill-switch is off).
- `LanguagePicker.razor`:
  - Add `aria-label="@(string.Format(T("ui.picker.language.aria", "Change language. Current: {0}"), Language?.LanguageName))"` to the button.
  - Wrap language change in snackbar feedback (success/failure).
  - Replace direct `IHttpClientFactory.CreateClient("BffClient")` usage with a new `ILanguagePreferenceService` wrapper in `Services/` (conforms to repo service layer).
  - Create `LanguagePicker.razor.css` with BEM + logical properties only.
  - Hide itself if `client_picker_enabled == false` (kill-switch support).
- `LanguageContext.cs`:
  - Retire hardcoded 11-language discovery (keep display-only metadata dictionary, or inherit from `CultureRegistry.TryGetEntry(code)`).
  - Point RTL check at `CultureRegistry.IsRtl(code)`.
- `App.razor`:
  - Read from `CultureRegistry.IsRtl(pageLang)` (drop local `_rtlLanguages` field).
  - Validate `langCookie` via **`CultureRegistry.TryGetEntry(code, out var entry)`** — no naive regex. An unknown/typo'd cookie falls back to `default_language`. This is a controlled allowlist, which is both safer than a regex and keeps the culture registry as the single source of truth.
- `localization.js`:
  - Add `try/catch` in cookie parsers (fail open to empty string on any parse error).
  - Consider ES module conversion (non-blocking; note in post-v1).

**Tests owned by Slice A** (live in this slice, not Phase 9):
- `TranslationServiceTests` — cache hit/miss, allowlist rejection, metric increments (only at fetch), change-language event.
- `LanguagePickerTests` — aria-label, keyboard nav, service call, announce, kill-switch hide.
- `LanguageProviderTests` — cascading context, Dispose unsubscription, kill-switch hide.
- `CultureRegistryTests` — `IsRtl`, `TryGetEntry`, unknown-code handling.
- `RuntimeTranslationProviderFallbackTests` — exception path → offline + metric increment.
- `BffPreferenceEndpointsTests` — language validation against `enabled_languages`, both cookies written, authenticated persistence (the Slice A cut of Phase 2's tests; remaining tests stay with Phase 2).

### Phase 2 — BFF Language Persistence for Authenticated Users (0.5 day) — Slice A

Make `/bff/language` feature-parity with `/bff/theme` and `/bff/direction`.

- Extend `UpdateUserAppearancePreferencesDto` with `public string Language { get; set; } = "en";`.
- Update API `PUT api/user/appearance` handler to accept the new field (read, validate against allowlist, persist to user record, clear resolver caches).
- Update `HandleLanguagePreference` in `BffPreferenceEndpoints.cs`:
  - Validate 2–5 char length (existing).
  - Validate against the configured allowlist.
  - If authenticated → call `PUT api/user/appearance` with all three fields preserved.
  - Always write **two** cookies: `lang` (existing) + `.AspNetCore.Culture` (new) via `CookieRequestCultureProvider.MakeCookieValue`.
- Add `UseRequestLocalization()` middleware in both `Explore.API/Program.cs` and `Explore.Blazor/Program.cs` with supported cultures from `GetAvailableLanguagesAsync` at startup (cached).

### Phase 3 — Bundle Export Persistence Fix (1 day) — Slice B

Make "Export from TMS" actually update offline fallback. The HA-constraint documentation and the abstraction are as important as the code fix.

- **`IBundleFileWriter` abstraction** in Application (`Explore.Application/Contracts/Infrastructure/IBundleFileWriter.cs`):
  - Method: `Task<BundleWriteResult> WriteBundleAsync(string languageCode, IReadOnlyDictionary<string,string> translations, CancellationToken ct)`.
  - Result record: `BundleWriteResult(bool Success, string? Path, string? ErrorMessage)`.
  - Reason for the abstraction: v1 is local-filesystem, but a future `DistributedBundleFileWriter` (S3 / Azure Blob / Redis) can drop in without touching the command handler. Keep the seam clean.
- **`BundleFileWriter` implementation** in Infrastructure:
  - Inject `IWebHostEnvironment`, target `{ContentRootPath}/App_Data/Localization/Bundles/`.
  - Create the directory if missing.
  - Serialize with **default `JsonSerializerOptions` + `WriteIndented = true`**. No `UnsafeRelaxedJsonEscaping` — the default serializer handles Arabic and other Unicode correctly; unsafe escaping is unnecessary and loses the safety guarantees of the default encoder.
  - Write atomically: stream to `{code}.json.tmp` then `File.Move` to `{code}.json`.
  - Log `Information` on success with path; log `Error` and return `Success = false` on failure (do NOT throw — the command handler converts to a `BaseCommandResponse<Guid>` failure).
- **Writable-path health check** (new, surfaced in admin UI):
  - New method `Task<WritablePathHealth> CheckHealthAsync()` returning `(bool Exists, bool Writable, string? Reason)`.
  - Called by the Localization admin UI during section mount + on each "Export & Persist" click precondition check.
  - If unhealthy, the Export button is disabled with a tooltip "Bundle directory not writable. See deployment docs for volume mount configuration."
- **`OfflineTranslationProvider` writable-first lookup**:
  - On each `LoadBundle(languageCode)` call, check `App_Data/Localization/Bundles/{code}.json` first. If present and readable, deserialize that. Otherwise fall back to `Assembly.GetManifestResourceStream(...)`.
  - Still thread-safe via `ConcurrentDictionary` cache.
  - New method `InvalidateLanguage(string languageCode)` that clears a single language's cache entry (called by the command handler after export).
  - **Lifetime adjustment note**: `OfflineTranslationProvider` is currently Singleton. Injecting `IWebHostEnvironment` is compatible (Singleton can consume Singleton). If any new dep is Scoped, downgrade to Scoped and document.
- **`ExportFromTmsCommandHandler` rewrite**:
  - Inject `IBundleFileWriter` + (reuse) `ITranslationResolver`.
  - Fetch exports, materialize to dictionary, call `_bundleFileWriter.WriteBundleAsync(...)`.
  - On success: call `_translationResolver.InvalidateLanguage(languageCode)` (new method, see below), return `BaseCommandResponse { Success = true, Message = $"Exported {count} translations for '{lang}' → {path}" }`.
  - On writer failure: return `BaseCommandResponse { Success = false, Message = result.ErrorMessage }`.
  - Drop the dead `ITranslationResolver` dependency OR use it for cache invalidation (preferred).
- **`ITranslationResolver.InvalidateLanguage`** new method (if not present):
  - Clears `Translation:{tenantId}:{languageCode}:*` entries from `IMemoryCache` (cache keys incorporate tenant + language + provider_mode per D14).

**HA constraint — explicitly documented**:

The local-filesystem implementation works correctly for:
- Single-instance deployments.
- Deployments where all replicas mount the same persistent volume (Kubernetes `PersistentVolumeClaim`, Docker named volume, NFS mount).

It does **not** automatically replicate across multiple replicas with local disks. This is a known constraint for v1:
- `docs/LOCALIZATION.md` gets a "Deployment Topology" subsection explaining the constraint and documenting the recommended volume-mount configurations.
- The admin UI shows a banner on the Localization section if `App_Data/Localization/Bundles/` is detected as a non-persistent path (heuristic: inspect `ContentRootPath` + writable-path check).
- `dev/backlog/` gets an explicit ticket "Add `DistributedBundleFileWriter` for horizontally-scaled self-hosters" citing S3 / Azure Blob / Redis options and `IBundleFileWriter` as the extension point.

**Tests owned by Slice B** (live here, not Phase 9):
- `BundleFileWriterTests` — writes to temp directory, handles missing directory, atomic move, failure returns false.
- `OfflineTranslationProviderTests` (extend existing) — writable-dir first, falls back to embedded, cache invalidation.
- `ExportFromTmsCommandHandlerTests` (extend existing) — persist + invalidate + count + path in response, writer failure path, empty exports.
- `WritablePathHealthCheckTests`.

### Phase 4 — Localization Admin UI (2–3 days, BIGGEST PHASE) — Slice B

Ship the UI that makes the dual-provider abstraction real for operators.

**Files to create:**

- `Explore.Blazor.Client/Pages/Admin/Instance/Components/Sections/InstanceLocalizationSection.razor` (+ `.razor.css`) — the main admin section.
- `Explore.Blazor.Client/Contracts/Services/ILocalizationAdminService.cs` (contract).
- `Explore.Blazor.Client/Services/LocalizationAdminService.cs` — wraps NSwag `_apiClient.GetLocalizationConfigurationAsync()`, `TestTmsConnectionAsync()`, `ExportFromTmsAsync()`.
- `Explore.Blazor.Client/Models/Admin/LocalizationAdminState.cs` — view model for the form.
- `Explore.Blazor.Client/Services/FooterAdminService.cs` pattern is the reference.

**Files to modify:**

- `InstanceAdminSettingsLayout.razor` — add new sidebar nav item `{Group: "Content", Label: "Localization", SectionId: "localization", Icon: Icons.Material.Rounded.Translate}` and a new `case "localization"` rendering the section.
- `ServiceCollectionExtensions.cs` — register `ILocalizationAdminService`.

**UI specification:**

- Header card (`AppCard`): current provider badge, default language chip, bundle path (if writable dir exists), connectivity status (pinged on section mount).
- Provider picker (`MudSelectList` of None / Tolgee / Weblate) — changes the form below.
- Form fields (`AppTextField<T>`):
  - **None**: no fields, info alert linking to docs/LOCALIZATION.md.
  - **Tolgee**: `API URL`, `Project ID`, `API Key` (masked input, stored via `SecretProvider` endpoint). License tooltip: "Apache 2.0 core. Webhooks require paid license."
  - **Weblate**: `API URL`, `Project Slug`, `Component Slug` (required), `API Token` (masked). License tooltip: "GPLv3 (copyleft) — suitable for self-hosted use."
- **[Test Connection]** `AppButton` → calls `TestTmsConnectionCommand` and shows inline success/failure with specific error messages per provider.
- **[Save]** `AppButton` → persists via `PUT /api/InstanceOnboarding/analytics-governance` equivalent (new `/api/InstanceOnboarding/localization-governance` endpoint — modeled on analytics).
- **Bundle management card**:
  - For each language the system knows about (from `GetAvailableLanguagesAsync`) display: language name + flag + key count in current bundle + last-modified timestamp.
  - **[Export & Persist]** `AppButton` per language — calls `ExportFromTmsCommand`, shows progress spinner, snackbar on completion, refreshes the card.
- **Danger zone** (at bottom, collapsed by default): **[Reset to Offline-Only]** — sets `tms_provider = none` and clears TMS secrets.

All buttons have `aria-label`, all inputs are keyboard navigable, all cards use logical CSS properties, dialogs save/restore focus via `IAccessibilityFocusService`.

**Backend (Application + API) additions required:**

- New handler: `UpdateLocalizationGovernanceCommandHandler` (Application) — writes governance settings atomically, invalidates config cache.
- New endpoint: `PUT /api/InstanceOnboarding/localization-governance` (API) — `[Authorize]`, takes a `UpdateLocalizationGovernanceDto { DefaultLanguage, TmsProvider, TmsApiUrl, TmsProjectId, TmsComponent }`.
- Secret storage for `tms_api_key` (Tolgee) and `tms_api_token` (Weblate) reuses the existing `SecretProvider` admin flow.

### Phase 5 — Populate Starter Bundles (0.5 day) — Slice A

Fix the empty-bundle blocker. **Curate manually, do not auto-scrape as source of truth.**

Auto-scraping from `.razor` files is a useful *audit aid* but a bad *content source* — it captures one-off error messages, disabled-state placeholder text, and transient debug strings that should never be translation keys. For v1 we hand-pick 80-150 core keys that are unambiguously user-facing and stable.

- **Curation process**:
  1. Manual walk-through of `NavMenu`, `MainLayout`, `LanguagePicker`, Landing, Error/NotFound pages, `AppButton` / `AppCard` labels, admin settings sidebar labels, and common MudBlazor integration points (dialog OK/Cancel, confirmation prompts).
  2. For each user-facing string, define a key following `ui.{area}.{component}.{element}` (e.g., `ui.nav.menu.dashboard`, `ui.picker.language.aria`, `ui.dialog.confirm.yes`).
  3. Record in `dev/active/blazor-localization/bundles-key-audit.md` as a table: `{Component Path | Source Line | Raw Text | Proposed Key}`.
  4. Populate `en.json`, `fr.json`, `ar.json` from the audit table.
- **Content requirements**:
  - English: direct strings, copy-edited for consistency.
  - French: native speaker translation (or machine-assisted with `(review)` suffix for lines needing human review).
  - Arabic: native speaker translation with RTL-aware phrasing; watch mixed-direction strings (numbers inside Arabic text stay LTR in render via `dir="auto"` on the container).
  - Include `mudblazor.*` key set seeded in Phase 6 (DataGrid headers, Dialog buttons, DatePicker labels, Pagination controls).
  - Flat JSON, pretty-printed (2-space indent), UTF-8 no BOM.
- **Advisory architecture test** (`Event.Architecture.Tests/HardcodedStringsTests.cs` — advisory at first, blocking post-v1):
  - Regex-scans `.razor` files for string literals inside `<MudText>`, `<AppButton>`, `<MudButton>`, `<PageTitle>` that are NOT wrapped in `@T("...")`.
  - Outputs a report; initial repo state becomes the baseline budget.
- **Docs**: add a "UI String Convention" subsection to `docs/LOCALIZATION.md` with the `ui.{area}.{component}.{element}` rule, a starter table, and the auto-scrape vs curation note.

**Tests owned by Slice A**:
- JSON parse validation for all three bundles (checks key count matches across languages ± 5).
- `HardcodedStringsTests` (advisory mode, runs against current repo to establish baseline).

### Phase 6 — Native .NET CultureInfo + MudLocalizer (0.5 day) — Slice B

Activate parallel native support.

- `UseRequestLocalization()` wired in both `Explore.API/Program.cs` and `Explore.Blazor/Program.cs` with:
  - `SupportedCultures = [new("en"), new("fr"), new("ar")]`
  - `DefaultRequestCulture = new RequestCulture("en")`
  - `RequestCultureProviders.Clear()` then `Insert(0, new CookieRequestCultureProvider())`
- In `Explore.Blazor.Client/Program.cs` (or `ServiceCollectionExtensions`) — consume the `PersistentComponentState` culture code on WASM startup, set `CultureInfo.DefaultThreadCurrentCulture` + `CurrentUICulture` before the first render.
- New `Explore.Blazor.Client/Services/MudBlazorLocalizer.cs` deriving from `MudLocalizer`, delegating to `ITranslationService.T()` using the MudBlazor key convention (`MudLocalizer[Key]` = `T($"mudblazor.{Key}")` with graceful fallback).
- Register: `services.AddTransient<MudLocalizer, MudBlazorLocalizer>()`.
- Seed the `mudblazor.*` keys into the three starter bundles (DataGrid headers, Dialog OK/Cancel, DatePicker labels, Pagination controls).

### Phase 7 — TMS Provider Resilience (1 day) — Slice C

Harden the two HTTP-based providers with a **single** Polly pipeline per client. No overlapping retry handlers.

- New package reference: `Microsoft.Extensions.Http.Resilience` (built-in .NET 10, Polly-based, no third-party dep).
- **No custom retrying `DelegatingHandler`s.** Custom code is limited to two small *readers* that parse provider-specific retry hints from responses and feed them to the pipeline's `DelayGenerator`:
  - `TolgeeRetryAfterReader.ReadDelayAsync(HttpResponseMessage, CancellationToken)` — deserializes a 429 response body to `TolgeeRateLimitError { Message, RetryAfter (ms), Global }`; returns `TimeSpan?`.
  - `WeblateRateLimitReader.ReadDelay(HttpResponseMessage)` — reads `X-RateLimit-Reset` header (Unix seconds), computes delta from `DateTimeOffset.UtcNow`, returns `TimeSpan?`.
  - Both readers are stateless static helpers — no DI, no state, no side effects.
- **One pipeline per client**, configured in `InfrastructureServicesRegistration.cs`:
  - `AddHttpClient<TolgeeTranslationProvider>` → `AddResilienceHandler("tolgee", pipeline => ...)` with `AddTimeoutPerAttempt(10s)` → `AddRetry(...)` → `AddCircuitBreaker(...)` → `AddTimeout(30s)` (total attempt budget).
  - `AddRetry` uses `DelayGenerator = async args => TolgeeRetryAfterReader.ReadDelayAsync(args.Outcome.Result, args.CancellationToken)` to honor 429 body hints. For non-429 retryable failures, `DelayGenerator` returns `null` → the pipeline falls back to its exponential backoff default.
  - `ShouldHandle` covers: `HttpRequestException`, `HttpResponseMessage { StatusCode: >=500 or 408 or 429 }`.
  - `AddCircuitBreaker` with `FailureRatio = 0.5`, `MinimumThroughput = 5`, `SamplingDuration = 30s`, `BreakDuration = 30s`.
  - Same shape for Weblate with its own reader.
- Failures short-circuit through `RuntimeTranslationProvider`'s existing exception-catch path → falls back to `OfflineTranslationProvider` cleanly. Circuit breakers give us fast-fail; retries give us transient-failure tolerance; the two do not fight.
- **Delay capping**: no retry waits longer than 60 s (pipeline total timeout is 30 s; reader outputs are clamped).

**Tests owned by Slice C**:
- `TolgeeRetryAfterReaderTests` — valid JSON body with `retryAfter`, missing field → null, malformed JSON → null (no throw).
- `WeblateRateLimitReaderTests` — valid header, missing header → null, negative delta → 1s floor.
- `TolgeeResilienceIntegrationTests` with `WireMock.Net`:
  - 429 with body → one retry after parsed delay.
  - 500 then 200 → pipeline exponential backoff used.
  - Repeated 500 → circuit opens, subsequent calls short-circuit without hitting the wire.
- `TolgeeFallbackIntegrationTests` using Testcontainers (kill container mid-request, verify `RuntimeTranslationProvider` falls to offline).

### Phase 8 — Observability & Error Tracking (0.5 day) — Slice C

Wire metrics and structured logs per Decision D8.

- Extend `IMetricsCollector` with the counters/histograms from D8.
- Instrument `TolgeeTranslationProvider`, `WeblateTranslationProvider`, `OfflineTranslationProvider`, `RuntimeTranslationProvider`, `TranslationService.cs` (client-side metric via JS interop — post-v1).
- Structured logging throughout (see D8 log shape).
- Dashboard skeleton (Grafana JSON) committed to `docs/observability/dashboards/localization.json` showing fetch latency, cache hit rate, fallback counter, provider test-connection results.

### Phase 9 — Test Registry (distributed across Slices A, B, C — NOT a terminal phase)

This phase is a **registry** of the tests the project owns, grouped by code surface. It is **not** a terminal phase where tests are written after implementation. Each test case is owned by the slice that introduces the code under test (see the Slice-to-Phase Matrix in the Delivery Slices section).

Repo-quality test coverage across the stack:

- **Unit tests** (`Event.Application.UnitTests`):
  - `TranslationServiceTests` (Blazor.Client side — already has project, see below): cache hit, cache miss, concurrent access, API error fallback, language change, T(key) missing, language allowlist.
  - Extended `ExportFromTmsCommandHandlerTests`: persists file, invalidates cache, returns success with path, partial failures.
  - `UpdateLocalizationGovernanceCommandHandlerTests`: writes all keys, invalidates config cache, tenant scoping.
- **Blazor.Client tests** (`Explore.Blazor.Client.Tests` using bUnit + TUnit):
  - `Services/TranslationServiceTests.cs` (Blazor-side service).
  - `Services/LocalizationAdminServiceTests.cs`.
  - `Components/LanguagePickerTests.cs`: renders languages, keyboard nav, announces change, calls BFF endpoint on selection.
  - `Components/InstanceLocalizationSectionTests.cs`: renders form per provider, test-connection button calls service, save shows snackbar.
  - Add `CreateTranslationService()` factory + `AddTranslationService()` helper to `MockServiceFactory.cs` and `BlazorTestContext.cs`.
- **Integration tests** (`Event.API.IntegrationTests`):
  - `LocalizationControllerTests`: `GET /api/translation/en` returns bundle.
  - `LocalizationAdminControllerTests`: `PUT localization-governance` persists, `POST test-connection` uses mocked TMS.
- **Architecture tests** (`Event.Architecture.Tests`):
  - Components may not inject `IEventApiClient`.
  - `ITranslationService` implementations live under `Services/`.
  - Infrastructure TMS providers must implement `ITranslationManagementProvider`.

### Phase 10 — Documentation & Rollout (0.5 day) — Slice C

- Update `docs/LOCALIZATION.md`:
  - Add section "Blazor Client Integration" pointing at `LanguageProvider`, `ITranslationService`, `LanguagePicker`, `MudBlazorLocalizer`.
  - Add section "Offline Bundles — Runtime Writable Directory" explaining the `App_Data/Localization/Bundles/` fallback layer.
  - Add section "Choosing Between Tolgee and Weblate" with the comparison table from Phase 4 licensing notes.
- Update `docs/BLAZOR.md` with a "Localization" subsection referencing `docs/LOCALIZATION.md`.
- Update `docs/ACCESSIBILITY_ARTIFACTS.md` with the LanguagePicker PR row (WCAG criteria satisfied: 1.3.1, 1.4.3, 2.1.1, 2.4.7, 3.3.8, 4.1.3).
- Update `.claude/skills/blazor-ui-conventions/` if a new convention emerged (e.g., "use `@T("...")` not raw strings").
- Add `docs/OPERATIONS.md` entry explaining the new metrics + alert thresholds.
- Mark `dev/active/blazor-localization/` phase 10 complete and move the folder to `dev/done/` upon user approval.

---

## Enterprise Concerns (Cross-Cutting)

### Security

- **Language code allowlist** — every input (cookie, query, DTO) is validated against `CultureRegistry ∩ enabled_languages` (NOT against `GetAvailableLanguagesAsync`, which is runtime-discovered reporting data). Prevents cache-key poisoning and SSRF-like misuse of free-text keys.
- **TMS URL validation** — admin UI rejects non-HTTPS in non-dev environments; warns on self-signed certs.
- **TMS secret lifecycle** (operational rules, not just "stored in SecretProvider"):
  - Secrets are **write-only** in the admin UI. Form field is `MudTextField.InputType.Password` with a clear button. There is no "reveal" toggle.
  - The UI shows **"configured"** or **"not configured"** badge — **never the raw value, not even partially masked**. No "sk_live_****abc3" tail rendering.
  - Rotating a secret (e.g., changing the Tolgee API key) must NOT require re-entering unrelated settings. The form submits the secret change independently of the non-secret governance settings, via the existing `SecretProvider` admin endpoint.
  - Secrets stored via the existing `SecretProvider` (Infisical-backed) — never in governance settings, never in cookies, never in logs.
- **LocalRedirect only** on any new redirect-style flows (BFF `/bff/language` returns `Results.Ok`, not a redirect, so this is academic but the rule stays).

### Performance

- `PersistentComponentState` for culture code only — HTML payload stays tiny.
- `HybridCache` on `/api/translation/{lang}` (already wired) with `ETag` middleware for 304 replays.
- `Preload on language change` (already in `TranslationService`) keeps a single fetch per change event.
- Bundle files are gzip-compressed by response compression middleware (already wired).
- Text expansion: FR/DE/ES can be 20–40 % longer than EN. All CSS tests run against the longest-language variant.
- **Cache variation (explicit)** — all translation caches key on the tuple `(tenantId, languageCode, providerMode)`:
  - **tenantId** — multi-tenant isolation; different tenants can configure different providers.
  - **languageCode** — obvious, but must include the canonical-case version (`en` not `En`).
  - **providerMode** — `"live"` or `"offline"`. A tenant flipping `force_offline_mode = true` must NOT serve stale live-TMS cached content. Keys include a mode suffix: `Translation:{tenantId}:{languageCode}:{mode}`.
- **Hot-path discipline** — `T(key)` is called on every component render; it must never touch I/O, never allocate outside the cache dictionary, and never emit metrics/logs (per D8). The only allowed operations are a single `TryGetValue` on an in-memory dictionary and a return.

### Rollback Strategy — Kill-switches are explicit governance settings, fully wired

See **Language Governance Model** for the design; the implementation tasks are explicit in the task list:

- `localization.client_picker_enabled` (default `true`):
  1. Seeded in `LookupTableSeeder.cs` alongside other `localization.*` keys.
  2. Parsed by `TranslationConfigResolver` and exposed on `TranslationConfiguration.ClientPickerEnabled`.
  3. Consumed by `LanguagePicker.razor` (returns `@null` from the render body when false — clean unmount, no residual Tab stops).
  4. Visible in the Localization admin UI as a toggle in a "Kill Switches" card with a runtime-disable warning.
- `localization.force_offline_mode` (default `false`):
  1. Seeded.
  2. Parsed and exposed on `TranslationConfiguration.ForceOfflineMode`.
  3. **Enforced inside `RuntimeTranslationProvider.ResolveProviderAsync()`** — short-circuits to `OfflineTranslationProvider` regardless of `tms_provider` value.
  4. Cache keys include the current mode (see Performance → Cache variation) so flipping the switch does not serve stale live-TMS content.
  5. Visible in the Localization admin UI with an emergency-switch label ("Bypass all TMS calls. Use during TMS outages only.").

Both kill-switches are verified by integration tests in Slice A (seed + parse + enforce + admin visibility) before we lean on them operationally.

### A11y Summary

- WCAG 2.2 Level AA target.
- `lang` attribute drives pronunciation.
- Logical properties everywhere.
- `aria-label` on icon-only controls.
- `IAccessibilityAnnouncerService` on dynamic changes.
- Focus restoration after dialogs.
- No color-only information (language shown with flag + name + code).

---

## Risk Assessment

### High

- **R1 — Breaking the language picker for existing users during refactor.** Mitigation: feature flag `localization.client_picker_enabled`; keep old code paths working through each phase; never merge phases with red tests.
- **R2 — InteractiveAuto culture flicker.** Mitigation: `PersistentComponentState` + HTTP output caching + PersistentComponentState unit test.

### Medium

- **R3 — `RuntimeTranslationProvider` fallback hides TMS misconfiguration.** Mitigation: fallback activations emit `Error`-level logs + `islamu_tms_fallback_activated_total` counter wired to alerts.
- **R4 — MudBlazor localizer keys explosion.** Mitigation: seed only the keys currently used by our MudBlazor feature surface (DataGrid, Dialog, Pagination, DatePicker); defer the full catalog to post-v1.
- **R5 — Writable bundle directory permissions.** Mitigation: fall back to read-only mode with a logged warning if `App_Data/Localization/Bundles/` is not writable; the admin UI surfaces this clearly.

### Low

- **R6 — Export file size > 100 KB.** Mitigation: gzip on wire, `ETag` on HTTP cache; measure and monitor.
- **R7 — Arabic rendering edge cases in MudBlazor** (MudSlider/MudMenu/MudAutocomplete need dir overrides per upstream PRs #12706/#12777/#9913). Mitigation: manual QA in Arabic during Phase 4 acceptance.

---

## Success Metrics

1. **Admin workflow** — An instance admin can configure Tolgee or Weblate, save, test the connection, and run an export-to-bundle entirely from the Blazor shell in under 2 minutes without leaving `/admin/instance`.
2. **Offline operation** — A fresh `docker run` with `tms_provider = none` shows all UI strings in English, French, and Arabic. Starter bundles are populated.
3. **Language switching UX** — Picker response time < 500 ms from click to re-render; zero flicker on Server→WASM hand-off; screen-reader announces the change; RTL applies instantly for Arabic.
4. **Resilience** — Killing the Tolgee container mid-request does NOT crash any user-facing page; `RuntimeTranslationProvider` falls back to offline bundles within 10 s (timeout tier).
5. **Observability** — Grafana dashboard shows `islamu_translation_lookup_total` > 0 and `islamu_tms_fallback_activated_total` = 0 during healthy operation.
6. **Test coverage** — `dotnet test` passes for all new tests (≥ 40 new tests across unit/integration/bUnit). Architecture tests pass.
7. **Accessibility** — Axe-DevTools automated scan on Blazor shell reports zero WCAG 2.2 AA violations attributable to the language picker or admin UI.
8. **Docs** — `docs/LOCALIZATION.md` reflects reality; a developer can set up a local Tolgee via `docker-compose` following the docs and have a working loop in under 30 minutes.

---

## Dependencies

- **NSwag swagger.json regeneration** after adding `PUT /api/InstanceOnboarding/localization-governance` and (if needed) extensions to `UpdateUserAppearancePreferencesDto`. Build target `GenerateApiClient` handles this automatically.
- **`IMetricsCollector`** (existing) — Phase 8 extends it with new metrics.
- **`SecretProvider`** (existing, Infisical-backed) — Phase 4 uses it for TMS secrets.
- **`IAccessibilityAnnouncerService`** + **`IAccessibilityFocusService`** (existing) — Phases 1 and 4 consume them.
- **`Microsoft.Extensions.Http.Resilience`** — NEW package reference for Phase 7.
- **Testcontainers** (already in use in `Event.Persistence.IntegrationTests`) — Phase 9 reuses for Tolgee/Weblate.
- **`docs/LOCALIZATION.md`**, **`docs/ACCESSIBILITY.md`**, **`docs/CONFIGURATION.md`**, **`docs/EXTENSIBILITY.md`** — reference reading before each phase.

---

## References

### Repo

- `docs/LOCALIZATION.md` — tier model, provider routing, governance keys
- `docs/ACCESSIBILITY.md` — WCAG 2.2 AA targets, CSS logical-property ban (PR-4), service contracts
- `docs/BLAZOR.md` — render modes, BFF pattern, CSS @layer architecture
- `docs/EXTENSIBILITY.md` — provider abstraction authored guide
- `docs/CONFIGURATION.md` — governance key layers, secret storage
- `.claude/skills/blazor-bff-patterns/SKILL.md`
- `.claude/skills/blazor-ui-conventions/SKILL.md`
- `.claude/skills/blazor-css-isolation/SKILL.md`
- `.claude/skills/clean-architecture-rules/SKILL.md`
- `.claude/skills/cqrs-mediatr-guidelines/SKILL.md`
- `.claude/skills/error-tracking/SKILL.md`

### External

- [Blazor globalization and localization | learn.microsoft.com](https://learn.microsoft.com/en-us/aspnet/core/blazor/globalization-localization) — official MS guidance, confirms custom service approach
- [Request localization middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization) — `CookieRequestCultureProvider`, `UseRequestLocalization`
- [MudBlazor Localization](https://mudblazor.com/features/localization) — `MudLocalizer` override pattern
- [Tolgee API v2 documentation](https://docs.tolgee.io/api) — endpoints, export formats, rate-limit body format
- [Tolgee self-hosting](https://docs.tolgee.io/platform/self_hosting/running_with_docker) — Docker, config, limits
- [Tolgee API keys and PATs](https://docs.tolgee.io/platform/account_settings/api_keys_and_pat_tokens) — `tgpak_`/`tgpat_` prefixes, scopes
- [Weblate REST API](https://docs.weblate.org/en/latest/api.html) — DRF pagination, `X-RateLimit-*` headers, Project→Component hierarchy
- [Weblate Docker deployment](https://docs.weblate.org/en/latest/admin/install/docker.html)
- [Weblate JSON format](https://docs.weblate.org/en/latest/formats/json.html) — flat JSON vs `json-nested`
- [Microsoft.Extensions.Http.Resilience](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience) — built-in Polly-based pipelines
- [.NET 10 Blazor what's new](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0) — `PersistentComponentState`, culture flow fix
