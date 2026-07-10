ABOUTME: Architecture and usage guide for the localization/i18n system.
ABOUTME: Covers TMS provider abstraction (Tolgee/Weblate), offline bundles, key conventions, and self-hoster tiers.

# Localization

## Overview

All translations are managed through a **Translation Management System (TMS)** — either live (connected to Tolgee or Weblate) or offline (pre-exported `.json` bundles shipped with the app). There is **no translations table** in the database; the TMS is the single source of truth.

## Self-Hoster Tiers

| Tier | TMS Connected? | How Translations Work |
|------|---------------|----------------------|
| **Tier 1 — Offline** | No | Bundled `.json` files shipped as embedded resources. Single or multi-language depending on which files are included. |
| **Tier 2 — Connected** | Yes (Tolgee or Weblate) | Live translation resolution from TMS API. Self-hoster can add languages, edit translations. |
| **Tier 3 — ISLAMU Global** | Yes + CDN | Tolgee/Weblate + community translators + CDN-cached live updates. |

## Architecture

```
┌─────────────────────────────┐
│   ITranslationResolver      │  ← Unified entry point (ResolveAsync / ResolveBatchAsync)
│   TranslationResolver       │     Preloads full language, caches 30min (live) / 24h (offline)
└─────────────┬───────────────┘
              │
┌─────────────▼───────────────┐
│ ITranslationManagementProvider │  ← Pluggable TMS backend
│ RuntimeTranslationProvider     │     Reads config → routes to active provider
└─────────────┬───────────────┘
              │
    ┌─────────┼──────────┐
    ▼         ▼          ▼
 Tolgee   Weblate    Offline
Provider  Provider   Provider
              │
    ┌─────────▼──────────┐
    │ ITranslationConfig  │
    │ Resolver             │  ← Reads governance settings (5-min cache)
    └─────────────────────┘
```

### Key Components

| Component | Location | Lifetime |
|---|---|---|
| `ITranslationManagementProvider` | `Explore.Application/Contracts/Infrastructure/` | — |
| `ITranslationConfigResolver` | `Explore.Application/Contracts/Infrastructure/` | — |
| `ITranslationResolver` | `Explore.Application/Contracts/Infrastructure/` | — |
| `RuntimeTranslationProvider` | `Explore.Infrastructure/Localization/` | Scoped |
| `TolgeeTranslationProvider` | `Explore.Infrastructure/Localization/` | Scoped |
| `WeblateTranslationProvider` | `Explore.Infrastructure/Localization/` | Scoped |
| `OfflineTranslationProvider` | `Explore.Infrastructure/Localization/` | Singleton |
| `BundleSchema` | `Explore.Infrastructure/Localization/` | Internal helper |
| `NullTranslationProvider` | `Explore.Infrastructure/Localization/` | Singleton |
| `TranslationResolver` | `Explore.Infrastructure/Localization/` | Scoped |
| `TranslationConfigResolver` | `Explore.Infrastructure/Localization/` | Scoped |

Tolgee and Weblate provider calls use NSwag-generated clients generated from
provider-normalized schema slices:

- `schemas/openapi-tolgee-provider.yaml` → `Explore.Infrastructure/Localization/Generated/Tolgee/TolgeeApiClient.g.cs`
- `schemas/openapi-weblate-provider.yaml` → `Explore.Infrastructure/Localization/Generated/Weblate/WeblateApiClient.g.cs`

The raw upstream schemas remain checked in at `schemas/openapi-tolgee.json` and
`schemas/openapi-weblate.yaml`. The provider slices only correct schema metadata
that blocks usable generated clients, such as Tolgee's missing `projectId` path
parameters and Weblate's multipart file upload type.

### Provider Resolution

When `tms_provider` governance setting is:
- `None` (0) → `OfflineTranslationProvider` (reads embedded `.json` bundles)
- `Tolgee` (1) → `TolgeeTranslationProvider` (Tolgee REST API)
- `Weblate` (2) → `WeblateTranslationProvider` (Weblate REST API)

On error, `RuntimeTranslationProvider` falls back to `OfflineTranslationProvider` (graceful degradation).

## Translation Key Convention

### Lookup Tables (DB content)
```
lookup.{entity_type}.{master_code}.{field}
```

Application code builds lookup translation keys through
`Explore.Application.Localization.TranslationKeys.Lookup(entityType, masterCode, field)`.
The `masterCode` argument is the lookup row's stable `MasterCode` value. Never build lookup
translation keys from database IDs, localized labels, or display names.

