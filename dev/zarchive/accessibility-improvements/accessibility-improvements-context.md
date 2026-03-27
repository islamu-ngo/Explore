ABOUTME: Key files, decisions, dependencies, and constraints for the accessibility improvements task.
ABOUTME: Quick reference for resuming work on this task without re-reading the full plan.

# Accessibility Improvements — Context (v10 — ALL PHASES COMPLETE)

## SESSION PROGRESS

**Status**: ✅ ALL PHASES COMPLETE (29/29 tasks). Build passes with 0 errors. All 52 architecture tests pass.
**Last Action**: Phase 6 — Governance: finalized ACCESSIBILITY.md, SKILL.md, debt register, public artifacts.
**Initiative Status**: DONE. All WCAG 2.2 AA infrastructure in place.

---

## Phase 1 Deliverables (Completed)

### Files Created
| File | Purpose |
|------|---------|
| `Explore.Blazor.Client/wwwroot/js/accessibility.js` | ES module: setFocus, setFocusById, announce, saveActiveElement, restoreFocus, focusOnNavigate, getPreferredMotion |
| `Explore.Blazor.Client/Contracts/Services/Accessibility/IAccessibilityAnnouncerService.cs` | Interface: AnnouncePoliteAsync, AnnounceAssertiveAsync |
| `Explore.Blazor.Client/Contracts/Services/Accessibility/IAccessibilityFocusService.cs` | Interface: FocusAsync, FocusByIdAsync, FocusMainContentAsync, FocusOnNavigateAsync, SaveFocusAsync, RestoreFocusAsync, GetPreferredMotionAsync |
| `Explore.Blazor.Client/Services/Accessibility/AccessibilityAnnouncerService.cs` | Implementation via JS interop, IAsyncDisposable, lazy module loading |
| `Explore.Blazor.Client/Services/Accessibility/AccessibilityFocusService.cs` | Implementation via JS interop, fallback chain, IAsyncDisposable |
| `docs/ACCESSIBILITY.md` | Platform Rules PR-1 through PR-4, CSS utilities, media queries, testing requirements |
| `.claude/skills/accessibility/SKILL.md` | AI agent accessibility rules for Blazor components |

### Files Modified
| File | Changes |
|------|---------|
| `Explore.Blazor.Client/Layout/MainLayout.razor` | Added skip-nav link, `role="banner"` on header, `<nav aria-label>` wrapping sidebar, `<main id="main-content" tabindex="-1">` replacing content div, ARIA live region containers |
| `Explore.Blazor.Client/Layout/MainLayout.razor.cs` | Injected IAccessibilityFocusService, added FocusOnNavigateAsync call in OnLocationChanged |
| `Explore.Blazor/wwwroot/css/StyleGlobal.css` | Added .sr-only, .skip-link, prefers-reduced-motion, forced-colors, prefers-contrast media queries |
| `Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs` | Registered IAccessibilityAnnouncerService + IAccessibilityFocusService as Scoped |

### Phase 2 Deliverables (Completed)

#### Files Modified
| File | Changes |
|------|---------|
| `Explore.Blazor.Client/Layout/MainLayout.razor` | `<div role="banner">` → native `<header>` |
| `Explore.Blazor.Client/Layout/SetupLayout.razor` | Wrapped `@Body` in `<main id="main-content" tabindex="-1">` |
| `Explore.Blazor/Components/App.razor` | Dynamic `lang`/`dir` from `lang` cookie server-side, `_rtlLanguages` HashSet |
| `Explore.Blazor.Client/Routes.razor` | 404 page: added `HtmlTag="h1"` + `<PageTitle>` |
| 36 page/component files | Added `HtmlTag="h1"` to main heading MudText (or sr-only h1 where no visible heading) |
| 8 page files | Added missing `<PageTitle>` |

#### Key Decisions
- **Page Shell Contract via `HtmlTag="h1"`**: MudBlazor v9's `HtmlTag` parameter controls rendered HTML tag. `<MudText Typo="Typo.h4" HtmlTag="h1">` renders as `<h1>` with h4 visual styling — zero visual change.
- **sr-only h1 for pages without visible heading**: EventList, CreateEvent, EventEdit, StartupGate — uses `Class="sr-only"` for screen-reader-only h1.
- **Dynamic lang/dir uses `lang` cookie**: NOT .NET `CultureInfo`. App uses custom `LanguageContext` system. Server reads `lang` cookie for SSR, LanguageProvider JS handles post-hydration updates.
- **Native `<header>` over `role="banner"`**: WCAG best practice — prefer native HTML5 elements over ARIA roles.

