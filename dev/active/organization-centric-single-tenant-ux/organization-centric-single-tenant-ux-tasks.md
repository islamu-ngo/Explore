<!-- ABOUTME: Checklist for implementing organization-centric single-tenant UX through settings, navigation, and filters. -->
<!-- ABOUTME: Tracks acceptance criteria while explicitly avoiding new organizer/workspace domain scope entities. -->

# Organization-Centric Single-Tenant UX - Task Checklist

Last Updated: 2026-04-30

## Phase 0: Planning and Evidence ✅ COMPLETE

- [x] Confirm user intent: no OrganizerScope/BusinessScope/Workspace/SubTenant model.
  - **Priority:** High
  - **Effort:** S
  - **Acceptance:** Plan explicitly rejects new internal scope domain entities.
  - **Skills:** agentic-research, clean-architecture-rules
- [x] Read `.claude/commands/dev-docs.md` and `dev/active/README.md`.
  - **Priority:** High
  - **Effort:** S
  - **Acceptance:** Three files created under `dev/active/organization-centric-single-tenant-ux/`.
  - **Skills:** agentic-research
- [x] Load architecture/UI/CQRS/EF/auth skills.
  - **Priority:** High
  - **Effort:** S
  - **Acceptance:** Plan reflects Clean Architecture, Blazor, CQRS, EF Core, and auth constraints.
  - **Skills:** clean-architecture-rules, blazor-ui-conventions, cqrs-mediatr-guidelines, dotnet-efcore-guidelines, auth-patterns
- [x] Use Tavily MCP for industry and UX research.
  - **Priority:** High
  - **Effort:** S
  - **Acceptance:** Plan references shallow navigation, filter-first UX, bounded customization, and community-platform usability findings.
  - **Skills:** agentic-research
- [x] Use Context7 MCP for Blazor, MudBlazor, and EF Core documentation.
  - **Priority:** High
  - **Effort:** S
  - **Acceptance:** Plan reflects supported Blazor component composition, MudBlazor navigation/filter components, and EF query/index guidance.
  - **Skills:** agentic-research, blazor-ui-conventions, dotnet-efcore-guidelines

## Phase 1: Public Experience Vocabulary and Guardrails ⏳ NOT STARTED

- [ ] T1.1 Define public-experience posture values.
  - **Priority:** High
  - **Effort:** S
  - **Dependencies:** Phase 0
  - **Acceptance:** DiscoveryCentric and OrganizationCentric can be represented without adding a domain scope entity; documentation/comments state `Organization` is an in-tenant actor-backed publisher/organizer, not the tenant.
  - **Skills:** clean-architecture-rules
- [ ] T1.2 Add setting metadata for event catalog label, primary organization, bounded CTAs/home blocks, and typed event-section presets.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T1.1
  - **Acceptance:** Anonymous public shell settings resolve from Tenant + Instance public settings plus tenant-local referenced content only; user/group-specific setting scopes do not affect anonymous home/nav/catalog/footer output; presets are versioned config records mapped to DTOs, not raw query strings or Blazor-facing DTOs as authoritative config.
  - **Skills:** clean-architecture-rules, cqrs-mediatr-guidelines
- [ ] T1.3 Add guardrail documentation/comments explaining segmentation is filter/category/tag based.
  - **Priority:** Medium
  - **Effort:** S
  - **Dependencies:** T1.1
  - **Acceptance:** Future implementers do not infer a need for OrganizerScope/Workspace and distinguish publisher ownership filters from audience/section segmentation.
  - **Skills:** agentic-research
- [ ] T1.4 Add architecture/convention guardrails for forbidden concepts and Domain default drift.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T1.1, T1.2
  - **Acceptance:** Context-aware tests fail on new forbidden Domain entity files, migration tables, `WorkspaceId`/`OrganizerScopeId` on `Event`, `SubTenantId` on tenant-scoped entities, `ScopeId` in event ownership paths, and public-experience code that treats `OrganizationId` as tenant resolver input; tests do not fail on legitimate existing settings, authorization, notification, registration, projection, secret, or governance scope vocabulary; tests/comments catch Domain-layer business defaults for public posture, presets, event visibility posture, and import convenience.
  - **Skills:** clean-architecture-rules, dotnet-efcore-guidelines

