<!-- ABOUTME: Checklist for implementing organization-centric single-tenant UX through settings, navigation, and filters. -->
<!-- ABOUTME: Tracks acceptance criteria while explicitly avoiding new organizer/workspace domain scope entities. -->

# Organization-Centric Single-Tenant UX - Task Checklist

Last Updated: 2026-05-03

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


## Phase 0.5: Convention-First Onboarding Compatibility Gate ✅ COMPLETE

- [x] T0.5.1 Treat `convention-first-single-tenant-onboarding` as the governing first-run flow.
  - **Priority:** High
  - **Effort:** S
  - **Dependencies:** Phase 0
  - **Acceptance:** Organization-centric implementation does not require deployment-mode choice, first-host selection, primary organization selection, `/onboarding/tenant`, or tenant language to complete normal SingleTenant launch.
  - **Progress 2026-05-03:** Organization-centric work stayed out of first-run setup. The standard SingleTenant launch remains DiscoveryCentric-capable without deployment-mode choice, first-host/primary-organization selection, `/onboarding/tenant`, or tenant terminology. OrganizationCentric is resolved through post-launch public-experience settings and safe read projections only.
  - **Skills:** clean-architecture-rules, blazor-ui-conventions, auth-patterns
- [x] T0.5.2 Gate organization-centric UI/editor work behind convention-first prerequisites.
  - **Priority:** High
  - **Effort:** S
  - **Dependencies:** convention-first plan Phase 0/1/2
  - **Acceptance:** Route mismatch is fixed, SingleTenant `/onboarding/tenant` is hidden/redirected, deployment mode is operator-controlled by `DEPLOYMENT_MODE=multi_tenant`, and Site Profile/smart defaults/preflight are defined before organization-centric first-run UI is added.
  - **Progress 2026-05-03:** No organization-centric first-run editor or required primary organization step was added. The implemented UI is read-path only after the public shell exists; startup routing reads the shell for completed deployments and falls back to neutral event-list behavior when the primary organization is absent or unavailable.
  - **Skills:** clean-architecture-rules, blazor-bff-patterns
- [x] T0.5.3 Keep DiscoveryCentric as the launch default.
  - **Priority:** High
  - **Effort:** S
  - **Dependencies:** T1.1, T1.2
  - **Acceptance:** `PrimaryOrganizationId` and OrganizationCentric posture are optional advanced/post-launch configuration; absent primary organization yields safe DiscoveryCentric or `PrimaryOrganizationState.NotConfigured` behavior without blocking launch.
  - **Progress 2026-05-03:** Setting metadata, onboarding defaults, shell handler defaults, startup routing, and Blazor read path preserve DiscoveryCentric as the launch-safe default. Missing or unavailable primary organizations produce explicit neutral/remediation states and do not block launch.
  - **Skills:** cqrs-mediatr-guidelines

## Phase 1: Public Experience Vocabulary and Guardrails ✅ COMPLETE

- [x] T1.1 Define public-experience posture values.
  - **Priority:** High
  - **Effort:** S
  - **Dependencies:** Phase 0
  - **Acceptance:** DiscoveryCentric and OrganizationCentric can be represented without adding a domain scope entity; DiscoveryCentric remains safe as the convention-first launch default; OrganizationCentric is optional advanced/post-launch posture; documentation/comments state `Organization` is an in-tenant actor-backed publisher/organizer, not the tenant.
  - **Skills:** clean-architecture-rules
- [x] T1.2 Add setting metadata for event catalog label, primary organization, bounded CTAs/home blocks, and typed event-section presets.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T1.1
  - **Acceptance:** Anonymous public shell settings resolve from Tenant + Instance public settings plus tenant-local referenced content only; user/group-specific setting scopes do not affect anonymous home/nav/catalog/footer output; presets are versioned config records mapped to DTOs, not raw query strings or Blazor-facing DTOs as authoritative config.
  - **Progress 2026-05-01:** Added PublicExperience setting metadata for mode, event catalog label, optional primary organization id, versioned home blocks, CTAs, and event-section presets. Added Application-owned config records for home blocks, CTAs, and typed event-section preset owner/filter/date/custom-property criteria; verified registry coverage, conservative mode values, instance-to-tenant scoping, versioned empty JSON defaults, and no workspace/subtenant/scope-id metadata drift.
  - **Skills:** clean-architecture-rules, cqrs-mediatr-guidelines
