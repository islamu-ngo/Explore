# Blazor Localization — Task Checklist

Last Updated: 2026-03-26

## Status Summary

| Phase | Description | Status | Tasks |
|-------|------------|--------|-------|
| 1 | NSwag Client Regeneration | ⏳ NOT STARTED | 0/3 |
| 2 | Translation Service (Core) | ⏳ NOT STARTED | 0/4 |
| 3 | Language State Provider | ⏳ NOT STARTED | 0/4 |
| 4 | Language Picker Component | ⏳ NOT STARTED | 0/3 |
| 5 | Component Integration Helpers | ⏳ NOT STARTED | 0/3 |
| 6 | RTL Support | ⏳ NOT STARTED | 0/4 |
| 7 | Testing | ⏳ NOT STARTED | 0/4 |
| 8 | Documentation | ⏳ NOT STARTED | 0/3 |
| **Total** | | | **0/28** |

---

## Phase 1: NSwag Client Regeneration ⏳ NOT STARTED

- [ ] **1.1** Export fresh swagger.json from Explore.API
  - Run API project, export OpenAPI spec
  - Verify `TranslationController` endpoints appear in spec
  - File: `Explore.API/swagger.json`
  - Acceptance: swagger.json contains `/api/translation/{languageCode}` and `/api/translation/languages`

