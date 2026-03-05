# Localization System — Task Checklist

Last Updated: 2026-03-04

## Phase 1: Domain Layer — Enums, Constants, Indexes (Effort: S)

- [x] **1.1** Create `TranslationManagementProviderEnum` (`Explore.Domain/Enums/TranslationManagementProviderEnum.cs`)
  - Values: None=0, Tolgee=1, Weblate=2
  - ABOUTME header, file-scoped namespace, matches AnalyticsProviderEnum pattern

- [x] **1.2** Add `GovernanceSettingKeys.Localization` group (`Explore.Domain/Constants/GovernanceSettingKeys.cs`)
  - Keys: default_language, tms_provider, tms_api_url, tms_project_id, tms_component
  - API key/token NOT stored here (use SecretProvider)

- [x] **1.3** Add unique index on Tag `(TenantId, MasterCode)` (`Explore.Persistence/Configurations/Entities/TagConfiguration.cs`)
  - Simple composite unique — TenantId stays required, no nullable changes
  - Index name: `ix_tags_tenant_master_code`

- [x] **1.4** Add unique index on Category `(TenantId, MasterCode)` (`Explore.Persistence/Configurations/Entities/CategoryConfiguration.cs`)
  - Same as 1.3, index name: `ix_categories_tenant_master_code`

## Phase 2: Application Layer — Contracts (Effort: M)

- [x] **2.1** Create `ITranslationManagementProvider` interface + DTOs
  - `Explore.Application/Contracts/Infrastructure/ITranslationManagementProvider.cs`
  - Methods: TestConnectionAsync, ImportKeysAsync, ExportTranslationsAsync, GetAvailableLanguagesAsync
  - DTOs: TranslationKeyImport, TranslationExport

- [x] **2.2** Create `ITranslationConfigResolver` interface + DTO
  - `Explore.Application/Contracts/Infrastructure/ITranslationConfigResolver.cs`
  - TranslationConfiguration record (Provider, ApiUrl, ProjectId, Component, DefaultLanguage)

- [x] **2.3** Create `ITranslationResolver` interface
  - `Explore.Application/Contracts/Infrastructure/ITranslationResolver.cs`
  - Methods: ResolveAsync(key, lang), ResolveBatchAsync(keys, lang)
  - Never returns null — falls back to key itself

- [ ] **2.4** Create CQRS queries: GetAvailableLanguages, GetTranslation, GetTranslationsBatch

- [ ] **2.5** Create CQRS commands: UpdateLocalizationConfig, TestTmsConnection, ExportFromTms, PushToTms

## Phase 3: Infrastructure Layer — Providers (Effort: XL)

- [x] **3.1** Create `NullTranslationProvider` (`Explore.Infrastructure/Localization/NullTranslationProvider.cs`)
  - All methods are safe no-ops

- [x] **3.2** Create `OfflineTranslationProvider` (`Explore.Infrastructure/Localization/OfflineTranslationProvider.cs`)
  - Loads from embedded `Bundles/{lang}.json` files (flat key-value JSON)
  - Singleton lifetime, in-memory dictionary, loaded on first access
  - ExportTranslationsAsync reads from file, ImportKeysAsync is no-op
  - GetAvailableLanguagesAsync returns list of embedded .json file names
  - TestConnectionAsync always returns true

- [x] **3.3** Create `TolgeeTranslationProvider` (`Explore.Infrastructure/Localization/TolgeeTranslationProvider.cs`)
  - POST /v2/projects/{id}/import, GET .../translations/{lang}, GET .../languages
  - X-API-Key auth from SecretProvider, 10s timeout, graceful error handling
  - Key convention: `lookup.{entityType}.{masterCode}.{field}`

- [x] **3.4** Create `WeblateTranslationProvider` (`Explore.Infrastructure/Localization/WeblateTranslationProvider.cs`)
  - POST /api/translations/{proj}/{comp}/{lang}/units/, GET .../file/, GET .../languages
  - Token auth from SecretProvider, 10s timeout
  - Component slug from governance settings

- [x] **3.5** Create `RuntimeTranslationProvider` (`Explore.Infrastructure/Localization/RuntimeTranslationProvider.cs`)
  - Reads provider from ITranslationConfigResolver
  - None → OfflineTranslationProvider (NOT NullProvider!)
  - Tolgee → TolgeeTranslationProvider
  - Weblate → WeblateTranslationProvider
  - On error → OfflineTranslationProvider fallback
  - 5-min config cache