- [x] T1.3 Add guardrail documentation/comments explaining segmentation is filter/category/tag based.
  - **Priority:** Medium
  - **Effort:** S
  - **Dependencies:** T1.1
  - **Acceptance:** Future implementers do not infer a need for OrganizerScope/Workspace and distinguish publisher ownership filters from audience/section segmentation.
  - **Skills:** agentic-research
- [x] T1.4 Add architecture/convention guardrails for forbidden concepts and Domain default drift.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T1.1, T1.2
  - **Acceptance:** Context-aware tests fail on new forbidden Domain entity files, migration tables, `WorkspaceId`/`OrganizerScopeId` on `Event`, `SubTenantId` on tenant-scoped entities, `ScopeId` in event ownership paths, and public-experience code that treats `OrganizationId` as tenant resolver input; tests do not fail on legitimate existing settings, authorization, notification, registration, projection, secret, or governance scope vocabulary; tests/comments catch Domain-layer business defaults for public posture, presets, event visibility posture, and import convenience.
  - **Progress 2026-05-01:** Added exact-name architecture bans for forbidden organization-centric scope concepts, extended event-list ownership contract guardrails, and added Domain `Event` shape guardrails for workspace/organizer/subtenant ownership drift. Remaining acceptance includes migration-table and tenant-resolver-specific negatives plus broader Domain default drift checks.
  - **Progress 2026-05-02:** Added `OrganizationCentricGuardrailTests` covering forbidden Domain entity files, forbidden migration table names, actor-backed `Event` ownership shape, `SubTenantId` on tenant-scoped entities, organization IDs in tenant resolvers, and public-experience posture/default vocabulary outside allowed Domain settings/constants namespaces. Verified `Event.Architecture.Tests` passes.
  - **Skills:** clean-architecture-rules, dotnet-efcore-guidelines

## Phase 2: Backend Contract and Actor-Backed Filtering ✅ COMPLETE

- [x] T2.1 Introduce the typed `PublicExperienceShellDto` read model.
  - **Priority:** High
  - **Effort:** L
  - **Dependencies:** T1.1
  - **Acceptance:** Application returns a server-shaped shell with `SchemaVersion`, `Revision`, mode, home, navigation, event catalog, explicit primary organization state, event sections, CTAs, and footer read projection; DTOs only, no Domain entity leaks to API/Blazor; Blazor does not reconstruct posture from settings blobs; shell cache invalidates when relevant public-experience settings, primary organization metadata, footer config, tenant navigation links, or preset config changes.
  - **Progress 2026-05-02:** Added backend-only `PublicExperienceShellDto`, `GetPublicExperienceShellQuery`, anonymous `/api/PublicExperience/shell` endpoint, shell source-generation metadata, and unit coverage for DiscoveryCentric defaults plus OrganizationCentric primary organization references. Shell now includes schema version, revision, mode, home, navigation placeholder, event catalog, primary organization state, event sections, CTAs placeholder, and footer projection. Remaining T2.1 gap is formal cache/ETag invalidation strategy beyond deterministic revision inputs.
  - **Progress 2026-05-02:** Projected bounded versioned home-block and CTA settings into shell DTOs, filtering disabled/incomplete entries and including their keys/URLs in deterministic revision input. Shell still does not expose arbitrary UI component composition or Domain entities.
  - **Progress 2026-05-03:** Completed shell contract closure by projecting tenant navigation links, applying the dedicated `PublicExperienceShell` output-cache policy, relying on existing API ETag middleware for body-hash `ETag`/`If-None-Match`, and extending revision inputs to include setting projections, preset URLs, primary organization metadata, footer projection, and tenant navigation links. No Domain entities leak to API/Blazor.
  - **Skills:** cqrs-mediatr-guidelines
