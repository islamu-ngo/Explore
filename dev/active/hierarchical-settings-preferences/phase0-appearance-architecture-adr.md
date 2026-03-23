ABOUTME: ADR for the first implementation slice of hierarchical appearance preferences and tenant theme catalogs.
ABOUTME: Fixes the storage boundary, precedence semantics, and SSR/bootstrap authority order before code-level rollout.

# ADR: Hierarchical Appearance Preferences And Theme Catalog Architecture

> **Last Updated:** 2026-03-22

## Status

Accepted for Phase 0 and the initial domain/settings foundation slice.

## Context

The repository already has the core ingredients for hierarchical preferences:

- `Explore.Application/Contracts/Infrastructure/IHierarchicalSettingsResolver.cs` resolves instance -> tenant -> organization -> group -> user precedence.
- `Explore.Domain/UserPreference.cs` already persists sparse per-user overrides.
- `Explore.Domain/Settings/SettingDefinition.cs` and `Explore.Domain/Settings/SettingRegistry.cs` already define the allowed scope and lock metadata per key.
- `Explore.Blazor.Client/Layout/MainLayout.razor.cs` and `Explore.Blazor.Client/Layout/SetupLayout.razor.cs` currently duplicate MudBlazor theme construction and browser-driven theme persistence.

The feature goal is broader than a light/dark toggle:

- instance admins must be able to control defaults and lock tenant behavior
- tenant admins must be able to manage named themes without editing free-form CSS
- users must be able to override only approved settings across devices
- authenticated preference state must become server-authoritative

The main architectural risk is treating a tenant theme catalog as just another JSON setting row. That would hide a mutable catalog inside generic settings storage and make concurrency, validation, fallback, and admin UX much harder to reason about.

## Decision

### 1. Keep one precedence system

The existing hierarchical settings engine remains the single precedence path for defaults and approved personal overrides.

- effective resolution stays instance -> tenant -> organization -> group -> user
- sparse per-user overrides continue to use `UserPreference`
- only explicitly promoted keys may resolve at `SettingScope.User`

No second long-term preference subsystem is introduced.

### 2. Store theme catalogs as first-class relational entities

Theme catalogs are not stored as JSON inside generic settings.

The initial model uses a single `UiTheme` aggregate with:

- nullable `TenantId`
- `TenantId = null` for platform-owned themes
- `TenantId != null` for tenant-owned themes
- bounded light/dark palette value objects mapped to explicit columns
- audit fields and optimistic concurrency via `xmin`/row version

This keeps theme selection reference-based while allowing proper validation, ordering, default management, and deterministic concurrency handling.

### 3. Use settings only for references and behavior

The settings engine stores only reference and behavior keys, not the catalog payload.

The first approved appearance keys are:

- `appearance.default_theme_id`
- `appearance.theme_mode`

The existing `events.card_click_opens_detail_page` key is promoted to user scope so the same precedence engine governs that personal behavior.

### 4. MVP lock semantics stay simple

For MVP, technical policy is:

- `SettingDefinition.MaxScope` decides whether user overrides are legal
- instance lock remains the only hard lock
- no tenant-level user-override suppression flags are introduced in this slice

If product requirements later need tenant-level suppression of user appearance choices, that is a separate decision.

### 5. Theme composition moves out of layouts

`MainLayout` and `SetupLayout` must become consumers of a dedicated runtime/theme service boundary.

Future runtime contracts should separate:

- appearance resolution and provenance
- theme composition into `MudTheme`
- SSR/bootstrap orchestration

Layout code must not remain the place where precedence, fallback, or palette mapping rules live.

### 6. SSR/bootstrap authority order is fixed now

Runtime authority order is:

1. anonymous SSR uses tenant-resolved appearance defaults
2. authenticated SSR uses server-known user preference when available
3. browser system preference is applied only when effective mode is `system`
4. cookies/bootstrap hints never override authoritative authenticated database state

Cookies may remain as short-lived SSR/bootstrap hints, but they are not the source of truth for authenticated users.

## Consequences

### Positive

- appearance behavior and user overrides stay inside one precedence model
- theme catalogs become easier to validate, query, preview, and migrate
- cross-device user preferences become compatible with authenticated server state
- layout code can shrink into presentation-only theme consumers

### Negative

- theme persistence is more complex than storing a JSON blob in settings
- nullable `TenantId` means theme repositories must enforce scope intentionally instead of relying only on `ITenantEntity` query filters
- full delivery still requires new runtime services, transport endpoints, and UI work beyond this foundation slice

## Rejected Alternatives

### Rejected: `appearance.available_themes` JSON in generic settings

Rejected because it would:

- create an accidental mini-database inside generic settings
- make concurrency/fallback behavior opaque
- complicate admin editing and list management
- make future filtering/reporting/support tooling harder

### Rejected: browser cookie/local storage as authenticated truth

Rejected because it cannot provide reliable cross-device preference persistence and would drift from server-authoritative settings.

### Rejected: keeping theme precedence in `MainLayout`

Rejected because it duplicates logic, mixes runtime policy with rendering, and makes SSR/bootstrap behavior harder to verify.

## Verification Target For This Slice

The initial implementation slice should establish:

1. an accepted ADR documenting the architecture and authority order
2. appearance setting keys and definitions registered in the existing settings engine
3. selective user-overridable keys promoted explicitly
4. a first-class `UiTheme` domain model and EF configuration with explicit palette columns and concurrency support
5. a typed appearance setting group that resolves the new reference/behavior keys without changing runtime UI behavior yet
