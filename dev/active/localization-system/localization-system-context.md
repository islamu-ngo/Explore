# Localization System — Key Context

Last Updated: 2026-03-04

## Key Decisions Made

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | **Single translation source: TMS only** | No DB translations table. All translations come from Tolgee/Weblate (live) or exported `.json` files (offline). One system, not two. |
| 2 | **Tag/Category keep required TenantId** | Instances are fully independent — Islamic events and tech events share zero tags/categories. No global tags concept. |
| 3 | **Offline-first with bundled exports** | Self-hosters without TMS use pre-exported `.json` files shipped as embedded resources. These files are exported from ISLAMU's Tolgee/Weblate. |
| 4 | **Multi-provider TMS abstraction** (Tolgee + Weblate) | Self-hosters choose their TMS. Both are open-source, self-hostable, with REST APIs. Abstraction uses the proven RuntimeAnalyticsProvider pattern. |
| 5 | **Unified key convention** for ALL translatable strings | `lookup.{entity_type}.{master_code}.{field}` for DB content, `ui.{area}.{component}.{element}` for UI strings. Same system resolves both. |
| 6 | **OfflineTranslationProvider as default** | When `tms_provider = None`, the system uses `OfflineTranslationProvider` (reads bundled `.json` files), NOT `NullTranslationProvider`. Self-hosters always get translations from last build. |
| 7 | **No `lookup_translations` DB table** | ~~Removed~~. Translations do not live in the database. They live in TMS (live) or exported files (offline). The DB stores only English FullName as the key fallback. |

## Critical Design Constraints

1. **Repositories return entities, never DTOs** — translation resolution happens in handlers or a translation resolver service
2. **Validators are manually instantiated** — no DI for validation
3. **Navigation properties are readonly**
4. **GET = AllowAnonymous, write = Authorize** — translation queries are public, admin endpoints require PlatformAdmin role
5. **File-scoped namespaces** for all new C# files
6. **ABOUTME: header** required on all new files
7. **No default values in domain entities** — set in EF configs or handlers
8. **Commands return `BaseCommandResponse<Guid>`**

## Core Interface Signatures

### ITranslationManagementProvider

```csharp
// File: Explore.Application/Contracts/Infrastructure/ITranslationManagementProvider.cs
public interface ITranslationManagementProvider
{
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
    Task ImportKeysAsync(IEnumerable<TranslationKeyImport> keys, CancellationToken ct = default);
    Task<IEnumerable<TranslationExport>> ExportTranslationsAsync(string languageCode, CancellationToken ct = default);
    Task<IEnumerable<string>> GetAvailableLanguagesAsync(CancellationToken ct = default);
}

public record TranslationKeyImport(string KeyName, IDictionary<string, string> Translations);
public record TranslationExport(string KeyName, string Value);
```

### ITranslationConfigResolver

```csharp
// File: Explore.Application/Contracts/Infrastructure/ITranslationConfigResolver.cs
public interface ITranslationConfigResolver
{
    Task<TranslationConfiguration> ResolveAsync(CancellationToken ct = default);
    void InvalidateCache(Guid? tenantId = null);
}

public record TranslationConfiguration(
    TranslationManagementProviderEnum Provider,
    string? ApiUrl,
    string? ProjectId,
    string? Component,
    string DefaultLanguage
);
```

### ITranslationResolver

```csharp
// File: Explore.Application/Contracts/Infrastructure/ITranslationResolver.cs
// Unified translation resolution — single entry point for all translation needs
public interface ITranslationResolver
{
    Task<string> ResolveAsync(string key, string languageCode, CancellationToken ct = default);
    Task<IDictionary<string, string>> ResolveBatchAsync(IEnumerable<string> keys, string languageCode, CancellationToken ct = default);
}
```

## Existing Patterns to Follow

### RuntimeAnalyticsProvider Pattern (exact blueprint)

```
IAnalyticsProvider (interface, Application.Contracts)
  ├── PostHogAnalyticsProvider (HttpClient, Infrastructure/Analytics)
  ├── PlausibleAnalyticsProvider (HttpClient, Infrastructure/Analytics)
  ├── RybbitAnalyticsProvider (HttpClient, Infrastructure/Analytics)
  ├── RudderStackAnalyticsProvider (HttpClient, Infrastructure/Analytics)
  ├── NullAnalyticsProvider (no-op, Infrastructure/Analytics)
  └── RuntimeAnalyticsProvider (wrapper, selects active via IAnalyticsConfigResolver)

IAnalyticsConfigResolver → AnalyticsConfigResolver (reads GovernanceSettingKeys.Analytics.*)
AnalyticsProviderEnum: None=0, Posthog=1, Plausible=2, Rybbit=3, RudderStack=4

DI: All concrete providers registered. RuntimeAnalyticsProvider resolved as IAnalyticsProvider.
```

### Translation Provider Tree (new, mirrors analytics)

```
ITranslationManagementProvider (interface, Application.Contracts)
  ├── TolgeeTranslationProvider (HttpClient, Infrastructure/Localization)
  ├── WeblateTranslationProvider (HttpClient, Infrastructure/Localization)
  ├── OfflineTranslationProvider (Singleton, reads bundled .json files)
  ├── NullTranslationProvider (no-op)
  └── RuntimeTranslationProvider (wrapper, selects via ITranslationConfigResolver)
      - None → OfflineTranslationProvider (NOT NullProvider)
      - Tolgee → TolgeeTranslationProvider
      - Weblate → WeblateTranslationProvider
      - On error → OfflineTranslationProvider (graceful fallback)

ITranslationResolver (unified resolution)
  └── TranslationResolver
      1. Ask RuntimeTranslationProvider (live TMS or offline)
      2. If empty → return key fallback (English FullName)
      3. HybridCache: 30-min TTL (live), app-lifetime (offline)
```