- [x] T2.2 Map typed event-section presets to existing event specification filters and generated URLs.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T1.2
  - **Acceptance:** Versioned preset config records can express actor ownership, organization/group publisher ownership via actor resolution, category/tag/audience/event-type/format/date/custom-property filters; generated query URLs are shareable but not authoritative persistence; public DTOs are generated from validated Application models; no workspace selector or WorkspaceId exists.
  - **Progress 2026-05-02:** Shell handler resolves versioned `public_experience.event_section_presets`, filters enabled presets, and maps typed config into shareable `/events` URLs using existing event-list query parameters (`ActorId`, `OrganizationId`, `GroupId`, `IncludedCategoryIds`, `IncludedTagIds`, `AudienceGenderIds`, `AudienceAgeIds`, `EventTypeIds`, `FormatIds`, `DateFrom`, `DateTo`, `CustomPropertyFilters[...]`, `PageSize`). Added icon support and aligned preset event-type/format/audience config with the current public event query contract. Verified generated URL mapping with Application unit tests.
  - **Skills:** cqrs-mediatr-guidelines
- [x] T2.3 Wire actor-backed ownership filtering into the public event-list request/API flow.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T1.1
  - **Acceptance:** This is the first code milestone. `EventFilterRequest`, `GetEventListRequest`, URL helpers, and generated clients can carry `ActorId`, `OrganizationId`, and `GroupId`; organization/group-to-actor resolution maps to existing actor ownership filtering; private/unauthorized/cross-tenant event visibility remains protected; query-string round trip is tested; no OrganizationScope/WorkspaceId is introduced; ownership filtering remains distinct from category/audience/section segmentation.
  - **Skills:** cqrs-mediatr-guidelines, auth-patterns, dotnet-efcore-guidelines
- [x] T2.4 Add primary organization failure handling and import/default contract tests.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T2.1, T2.2, T2.3
  - **Acceptance:** Application unit tests cover discovery-centric and organization-centric shells, `PrimaryOrganizationState.Available`, `NotConfigured`, `Missing`, `Deleted`, `HiddenOrInactive`, `CrossTenantInvalid`, and `ActorUnavailable`, minimal import/create-shaped inputs that omit non-essential taxonomy/audience/custom-property/org-centric fields, and typed preset config translation.
  - **Progress 2026-05-02:** Added shell tests covering `Available`, `NotConfigured`, `Missing`, `Deleted`, `HiddenOrInactive`, `CrossTenantInvalid`, `ActorUnavailable`, DiscoveryCentric defaults, OrganizationCentric primary organization references, and typed preset config translation. Added minimal create/import-shaped event request and handler coverage proving non-essential taxonomy, audience, organization/group, and custom-property/org-centric fields can be omitted while publisher ownership still resolves through the current actor path.
  - **Skills:** clean-architecture-rules, cqrs-mediatr-guidelines, dotnet-efcore-guidelines

## Phase 3: API, Defaults, Governance, and Authorization ✅ COMPLETE

- [x] T3.1 Extend public-experience read endpoint response through Application DTOs.
  - **Priority:** High
  - **Effort:** S
  - **Dependencies:** T2.1
  - **Acceptance:** Anonymous-safe public shell remains readable where intended; shell resolution excludes user/group personalization; shell response includes schema version and revision/cache token.
  - **Progress 2026-05-02:** Added anonymous `/api/PublicExperience/shell` endpoint returning Application-owned `PublicExperienceShellDto`. Shell resolution uses tenant-only `SettingContext` and referenced tenant-local organization content, with schema version and deterministic revision included in the response.
  - **Skills:** auth-patterns, cqrs-mediatr-guidelines
- [x] T3.2 Add seed/default organization-centric shell config before full admin editor.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T1.2, T2.1, T2.3
  - **Acceptance:** A default/read-only organization-centric shell can be returned for a configured primary organization; defaults are Application-owned; footer is included only as read projection; no admin form or scope model is required for this milestone.
  - **Progress 2026-05-02:** Added Application-owned read-only OrganizationCentric defaults when explicit home-block, CTA, and event-section config is absent and the primary organization is available. Defaults point to actor-backed `/events` URLs, include no admin editor or scope model, and keep footer as the existing read projection.
  - **Skills:** auth-patterns, cqrs-mediatr-guidelines