## Phase 2: Backend Contract and Actor-Backed Filtering ⏳ NOT STARTED

- [ ] T2.1 Introduce the typed `PublicExperienceShellDto` read model.
  - **Priority:** High
  - **Effort:** L
  - **Dependencies:** T1.1
  - **Acceptance:** Application returns a server-shaped shell with `SchemaVersion`, `Revision`, mode, home, navigation, event catalog, explicit primary organization state, event sections, CTAs, and footer read projection; DTOs only, no Domain entity leaks to API/Blazor; Blazor does not reconstruct posture from settings blobs; shell cache invalidates when relevant public-experience settings, primary organization metadata, footer config, tenant navigation links, or preset config changes.
  - **Skills:** cqrs-mediatr-guidelines
- [ ] T2.2 Map typed event-section presets to existing event specification filters and generated URLs.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T1.2
  - **Acceptance:** Versioned preset config records can express actor ownership, organization/group publisher ownership via actor resolution, category/tag/audience/event-type/format/date/custom-property filters; generated query URLs are shareable but not authoritative persistence; public DTOs are generated from validated Application models; no workspace selector or WorkspaceId exists.
  - **Skills:** cqrs-mediatr-guidelines
- [ ] T2.3 Wire actor-backed ownership filtering into the public event-list request/API flow.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T1.1
  - **Acceptance:** This is the first code milestone. `EventFilterRequest`, `GetEventListRequest`, URL helpers, and generated clients can carry `ActorId`, `OrganizationId`, and `GroupId`; organization/group-to-actor resolution maps to existing actor ownership filtering; private/unauthorized/cross-tenant event visibility remains protected; query-string round trip is tested; no OrganizationScope/WorkspaceId is introduced; ownership filtering remains distinct from category/audience/section segmentation.
  - **Skills:** cqrs-mediatr-guidelines, auth-patterns, dotnet-efcore-guidelines
- [ ] T2.4 Add primary organization failure handling and import/default contract tests.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T2.1, T2.2, T2.3
  - **Acceptance:** Application unit tests cover discovery-centric and organization-centric shells, `PrimaryOrganizationState.Available`, `NotConfigured`, `Missing`, `Deleted`, `HiddenOrInactive`, `CrossTenantInvalid`, and `ActorUnavailable`, minimal import/create-shaped inputs that omit non-essential taxonomy/audience/custom-property/org-centric fields, and typed preset config translation.
  - **Skills:** clean-architecture-rules, cqrs-mediatr-guidelines, dotnet-efcore-guidelines

## Phase 3: API, Defaults, Governance, and Authorization ⏳ NOT STARTED

- [ ] T3.1 Extend public-experience read endpoint response through Application DTOs.
  - **Priority:** High
  - **Effort:** S
  - **Dependencies:** T2.1
  - **Acceptance:** Anonymous-safe public shell remains readable where intended; shell resolution excludes user/group personalization; shell response includes schema version and revision/cache token.
  - **Skills:** auth-patterns, cqrs-mediatr-guidelines
- [ ] T3.2 Add seed/default organization-centric shell config before full admin editor.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T1.2, T2.1, T2.3
  - **Acceptance:** A default/read-only organization-centric shell can be returned for a configured primary organization; defaults are Application-owned; footer is included only as read projection; no admin form or scope model is required for this milestone.
  - **Skills:** auth-patterns, cqrs-mediatr-guidelines
- [ ] T3.3 Update Cerbos/local authorization parity if new admin actions are introduced.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T3.2
  - **Acceptance:** AuthorizationParity tests cover new actions or prove existing action coverage is reused.
  - **Skills:** auth-patterns

## Phase 4: Persistence and Performance ⏳ NOT STARTED