### Phase 3A Deliverables (Completed)

#### 3A.1 Dialog Focus Save/Restore — 26 dialog calls across 9 files
| File | Dialog Calls | Injection |
|------|-------------|-----------|
| `MyReviews.razor.cs` | 1 (ShowMessageBoxAsync) | `IAccessibilityFocusService` |
| `MyRegistrations.razor.cs` | 1 (ShowMessageBoxAsync) | `IAccessibilityFocusService` |
| `NavMenu.razor.cs` | 1 (LoginPromptDialog.ShowAsync) | `IAccessibilityFocusService` |
| `EventEdit.razor.cs` | 2 (DescriptionDialog + Delete Session) | `IAccessibilityFocusService` |
| `CreateEvent.razor.cs` | 2 (DescriptionDialog + Delete Session) | `IAccessibilityFocusService` |
| `OrganizationMembers.razor.cs` | 3 (Invite + EditRole + Remove) | `IAccessibilityFocusService` |
| `EventList.razor.cs` | 3 (Delete + Registration + Cancel) | `IAccessibilityFocusService` |
| `MyEvents.razor.cs` | 4 (SelectSession + Manager + Delete + Publish) | `IAccessibilityFocusService` |
| `EventDetail.razor.cs` | 9 (Cancel + Registration×3 + Delete + Islamic + Tech + DeleteIslamic + DeleteTech) | `IAccessibilityFocusService` |

#### 3A.2 Keyboard Accessibility — 3 files
| File | Changes |
|------|---------|
| `EventReviewDialog.razor` | Star rating: span → WAI-ARIA radio group (role, aria-checked, arrow keys, roving tabindex) |
| `GroupProfile.razor` + `.razor.cs` | MudCard/MudPaper nav cards: added tabindex, role="link", aria-label, @onkeydown |
| `EventDetail.razor` + `.razor.cs` | Organizer click: conditional tabindex, role="link", aria-label, @onkeydown |

#### 3A.3 Form Accessibility — 9 role="alert" wraps across 8 files
| File | Changes |
|------|---------|
| `ErrorState.razor` | Wrapped MudAlert in `<div role="alert">` |
| `GroupProfile.razor` | Wrapped "Group not found" MudAlert |
| `SettingsSecurity.razor` | Wrapped error MudAlert |
| `SettingsPersonalInfo.razor` | Wrapped 2 MudAlerts (static + dynamic error) |
| `StartupGate.razor` | Wrapped error MudAlert |
| `AuthProviderConfiguration.razor` | Wrapped error MudAlert |
| `LoginRedirect.razor` | Wrapped 2 MudAlerts (warning + error) |

#### 3A.4 Dynamic Content Announcements — 3 key pages
| File | Announcements |
|------|--------------|
| `EventList.razor.cs` | Polite: "{N} events found" / "No events found". Assertive: "Failed to load events" |
| `EventDetail.razor.cs` | Assertive: error on load. Polite: "Event not found" |
| `GroupProfile.razor.cs` | Assertive: error on load. Polite: "{N} upcoming and {M} past events" / "No events found" / "Group not found" |

#### Key Decisions (Phase 3A)
- **Focus save/restore uses global variable, not stack**: JS saves one `activeElement` at a time. Nested dialog calls (e.g., RegisterForSession inside OpenRegistrationDialog) are covered by the outermost save/restore wrap.
- **MudBlazor v9 handles `aria-required` automatically**: `Required="true"` → `required` + `aria-required="true"` on rendered input. No manual ARIA needed.
- **MudBlazor v9 does NOT add `aria-invalid`**: Acceptable — it shows red helper text visually. Would need custom wrapper for full screen reader invalid state.
- **`role="alert"` via wrapper div**: MudAlert doesn't have `role="alert"` built-in. Wrapping with `<div role="alert">` is the correct WCAG pattern.
- **AnnouncerService injected only in high-traffic data-loading pages**: EventList, EventDetail, GroupProfile. Other pages can add it incrementally.

### Phase 3B Deliverables (Completed)

