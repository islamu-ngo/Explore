ABOUTME: Execution checklist for hierarchical settings, tenant theming, and user preference persistence work.
ABOUTME: Breaks the revised implementation into ordered, testable tasks with explicit architectural guardrails.

# Hierarchical Settings Preferences - Task Checklist

Last Updated: 2026-03-22

## Session Checkpoint ✅ COMPLETE

- [x] Create the planning doc set for this track
- [x] Verify current settings/theming architecture and relevant repo patterns
- [x] Revise the plan to reject JSON-based theme catalogs
- [x] Capture reset-safe handoff notes, restart commands, and next implementation steps

## Phase 0: ADR And Runtime Contract

- [ ] Write the appearance architecture ADR before coding
  - Acceptance: ADR defines first-class theme entities, rejects JSON catalog storage, fixes MVP lock semantics, and documents SSR/bootstrap authority order.
  - Depends on: none
  - Effort: S
  - Skills: `clean-architecture-rules`

## Phase 1: Domain, Registry, And Theme Model Foundations

- [ ] Add appearance/theme keys to `Explore.Domain/Constants/GovernanceSettingKeys.cs`
  - Acceptance: stable dot-notation keys exist for default theme reference and theme mode without storing theme catalog JSON.
  - Depends on: ADR complete
  - Effort: M
  - Skills: `clean-architecture-rules`

- [ ] Create first-class theme entities and bounded palette value objects
  - Acceptance: theme catalog is modeled relationally with auditing, active/default state, and concurrency support.
  - Depends on: previous task
  - Effort: L
  - Skills: `clean-architecture-rules`, `dotnet-efcore-guidelines`

- [ ] Create `Explore.Domain/Settings/Definitions/AppearanceSettingDefinitions.cs`
  - Acceptance: definitions include defaults, allowed values, scope limits, and clear descriptions for references/behavior only.
  - Depends on: previous tasks
  - Effort: M
  - Skills: `clean-architecture-rules`

- [ ] Register appearance settings in `Explore.Domain/Settings/SettingRegistry.cs`
  - Acceptance: new definitions are discoverable through the registry.
  - Depends on: previous task
  - Effort: S
  - Skills: `clean-architecture-rules`

- [ ] Promote selective existing keys to user scope
  - Acceptance: `events.card_click_opens_detail_page` becomes explicitly user-overridable, while theme-catalog/admin settings stay tenant/admin scoped.
  - Depends on: previous tasks
  - Effort: S
  - Skills: `clean-architecture-rules`

## Phase 2: Application Resolution And Validation

- [ ] Add typed appearance setting group(s) under `Explore.Application/Settings/Groups/`
  - Acceptance: default theme reference and theme mode resolve through one typed API.
  - Depends on: Phase 1 complete
  - Effort: M
  - Skills: `clean-architecture-rules`

- [ ] Add theme catalog validators with hex, uniqueness, and default-integrity checks
  - Acceptance: invalid hex values, duplicate IDs, incomplete palettes, invalid default references, and disabled-theme misuse are rejected.
  - Depends on: Phase 1 complete
  - Effort: M
  - Skills: `cqrs-mediatr-guidelines`, `blazor-ui-conventions`

- [ ] Add theme catalog CRUD CQRS flows with concurrency handling
  - Acceptance: admins can create/update/activate/deactivate themes and concurrency conflicts are surfaced deterministically.
  - Depends on: previous tasks
  - Effort: L
  - Skills: `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`

- [ ] Add admin CQRS flows for instance and tenant appearance defaults/locks
  - Acceptance: instance admins manage defaults/locks; tenant admins manage default theme selection and related appearance behavior.
  - Depends on: previous tasks
  - Effort: L
  - Skills: `cqrs-mediatr-guidelines`, `clean-architecture-rules`

- [ ] Add authenticated user preference CQRS flows for approved overrides
  - Acceptance: users can save/reset theme and event-card behavior overrides without bypassing lock rules.
  - Depends on: previous tasks
  - Effort: L
  - Skills: `cqrs-mediatr-guidelines`, `auth-patterns`

- [ ] Add dedicated theme composition/runtime services
  - Acceptance: `MainLayout` and `SetupLayout` consume a service boundary instead of embedding precedence and palette mapping logic.
  - Depends on: previous tasks
  - Effort: M
  - Skills: `clean-architecture-rules`, `blazor-ui-conventions`

- [ ] Split anonymous runtime DTOs from authenticated preference DTOs
  - Acceptance: public-safe settings remain separate from authenticated user overlays.
  - Depends on: previous tasks
  - Effort: M
  - Skills: `clean-architecture-rules`

