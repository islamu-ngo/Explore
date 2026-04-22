# Hierarchical Settings Preferences - Implementation Report

Last Updated: 2026-04-21

## Executive Summary
The "Hierarchical Settings Preferences" feature has advanced through Phase 2 (backend user-preference completeness) and Phase 4 (BFF DB-backed flow + cache/SSR policy). Phases 5 (Runtime theme composition, admin/user UI) and Phase 6 (tests, docs) remain deferred per user direction. The runtime presentation (Blazor Client) still relies on hardcoded palettes; admin UI for `UiTheme` catalog is not yet wired.

## Findings Details

### What is Completed

#### Phase 0 — ADR
`UiTheme` first-class entities adopted over JSON settings.

#### Phase 1 — Domain & Registry
`UiTheme` aggregate, `UiThemePalette` value object (16 MudBlazor tokens), `AppearanceSettingDefinitions` (ThemeMode, Language, Direction, DefaultThemeId) implemented.

#### Phase 3 — Persistence
`UiThemeConfiguration`, `UiThemeRepository`, EF Core migrations applied.

#### Phase 2 — Application (Backend user preferences)
- `AppearanceSettingGroup` resolves all four appearance keys from the 5-tier resolver.
- `UiTheme` catalog read/update handlers present (`GetUiThemeCatalog`, `GetUiThemeDetails`, `CreateUiTheme`, `UpdateUiTheme`). Delete handler NOT yet implemented.
- `UpdateCurrentUserAppearancePreferencesCommandHandler` refactored: single `UpsertOrRemoveOverrideAsync` helper eliminates triple duplication and persists all four keys (ThemeMode, Language, Direction, DefaultThemeId) as sparse overrides. `DefaultThemeId` is validated for tenant visibility via `IUiThemeRepository.IsThemeVisibleToTenantAsync`.
- `UpdateUserAppearancePreferencesDto` + validator extended with `DefaultThemeId` (nullable Guid; existence/visibility deferred to handler).
- `PublicExperienceSettingsDto` + `GetPublicExperienceSettingsQueryHandler` expose resolved anonymous-safe `DefaultThemeId`, `ThemeMode`, `Direction`, `Language` via tenant-scope `SettingContext(TenantId: tenantId)` — no user scope so anonymous SSR is safe.

#### Phase 4 — API/BFF transport (DB-backed flow)
- `/bff/theme`, `/bff/language`, `/bff/direction` rewritten:
  1. Read current effective preferences (API-authoritative for authenticated, cookie-sourced for anonymous).
  2. Mutate only the requested field via record-style `with { ... }` (DTO converted to `record class`).
  3. Persist full record via PUT `api/user/appearance` (prevents the prior payload-loss bug where Language/DefaultThemeId got reset).
  4. Mirror the single changed field to its cookie for anonymous continuity and SSR hydration.
- Client `AppearanceThemeService.PersistThemeModeAsync` fixed to POST `?theme={mode}` query string (was posting JSON body — unreadable by BFF).
- Client private duplicate `UserThemePreferenceResponse` removed in favor of NSwag-generated `Explore.Blazor.Client.Clients.UserAppearancePreferencesDto`.

#### Phase 5 (Partial) — UI shell
`MainLayout.razor.cs` and `SetupLayout.razor.cs` delegate theme/direction decisions to `AppearanceThemeService`. Service still composes a hardcoded palette.

### Cache Keys & Invalidation Strategy (Phase 4 Task 2)

#### Where cache exists today
| Component | Cache | Key format | TTL | Invalidation |
|---|---|---|---|---|
| `HierarchicalSettingsResolver` | `IMemoryCache` | `HierSettings:System`, `HierSettings:Tenant:{tenantId}`, `HierSettings:Org:{orgId}`, `HierSettings:Group:{groupId}`, `HierSettings:User:{tenantId}:{userId}` | 5 min | `InvalidateCache(scope, scopeId)` (scope-aware cascade) and `InvalidateUserCache(tenantId, userId)` |
| `UiThemeRepository` | None | — | — | — (reads hit DB) |
| Output cache on API endpoints | `OutputCache` | Policy-driven (`ListData` 30s, `DetailData` 60s, `LookupData` 1h) | See policy | Tagged eviction (`events:list`, `events:detail`, etc.) |
| HybridCache (entity reads) | `HybridCache` | `event:detail:{id}` etc. | 5/30 min | `RemoveAsync(key)` on write handlers |

#### Appearance data flow
1. User-scope appearance resolution walks settings rows → cached in `HierSettings:User:{tenantId}:{userId}`.
2. Tenant-scope appearance resolution (anonymous SSR path) cached in `HierSettings:Tenant:{tenantId}`.
3. `AppearanceSettingGroup` is a lightweight DTO built from cached rows — not itself cached.
4. `UiTheme` entity lookups (palette data) hit the repository directly — no caching layer today.

#### Invalidation rules (enforced in handlers)

