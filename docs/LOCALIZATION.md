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
| `NullTranslationProvider` | `Explore.Infrastructure/Localization/` | Singleton |
| `TranslationResolver` | `Explore.Infrastructure/Localization/` | Scoped |
| `TranslationConfigResolver` | `Explore.Infrastructure/Localization/` | Scoped |

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

**API key/token** for TMS authentication is stored via `SecretProvider`, not in governance settings.

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

## API Endpoints

### Public (AllowAnonymous)
- `GET /api/translation/{languageCode}` — Get all translations for a language
- `GET /api/translation/languages` — List available languages

### Admin (Authorize)
- `POST /api/admin/localization/test-connection` — Test TMS connectivity
- `GET /api/admin/localization/configuration` — Get current localization config
- `POST /api/admin/localization/export-from-tms?languageCode={code}` — Pull translations from TMS

## TMS Provider API Details

### Tolgee
- Auth: `X-API-Key` header
- Export: `GET /v2/projects/{projectId}/translations/{lang}`
- Import: `POST /v2/projects/{projectId}/keys/import-resolvable`
- Languages: `GET /v2/projects/{projectId}/languages`

### Weblate
- Auth: `Authorization: Token {apiKey}` header
- Export: `GET /api/translations/{project}/{component}/{lang}/file/`
- Import: `POST /api/translations/{project}/{component}/{lang}/units/`
- Languages: `GET /api/projects/{project}/languages/`

## Cache Behavior

| Layer | TTL | Scope |
|-------|-----|-------|
| Config resolution | 5 min | Per tenant |
| Live translations | 30 min | Per language |
| Offline translations | 24 hours | Per language (singleton) |
| Offline bundle loading | Forever | Singleton (ConcurrentDictionary) |

## Related

- [CONFIGURATION.md](CONFIGURATION.md) — `localization.*` governance keys
- [EXTENSIBILITY.md](EXTENSIBILITY.md) — TMS provider abstraction pattern
- [DOMAIN.md](DOMAIN.md) — Tag/Category entities (tenant-scoped, translatable)