Examples:
- `lookup.tag.FIQH.full_name` → "Jurisprudence" (French)
- `lookup.category.EDUCATION.description` → "Programmes éducatifs"
- `lookup.event_type.CONFERENCE.full_name` → "مؤتمر" (Arabic)

### UI Strings
```
ui.{area}.{component}.{element}
```
Examples:
- `ui.button.save` → "Enregistrer"
- `ui.event.list.title` → "Événements"
- `ui.registration.confirm.message` → "تأكيد التسجيل"

## Governance Settings

All localization config is stored as governance settings (system-level defaults, tenant overrides supported):

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `localization.default_language` | string | `"en"` | Default language code (ISO 639-1) |
| `localization.tms_provider` | int | `0` (None) | TMS provider enum value |
| `localization.tms_api_url` | string | `null` | Base URL for TMS API |
| `localization.tms_project_id` | string | `null` | TMS project identifier |
| `localization.tms_component` | string | `null` | Weblate component slug (Weblate only) |

Seed IDs: 560–564 in `LookupTableSeeder`.

Extended governance keys (seed IDs 565–568):

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `localization.enabled_languages` | string | `"en,fr,ar"` | Comma-separated culture codes admins have enabled |
| `localization.fallback_language` | string | `"en"` | Used when user's preferred language has no translations |
| `localization.client_picker_enabled` | string | `"true"` | Kill-switch: hide language picker if false |
| `localization.force_offline_mode` | string | `"false"` | Emergency toggle: route through OfflineTranslationProvider |

**API key/token** for TMS authentication is stored through the shared
`SecretProvider` binding key `localization.tms_api_key`, not in governance
settings. The admin API exposes only `TmsApiKeyConfigured`; plaintext keys never
go to Blazor, generated API clients, OpenAPI examples, logs, metrics, or
ProblemDetails.

## Language Governance Model

The system separates three distinct concerns:

1. **Culture Registry** (`Explore.Domain/Common/Localization/CultureRegistry.cs`) — compile-time allowlist of all cultures the codebase knows how to handle. Never touches the DB or TMS.
2. **Enabled Languages** — ops-controlled subset from governance settings (`localization.enabled_languages`). Drives the language picker, request-localization middleware, and user preference validation.
3. **Available Translations** — runtime-discovered from the TMS (or offline bundles). A language can be enabled without translations existing yet.

**Resolution order**: `CultureRegistry.Contains(code)` → `EnabledLanguages.Contains(code)` → serve translations.

**Kill-switches**:
- `client_picker_enabled = false` → language picker renders nothing; feature is genuinely hidden.
- `force_offline_mode = true` → `RuntimeTranslationProvider` short-circuits to `OfflineTranslationProvider` regardless of `tms_provider`.

## Cache Variation

Cache key format: `Translation:{tenantId}:{languageCode}:{mode}` where `mode ∈ {"live","offline"}`.

`InvalidateLanguageAsync(languageCode)` clears **both** mode variants for the given language and tenant. This is called after bundle export and after governance changes.

## Offline Translation Bundles

Pre-exported bundles are embedded resources at:
```
Explore.Infrastructure/Localization/Bundles/{lang}.json
```

Format: flat key-value JSON:
```json
{
  "lookup.tag.FIQH.full_name": "Jurisprudence",
  "ui.button.save": "Enregistrer"
}
```

Starter bundles shipped: `en.json`, `fr.json`, `ar.json`.

Bundle schema rules are enforced by `BundleSchema` before local writes and while
loading embedded/writable bundles:

- the root must be a JSON object;
- every value must be a string;
- keys must be nonblank, contain no whitespace, have no empty dot segments, and
  start with `ui.` or `lookup.`;
- output is sorted deterministically before writing.

### Bundle Persistence & HA Constraint

The admin UI "Export from TMS" feature writes bundles to a **writable directory** on the local filesystem:

```
{ContentRoot}/App_Data/Localization/Bundles/{code}.json
```

**`OfflineTranslationProvider`** loads embedded defaults first, then merges a
valid writable bundle over those defaults key-by-key. Writable keys override
embedded keys, writable-only keys are included, and malformed writable bundles
are ignored safely so embedded defaults remain available.

**HA constraint**: `App_Data/Localization/Bundles/` is a **local filesystem path**. It is correct for:
1. **Single-instance deployments** — the export writes locally and the same instance reads it.
2. **Multi-instance deployments with a shared persistent volume** — all replicas mount the same directory.

