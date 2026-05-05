# Blazor Localization — Task Checklist

Last Updated: 2026-04-14 (plan audit — 57/73 tasks)

> **How this checklist is organised.** Work is delivered in three sliceable drops — **Slice A** stabilises the existing implementation, **Slice B** makes the product operable (admin UI + bundle persistence + MudLocalizer), **Slice C** hardens for production (resilience, observability, docs). **Tests live inside the slice that introduces the code** (Phase 9 is a registry, not a terminal phase). Task IDs are stable; the slice tag tells you *when* a task ships.
>
> See `blazor-localization-plan.md → Delivery Slices` for scope, duration, and exit criteria per slice.

## Status by Slice

| Slice | Theme | Phases | Duration | Cumulative |
|-------|-------|--------|----------|------------|
| **A** | Stabilize What Exists | 0, 1, 2, 5 (+ in-slice tests) | ~2.5 days | 2.5 d |
| **B** | Make the Product Operable | 3, 4, 6 (+ in-slice tests) | ~3.5 days | 6.0 d |
| **C** | Enterprise Hardening | 7, 8, 10 (+ integration/arch tests) | ~2.0 days | 8.0 d |

## Status Summary

| Phase | Description | Slice | Status | Tasks |
|-------|-------------|-------|--------|-------|
| 0 | Audit & Architecture Anchors | A | ✅ COMPLETE | 3/3 |
| 1 | Enterprise Quality Pass (existing components) | A | 🟡 IN PROGRESS | 13/15 |
| 2 | BFF Language Persistence | A | ✅ COMPLETE | 4/4 |
| 3 | Bundle Export Persistence Fix | B | ✅ COMPLETE | 6/6 |
| 4 | Localization Admin UI | B | ✅ COMPLETE | 12/12 |
| 5 | Populate Starter Bundles (manual curation) | A | ⏳ NOT STARTED | 0/3 |
| 6 | Native .NET CultureInfo + MudLocalizer | B | 🟡 IN PROGRESS | 4/5 |
| 7 | TMS Provider Resilience | C | 🟡 IN PROGRESS | 5/6 |
| 8 | Observability & Error Tracking | C | 🟡 IN PROGRESS | 3/4 |
| 9 | Test Registry *(distributed across slices)* | A+B+C | 🟡 IN PROGRESS | 2/8 |
| 10 | Documentation & Rollout (incl. tech-debt tickets) | C | 🟡 IN PROGRESS | 5/7 |
| **Total** | | | | **57/73** |

---

## Phase 0 — Slice A · Audit & Architecture Anchors ✅ COMPLETE

**Objective**: Lock in the reality documented in `blazor-localization-plan.md` and add architecture-level guard rails so future work cannot drift away from the patterns agreed here.

- [x] **0.1** Add architecture test: components may not inject `IEventApiClient`
  - File: `Event.Architecture.Tests/BlazorClientArchitectureTests.cs` (extend existing or add new)
  - Rule: any `.razor` component under `Explore.Blazor.Client/Pages/**` or `Explore.Blazor.Client/Shared/**` must NOT declare a field or `@inject` of type `IEventApiClient`; calls must go through a service in `Services/`
  - Acceptance: test passes with current codebase (fail if any component directly injects the NSwag client)

- [x] **0.2** Add architecture test: `ITranslationService` implementations must live in `Services/`
  - File: same as 0.1
  - Rule: types implementing `ITranslationService` must reside in namespace `Explore.Blazor.Client.Services`
  - Acceptance: test passes with current `TranslationService.cs`

- [x] **0.3** Smoke test: `RuntimeTranslationProvider` falls back to offline on exception
  - File: `Event.Application.UnitTests/Infrastructure/Localization/RuntimeTranslationProviderFallbackTests.cs` (new) — TUnit
  - Scenario: mock `TolgeeTranslationProvider` to throw `HttpRequestException` on `ExportTranslationsAsync`; verify `RuntimeTranslationProvider` returns offline bundle without bubbling the exception
  - Acceptance: test exists and passes; failure mode documented in output

---

## Phase 1 — Slice A · Enterprise Quality Pass on Existing Components 🟡 IN PROGRESS (13/15)

**Objective**: Close code-quality, a11y, and safety gaps in files that already exist without rewriting them, and introduce the **Language Governance Model** primitives (Culture Registry, enabled-languages governance, kill-switches) into the platform without depending on runtime TMS discovery.

> **Location rule.** Shared localization primitives live in `Explore.Application/Common/Localization/`, **not** `Explore.Blazor.Client/Constants/`. The `Explore.Blazor` server host must not depend on the `Explore.Blazor.Client` project just to reuse a constant (see plan D12).

- [x] **1.1** Create shared `RtlLanguages` helper in the Application layer
  - File: `Explore.Application/Common/Localization/RtlLanguages.cs` (NEW)
  - Content: `public static class RtlLanguages { public static bool IsRtl(string code) => CultureRegistry.TryGetEntry(code, out var entry) && entry.IsRtl; }`
  - ABOUTME header required (2 lines starting with `ABOUTME:`)
  - Notes: thin shim over the registry — one source of truth for "which code is RTL?"
  - Acceptance: file exists, `dotnet build` green, unit test covers `ar`/`he`/`fa`/`ur` → true and `en`/`fr`/unknown → false

- [x] **1.1.5** Create `CultureRegistry` + `CultureEntry` (single source of truth for supported cultures) **NEW**
  - Files:
    - `Explore.Application/Common/Localization/CultureEntry.cs` (NEW) — record: `public record CultureEntry(string Code, string DisplayName, string NativeName, string Flag, bool IsRtl);`
    - `Explore.Application/Common/Localization/CultureRegistry.cs` (NEW) — static list of all cultures the codebase knows how to handle; initial set = `en`, `fr`, `ar`
    - Methods: `IReadOnlyList<CultureEntry> GetAll()`, `bool TryGetEntry(string code, out CultureEntry entry)`, `bool Contains(string code)`, `string Normalize(string code)` (lowercased + trimmed, returns `""` for invalid)
  - Rule: registry is **compile-time**; never touches the DB, never hits the TMS. It is the neutral allowlist that startup/culture middleware/picker/user-preference validation all consume.
  - ABOUTME headers required on both files
  - Acceptance: unit test `CultureRegistryTests` covers normalize + contains + TryGetEntry for `en`/`fr`/`ar` (valid) and `"EN-US"` (normalised to `en-us` → miss — we use bare 2-letter codes for v1) and `"<script>"` (rejected)

- [x] **1.2** Retire duplicate RTL lists in `App.razor` and `LanguageContext.cs`
  - Files: `Explore.Blazor/Components/App.razor`, `Explore.Blazor.Client/Models/LanguageContext.cs`
  - Change: both files reference `Explore.Application.Common.Localization.RtlLanguages.IsRtl(code)` instead of their local HashSet
  - Acceptance: both local `_rtlLanguages`/`RtlLanguages` fields removed; build + all existing tests still pass

- [ ] **1.3** Harden `App.razor` language cookie validation with the registry (no regex)
  - File: `Explore.Blazor/Components/App.razor`
  - Change: validate `langCookie` via `CultureRegistry.TryGetEntry(normalized, out var entry)` **before** using it for `<html lang dir>`; invalid cookie → default to `localization.default_language` governance value (via scoped resolver), never trust raw user input
  - Explicitly **remove** the naive `^[a-z]{2}(-[a-z]{2})?$` regex approach — regex validation is a maintenance trap and cannot distinguish "valid culture" from "valid code shape"
  - Acceptance: unit/integration test covers a malicious cookie (`"zh'; DROP TABLE"`, `"<script>"`, `"fr_FR"`, `"EN-US"`, empty string) and verifies each is discarded in favour of the governance default

- [x] **1.4** Create `ILanguagePreferenceService` wrapper
  - Files:
    - `Explore.Blazor.Client/Contracts/Services/ILanguagePreferenceService.cs` (NEW)
    - `Explore.Blazor.Client/Services/LanguagePreferenceService.cs` (NEW)
  - Contract: `Task<bool> SetLanguageAsync(string languageCode, CancellationToken ct = default)`
  - Impl: injects `IHttpClientFactory`, creates `"BffClient"`, calls `POST /bff/language?lang={code}`, logs on failure, returns success bool. Validates input against `CultureRegistry` first; rejects unknown codes without calling the BFF.
  - DI: register in `ServiceCollectionExtensions.cs` as scoped
  - ABOUTME headers required
  - Acceptance: `LanguagePicker.razor` consumes the new service instead of calling `HttpClientFactory` directly