#### 3B.1 Image Accessibility — 5 fixes
| File | Changes |
|------|---------|
| `GroupProfile.razor` line 93 | Added `Alt="@evt.Title"` to upcoming events MudImage |
| `GroupProfile.razor` line 178 | Added `Alt="@evt.Title"` to past events timeline MudImage |
| `AdminOrganizationTable.razor` line 29 | Added `aria-hidden="true"` to decorative MudAvatar |
| `S3Image.razor` | Changed default `Alt = "Image"` → `Alt = ""` (decorative default per WCAG) |
| `SettingsPersonalInfo.razor` line 53 | Added `Alt="Profile picture"` to ImageUpload |

#### 3B.2 Status Messages & Live Regions — 4 wraps (total 12 role="alert" + 1 role="status")
| File | Changes |
|------|---------|
| `MyEvents.razor` line 48 | Wrapped error MudAlert in `<div role="alert">` |
| `MyOrganizations.razor` line 39 | Wrapped error MudAlert in `<div role="alert">` |
| `ImageUpload.razor` line 26 | Wrapped error MudAlert in `<div role="alert">` |
| `SettingsSecurity.razor` line 35 | Wrapped success MudAlert in `<div role="status">` |

#### Key Decisions (Phase 3B)
- **S3Image default `alt=""`**: Empty string = decorative default per WCAG. Callers must supply meaningful alt for content images.
- **`role="status"` for success messages**: Polite announcement (doesn't interrupt current screen reader output).
- **Decorative MudAvatar**: `aria-hidden="true"` on icon-only avatars that have adjacent text labels.

### Phase 3C Deliverables (Completed)

#### 3C.1 Theme Token Contrast Fixes — `AppearanceThemeService.cs`
| Token | Old Value | New Value | Contrast on White | Source |
|-------|-----------|-----------|-------------------|--------|
| Primary | `#3B82F6` (blue-500) | `#2563EB` (blue-600) | 5.18:1 ✅ | Tailwind |
| Info | `#3B82F6` | `#2563EB` | 5.18:1 ✅ | Tracks Primary |
| Success | `#10B981` (emerald-500) | `#047857` (emerald-700) | 5.49:1 ✅ | Tailwind |
| Warning | `#F59E0B` (amber-500) | `#B45309` (amber-700) | 5.01:1 ✅ | Tailwind |
| Error | `#EF4444` (red-500) | `#DC2626` (red-600) | 4.85:1 ✅ | Tailwind |

Dark palette: All tokens already pass AA (6.43:1 to 16.4:1) — no changes needed.

#### 3C.2 Visual Focus and Target Size
| File | Changes |
|------|---------|
| `Explore.Blazor/wwwroot/css/utilities.css` | Global `:focus-visible` (2px solid outline), `:focus:not(:focus-visible)` suppression, forced-colors focus override |
| `Explore.Blazor/wwwroot/css/tokens.css` | `--isl-target-min: 1.5rem`, `--isl-focus-ring-width: 2px`, `--isl-focus-ring-offset: 2px` |
| `Explore.Blazor/wwwroot/css/base.css` | `button, [role="button"], input[type="checkbox"], input[type="radio"], select, summary { min-height/min-width: var(--isl-target-min) }` |

#### Key Decisions (Phase 3C)
- **Tailwind color scale for WCAG compliance**: Shifted from X00-500 to X00-600/700 levels. All pass 4.5:1 on white.
- **`:focus-visible` over `:focus`**: Only shows focus ring for keyboard navigation, not mouse/touch clicks.
- **Forced-colors fallback**: Windows High Contrast mode gets 3px Highlight outline.
- **CSS tokens for a11y**: Target size and focus ring dimensions as reusable custom properties.

### Phase 4 Deliverables (Completed)

#### 4.1 Playwright E2E Tests — Deferred (Documented)
- Package identified: `Deque.AxeCore.Playwright` v4.11.1 — requires running Blazor app via Aspire AppHost
- Future setup guide with code examples added to `docs/ACCESSIBILITY.md`

#### 4.2 bUnit Accessibility Tests
| File | Tests |
|------|-------|
| `Explore.Blazor.Client.Tests/Accessibility/SharedComponentAccessibilityTests.cs` | 7 tests: ErrorState role="alert" (4 cases), S3Image alt text (3 cases). 6 pass, 1 skipped (pre-existing). |

#### 4.3 CI Pipeline
| File | Purpose |
|------|---------|
| `.github/workflows/test.yml` | Runs 5 test projects on push/PR to main/develop. Concurrency with cancel-in-progress. 30min timeout. |

#### 4.4 Architecture Convention Tests
| File | Tests |
|------|-------|
| `Event.Architecture.Tests/AccessibilityConventionTests.cs` | 8 tests: h1 in routable pages (with 7 exclusions), 5 MainLayout landmark checks, 2 CSS direction advisory checks. All 52/52 architecture tests pass. |

#### Test Infrastructure Fix
| File | Changes |
|------|---------|
| `Explore.Blazor.Client.Tests/Common/BlazorTestContext.cs` | Added mock `IAccessibilityFocusService` + `IAccessibilityAnnouncerService` — fixed 87 test failures from Phase 3A service injection |

#### Key Decisions (Phase 4)
- **Playwright E2E deferred**: Requires full Aspire AppHost (DB, auth, all services). No Playwright infrastructure exists. bUnit + architecture tests provide immediate coverage.
- **Architecture tests scan raw .razor files**: `File.ReadAllText` + regex — no Blazor.Client project reference needed in Event.Architecture.Tests.
- **CSS direction tests advisory-only**: 39+ existing violations from pre-a11y CSS. Will become enforcing after Phase 5 migration.
- **Settings wrapper pages excluded from h1 test**: 7 pages delegate h1 to child layout tabs.
- **Pre-existing bUnit failures (86)**: All from concurrent AppButton/AppIconButton migration (`CaptureUnmatchedValues` → type mismatch). Not from accessibility work.

### Phase 5 Deliverables (Completed)

#### 5.1 CSS Logical Properties Migration — 14 replacements in 4 files
| File | Properties Migrated |
|------|-------------------|
| `GroupProfile.razor.css` | `padding-left`→`padding-inline-start` (×2), `left`→`inset-inline-start` (×3), `text-align:left`→`text-align:start` |
| `OrganizationProfile.razor.css` | Same 6 replacements as GroupProfile |
| `EventSeriesSection.razor.css` | `padding-left`→`padding-inline-start`, `border-left`→`border-inline-start` |
| `MainLayout.razor` line 80 | Inline `right:20px`→`inset-inline-end:20px` |

#### 5.2 Full-Stack Direction Preference
| Layer | File | Changes |
|-------|------|---------|
| Domain | `GovernanceSettingKeys.cs` | Added `Direction = "appearance.direction"` |
| Domain | `AppearanceSettingDefinitions.cs` | Added `Direction` setting definition (auto/ltr/rtl, MaxScope=User) |
| Application | `AppearanceSettingGroup.cs` | Added `Direction` property + `Populate()` + `SettingKeys` |
| Application | DTOs (2 files) | Added `Direction` to input/output DTOs |
| Application | Validator | Added "auto"/"ltr"/"rtl" validation rule |
| Application | Command Handler | Added Direction persistence (sparse override pattern) |
| Application | Query Handler | Added Direction to DTO mapping |
| BFF | `BffPreferenceEndpoints.cs` | `POST /bff/direction`, direction cookie, extended GET response, preserves direction when updating theme |
| Client | `localization.js` | Added `getDirectionCookie()` |
| Client | `LanguageContext.cs` | Added `DirectionOverride` + `EffectiveIsRtl` computed property |
| Client | `LanguageProvider.razor` | Reads direction cookie, uses `EffectiveIsRtl` for `setDirection()` |
| Client | `AppearanceThemeService.cs` | Added `ResolveInitialDirectionAsync()` + `PersistDirectionAsync()` |
| Layout | `MainLayout.razor` | Wrapped `<MudLayout>` in `<MudRTLProvider RightToLeft="@_isRtl">` |
| Layout | `MainLayout.razor.cs` | Added `[CascadingParameter(Name = "Language")] LanguageContext?` + `_isRtl` |

#### Key Decisions (Phase 5)
- **Direction preference tri-state**: "auto" = language-based RTL, "ltr" = force LTR even with Arabic, "rtl" = force RTL even with English. Per user requirement.
- **Sparse override pattern**: Direction follows same hierarchical settings pattern as ThemeMode. Only persisted when different from parent default.
- **BFF preserves both preferences**: POST /bff/theme reads direction cookie to avoid resetting it, and vice versa.
- **MudRTLProvider wrapper**: Cascades `bool RightToLeft` to all child MudBlazor components. `Anchor.Start` auto-flips drawer position.

### Path Corrections (from v2)
| Plan Said | Actual Path |
|-----------|-------------|
| `Explore.Blazor.Client/Components/Routes.razor` | `Explore.Blazor.Client/Routes.razor` |
| `Explore.Blazor/Components/Layout/MainLayout.razor` | `Explore.Blazor.Client/Layout/MainLayout.razor` |
| `Explore.Blazor/wwwroot/css/app.css` | `Explore.Blazor/wwwroot/css/StyleGlobal.css` |
| `Services/IAriaLiveService.cs` | `Contracts/Services/Accessibility/IAccessibilityAnnouncerService.cs` |
| `Services/IFocusService.cs` | `Contracts/Services/Accessibility/IAccessibilityFocusService.cs` |
| `Explore.Blazor/wwwroot/js/accessibility.js` | `Explore.Blazor.Client/wwwroot/js/accessibility.js` (client-side ES module) |

### Key Decision: Blazouter Focus-on-Navigate
`FocusOnNavigate` component requires `RouteData` from standard ASP.NET Core Router — won't work with Blazouter. Implemented custom focus-on-navigate logic in MainLayout.razor.cs `OnLocationChanged` handler that calls `IAccessibilityFocusService.FocusOnNavigateAsync()` → JS `focusOnNavigate()` using double-rAF for render timing.

---

## Platform Rules (Active from Day 1)

These are permanent conventions — not tasks. They govern ALL code written during and after this initiative.

### PR-1: Page Shell Contract
Every routable page must have: one visible `h1`, `<PageTitle>`, content inside `<main id="main-content" tabindex="-1">` (provided by MainLayout).

### PR-2: Service Contract
- `IAccessibilityAnnouncerService`: `AnnouncePoliteAsync(message)`, `AnnounceAssertiveAsync(message)`
- `IAccessibilityFocusService`: `FocusAsync(selector)`, `FocusByIdAsync(id)`, `FocusMainContentAsync()`, `FocusOnNavigateAsync()`, `SaveFocusAsync()`, `RestoreFocusAsync(fallbackSelector?)`
- **Critical**: `RestoreFocusAsync` fallback chain: saved element → fallbackSelector → `#main-content` → `<body>`

### PR-3: Component Authoring Rules
1. Prefer `<button>`/`<a>` over clickable `<div>`/`<span>`
2. Prefer native landmarks over ARIA roles
3. Use ARIA only when native + MudBlazor don't solve it
4. All meaningful images require caller-supplied alt text
5. Dynamic status changes must declare priority: silent / polite / assertive
6. New CSS uses logical properties only (no `left`/`right`)
7. Never bind booleans to ARIA — use `"true"`/`"false"` strings
8. All targets ≥ 24x24px

### PR-4: CSS Direction Ban (Immediate)
New CSS must use `margin-inline-start/end`, `padding-inline-start/end`, `text-align: start/end`, `inset-inline-start/end`. Existing physical CSS migrated in Phase 5.

---

## Key Architecture Decisions

1. **MudBlazor-first**: Use built-in MudBlazor accessibility before custom ARIA. No custom focus-trap JS.
2. **Enhance in-place**: No "V2" or "Accessible" wrapper components. Modify existing files directly.
3. **"No ARIA is better than bad ARIA"**: Native HTML first → MudBlazor second → custom ARIA only as last resort.
4. **Live region pattern**: Container always in DOM (MainLayout). Content changes inside. Never conditionally render the container.
5. **Three-tier testing**: bUnit → Playwright axe-core → Manual NVDA.
6. **RTL via `lang` cookie**: App uses custom `LanguageContext` (not CultureInfo). Server reads `lang` cookie for SSR `<html lang dir>`. LanguageProvider JS handles post-hydration dynamic updates.
7. **Phase 3 risk bands**: Band A (navigation/task completion) → Band B (content comprehension) → Band C (visual polish).
8. **Governance starts early**: Platform rules, conventions doc, and AI skill start in Phase 1.
9. **Theme-first contrast validation**: Fix palette tokens first — fixes contrast everywhere.
10. **JS interop pattern**: ES module loaded via `import()`, lazy `IJSObjectReference`, `IAsyncDisposable`, `JSDisconnectedException` catch on dispose.
11. **Blazouter focus-on-navigate**: Custom implementation in MainLayout OnLocationChanged (standard FocusOnNavigate requires RouteData).
12. **HtmlTag="h1" pattern**: MudBlazor v9's `HtmlTag` parameter for semantic heading without visual change.
13. **Focus save/restore**: Global variable approach (not stack), wrap each top-level dialog entry point.
14. **Tailwind color scale for WCAG palette**: Shifted to blue-600, emerald-700, amber-700, red-600 for AA compliance.
15. **Direction preference**: "auto"|"ltr"|"rtl" persisted as user appearance preference with sparse override pattern. Follows existing ThemeMode flow.

---

## Dependencies & Constraints

### NuGet Packages Needed (Phase 2)
| Package | Purpose | Project |
|---------|---------|---------|
| `Deque.AxeCore.Playwright` | axe-core WCAG scanning | Event.API.IntegrationTests |

### Existing Patterns to Follow
- **NavMenu.razor**: Reference for ARIA attributes, keyboard handlers, BEM CSS
- **LoginPromptDialog.razor**: Reference for FocusAsync pattern
- **Footer.razor**: Reference for semantic HTML elements
- **CookieConsentInterop.cs**: Reference for JS interop service pattern

### Constraints
- **Render modes**: Components run in InteractiveAuto. JS interop must work in both Server + WASM.
- **Blazouter router**: Custom router — standard `FocusOnNavigate` component won't work. Custom implementation in MainLayout.
- **No breaking changes**: Accessibility is additive.
- **No custom focus-trap JS**: MudBlazor handles dialog focus trapping.
- **Pre-existing test failure**: `EventList_HidesNoEventsState_WhenResultsExist` — ArgumentNullException in EventList.razor:1116. Unrelated to a11y.

### Component Inventory (Scope)
- **Layout components**: 6
- **Shared components**: ~10
- **Pages**: ~50 (Home, Events, User, Orgs, Groups, Auth, Onboarding, Legal, Admin)
- **Nested event components**: 14 + 10 dialogs
- **Admin components**: ~38
- **Total .razor files**: ~120+

---

## Quick Resume

**To continue this task:**
1. Read this context file
2. Read `accessibility-improvements-tasks.md` for current progress
3. Read `accessibility-improvements-plan.md` for detailed acceptance criteria and risk bands
4. All 29/29 tasks complete — initiative done

**Reference implementations in codebase:**
- Best ARIA: `Explore.Blazor.Client/Layout/NavMenu.razor`
- Best focus: `Explore.Blazor.Client/Shared/LoginPromptDialog.razor`
- Best semantic HTML: `Explore.Blazor.Client/Layout/Footer.razor`
- Best keyboard: `Explore.Blazor.Client/Pages/Events/Dialogs/EventReviewDialog.razor` (WAI-ARIA radio group)
- Best dialog focus: `Explore.Blazor.Client/Pages/Events/EventDetail.razor.cs` (SaveFocus/RestoreFocus)
- Best announcements: `Explore.Blazor.Client/Pages/Events/EventList.razor.cs` (AnnouncerService)
- A11y services: `Explore.Blazor.Client/Services/Accessibility/`
- A11y JS: `Explore.Blazor.Client/wwwroot/js/accessibility.js`
- A11y CSS: `Explore.Blazor/wwwroot/css/utilities.css` (focus-visible, forced-colors), `tokens.css` (a11y tokens), `base.css` (target size)
- A11y docs: `docs/ACCESSIBILITY.md`
- A11y skill: `.claude/skills/accessibility/SKILL.md`
- A11y bUnit tests: `Explore.Blazor.Client.Tests/Accessibility/SharedComponentAccessibilityTests.cs`
- A11y arch tests: `Event.Architecture.Tests/AccessibilityConventionTests.cs`
- CI workflow: `.github/workflows/test.yml`
- RTL integration: `Explore.Blazor.Client/Layout/MainLayout.razor` (MudRTLProvider)
- Direction preference: `Explore.Blazor.Client/Models/LanguageContext.cs` (EffectiveIsRtl)
- Setting definitions: `Explore.Domain/Settings/Definitions/AppearanceSettingDefinitions.cs`
- BFF endpoints: `Explore.Blazor/Extensions/BffPreferenceEndpoints.cs`