| Writer | What changes | Must invalidate |
|---|---|---|
| `UpdateCurrentUserAppearancePreferencesCommandHandler` | User's `UserPreference` rows for `Appearance.*` keys | `InvalidateUserCache(tenantId, userId)` — **already implemented** at end of handler |
| Future: tenant admin handler for `Appearance.DefaultThemeId` at tenant scope | Tenant row for setting | `InvalidateCache(SettingScope.Tenant, tenantId)` — cascade clears org/group/user implicitly |
| Future: instance admin handler for `Appearance.DefaultThemeId` at instance scope | Instance row for setting | `InvalidateCache(SettingScope.Instance, null)` — cascade clears all tenants |
| `CreateUiThemeCommandHandler` / `UpdateUiThemeCommandHandler` | Theme palette / active / default flags | **No resolver cache invalidation needed** — palettes are read live from `UiThemeRepository`; `DefaultThemeId` values stored in settings are unaffected. Output cache on future `UiThemeAdminController` endpoints must be tagged (see Phase 5). |
| Future: `DeleteUiThemeCommandHandler` | Theme hard-deleted (or deactivated) | **Orphan cleanup**: if any tenant/user `DefaultThemeId` points at the removed theme, re-resolution must gracefully fall back. Cache invalidation alone is insufficient — `AppearanceThemeService` (Phase 5) must handle `null` / invalid `DefaultThemeId` lookups and fall back to the instance default. |

#### Dangling-pointer policy (important)
`DefaultThemeId` stored in `UserPreference` / tenant / instance rows is NOT FK-enforced against `UiTheme` (keep it loose to survive theme rotation). Consumers (`AppearanceThemeService`, API) MUST:
1. Query theme by ID.
2. If not found OR `!IsActive`, fall back to tenant default (`UiThemeRepository.GetDefaultThemeAsync(tenantId)`).
3. If tenant default missing, fall back to platform default (`GetDefaultThemeAsync(null)`).
4. If no platform default, use built-in `AppearanceThemeService` fallback palette.

### SSR Bootstrap Authority Order (Phase 4 Task 3)

The server-side render must resolve the correct theme on first request before any JS runs. Authority order:

1. **Authenticated user** — `GET api/user/appearance` returns `UserAppearancePreferencesDto` resolved from full 5-tier hierarchy. Use DTO's `ThemeMode` / `DefaultThemeId` / `Direction` / `Language` as authoritative.
2. **Anonymous user, tenant-scoped** — `GetPublicExperienceSettingsQuery` returns `PublicExperienceSettingsDto` with the four appearance fields resolved at `SettingContext(TenantId: tenantId)`. No user scope — safe for anonymous rendering.
3. **Anonymous user, cookies present** — `theme`, `direction`, `lang` cookies reflect the user's last explicit choice (written by BFF endpoints). Used for second/subsequent anonymous renders between sessions. BFF never reads cookies for authenticated users; API DB is authoritative there.
4. **Anonymous user, no cookies** — BFF `/bff/theme` GET falls back to `ReadCookiePreferences` defaults: `ThemeMode="system"`, `Direction="auto"`, `Language="en"`, `DefaultThemeId=null`.

Blazor bootstrap (App.razor → MainLayout/SetupLayout):
- SSR pass: `HttpContext` available. Read cookies + optionally `PublicExperienceService` hydrated `DefaultThemeId`. Cascade as `InitialTheme` parameter.
- InteractiveAuto pass (Server then WASM): `AppearanceThemeService.ResolveInitialDarkModeAsync` hits `/bff/theme` as the single source of truth. Cookies still act as the anonymous continuity mechanism.
- No `HttpContext`-dependent code in Client project — all transport via `/bff/*` endpoints.

### What Still Needs Implementation (Deferred)

- **Phase 5 (Runtime & UI)**:
  - `AppearanceThemeService` to consume dynamic `UiTheme` data from backend (via new `/bff/themes` or similar proxy) instead of hardcoded `PaletteLight`/`PaletteDark`.
  - `UiThemeAdminController` REST endpoints with HAL links (catalog list, details, create, update, delete) + `DeleteUiThemeCommand` + handler.
  - `InstanceBrandingSection` rebuilt to manage platform `UiTheme` catalog + instance-scope `DefaultThemeId` setting (with lock).
  - `TenantBrandingSection` rebuilt to manage tenant `UiTheme` catalog + tenant-scope `DefaultThemeId`.
  - User preferences page: theme picker + event-card click behavior toggle.
- **Phase 6**: Unit tests for the refactored handler + integration tests for `/bff/theme` round-trip. Doc + journal updates.

## Recommendations
- **Phase 5 priority**: `AppearanceThemeService` rework first — nothing else matters until dynamic palettes render. Then `UiThemeAdminController` (API) to unlock Admin UI work.
- **Orphan policy**: Implement the 4-step fallback in `AppearanceThemeService` before exposing `DefaultThemeId` editing UI. Without it, a deactivated theme produces a silent render fallback with no UX signal.
- **Don't add premature cache layers**: The current invalidation footprint (`InvalidateUserCache` on user preference write) is sufficient for Phase 4. Add new invalidations only when new writers appear (tenant/instance admin handlers in Phase 5).