- [x] **1.5** Fix `LanguageProvider.razor` interface + async correctness + announcement
  - File: `Explore.Blazor.Client/Providers/LanguageProvider.razor`
  - Changes:
    - Declare `@implements IDisposable` at the top of the Razor file
    - Change `private async void HandleLanguageChanged(...)` → `private async Task HandleLanguageChangedAsync(...)` wrapped via `InvokeAsync` inside an `Action<string>` shim
    - Inject `IAccessibilityAnnouncerService` and call `AnnouncePoliteAsync($"Language changed to {_languageContext.LanguageName}")` after a successful change
  - Acceptance: no `async void` in the component; `Dispose` unsubscribes correctly; announce fires on change (verified in in-slice bUnit test 9.3)

- [x] **1.6** Upgrade `LanguagePicker.razor` accessibility + service layer + kill-switch visibility
  - File: `Explore.Blazor.Client/Shared/LanguagePicker.razor`
  - Changes:
    - Add `aria-label="@(string.Format(T("ui.picker.language.aria", "Change language. Current: {0}"), Language?.LanguageName))"` on the `AppButton`
    - Replace direct `IHttpClientFactory` usage with `ILanguagePreferenceService` (injected from task 1.4)
    - Inject `ISnackbar` and show success/failure feedback
    - Inject `IAccessibilityAnnouncerService` for announce-on-change
    - Move hardcoded URL `"/bff/language"` into `LanguagePreferenceService` as a `private const string`
    - **Kill-switch**: consume `TranslationConfiguration.ClientPickerEnabled` via `ITranslationConfigResolver`; if `false`, the picker renders nothing (returns early from BuildRenderTree). No visual placeholder — the feature is genuinely hidden when disabled.
  - Acceptance: manual keyboard test (Tab to button, Enter opens menu, Arrow keys navigate, Enter selects, Escape closes); automated in-slice test (9.2); kill-switch hides component in test with `ClientPickerEnabled = false`

- [x] **1.7** Create `LanguagePicker.razor.css` with BEM + logical properties
  - File: `Explore.Blazor.Client/Shared/LanguagePicker.razor.css` (NEW)
  - Content: BEM blocks `.language-picker`, `.language-picker__button`, `.language-picker__menu`, `.language-picker__item`, `.language-picker__item--active`
  - Constraints: **logical properties only** (`margin-inline-start`, `padding-inline-end`, `inset-inline-start`, `text-align: start`) per `docs/ACCESSIBILITY.md` PR-4
  - Focus ring via `--isl-focus-ring-width`/`--isl-focus-ring-offset` tokens
  - Target size ≥ 24×24 CSS px
  - ABOUTME header required
  - Acceptance: visual QA in Chrome (LTR) and Arabic Chrome (RTL) shows correct mirroring without physical properties leaking

- [x] **1.8** Instrument `TranslationService` **at fetch/change-language boundaries only** (no hot-path metrics)
  - File: `Explore.Blazor.Client/Services/TranslationService.cs`
  - Allowed instrumentation:
    - Wrap `GetTranslationsAsync` body in a `Stopwatch`; observe `islamu_translation_fetch_duration_seconds{provider, language}` histogram (both success and failure paths)
    - On `GetTranslationsAsync` exit, increment `islamu_translation_fetch_total{provider, language, result}` with `result ∈ {"hit_cache","hit_api","error"}`
    - On `ChangeLanguageAsync` entry, increment `islamu_translation_change_language_total{from, to}`
    - Wrap both methods in a logger scope carrying `Language`, `CacheState`, and (if available) `CorrelationId`
  - **Forbidden instrumentation** (per plan D8):
    - Do NOT wrap `T(key)` in metrics, spans, or structured logs
    - Do NOT count cache hits/misses on the in-memory dictionary lookup (it is a hot path and must stay allocation-free)
    - If you absolutely need to trace a missing key, a `LogDebug` with 1/N sampling is the only allowed form
  - Also: add `CultureRegistry` validation — reject unknown language codes with a logged warning and return an empty dictionary (do NOT poison cache)
  - Acceptance: in-slice test (9.1) asserts metric increments at fetch/change boundaries AND asserts that calling `T(key)` 1000× produces **zero** metric observations

- [x] **1.9** Seed new localization governance keys **NEW**
  - File: `Explore.Persistence/Seed/LookupTableSeeder.cs` (extend existing `localization.*` seeds at lines ~350–355)
  - Add 4 new `SystemSetting` rows:
    - `localization.enabled_languages` — String, default `"en,fr,ar"`, description: "Comma-separated culture codes that admins have enabled for the instance."
    - `localization.fallback_language` — String, default `"en"`, description: "Used when user's preferred language is not enabled or has no translations."
    - `localization.client_picker_enabled` — String (bool as `"true"`/`"false"`), default `"true"`, description: "Kill-switch: hide language picker if false."
    - `localization.force_offline_mode` — String, default `"false"`, description: "Emergency toggle: force `RuntimeTranslationProvider` to route through `OfflineTranslationProvider` regardless of `tms_provider`."
  - Use the next available seed IDs following the existing 560-series range
  - Acceptance: migration runs cleanly, all 4 keys present in `SystemSettings` table after seed; duplicate-seed runs are idempotent

- [x] **1.10** Extend `TranslationConfiguration` record + `TranslationConfigResolver` **NEW**
  - Files:
    - `Explore.Application/Contracts/Infrastructure/ITranslationConfigResolver.cs` (extend `TranslationConfiguration` record)
    - `Explore.Infrastructure/Localization/TranslationConfigResolver.cs` (extend `ResolveAsync`)
  - New properties on `TranslationConfiguration`:
    - `IReadOnlyList<string> EnabledLanguages` (parsed from `localization.enabled_languages` → normalised via `CultureRegistry`; items not in the registry are silently dropped with a warning log)
    - `string FallbackLanguage` (from `localization.fallback_language`, must be in `CultureRegistry`, otherwise falls back to `"en"`)
    - `bool ClientPickerEnabled` (parsed `localization.client_picker_enabled`, default `true` on parse failure)
    - `bool ForceOfflineMode` (parsed `localization.force_offline_mode`, default `false` on parse failure)
  - Keep the existing 5-min cache TTL; invalidate on any governance write from the admin UI
  - Acceptance: in-slice unit test `TranslationConfigResolverTests` covers happy path, invalid CSV entries filtered out, unknown fallback falls back to `en`, bool parse failures default to safe values

- [x] **1.11** Enforce `force_offline_mode` in `RuntimeTranslationProvider` **NEW**
  - File: `Explore.Infrastructure/Localization/RuntimeTranslationProvider.cs`
  - Change: at the top of `ResolveProviderAsync()` (or equivalent), short-circuit to the `OfflineTranslationProvider` when `config.ForceOfflineMode == true`, logging `LogWarning("[LOCALIZATION] force_offline_mode active, bypassing {Provider}", config.Provider)`
  - Cache impact: the provider-mode component of the cache key (see D14) ensures that entries produced while force-offline is on do not contaminate entries produced when it is off (and vice versa)
  - Acceptance: in-slice integration test flips the setting, verifies provider selection changes without a restart, verifies cache keys differ before and after

- [ ] **1.12** Validate user-selected language against `CultureRegistry ∩ EnabledLanguages` **NEW**
  - File: `Explore.Blazor.Client/Services/LanguagePreferenceService.cs` (created in 1.4)
  - Change: before calling `POST /bff/language`, resolve the current `TranslationConfiguration` from an injected `ITranslationConfigResolver`; reject any code not in the intersection; return `false` with a user-facing error via `ISnackbar`
  - Acceptance: unit test sends an unenabled code and verifies the HTTP call never fires

- [x] **1.13** Populate `_languageContext` flag field from `CultureRegistry` (replace hardcoded dictionary)
  - File: `Explore.Blazor.Client/Models/LanguageContext.cs`
  - Change: drop the local `LanguageFlags`/`LanguageNames`/`RtlLanguages` static dictionaries; `ForLanguage(code)` now delegates to `CultureRegistry.TryGetEntry(code, out var entry)`, copying `DisplayName`, `NativeName`, `Flag`, `IsRtl`
  - Acceptance: `LanguageContext.ForLanguage("ar").Flag == "🇸🇦"` (assuming registry seeds it); building fails if the test reference isn't in the registry, forcing registry updates to ride along with UI updates

- [x] **1.14** Cache variation tuple: update cache key format in `TranslationResolver` to include provider mode **NEW**
  - File: `Explore.Infrastructure/Localization/TranslationResolver.cs`
  - Change: cache key becomes `Translation:{tenantId}:{languageCode}:{mode}` where `mode ∈ {"live","offline"}`, resolved from the provider currently in effect (considering `ForceOfflineMode`)
  - Invalidation: `InvalidateLanguage(languageCode)` must clear **both** mode variants
  - Acceptance: in-slice test verifies two different entries exist for the same `(tenant, lang)` under different modes, and that a single invalidation clears both

