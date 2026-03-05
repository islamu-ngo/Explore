# Blazor Localization — Implementation Plan

Last Updated: 2026-03-26

## Executive Summary

Implement client-side localization in the Blazor frontend by connecting to the existing backend translation API (`/api/translation/*`). The system must work seamlessly with InteractiveAuto rendering (both Server and WASM), support RTL languages (Arabic), cache translations efficiently, and follow the established service layer pattern. The backend localization infrastructure (TMS providers, offline bundles, CQRS handlers, API endpoints) is **fully implemented** — this plan covers the Blazor client integration only.

## Current State

### What Exists (Backend — Complete)
- `TranslationController`: `GET /api/translation/{languageCode}`, `GET /api/translation/languages`
- `LocalizationAdminController`: test-connection, configuration, export-from-tms
- TMS providers: Tolgee, Weblate, Offline, Null (runtime-routed)
- Offline JSON bundles: `en.json`, `fr.json`, `ar.json` (embedded resources)
- Governance settings: `localization.default_language`, `localization.tms_provider`, etc.
- CQRS: `GetTranslationsQuery`, `GetAvailableLanguagesQuery`, handlers
- HybridCache: 30min live, 24h offline, 5min config

### What Exists (Blazor — Nothing)
- No translation service in `Explore.Blazor.Client`
- No language picker UI
- No RTL support
- No language state management
- NSwag client does NOT include translation endpoints (swagger.json not regenerated)
- All UI strings are hardcoded English

### Established Patterns to Follow
- **Service layer**: Interface → Implementation, try-catch API calls, graceful fallbacks (see `EventService`)
- **Caching**: `LookupCacheService` pattern — `SemaphoreSlim`, `CacheEntry<T>` with TTL, `GetOrFetchAsync`
- **Cascading state**: `TenantContextProvider.razor` pattern for app-wide state
- **DI registration**: `ServiceCollectionExtensions.AddSharedApplicationServices()` for shared client services
- **Testing**: bUnit + NSubstitute via `BlazorTestContext`, `MockServiceFactory`

## Proposed Architecture

```
┌─────────────────────────────────────────────────┐
│              LanguageProvider.razor              │  ← CascadingValue (wraps App content)
│  Loads language from cookie, provides context    │
└──────────────┬──────────────────────────────────┘
               │ injects
┌──────────────▼──────────────────────────────────┐
│           ITranslationService                    │  ← Scoped service (Blazor.Client)
│  GetTranslationsAsync(lang)                      │
│  GetAvailableLanguagesAsync()                    │
│  T(key) — quick accessor                         │
│  ChangeLanguageAsync(lang) — updates + notifies  │
└──────────────┬──────────────────────────────────┘
               │ calls via
┌──────────────▼──────────────────────────────────┐
│         IEventApiClient (NSwag)                  │  ← Auto-generated from swagger.json
│  TranslationAsync(languageCode)                  │
│  TranslationLanguagesAsync()                     │
└──────────────┬──────────────────────────────────┘
               │ BFF proxy
┌──────────────▼──────────────────────────────────┐
│         TranslationController (API)              │  ← Existing backend
│  GET /api/translation/{lang}                     │
│  GET /api/translation/languages                  │
└─────────────────────────────────────────────────┘
```

### Key Design Decisions

1. **Single `ITranslationService`** — not splitting into separate cache + API services. The caching is internal to the service (like `LookupCacheService`). Components inject one service.

2. **CascadingValue for language context** — `LanguageProvider.razor` wraps the app tree, provides current language code and dir (LTR/RTL) via `LanguageContext` model. Same pattern as `TenantContextProvider.razor`.

3. **Cookie-based language persistence** — language stored in a cookie (`lang`), readable on both server and WASM. BFF endpoint `POST /bff/language` sets the cookie (matching the existing `POST /bff/theme` pattern).

4. **No IStringLocalizer** — We don't use .NET's built-in `IStringLocalizer<T>` because our translation keys follow `ui.{area}.{component}.{element}` convention and come from a REST API, not `.resx` files. A custom `T["key"]` accessor on `ITranslationService` is simpler and consistent with the TMS-driven architecture.

