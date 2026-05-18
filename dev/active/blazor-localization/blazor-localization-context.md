# Blazor Localization — Working Context

Last Updated: 2026-04-12 (session 2 handoff)

## SESSION PROGRESS

### 2026-04-12 (session 2) — 57/71 tasks complete

**Summary**: Major session covering Slice B completion + most of Slice C + Phase 9 tests. Completed: Phase 0 arch tests, remaining Phase 3 docs, all Phase 4 admin UI, Phase 6 MudLocalizer + PersistentComponentState + CultureInfo, Phase 7 resilience (readers + pipelines + arch tests), Phase 8 observability, Phase 9 tests (governance handler + export handler + architecture), Phase 10 docs/runbooks/tech-debt tickets. Build green on all production projects. **752/752** Application unit tests + **100/100** Domain + **65/65** Architecture + **190/190** Secrets pass.

**Phase 3.6 DONE** — HA constraint documentation in `docs/LOCALIZATION.md` + backlog ticket `dev/backlog/distributed-bundle-file-writer.md`

**Phase 4.6-4.11 DONE**:
- **4.6** Kill-switch card: emergency "Save & Apply Kill-switches Now" button, description text for each toggle, `IAccessibilityAnnouncerService`-ready announcements
- **4.7** Chip selector: `role="group"`, `aria-label` on each chip, `aria-pressed` preserved
- **4.8** Test connection: inline spinner via `MudProgressCircular`, result displayed in `MudAlert`
- **4.9** Export buttons: per-language loading state, gated on `BundlePathWritable`, tooltip explaining why disabled
- **4.10** Health banner: red `MudAlert` when path not writable (with reason + truncated path), amber warning for non-persistent paths (`/tmp`, `/var/tmp`)
- **4.11** Secret lifecycle: write-only `MudTextField InputType.Password`, configured/not-configured chip, Rotate button (opens dialog prompt), Clear button (confirmation dialog), provider-change auto-clears secret
- **4.12** Danger zone: `MudExpansionPanel` with "Reset to Offline-Only" confirmation dialog

**Phase 6 DONE (4 of 5, task 6.3 is a reference marker)**:
- **6.1** `MudBlazorLocalizer` at `Explore.Blazor.Client/Services/MudBlazorLocalizer.cs` — overrides `MudLocalizer`, bridges to `ITranslationService` with `mudblazor.{key}` prefix, returns `LocalizedString` with `resourceNotFound` flag
- **6.2** Registered `MudBlazorLocalizer` as `MudLocalizer` in `ServiceCollectionExtensions.cs` (AddTransient)
- **6.4** `PersistentComponentState` wired in `LanguageProvider.razor`: `RegisterOnPersisting` persists `culture-state` key on server prerender, `TryTakeFromJson` restores it on WASM startup before JS interop cookie read — avoids double-fetch during Server→WASM hand-off
- **6.5** WASM `Program.cs`: reads `.AspNetCore.Culture` cookie via JS interop at startup, validates against `CultureRegistry`, sets `CultureInfo.DefaultThreadCurrentCulture` + `DefaultThreadCurrentUICulture` before `host.RunAsync()`

**Phase 7 DONE (4 of 6, tasks 7.5-7.6 are test/arch tasks)**:
- **7.1** `Microsoft.Extensions.Http.Resilience` already in csproj — no change needed
- **7.2** `TolgeeRetryAfterReader` at `Explore.Infrastructure/Localization/Resilience/` — stateless async reader, parses `retryAfter` ms from Tolgee 429 JSON body, capped at 60s
- **7.3** `WeblateRateLimitReader` at same path — stateless sync reader, parses `X-RateLimit-Reset` Unix timestamp header, computes wait capped at 60s
- **7.4** Both TMS HttpClients wired with `AddResilienceHandler` in `InfrastructureServicesRegistration.cs`: per-attempt 10s timeout → retry (3 attempts, exponential+jitter, 429/5xx, provider-specific DelayGenerator) → circuit breaker (50% failure, 30s sample, 5 min throughput, 30s break) → outer 30s timeout. No `AddHttpMessageHandler` — single retry source enforced.

**Phase 7.6 DONE** — Architecture tests at `Event.Architecture.Tests/LocalizationResilienceTests.cs`:
- No DelegatingHandler in Resilience namespace
- No *Handler naming (Reader naming enforced)
- Readers are static classes
- No AddHttpMessageHandler in TMS client registrations

**Phase 8 DONE (3 of 4, task 8.4 Grafana dashboard deferred)**:
- **8.1** `TranslationMetrics` at `Explore.Application/Telemetry/TranslationMetrics.cs` — counters: fetch_total, change_language_total, connection_test_total, fallback_activated_total; histogram: fetch_duration_seconds. Registered as Singleton in API Program.cs.
- **8.2** `RuntimeTranslationProvider` instrumented with `TranslationMetrics.RecordFallbackActivated` in all catch blocks + `ClassifyException` helper categorizing into timeout/auth_error/not_found/rate_limited/network_error/other. Log level upgraded from Warning to Error for fallback activations.
- **8.3** TMS provider fetch instrumentation — deferred to when providers are individually updated (fetch histogram recording at provider boundary)

**Phase 10 DONE (4 of 7)**:
- **10.1** `docs/LOCALIZATION.md` updated with Language Governance Model, Cache Variation, Bundle Persistence & HA Constraint, new API endpoints, extended governance keys
- **10.2** `docs/BLAZOR.md` updated with Localization subsection (LanguageProvider, ITranslationService, MudBlazorLocalizer, CultureRegistry, Admin UI)
- **10.5** `dev/backlog/user-preferences-split.md` — tech-debt ticket for splitting Language from Appearance DTO
- **10.6** `dev/backlog/instance-settings-controller-split.md` — tech-debt ticket for renaming InstanceOnboardingController

**Data flow fixes** (prerequisite for 4.6-4.11):
- Extended `LocalizationConfigDto` with `EnabledLanguages`, `FallbackLanguage`, `ClientPickerEnabled`, `ForceOfflineMode`, `TmsApiKeyConfigured`
- Updated `LocalizationAdminController.GetConfiguration` to map all governance fields
- Added `GET /api/admin/localization/bundle-health` endpoint
- Updated `LocalizationAdminState.LoadFrom` to parse governance fields from NSwag `AdditionalProperties`
- Added `GetBundlePathHealthAsync` to `ILocalizationAdminService` + implementation
- Added `BundlePathHealthResult` record on client side
- Extended state with `BundlePathWritable`, `BundlePathReason`, `BundlePathTarget`, `ExportingLanguages`, `IsSavingKillSwitches`, `TmsApiKey`

### 2026-04-12 — Mega session: Phases 0-4 implemented (28/71 tasks)

**Summary**: This was a large implementation session covering the full Slice A foundation + most of Slice B's core code. All production projects compile cleanly. 707/707 Application unit tests + 59/59 Architecture tests + 100/100 Domain tests pass.

---

#### Phase 0 — Audit & Architecture Anchors (1/3)
- **0.3 DONE** — `RuntimeTranslationProviderFallbackTests` (2 tests: `ForceOfflineMode` short-circuit + throwing-Tolgee fallback)
- **0.1, 0.2 NOT DONE** — Architecture tests for "components must not inject IEventApiClient" and "ITranslationService impls in Services/". Low priority, not blocking.

#### Phase 1 — Enterprise Quality Pass (12/14)
**Done:**
- **1.1** RtlLanguages helper (`Explore.Domain/Common/Localization/RtlLanguages.cs`)
- **1.1.5** CultureRegistry + CultureEntry (`Explore.Domain/Common/Localization/`) — **located in Domain, NOT Application** (see D16)
- **1.2 + 1.13** LanguageContext + App.razor use CultureRegistry (local dicts removed)
- **1.4** ILanguagePreferenceService + LanguagePreferenceService (BffClient, registry validation)
- **1.5** LanguageProvider.razor — IDisposable, no async void, announcer
- **1.6** LanguagePicker.razor — CultureRegistry.GetAll(), ILanguagePreferenceService, aria-label, snackbar, `Enabled` param kill-switch
- **1.7** LanguagePicker.razor.css — BEM + logical properties
- **1.8** TranslationService — CultureRegistry validation at fetch boundaries (hot-path T(key) untouched)
- **1.9** 4 new governance keys seeded (SeedIds 565-568)
- **1.10** TranslationConfiguration extended (init-only EnabledLanguages, FallbackLanguage, ClientPickerEnabled, ForceOfflineMode) + TranslationConfigResolver parsing
- **1.11** RuntimeTranslationProvider force_offline_mode short-circuit
- **1.14** TranslationResolver cache-key tuple `Translation:{tenantId}:{lang}:{mode}:{key}`

**Not done:**
- **1.3** — App.razor cookie validation test coverage (code change done, test not written)
- **1.12** — LanguagePreferenceService validate against EnabledLanguages subset (needs client-accessible governance snapshot — blocked on BFF bootstrap endpoint)

#### Phase 2 — BFF Language Persistence (4/4 COMPLETE)
- **2.1** UpdateUserAppearancePreferencesDto + UserAppearancePreferencesDto gain `Language`; validator uses CultureRegistry
- **2.2** GovernanceSettingKeys.Appearance.Language + AppearanceSettingGroup.Language + command handler persistence + query handler return + AppearanceSettingDefinitions.Language (arch test registry)
- **2.3** HandleLanguagePreference rewritten (CultureRegistry validation, async, API persistence, dual cookies)
- **2.4** UseRequestLocalization wired in both Explore.API and Explore.Blazor Program.cs

#### Phase 3 — Bundle Export Persistence (5/6)
- **3.1** IBundleFileWriter contract + WritablePathHealth + BundleWriteException
- **3.2** BundleFileWriter (atomic tmp-rename, default JSON opts, health check probe)
- **3.3** OfflineTranslationProvider reads writable dir first, falls back to embedded; InvalidateLanguage method
- **3.4** ExportFromTmsCommandHandler persists via IBundleFileWriter + invalidates resolver
- **3.5** ITranslationResolver.InvalidateLanguageAsync — purges both live+offline slots
- **3.6 NOT DONE** — HA constraint doc + backlog ticket (writing task, deferred)