### Phase 1 Tests (in-slice → ship with Phase 1, NOT in Phase 9)
- **Slice A** owns: `CultureRegistryTests`, `RtlLanguagesTests`, `TranslationServiceTests` (9.1), `LanguagePickerTests` (9.2), `LanguageProviderTests` (9.3), `TranslationConfigResolverTests`, `RuntimeTranslationProviderFallbackTests` (0.3), `LanguagePreferenceServiceTests`, `BffPreferenceEndpointsTests` (language subset).

---

## Phase 2 — Slice A · BFF Language Persistence for Authenticated Users ✅ COMPLETE

**Objective**: Bring `/bff/language` to feature parity with `/bff/theme` and `/bff/direction` so authenticated users' language persists across devices.

- [x] **2.1** Extend `UpdateUserAppearancePreferencesDto` with `Language`
  - File: `Explore.Application/DTOs/Appearance/UpdateUserAppearancePreferencesDto.cs`
  - Add: `public string Language { get; set; } = "en";`
  - Update ABOUTME comment to reflect the expanded contract
  - **Acknowledged debt**: language is semantically not "appearance"; accepted for v1 for delivery speed. Refactor into `UpdateUserPreferencesDto` (or similar) is tracked under task **10.5**.
  - Acceptance: NSwag regenerates the DTO on next Blazor.Client build

- [x] **2.2** Update API `PUT api/user/appearance` handler to persist `Language`
  - Files: whichever controller + MediatR handler currently handles `/api/user/appearance` (locate via grep for `api/user/appearance`)
  - Changes:
    - Read `Language` from DTO
    - Validate against `CultureRegistry ∩ EnabledLanguages` (reject unknown/unenabled codes with `ProblemDetails` 400)
    - Persist to the user record / user preference table
    - Invalidate any user-scoped cache
  - Acceptance: unit test confirms persistence; integration test on the API round-trip confirms the Language field survives

- [x] **2.3** Rewrite `HandleLanguagePreference` in `BffPreferenceEndpoints.cs`
  - File: `Explore.Blazor/Extensions/BffPreferenceEndpoints.cs` lines 105–126
  - Changes:
    - Replace the existing 2–5 char check with a `CultureRegistry ∩ EnabledLanguages` allowlist check (resolve `TranslationConfiguration` via scoped `ITranslationConfigResolver`)
    - Convert to async signature: `private static async Task<IResult> HandleLanguagePreference(HttpContext ctx, CancellationToken cancellationToken)`
    - If authenticated, read current theme + direction from cookies, build full `UpdateUserAppearancePreferencesDto { ThemeMode, Direction, Language }`, call `PUT api/user/appearance` via `BffClient` named HttpClient (match `/bff/theme` pattern)
    - Always write BOTH cookies: `lang` (existing via `PersistLanguageCookie`) and `.AspNetCore.Culture` (new via `CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(code))`)
    - Return `Ok(new UserAppearancePreferencesDto { ThemeMode, Direction, Language = lang })`
  - Add helper: `PersistAspNetCoreCultureCookie(HttpContext, string languageCode)` matching the style of `PersistThemeCookie`
  - Acceptance: in-slice integration test hits `/bff/language?lang=fr` as an authenticated user, verifies both cookies are set AND the API round-trip is made; a second test verifies a rejected unenabled code does not set either cookie

- [x] **2.4** Wire `UseRequestLocalization()` in both API and Blazor Program.cs
  - Files: `Explore.API/Program.cs`, `Explore.Blazor/Program.cs`
  - Changes:
    - `builder.Services.AddLocalization()`
    - `builder.Services.Configure<RequestLocalizationOptions>(opts => { var cultures = CultureRegistry.GetAll().Select(e => new CultureInfo(e.Code)).ToArray(); opts.SupportedCultures = cultures; opts.SupportedUICultures = cultures; opts.DefaultRequestCulture = new RequestCulture("en"); opts.RequestCultureProviders.Clear(); opts.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider()); });`
    - `app.UseRequestLocalization();` positioned AFTER authentication but BEFORE endpoint routing (confirm ordering against middleware docs in `docs/ARCHITECTURE.md`)
  - Note: supported cultures come from the **compile-time** `CultureRegistry`, never from the runtime TMS. Enabling/disabling cultures at runtime is the job of `enabled_languages`, not of startup wiring.
  - Acceptance: setting `.AspNetCore.Culture` cookie to `c=fr|uic=fr` changes `CultureInfo.CurrentCulture` in the request pipeline (verified via integration test hitting a simple echo endpoint)

### Phase 2 Tests (in-slice → ship with Phase 2)
- **Slice A** owns: `BffPreferenceEndpointsTests` (language subset with auth + non-auth + unenabled code), API `UpdateUserAppearanceHandlerTests` (new Language field coverage), Blazor integration smoke test for `UseRequestLocalization` with cookie.

---

## Phase 3 — Slice B · Bundle Export Persistence Fix ✅ COMPLETE

**Objective**: Make "Export from TMS" actually persist bundle JSON to disk and refresh the offline fallback, while being honest about HA constraints.

> **HA constraint (must be explicit in ops docs).** `App_Data/Localization/Bundles/` is a **local filesystem path**. It is correct for (1) single-instance deployments and (2) multi-instance deployments with a shared persistent volume. It is **not** HA-safe behind a load balancer without shared storage. `IBundleFileWriter` is the seam where a `DistributedBundleFileWriter` (S3/blob/shared-cache) can ship post-v1 without touching call sites.

- [x] **3.1** Add `IBundleFileWriter` contract with health check **REVISED**
  - File: `Explore.Application/Contracts/Infrastructure/IBundleFileWriter.cs` (NEW)
  - Contract:
    ```csharp
    public interface IBundleFileWriter {
        Task<string> WriteBundleAsync(string languageCode, IReadOnlyDictionary<string, string> translations, CancellationToken ct = default);
        Task<WritablePathHealth> CheckHealthAsync(CancellationToken ct = default);
    }
    public record WritablePathHealth(bool Exists, bool Writable, string? Reason, string TargetPath);
    public class BundleWriteException(string message, Exception? inner = null) : Exception(message, inner);
    ```
  - Returns: `WriteBundleAsync` → absolute file path written. `CheckHealthAsync` → health record the admin UI surfaces as a banner when `!Writable`.
  - ABOUTME header required
  - Acceptance: compiles, used in Phase 3 tasks below

- [x] **3.2** Implement `BundleFileWriter` — safe defaults, no relaxed escaping **REVISED**
  - File: `Explore.Infrastructure/Localization/BundleFileWriter.cs` (NEW)
  - Impl:
    - Inject `IWebHostEnvironment` to get `ContentRootPath`
    - Target dir: `Path.Combine(env.ContentRootPath, "App_Data", "Localization", "Bundles")`
    - Create dir if missing via `Directory.CreateDirectory`
    - **Serializer options**: `new JsonSerializerOptions { WriteIndented = true }` — nothing else. **Do NOT set `UnsafeRelaxedJsonEscaping`.** Default UTF-8 is correct and safe for Arabic, Hebrew, and all BMP characters; relaxed escaping exists for HTML-embedding scenarios and is not a bundle concern.
    - Write atomically: serialize to `{code}.json.tmp` → `File.Move({code}.json.tmp, {code}.json, overwrite: true)`
    - `CheckHealthAsync`: probe `Directory.Exists`, attempt a zero-byte `.healthcheck.tmp` write-delete, return `WritablePathHealth` with the outcome
    - Log `Information` on success with path; log `Error` and throw `BundleWriteException` on failure
  - Register in `InfrastructureServicesRegistration.cs` as **scoped**: `services.AddScoped<IBundleFileWriter, BundleFileWriter>();`
  - ABOUTME header required
  - Acceptance: in-slice unit test writes a bundle with Arabic content to a temp dir, re-reads it, asserts content + path + that `\u` escapes are NOT used

- [x] **3.3** Teach `OfflineTranslationProvider` to check writable dir first
  - File: `Explore.Infrastructure/Localization/OfflineTranslationProvider.cs`
  - Changes:
    - Inject `IWebHostEnvironment` (constructor parameter; if the provider must stay Singleton, use an `IServiceScopeFactory` and resolve `IWebHostEnvironment` from a scope)
    - In `LoadBundle(languageCode)`: check `{ContentRoot}/App_Data/Localization/Bundles/{code}.json` first via `File.Exists`; if present, deserialize it; otherwise fall back to embedded resource stream
    - Still thread-safe via `ConcurrentDictionary`
    - Add method `InvalidateLanguage(string languageCode)` that removes the cache entry for a specific language (callers use this after export)
  - Acceptance: in-slice integration test writes a test bundle to the writable dir, calls the provider, asserts it returns the file content; deletes the file and asserts fallback to embedded