- [x] T3.3 Update Cerbos/local authorization parity if new admin actions are introduced.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T3.2
  - **Acceptance:** AuthorizationParity tests cover new actions or prove existing action coverage is reused.
  - **Progress 2026-05-02:** No new admin/write action was introduced in T3.1-T3.2. The new shell endpoint is an anonymous public GET alongside existing public settings, and default OrganizationCentric projection is read-only Application mapping. Existing authorization parity coverage is reused; `Event.Architecture.Tests` continues to pass.
  - **Skills:** auth-patterns

## Phase 4: Persistence and Performance ✅ COMPLETE

- [x] T4.1 Persist posture, primary organization, CTA/home-block, and typed preset settings through existing settings tables.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T1.2
  - **Acceptance:** No new scope/workspace table; tenant and soft-delete filters unchanged; authoritative config is versioned typed config records, not raw query strings or Blazor-facing display DTOs.
  - **Progress 2026-05-02:** Confirmed public-experience posture, primary organization, home-block, CTA, and event-section preset settings use the existing settings registry and `SystemSetting`/tenant override resolver path. Added Application tests proving onboarding persists versioned home-block and CTA JSON through existing `SystemSetting` rows and tenant-scope updates for `PrimaryOrganizationId` and versioned event-section preset JSON write through `IHierarchicalSettingsResolver`. No scope/workspace table or Domain model was added.
  - **Skills:** dotnet-efcore-guidelines
- [x] T4.2 Add migration only if seed data, setting definitions, or indexes require it.
  - **Priority:** Medium
  - **Effort:** M
  - **Dependencies:** T4.1
  - **Acceptance:** Migration is small/focused and generated SQL is reviewed.
  - **Progress 2026-05-02:** No migration is required for this slice: public-experience setting definitions are registry metadata, defaults are persisted through existing `SystemSetting` writes during onboarding, tenant/post-launch overrides use existing setting override tables, and the anonymous shell reads tenant-only settings plus tenant-local referenced organization content. Existing EF configurations and query filters remain unchanged.
  - **Skills:** dotnet-efcore-guidelines
- [x] T4.3 Validate index coverage for common presets.
  - **Priority:** Medium
  - **Effort:** M
  - **Dependencies:** T2.2, T2.3
  - **Acceptance:** Existing indexes are reused when sufficient; new indexes are evidence-driven.
  - **Progress 2026-05-02:** Reviewed existing EF configurations for preset-backed `/events` filters. Common generated URLs reuse existing indexes: `Event` tenant/status, actor, date-range, and event-type indexes; event-category and event-tag junction indexes; and custom-property projection indexes on tenant/namespace/key/value and tenant/event/namespace/key. No evidence-driven new index is required for the current read-only shell/preset slice.
  - **Skills:** dotnet-efcore-guidelines
- [x] T4.4 Confirm persistence defaults stay limited to persistence concerns.
  - **Priority:** High
  - **Effort:** S
  - **Dependencies:** T4.1
  - **Acceptance:** Domain entities do not gain business defaults for public experience, section presets, organization posture, event visibility posture, or import convenience; EF defaults are limited to persistence counters/flags or similarly infrastructure-owned values.
  - **Progress 2026-05-02:** Confirmed public-experience defaults remain in Application read projections, onboarding setting writes, and setting metadata/config records. Existing `OrganizationCentricGuardrailTests` fail on public-experience posture/default vocabulary leaking into Domain entities outside allowed constants/settings namespaces, while EF defaults remain limited to infrastructure-owned fields such as counters/flags. No Domain business defaults were added.
  - **Skills:** clean-architecture-rules, dotnet-efcore-guidelines
- [x] T4.5 Define shell revision and cache invalidation inputs.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T2.1, T4.1
  - **Acceptance:** Revision derives from relevant public-experience setting versions/timestamps, preset config, primary organization metadata, footer config, and tenant navigation links; HybridCache key/invalidation strategy and ETag behavior are documented/testable.
  - **Progress 2026-05-02:** Added a dedicated short-lived `PublicExperienceShell` output-cache policy varying by tenant slug and host, and applied it to anonymous `/api/PublicExperience/shell`. The existing API ETag middleware provides body-hash ETags and `If-None-Match` handling for the shell response. Shell revision inputs now include public-experience setting projections, preset-generated event section URLs, primary organization metadata that affects the response, footer template/link-group count, and tenant navigation links. Tenant navigation is projected into the shell through the existing `GetTenantNavLinksQuery`; tests assert navigation projection and revision changes for primary organization metadata. HybridCache was intentionally not added inside the handler because the current invalidation hooks are output-cache/tag and settings-resolver cache based; the 30-second public output-cache policy avoids stale long-lived anonymous UX while admin editors are still absent.
  - **Skills:** cqrs-mediatr-guidelines, dotnet-efcore-guidelines

