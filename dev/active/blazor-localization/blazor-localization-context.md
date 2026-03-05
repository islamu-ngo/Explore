# Blazor Localization — Context

Last Updated: 2026-03-26

## SESSION PROGRESS (2026-03-26)

### ✅ COMPLETED
- Codebase analysis (Blazor.Client service patterns, layout, providers, testing)
- Plan created (`blazor-localization-plan.md`)
- Context created (this file)
- Tasks created (`blazor-localization-tasks.md`)

### 🟡 IN PROGRESS
- Nothing yet — awaiting approval to implement

### ⚠️ BLOCKERS
- None identified

## Quick Resume

1. Read this file for current state
2. Read `blazor-localization-tasks.md` for checklist
3. Read `blazor-localization-plan.md` for detailed design
4. Start with Phase 1 (NSwag regeneration) — verify translation endpoints appear in swagger.json first

## Key Files

### Backend (Already Implemented — Do Not Modify)

**`Explore.API/Controllers/TranslationController.cs`**
- Public endpoints: `GET /api/translation/{languageCode}`, `GET /api/translation/languages`
- AllowAnonymous — no auth required for reading translations
- Returns `Dictionary<string, string>` for translations, `List<LanguageInfo>` for languages

**`Explore.API/Controllers/LocalizationAdminController.cs`**
- Admin endpoints: test-connection, configuration, export-from-tms
- Authorize attribute — admin only

**`Explore.Infrastructure/Localization/RuntimeTranslationProvider.cs`**
- Core provider routing logic — resolves TMS provider from governance config
- Falls back to OfflineTranslationProvider on error

**`Explore.Infrastructure/Localization/Bundles/{en,fr,ar}.json`**
- Offline translation bundles (embedded resources)
- Flat key-value format: `{"lookup.tag.FIQH.full_name": "Jurisprudence"}`

**`Explore.Application/Features/Localization/Queries/GetTranslationsQuery.cs`**
- MediatR query: `GetTranslationsQuery { LanguageCode }`
- Handler calls `ITranslationManagementProvider.ExportTranslationsAsync()`

### Blazor Client (Files to Create/Modify)

**`Explore.Blazor.Client/Models/LanguageContext.cs`** (TO CREATE)
- Record/class with: `LanguageCode` (string), `IsRtl` (bool), `LanguageName` (string)
- Used as CascadingValue type

**`Explore.Blazor.Client/Contracts/Services/ITranslationService.cs`** (TO CREATE)
- Interface: `GetTranslationsAsync(lang)`, `GetAvailableLanguagesAsync()`, `T(key, fallback?)`, `ChangeLanguageAsync(lang)`, `CurrentLanguage`
- Event: `OnLanguageChanged`

**`Explore.Blazor.Client/Services/TranslationService.cs`** (TO CREATE)
- Implementation with in-memory cache (SemaphoreSlim, CacheEntry pattern from LookupCacheService)
- 30-minute TTL for translations
- Calls NSwag client for API access
- Graceful fallback: returns key as-is if translation missing

**`Explore.Blazor.Client/Providers/LanguageProvider.razor`** (TO CREATE)
- Cascading value provider (like TenantContextProvider.razor)
- Reads initial language from cookie or default ("en")
- Provides `LanguageContext` to entire component tree
- Listens to `ITranslationService.OnLanguageChanged` to update

**`Explore.Blazor.Client/Shared/LanguagePicker.razor`** (TO CREATE)
- MudMenu dropdown with available languages
- Shows current language flag/label
- Calls `ITranslationService.ChangeLanguageAsync()` on selection

**`Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs`** (MODIFY)
- Add `services.AddScoped<ITranslationService, TranslationService>()` registration

**`Explore.Blazor.Client/_Imports.razor`** (MODIFY)
- Add `@using Explore.Blazor.Client.Models` if not present (for LanguageContext)

**`Explore.Blazor.Client/Layout/MainLayout.razor`** (MODIFY)
- Add `<LanguagePicker />` component in header bar