- [x] **3.4** Rewrite `ExportFromTmsCommandHandler` to persist + invalidate
  - File: `Explore.Application/Features/Localization/Handlers/Commands/ExportFromTmsCommandHandler.cs`
  - Changes:
    - Inject `IBundleFileWriter` and `ITranslationResolver` (remove the currently unused injection)
    - Fetch exports (already done)
    - Convert `IEnumerable<TranslationExport>` → `Dictionary<string, string>` keyed by `KeyName`
    - Call `await _bundleFileWriter.WriteBundleAsync(request.LanguageCode, dict, ct)` → absolute path
    - Call `await _translationResolver.InvalidateLanguageAsync(request.LanguageCode, ct)` (see 3.5)
    - Return `BaseCommandResponse<Guid> { Success = true, Message = $"Exported {dict.Count} translations for '{request.LanguageCode}' → {path}", Id = Guid.NewGuid() }`
    - Catch `BundleWriteException` and return `Success = false` response with exception message
  - Acceptance: in-slice unit test mocks writer + resolver, verifies writer called once with correct dict, resolver invalidation called, response contains count + path

- [x] **3.5** Add `InvalidateLanguageAsync` to `ITranslationResolver`
  - Files:
    - `Explore.Application/Contracts/Infrastructure/ITranslationResolver.cs` — add method signature
    - `Explore.Infrastructure/Localization/TranslationResolver.cs` — implement: clear `IMemoryCache` entries for both cache-key variants `Translation:{tenantId}:{code}:live` and `Translation:{tenantId}:{code}:offline` for the current tenant (use the cache-key tuple introduced in 1.14)
  - Acceptance: in-slice unit test asserts both variants are cleared after invoke

- [x] **3.6** Document HA constraint + file backlog ticket for distributed writer **NEW**
  - Files:
    - `docs/LOCALIZATION.md` — add a "Bundle Persistence & HA Constraint" subsection: writable path, single-instance vs shared-volume, the `IBundleFileWriter` seam, the Admin UI health banner, the `dev/backlog/` ticket link
    - `dev/backlog/distributed-bundle-file-writer.md` (NEW) — describes the post-v1 `DistributedBundleFileWriter` (S3/blob/shared-cache) with problem statement, interface contract, candidate implementations, acceptance criteria
  - Acceptance: docs and backlog ticket exist; the backlog ticket is referenced from both `docs/LOCALIZATION.md` and the Admin UI health banner tooltip

### Phase 3 Tests (in-slice → ship with Phase 3)
- **Slice B** owns: `BundleFileWriterTests`, `OfflineTranslationProviderTests` (writable-dir variant), `ExportFromTmsCommandHandlerTests` (9.6), `TranslationResolverInvalidationTests`.

---

## Phase 4 — Slice B · Localization Admin UI ✅ COMPLETE

**Objective**: Ship the Blazor admin UI that makes the dual-provider abstraction operable — including kill-switch toggles, enabled-languages management, writable-path health banner, and secret lifecycle enforcement.

> **Controller naming note.** Per D17, the governance endpoint was placed on `LocalizationAdminController` for cohesion (not `InstanceOnboardingController` as originally planned in D15). The medium-term controller-split refactor is tracked as task **10.6**.

- [x] **4.1** Create `UpdateLocalizationGovernanceDto` + Command + Handler
  - Files:
    - `Explore.Application/DTOs/Localization/UpdateLocalizationGovernanceDto.cs` (NEW) — `{ string DefaultLanguage, string TmsProvider, string? TmsApiUrl, string? TmsProjectId, string? TmsComponent, string[] EnabledLanguages, string FallbackLanguage, bool ClientPickerEnabled, bool ForceOfflineMode }`
    - `Explore.Application/Features/Localization/Requests/Commands/UpdateLocalizationGovernanceCommand.cs` (NEW) — wraps the DTO, returns `BaseCommandResponse<Guid>`
    - `Explore.Application/Features/Localization/Handlers/Commands/UpdateLocalizationGovernanceCommandHandler.cs` (NEW) — validates manually (no DI), writes all 9 governance keys atomically, invalidates `ITranslationConfigResolver` cache
  - Validation rules:
    - `DefaultLanguage`: must be in `CultureRegistry` **and** in `EnabledLanguages`
    - `FallbackLanguage`: must be in `CultureRegistry`
    - `EnabledLanguages`: non-empty, every entry in `CultureRegistry` (drop unknowns with warning log)
    - `TmsProvider`: one of `"none"`, `"tolgee"`, `"weblate"`
    - If `TmsProvider != "none"`: `TmsApiUrl` required, must be absolute https:// (http:// allowed in Development)
    - If `TmsProvider == "tolgee"`: `TmsProjectId` required
    - If `TmsProvider == "weblate"`: `TmsProjectId` AND `TmsComponent` required
    - `ClientPickerEnabled`, `ForceOfflineMode`: validated as boolean primitives
  - ABOUTME headers required on all files
  - Acceptance: in-slice handler tests cover all validation branches + persistence + cache invalidation (task 9.7)

- [x] **4.2** Add `PUT /api/InstanceOnboarding/localization-governance` endpoint
  - File: `Explore.API/Controllers/InstanceOnboardingController.cs` (extend — follow the analytics-governance pattern in the same file)
  - Endpoint:
    ```csharp
    [HttpPut("localization-governance")]
    [Authorize(Roles = "InstanceAdmin")]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateLocalizationGovernance(
        [FromBody] UpdateLocalizationGovernanceDto dto,
        CancellationToken cancellationToken) { ... }
    ```
  - Acceptance: swagger.json regenerates on API build and includes the new endpoint; in-slice integration test hits it (task 9.8)

- [x] **4.3** Create `ILocalizationAdminService` + implementation
  - Files:
    - `Explore.Blazor.Client/Contracts/Services/ILocalizationAdminService.cs` (NEW)
    - `Explore.Blazor.Client/Services/LocalizationAdminService.cs` (NEW)
  - Contract methods:
    - `Task<LocalizationConfigDto?> GetConfigurationAsync(CancellationToken ct = default)` — also returns writable-path health (include on the DTO or as a separate call)
    - `Task<BaseCommandResponse<Guid>?> TestConnectionAsync(CancellationToken ct = default)`
    - `Task<BaseCommandResponse<Guid>?> ExportFromTmsAsync(string languageCode, CancellationToken ct = default)`
    - `Task<BaseCommandResponse<Guid>?> UpdateGovernanceAsync(UpdateLocalizationGovernanceDto dto, CancellationToken ct = default)`
    - `Task<WritablePathHealth?> GetBundlePathHealthAsync(CancellationToken ct = default)`
  - Impl pattern: match `FooterAdminService.cs` exactly — inject `IEventApiClient`, try/catch with logging, safe defaults (`null` or empty response on failure)
  - DI: register in `ServiceCollectionExtensions.cs` line ~78 as scoped
  - ABOUTME headers required
  - Acceptance: service compiles; in-slice unit tests cover each method's happy + sad path (task 9.4)

- [x] **4.4** Create `LocalizationAdminState` view model
  - File: `Explore.Blazor.Client/Models/Admin/LocalizationAdminState.cs` (NEW)
  - Contents:
    - Properties matching the form fields: `SelectedProvider`, `DefaultLanguage`, `TmsApiUrl`, `TmsProjectId`, `TmsComponent`, `TmsApiKey` (transient, never round-trips), `EnabledLanguages` (list), `FallbackLanguage`, `ClientPickerEnabled`, `ForceOfflineMode`, `TmsApiKeyConfigured` (bool from server; raw value never sent back)
    - Computed: `IsTolgee`, `IsWeblate`, `IsOffline`, `IsDirty`, `IsConnectionTestRunning`, `LastTestResult`
    - `Reset()`, `LoadFrom(LocalizationConfigDto)`, `Validate()` helpers
  - ABOUTME header required
  - Acceptance: type compiles; used by the section component in 4.5

- [x] **4.5** Create `InstanceLocalizationSection.razor` component (shell)
  - Files:
    - `Explore.Blazor.Client/Pages/Admin/Instance/Components/Sections/InstanceLocalizationSection.razor` (NEW)
    - `Explore.Blazor.Client/Pages/Admin/Instance/Components/Sections/InstanceLocalizationSection.razor.css` (NEW)
  - Sections:
    1. **Header card (`AppCard`)** — current provider badge, default language chip, connectivity status, language count, last-saved timestamp
    2. **Provider form** — `MudSelect<string>` for provider + conditional form fields (see plan D5) using `AppTextField<T>`, `MudTextField.InputType.Password` for API key
    3. **Action row** — `[Test Connection]` (`AppButton` primary text), `[Save]` (`AppButton` filled primary, disabled when `!IsDirty || !Validate()`)
    4. **Bundle management card** — per-language rows with key count, timestamp, `[Export & Persist]` button; gated on writable-path health (see 4.10)
    5. **Kill-switch card** — two toggles (see 4.6)
    6. **Enabled-languages card** — chip selector from `CultureRegistry` (see 4.7)
    7. **Danger zone** (collapsed `MudExpansionPanel`) — `[Reset to Offline-Only]` confirmation dialog via `AppDialogShell` + `DialogOptionsFactory.Confirmation()`
  - A11y requirements: `aria-label` on all buttons; spinner `role="status"` + polite announcement; focus save/restore before/after dialog via `IAccessibilityFocusService`; snackbar on every save/test/export
  - CSS: BEM blocks `.instance-localization`, `.instance-localization__header`, `.instance-localization__form`, `.instance-localization__bundle-card`, `.instance-localization__killswitch`, `.instance-localization__enabled-languages`; **logical properties only**
  - ABOUTME headers required on both files
  - Acceptance: manual QA — open `/admin/instance/settings`, navigate to Localization section, walk through provider-switch flow without keyboard traps; automated in-slice bUnit tests (task 9.5)