## Phase 3: Persistence And Transport

- [ ] Persist the theme catalog as first-class relational data
  - Acceptance: theme catalog data is not stored in generic settings or JSON policy blobs.
  - Depends on: Phase 2 contract complete
  - Effort: L
  - Skills: `dotnet-efcore-guidelines`

- [ ] Reuse `UserPreference` for sparse personal overrides and extend repositories only where needed
  - Acceptance: no parallel long-term per-user preference store is introduced.
  - Depends on: Phase 2 contract complete
  - Effort: M
  - Skills: `dotnet-efcore-guidelines`

- [ ] Add EF migration for theme entities, references, and concurrency support
  - Acceptance: migration scope is justified and documented; snapshot updated if needed.
  - Depends on: previous tasks
  - Effort: M
  - Skills: `dotnet-efcore-guidelines`

- [ ] Replace cookie-only theme persistence path with authenticated DB-backed endpoint flow
  - Acceptance: theme selection persists across devices through server state, not only browser state.
  - Depends on: user preference CQRS ready
  - Effort: M
  - Skills: `blazor-bff-patterns`, `auth-patterns`

- [ ] Define cache keys and invalidation for tenant/user appearance runtime
  - Acceptance: tenant-level, user-level, and theme-catalog changes invalidate the correct cache scopes.
  - Depends on: previous tasks
  - Effort: M
  - Skills: `clean-architecture-rules`

- [ ] Formalize SSR bootstrap authority order before UI wiring
  - Acceptance: anonymous, authenticated, and `system` mode flows are deterministic and documented.
  - Depends on: previous tasks
  - Effort: M
  - Skills: `blazor-bff-patterns`, `auth-patterns`

## Phase 4: Admin And User Interface

- [ ] Extend `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceBrandingSection.razor`
  - Acceptance: instance admins can define appearance defaults and lock theme-related tenant customization.
  - Depends on: transport layer ready
  - Effort: L
  - Skills: `blazor-ui-conventions`, `blazor-css-isolation`

- [ ] Extend `Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantBrandingSection.razor`
  - Acceptance: tenant admins can manage multiple named themes with validated hex inputs and preview states.
  - Depends on: transport layer ready
  - Effort: XL
  - Skills: `blazor-ui-conventions`, `blazor-css-isolation`

- [ ] Add user preferences UI for theme and event-card behavior
  - Acceptance: users can choose theme/mode, override click behavior, and reset to inherited defaults.
  - Depends on: user preference transport ready
  - Effort: L
  - Skills: `blazor-ui-conventions`, `blazor-css-isolation`

- [ ] Rework `Explore.Blazor.Client/Layout/MainLayout.razor.cs` and `Explore.Blazor.Client/Layout/SetupLayout.razor.cs` to consume the runtime theme service
  - Acceptance: runtime theme comes from resolved tenant/user settings via a dedicated service and still supports `system` mode.
  - Depends on: previous UI + transport tasks
  - Effort: L
  - Skills: `blazor-ui-conventions`

## Phase 5: Tests And Documentation

- [ ] Add unit tests for resolver precedence, lock behavior, and validation
  - Acceptance: user-overridable keys, locked keys, invalid theme data, invalid default references, and disabled-theme restrictions are covered.
  - Depends on: implementation complete
  - Effort: M

- [ ] Add integration tests for save/read/reset flows and tenant isolation
  - Acceptance: database persistence, authenticated preference flows, concurrency conflicts, fallback from removed themes, and SSR bootstrap consistency are covered end-to-end.
  - Depends on: previous task
  - Effort: L

- [ ] Add Blazor/client tests for user preference UX and event-card behavior
  - Acceptance: theme selection and detail-page/drawer behavior are verifiable at UI level.
  - Depends on: previous task
  - Effort: M

- [ ] Update docs and dev docs with final architecture decisions
  - Acceptance: settings categories, override semantics, bootstrap authority order, and rollout notes are documented.
  - Depends on: tests stable
  - Effort: S/M

## Quick Resume

Start with Phase 0. The highest-leverage first steps are the ADR plus the decision to model theme catalogs as first-class entities while keeping settings reference-based.

Before coding:
- read `dev/active/hierarchical-settings-preferences/hierarchical-settings-preferences-context.md`
- read `dev/active/hierarchical-settings-preferences/hierarchical-settings-preferences-handoff-2026-03-22.md`
- inspect `git status --short` because the worktree contains many unrelated changes