- [ ] T4.1 Persist posture, primary organization, CTA/home-block, and typed preset settings through existing settings tables.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T1.2
  - **Acceptance:** No new scope/workspace table; tenant and soft-delete filters unchanged; authoritative config is versioned typed config records, not raw query strings or Blazor-facing display DTOs.
  - **Skills:** dotnet-efcore-guidelines
- [ ] T4.2 Add migration only if seed data, setting definitions, or indexes require it.
  - **Priority:** Medium
  - **Effort:** M
  - **Dependencies:** T4.1
  - **Acceptance:** Migration is small/focused and generated SQL is reviewed.
  - **Skills:** dotnet-efcore-guidelines
- [ ] T4.3 Validate index coverage for common presets.
  - **Priority:** Medium
  - **Effort:** M
  - **Dependencies:** T2.2, T2.3
  - **Acceptance:** Existing indexes are reused when sufficient; new indexes are evidence-driven.
  - **Skills:** dotnet-efcore-guidelines
- [ ] T4.4 Confirm persistence defaults stay limited to persistence concerns.
  - **Priority:** High
  - **Effort:** S
  - **Dependencies:** T4.1
  - **Acceptance:** Domain entities do not gain business defaults for public experience, section presets, organization posture, event visibility posture, or import convenience; EF defaults are limited to persistence counters/flags or similarly infrastructure-owned values.
  - **Skills:** clean-architecture-rules, dotnet-efcore-guidelines
- [ ] T4.5 Define shell revision and cache invalidation inputs.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T2.1, T4.1
  - **Acceptance:** Revision derives from relevant public-experience setting versions/timestamps, preset config, primary organization metadata, footer config, and tenant navigation links; HybridCache key/invalidation strategy and ETag behavior are documented/testable.
  - **Skills:** cqrs-mediatr-guidelines, dotnet-efcore-guidelines

## Phase 5: Shell-Driven Blazor Read Path ⏳ NOT STARTED

- [ ] T5.1 Implement organization-centric home composition.
  - **Priority:** High
  - **Effort:** L
  - **Dependencies:** T2.1
  - **Acceptance:** Home renders from `PublicExperienceShellDto` via a typed shell client method and can show organization-first content, upcoming events, featured event, featured filters/sections, CTAs, contact/location, donation/volunteer, and footer read projection without arbitrary HTML/CSS/component composition.
  - **Skills:** blazor-ui-conventions
- [ ] T5.2 Update startup routing/home resolution for posture.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T5.1
  - **Acceptance:** OrganizationCentric mode reaches the organization-first home; DiscoveryCentric mode can still reach event-list-first behavior; missing primary organization renders safe neutral/onboarding UX.
  - **Skills:** blazor-ui-conventions
- [ ] T5.3 Update `NavMenu.razor` and `AppSideNav.razor` for shallow posture-aware navigation.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T2.1
  - **Acceptance:** Advanced Search/Recently Added/Random are hidden or demoted in OrganizationCentric mode; event catalog can be relabeled Calendar/Programs/Activities/Events; tenant nav links remain supported.
  - **Skills:** blazor-ui-conventions
- [ ] T5.4 Add curated filter chips/presets to event list.
  - **Priority:** High
  - **Effort:** L
  - **Dependencies:** T2.2, T2.3
  - **Acceptance:** Admin-configured sections like Youth/Sisters/Education/Community Services are represented as typed presets rendered as keyboard-reachable filters and generated query-string links.
  - **Skills:** blazor-ui-conventions, cqrs-mediatr-guidelines
- [ ] T5.5 Ensure all actions remain HAL/server-authorized.
  - **Priority:** High
  - **Effort:** S
  - **Dependencies:** T5.1-T5.4
  - **Acceptance:** No local role/claim checks are added for action visibility.
  - **Skills:** auth-patterns, blazor-ui-conventions
- [ ] T5.6 Implement accessibility and empty-state acceptance criteria.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T5.1-T5.4
  - **Acceptance:** OrganizationCentric home has one visible h1; skip link/main/header/nav/live regions remain intact; presets have accessible names and keyboard access; active filters are visually clear and announced where appropriate; empty states distinguish no events, no matches, and missing primary organization; focus-visible and RTL/logical CSS are preserved.
  - **Skills:** blazor-ui-conventions