- [x] **4.6** Kill-switch card: `client_picker_enabled` + `force_offline_mode` toggles **NEW**
  - File: `InstanceLocalizationSection.razor` (kill-switch card region)
  - Behavior:
    - Two `MudSwitch<bool>` controls bound to `LocalizationAdminState.ClientPickerEnabled` and `LocalizationAdminState.ForceOfflineMode`
    - Each toggle is paired with a short description and a "Learn more" tooltip that explains the effect
    - Flipping `force_offline_mode = true` surfaces a yellow warning banner: "Force-offline is an emergency toggle. Users will see offline bundle translations only. Remember to disable when incident is resolved."
    - Changes are staged in `LocalizationAdminState`; only the global `[Save]` button commits them (preventing accidental instant-effect)
    - Emergency path: a second `[Save and Apply Kill-switches Now]` button is available specifically for kill-switches, bypassing the full form save, for the "TMS is on fire, I need this off" scenario
  - A11y: both toggles reachable via Tab, state announced on change via `IAccessibilityAnnouncerService.AnnounceAssertiveAsync` (this is safety-critical)
  - Acceptance: in-slice bUnit test flips each toggle, saves, verifies governance updates and `ITranslationConfigResolver` cache invalidation

- [x] **4.7** Enabled-languages chip selector **NEW**
  - File: `InstanceLocalizationSection.razor` (enabled-languages card region)
  - Behavior:
    - Render every `CultureEntry` from `CultureRegistry.GetAll()` as a toggleable chip showing flag + native name + code
    - Chips bound to `LocalizationAdminState.EnabledLanguages` (array of codes)
    - At least one chip must remain toggled on; disabling the last chip shows a validation error ("At least one language must be enabled")
    - `DefaultLanguage` and `FallbackLanguage` dropdowns filter to only the currently-enabled set; deselecting them here re-prompts the admin to pick a new default/fallback
  - A11y: chips are `role="button" aria-pressed="true|false"`, keyboard-navigable with Arrow keys + Space toggle
  - Acceptance: in-slice bUnit test selects/deselects chips, asserts validation, asserts default/fallback filtering

- [x] **4.8** Admin UI "Test Connection" sequence per provider
  - File: `Explore.Blazor.Client/Pages/Admin/Instance/Components/Sections/InstanceLocalizationSection.razor` (test button handler)
  - Behavior:
    - Disable button + show inline spinner
    - Call `LocalizationAdminService.TestConnectionAsync()` (which backs onto `POST /api/admin/localization/test-connection`)
    - On success: inline green `MudAlert` with configured provider + discovered language count
    - On failure: inline red `MudAlert` with provider-specific error:
      - Tolgee: "Invalid API key" / "Project not found" / "Insufficient permissions (requires keys.view)" / "Network error"
      - Weblate: "Invalid token" / "Project not found" / "Component not found" / "Network error"
    - Announce result via `AnnouncePoliteAsync`
  - Acceptance: in-slice bUnit test covers success and failure snackbars (task 9.5)

- [x] **4.9** Admin UI "Export & Persist" per-language button (gated on health)
  - File: `Explore.Blazor.Client/Pages/Admin/Instance/Components/Sections/InstanceLocalizationSection.razor` (bundle card handler)
  - Behavior:
    - Per-language button disabled while running (loading state)
    - **Button is additionally disabled** if `GetBundlePathHealthAsync()` returned `Writable == false`, with a tooltip pointing at the health banner (see 4.10)
    - Call `LocalizationAdminService.ExportFromTmsAsync(languageCode)` (backs onto `POST /api/admin/localization/export-from-tms`)
    - On success: show snackbar with count + file path; refresh the card (`StateHasChanged`) to show updated key count + timestamp
    - On failure: snackbar with error detail
  - Acceptance: in-slice bUnit test covers success and failure paths; manual QA verifies bundle file appears in `App_Data/Localization/Bundles/`

- [x] **4.10** Writable-path health banner **NEW**
  - File: `InstanceLocalizationSection.razor` (top of bundle management card)
  - Behavior:
    - On load, call `GetBundlePathHealthAsync()`
    - If `!Writable`: show a red `MudAlert` with the `Reason`, a truncated target path, and a link to the `dev/backlog/distributed-bundle-file-writer.md` ticket ("HA deployments require a shared volume or a distributed bundle writer — see backlog")
    - If `Writable` but target path looks non-persistent (e.g. `/tmp`, `/var/tmp`): show an amber banner warning that exports will be lost on restart
  - Acceptance: in-slice bUnit test with mocked writer health reports each state and verifies UI rendering + export-button gating

- [x] **4.11** Secret lifecycle enforcement (write-only, configured-badge, independent rotation) **NEW**
  - Files: `InstanceLocalizationSection.razor` + `LocalizationAdminService.cs`
  - Rules (per plan Enterprise Concerns → Security):
    - The API key field is `MudTextField.InputType.Password`, cleared to empty on every re-render; server never sends a current value back
    - A chip next to the field shows **"Configured"** (green) or **"Not configured"** (grey) based on a boolean returned from the server (no value, not even masked)
    - A dedicated `[Rotate API Key]` button opens a confirmation dialog: typing the new key here submits only that key change; it does **not** require the admin to re-enter other governance fields
    - A dedicated `[Clear API Key]` button with a confirmation dialog removes the secret entirely (provider falls back to offline until a new key is set)
    - On provider **change** (Tolgee→Weblate), the old secret is cleared automatically and the admin is prompted for the new one
  - Acceptance: in-slice bUnit test verifies: (a) field always renders empty on load, (b) chip reflects server-reported configured state, (c) rotate dialog only submits the key, (d) clear dialog confirms before calling the clear endpoint

- [x] **4.12** Dock new section into `InstanceAdminSettingsLayout.razor`
  - File: `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAdminSettingsLayout.razor`
  - Changes:
    - Add new nav item to `BuildNavItems(IsSingleTenantMode)`: `{ Group = "Content", Label = "Localization", Icon = Icons.Material.Rounded.Translate, IconColor = Color.Primary, SectionId = "localization" }`
    - Add new `case "localization": <InstanceLocalizationSection /> break;` to the section switch block
  - Acceptance: navigating to `/admin/instance/settings` and clicking the new sidebar item shows the section

### Phase 4 Tests (in-slice → ship with Phase 4)
- **Slice B** owns: `UpdateLocalizationGovernanceCommandHandlerTests` (9.7), `LocalizationAdminControllerTests` integration (9.8), `LocalizationAdminServiceTests` (9.4), `InstanceLocalizationSectionTests` bUnit (9.5, ≥15 scenarios covering kill-switches, chip selector, health banner, secret lifecycle).

---

## Phase 5 — Slice A · Populate Starter Bundles (Manual Curation) ⏳ NOT STARTED

**Objective**: Give self-hosters a working offline bundle out of the box. **Manual curation is authoritative**; auto-scrape is advisory only.

- [ ] **5.1** Manually curate the initial key set (~80–150 keys) **REVISED**
  - Scope: walk through `Explore.Blazor.Client/Layout/**`, `Shared/**`, `Pages/**/*.razor`, `Components/Common/**` by hand; record every user-visible string you want translated in v1
  - Output: `dev/active/blazor-localization/bundles-key-audit.md` — table with columns `Component Path | Source Line | Raw Text | Proposed Key | Notes`
  - Keys follow `ui.{area}.{component}.{element}` convention
  - Include `mudblazor.*` keys for Phase 6 at the same time so bundle shape stays consistent
  - Target: 80–150 **intentional** keys — not a dump of every string literal. Prefer quality over coverage for v1.
  - **Auto-scrape is advisory**: task 5.3 adds an architecture test that *reports* any missed strings, but the audit file remains the source of truth
  - Acceptance: audit file exists with at least 80 curated entries, reviewed for naming consistency; auto-scrape diff reviewed (not blocking)

