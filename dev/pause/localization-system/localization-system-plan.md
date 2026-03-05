# Localization System — Implementation Plan

Last Updated: 2026-03-04

## Executive Summary

Implement a **single-source translation system** for the ISLAMU Event platform where **all translations are managed exclusively through a TMS (Tolgee or Weblate)** — either live (connected) or offline (exported translation files).

**Key principles:**
- **One system, not two.** No DB translations table. All translations live in TMS or in exported files from TMS.
- **Tag/Category keep required TenantId.** Instances are fully independent (Islamic events vs tech events share nothing). No global tags.
- **TMS provider abstraction** — `ITranslationManagementProvider` interface (modeled after `RuntimeAnalyticsProvider`) supporting Tolgee and Weblate as pluggable backends.
- **Offline-first.** Self-hosters without TMS use pre-exported translation files shipped as embedded resources. Self-hosters with TMS get live translations.

### Three Tiers of Self-Hoster Experience

| Tier | TMS Connected? | How Translations Work |
|------|---------------|----------------------|
| **Tier 1 — Offline** | No | Translations loaded from exported `.json` files shipped with the app (pre-exported from ISLAMU's Tolgee/Weblate). Single or multi-language depending on which files are included. |
| **Tier 2 — Connected** | Yes (Tolgee or Weblate) | Live translation resolution from TMS API/SDK. Self-hoster can add languages, edit translations, contribute back. |
| **Tier 3 — ISLAMU Global** | Yes + CDN | Tolgee/Weblate + community translators + CDN-cached live updates for global multi-language support. |

---

## Current State Analysis

### What Exists Today

| Component | Status | Notes |
|---|---|---|
| **23 lookup tables** (int PK) | ✅ Seeded | `LookupTableSeeder.cs` — English-only FullName, no translation support |
| **Tag/Category** (Guid PK, required TenantId) | ✅ Exists | Per-tenant, no unique constraint on (TenantId, MasterCode) yet |
| **EventType** (int PK, nullable TenantId) | ✅ Exists | Dual unique constraint — global + tenant-specific |
| **Language entity** | ✅ Seeded | `Language` with MasterCode (ISO codes), FullName |
| **Analytics provider abstraction** | ✅ Exists | `IAnalyticsProvider` → `RuntimeAnalyticsProvider` → PostHog/Plausible/Rybbit/RudderStack/Null. **The pattern to replicate.** |
| **Email provider abstraction** | ✅ Exists | `IEmailService` → `SmtpEmailService` with `ISmtpConfigResolver` |
| **Governance settings cascade** | ✅ Exists | `SettingsResolver` with system → tenant override → fallback. 5-min cache |
| **GovernanceSettingKeys** | ✅ Exists | No `Localization` or `TranslationManagement` group yet |
| **TMS integration** | ❌ Missing | No Tolgee/Weblate code |
| **Offline translation files** | ❌ Missing | No exported translation bundles |
| **Instance language config** | ❌ Missing | No `Instance:DefaultLanguage` setting |

### Key Files

| File | Purpose |
|---|---|
| `Explore.Domain/Tag.cs` | Tag entity — required TenantId (stays required) |
| `Explore.Domain/Category.cs` | Category entity — required TenantId (stays required), hierarchical (ParentId) |
| `Explore.Domain/EventType.cs` | EventType — nullable TenantId, dual unique constraint |
| `Explore.Domain/Language.cs` | Language lookup — non-tenant, MasterCode = ISO code |
| `Explore.Persistence/Seed/LookupTableSeeder.cs` | Seeds 23 lookup tables at runtime (all environments) |
| `Explore.Application/Contracts/Infrastructure/IAnalyticsProvider.cs` | Analytics abstraction interface — **blueprint** |
| `Explore.Infrastructure/Analytics/RuntimeAnalyticsProvider.cs` | Runtime-switchable analytics wrapper — **blueprint** |
| `Explore.Domain/Constants/GovernanceSettingKeys.cs` | All governance key constants |
| `Explore.Domain/Enums/AnalyticsProviderEnum.cs` | Provider enum (None=0, Posthog=1, ...) — **blueprint** |
| `Explore.Infrastructure/InfrastructureServicesRegistration.cs` | DI registration for all infra services |

---

## Proposed Future State

### Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                        ISLAMU Event                               │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │  ITranslationManagementProvider (single source of truth)    │  │
│  │                                                              │  │
│  │  RuntimeTranslationProvider (wrapper, selects at runtime)    │  │
│  │    ├── TolgeeTranslationProvider   (live, connected)         │  │
│  │    ├── WeblateTranslationProvider  (live, connected)         │  │
│  │    ├── OfflineTranslationProvider  (exported .json files)    │  │
│  │    └── NullTranslationProvider     (no translations, en-only)│  │
│  └─────────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │  Translation Resolution (unified for ALL strings)            │  │
│  │                                                              │  │
│  │  ITranslationResolver                                        │  │
│  │    resolve("lookup.tag.FIQH.full_name", "fr")                │  │
│  │    resolve("ui.button.save", "fr")                           │  │
│  │                                                              │  │
│  │  Priority:                                                   │  │
│  │    1. TMS live (if Tolgee/Weblate connected)                 │  │
│  │    2. Offline file (exported .json from TMS)                 │  │
│  │    3. Fallback key (English FullName / key itself)           │  │
│  └─────────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │  Translation Bundles (shipped with app)                      │  │
│  │                                                              │  │
│  │  Explore.Infrastructure/Localization/Bundles/                │  │
│  │    ├── en.json    (exported from Tolgee/Weblate)             │  │
│  │    ├── fr.json    (exported from Tolgee/Weblate)             │  │
│  │    ├── ar.json    (exported from Tolgee/Weblate)             │  │
│  │    └── ...                                                   │  │
│  │                                                              │  │
│  │  Key format: "lookup.madhab.HANAFI.full_name" = "Hanafite"   │  │
│  │              "lookup.tag.FIQH.full_name" = "Jurisprudence"   │  │
│  │              "ui.page.events.title" = "Événements"           │  │
│  └─────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────┘
```

### Translation Key Convention

All translatable strings use a dot-separated key:

```
lookup.{entity_type}.{master_code}.{field}
  → lookup.madhab.HANAFI.full_name
  → lookup.madhab.HANAFI.description
  → lookup.tag.FIQH.full_name
  → lookup.event_type.CONFERENCE.full_name

ui.{area}.{component}.{element}
  → ui.page.events.title
  → ui.button.save
  → ui.validation.required_field
```

### Translation Resolution Flow

```
Request for translate("lookup.tag.FIQH.full_name", "fr")
         │
         ▼
  ┌─ TMS connected? (Tolgee/Weblate)
  │   YES → query TMS API/SDK for key in language "fr"
  │          found? → return "Jurisprudence islamique" ✅
  │          not found? ↓
  │
  ├─ Offline bundle exists for "fr"?
  │   YES → look up key in fr.json
  │          found? → return "Jurisprudence islamique" ✅
  │          not found? ↓
  │
  └─ Fallback → return entity.FullName (English default)
```

### What DOES NOT change

- **Tag/Category keep required TenantId** — each instance is independent, no global tags
- **Lookup tables keep FullName** — FullName is the English default / key fallback
- **LookupTableSeeder stays** — seeds English data as before
- **No DB translation table** — translations live in TMS + offline files only

---

## Implementation Phases

### Phase 1: Domain Layer — Enums & Constants
**Estimated Effort: S**
**Related Skills:** `clean-architecture-rules`

#### Task 1.1: Create `TranslationManagementProviderEnum`
- **File:** `Explore.Domain/Enums/TranslationManagementProviderEnum.cs`
- **Values:** `None = 0`, `Tolgee = 1`, `Weblate = 2`
- **Acceptance Criteria:**
  - [ ] Enum file with ABOUTME header, file-scoped namespace
  - [ ] Matches `AnalyticsProviderEnum` pattern

#### Task 1.2: Add `GovernanceSettingKeys.Localization` group
- **File:** `Explore.Domain/Constants/GovernanceSettingKeys.cs`
- **Keys:**
  - `localization.default_language` — instance default language (ISO code, default "en")
  - `localization.tms_provider` — "None" / "Tolgee" / "Weblate"
  - `localization.tms_api_url` — TMS base URL
  - `localization.tms_project_id` — Tolgee project ID / Weblate project slug
  - `localization.tms_component` — Weblate component slug (Weblate-specific)
- **Acceptance Criteria:**
  - [ ] Keys follow existing naming convention (`group.setting_name`)
  - [ ] No secrets in governance keys (API key/token stored via SecretProvider)

#### Task 1.3: Add unique constraint on Tag (TenantId, MasterCode)
- **File:** `Explore.Persistence/Configurations/Entities/TagConfiguration.cs`
- **Add:** `UNIQUE(TenantId, MasterCode)` — all tags are tenant-scoped, simple composite unique
- **Acceptance Criteria:**
  - [ ] Unique index `ix_tags_tenant_master_code` on (tenant_id, master_code)
  - [ ] Tag.TenantId stays required — no changes to domain entity

#### Task 1.4: Add unique constraint on Category (TenantId, MasterCode)
- **File:** `Explore.Persistence/Configurations/Entities/CategoryConfiguration.cs`
- **Same pattern as 1.3
- **Acceptance Criteria:**
  - [ ] Unique index `ix_categories_tenant_master_code` on (tenant_id, master_code)

---

### Phase 2: Application Layer — Contracts
**Estimated Effort: M**
**Related Skills:** `cqrs-mediatr-guidelines`, `clean-architecture-rules`

#### Task 2.1: Create `ITranslationManagementProvider` interface
- **File:** `Explore.Application/Contracts/Infrastructure/ITranslationManagementProvider.cs`
- **Interface:**
  ```csharp
  public interface ITranslationManagementProvider
  {
      Task<bool> TestConnectionAsync(CancellationToken ct = default);
      Task ImportKeysAsync(IEnumerable<TranslationKeyImport> keys, CancellationToken ct = default);
      Task<IEnumerable<TranslationExport>> ExportTranslationsAsync(string languageCode, CancellationToken ct = default);
      Task<IEnumerable<string>> GetAvailableLanguagesAsync(CancellationToken ct = default);
  }
  ```
- **Acceptance Criteria:**
  - [ ] Interface in Application.Contracts (no infrastructure dependency)
  - [ ] DTOs for `TranslationKeyImport` and `TranslationExport` defined alongside

#### Task 2.2: Create `ITranslationConfigResolver` interface
- **File:** `Explore.Application/Contracts/Infrastructure/ITranslationConfigResolver.cs`
- **Interface:**
  ```csharp
  public interface ITranslationConfigResolver
  {
      Task<TranslationConfiguration> ResolveAsync(CancellationToken ct = default);
      void InvalidateCache(Guid? tenantId = null);
  }
  ```
- **Acceptance Criteria:**
  - [ ] `TranslationConfiguration` record: Provider (enum), ApiUrl, ProjectId, Component, DefaultLanguage

#### Task 2.3: Create `ITranslationResolver` interface
- **File:** `Explore.Application/Contracts/Infrastructure/ITranslationResolver.cs`
- **Purpose:** Unified translation resolution — single method to get any translated string
- **Interface:**
  ```csharp
  public interface ITranslationResolver
  {
      Task<string> ResolveAsync(string key, string languageCode, CancellationToken ct = default);
      Task<IDictionary<string, string>> ResolveBatchAsync(IEnumerable<string> keys, string languageCode, CancellationToken ct = default);
  }
  ```
- **Acceptance Criteria:**
  - [ ] Returns the key itself (or FullName fallback) when no translation found
  - [ ] Batch method for list views (resolve all tags in one call)

#### Task 2.4: Create CQRS commands/queries
- **Queries:** `GetAvailableLanguages`, `GetTranslation`, `GetTranslationsBatch`
- **Commands:** `UpdateLocalizationConfig`, `TestTmsConnection`, `ExportTranslationsFromTms`, `ImportTranslationsToTms`
- **Acceptance Criteria:**
  - [ ] Commands return `BaseCommandResponse<Guid>` per CLAUDE.md rules
  - [ ] Queries are AllowAnonymous, commands are Authorize

---

### Phase 3: Infrastructure Layer — Providers
**Estimated Effort: XL**
**Related Skills:** `clean-architecture-rules`

#### Task 3.1: Create `NullTranslationProvider`
- **File:** `Explore.Infrastructure/Localization/NullTranslationProvider.cs`
- **Behavior:** All methods return empty/success — no-op
- **Acceptance Criteria:**
  - [ ] Implements `ITranslationManagementProvider`
  - [ ] All methods are safe no-ops

#### Task 3.2: Create `OfflineTranslationProvider`
- **File:** `Explore.Infrastructure/Localization/OfflineTranslationProvider.cs`
- **Behavior:**
  - Loads translation bundles from embedded `.json` files (`Bundles/{lang}.json`)
  - Files are flat key-value JSON: `{ "lookup.tag.FIQH.full_name": "Jurisprudence islamique", ... }`
  - These files are **exported from Tolgee/Weblate** and shipped with the app build
  - Read-only: `ImportKeysAsync` is a no-op, `ExportTranslationsAsync` reads from embedded file
  - `GetAvailableLanguagesAsync` returns list of embedded `*.json` file names
  - **In-memory dictionary** loaded on first access, cached for app lifetime
- **Acceptance Criteria:**
  - [ ] Loads from assembly embedded resources
  - [ ] Returns translations from file for `ExportTranslationsAsync`
  - [ ] `TestConnectionAsync` returns true (always available)
  - [ ] Import is a no-op (offline files are read-only at runtime)

#### Task 3.3: Create `TolgeeTranslationProvider`
- **File:** `Explore.Infrastructure/Localization/TolgeeTranslationProvider.cs`
- **API endpoints:**
  - `POST /v2/projects/{id}/import` — import keys+translations
  - `GET /v2/projects/{id}/translations/{lang}` — export
  - `GET /v2/projects/{id}/languages` — list languages
  - `GET /v2/projects/{id}` — test connection
- **Auth:** `X-API-Key` header from SecretProvider
- **Acceptance Criteria:**
  - [ ] Uses HttpClient via DI (10s timeout)
  - [ ] Key convention: `lookup.{entityType}.{masterCode}.{field}`
  - [ ] Error handling: log + return failure, never throw
  - [ ] Batch import for efficiency

#### Task 3.4: Create `WeblateTranslationProvider`
- **File:** `Explore.Infrastructure/Localization/WeblateTranslationProvider.cs`
- **API endpoints:**
  - `POST /api/translations/{proj}/{comp}/{lang}/units/` — create/update
  - `GET /api/translations/{proj}/{comp}/{lang}/file/` — export
  - `GET /api/projects/{proj}/languages/` — list languages
  - `GET /api/projects/{proj}/` — test connection
- **Auth:** `Authorization: Token {token}` from SecretProvider
- **Acceptance Criteria:**
  - [ ] Same pattern as Tolgee provider
  - [ ] Component slug from governance settings

#### Task 3.5: Create `RuntimeTranslationProvider`
- **File:** `Explore.Infrastructure/Localization/RuntimeTranslationProvider.cs`
- **Pattern:** Matches `RuntimeAnalyticsProvider`:
  - Reads provider from `ITranslationConfigResolver`
  - `None` → `OfflineTranslationProvider` (NOT NullProvider — offline bundles are always available)
  - `Tolgee` → `TolgeeTranslationProvider`
  - `Weblate` → `WeblateTranslationProvider`
  - On error → falls back to `OfflineTranslationProvider`
  - 5-min cache for config resolution
- **Acceptance Criteria:**
  - [ ] Runtime-switchable via governance settings
  - [ ] Default (None) uses offline bundles, not null/empty
  - [ ] Graceful degradation to offline on TMS connection errors

#### Task 3.6: Create `TranslationResolver`
- **File:** `Explore.Infrastructure/Localization/TranslationResolver.cs`
- **Implements:** `ITranslationResolver`
- **Resolution chain:**
  1. Ask `ITranslationManagementProvider.ExportTranslationsAsync(lang)` for the key
  2. If TMS returned nothing → try `OfflineTranslationProvider` explicitly
  3. If still nothing → return key itself or FullName fallback
- **Caching:** HybridCache with `Translations_{lang}` key, 30-min TTL (live); app-lifetime (offline)
- **Acceptance Criteria:**
  - [ ] Never returns null — always falls back to something displayable
  - [ ] Batch method is efficient (loads all keys for lang, then picks)
  - [ ] Cache is invalidated when admin triggers re-export from TMS

#### Task 3.7: Create `TranslationConfigResolver`
- **File:** `Explore.Infrastructure/Localization/TranslationConfigResolver.cs`
- **Pattern:** Matches `AnalyticsConfigResolver`
- **Acceptance Criteria:**
  - [ ] Reads GovernanceSettingKeys.Localization.*
  - [ ] API key from SecretProvider
  - [ ] 5-min cache per tenant

#### Task 3.8: Register all DI services
- **File:** `Explore.Infrastructure/InfrastructureServicesRegistration.cs`
- **Registration:**
  ```csharp
  services.AddHttpClient<TolgeeTranslationProvider>(client => client.Timeout = TimeSpan.FromSeconds(10));
  services.AddHttpClient<WeblateTranslationProvider>(client => client.Timeout = TimeSpan.FromSeconds(10));
  services.AddSingleton<OfflineTranslationProvider>();  // app-lifetime, read-only
  services.AddScoped<NullTranslationProvider>();
  services.AddScoped<ITranslationConfigResolver, TranslationConfigResolver>();
  services.AddScoped<RuntimeTranslationProvider>();
  services.AddScoped<ITranslationManagementProvider>(sp => sp.GetRequiredService<RuntimeTranslationProvider>());
  services.AddScoped<ITranslationResolver, TranslationResolver>();
  ```
- **Acceptance Criteria:**
  - [ ] OfflineTranslationProvider is Singleton (loaded once, read-only)
  - [ ] RuntimeTranslationProvider resolves as ITranslationManagementProvider

---

### Phase 4: Offline Translation Bundles
**Estimated Effort: M**

#### Task 4.1: Create initial translation bundle structure
- **Directory:** `Explore.Infrastructure/Localization/Bundles/`
- **Files:** `en.json`, `fr.json`, `ar.json` (minimum)
- **Format:** Flat key-value JSON exported from Tolgee/Weblate:
  ```json
  {
    "lookup.madhab.HANAFI.full_name": "Hanafi",
    "lookup.madhab.HANAFI.description": "One of the four major schools of Islamic jurisprudence",
    "lookup.tag.FIQH.full_name": "Fiqh",
    "lookup.event_type.CONFERENCE.full_name": "Conference",
    "ui.page.events.title": "Events",
    "ui.button.save": "Save"
  }
  ```
- **These files are generated by exporting from Tolgee/Weblate** — they are NOT hand-written. For initial bootstrap, create them manually with lookup translations only.
- **Acceptance Criteria:**
  - [ ] Embedded as assembly resources in .csproj
  - [ ] All 23 lookup tables' FullNames have translation keys
  - [ ] At least en, fr, ar bundles

#### Task 4.2: Create CI/CD export script (documentation only)
- **File:** `docs/LOCALIZATION.md` — document the workflow:
  1. Translators work in Tolgee/Weblate
  2. CI pipeline exports translations: `tolgee export --format json` or Weblate API
  3. Exported files committed to `Explore.Infrastructure/Localization/Bundles/`
  4. App build embeds these as resources
  5. Self-hosters without TMS get the latest translations from last build
- **Acceptance Criteria:**
  - [ ] Clear documentation of export workflow
  - [ ] Example CI commands for Tolgee and Weblate

#### Task 4.3: Seed `localization.*` governance settings
- **File:** `Explore.Persistence/Seed/LookupTableSeeder.cs` (in `SeedSystemSettingsAsync`)
- **Add keys:**
  - `localization.default_language` = "en"
  - `localization.tms_provider` = "None" (uses offline bundles by default)
- **Acceptance Criteria:**
  - [ ] Keys seeded with sensible defaults
  - [ ] Existing settings not overwritten

---

### Phase 5: API Layer
**Estimated Effort: M**
**Related Skills:** `cqrs-mediatr-guidelines`

#### Task 5.1: Create translation query endpoints
- **Controller:** `Explore.API/Controllers/TranslationController.cs`
- **Endpoints:**
  - `GET /api/translations/{languageCode}` — get all translations for a language
  - `GET /api/translations/{key}/{languageCode}` — get single translation
  - `GET /api/translations/languages` — list available languages
- **Auth:** `AllowAnonymous` (GET pattern per CLAUDE.md)
- **Acceptance Criteria:**
  - [ ] MediatR query handlers
  - [ ] Output cache (1h for offline, 30min for live TMS)
  - [ ] Returns resolved translation via `ITranslationResolver`

#### Task 5.2: Create admin TMS management endpoints
- **Controller:** `Explore.API/Controllers/Admin/LocalizationAdminController.cs`
- **Endpoints:**
  - `POST /api/admin/localization/test-connection` — test TMS connectivity
  - `GET /api/admin/localization/configuration` — get current localization config
  - `PUT /api/admin/localization/configuration` — update localization settings
  - `POST /api/admin/localization/export-from-tms` — pull translations from TMS and cache
  - `POST /api/admin/localization/push-to-tms` — push lookup FullNames as keys to TMS
- **Auth:** `Authorize(Roles = "PlatformAdmin")`
- **Acceptance Criteria:**
  - [ ] MediatR command handlers
  - [ ] Push-to-TMS reads all lookup tables, generates keys, sends via ITranslationManagementProvider
  - [ ] Export-from-TMS refreshes the cached translations

---

### Phase 6: Testing
**Estimated Effort: L**

#### Task 6.1: Domain unit tests — TranslationManagementProviderEnum
#### Task 6.2: Application unit tests — translation query/command handlers, resolver fallback logic
#### Task 6.3: Architecture tests — interface in Application.Contracts, implementations in Infrastructure
#### Task 6.4: Infrastructure unit tests (mock HTTP) — Tolgee + Weblate providers, offline provider
#### Task 6.5: Integration tests — resolver chain (live → offline → fallback), admin config flow

---

### Phase 7: Documentation
**Estimated Effort: S**

#### Task 7.1: Create `docs/LOCALIZATION.md`
- Translation architecture, key convention, export workflow, self-hoster tiers
#### Task 7.2: Update `docs/CONFIGURATION.md` — localization settings
#### Task 7.3: Update `docs/EXTENSIBILITY.md` — TMS provider abstraction
#### Task 7.4: Update `schemas/islamu-event.md` — add translation_management_provider_enum, Tag/Category unique indexes

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| **Tolgee/Weblate API breaking changes** | Low | Medium | Abstract behind interface; version-pin API calls; integration tests |
| **Offline bundle staleness** | Medium | Low | CI/CD exports on every release; bundles are a snapshot of last build |
| **Resolution chain latency** (TMS API call per request) | Medium | Medium | 30-min HybridCache; batch resolution; preload on app start |
| **Translation key drift** (code renames key, TMS still has old key) | Medium | Medium | Push-to-TMS admin action regenerates keys from current lookup data |
| **Self-hoster confusion** (which tier am I on?) | Low | Low | Admin settings page shows current status + available languages |

## Success Metrics

1. Self-hoster without TMS sees UI and lookup data in available bundle languages
2. Self-hoster connects Tolgee/Weblate and sees live translations immediately
3. Admin can push current lookup FullNames to TMS as translation keys
4. `GET /api/translations/lookup.tag.FIQH.full_name/fr` returns "Jurisprudence islamique"
5. Offline bundles updated automatically in CI/CD from TMS export
6. All existing tests pass (646+) plus new localization-specific tests

## Potential Risks & Unknowns

The **highest-risk aspect is the resolution chain latency for live TMS mode.** Every API request that needs translated lookup data must resolve through the TMS provider, which is an HTTP call to an external service. Without aggressive caching (30-min HybridCache + preload on startup), this could add 50-200ms per request. The mitigation is clear (cache), but the **cache invalidation strategy needs careful design** — when a translator updates a string in Tolgee, how quickly does it appear in the app? The answer is "up to 30 minutes" with the default cache TTL, which is acceptable for lookup data but may frustrate translators testing their changes. Consider exposing a "flush translation cache" admin endpoint.