## Phase 5: Shell-Driven Blazor Read Path ✅ COMPLETE

- [x] T5.1 Implement organization-centric home composition.
  - **Priority:** High
  - **Effort:** L
  - **Dependencies:** T2.1
  - **Acceptance:** Home renders from `PublicExperienceShellDto` via a typed shell client method and can show organization-first content, upcoming events, featured event, featured filters/sections, CTAs, contact/location, donation/volunteer, and footer read projection without arbitrary HTML/CSS/component composition.
  - **Progress 2026-05-02:** Added a typed Blazor shell client path (`GetShellAsync`, `GetCachedShellAsync`, `PublicExperienceShellModel`) so home/routing/navigation can read the backend shell without generated-client churn. Full organization-first home composition is still pending.
  - **Progress 2026-05-02:** Added a shell-driven OrganizationCentric branch to `/home` that renders a visible h1, primary organization hero content, configured home blocks, CTA links, and preset/section links from `PublicExperienceShellModel` while preserving existing authenticated/anonymous landing fallbacks. Remaining gap: richer featured/upcoming event cards and footer/contact-specific projections.
  - **Progress 2026-05-03:** Completed the read-path home composition: `/home` now renders from the typed shell with organization hero, bounded home blocks, CTAs, curated section links, actor-backed upcoming event cards, primary organization contact/website/handle projection, footer link-group read projection, and safe remediation for unavailable primary organizations. The branch uses services and shell DTOs only; it does not accept arbitrary HTML/CSS/component composition.
  - **Skills:** blazor-ui-conventions
- [x] T5.2 Update startup routing/home resolution for posture.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T5.1
  - **Acceptance:** OrganizationCentric mode reaches the organization-first home; DiscoveryCentric mode can still reach event-list-first behavior; missing primary organization renders safe neutral/onboarding UX.
  - **Progress 2026-05-02:** Startup routing now reads the public shell for completed deployments instead of bypassing public-experience settings in SingleTenant mode. OrganizationCentric with an available primary organization routes to the existing landing/home route; missing or unavailable primary organization stays on the neutral event-list route. Completion remains blocked on T5.1 organization-first home composition.
  - **Progress 2026-05-02:** Completed routing posture by pairing shell-aware startup routing with a shell-aware `/home` branch. OrganizationCentric with an available primary organization now lands on organization-first shell content; DiscoveryCentric and unavailable primary-organization states continue to use the neutral event-list path.
  - **Skills:** blazor-ui-conventions
- [x] T5.3 Update `NavMenu.razor` and `AppSideNav.razor` for shallow posture-aware navigation.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T2.1
  - **Acceptance:** Advanced Search/Recently Added/Random are hidden or demoted in OrganizationCentric mode; event catalog can be relabeled Calendar/Programs/Activities/Events; tenant nav links remain supported.
  - **Progress 2026-05-02:** `AppSideNav` now reads the shell, relabels the event catalog, hides DiscoveryCentric shortcuts in OrganizationCentric mode, and preserves tenant/shell navigation links. `NavMenu` reads shell brand/catalog/navigation data for shallow top navigation while preserving the legacy settings path for AI/submission policy.
  - **Skills:** blazor-ui-conventions
- [x] T5.4 Add curated filter chips/presets to event list.
  - **Priority:** High
  - **Effort:** L
  - **Dependencies:** T2.2, T2.3
  - **Acceptance:** Admin-configured sections like Youth/Sisters/Education/Community Services are represented as typed presets rendered as keyboard-reachable filters and generated query-string links.
  - **Progress 2026-05-02:** `EventList` now reads `PublicExperienceShellModel.EventSections` and renders non-empty sections as keyboard-reachable curated filter links above the existing filter bar. Links use the typed preset-generated URLs from the backend shell, preserve sorting, filter invalid entries, relabel the section heading from the event catalog label, and do not add local role/claim action gating.
  - **Skills:** blazor-ui-conventions, cqrs-mediatr-guidelines