- [ ] **5.2** Populate `en.json`, `fr.json`, `ar.json`
  - File: `Explore.Infrastructure/Localization/Bundles/{en,fr,ar}.json`
  - Source: keys from 5.1
  - Content:
    - English: direct strings, reviewed for tone/voice consistency
    - French: native speaker translation (solicit review if unsure); machine-assisted strings marked with `(review)` suffix for post-v1 cleanup
    - Arabic: native speaker translation with RTL-aware text; watch for mixed-direction strings (numbers inside Arabic text); use Unicode bidi controls only when truly necessary
  - Include MudBlazor keys from 5.1 (`mudblazor.mud_data_grid.*`, `mudblazor.mud_dialog.*`, `mudblazor.mud_date_range_picker.*`, `mudblazor.mud_pagination.*`, `mudblazor.mud_input_control.*`)
  - File format: flat JSON, pretty-printed (2-space indent), UTF-8 no BOM, Unicode escapes **not** used (direct characters)
  - Acceptance: `dotnet build` succeeds, all three files parse as valid JSON, key counts match across languages (±5 for language-specific items), Arabic renders correctly in a quick manual render test

- [ ] **5.3** Add **advisory** architecture test for hardcoded strings
  - File: `Event.Architecture.Tests/HardcodedStringsTests.cs` (NEW)
  - Rule: **advisory-only** (logs warnings, does not fail the build) — regex scan `.razor` files for string literals inside `<MudText>`, `<AppButton>`, `<MudButton>`, `<PageTitle>` that are NOT wrapped in `@T("...")` or `@context.Translate("...")`
  - Exclusions: pure-numeric/punctuation strings, empty strings, known-ok placeholders
  - Post-v1 plan: flip to blocking once the curated bundle set stabilises
  - Acceptance: test runs, produces a report; repo's current count becomes the initial budget baseline

### Phase 5 Tests (in-slice → ship with Phase 5)
- **Slice A** owns: `BundleContentTests` (parse all three files, assert equal key counts ±5, assert every key matches either `ui.*` or `mudblazor.*` convention).

---

## Phase 6 — Slice B · Native .NET CultureInfo + MudLocalizer 🟡 IN PROGRESS (4/5)

**Objective**: Activate native .NET culture formatting for dates/numbers and wire MudBlazor's built-in localizer so its strings match user language.

- [x] **6.1** Create `MudBlazorLocalizer` bridging MudLocalizer → ITranslationService
  - File: `Explore.Blazor.Client/Services/MudBlazorLocalizer.cs` (NEW)
  - Class signature: `public class MudBlazorLocalizer : MudLocalizer`
  - Override: `public override LocalizedString this[string key]`
  - Implementation:
    - Inject `ITranslationService` via constructor
    - Lookup: `_translations.T($"mudblazor.{key.ToLowerInvariant()}", fallback: key)`
    - Return `new LocalizedString(key, value, resourceNotFound: value == key)`
  - ABOUTME header required
  - Acceptance: compiles; in-slice unit test (9.9) verifies lookup pattern

- [x] **6.2** Register `MudLocalizer` in DI
  - File: `Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs`
  - Add: `services.AddTransient<MudLocalizer, MudBlazorLocalizer>();`
  - Position: after `AddScoped<ITranslationService, TranslationService>()`
  - Acceptance: MudBlazor data-grid headers in French bundle render in French after rebuild

- [ ] **6.3** Seed `mudblazor.*` keys into starter bundles (delegated to 5.1/5.2)
  - Status: Done as part of tasks 5.1 and 5.2 — this item remains here only as a reference marker so Slice B can confirm the dependency was satisfied by Slice A.
  - Acceptance: Slice B engineer verifies `mudblazor.*` keys exist in all three bundle files before running tests

- [x] **6.4** Wire `PersistentComponentState` for culture code
  - File: `Explore.Blazor.Client/Services/TranslationService.cs` (or new `CulturePersistenceService`)
  - Behavior:
    - On Server prerender: register a persist callback that writes `{ Language: _currentLanguage }` (culture code only — **not** the full translation dictionary) to `PersistentComponentState.PersistAsJson("culture-state", ...)`
    - On WASM startup: try-take from persistent state; if present, set `_currentLanguage` before the first render
  - This avoids the double-fetch scenario where WASM doesn't know the language until `LanguageProvider.OnAfterRenderAsync` runs
  - Acceptance: network-tab inspection shows only ONE `/api/translation/{lang}` call during Server→WASM hand-off (not two); payload size delta is minimal because only the code is persisted

- [x] **6.5** Set `CultureInfo.DefaultThreadCurrentCulture` on WASM startup
  - File: `Explore.Blazor.Client/Program.cs`
  - Change: read `.AspNetCore.Culture` cookie via JS interop at startup, validate against `CultureRegistry`, set both `CultureInfo.DefaultThreadCurrentCulture` and `DefaultThreadCurrentUICulture`, then `await builder.Build().RunAsync()`
  - Rationale: .NET 10 now respects this for WASM — dates/numbers format correctly without per-component culture wiring
  - Acceptance: on WASM with Arabic cookie, `DateTime.Now.ToString("d")` returns Arabic-formatted date

### Phase 6 Tests (in-slice → ship with Phase 6)
- **Slice B** owns: `MudBlazorLocalizerTests` (9.9), `CulturePersistenceTests`, `BlazorProgramCultureStartupTests`.

---

## Phase 7 — Slice C · TMS Provider Resilience 🟡 IN PROGRESS (5/6)

**Objective**: Harden the Tolgee and Weblate HttpClients with a **single** Polly-backed resilience pipeline per client (retry + circuit breaker + timeout + 429-aware backoff via stateless readers).

> **Design rule (per plan D7).** One pipeline per client. Custom code is **two stateless readers**, not DelegatingHandlers. Readers feed `DelayGenerator`; they do **not** execute retries. No double-retry layering.

- [x] **7.1** Add `Microsoft.Extensions.Http.Resilience` package reference
  - File: `Explore.Infrastructure/Explore.Infrastructure.csproj`
  - Add: `<PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.0.*" />` (or latest compatible with .NET 10)
  - Acceptance: `dotnet build` green; no version conflicts

- [x] **7.2** Create `TolgeeRetryAfterReader` (stateless helper, NOT a DelegatingHandler) **REVISED**
  - File: `Explore.Infrastructure/Localization/Resilience/TolgeeRetryAfterReader.cs` (NEW)
  - Class: `public static class TolgeeRetryAfterReader`
  - Signature: `public static async ValueTask<TimeSpan?> ReadDelayAsync(HttpResponseMessage response, CancellationToken ct)`
  - Behavior:
    - If `response.StatusCode != TooManyRequests`, return `null`
    - Try to deserialize body to `TolgeeRateLimitError { Message, RetryAfter (ms), Global }` record
    - Return `TimeSpan.FromMilliseconds(retryAfter)` capped at 60s
    - On parse failure: return `null` (the pipeline falls back to its default exponential backoff)
  - **Not a DelegatingHandler.** The reader is called from the pipeline's `DelayGenerator` in 7.4; it never executes the retry itself.
  - ABOUTME header required
  - Acceptance: in-slice unit test provides a mock 429 response with body `{"message":"...","retryAfter":2000,"global":false}` and asserts a 2-second delay is returned; malformed body returns null

- [x] **7.3** Create `WeblateRateLimitReader` (stateless helper, NOT a DelegatingHandler) **REVISED**
  - File: `Explore.Infrastructure/Localization/Resilience/WeblateRateLimitReader.cs` (NEW)
  - Class: `public static class WeblateRateLimitReader`
  - Signature: `public static TimeSpan? ReadDelay(HttpResponseMessage response)` (synchronous — header parsing only)
  - Behavior:
    - If `response.StatusCode != TooManyRequests`, return `null`
    - Read `X-RateLimit-Reset` header (Unix timestamp seconds)
    - Compute `wait = max(resetAt - now, 1s)`, cap at 60s
    - Read `X-RateLimit-Remaining` for observability logging via a side channel (the reader itself stays pure)
    - On missing header: return `null`
  - ABOUTME header required
  - Acceptance: in-slice unit test covers header parsing, cap, and missing-header fallback