**`Explore.Blazor/Extensions/BffEndpointExtensions.cs`** (MODIFY)
- Add `POST /bff/language` endpoint (set cookie, return OK)

### Blazor BFF (Server-Side)

**`Explore.Blazor/Program.cs`** (REFERENCE)
- Service registration order: MudServices → ApplicationServices → ServerOnlyServices → HttpClients
- Authentication: BFF pattern with YARP proxy

**`Explore.Blazor/Extensions/ServiceRegistrationExtensions.cs`** (REFERENCE)
- Calls `AddSharedApplicationServices()` then adds server-only services

### NSwag Configuration

**`Explore.Blazor.Client/nswag.json`** (REFERENCE)
- Input: `../Explore.API/swagger.json`
- Output: `Clients/EventApiClient.g.cs`
- Build target: `GenerateApiClient` before `CoreCompile`

**`Explore.API/swagger.json`** (TO REGENERATE)
- Must include TranslationController endpoints after regeneration

### Testing

**`Explore.Blazor.Client.Tests/Common/MockServiceFactory.cs`** (MODIFY)
- Add `CreateTranslationService()` factory method
- Add to `RegisterAllCoreMocks()`

**`Explore.Blazor.Client.Tests/Common/BlazorTestContext.cs`** (REFERENCE)
- bUnit context with MudBlazor, auth, JSInterop support
- Pattern for adding new service mocks

**`Explore.Blazor.Client.Tests/Services/TranslationServiceTests.cs`** (TO CREATE)
- Cache behavior, API error handling, `T(key)` resolution, language change

## Important Decisions

### Why Not IStringLocalizer?
Our translations come from a REST API (TMS-backed), not `.resx` files. Keys follow `ui.{area}.{component}.{element}` convention. IStringLocalizer would add complexity without benefit. A simple `T(key)` method on our service is more aligned with the architecture.

### Why Cookie for Language Persistence?
Cookies work across both Server and WASM render modes. Server-side can read cookies from HttpContext during prerendering; WASM can read via JS interop. Same pattern as theme persistence (`/bff/theme`).

### Why CascadingValue (Not Static/Singleton)?
Language is user-scoped state. CascadingValue ensures all child components re-render when language changes. Server-side: per-circuit scope. WASM: per-tab scope. Both work correctly with CascadingValue.

### Translation Cache Strategy
- 30-minute TTL (matching backend live translation cache)
- Full language preload (single API call returns all keys for a language)
- SemaphoreSlim to prevent thundering herd on cache miss
- Fallback: return key itself if translation not found (visible for debugging, safe for production)

### RTL Approach
- Set `dir="rtl"` on `<html>` element via JS interop
- MudBlazor respects `dir` attribute natively for layout mirroring
- Minimal custom RTL CSS needed (mostly for custom components)
- RTL languages: `ar` (Arabic), `he` (Hebrew), `fa` (Persian), `ur` (Urdu)

## Dependencies

- **NSwag regeneration** depends on: Explore.API running/exporting swagger.json with translation endpoints
- **TranslationService** depends on: NSwag client including translation methods
- **LanguageProvider** depends on: TranslationService, BFF language endpoint
- **LanguagePicker** depends on: LanguageProvider, TranslationService
- **RTL support** depends on: LanguageProvider (direction change trigger)
- **Testing** depends on: TranslationService interface (for mocking)

## Technical Constraints

1. **InteractiveAuto** — Code must work in both Server (SignalR circuit) and WASM modes. No `HttpContext` access in components (use BFF endpoints or JS interop for cookies).
2. **BFF proxy** — All API calls go through YARP proxy (Blazor → BFF → API). Translation endpoints will be proxied like all others.
3. **NSwag** — Must regenerate swagger.json before NSwag client has translation methods. Cannot manually add methods to generated client.
4. **MudBlazor v9** — Use MudBlazor components for language picker (MudMenu, MudMenuItem). Follow existing MudBlazor patterns in the codebase.
5. **BEM CSS** — Any custom CSS must follow BEM methodology with CSS isolation (`.razor.css`).