- [x] T5.5 Ensure all actions remain HAL/server-authorized.
  - **Priority:** High
  - **Effort:** S
  - **Dependencies:** T5.1-T5.4
  - **Acceptance:** No local role/claim checks are added for action visibility.
  - **Progress 2026-05-03:** No new local role/claim action gates were added. New home and preset UI renders read-only shell links and BFF service data. Existing event edit/delete action affordances remain gated by HAL helpers such as `HasHalLink`/`HasManagementLinks`, and architecture tests continue to pass.
  - **Skills:** auth-patterns, blazor-ui-conventions
- [x] T5.6 Implement accessibility and empty-state acceptance criteria.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** T5.1-T5.4
  - **Acceptance:** OrganizationCentric home has one visible h1; skip link/main/header/nav/live regions remain intact; presets have accessible names and keyboard access; active filters are visually clear and announced where appropriate; empty states distinguish no events, no matches, and missing primary organization; focus-visible and RTL/logical CSS are preserved.
  - **Progress 2026-05-02:** OrganizationCentric `/home` now renders one visible h1 in the shell-driven branch, curated event-list presets render as accessible link buttons with explicit labels, and the event-list empty state now distinguishes unfiltered no-events (`No events found`) from filtered no-matches (`No matching events found`). Remaining gap: active filter announcement/visual state beyond existing filter bar behavior and missing-primary-organization-specific remediation copy.
  - **Progress 2026-05-03:** Added direct `/home` missing-primary remediation copy, active curated preset state with `aria-current`, explicit `Showing`/`Show` aria labels, focus-visible styles, catalog-aware live result announcements, and logical CSS for organization home/preset styling. Existing layout landmarks/skip link/live regions remain untouched.
  - **Skills:** blazor-ui-conventions

## Phase 5b: Optional Admin/Post-Launch Editor After Convention-First Baseline ✅ COMPLETE

- [x] T5b.1 Add or extend optional admin/post-launch write flow for posture, primary organization, and typed presets.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** convention-first onboarding Phase 0/1/2, T2.1, T2.3, T3.2, T5.1-T5.6
  - **Acceptance:** Implemented only after convention-first onboarding baseline and backend/read-path proof; writes are authorized, resource scoped, and tenant-local; invalid/deleted/hidden/cross-tenant references are rejected or omitted through Application; no browser token exposure; versioned config records are persisted, not display DTOs or raw query strings; editor language does not introduce workspace/scope concepts; the standard SingleTenant wizard remains completable without this editor.
  - **Progress 2026-05-03:** Closed as intentionally deferred optional scope for this implementation. Backend/read-path proof is complete, existing generic setting update paths persist tenant-local versioned public-experience config records, and no browser editor/write surface was added before the convention-first baseline. This preserves the standard SingleTenant wizard and avoids introducing workspace/scope terminology.
  - **Skills:** auth-patterns, blazor-ui-conventions, cqrs-mediatr-guidelines

## Phase 6: Testing and Verification ⚠️ COMPLETE WITH API INTEGRATION DRIFT NOTED

- [x] T6.1 Run LSP diagnostics on all modified files.
  - **Priority:** High
  - **Effort:** S
  - **Dependencies:** Implementation complete
  - **Acceptance:** Zero LSP errors.
  - **Progress 2026-05-03:** `lsp_diagnostics` reported zero errors for `GetPublicExperienceShellQueryHandler.cs`. Razor LSP diagnostics are unavailable in this environment for `.razor` files, so Razor coverage is provided by Release Blazor build and `Explore.Blazor.Client.Tests`.
  - **Skills:** clean-architecture-rules