### GovernanceSettingKeys Group Pattern

```csharp
public static class Localization
{
    public const string DefaultLanguage = "localization.default_language";
    public const string TmsProvider = "localization.tms_provider";
    public const string TmsApiUrl = "localization.tms_api_url";
    public const string TmsProjectId = "localization.tms_project_id";
    public const string TmsComponent = "localization.tms_component";
}
```

## TMS API Mapping

| Operation | Tolgee API | Weblate API |
|---|---|---|
| Test connection | `GET /v2/projects/{id}` | `GET /api/projects/{slug}/` |
| Import keys | `POST /v2/projects/{id}/import` | `POST /api/translations/{proj}/{comp}/{lang}/units/` |
| Export translations | `GET /v2/projects/{id}/translations/{lang}` | `GET /api/translations/{proj}/{comp}/{lang}/file/` |
| List languages | `GET /v2/projects/{id}/languages` | `GET /api/projects/{proj}/languages/` |

**Auth headers:**
- Tolgee: `X-API-Key: {key}` (from SecretProvider)
- Weblate: `Authorization: Token {token}` (from SecretProvider)

## Translation Key Convention

```
# Lookup table translations (DB content)
lookup.{entity_type}.{master_code}.full_name
lookup.{entity_type}.{master_code}.description

Examples:
  lookup.tag.FIQH.full_name         → "Jurisprudence islamique" (fr)
  lookup.madhab.HANAFI.full_name    → "Hanafite" (fr)
  lookup.madhab.HANAFI.description  → "L'une des quatre écoles" (fr)
  lookup.event_type.CONFERENCE.full_name → "Conférence" (fr)

# UI strings (code artifacts)
ui.{area}.{component}.{element}

Examples:
  ui.page.events.title              → "Événements" (fr)
  ui.button.save                    → "Enregistrer" (fr)
  ui.validation.required_field      → "Ce champ est obligatoire" (fr)
```

## Offline Bundle Format

Flat key-value JSON, one file per language, exported from TMS:

```json
// Bundles/fr.json
{
  "lookup.madhab.HANAFI.full_name": "Hanafite",
  "lookup.madhab.MALIKI.full_name": "Malikite",
  "lookup.tag.FIQH.full_name": "Jurisprudence islamique",
  "lookup.event_type.CONFERENCE.full_name": "Conférence",
  "ui.page.events.title": "Événements",
  "ui.button.save": "Enregistrer"
}
```

## File Map (all new files)

```
Explore.Domain/
  ├── Enums/
  │   └── TranslationManagementProviderEnum.cs       (NEW — enum)
  └── Constants/
      └── GovernanceSettingKeys.cs                   (EDIT — add Localization group)

Explore.Application/
  └── Contracts/
      └── Infrastructure/
          ├── ITranslationManagementProvider.cs      (NEW — interface + DTOs)
          ├── ITranslationConfigResolver.cs          (NEW — config resolver)
          └── ITranslationResolver.cs                (NEW — unified resolver)

Explore.Persistence/
  ├── Configurations/Entities/
  │   ├── TagConfiguration.cs                        (EDIT — add unique index)
  │   └── CategoryConfiguration.cs                   (EDIT — add unique index)
  └── Seed/
      └── LookupTableSeeder.cs                      (EDIT — seed localization settings)

Explore.Infrastructure/
  ├── Localization/
  │   ├── NullTranslationProvider.cs                 (NEW)
  │   ├── OfflineTranslationProvider.cs              (NEW — reads bundled .json)
  │   ├── TolgeeTranslationProvider.cs               (NEW)
  │   ├── WeblateTranslationProvider.cs              (NEW)
  │   ├── RuntimeTranslationProvider.cs              (NEW — wrapper)
  │   ├── TranslationResolver.cs                     (NEW — unified resolution)
  │   ├── TranslationConfigResolver.cs               (NEW)
  │   └── Bundles/
  │       ├── en.json                                (NEW — embedded resource)
  │       ├── fr.json                                (NEW — embedded resource)
  │       └── ar.json                                (NEW — embedded resource)
  └── InfrastructureServicesRegistration.cs          (EDIT — register DI)

Explore.API/
  └── Controllers/
      ├── TranslationController.cs                   (NEW — public queries)
      └── Admin/
          └── LocalizationAdminController.cs         (NEW — admin management)

schemas/
  └── islamu-event.md                                (EDIT — enum + indexes only, no new table)
```

## Dependencies

```
Phase 1 (Domain: enum + settings + indexes) ──► Phase 2 (Application: contracts)
                                                       │
                                                       ▼
                                                Phase 3 (Infrastructure: providers)
                                                       │
                                                       ▼
                                                Phase 4 (Bundles + seeder)
                                                       │
                                                       ▼
                                                Phase 5 (API endpoints)
                                                       │
                                                       ▼
                                                Phase 6 (Tests)
                                                       │
                                                       ▼
                                                Phase 7 (Docs)
```