## Phase 5b: Admin/Onboarding Editor After Read-Path Proof ⏳ NOT STARTED

- [ ] T5b.1 Add or extend admin/onboarding write flow for posture, primary organization, and typed presets.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T2.1, T2.3, T3.2, T5.1-T5.6
  - **Acceptance:** Implemented only after backend/read-path proof; writes are authorized, resource scoped, and tenant-local; invalid/deleted/hidden/cross-tenant references are rejected or omitted through Application; no browser token exposure; versioned config records are persisted, not display DTOs or raw query strings; editor language does not introduce workspace/scope concepts.
  - **Skills:** auth-patterns, blazor-ui-conventions, cqrs-mediatr-guidelines

## Phase 6: Testing and Verification ⏳ NOT STARTED

- [ ] T6.1 Run LSP diagnostics on all modified files.
  - **Priority:** High
  - **Effort:** S
  - **Dependencies:** Implementation complete
  - **Acceptance:** Zero LSP errors.
  - **Skills:** clean-architecture-rules
- [ ] T6.2 Run architecture tests.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** Implementation complete
  - **Acceptance:** CleanArchitecture, CqrsPattern, BlazorClientArchitecture, and AuthorizationParity tests pass.
  - **Skills:** clean-architecture-rules, auth-patterns
- [ ] T6.3 Run application, persistence, API, and Blazor tests.
  - **Priority:** High
  - **Effort:** L
  - **Dependencies:** Implementation complete
  - **Acceptance:** Related project test suites pass with Release configuration.
  - **Skills:** cqrs-mediatr-guidelines, dotnet-efcore-guidelines, blazor-ui-conventions
- [ ] T6.4 Run full build.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** Tests passing
  - **Acceptance:** `dotnet build --configuration Release --verbosity quiet` exits 0.
  - **Skills:** clean-architecture-rules
- [ ] T6.5 Search for forbidden domain additions before final review.
  - **Priority:** High
  - **Effort:** S
  - **Dependencies:** Implementation complete
  - **Acceptance:** Precise guardrails modeled after existing architecture/naming tests pass: no forbidden Domain entity file, no forbidden migration table, no `WorkspaceId`/`OrganizerScopeId` on `Event`, no `SubTenantId` on tenant-scoped entities, no `ScopeId` in event ownership paths, no public-experience code treating `OrganizationId` as tenant resolver input; valid existing `Scope` vocabulary remains allowed; no wording treats Organization as Tenant.
  - **Skills:** clean-architecture-rules
- [ ] T6.6 Run product/UX regression tests for public postures and accessibility.
  - **Priority:** High
  - **Effort:** L
  - **Dependencies:** Implementation complete
  - **Acceptance:** Tests cover DiscoveryCentric and OrganizationCentric shells, tenant/instance-only anonymous shell resolution, shell schema/revision/cache invalidation, `/events` reachability and relabeling, curated preset URLs, explicit primary-organization enum states, bounded home blocks, accessible filter presets, empty states, and HAL-gated action visibility.
  - **Skills:** blazor-ui-conventions, auth-patterns, cqrs-mediatr-guidelines

## Quick Resume

1. Read `organization-centric-single-tenant-ux-context.md`.
2. Start implementation at Phase 1.
3. First code milestone: wire actor-backed `/events` ownership filtering and tests before curated UI/admin editors.
4. Build the typed, versioned `PublicExperienceShellDto` and versioned preset config pipeline before Blazor UI work.
5. Anonymous public shell resolution must be tenant/instance public settings + tenant-local referenced content only.
6. Keep every implementation change aligned to existing Tenant/Organization/Group/Actor/Event/filtering architecture: Organization is a publisher/organizer actor inside a tenant, not the tenant.
7. Keep defaults and import tolerance in Application/validators or EF persistence configuration where appropriate; do not add Domain business defaults.
8. Do not introduce a new operational scope model.