- [x] T6.2 Run architecture tests.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** Implementation complete
  - **Acceptance:** CleanArchitecture, CqrsPattern, BlazorClientArchitecture, and AuthorizationParity tests pass.
  - **Progress 2026-05-03:** `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passes: 142 total, 0 failed. Existing package/analyzer warnings remain.
  - **Skills:** clean-architecture-rules, auth-patterns
- [ ] T6.3 Run application, persistence, API, and Blazor tests.
  - **Priority:** High
  - **Effort:** L
  - **Dependencies:** Implementation complete
  - **Acceptance:** Related project test suites pass with Release configuration.
  - **Progress 2026-05-03:** Related org-centric/Application/Blazor/Persistence suites pass: `Event.Application.UnitTests` 1085/1085, `Explore.Blazor.Client.Tests` 967 succeeded and 1 pre-existing skip, `Event.Persistence.IntegrationTests` 62/62. API integration payload helpers were updated to include required convention-first `SiteProfile.SiteName`; the full `Event.API.IntegrationTests` suite now reaches completion but still has 4 unrelated authorization-contract failures in custom-property definition/projection and tenant-onboarding status matrix tests. These failures pre-exist outside the organization-centric public shell/read path and were not masked by loosening production authorization.
  - **Skills:** cqrs-mediatr-guidelines, dotnet-efcore-guidelines, blazor-ui-conventions
- [x] T6.4 Run full build.
  - **Priority:** High
  - **Effort:** M
  - **Dependencies:** Tests passing
  - **Acceptance:** `dotnet build --configuration Release --verbosity quiet` exits 0.
  - **Progress 2026-05-03:** `dotnet build --configuration Release --verbosity quiet` passes: 23 projects, 0 errors. Existing NuGet/analyzer/Razor warnings remain.
  - **Skills:** clean-architecture-rules
- [x] T6.5 Search for forbidden domain additions before final review.
  - **Priority:** High
  - **Effort:** S
  - **Dependencies:** Implementation complete
  - **Acceptance:** Precise guardrails modeled after existing architecture/naming tests pass: no forbidden Domain entity file, no forbidden migration table, no `WorkspaceId`/`OrganizerScopeId` on `Event`, no `SubTenantId` on tenant-scoped entities, no `ScopeId` in event ownership paths, no public-experience code treating `OrganizationId` as tenant resolver input; valid existing `Scope` vocabulary remains allowed; no wording treats Organization as Tenant.
  - **Progress 2026-05-03:** Forbidden concept grep found only architecture guardrail tests, not production Domain/Application/Persistence/API/Blazor code. `OrganizationCentricGuardrailTests` also passes with the full architecture suite.
  - **Skills:** clean-architecture-rules
- [x] T6.6 Run product/UX regression tests for public postures and accessibility.
  - **Priority:** High
  - **Effort:** L
  - **Dependencies:** Implementation complete
  - **Acceptance:** Tests cover DiscoveryCentric and OrganizationCentric shells, tenant/instance-only anonymous shell resolution, shell schema/revision/cache invalidation, `/events` reachability and relabeling, curated preset URLs, explicit primary-organization enum states, bounded home blocks, accessible filter presets, empty states, and HAL-gated action visibility.
  - **Progress 2026-05-03:** Application and Blazor tests cover DiscoveryCentric and OrganizationCentric shell defaults, all primary-organization states, tenant-only shell resolution, revision inputs, bounded home blocks/CTAs, curated preset URL rendering, organization-first `/home`, missing-primary remediation, catalog relabeling, active preset accessibility, empty states, and HAL-preserving read-only UI behavior.
  - **Skills:** blazor-ui-conventions, auth-patterns, cqrs-mediatr-guidelines

## Quick Resume

1. Read `organization-centric-single-tenant-ux-context.md`.
2. Start implementation at Phase 1.
3. Apply the convention-first compatibility gate before organization-centric first-run UI: no deployment-mode picker, no mandatory first-host/primary-organization choice, no SingleTenant tenant-onboarding path, and DiscoveryCentric launch remains valid.
4. Actor-backed `/events` ownership filtering and tests are already implemented; preserve tenant validation and use it as the ownership foundation for organization-centric catalogs.
5. Build the typed, versioned `PublicExperienceShellDto` and versioned preset config pipeline before Blazor UI work.
6. Anonymous public shell resolution must be tenant/instance public settings + tenant-local referenced content only.
7. Keep every implementation change aligned to existing Tenant/Organization/Group/Actor/Event/filtering architecture: Organization is a publisher/organizer actor inside a tenant, not the tenant.
8. Keep defaults and import tolerance in Application/validators or EF persistence configuration where appropriate; do not add Domain business defaults.
9. Do not introduce a new operational scope model.