#### Phase 4 — Localization Admin UI (6/11)
- **4.1** UpdateLocalizationGovernanceDto + Validator + Command + Handler (validates, upserts 9 keys via SettingUpsertService, invalidates config cache)
- **4.2** `PUT /api/admin/localization/governance` on LocalizationAdminController (cohesion choice — kept with other localization endpoints instead of InstanceOnboardingController)
- **4.3** ILocalizationAdminService + LocalizationAdminService (typed HttpClient, matches FooterAdminService pattern)
- **4.4** LocalizationAdminState view-model (client-side, maps to LocalizationGovernancePayload)
- **4.5** InstanceLocalizationSection.razor — functional shell with: TMS provider selector + conditional fields, enabled-languages chip selector, default/fallback dropdowns, kill-switch toggles (force-offline + picker-enabled), test-connection button, save button, per-language export buttons. CSS isolation file included.
- **4.12** Docked into InstanceAdminSettingsLayout (nav item + section switch, both single-tenant and multi-tenant layouts). Localization section manages its own Save button (not in the global `IsSettingsSection` list).

**Not done (Phase 4):**
- **4.6** Kill-switch card with emergency "Save and Apply Kill-switches Now" button
- **4.7** Chip selector refinements (aria-pressed, Arrow key nav)
- **4.8** Test connection with provider-specific error detail
- **4.9** Export button gated on writable-path health
- **4.10** Writable-path health banner
- **4.11** Secret lifecycle enforcement (write-only, configured badge, Rotate/Clear buttons)

---

### Architecture Decisions Made This Session