It is **not** HA-safe behind a load balancer without shared storage. `IBundleFileWriter` (`Explore.Application/Contracts/Infrastructure/IBundleFileWriter.cs`) is the seam where a `DistributedBundleFileWriter` (S3/blob/shared-cache) can ship post-v1 without touching call sites.

The admin UI surfaces a health banner when the writable path is not available,
and gates TMS export buttons on `WritablePathHealth.Writable`. The admin API also
offers direct static bundle import/export for no-TMS operators:

- `GET /api/admin/localization/bundle?languageCode={code}` returns the merged static bundle without calling live Tolgee/Weblate.
- `POST /api/admin/localization/bundle` validates and writes a flat bundle JSON payload, then invalidates the translation resolver cache for that language.

Static bundle import/export is authorized admin-only. Import failures return safe
command errors and logs never include raw bundle content.

**Backlog ticket**: `dev/backlog/distributed-bundle-file-writer.md`

## API Endpoints

### Public (AllowAnonymous)
- `GET /api/translation/{languageCode}` — Get all translations for a language
- `GET /api/translation/languages` — List available languages

### Admin (Authorize)
- `POST /api/admin/localization/test-connection` — Test TMS connectivity
- `GET /api/admin/localization/configuration` — Get current localization config (includes governance fields)
- `PUT /api/admin/localization/governance` — Update localization governance settings (9 keys)
- `POST /api/admin/localization/export-from-tms?languageCode={code}` — Pull translations from TMS and persist bundle
- `GET /api/admin/localization/bundle?languageCode={code}` — Export merged static bundle
- `POST /api/admin/localization/bundle` — Import flat static bundle JSON
- `GET /api/admin/localization/bundle-health` — Probe writable bundle path health

## TMS Provider API Details

### Tolgee
- Auth: `X-API-Key` header
- Export: `GET /v2/projects/{projectId}/translations/{lang}?structureDelimiter=.`
- Import: `POST /v2/projects/{projectId}/keys/import-resolvable`
- Languages: `GET /v2/projects/{projectId}/languages`

Tolgee project IDs are numeric in the generated client; configure
`localization.tms_project_id` with the numeric project id from Tolgee.

### Weblate
- Auth: `Authorization: Token {apiKey}` header
- Export: `GET /api/translations/{project}/{component}/{lang}/file/`
- Import: `POST /api/translations/{project}/{component}/{lang}/file/` as multipart JSON file upload with method `translate`, fuzzy handling `process`, and conflicts `replace`
- Languages: `GET /api/projects/{project}/languages/`

## Cache Behavior

| Layer | TTL | Scope |
|-------|-----|-------|
| Config resolution | 5 min | Per tenant |
| Live translations | 30 min | Per language |
| Offline translations | 24 hours | Per language (singleton) |
| Offline bundle loading | Forever | Singleton (ConcurrentDictionary) |

## Observability

Localization metrics use the `Explore.Translation` meter and record only
boundary outcomes. Do not tag metrics with translation keys, bundle values,
API keys, bearer tokens, or raw provider payloads.

| Metric | Recorded When | Tags |
|---|---|---|
| `islamu.translation.fetch_total` | Runtime translation fetch completes. | `provider`, `language`, `result` (`hit_cache`, `hit_tms`, `hit_offline`, `error`) |
| `islamu.translation.fetch_duration_seconds` | Runtime translation fetch duration is observed. | `provider`, `language` |
| `islamu.translation.change_language_total` | UI/BFF language preference changes. | `from`, `to` |
| `islamu.tms.connection_test_total` | Admin TMS connection test completes. | `provider`, `result` |
| `islamu.tms.fallback_activated_total` | Live provider failure activates offline fallback. | `provider`, `reason` (`timeout`, `auth_error`, `not_found`, `rate_limited`, `network_error`, `other`) |
| `islamu.localization.static_bundle_operation_total` | Static bundle import/export or TMS-to-static mirror operation completes. | `operation`, `language`, `result` |

`TranslationService.T(key)` remains a synchronous in-memory lookup and is not
instrumented. Instrument fetch, fallback, connection-test, language-change, and
admin bundle boundaries instead.

## Related

- [CONFIGURATION.md](CONFIGURATION.md) — `localization.*` governance keys
- [EXTENSIBILITY.md](EXTENSIBILITY.md) — TMS provider abstraction pattern
- [DOMAIN.md](DOMAIN.md) — Tag/Category entities (tenant-scoped, translatable)