- [x] **7.4** Wire the single resilience pipeline in `InfrastructureServicesRegistration.cs` **REVISED**
  - File: `Explore.Infrastructure/InfrastructureServicesRegistration.cs` lines 152–169
  - Changes (illustrative shape — Tolgee; same pattern for Weblate with `WeblateRateLimitReader`):
    ```csharp
    services.AddHttpClient<TolgeeTranslationProvider>()
        .AddResilienceHandler("tolgee-pipeline", builder =>
        {
            builder
                .AddTimeout(TimeSpan.FromSeconds(10))  // per-attempt
                .AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    UseJitter = true,
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = args => ValueTask.FromResult(
                        args.Outcome.Exception is HttpRequestException ||
                        (args.Outcome.Result is { StatusCode: HttpStatusCode.TooManyRequests
                                                             or HttpStatusCode.InternalServerError
                                                             or HttpStatusCode.BadGateway
                                                             or HttpStatusCode.ServiceUnavailable })),
                    DelayGenerator = async args =>
                    {
                        if (args.Outcome.Result is { } response)
                        {
                            var readerDelay = await TolgeeRetryAfterReader.ReadDelayAsync(response, args.Context.CancellationToken);
                            if (readerDelay is not null) return readerDelay.Value;
                        }
                        return null; // fall through to default exponential backoff
                    }
                })
                .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 5,
                    BreakDuration = TimeSpan.FromSeconds(30)
                })
                .AddTimeout(TimeSpan.FromSeconds(30));  // outer total-attempt timeout
        });
    ```
  - **Explicitly forbidden**: `AddHttpMessageHandler<SomeRetryingDelegatingHandler>()` alongside the pipeline's `AddRetry`. One retry source, not two.
  - Same shape for `WeblateTranslationProvider` using `WeblateRateLimitReader.ReadDelay(response)` (synchronous, no await needed)
  - Acceptance: builds; in-slice integration tests verify retry count + circuit-open behavior + timeout; a dedicated test verifies the pipeline performs **exactly** `MaxRetryAttempts` retries (never 2×)

- [ ] **7.5** Integration test: kill-container fallback
  - File: `Event.Persistence.IntegrationTests/Localization/ProviderFallbackTests.cs` (NEW) using Testcontainers
  - Scenario: spin up a dummy HTTP container on localhost acting as Tolgee; configure `TolgeeTranslationProvider` to point at it; kill the container mid-request; verify `RuntimeTranslationProvider` falls back to `OfflineTranslationProvider` within 30 seconds
  - Acceptance: test passes reliably in CI; teardown clean

- [x] **7.6** Add architecture tests enforcing the "single retry source" rule **NEW**
  - File: `Event.Architecture.Tests/LocalizationResilienceTests.cs` (NEW)
  - Rules:
    - No class named `*Handler` under `Explore.Infrastructure/Localization/Resilience/` (enforce Reader naming)
    - No type inheriting `DelegatingHandler` in `Explore.Infrastructure/Localization/Resilience/`
    - `AddHttpMessageHandler<>` is NOT used in `InfrastructureServicesRegistration.cs` for the Tolgee/Weblate HttpClient registrations (grep-style assertion on the source file or reflection over the service descriptors)
  - Acceptance: tests pass; future drift toward handler-based retries is blocked at the architecture layer

### Phase 7 Tests (in-slice → ship with Phase 7)
- **Slice C** owns: `TolgeeRetryAfterReaderTests`, `WeblateRateLimitReaderTests`, `ProviderFallbackTests` (7.5, Testcontainers), `LocalizationResilienceTests` (7.6, architecture).

---

## Phase 8 — Slice C · Observability & Error Tracking 🟡 IN PROGRESS (3/4)

**Objective**: Wire Prometheus metrics, Loki logs, and OpenTelemetry spans for the TMS stack — **at fetch/fallback/admin boundaries only, never on the `T(key)` hot path**.

- [x] **8.1** Extend `IMetricsCollector` with translation counters/histograms **REVISED**
  - Files:
    - `Explore.Application/Contracts/Infrastructure/IMetricsCollector.cs` (extend)
    - `Explore.Infrastructure/.../MetricsCollector.cs` (impl)
  - **Final metric list** (per plan D8 — note the deliberate absence of any `T(key)` hot-path counter):
    - Counter `islamu_translation_fetch_total{provider, language, result}` where `result ∈ {"hit_cache","hit_tms","hit_offline","error"}` — incremented only in `TranslationService.GetTranslationsAsync`, `TolgeeTranslationProvider.ExportTranslationsAsync`, `WeblateTranslationProvider.ExportTranslationsAsync`, `OfflineTranslationProvider.ExportTranslationsAsync`
    - Histogram `islamu_translation_fetch_duration_seconds{provider, language}` — same call sites
    - Counter `islamu_translation_change_language_total{from, to}` — incremented in `TranslationService.ChangeLanguageAsync`
    - Counter `islamu_tms_connection_test_total{provider, result}` — incremented only when an admin clicks `[Test Connection]` (i.e. in `TestTmsConnectionCommandHandler`)
    - Counter `islamu_tms_fallback_activated_total{provider, reason}` — **alertable** — incremented in `RuntimeTranslationProvider` catch blocks
    - Gauge `islamu_translation_cache_entries{scope}` — refreshed on a timer from `TranslationResolver` cache stats
  - Acceptance: metrics appear on `/metrics` endpoint after a few translation fetches; `T(key)` load test (1000 calls) produces zero metric observations

- [x] **8.2** Instrument `RuntimeTranslationProvider` with fallback counter
  - File: `Explore.Infrastructure/Localization/RuntimeTranslationProvider.cs`
  - Change: inject `IMetricsCollector`; on every exception-caught fallback, increment `islamu_tms_fallback_activated_total{provider, reason}` with `reason` derived from the exception type (`"timeout"`, `"auth_error"`, `"not_found"`, `"rate_limited"`, `"network_error"`, `"other"`)
  - Log at `Error` level (not `Warning`) so existing alerting rules fire; include `TenantId`, `Provider`, `Reason`, `CorrelationId` in the structured log
  - Acceptance: kill Tolgee → fallback ticks the counter; Loki query on `correlation_id` returns the incident line

- [x] **8.3** Instrument `TolgeeTranslationProvider` + `WeblateTranslationProvider` with fetch histogram
  - Files:
    - `Explore.Infrastructure/Localization/TolgeeTranslationProvider.cs`
    - `Explore.Infrastructure/Localization/WeblateTranslationProvider.cs`
  - Change: wrap `ExportTranslationsAsync` body in `Stopwatch.StartNew()`; observe histogram on exit (both success and failure paths)
  - Also increment `islamu_translation_fetch_total{provider, language, result}` with `result = "hit_tms"` on success, `"error"` on exception
  - Acceptance: Grafana dashboard (task 8.4) plots non-zero data

- [ ] **8.4** Create Grafana dashboard JSON
  - File: `docs/observability/dashboards/localization.json` (NEW)
  - Panels:
    - "Translation Fetch Latency (p50 / p95 / p99)" — histogram quantile from `islamu_translation_fetch_duration_seconds`
    - "Translation Fetches by Provider + Result" — stacked bar from `islamu_translation_fetch_total`
    - "Language Changes (24h)" — counter from `islamu_translation_change_language_total`
    - "TMS Test Connection Results" — pie from `islamu_tms_connection_test_total`
    - "TMS Fallback Activations (5m)" — single-stat from `increase(islamu_tms_fallback_activated_total[5m])` — alert if > 0
    - "Translation Cache Entries" — line from `islamu_translation_cache_entries`
  - Import JSON into local Grafana (`dotnet aspire` dev env) and screenshot
  - Acceptance: dashboard renders, all panels show data after synthetic load

### Phase 8 Tests (in-slice → ship with Phase 8)
- **Slice C** owns: `TranslationMetricsTests` (asserts counter/histogram increments exactly at the approved boundaries and **never** in `T(key)`), Grafana dashboard visual smoke test.

---

## Phase 9 — Test Registry *(distributed across Slices A/B/C)* 🟡 IN PROGRESS (2/8)

**Objective**: Single-place registry of the test files that must exist by the end of the program. **Each test ships inside its owning slice, not at the end.** This section exists so a reviewer can read "which tests are expected" in one place.

- [ ] **9.1 — Slice A** `TranslationServiceTests.cs` (Blazor.Client unit tests)
  - File: `Explore.Blazor.Client.Tests/Services/TranslationServiceTests.cs` (NEW)
  - Framework: TUnit + NSubstitute
  - Scenarios: `T` hit/miss/null, `GetTranslationsAsync` cold/warm/error/allowlist, `ChangeLanguageAsync` clears+fetches+fires, concurrent serialization via SemaphoreSlim, **metric increments on fetch boundaries only + zero increments on 1000 `T()` calls**
  - Coverage target: ≥ 90 % of `TranslationService.cs`

- [ ] **9.2 — Slice A** `LanguagePickerTests.cs` (bUnit component tests)
  - File: `Explore.Blazor.Client.Tests/Components/LanguagePickerTests.cs` (NEW)
  - Scenarios: render from mocked service, click → `ILanguagePreferenceService.SetLanguageAsync`, announce + snackbar on success/failure, keyboard navigation, `aria-label` presence, kill-switch hides component when `ClientPickerEnabled = false`

- [ ] **9.3 — Slice A** `LanguageProviderTests.cs` (bUnit provider tests)
  - File: `Explore.Blazor.Client.Tests/Providers/LanguageProviderTests.cs` (NEW)
  - Scenarios: initial language from mocked JS interop, cascades `LanguageContext`, responds to `OnLanguageChanged`, disposes subscription, `async Task` (not `async void`) verified by source analyser or behaviour test