5. **Preload on language change** — When language changes, the service fetches ALL translations for that language in one API call (they're flat key-value), caches in memory, and all components re-render via `StateHasChanged` triggered by the cascading value change.

6. **RTL via CSS** — `dir="rtl"` attribute on `<html>` element, toggled via JS interop when language changes. MudBlazor supports RTL natively when `dir` is set.

## Implementation Phases

### Phase 1: NSwag Client Regeneration
Regenerate swagger.json to include translation endpoints, rebuild NSwag client.

**Tasks:**
- 1.1: Export fresh swagger.json from Explore.API (including TranslationController endpoints)
- 1.2: Rebuild NSwag client (`Explore.Blazor.Client`)
- 1.3: Verify generated `IEventApiClient` includes translation methods

**Effort:** S
**Skill:** `blazor-bff-patterns`

### Phase 2: Translation Service (Core)
Create the client-side translation service with caching and API integration.

**Tasks:**
- 2.1: Create `LanguageContext` model (`Models/LanguageContext.cs`)
- 2.2: Create `ITranslationService` interface (`Contracts/Services/ITranslationService.cs`)
- 2.3: Implement `TranslationService` (`Services/TranslationService.cs`) — API calls, caching, `T(key)` accessor
- 2.4: Register in `ServiceCollectionExtensions.AddSharedApplicationServices()`

**Effort:** M
**Skill:** `blazor-bff-patterns`, `clean-architecture-rules`

### Phase 3: Language State Provider
Create cascading language provider and BFF endpoint for cookie persistence.

**Tasks:**
- 3.1: Create `LanguageProvider.razor` (`Providers/LanguageProvider.razor`) — cascading value, initialization from cookie/default
- 3.2: Add BFF language endpoint in `Explore.Blazor` (`POST /bff/language`) — sets cookie, returns success
- 3.3: Wire `LanguageProvider` into `App.razor` (wrap content tree)
- 3.4: Add JS interop for setting `dir` attribute on `<html>` (`wwwroot/js/localization.js`)

**Effort:** M
**Skill:** `blazor-ui-conventions`, `blazor-bff-patterns`

### Phase 4: Language Picker Component
Create MudBlazor-based language selector in the app header.

**Tasks:**
- 4.1: Create `LanguagePicker.razor` component (`Shared/LanguagePicker.razor`) — MudMenu with flag/label, calls `ChangeLanguageAsync`
- 4.2: Integrate into `MainLayout.razor` header (next to theme toggle)
- 4.3: Style with CSS isolation (`LanguagePicker.razor.css`) using BEM methodology

**Effort:** S
**Skill:** `blazor-ui-conventions`, `blazor-css-isolation`

### Phase 5: Component Integration Helpers
Create helpers/extensions to make translation usage ergonomic in Razor components.

**Tasks:**
- 5.1: Create `TranslationExtensions` static class (`Extensions/TranslationExtensions.cs`) — `T(key, fallback?)` extension
- 5.2: Add `@using` to `_Imports.razor` for translation namespace access
- 5.3: Migrate 1 representative page to use translations (e.g., `EventList.razor` — page title, filter labels, empty state text) as proof-of-concept

**Effort:** S
**Skill:** `blazor-ui-conventions`

### Phase 6: RTL Support
Add right-to-left layout support for Arabic and other RTL languages.

**Tasks:**
- 6.1: Create `localization.js` interop functions (`setDirection`, `getDirection`)
- 6.2: Add RTL CSS overrides (`wwwroot/css/rtl.css`) — MudBlazor RTL adjustments
- 6.3: Wire direction change into `LanguageProvider` — call JS interop on language change
- 6.4: Test RTL rendering with Arabic language selection

**Effort:** M
**Skill:** `blazor-ui-conventions`, `blazor-css-isolation`

### Phase 7: Testing
Unit tests for translation service, component tests for language picker.

**Tasks:**
- 7.1: `TranslationServiceTests.cs` — caching behavior, API error fallback, language change, `T(key)` resolution
- 7.2: `LanguagePickerTests.cs` — rendering, language selection, availability
- 7.3: Add `ITranslationService` mock to `MockServiceFactory` with default translations
- 7.4: Update `BlazorTestContext` helper for language-aware testing

**Effort:** M
**Skill:** `blazor-ui-conventions`

### Phase 8: Documentation
Update docs to reflect Blazor localization.

**Tasks:**
- 8.1: Update `docs/LOCALIZATION.md` — add "Blazor Client Integration" section
- 8.2: Update `docs/BLAZOR.md` — add "Localization" section referencing docs/LOCALIZATION.md
- 8.3: Update `dev/active/blazor-localization/` task files with final status

**Effort:** S

## Risk Assessment

### High Risk
- **InteractiveAuto dual-mode** — Translation service must work identically in Server and WASM. The `IEventApiClient` handles this (same HTTP path via BFF proxy), but caching behavior differs: Server-side has per-circuit scope, WASM has per-tab scope. Mitigation: Use scoped service lifetime; cache is per-scope which works for both.

### Medium Risk
- **NSwag regeneration** — If swagger.json export doesn't include translation endpoints (controller might be excluded from OpenAPI doc generation), need to verify `[ApiExplorerSettings]` attributes. Mitigation: Check controller attributes before regenerating.
- **RTL CSS conflicts** — MudBlazor's RTL support may have edge cases with custom CSS. Mitigation: Test thoroughly with Arabic, limit RTL overrides to what's necessary.

### Low Risk
- **Bundle size** — All translations for a language loaded in one call. For large translation sets, this could be >100KB. Mitigation: Current bundle sizes are small (en/fr/ar.json); compress with gzip (default in ASP.NET Core).
- **Language flicker on load** — First render before translations load shows keys or English fallbacks. Mitigation: Preload translations during LanguageProvider initialization before rendering children.

## Success Metrics

1. All hardcoded strings on `EventList` page replaced with translation keys
2. Language picker visible in header, switching between en/fr/ar works
3. Arabic selection applies RTL layout
4. Translation cache prevents redundant API calls (verify via network tab)
5. All existing tests still pass + new translation tests pass
6. Works in both Server and WASM render modes

## Potential Risks & Unknowns

The **highest-risk area** is the NSwag client regeneration (Phase 1). If the `TranslationController` is not included in the exported swagger.json (e.g., missing `[ApiController]` attribute, or excluded from the OpenAPI document), we'd need to either fix the API-side configuration or manually add HTTP calls bypassing NSwag — which breaks the established pattern. Verify this first before proceeding.

The second risk is **LanguageProvider initialization timing** in InteractiveAuto mode. During server prerendering, the provider reads the cookie from `HttpContext`; during WASM hydration, `HttpContext` is null so we need a fallback (read from JS interop or use the prerendered value). The `TenantContextProvider` already navigates this — study its exact pattern before implementing.