- [x] **3.6** Create `TranslationResolver` (`Explore.Infrastructure/Localization/TranslationResolver.cs`)
  - Implements ITranslationResolver
  - Resolution: RuntimeTranslationProvider → offline fallback → key itself
  - MemoryCache: 30-min TTL (live TMS), 24h (offline)
  - Batch method preloads all keys for language then picks

- [x] **3.7** Create `TranslationConfigResolver` (`Explore.Infrastructure/Localization/TranslationConfigResolver.cs`)
  - Reads GovernanceSettingKeys.Localization.*, API key from SecretProvider
  - 5-min cache per tenant

- [x] **3.8** Register all DI services (`Explore.Infrastructure/InfrastructureServicesRegistration.cs`)
  - HttpClient factory: Tolgee (10s), Weblate (10s)
  - OfflineTranslationProvider as Singleton
  - RuntimeTranslationProvider as ITranslationManagementProvider
  - TranslationResolver as ITranslationResolver

## Phase 4: Offline Translation Bundles (Effort: M)

- [x] **4.1** Create bundle directory + initial files (`Explore.Infrastructure/Localization/Bundles/`)
  - en.json, fr.json, ar.json (empty starters)
  - Flat key-value: `{ "lookup.tag.FIQH.full_name": "Jurisprudence islamique", ... }`
  - Mark as embedded resources in .csproj

- [ ] **4.2** Document translation export workflow (`docs/LOCALIZATION.md`)
  - CI/CD: export from Tolgee/Weblate → commit to Bundles/ → ship in build
  - Example commands for both Tolgee CLI and Weblate API

- [x] **4.3** Seed `localization.*` governance settings in LookupTableSeeder
  - localization.default_language = "en"
  - localization.tms_provider = "none"

## Phase 5: API Layer (Effort: M)

- [x] **5.1** Create `TranslationController` (`Explore.API/Controllers/TranslationController.cs`)
  - GET /api/translation/{languageCode} — AllowAnonymous
  - GET /api/translation/languages — AllowAnonymous

- [x] **5.2** Create `LocalizationAdminController` (`Explore.API/Controllers/LocalizationAdminController.cs`)
  - POST /api/admin/localization/test-connection — Authorize
  - GET /api/admin/localization/configuration — Authorize
  - POST /api/admin/localization/export-from-tms — Authorize

## Phase 6: Testing (Effort: L)

- [x] **6.1** RuntimeTranslationProvider tests — provider routing, fallback, cache (6 tests)
- [x] **6.2** OfflineTranslationProvider tests — bundle loading, language discovery (5 tests)
- [x] **6.3** NullTranslationProvider tests — no-op verification (4 tests)
- [x] **6.4** GetTranslationsQueryHandler tests — handler delegation (2 tests)
- [x] **6.5** TestTmsConnectionCommandHandler tests — success/failure (2 tests)

## Phase 7: Documentation (Effort: S)

- [x] **7.1** Create `docs/LOCALIZATION.md` — architecture, key convention, tiers, API endpoints
- [x] **7.2** Update `docs/CONFIGURATION.md` — localization governance settings section
- [x] **7.3** Update `docs/EXTENSIBILITY.md` — TMS provider abstraction section
- [x] **7.4** Update `schemas/islamu-event.md` — TranslationManagementProviderEnum + Tag/Category unique indexes
- [x] **7.5** Update `CLAUDE.md` — add LOCALIZATION.md to documentation index

---

## Status Summary

| Phase | Tasks | Done | Status |
|-------|-------|------|--------|
| 1. Domain | 4 | 4 | ✅ Complete |
| 2. Application | 5 | 5 | ✅ Complete |
| 3. Infrastructure | 8 | 8 | ✅ Complete |
| 4. Bundles | 3 | 2 | 🟡 Export workflow docs deferred |
| 5. API | 2 | 2 | ✅ Complete |
| 6. Testing | 5 | 5 | ✅ Complete (19 tests passing) |
| 7. Documentation | 5 | 5 | ✅ Complete |
| **Total** | **32** | **31** | ✅ Implementation Complete |