- [ ] **9.4 — Slice B** `LocalizationAdminServiceTests.cs` (Blazor.Client unit tests)
  - File: `Explore.Blazor.Client.Tests/Services/LocalizationAdminServiceTests.cs` (NEW)
  - Scenarios for all 5 service methods (including `GetBundlePathHealthAsync`): happy path, API throws, 401/403, network error

- [ ] **9.5 — Slice B** `InstanceLocalizationSectionTests.cs` (bUnit component tests)
  - File: `Explore.Blazor.Client.Tests/Components/InstanceLocalizationSectionTests.cs` (NEW)
  - Scenarios: None/Tolgee/Weblate form rendering, provider swap without reload, Test Connection flow, Save gating, Export & Persist, Reset Confirmation, **kill-switch toggles save+apply**, **enabled-languages chip selector**, **writable-path health banner states**, **secret lifecycle (write-only, chip, rotate, clear)**, focus save/restore around dialogs

- [x] **9.6 — Slice B** `ExportFromTmsCommandHandlerTests.cs` (Application unit tests, extend existing)
  - New scenarios: `IBundleFileWriter.WriteBundleAsync` called exactly once with correct dict, `ITranslationResolver.InvalidateLanguageAsync` called, `BundleWriteException` handled, empty exports returns `Success = false`

- [x] **9.7 — Slice B** `UpdateLocalizationGovernanceCommandHandlerTests.cs` (Application unit tests)
  - File: `Event.Application.UnitTests/Infrastructure/Localization/UpdateLocalizationGovernanceCommandHandlerTests.cs` (NEW)
  - Scenarios: atomic write of all 9 governance keys, Weblate Component validation, Tolgee ignores Component, None clears TMS fields, `EnabledLanguages` validation + unknown-code filtering, cache invalidation, tenant isolation

- [ ] **9.8 — Slice B** `LocalizationAdminControllerTests.cs` (Integration tests)
  - File: `Event.API.IntegrationTests/Controllers/LocalizationAdminControllerTests.cs` (extend existing if present)
  - Scenarios: `GET configuration` (auth + returns current), `POST test-connection` success/failure, `POST export-from-tms` writes file, `PUT localization-governance` 200/400/403 paths

### Tests owned by Slice C (not numbered individually)
- Slice C also ships: `TolgeeRetryAfterReaderTests` (7.2), `WeblateRateLimitReaderTests` (7.3), `ProviderFallbackTests` (7.5, Testcontainers), `LocalizationResilienceTests` (7.6, architecture), `TranslationMetricsTests` (8.1).

---

## Phase 10 — Slice C · Documentation & Rollout 🟡 IN PROGRESS (5/7)

- [x] **10.1** Update `docs/LOCALIZATION.md`
  - Add sections:
    - "Blazor Client Integration" — links to `LanguageProvider`, `ITranslationService`, `LanguagePicker`, `MudBlazorLocalizer`, `CultureRegistry`
    - "Language Governance Model" — Culture Registry vs Enabled Languages vs Available Translations; governance keys; kill-switches; resolution order
    - "Offline Bundles — Runtime Writable Directory" — `App_Data/Localization/Bundles/` + embedded-resource fallback + `ExportFromTmsCommand` persistence + **HA constraint section** (single-instance or shared-volume only, backlog link to `DistributedBundleFileWriter`)
    - "Choosing Between Tolgee and Weblate" — comparison table, licensing, webhook availability
    - "Admin Workflow" — screenshots/placeholders of the admin UI flow, including kill-switches and health banner
    - "Observability" — metrics list + dashboard path + alerting thresholds
    - "Cache Variation" — explicit `Translation:{tenantId}:{languageCode}:{mode}` key format, invalidation rules
    - "Secret Lifecycle" — write-only, configured chip, independent rotation rules
  - Acceptance: doc matches shipped reality; a new developer can follow it end-to-end

- [x] **10.2** Update `docs/BLAZOR.md` and `docs/ACCESSIBILITY_ARTIFACTS.md`
  - `docs/BLAZOR.md`: add a `### Localization` subsection referencing `docs/LOCALIZATION.md`, `MudRTLProvider`, `MudBlazorLocalizer`, `CultureRegistry`
  - `docs/ACCESSIBILITY_ARTIFACTS.md`: add rows for the LanguagePicker, InstanceLocalizationSection, and kill-switch toggles with WCAG criteria satisfied (1.3.1, 1.4.3, 2.1.1, 2.4.7, 3.3.8, 4.1.3)
  - Acceptance: cross-links verified

- [x] **10.3** Update `docs/OPERATIONS.md` with alerts + runbooks
  - Add:
    - New Prometheus metrics + alert thresholds (`islamu_tms_fallback_activated_total > 0 in 5m` → page on-call)
    - Runbook: "TMS provider is down" — check Grafana → container logs → flip `localization.force_offline_mode` → investigate
    - Runbook: "Bundle file lost / corrupted" — use admin UI `[Export & Persist]` to re-seed
    - Runbook: "Writable path health banner red" — check deployment topology (single-instance vs shared-volume), check permissions on `App_Data/`, escalate to SRE if backlog ticket for `DistributedBundleFileWriter` is needed
    - Runbook: "API key rotation" — walk through the independent-rotation flow in the admin UI
  - Acceptance: runbooks peer-reviewed

- [ ] **10.4** Final housekeeping
  - Update this task file with all phases marked ✅ COMPLETED
  - Update `blazor-localization-context.md` session progress with implementation completion date
  - Move `dev/active/blazor-localization/` → `dev/done/blazor-localization/` (only with explicit user approval per repo convention)
  - Update root `dev/active/README.md` if any index entry needs adjustment
  - Acceptance: the working folder reflects the final shipped state

- [x] **10.5** Tech-debt ticket: split `UserAppearancePreferences` → `UserPreferences` **NEW**
  - File: `dev/backlog/user-preferences-split.md` (NEW)
  - Problem: `UpdateUserAppearancePreferencesDto.Language` is semantically wrong (language ≠ appearance). Accepted for v1 speed; needs to be split into a broader `UpdateUserPreferencesDto` (or `UpdateUserLanguagePreferencesDto`) in a future iteration.
  - Acceptance: ticket exists with problem statement, proposed API shape, migration plan (add new endpoint, dual-write for one release, deprecate old)

- [x] **10.6** Tech-debt ticket: split `InstanceOnboardingController` → `InstanceSettingsController` **NEW**
  - File: `dev/backlog/instance-settings-controller-split.md` (NEW)
  - Problem: `InstanceOnboardingController` now hosts ongoing-settings endpoints (analytics-governance, localization-governance) that aren't really "onboarding". Name is misleading.
  - Acceptance: ticket exists with problem statement, proposed split (`InstanceSetupController` for onboarding, `InstanceSettingsController` for ongoing admin), migration plan

- [ ] **10.7** Tech-debt ticket: `DistributedBundleFileWriter` **NEW**
  - File: `dev/backlog/distributed-bundle-file-writer.md` (created in task 3.6 — this entry finalises the ticket)
  - Problem statement: local filesystem `App_Data/Localization/Bundles/` is not HA-safe without shared storage; large deployments need a distributed backing store
  - Candidate implementations: S3/blob writer, shared-volume writer, `HybridCache`-backed distributed writer
  - Acceptance criteria: `IBundleFileWriter` contract unchanged, new implementation registered by deployment config

---

## Execution Notes

- **Build & test before every commit** per `CLAUDE.md`:
  ```bash
  dotnet build --configuration Release --verbosity quiet
  dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
  dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
  dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
  dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
  ```

- **Never** run destructive shell commands; if files need removal, report candidates at the end of the response.

- **Never** commit without explicit user request.

- **Never** skip pre-commit hooks.

- **ABOUTME headers required** on every new file (first two lines start with `ABOUTME:`).

- **File-scoped namespaces** on every new C# file.

- **TDD encouraged** where practical — write the test alongside the implementation in the same slice; the Test Registry (Phase 9) documents what's expected, but tests **ship with their feature slice**, not at the end.

- **Parallel execution within a slice** is allowed where dependencies permit; see `blazor-localization-plan.md → Delivery Slices` for the dependency graph.

- **Slice completion gate**: a slice is not done until (a) all its phase tasks are checked, (b) all its in-slice tests are green, (c) the `blazor-localization-context.md` session progress is updated, and (d) a manual smoke test of the new user-visible behaviour passes.

## Session Handoff — 2026-05-03 Europe/Brussels

- [x] No task-state changes were made for this workstream during the sidebar dock refactor handoff session.
- [ ] Reconfirm this workstream's current state from its existing context/plan before resuming implementation.