- [ ] **1.2** Rebuild NSwag client
  - Build `Explore.Blazor.Client` to trigger NSwag generation
  - File: `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
  - Acceptance: Generated client compiles without errors

- [ ] **1.3** Verify generated client includes translation methods
  - Check `IEventApiClient` for `TranslationAsync()` and `TranslationLanguagesAsync()` (or similar)
  - Acceptance: Translation methods available on client interface

---

## Phase 2: Translation Service (Core) ⏳ NOT STARTED

- [ ] **2.1** Create `LanguageContext` model
  - File: `Explore.Blazor.Client/Models/LanguageContext.cs`
  - Properties: `LanguageCode` (string), `IsRtl` (bool), `LanguageName` (string)
  - ABOUTME header required
  - Acceptance: Clean compile, model usable as CascadingValue type

- [ ] **2.2** Create `ITranslationService` interface
  - File: `Explore.Blazor.Client/Contracts/Services/ITranslationService.cs`
  - Methods: `GetTranslationsAsync(lang)`, `GetAvailableLanguagesAsync()`, `T(key, fallback?)`, `ChangeLanguageAsync(lang)`, `CurrentLanguage` property
  - Event: `OnLanguageChanged` (Action<string>)
  - ABOUTME header required
  - Acceptance: Interface compiles, follows project patterns

- [ ] **2.3** Implement `TranslationService`
  - File: `Explore.Blazor.Client/Services/TranslationService.cs`
  - In-memory cache with 30-min TTL (SemaphoreSlim + CacheEntry pattern from LookupCacheService)
  - API calls via NSwag `IEventApiClient`
  - `T(key)` returns translation or key itself as fallback
  - `ChangeLanguageAsync(lang)` clears cache, fetches new translations, fires event
  - Try-catch with ILogger for all API calls
  - ABOUTME header required
  - Acceptance: Service compiles, follows EventService/LookupCacheService patterns

- [ ] **2.4** Register TranslationService in DI
  - File: `Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs`
  - Add: `services.AddScoped<ITranslationService, TranslationService>()`
  - Acceptance: Service resolves correctly in both Server and WASM

---

## Phase 3: Language State Provider ⏳ NOT STARTED

- [ ] **3.1** Create `LanguageProvider.razor`
  - File: `Explore.Blazor.Client/Providers/LanguageProvider.razor`
  - CascadingValue pattern (like TenantContextProvider.razor)
  - Initialization: read `lang` cookie → fallback to "en"
  - Provides `LanguageContext` to child components
  - Subscribes to `ITranslationService.OnLanguageChanged`
  - Updates `LanguageContext` and triggers re-render on language change
  - ABOUTME header required
  - Acceptance: CascadingValue available in child components, language changes propagate

- [ ] **3.2** Add BFF language endpoint
  - File: `Explore.Blazor/Extensions/BffEndpointExtensions.cs`
  - Endpoint: `POST /bff/language` with `{ "language": "fr" }` body
  - Sets `lang` cookie (HttpOnly=false so JS can read, SameSite=Lax, 1-year expiry)
  - Returns 200 OK
  - Follow existing `/bff/theme` pattern
  - Acceptance: Cookie set correctly on POST, readable client-side

- [ ] **3.3** Wire LanguageProvider into App.razor
  - File: `Explore.Blazor.Client/Routes.razor` or `Explore.Blazor/Components/App.razor`
  - Wrap content with `<LanguageProvider>` (after TenantContextProvider, before pages)
  - Acceptance: LanguageContext cascading value available on all pages

- [ ] **3.4** Create JS interop for RTL direction
  - File: `Explore.Blazor.Client/wwwroot/js/localization.js` (or add to existing JS)
  - Functions: `setDocumentDirection(dir)`, `getDocumentDirection()`
  - Sets `dir` and `lang` attributes on `<html>` element
  - Acceptance: JS functions callable from Blazor via IJSRuntime

---

## Phase 4: Language Picker Component ⏳ NOT STARTED

- [ ] **4.1** Create `LanguagePicker.razor` component
  - File: `Explore.Blazor.Client/Shared/LanguagePicker.razor`
  - MudMenu dropdown with language list from `ITranslationService.GetAvailableLanguagesAsync()`
  - Shows current language code/name
  - Calls `ITranslationService.ChangeLanguageAsync()` on selection
  - Also calls `POST /bff/language` to persist in cookie
  - ABOUTME header required
  - Acceptance: Renders language list, clicking changes language

- [ ] **4.2** Integrate into MainLayout header
  - File: `Explore.Blazor.Client/Layout/MainLayout.razor`
  - Add `<LanguagePicker />` in MudAppBar (next to theme toggle)
  - Acceptance: Picker visible in header on all pages

- [ ] **4.3** CSS isolation for LanguagePicker
  - File: `Explore.Blazor.Client/Shared/LanguagePicker.razor.css`
  - BEM methodology: `.language-picker`, `.language-picker__button`, `.language-picker__item`
  - Acceptance: Styled consistently with existing header elements

---

## Phase 5: Component Integration Helpers ⏳ NOT STARTED

- [ ] **5.1** Create TranslationExtensions
  - File: `Explore.Blazor.Client/Extensions/TranslationExtensions.cs`
  - Static helper methods for common patterns
  - ABOUTME header required
  - Acceptance: Extension methods compile and are accessible from Razor components

- [ ] **5.2** Add using directives to _Imports.razor
  - File: `Explore.Blazor.Client/_Imports.razor`
  - Add required namespaces for LanguageContext, ITranslationService
  - Acceptance: Translation types available in all Razor components without per-file @using

- [ ] **5.3** Proof-of-concept: Migrate EventList page
  - File: `Explore.Blazor.Client/Pages/Events/EventList.razor`
  - Replace hardcoded strings with `T["ui.event.list.title"]` etc.
  - At minimum: page title, filter labels, empty state message, button text
  - Acceptance: Page renders correctly with translations, falls back to keys gracefully

---

## Phase 6: RTL Support ⏳ NOT STARTED

- [ ] **6.1** Implement JS interop functions
  - File: `Explore.Blazor.Client/wwwroot/js/localization.js`
  - `window.localization = { setDirection(dir, lang), getDirection() }`
  - Acceptance: Functions work in both Server (pre-render) and WASM modes

- [ ] **6.2** RTL CSS overrides
  - File: `Explore.Blazor.Client/wwwroot/css/rtl.css`
  - MudBlazor handles most RTL via `dir="rtl"`, add overrides only for custom components
  - Acceptance: Arabic renders RTL correctly, LTR languages unaffected

- [ ] **6.3** Wire direction into LanguageProvider
  - File: `Explore.Blazor.Client/Providers/LanguageProvider.razor`
  - On language change: call `setDirection()` JS interop
  - RTL languages list: ar, he, fa, ur
  - Acceptance: Switching to Arabic sets `dir="rtl"`, switching back sets `dir="ltr"`

- [ ] **6.4** Test RTL layout
  - Manual test: Switch to Arabic, verify layout mirrors correctly
  - Check: NavMenu, MainLayout, EventList, LanguagePicker all display RTL
  - Acceptance: No layout breaks in RTL mode

---

## Phase 7: Testing ⏳ NOT STARTED

- [ ] **7.1** TranslationService unit tests
  - File: `Explore.Blazor.Client.Tests/Services/TranslationServiceTests.cs`
  - Tests: cache hit, cache miss (API call), cache expiry, API error fallback, T(key) found/not-found, language change clears cache, concurrent access
  - Mock: `IEventApiClient` translation methods
  - Acceptance: All tests pass, cover critical paths

- [ ] **7.2** LanguagePicker component tests
  - File: `Explore.Blazor.Client.Tests/Components/LanguagePickerTests.cs` (or Shared/)
  - Tests: renders available languages, shows current language, selection triggers change
  - Uses `BlazorTestContext` + `MockServiceFactory`
  - Acceptance: All tests pass

- [ ] **7.3** Add ITranslationService to MockServiceFactory
  - File: `Explore.Blazor.Client.Tests/Common/MockServiceFactory.cs`
  - Add `CreateTranslationService()` method with default translations
  - Add to `RegisterAllCoreMocks()`
  - Acceptance: Existing tests still pass with new mock registered

- [ ] **7.4** Update BlazorTestContext if needed
  - File: `Explore.Blazor.Client.Tests/Common/BlazorTestContext.cs`
  - Add `AddTranslationService()` helper method if useful
  - Acceptance: Test context supports translation-dependent component testing

---

## Phase 8: Documentation ⏳ NOT STARTED

- [ ] **8.1** Update docs/LOCALIZATION.md
  - Add "Blazor Client Integration" section
  - Document: translation service, language provider, language picker, RTL, testing
  - Acceptance: Doc complete and accurate

- [ ] **8.2** Update docs/BLAZOR.md
  - Add "Localization" bullet/section referencing LOCALIZATION.md
  - Acceptance: Doc updated

- [ ] **8.3** Update task files
  - Mark all tasks complete
  - Update context.md with final session progress
  - Acceptance: Files reflect completed state