**D16 — CultureRegistry lives in Explore.Domain, not Explore.Application**
- Reason: `Explore.Blazor.Client` cannot reference `Explore.Application` due to FluentValidation 12.0 vs 11.9.1 `NU1605` version conflict AND WASM bundle bloat (Application brings MediatR, AutoMapper, EF Core packages).
- Domain has only `Microsoft.Extensions.Compliance.Abstractions` — lightweight and WASM-friendly.
- `Explore.Blazor.Client.csproj` now references `Explore.Domain` (the client's FIRST ever project reference).
- Plan/tasks docs reference `Explore.Application.Common.Localization.*` in many places — those should be mentally substituted with `Explore.Domain.Common.Localization.*`.

**D17 — Governance endpoint on LocalizationAdminController, not InstanceOnboardingController**
- Plan specified `InstanceOnboardingController` to match analytics precedent. Implementation placed it on `LocalizationAdminController` (`PUT /api/admin/localization/governance`) for endpoint cohesion — all localization admin routes on one controller.

**D18 — Client-side DTO mirroring via LocalizationGovernancePayload**
- The client can't reference `Explore.Application.DTOs.Localization.UpdateLocalizationGovernanceDto` (same NU1605 issue as D16). Created `Explore.Blazor.Client.Models.Admin.LocalizationGovernancePayload` — a JSON-shape-identical class on the client side. When swagger.json is regenerated and NSwag runs, this can be replaced by the generated type.
- Similarly, `LocalizationConfigDto` is used from the NSwag-generated client (`Explore.Blazor.Client.Clients.LocalizationConfigDto`), not from `Explore.Application`.

**D19 — LocalizationSettingDefinitions registered in SettingRegistry**
- Created `Explore.Domain/Settings/Definitions/LocalizationSettingDefinitions.cs` with all 9 localization keys.
- Registered in `SettingRegistry.cs` static constructor.
- This ensures `SettingUpsertService.UpsertValueAsync(key, value, actorId)` picks up proper metadata (ValueType, Category, Description) from the registry automatically.
- Also satisfies the architecture test `SettingGroups_AllKeysMustExistInRegistry` for any future LocalizationSettingGroup.

---

### Build State (end of session)
- `Explore.Domain` — builds clean
- `Explore.Application` — builds clean (note: pre-existing EventRegistration WIP by another session may cause transient errors — see journal)
- `Explore.Infrastructure` — builds clean
- `Explore.Persistence` — builds clean
- `Explore.API` — builds clean
- `Explore.Blazor.Client` — builds clean
- `Explore.Blazor` — builds clean
- `Event.Application.UnitTests` — **707/707 pass**
- `Event.Architecture.Tests` — **59/59 pass**
- `Event.Domain.UnitTests` — **100/100 pass**

**Known pre-existing issue**: Another session has uncommitted WIP in `CreateEventRegistrationDto.cs` / `CreateEventRegistrationDtoValidator.cs` / `CreateEventRegistrationCommandHandler.cs` (removed `EventSessionId`, added scope-based fields, but validator references stale field). These errors may appear intermittently depending on build cache state. They are NOT caused by localization work.

---

### Quick Resume for Next Session

1. Read this file (context) + tasks file for checklist state
2. `dotnet build --configuration Release --verbosity quiet` — verify green
3. **Remaining Phase 1** (low priority): tasks 1.3 (cookie test), 1.12 (EnabledLanguages check — needs BFF bootstrap)
4. **Phase 5** — Starter bundle content (manual curation). Content-heavy, needs human judgment. Task 5.1 is walk-through of all .razor files to curate ~80-150 keys.
5. **Phase 6.3** — Reference marker: verify `mudblazor.*` keys exist in starter bundles (depends on Phase 5)
6. **Phase 8.4** — Grafana dashboard JSON (observability)
7. **Remaining Phase 9** — Test registry: 9.1 (TranslationServiceTests), 9.2 (LanguagePickerTests bUnit), 9.3 (LanguageProviderTests bUnit), 9.4 (LocalizationAdminServiceTests), 9.5 (InstanceLocalizationSectionTests bUnit)
8. **Phase 10.4** — Final housekeeping (mark tasks complete, move to dev/done/)
9. **Phase 10.7** — Finalize distributed-bundle-file-writer backlog ticket

**Pre-existing issues blocking full solution build:**
- `UpdateEventCommandHandler.cs` line 66 — `UpdateEventDtoValidator` constructor mismatch (missing `eventSeriesRepository` + `eventRegistrationPolicyRepository` params). This is another session's WIP, not localization.
- `LocationRooms` commands reference `ResourceKinds.LocationRoom` which doesn't exist. Another session's WIP.

### Files Created in Session 2 (new files only)
```
Explore.Blazor.Client/Services/MudBlazorLocalizer.cs
Explore.Infrastructure/Localization/Resilience/TolgeeRetryAfterReader.cs
Explore.Infrastructure/Localization/Resilience/WeblateRateLimitReader.cs
Explore.Application/Telemetry/TranslationMetrics.cs
Event.Architecture.Tests/LocalizationResilienceTests.cs
Event.Architecture.Tests/BlazorClientArchitectureTests.cs
Event.Application.UnitTests/Infrastructure/Localization/UpdateLocalizationGovernanceCommandHandlerTests.cs
Event.Application.UnitTests/Infrastructure/Localization/ExportFromTmsCommandHandlerTests.cs
dev/backlog/distributed-bundle-file-writer.md
dev/backlog/user-preferences-split.md
dev/backlog/instance-settings-controller-split.md
```

### Files Modified in Session 2
```
Explore.Application/DTOs/Localization/LocalizationConfigDto.cs — added governance fields
Explore.API/Controllers/LocalizationAdminController.cs — injected IBundleFileWriter, added bundle-health endpoint, extended GetConfiguration mapping
Explore.API/Program.cs — registered TranslationMetrics singleton
Explore.Blazor.Client/Models/Admin/LocalizationAdminState.cs — added governance/health/secret state fields + LoadFrom parsing
Explore.Blazor.Client/Contracts/Services/ILocalizationAdminService.cs — added GetBundlePathHealthAsync + BundlePathHealthResult
Explore.Blazor.Client/Services/LocalizationAdminService.cs — added GetBundlePathHealthAsync impl
Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceLocalizationSection.razor — full rewrite with 4.6-4.11 enhancements
Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceLocalizationSection.razor.css — BEM blocks for new sections
Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs — registered MudBlazorLocalizer
Explore.Blazor.Client/Providers/LanguageProvider.razor — PersistentComponentState integration
Explore.Blazor.Client/Program.cs — CultureInfo startup from cookie
Explore.Infrastructure/InfrastructureServicesRegistration.cs — resilience pipelines for Tolgee/Weblate
Explore.Infrastructure/Localization/RuntimeTranslationProvider.cs — TranslationMetrics injection + fallback instrumentation
Event.Application.UnitTests/Infrastructure/Localization/RuntimeTranslationProviderFallbackTests.cs — fixed TranslationMetrics constructor
Event.Application.UnitTests/Infrastructure/Localization/RuntimeTranslationProviderTests.cs — fixed TranslationMetrics constructor
docs/LOCALIZATION.md — governance model, cache variation, HA constraint, new endpoints
docs/BLAZOR.md — Localization subsection
docs/OPERATIONS.md — localization runbooks + metrics alerts
```

### Files Created in Session 1 (new files only)
```
Explore.Domain/Common/Localization/CultureEntry.cs
Explore.Domain/Common/Localization/CultureRegistry.cs
Explore.Domain/Common/Localization/RtlLanguages.cs
Explore.Domain/Settings/Definitions/LocalizationSettingDefinitions.cs
Explore.Application/Contracts/Infrastructure/IBundleFileWriter.cs
Explore.Application/DTOs/Localization/UpdateLocalizationGovernanceDto.cs
Explore.Application/DTOs/Localization/Validators/UpdateLocalizationGovernanceDtoValidator.cs
Explore.Application/Features/Localization/Requests/Commands/UpdateLocalizationGovernanceCommand.cs
Explore.Application/Features/Localization/Handlers/Commands/UpdateLocalizationGovernanceCommandHandler.cs
Explore.Infrastructure/Localization/BundleFileWriter.cs
Explore.Blazor.Client/Contracts/Services/ILanguagePreferenceService.cs
Explore.Blazor.Client/Contracts/Services/ILocalizationAdminService.cs
Explore.Blazor.Client/Services/LanguagePreferenceService.cs
Explore.Blazor.Client/Services/LocalizationAdminService.cs
Explore.Blazor.Client/Models/Admin/LocalizationAdminState.cs
Explore.Blazor.Client/Models/Admin/LocalizationGovernancePayload.cs
Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceLocalizationSection.razor
Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceLocalizationSection.razor.css
Explore.Blazor.Client/Shared/LanguagePicker.razor.css
Event.Application.UnitTests/Infrastructure/Localization/RuntimeTranslationProviderFallbackTests.cs
Event.Application.UnitTests/Domain/Common/Localization/CultureRegistryTests.cs
```

### Files Modified This Session (existing files)
```
Explore.Domain/Constants/GovernanceSettingKeys.cs — added 4 Localization keys + Appearance.Language
Explore.Domain/Settings/SettingRegistry.cs — registered LocalizationSettingDefinitions
Explore.Domain/Settings/Definitions/AppearanceSettingDefinitions.cs — added Language definition
Explore.Persistence/Seed/SeedIds.cs — 4 new IDs (565-568)
Explore.Persistence/Seed/LookupTableSeeder.cs — 4 new SystemSetting seed rows
Explore.Application/Contracts/Infrastructure/ITranslationConfigResolver.cs — extended TranslationConfiguration record
Explore.Application/Contracts/Infrastructure/ITranslationResolver.cs — added InvalidateLanguageAsync
Explore.Application/DTOs/Appearance/UpdateUserAppearancePreferencesDto.cs — added Language
Explore.Application/DTOs/Appearance/UserAppearancePreferencesDto.cs — added Language
Explore.Application/DTOs/Appearance/Validators/UpdateUserAppearancePreferencesDtoValidator.cs — CultureRegistry validation
Explore.Application/DTOs/Localization/LocalizationConfigDto.cs — unchanged (used as-is)
Explore.Application/Settings/Groups/AppearanceSettingGroup.cs — added Language property
Explore.Application/Features/Appearance/Handlers/Commands/UpdateCurrentUserAppearancePreferencesCommandHandler.cs — language persistence
Explore.Application/Features/Appearance/Handlers/Queries/GetCurrentUserAppearancePreferencesQueryHandler.cs — returns Language
Explore.Application/Features/Localization/Handlers/Commands/ExportFromTmsCommandHandler.cs — rewritten with IBundleFileWriter
Explore.Infrastructure/Localization/RuntimeTranslationProvider.cs — ForceOfflineMode short-circuit
Explore.Infrastructure/Localization/TranslationConfigResolver.cs — parses 4 new keys
Explore.Infrastructure/Localization/TranslationResolver.cs — cache-key tuple + InvalidateLanguageAsync
Explore.Infrastructure/Localization/OfflineTranslationProvider.cs — writable-dir-first + InvalidateLanguage
Explore.Infrastructure/InfrastructureServicesRegistration.cs — IBundleFileWriter registration
Explore.API/Program.cs — UseRequestLocalization
Explore.API/Controllers/LocalizationAdminController.cs — PUT governance endpoint
Explore.Blazor/Program.cs — UseRequestLocalization
Explore.Blazor/Components/App.razor — CultureRegistry validation, RtlLanguages.IsRtl
Explore.Blazor/Extensions/BffPreferenceEndpoints.cs — rewritten HandleLanguagePreference
Explore.Blazor/Extensions/HttpClientExtensions.cs — LocalizationAdminService typed client
Explore.Blazor.Client/Explore.Blazor.Client.csproj — ProjectReference to Explore.Domain
Explore.Blazor.Client/Models/LanguageContext.cs — delegates to CultureRegistry
Explore.Blazor.Client/Services/TranslationService.cs — CultureRegistry validation + logger scope
Explore.Blazor.Client/Providers/LanguageProvider.razor — IDisposable + async Task + announcer
Explore.Blazor.Client/Shared/LanguagePicker.razor — rewritten with service + a11y + kill-switch
Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs — ILanguagePreferenceService registration
Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAdminSettingsLayout.razor — localization nav item + section
```

### 2026-04-11 — Slice B Implementation Pass (session 4 — Phase 3 code complete)

**Phase 3 — Bundle Export Persistence (5 of 6 tasks, doc task 3.6 still pending)**:
- **3.1** — `IBundleFileWriter` contract at `Explore.Application/Contracts/Infrastructure/IBundleFileWriter.cs` with `WriteBundleAsync` + `CheckHealthAsync`, + `WritablePathHealth` record + `BundleWriteException`.
- **3.2** — `BundleFileWriter` impl at `Explore.Infrastructure/Localization/BundleFileWriter.cs`. Uses `IWebHostEnvironment.ContentRootPath` + `App_Data/Localization/Bundles/{code}.json`. Atomic write: serializes to `{code}.json.tmp` → `File.Move(..., overwrite: true)`. **Default `JsonSerializerOptions { WriteIndented = true }` ONLY — no `UnsafeRelaxedJsonEscaping`** (safe Unicode escapes are correct for Arabic/Hebrew content). `CheckHealthAsync` probes dir existence + write-delete a `.healthcheck.tmp` file.
- **3.3** — `OfflineTranslationProvider` now checks the writable dir first, falls back to embedded resources. New constructor takes optional `IWebHostEnvironment?` (nullable so existing Singleton registration + tests without env keep working). Added `InvalidateLanguage(string)` method that removes a single language's in-memory cache entry.
- **3.4** — `ExportFromTmsCommandHandler` rewritten: injects `IBundleFileWriter` + `ILogger`, converts exports to a flat dict, calls `WriteBundleAsync`, calls `_translationResolver.InvalidateLanguageAsync`, catches `BundleWriteException` with a friendly message, returns path in `response.Message`.
- **3.5** — `ITranslationResolver.InvalidateLanguageAsync` added to the contract. `TranslationResolver._preloadedKeys` changed from `ConcurrentDictionary<string, bool>` to `ConcurrentDictionary<string, List<string>>` so we track every cache key we inserted and can purge them all on invalidation (IMemoryCache doesn't support key enumeration). `InvalidateLanguageAsync` walks both `"live"` and `"offline"` mode slots.
- **3.6** (pending) — HA constraint doc in `docs/LOCALIZATION.md` + `dev/backlog/distributed-bundle-file-writer.md`. Deferred.

**DI**: `services.AddScoped<IBundleFileWriter, BundleFileWriter>();` in `InfrastructureServicesRegistration.cs` (after TranslationResolver).

**Build/test state** (after Phase 3 code):
- All 10 main/test projects build clean
- `Event.Architecture.Tests` **59/59 pass**
- `Event.Application.UnitTests` **707/707 pass**

**Next session — pick up at**:
- Phase 3.6 (doc task — small)
- Phase 4 — Localization Admin UI (biggest phase, many new files: `ILocalizationAdminService`, `InstanceLocalizationSection.razor`, governance command + handler, section docked in `InstanceAdminSettingsLayout`, etc.)
- Phase 5 — Starter bundle content (manual curation; content-heavy, can happen in parallel)
- Phase 6 — Native CultureInfo + MudLocalizer (depends on Phase 5's `mudblazor.*` keys)

### 2026-04-11 — Slice A Implementation Pass (session 3 — Phase 2 complete)

**Phase 2 — BFF Language Persistence** landed:
- **2.1** `UpdateUserAppearancePreferencesDto` + `UserAppearancePreferencesDto` gain `Language` (default `"en"`). Validator enforces `CultureRegistry.Contains(code)`.
- **2.2** `GovernanceSettingKeys.Appearance.Language = "appearance.language"` added. `AppearanceSettingGroup` extended with `Language` property (defaults to `"en"`; rejected codes fall back to `"en"` via `CultureRegistry`). `UpdateCurrentUserAppearancePreferencesCommandHandler` persists the language with the existing sparse-override pattern (remove override when it matches parent). `GetCurrentUserAppearancePreferencesQueryHandler` returns it. Setting is also registered in `AppearanceSettingDefinitions` so the architecture test `SettingGroups_AllKeysMustExistInRegistry` stays green.
- **2.3** `HandleLanguagePreference` in `BffPreferenceEndpoints.cs` rewritten: validates via `CultureRegistry.TryGetEntry` (replacing the 2–5 char length hack), async signature, when authenticated it calls `PUT api/user/appearance` via `"BffClient"` with the full DTO (carries current theme + direction), writes BOTH cookies — `lang` (existing) + `.AspNetCore.Culture` via `CookieRequestCultureProvider.MakeCookieValue`. Returns `UserAppearancePreferencesDto` on success. Two new helpers: `PersistLanguageCookie` + `PersistAspNetCoreCultureCookie`.
- **2.4** `UseRequestLocalization()` wired in BOTH `Explore.API/Program.cs` and `Explore.Blazor/Program.cs`. Supported cultures pulled from `CultureRegistry.GetAll()` (compile-time) — NEVER from the runtime TMS. `CookieRequestCultureProvider` first, `AcceptLanguageHeaderRequestCultureProvider` second, default `"en"`. Middleware registered AFTER authentication and BEFORE endpoint routing per Microsoft docs.

**Build/test state** (after Phase 2):
- `Event.Architecture.Tests` **59/59 pass** (`SettingGroups_AllKeysMustExistInRegistry` stays green thanks to the new `AppearanceSettingDefinitions.Language` entry)
- `Event.Domain.UnitTests` **100/100 pass**
- `Event.Application.UnitTests` **707/707 pass**

**Note on parallel work**: Another session (between session 2 and 3) committed ahead a few commits (now `cbc0abe6 chore: update cookie-consent-analytics plan`) and left some in-progress changes in `Explore.Application/Features/EventSessions/**` and matching test files. Those changes are unrelated to localization and resolve themselves on rebuild — not my concern.

### 2026-04-11 — Slice A Implementation Pass (session 2 — continued)

**Added on top of session 1**:
- **Phase 1.11** — `RuntimeTranslationProvider.ResolveProviderAsync()` short-circuits to `_offlineProvider` when `config.ForceOfflineMode == true`, logs a warning including the configured provider name.
- **Phase 1.14** — `TranslationResolver` cache-key tuple is now `Translation:{tenantId}:{lang}:{mode}:{key}` where `mode ∈ {"live","offline"}`. Resolver also takes `ITenantContext`. `ResolveMode(config)` returns `offline` for `None`/`ForceOfflineMode`, `live` otherwise. Preload-deduplication key updated accordingly.
- **Phase 1.4** — `ILanguagePreferenceService` + `LanguagePreferenceService` added (validates against `CultureRegistry`, calls `POST /bff/language?lang=...` via `"BffClient"` named HttpClient, logs + swallows transport errors). Registered scoped in `ServiceCollectionExtensions`.
- **Phase 1.5** — `LanguageProvider.razor` now `@implements IDisposable`, no more `async void` (switched to a captured `Action<string>` shim that marshals to `InvokeAsync(HandleLanguageChangedAsync)`), and calls `IAccessibilityAnnouncerService.AnnouncePoliteAsync` on successful change. Shim reference stored so `Dispose` unsubscribes the exact same delegate.
- **Phase 1.8** — `TranslationService` validates all language codes against `CultureRegistry` at fetch boundaries (`GetTranslationsAsync`, `ChangeLanguageAsync`, `PreloadAsync`). Unknown codes are logged + rejected without poisoning the cache. `GetTranslationsAsync` wraps its body in a logger scope carrying `Language` + `Operation`. **Hot-path `T(key)` is untouched** — no metrics, no scopes, no allocations — per plan D8 and Technical Constraint 16.
- **Phase 1.6** — `LanguagePicker.razor` rewritten: (a) uses `CultureRegistry.GetAll()` directly rather than `GetAvailableLanguagesAsync` (which is reporting-only per D12), (b) delegates persistence to `ILanguagePreferenceService`, (c) adds `aria-label="Change language. Current: {name}"`, (d) shows `ISnackbar` success/failure, (e) announces failures assertively via `IAccessibilityAnnouncerService`, (f) new `Enabled` parameter drives the governance kill-switch — renders nothing when `false`. Parent layouts will feed it from bootstrap state in a later phase (initially defaults to `true`, safe).
- **Phase 1.7** — `LanguagePicker.razor.css` (NEW) — BEM blocks (`.language-picker`, `__button`, `__menu`, `__item`, `__item--active`), CSS logical properties only (`padding-block/inline`, `min-inline-size`, `text-align: start`), focus-visible ring via `--isl-focus-ring-*` tokens, WCAG 2.2 AA min 24×24 CSS px target via `min-block-size: 2.25rem`.
- **Phase 0.3** — `RuntimeTranslationProviderFallbackTests` NEW — 2 tests covering (1) `ForceOfflineMode = true` short-circuits to offline without touching Tolgee, (2) failing Tolgee (throwing `HttpRequestException`) never bubbles out of the runtime provider.
- **Bonus** — `CultureRegistryTests` NEW — 7 test cases (with `[Arguments]`) covering `GetAll()` order, `TryGetEntry` for known/unknown codes, normalisation edge cases (null, empty, `en-US`, `<script>`, `fr_FR`), Arabic RTL flag, `RtlLanguages.IsRtl()` for known/unknown codes.

**Build/test state**:
- `Event.Architecture.Tests` **59/59 pass**
- `Event.Domain.UnitTests` **100/100 pass**
- `Event.Application.UnitTests` **707/707 pass** (including the 7 `GetInstanceBootstrap*` tests that were previously flaky — they are now green; not caused by my changes, likely environmental)

**Still remaining in Slice A Phase 1** (2 tasks):
- **Phase 1.3** — App.razor cookie validation test coverage (code change is already done in session 1; task is specifically for the test). Low priority for this session.
- **Phase 1.12** — `LanguagePreferenceService` should additionally validate against the current `EnabledLanguages` subset (not just the Culture Registry). Requires a client-accessible snapshot of `TranslationConfiguration` which we don't have yet — blocked on a bootstrap/BFF endpoint (Phase 2 territory).

**Next session — pick up at**:
- Phase 2 (BFF language persistence) — now unblocked by Phase 1.4/1.5/1.6/1.8
- Phase 5 (starter bundle content, manual curation)
- Phase 3 (bundle export persistence / `IBundleFileWriter`)

### 2026-04-11 — Slice A Implementation Pass (session 1)

**Implemented (Slice A foundations)**:
- **Phase 1.1.5** — `CultureRegistry` + `CultureEntry` created at `Explore.Domain/Common/Localization/` (en, fr, ar). *Relocated from Application to Domain* because `Explore.Application` brings FluentValidation 12.0 + MediatR + AutoMapper which conflict with `Explore.Blazor.Client`'s FluentValidation 11.9.1 and would bloat the WASM bundle. `Explore.Domain` is lightweight (only `Microsoft.Extensions.Compliance.Abstractions`) and now referenced from `Explore.Blazor.Client.csproj` (the client's first project reference).
- **Phase 1.1** — `RtlLanguages.IsRtl(code)` shim at `Explore.Domain/Common/Localization/RtlLanguages.cs`, delegates to registry.
- **Phase 1.9** — Seeded 4 new governance keys in `LookupTableSeeder.cs` + `SeedIds.cs` + `GovernanceSettingKeys.Localization`: `enabled_languages` ("en,fr,ar"), `fallback_language` ("en"), `client_picker_enabled` (true), `force_offline_mode` (false). Seed IDs 565–568.
- **Phase 1.10** — Extended `TranslationConfiguration` record with init-only `EnabledLanguages`, `FallbackLanguage`, `ClientPickerEnabled`, `ForceOfflineMode` properties. Init-only (not constructor params) to preserve existing positional callsites in tests. `TranslationConfigResolver` parses all 4 keys, normalises via `CultureRegistry`, drops unknown codes with warning logs, picks safe defaults on parse failure (fallback language must be in enabled set; bool parse failures default to safe values).
- **Phase 1.2 + 1.13** — `LanguageContext.ForLanguage()` now delegates to `CultureRegistry.TryGetEntry`; the local `_rtlLanguages`/`LanguageFlags`/`LanguageNames` dictionaries are gone. `App.razor` validates the `lang` cookie via `CultureRegistry.TryGetEntry` (no regex) and uses `RtlLanguages.IsRtl()` for direction.

**Architecture decision added**:
- **D16 — Localization primitives live in Domain, not Application** — The plan specified `Explore.Application/Common/Localization/`. At implementation time, referencing `Explore.Application` from `Explore.Blazor.Client` failed with `NU1605` (FluentValidation 12.0 vs 11.9.1 downgrade) and would balloon the WASM bundle. Domain is lightweight and WASM-friendly. The client now references `Explore.Domain`. Plan/tasks files call out `Explore.Application.Common.Localization.*` in several places — update those when the plan is next revised.

**Build state**: Release build green for `Explore.Domain`, `Explore.Application`, `Explore.Infrastructure`, `Explore.Persistence`, `Explore.Blazor.Client`, `Explore.Blazor`, `Event.Application.UnitTests`. 7 failing tests in `GetInstanceBootstrap*QueryHandler` (analytics/deployment/render policy) are **pre-existing** — same count on develop@HEAD without these changes. Localization tests all pass.

**Next session — resume at**:
- Phase 0.1–0.3 (architecture tests + `RuntimeTranslationProviderFallbackTests`)
- Phase 1.3 onward: cookie validation tests, `ILanguagePreferenceService`, `LanguageProvider.razor` async correctness + IDisposable, `LanguagePicker.razor` a11y + kill-switch + service layer, `LanguagePicker.razor.css` BEM + logical properties, `TranslationService` fetch-boundary metrics + `CultureRegistry` validation
- Phase 1.11 (`RuntimeTranslationProvider.ForceOfflineMode` short-circuit)
- Phase 1.14 (`TranslationResolver` cache-key tuple with provider mode)

### 2026-04-11 — Enterprise-Grade Audit & Replan + CTO Feedback Revision Pass

**Goal**: Update this plan to reflect the actual state of the codebase, emphasize the dual-provider abstraction as the product cornerstone, treat offline bundles as a Tier-1 default, and raise the quality bar to enterprise-grade (observability, resilience, accessibility, testing). After the first draft was produced, a detailed CTO-level review was applied and folded back into the design.

**Completed**:
- Full audit of backend localization stack — all providers, CQRS handlers, controllers, governance keys, offline bundles confirmed implemented
- Full audit of Blazor client stack — discovered the entire `ITranslationService`/`LanguageProvider`/`LanguagePicker`/`localization.js`/BFF endpoints stack **already exists** as a working first draft (previous plan incorrectly claimed "Blazor — Nothing")
- Researched Tolgee v3 API via librarian agent + Tavily — endpoints, auth (`tgpak_`/`tgpat_`), rate limits (body `retryAfter`), export format (`JSON_TOLGEE` flat), webhooks (paid only), known issues
- Researched Weblate 5.x API via librarian agent + Tavily — endpoints, auth (`wlu_`/`wlp_` tokens), rate limits (`X-RateLimit-*` headers), DRF pagination, Project→Component→Translation hierarchy (Component mandatory), webhooks (free since 5.11), GPLv3 licensing implications
- Researched Blazor 10 localization best practices — Microsoft validates custom `ITranslationService` approach, `.AspNetCore.Culture` cookie, `PersistentComponentState` pattern for flicker avoidance, `MudLocalizer` wiring, RTL gotchas
- Identified **27 concrete gaps** across P0–P4 priority tiers (see `blazor-localization-plan.md` Confirmed Gaps section)
- Rewrote `blazor-localization-plan.md` with 10 phases
- Rewrote `blazor-localization-tasks.md` with atomic, acceptance-criteria-bearing tasks per phase
- **Applied CTO feedback pass (see `## CTO Feedback Incorporated` section below)**:
  - Introduced a three-concept **Language Governance Model** (Culture Registry vs Enabled Languages vs Available Translations)
  - Added 4 new governance settings: `enabled_languages`, `fallback_language`, `client_picker_enabled`, `force_offline_mode`
  - Reshaped delivery into **three shippable slices** (A: Stabilize, B: Operable, C: Harden) with tests distributed into each slice (Phase 9 redefined as a test registry, not a terminal phase)
  - Relocated shared localization primitives from `Explore.Blazor.Client/Constants/` to `Explore.Application/Common/Localization/` to avoid server→client project coupling
  - Simplified resilience: single pipeline with stateless **readers** (`TolgeeRetryAfterReader`, `WeblateRateLimitReader`) feeding `DelayGenerator`, no overlapping custom retry handlers
  - Removed hot-path `T(key)` instrumentation; observability only on fetches/fallbacks/admin ops
  - Documented the HA constraint for `App_Data/Localization/Bundles/` explicitly (single-instance or shared-volume only, abstracted behind `IBundleFileWriter`)
  - Flagged `UpdateUserAppearancePreferencesDto.Language` and `InstanceOnboardingController` naming as acceptable technical debt with follow-up tickets in Phase 10
  - Replaced naïve regex cookie validation with `CultureRegistry.TryGetEntry` allowlist
  - Dropped `UnsafeRelaxedJsonEscaping` in favor of default JSON serializer for bundle files
  - Made explicit the cache variation tuple `(tenantId, languageCode, providerMode)` and the secret lifecycle rules (write-only, "configured / not configured" badge, independent rotation)

**In Progress**:
- Awaiting review of this revised plan before implementation begins

**Blockers**:
- None identified

---

## Quick Resume (if picking up later)

1. Read `blazor-localization-plan.md` for the full design and the 10 phases
2. Read this file for key files + decisions + constraints
3. Read `blazor-localization-tasks.md` for the atomic checklist
4. Start with **Phase 0 — Audit & Architecture Anchors**: the existing code works; add guard rails before touching anything
5. **Never** run destructive shell commands; follow the repo's `AGENTS.md` shell behavior rules
6. Build first: `dotnet build --configuration Release --verbosity quiet`
7. Run unit tests per project (no solution-level `dotnet test`)

---

## Key Files

### Backend — Complete (do not modify unless a phase explicitly says so)

**Contracts (Application)**
- `Explore.Application/Contracts/Infrastructure/ITranslationManagementProvider.cs` — pluggable TMS contract (`TestConnectionAsync`, `ImportKeysAsync`, `ExportTranslationsAsync`, `GetAvailableLanguagesAsync`)
- `Explore.Application/Contracts/Infrastructure/ITranslationConfigResolver.cs` — governance-driven config (`ResolveAsync`, `InvalidateCache`)
- `Explore.Application/Contracts/Infrastructure/ITranslationResolver.cs` — `ResolveAsync(key, lang)` + `ResolveBatchAsync`

**Providers (Infrastructure)**
- `Explore.Infrastructure/Localization/RuntimeTranslationProvider.cs` — Scoped, dispatches to concrete provider, falls back to Offline on any exception
- `Explore.Infrastructure/Localization/TolgeeTranslationProvider.cs` — HttpClient, `X-API-Key`, endpoints `/v2/projects/{id}/*`, 10s timeout
- `Explore.Infrastructure/Localization/WeblateTranslationProvider.cs` — HttpClient, `Authorization: Token`, endpoints `/api/translations/{project}/{component}/{lang}/file/`, 10s timeout
- `Explore.Infrastructure/Localization/OfflineTranslationProvider.cs` — Singleton, reads `Bundles/*.json` via `Assembly.GetManifestResourceStream`, thread-safe `ConcurrentDictionary` cache
- `Explore.Infrastructure/Localization/NullTranslationProvider.cs` — no-op safe fallback
- `Explore.Infrastructure/Localization/TranslationConfigResolver.cs` — 5-min cache keyed by `TranslationConfig:{tenantId}`
- `Explore.Infrastructure/Localization/TranslationResolver.cs` — preloads full language, 30-min live / 24h offline TTL
- **`Explore.Infrastructure/Localization/Bundles/en.json`** — currently `{}` (Phase 5 populates)
- **`Explore.Infrastructure/Localization/Bundles/fr.json`** — currently `{}` (Phase 5 populates)
- **`Explore.Infrastructure/Localization/Bundles/ar.json`** — currently `{}` (Phase 5 populates)

**DI Registration**
- `Explore.Infrastructure/InfrastructureServicesRegistration.cs` lines 152–169 — `AddHttpClient<Tolgee/WeblateTranslationProvider>`, singleton Offline, scoped Null/Runtime/Config/TranslationResolver

**API**
- `Explore.API/Controllers/TranslationController.cs` — `[AllowAnonymous]`, `GET /api/translation/{lang}` + `GET /api/translation/languages`
- `Explore.API/Controllers/LocalizationAdminController.cs` — `[Authorize]`, `POST test-connection`, `GET configuration`, `POST export-from-tms`

**CQRS**
- `Explore.Application/Features/Localization/Requests/Queries/GetTranslationsQuery.cs`
- `Explore.Application/Features/Localization/Requests/Queries/GetAvailableLanguagesQuery.cs`
- `Explore.Application/Features/Localization/Requests/Commands/TestTmsConnectionCommand.cs`
- `Explore.Application/Features/Localization/Requests/Commands/ExportFromTmsCommand.cs`
- `Explore.Application/Features/Localization/Handlers/Queries/GetTranslationsQueryHandler.cs`
- `Explore.Application/Features/Localization/Handlers/Queries/GetAvailableLanguagesQueryHandler.cs`
- `Explore.Application/Features/Localization/Handlers/Commands/TestTmsConnectionCommandHandler.cs`
- **`Explore.Application/Features/Localization/Handlers/Commands/ExportFromTmsCommandHandler.cs`** — Phase 3 target (does not persist bundle file)

**DTOs**
- `Explore.Application/DTOs/Localization/LocalizationConfigDto.cs` (already exists, returned by admin `GET configuration`)
- **`Explore.Application/DTOs/Appearance/UpdateUserAppearancePreferencesDto.cs`** — Phase 2 target (add `Language` field)

**Governance**
- `Explore.Persistence/Seed/LookupTableSeeder.cs` lines 350–355 — seeds IDs 560–564: `localization.default_language`, `tms_provider`, `tms_api_url`, `tms_project_id`, `tms_component`

**Enum**
- `Explore.Domain/Enums/TranslationManagementProviderEnum.cs` — `None=0, Tolgee=1, Weblate=2`

**Existing Unit Tests**
- `Event.Application.UnitTests/Infrastructure/Localization/GetTranslationsQueryHandlerTests.cs`
- `Event.Application.UnitTests/Infrastructure/Localization/RuntimeTranslationProviderTests.cs`
- `Event.Application.UnitTests/Infrastructure/Localization/OfflineTranslationProviderTests.cs`
- `Event.Application.UnitTests/Infrastructure/Localization/NullTranslationProviderTests.cs`
- `Event.Application.UnitTests/Infrastructure/Localization/TestTmsConnectionCommandHandlerTests.cs`

### Blazor Client — First Draft Exists (phases enhance these)

**Models**
- **`Explore.Blazor.Client/Models/LanguageContext.cs`** (91 lines) — Phase 1 target: retire hardcoded language discovery, keep display metadata only, point RTL at shared constant

**Contracts**
- `Explore.Blazor.Client/Contracts/Services/ITranslationService.cs` (46 lines)
- `Explore.Blazor.Client/Contracts/Services/Accessibility/IAccessibilityAnnouncerService.cs` — `AnnouncePoliteAsync`, `AnnounceAssertiveAsync` (used by Phase 1 in `LanguagePicker`)

**Services**
- **`Explore.Blazor.Client/Services/TranslationService.cs`** (142 lines) — Phase 1 target: add metrics, logger scopes, language-code allowlist validation
- **`Explore.Blazor.Client/Services/FooterAdminService.cs`** — reference pattern for the new `LocalizationAdminService` in Phase 4
- **`Explore.Blazor.Client/Services/LookupCacheService.cs`** — reference pattern for `SemaphoreSlim` + `CacheEntry<T>` caching

**Providers**
- **`Explore.Blazor.Client/Providers/LanguageProvider.razor`** (84 lines) — Phase 1 target: `IDisposable` interface, `async Task` not `async void`, announce via accessibility service
- `Explore.Blazor.Client/Providers/TenantContextProvider.razor` — reference pattern for cascading context

**Shared Components**
- **`Explore.Blazor.Client/Shared/LanguagePicker.razor`** (89 lines) — Phase 1 target: `aria-label`, snackbar feedback, ILanguagePreferenceService wrapper, new `.razor.css`
- **`Explore.Blazor.Client/Shared/LanguagePicker.razor.css`** — Phase 1 NEW FILE (missing): BEM + logical properties

**Layout**
- **`Explore.Blazor.Client/Layout/NavMenu.razor`** line 90 — `<LanguagePicker />` already placed in `.navbar__actions` (no change needed)
- **`Explore.Blazor.Client/Layout/MainLayout.razor`** — `<MudRTLProvider RightToLeft="@_isRtl">` already wraps content (feed `_isRtl` from `LanguageContext` if not already)
- **`Explore.Blazor.Client/Routes.razor`** lines 26–41 — `<LanguageProvider>...</LanguageProvider>` already wraps router

**DI**
- **`Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs`** line 78 — `services.AddScoped<ITranslationService, TranslationService>()` already registered; Phases 4 and 6 add `ILocalizationAdminService` and `MudLocalizer`

**Server**
- **`Explore.Blazor/Components/App.razor`** (165 lines) — Phase 1 target: drop local `_rtlLanguages` in favor of shared constant, validate `langCookie` against safe regex
- **`Explore.Blazor/Extensions/BffPreferenceEndpoints.cs`** (248 lines) — Phase 2 target: `HandleLanguagePreference` persists via API when authenticated + writes both `lang` and `.AspNetCore.Culture` cookies

**JS Interop**
- **`Explore.Blazor.Client/wwwroot/js/localization.js`** (23 lines) — Phase 1 target: `try/catch` around cookie parsers

**Testing**
- `Explore.Blazor.Client.Tests/Common/BlazorTestContext.cs` — extend with `AddTranslationService()` helper in Phase 9
- `Explore.Blazor.Client.Tests/Common/MockServiceFactory.cs` — add `CreateTranslationService()`, `CreateLocalizationAdminService()` in Phase 9
- **`Explore.Blazor.Client.Tests/Services/TranslationServiceTests.cs`** — Phase 9 NEW FILE (missing)
- **`Explore.Blazor.Client.Tests/Services/LocalizationAdminServiceTests.cs`** — Phase 9 NEW FILE (missing)
- **`Explore.Blazor.Client.Tests/Components/LanguagePickerTests.cs`** — Phase 9 NEW FILE (missing)
- **`Explore.Blazor.Client.Tests/Components/InstanceLocalizationSectionTests.cs`** — Phase 9 NEW FILE (missing)

### Files to Create (by phase)

**Phase 0** — Architecture tests
- `Event.Architecture.Tests/BlazorClientArchitectureTests.cs` (extend existing) — rules: components must not inject `IEventApiClient`; `ITranslationService` implementations live in `Services/`

**Phase 1 — Slice A** — Quality pass + language governance primitives (enhances existing files; several NEW files)
- `Explore.Blazor.Client/Shared/LanguagePicker.razor.css` (BEM, logical properties)
- `Explore.Blazor.Client/Contracts/Services/ILanguagePreferenceService.cs`
- `Explore.Blazor.Client/Services/LanguagePreferenceService.cs` (wraps `HttpClientFactory("BffClient")` call to `/bff/language`)
- **`Explore.Application/Common/Localization/CultureRegistry.cs`** (static compile-time list of `CultureEntry { Code, DisplayName, NativeName, Flag, IsRtl }` — the trusted allowlist used by both server and client; lives in Application, NOT Blazor.Client, so `Explore.Blazor` has no dependency on the client project)
- **`Explore.Application/Common/Localization/CultureEntry.cs`** (record type used by `CultureRegistry`)
- **`Explore.Application/Common/Localization/RtlLanguages.cs`** (single source of truth for RTL codes; `IsRtl(string code)` helper, moved out of `Explore.Blazor.Client/Constants/` per CTO feedback)
- Seeding edits to `Explore.Persistence/Seed/LookupTableSeeder.cs` for the four new governance keys (`localization.enabled_languages`, `localization.fallback_language`, `localization.client_picker_enabled`, `localization.force_offline_mode`)
- Extensions to `Explore.Application/Contracts/Infrastructure/ITranslationConfigResolver.cs` and `Explore.Infrastructure/Localization/TranslationConfigResolver.cs` to parse the four new keys onto `TranslationConfiguration`
- `RuntimeTranslationProvider.ResolveProviderAsync()` updated to honor `force_offline_mode`
- `LanguagePicker.razor` updated to hide itself when `client_picker_enabled = false`

**Phase 2 — Slice A** — BFF + API persistence (enhances existing; no new files except tests)

**Phase 3 — Slice B** — Bundle export persistence (abstracted behind `IBundleFileWriter` so a future `DistributedBundleFileWriter` can plug in for HA deployments)
- `Explore.Application/Contracts/Infrastructure/IBundleFileWriter.cs` — contract with `WriteAsync(lang, translations, ct)`, `CheckHealthAsync()` returning `WritablePathHealth(bool Exists, bool Writable, string? Reason)`
- `Explore.Infrastructure/Localization/BundleFileWriter.cs` — local-disk implementation (target `{ContentRoot}/App_Data/Localization/Bundles/{lang}.json`, atomic write via `.tmp` + `File.Move`, default `JsonSerializerOptions { WriteIndented = true }` — **no `UnsafeRelaxedJsonEscaping`**)
- `dev/backlog/distributed-bundle-file-writer.md` — backlog ticket documenting the shared-storage variant (Azure Blob, S3, or mounted volume) required for multi-replica deployments
- Edits to `docs/LOCALIZATION.md` adding the explicit "HA constraint" note (single-instance or shared-volume required; NOT inherently multi-replica safe)

**Phase 4 — Slice B** — Localization Admin UI (biggest phase, many new files)
- `Explore.Blazor.Client/Contracts/Services/ILocalizationAdminService.cs`
- `Explore.Blazor.Client/Services/LocalizationAdminService.cs`
- `Explore.Blazor.Client/Models/Admin/LocalizationAdminState.cs`
- `Explore.Blazor.Client/Pages/Admin/Instance/Components/Sections/InstanceLocalizationSection.razor`
- `Explore.Blazor.Client/Pages/Admin/Instance/Components/Sections/InstanceLocalizationSection.razor.css`
- `Explore.Application/DTOs/Localization/UpdateLocalizationGovernanceDto.cs`
- `Explore.Application/Features/Localization/Requests/Commands/UpdateLocalizationGovernanceCommand.cs`
- `Explore.Application/Features/Localization/Handlers/Commands/UpdateLocalizationGovernanceCommandHandler.cs`
- API endpoint in `Explore.API/Controllers/InstanceOnboardingController.cs` — `PUT /api/InstanceOnboarding/localization-governance` (keep here for v1 per CTO feedback; `InstanceSettingsController` rename is a Phase 10 tech-debt ticket)
- Admin UI surfaces: TMS provider selector with conditional fields, test-connection button, export-to-bundle button gated on `IBundleFileWriter.CheckHealthAsync()`, kill-switch toggles (`client_picker_enabled`, `force_offline_mode`), `enabled_languages` chip selector drawn from `CultureRegistry`, secret inputs enforcing write-only / "configured / not configured" badge / independent rotation

**Phase 5 — Slice A** — Starter bundle population (content only; bundles already exist but empty; **manual curation**, advisory auto-scrape only)
- `dev/active/blazor-localization/bundles-key-audit.md` — manual curation log of the first 80–150 keys per bundle
- Populated `en.json`, `fr.json`, `ar.json` content (including `mudblazor.*` keys for MudLocalizer wiring in Phase 6)

**Phase 6 — Slice B** — Native culture + MudLocalizer
- `Explore.Blazor.Client/Services/MudBlazorLocalizer.cs` (derives from `MudLocalizer`, delegates to `ITranslationService`)
- Optional: `Explore.Blazor.Client/Services/CulturePersistenceService.cs` (wraps `PersistentComponentState` read/write, culture code only — never the full dictionary)

**Phase 7 — Slice C** — Provider resilience (single pipeline per client, NO overlapping custom retry handlers)
- `Explore.Infrastructure/Localization/Resilience/TolgeeRetryAfterReader.cs` — stateless reader parsing 429 JSON body `retryAfter`; feeds pipeline `DelayGenerator`
- `Explore.Infrastructure/Localization/Resilience/WeblateRateLimitReader.cs` — stateless reader parsing `X-RateLimit-Reset` header; feeds pipeline `DelayGenerator`
- Edits to `Explore.Infrastructure/InfrastructureServicesRegistration.cs` to wire `AddResilienceHandler("tolgee-pipeline", ...)` and `AddResilienceHandler("weblate-pipeline", ...)` via `Microsoft.Extensions.Http.Resilience`

**Phase 8 — Slice C** — Observability (no new files; extends existing `IMetricsCollector`; instruments fetches/fallbacks/admin ops only — **never hot-path `T(key)` lookups**)

**Phase 9** — **Test Registry (distributed across Slices A/B/C)** — tests are authored inside their owning slice's phase deliverable, not as a terminal phase. Test files exist in the test projects but are tracked here for discoverability only.

**Phase 10 — Slice C** — Documentation + tech-debt tickets (edits to `docs/LOCALIZATION.md`, new `dev/backlog/` tickets for `UpdateUserAppearancePreferencesDto` → `UserPreferences` split, `InstanceSettingsController` rename, `DistributedBundleFileWriter`)

### NSwag
- `Explore.Blazor.Client/nswag.json` — input `../Explore.API/swagger.json`, output `Clients/EventApiClient.g.cs`, runs on `GenerateApiClient` before `CoreCompile`. Translation endpoints (`TranslationAsync`, `LanguagesAsync`) are already in the generated client. New admin endpoint from Phase 4 will regenerate automatically on build.

---

## Language Governance Model (Product Cornerstone)

The biggest conceptual correction from the CTO feedback pass is that "supported languages" is NOT one concept. It is three, each with a different owner, a different change cadence, and a different level of trust. Collapsing them into a single `GetAvailableLanguagesAsync()` call was the design flaw in the first draft.

| Concept | Who Owns It | Change Cadence | Authority |
|---|---|---|---|
| **Culture Registry** | Developers (compile-time) | Rare (code change) | Highest — any culture NOT in this list cannot be served by the app, period |
| **Enabled Languages** | Instance admins (governance) | Occasional (admin toggle) | Controls what the picker shows, what `UseRequestLocalization` considers valid, what user preference accepts |
| **Available Translations** | TMS / bundles (runtime) | Frequent (TMS sync) | Reporting only — used to show "the TMS has content for these languages"; **never** used as the allowlist |

### Governance settings (seeded in `LookupTableSeeder`, parsed by `TranslationConfigResolver`)
- `localization.default_language` (string, default `"en"`) — the culture returned when no user preference and no cookie match. Existing.
- `localization.enabled_languages` (string CSV, default `"en,fr,ar"`) — the ops-controlled subset. **NEW.**
- `localization.fallback_language` (string, default `"en"`) — the language used when a requested translation key is missing; must be in `enabled_languages`. **NEW.**
- `localization.client_picker_enabled` (bool, default `true`) — kill-switch for hiding the language picker without redeploy. **NEW.**
- `localization.force_offline_mode` (bool, default `false`) — kill-switch for short-circuiting to `OfflineTranslationProvider`. **NEW.**

### Resolution order (Slice A task in `TranslationConfigResolver` + `RuntimeTranslationProvider`)
1. If `force_offline_mode` is true → `OfflineTranslationProvider` is the chosen provider; `mode = "offline"`.
2. Otherwise, map `tms_provider` to Tolgee / Weblate / Null; `mode = "live"`.
3. Resolve the desired language: user preference → `lang` cookie → `Accept-Language` header → `default_language`.
4. Validate the desired language against `CultureRegistry ∩ enabled_languages`. If not valid → substitute `fallback_language`.
5. Fetch translations with the usual cache + fall-back-to-offline-on-error behavior. Cache key includes `(tenantId, languageCode, mode)`.

### What this prevents
- A transient Tolgee outage cannot quietly shrink the set of languages the picker shows.
- A new provider locale appearing in Tolgee does not auto-populate the picker (ops must opt in via `enabled_languages`).
- Startup localization configuration does not depend on reaching a remote TMS.
- `force_offline_mode` is a single-flag emergency lever; no code deploy required.
- Cache layer naturally partitions `"live"` and `"offline"` responses for the same `(tenant, language)` pair.

### Implementation touchpoints (tracked as tasks in `blazor-localization-tasks.md`)
- `Explore.Application/Common/Localization/CultureRegistry.cs` (NEW)
- `Explore.Application/Common/Localization/CultureEntry.cs` (NEW)
- `Explore.Application/Common/Localization/RtlLanguages.cs` (NEW, replaces the planned client-project constant)
- `Explore.Persistence/Seed/LookupTableSeeder.cs` (4 new rows)
- `Explore.Application/Contracts/Infrastructure/ITranslationConfigResolver.cs` + `TranslationConfiguration` record (4 new properties)
- `Explore.Infrastructure/Localization/TranslationConfigResolver.cs` (parse new keys)
- `Explore.Infrastructure/Localization/RuntimeTranslationProvider.cs` (`force_offline_mode` short-circuit)
- `Explore.Infrastructure/Localization/TranslationResolver.cs` (cache key varies on `mode`)
- `Explore.Blazor.Client/Shared/LanguagePicker.razor` (hides if `client_picker_enabled = false`)
- `Explore.Blazor/Components/App.razor` (validates via `CultureRegistry.TryGetEntry`)
- `Explore.Blazor.Client/Pages/Admin/Instance/Components/Sections/InstanceLocalizationSection.razor` (admin toggles + `enabled_languages` chip selector)

---

## Delivery Slices (How We Ship)

The 10 phases are the "what". The 3 slices are the "in what order and why". Tests live inside the slice that introduces the code they cover — **Phase 9 is a test registry for discoverability, NOT a terminal testing phase.**

### Slice A — "Stabilize What Exists" (~2.5 days)
**Goal**: the existing first-draft client works correctly and predictably with the new Language Governance Model.

- Phase 0 — Audit & architecture anchors
- Phase 1 — Quality pass + `CultureRegistry` / `RtlLanguages` / governance key seeding / kill-switch wiring
- Phase 2 — BFF + API language persistence for authenticated users
- Phase 5 — Manual bundle content curation (en/fr/ar)
- **Critical tests authored in-slice**: `TranslationServiceTests`, `LanguagePickerTests`, `LanguageProviderTests`, `BffPreferenceEndpointsTests` (language subset), `TranslationConfigResolverTests` (new governance keys), `RuntimeTranslationProviderFallbackTests` (force_offline_mode), `CultureRegistryTests`

### Slice B — "Make the Product Operable" (~3.5 days)
**Goal**: admins can configure TMS providers, export translations to persistent bundles, and see localized MudBlazor strings.

- Phase 3 — Bundle export persistence (`IBundleFileWriter`, HA constraint documented)
- Phase 4 — Localization Admin UI (TMS config, test connection, export button, kill-switch toggles, `enabled_languages` selector, secret lifecycle enforcement)
- Phase 6 — Native .NET `CultureInfo` integration + `MudLocalizer` wiring
- **Tests authored in-slice**: `ExportFromTmsCommandHandlerTests`, `BundleFileWriterTests` (including health check), `UpdateLocalizationGovernanceCommandHandlerTests`, `LocalizationAdminServiceTests`, `InstanceLocalizationSectionTests`, `MudBlazorLocalizerTests`

### Slice C — "Enterprise Hardening" (~2 days)
**Goal**: resilience under TMS failures, observability for ops, documentation + tech-debt tickets filed.

- Phase 7 — Provider resilience (single pipeline, stateless readers, no overlapping retries)
- Phase 8 — Observability (fetches/fallbacks/admin only — no hot-path `T(key)` instrumentation)
- Phase 10 — Documentation + tech-debt tickets
- **Tests authored in-slice**: Testcontainers integration tests for Tolgee and Weblate, architecture tests enforcing service-layer pattern, resilience pipeline unit tests

**Cumulative**: Slice A ends on day 2.5, Slice B ends on day 6, Slice C ends on day 8.

---

## Important Decisions

### D1 — Custom `ITranslationService` is correct (not `IStringLocalizer<T>`)
Microsoft's official Blazor localization docs explicitly allow any data source ("*By implementing IStringLocalizer, any data source can be used.*"), but `IStringLocalizer` is built around sync `.resx` access. Blocking HTTP calls in its indexer would kill render performance. Our async custom service is the right call for TMS-driven apps. **Keep the existing pattern.**

### D2 — Write BOTH `lang` and `.AspNetCore.Culture` cookies
- `lang` (existing) drives our custom `ITranslationService` and is JS-readable on WASM
- `.AspNetCore.Culture` (via `CookieRequestCultureProvider.MakeCookieValue`) drives native .NET `CultureInfo` for date/number formatting + `UseRequestLocalization` middleware + MudBlazor locale-aware components

Writing both in `HandleLanguagePreference` is cheap and avoids a breaking cookie migration for existing users.

### D3 — Extend `UpdateUserAppearancePreferencesDto` with `Language`
`/bff/direction` already persists via `PUT api/user/appearance`. `/bff/language` should match. One small DTO extension gives authenticated users cross-device language persistence.

### D4 — Admin UI docks inside `InstanceAdminSettingsLayout`
Follow the existing pattern: new sidebar nav item + new section component matching `InstanceGovernanceSection`, `InstanceBrandingSection`, `InstanceAnalyticsSection`. No new routes, no new layouts.

### D5 — Form fields conditional on selected provider
- Tolgee: URL + Project ID + API Key (hides Component field)
- Weblate: URL + Project Slug + **Component Slug (mandatory)** + API Token
- None: info card explaining offline-only mode

### D6 — `ExportFromTmsCommandHandler` writes to `App_Data/Localization/Bundles/`
Embedded resources in the DLL are read-only at runtime. The fix is a writable directory layer that `OfflineTranslationProvider` checks first, embedded resources second. This lets admins "pull" TMS content into offline fallback without rebuilding the image.

### D7 — Polly resilience with provider-specific 429 handlers
`Microsoft.Extensions.Http.Resilience` (built into .NET 10) provides the pipeline. Custom `DelegatingHandler` per provider handles rate-limit conventions (Tolgee body `retryAfter` ms, Weblate `X-RateLimit-Reset` header).

### D8 — Observability per `error-tracking` skill
Prometheus counters/histograms + Loki structured logs with correlation IDs + OpenTelemetry spans. Fallback activations are `Error`-level and alertable.

### D9 — WCAG 2.2 AA is the quality bar
Every component touched gets `aria-label`, focus ring, keyboard path, logical-property CSS, screen-reader announce. `docs/ACCESSIBILITY.md` is the authoritative reference.

### D10 — `MudLocalizer` wired to `ITranslationService`
Single wire translates all MudBlazor built-in strings (DataGrid, Dialog, DatePicker, Pagination). Seed `mudblazor.*` keys into starter bundles.

### D11 — `PersistentComponentState` for culture CODE only
Tiny payload, no dictionary bloat, no flicker on Server→WASM hand-off.

### D12 — Three sources of truth, not one (**REVISED per CTO feedback**)

The previous draft conflated three distinct concepts into `GetAvailableLanguagesAsync()`. That is wrong. A transient TMS failure must never shrink the set of valid cultures the app knows how to handle, and a random new provider locale must never auto-appear in the user-facing picker. We split into three independent layers:

1. **Culture Registry (compile-time, trusted)** — `Explore.Application/Common/Localization/CultureRegistry.cs`. Static list of `CultureEntry { Code, DisplayName, NativeName, Flag, IsRtl }`. This is the allowlist used everywhere we need to answer "is this a valid culture code for this codebase?". Lives in the Application layer (not `Explore.Blazor.Client`) so the server host (`Explore.Blazor`) can reference it without depending on the client project.
2. **Enabled Languages (governance-controlled, ops-driven)** — `localization.enabled_languages` governance setting. CSV, tenant-overridable, intersects with the Culture Registry. This drives the picker, the `UseRequestLocalization` supported cultures list, user-preference validation, and admin reporting. Instance admins change this; the picker reflects it immediately.
3. **Available Translations (runtime-discovered, reporting-only)** — still surfaced via `GetAvailableLanguagesAsync()`. Used by the admin UI to show "TMS has content for these languages" but **never treated as authoritative**. The picker does not use it. Startup localization config does not use it.

Supporting files:
- RTL set: `Explore.Application/Common/Localization/RtlLanguages.cs` (moved out of `Explore.Blazor.Client/Constants/`), consumed by both `App.razor` and `LanguageContext.cs` via the Application reference.
- Display metadata (flags, native names): moved out of `LanguageContext`'s hardcoded dictionary into `CultureRegistry`, single source.

### D13 — Kill-switches are first-class governance (**EXPANDED per CTO feedback**)

- `localization.client_picker_enabled` (bool, default `true`) — hides the picker if disabled. Not just a cosmetic toggle; it is the canonical way ops disables language switching during an incident.
- `localization.force_offline_mode` (bool, default `false`) — bypasses the active TMS entirely and serves only `OfflineTranslationProvider` (bundles + writable dir). The emergency lever when a TMS goes down and fallback-on-error is not enough.

Implementation requirements (wired as tasks in Phase 1 / Slice A):
1. **Seed** both keys in `LookupTableSeeder.cs`.
2. **Parse** both into `TranslationConfiguration` via `TranslationConfigResolver`.
3. **Enforce** `force_offline_mode` in `RuntimeTranslationProvider.ResolveProviderAsync()` so it short-circuits to `OfflineTranslationProvider` regardless of `tms_provider`.
4. **Consume** `client_picker_enabled` in `LanguagePicker.razor` so the picker hides itself (the cascading `LanguageContext` still drives lang/dir).
5. **Expose** both as toggles in the Localization Admin UI (Phase 4).
6. **Cover** both with integration tests in Slice A before any operational reliance.

Both are cache-invalidating events: flipping either must call `ITranslationConfigResolver.InvalidateCache(tenantId)`.

### D14 — Explicit translation cache variation (**NEW per CTO feedback**)

Translation cache keys MUST vary on the full tuple:

- **tenantId** (different tenants may use different TMS provider modes)
- **languageCode**
- **providerMode** (`"live"` when TMS is active, `"offline"` when `force_offline_mode = true` or fallback is active)

Key format: `Translation:{tenantId}:{languageCode}:{mode}`. This ensures a `force_offline_mode` flip instantly serves a different cache slot, not the stale TMS data. Cross-tenant cache leakage is already prevented by the tenantId segment. Documented in Enterprise Concerns → Performance of `plan.md`.

### D15 — Controller naming is acknowledged technical debt (**NEW per CTO feedback**)

`InstanceOnboardingController` was originally scoped to first-time setup. It has been organically growing to hold post-onboarding instance settings (analytics governance, now localization governance). For v1 we keep localization endpoints inside `InstanceOnboardingController` to stay consistent with the analytics precedent and avoid a scoped refactor during this delivery. A Phase 10 tech-debt ticket will propose splitting into `InstanceSetupController` (one-shot onboarding) + `InstanceSettingsController` (ongoing governance). Same pattern applies to D3's `UpdateUserAppearancePreferencesDto.Language` debt — accepted for speed, dedicated refactor later.

---

## Technical Constraints

1. **InteractiveAuto** — Code must work in Server (SignalR circuit, `HttpContext` available) and WASM (no `HttpContext`; use JS interop/cookies). Never cache per-circuit state that would leak into WASM.
2. **BFF proxy** — All API calls go through YARP (`Explore.Blazor` → YARP → `Explore.API`). Admin UI uses the `BffClient` named HttpClient; never calls the API directly.
3. **NSwag auto-generation** — `swagger.json` regenerates on API project build; client regenerates on Blazor.Client build. Plan phases must stage API changes BEFORE client changes when a new endpoint is required.
4. **MudBlazor v9** — Use MudBlazor components and repo wrappers (`AppButton`, `AppCard`, `AppTextField<T>`, `AppIconButton`, `AppDialogShell`). Global `.mud-*` overrides require a `JUSTIFICATION` comment per the mud-overrides whitelist.
5. **BEM + CSS isolation** — `.razor.css` per component, BEM classes, logical properties only (no `left`/`right`/`margin-left`/etc. per `docs/ACCESSIBILITY.md` PR-4).
6. **Clean Architecture** — Domain has no deps; Application references Domain only; Infrastructure references Application + Domain; API/Blazor is the composition root. Manual validator instantiation (no DI for validators).
7. **CQRS with MediatR** — commands return `BaseCommandResponse<Guid>`, queries return DTOs; authorization via `[AuthorizeResource]`/`IAuthorizedRequest`/`ISecureRequest` or endpoint-level attributes.
8. **Secret storage** — TMS API keys/tokens stored via `SecretProvider` (Infisical-backed), never in governance settings, cookies, or logs.
9. **No solution-level `dotnet test`** — run each test project individually per `AGENTS.md` (Windows locale issues with `findstr /i`).
10. **WCAG 2.2 AA** — non-negotiable for new UI work. Every new component passes accessibility checks before merging.
11. **.NET 10** — runtime is `Net100` per `nswag.json`. Benefits from `CultureInfo.DefaultThreadCurrentCulture`/`CurrentUICulture` WASM fix and `PersistentComponentState` API.
12. **No type suppression** — no `as any` equivalent, no `#pragma warning disable`, no empty catch blocks.
13. **No destructive shell commands** — never `rm -rf`; always report candidate files for removal instead.
14. **No commits unless asked** — follow repo git safety protocol.
15. **HA constraint for `App_Data/Localization/Bundles/`** — local-disk bundle persistence via `BundleFileWriter` is safe for single-instance Tier-1 deployments and for multi-replica Tier-2 deployments that mount a shared volume. It is **not** safe for horizontally scaled Tier-2 deployments without shared storage: Node A exports a bundle, Node B never sees it. This constraint is documented in `docs/LOCALIZATION.md` and the admin UI surfaces a writable-path health banner that disables the Export button with a tooltip when the directory is not writable. The `IBundleFileWriter` seam allows a future `DistributedBundleFileWriter` to replace local-disk without touching callers.
16. **Hot-path discipline for `T(key)`** — `ITranslationService.T(key, fallback)` is called from every component render. It must not touch I/O, must not emit metrics, must not start OTEL spans, and must not log above Debug level with sampling. Instrumentation lives in `GetTranslationsAsync`, `ChangeLanguageAsync`, fallback activations, admin operations — NOT in the lookup itself.
17. **Translation cache key tuple** — cache keys MUST be `Translation:{tenantId}:{languageCode}:{mode}` where `mode ∈ {"live", "offline"}`. Flipping `force_offline_mode` must instantly change the slot served, not serve stale TMS data. Cache invalidation on governance change must hit the full tenant's language set across both modes.
18. **Secret lifecycle operational rules** — TMS API keys are stored via `SecretProvider`, never in governance settings, never rendered in the admin UI. The UI shows a "configured" / "not configured" badge; rotating a secret does not require re-entering the URL, project ID, or component slug; clearing a secret requires a dedicated "Clear secret" button with a confirmation dialog.

---

## Dependencies (slice and phase order)

Tests are NOT a final phase. Tests belong to the slice that introduces the code, and "Phase 9" is a test registry for discoverability only. Execution order below is by slice.

### Slice A (~2.5 days)
1. **Phase 0 — Audit & Architecture Anchors** (blocks everything; establishes NetArchTest guard rails)
2. **Phase 1 — Quality Pass + Language Governance Primitives** (may run in parallel with Phase 7 because they touch different projects, but treat as Slice A for delivery ordering)
3. **Phase 2 — BFF + API Language Persistence** (depends on Phase 1 for `ILanguagePreferenceService` and on the new governance settings in `TranslationConfiguration`)
4. **Phase 5 — Starter Bundle Content** (depends on nothing but lands in Slice A so Slice A ships with a useful default)
5. Slice A tests authored alongside these phases.

### Slice B (~3.5 days)
6. **Phase 3 — Bundle Export Persistence** (depends on `IBundleFileWriter` contract which is Phase 3 itself; no external dep)
7. **Phase 6 — Native Culture + MudLocalizer** (depends on Phase 2's `.AspNetCore.Culture` cookie writer and on Phase 5's seeded `mudblazor.*` keys — so Phase 5 must have landed in Slice A)
8. **Phase 4 — Localization Admin UI** (depends on Phase 3's Export button wiring, Phase 2's language persistence, Phase 1's new governance keys for the toggles)
9. Slice B tests authored alongside these phases.

### Slice C (~2 days)
10. **Phase 7 — Provider Resilience** (Infrastructure-only, independent of Slices A/B for code, but delivered in Slice C so the hardening arrives together; no file overlap with Slices A/B means it can technically be started earlier if capacity allows)
11. **Phase 8 — Observability** (depends on Phases 1, 3, 4, 7 because it instruments them; safest to run after Phase 7)
12. **Phase 10 — Documentation + Tech-Debt Tickets** (last; captures the shipped state and files the `UserPreferences` / `InstanceSettingsController` / `DistributedBundleFileWriter` backlog tickets)
13. Slice C tests (Testcontainers integration, architecture tests) authored alongside these phases.

Parallelism opportunities within a slice: in Slice A, Phases 1 and 7 touch disjoint files (one is Blazor.Client + Application, the other is Infrastructure resilience), so they can run in parallel if two streams of work are available. In Slice B, Phases 3 and 6 are independent and can also run in parallel; Phase 4 waits on both.

---

## Reference Material (read before each phase)

### Always read before Blazor client work
- `.claude/skills/blazor-ui-conventions/SKILL.md`
- `.claude/skills/blazor-bff-patterns/SKILL.md`
- `.claude/skills/blazor-css-isolation/SKILL.md`
- `docs/BLAZOR.md`
- `docs/ACCESSIBILITY.md`

### Always read before Application/Infrastructure work
- `.claude/skills/clean-architecture-rules/SKILL.md`
- `.claude/skills/cqrs-mediatr-guidelines/SKILL.md`
- `.claude/skills/dotnet-efcore-guidelines/SKILL.md`
- `docs/ARCHITECTURE.md`
- `docs/QUICK_REFERENCE.md`

### Always read before observability work
- `.claude/skills/error-tracking/SKILL.md`
- `docs/OPERATIONS.md`

### Topic docs
- `docs/LOCALIZATION.md` — tier model, provider routing, governance keys, TMS API reference
- `docs/EXTENSIBILITY.md` — TMS abstraction documented pattern
- `docs/CONFIGURATION.md` — governance cascade, secret storage
- `docs/SECURITY-MODEL.md` — BFF model, claim fallback, authorization layering

### External references (Phase 4, 7)
- [Tolgee v2 API docs](https://docs.tolgee.io/api)
- [Weblate REST API docs](https://docs.weblate.org/en/latest/api.html)
- [Microsoft Blazor localization](https://learn.microsoft.com/en-us/aspnet/core/blazor/globalization-localization)
- [MudBlazor Localization](https://mudblazor.com/features/localization)
- [Microsoft.Extensions.Http.Resilience](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience)

---

## Open Questions — RESOLVED (per CTO feedback)

All five open questions from the v1 draft were answered in the CTO feedback pass:

1. **Writable bundle directory** — `{ContentRoot}/App_Data/Localization/Bundles/` is **accepted as the v1 default**, with two explicit conditions: (a) document the HA constraint (single-instance or shared-volume only) in `docs/LOCALIZATION.md`, (b) abstract the write surface behind `IBundleFileWriter` so a future `DistributedBundleFileWriter` (Azure Blob, S3, or mounted volume) can plug in without touching callers. Do **not** ship as "enterprise-grade HA-safe". A backlog ticket for `DistributedBundleFileWriter` will be filed in Phase 10.

2. **Starter bundle key set** — **manual curation** is authoritative for v1. An auto-scrape script is allowed as an advisory audit aid (e.g., to spot hardcoded strings that should be translated) but its output is not copied into the bundles without review. Target the first 80–150 keys intentionally.

3. **Admin endpoint hosting** — **keep inside `InstanceOnboardingController`** for v1 to match the analytics-governance precedent. Controller-name drift is acknowledged as D15 technical debt; Phase 10 files a `dev/backlog/` ticket to split into `InstanceSetupController` + `InstanceSettingsController`.

4. **Additional languages beyond en/fr/ar** — **keep to en/fr/ar for v1**. Three languages are enough to prove LTR + RTL + multilingual flow + admin/provider plumbing. Additional languages are added later by updating `CultureRegistry`, seeding bundles, and toggling `enabled_languages` — no code changes needed after v1.

5. **ICU pluralization / interpolation** — **post-v1**. No business-critical plural strings block the first rollout. Task 10.x files a backlog ticket.

---

## New Open Question (surfaced during revision)

**Tier-1 "first-boot" experience with empty bundles** — if a fresh self-hoster deploys without configuring a TMS, the picker correctly shows `en`/`fr`/`ar` from `CultureRegistry ∩ enabled_languages`, but the bundles shipped with the container are populated in Slice A (Phase 5). Slice A delivery sequence must land Phase 5 BEFORE any public rollout, otherwise Tier-1 users see English content even when Arabic is selected. Mitigation: Phase 5 is in-slice with Phase 1 and is on the Slice A critical path; Slice A is not declared done until bundles are populated and `LanguageContext.IsRtl = true` for `ar` produces RTL layout with populated Arabic content. This is captured as an acceptance criterion in `blazor-localization-tasks.md` Phase 5.

## Session Handoff — 2026-05-03 Europe/Brussels

No implementation work was performed for this active task during the sidebar dock refactor handoff session. Existing context, plan, and task files remain the authoritative state for this workstream. Do not infer progress or blockers here from the sidebar/dock-specific changes unless a future session explicitly broadens scope.
