<!-- ABOUTME: Resume context for organization-centric single-tenant UX planning and future implementation sessions. -->
<!-- ABOUTME: Captures decisions, researched evidence, verified files, constraints, and next implementation steps. -->

# Organization-Centric Single-Tenant UX - Context

Last Updated: 2026-05-02

## SESSION PROGRESS (2026-05-02)

### ✅ COMPLETED

- Updated this dev-docs set for compatibility with `dev/active/convention-first-single-tenant-onboarding/`: OrganizationCentric is now optional advanced/post-launch public-experience posture, not a required first-run decision; DiscoveryCentric remains the frictionless launch default; normal SingleTenant onboarding must not ask for deployment mode, first host, primary organization, or tenant concepts.
- Confirmed user direction: do **not** add OrganizerScope, BusinessScope, Workspace, SubTenant, or equivalent domain model.
- Confirmed desired approach: use existing model plus correct filtering, navigation, categories, tags, groups, actors, organization pages, settings, and custom-property projections.
- Read `.claude/commands/dev-docs.md` requirements and created this three-file dev-docs set.
- Loaded relevant skills: `agentic-research`, `clean-architecture-rules`, `blazor-ui-conventions`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`.
- Used Tavily MCP for industry research on self-hosted/community platform UX, configurable navigation, enterprise SaaS navigation, and filter UX.
- Used Context7 MCP for ASP.NET Core Blazor, MudBlazor, and EF Core documentation.
- Verified relevant repository docs and files before including them in the plan.
- Drafted implementation direction that preserves Clean Architecture and avoids new domain scope entities.
- Incorporated filtering-stack finding: actor filtering exists in Application, but public event-list request/API wiring needs to expose actor-backed organization/group catalog filtering.
- Collected `bg_f9cf047a` filtering-stack result and incorporated its verified gap/task updates.
- Checked `bg_ab7536bf`; it failed with `UnknownError` and produced no usable findings beyond the original prompt. Direct repo verification already covered the required single-tenant UX paths for this planning deliverable.
- Incorporated CTO review feedback into the plan, task checklist, and context after approval of the architecture direction with a tighter implementation contract.
- Corrected the terminology contract: `Organization` is an in-tenant event organizer/publisher backed by `Actor`, not the tenant; a tenant can contain multiple organizations even in single-tenant deployments.
- Added the Application-owned `PublicExperienceShellDto` concept plus typed `PublicEventSectionPresetDto` requirements so Blazor receives one server-shaped public shell rather than reconstructing UX from raw settings blobs.
- Added primary organization failure behavior, bounded customization blocks, accessibility acceptance criteria, forbidden-concept guardrails, import/default rules, and an updated 1.5-2.5 focused engineering week estimate.
- Incorporated the follow-up CTO implementation-readiness amendments: anonymous shell resolution scope, shell schema/revision, cache invalidation inputs, precise guardrail strategy, explicit primary organization state enum, footer read-projection ownership, config-record-versus-display-DTO separation, and phased delivery order.
- Verified current repo seams for the amendment areas: public-experience query/controller/service, footer CQRS projection and governance, TenantNav output caching, ETag middleware, HybridCache/cache-tag conventions, actor-backed `EventFilter.Actor(Guid)`, and legitimate non-event `Scope`/`ScopeId` usages that guardrails must not ban globally.

### 🟡 IN PROGRESS

- None for planning. Implementation has not started.

### ⚠️ BLOCKERS

- No implementation blocker for the plan itself.
- Before coding, optionally retry a focused single-tenant UX map if fresh implementation context is desired, but the plan already includes verified route/nav/home/onboarding/public-experience paths.

## Key Decisions

### Decision 1 - Tenant remains governance boundary

`Tenant` continues to mean governance, policy, module enablement, deployment isolation, and tenant-safe persistence. SingleTenant mode changes resolution and UX posture, not the Domain model.

### Decision 2 - No OrganizerScope/Workspace domain model

The product must not introduce a new internal scope layer for use cases like Youth Committee, Sisters Program, Education, or Community Services. Those examples are better represented through categories, tags, groups, actors, audience fields, and curated filter presets.

### Decision 3 - Organization-centric mode is UX/configuration, not tenancy

Self-hosted single-organization deployments should feel like an owned organization site with events/programs. That should be implemented through public-experience posture settings, routing, nav labels, organization-first home composition, and curated filters.

### Decision 4 - Keep `/events`, but demote/relabel it

Do not delete the event list. It remains useful as a calendar/catalog/search surface, but organization-centric mode can label it as Calendar, Programs, Activities, or Events and remove marketplace-style prominence.

### Decision 5 - Filters are the internal segmentation mechanism

Youth, Sisters, Education, Community Services, and similar groupings should be first-class user-facing filters/sections, not separate operational workspaces.

### Decision 6 - Organization is an actor-backed publisher, not the tenant

`Organization` remains an entity inside a tenant. It can publish/own events through `Actor` and can be emphasized as the public face of a self-hosted site, but it must not become a tenant proxy, hidden sub-tenant, workspace, or operational scope boundary. Event ownership/publisher filtering is distinct from audience, category, tag, custom-property, and section segmentation.

### Decision 7 - Public UX uses a typed Application shell

The public UI should consume a typed Application read model, conceptually `PublicExperienceShellDto`, that composes mode, home, navigation, event catalog, typed event-section presets, primary organization state, CTAs, and footer configuration. Settings may remain the storage mechanism, but Blazor should not infer public posture by combining unrelated settings blobs or persisted query-string fragments.

### Decision 8 - Typed presets replace authoritative raw query strings

Curated sections such as Youth, Sisters, Education, and Community Services should be represented as typed `PublicEventSectionPresetDto` values. Query-string URLs can be generated from presets for sharing/navigation, but raw query strings must not be the authoritative persisted configuration.

### Decision 9 - Defaults and import tolerance belong outside Domain entities

Domain entities must not gain business defaults for public experience, event visibility posture, organization posture, presets, or import convenience. Interactive create flows can enforce richer requirements in Application DTO validators/handlers, while import-shaped flows must tolerate common-denominator event data and enrich defaults in Application or persistence configuration where appropriate.

### Decision 10 - Primary organization failure is an Application concern

OrganizationCentric mode may emphasize a configured primary organization, but missing, deleted, hidden, suspended, or cross-tenant primary organization references must be resolved in Application to safe neutral/onboarding states with no cross-tenant leakage. Multiple organizations inside a tenant remain valid.

### Decision 11 - Anonymous public shell is tenant/instance scoped only

The anonymous `PublicExperienceShellDto` used for public and SEO surfaces resolves from Tenant + Instance public settings and tenant-local referenced content only. User-, group-, and organization-specific setting scopes must not personalize anonymous home, navigation, catalog, or footer output. `PrimaryOrganizationId` is referenced content inside the tenant, not a setting-scope override or tenant resolver input. Personalized member dashboards or role-aware shells are future work with separate contracts.

### Decision 12 - Shell versioning and revision are first-class contract fields

`PublicExperienceShellDto` must include `SchemaVersion` and `Revision`. `SchemaVersion` versions the public read-model contract; `Revision` is the cache/revalidation token that changes when relevant public-experience settings, event-section preset config, primary organization metadata, footer config, or tenant navigation links change. Shell caching should align with existing HybridCache, cache tags, output-cache, and ETag patterns rather than inventing a parallel cache model.

### Decision 13 - Persist versioned config records, not display DTOs

Settings storage should persist versioned config records such as `PublicEventSectionPresetConfigV1`, then map `ConfigV1 -> validated Application model -> PublicEventSectionPresetDto -> generated URL`. Blazor/API display DTOs are read outputs and must not become the persisted configuration format.

### Decision 14 - Footer stays owned by the footer subsystem

The shell may include `FooterConfigDto` as a read-only projection for anonymous rendering. Footer templates, link groups, social links, governance locks, validation, and writes remain owned by the existing footer subsystem and its CQRS endpoints.

### Decision 15 - Backend proof precedes admin editing

The first code milestone is actor-backed `/events` filtering: `EventFilterRequest`/`GetEventListRequest` carry `ActorId`, `OrganizationId`, and `GroupId`; Application resolves organization/group to `Actor`; `EventFilter.Actor(Guid)` applies ownership filtering; private/unauthorized/cross-tenant events remain hidden; URL round trips work; no workspace/scope model appears. Admin/onboarding editors come only after backend contract, filtering, shell read path, preset rendering, and the convention-first onboarding baseline are proven.

### Decision 16 - Convention-first onboarding owns the first-run path

The standard SingleTenant launch path is owned by `dev/active/convention-first-single-tenant-onboarding/`: Setup Secret -> Admin Auth -> Site Profile -> Smart Defaults -> Preflight -> Launch/Handoff. OrganizationCentric UX must not be required to complete that flow. It is an optional advanced/post-launch public-experience posture that can emphasize a configured primary organization after the site is already launchable. DiscoveryCentric remains the safe default when no primary organization is configured.

## Verified Repository References

### Canonical documentation

- `dev/active/convention-first-single-tenant-onboarding/convention-first-single-tenant-onboarding-plan.md` - governing first-run onboarding plan: SingleTenant by default, MultiTenant only by `DEPLOYMENT_MODE=multi_tenant`, Site Profile + smart defaults + preflight + launch handoff.
- `dev/active/convention-first-single-tenant-onboarding/convention-first-single-tenant-onboarding-tasks.md` - prerequisite tasks for route mismatch, tenant onboarding hiding/redirecting, deployment-mode env gate, Site Profile, preflight, and convention-first wizard.
- `CLAUDE.md` - agent/codebase contract, build command, HAL, repository, validation, and file-header rules.
- `docs/ARCHITECTURE.md` - Clean Architecture, CQRS, BFF, multi-tenancy, settings hierarchy, specs, auth/HAL, caching, outbox.
- `docs/DOMAIN.md` - Tenant, Organization, Group, Actor, Event, classification, settings, modules, custom properties.
- `docs/SECURITY.md` - security and authorization reference required by planning command.
- `docs/CONFIGURATION.md` - static/runtime/governance settings, deployment mode, tenant settings.
- `docs/GOVERNANCE.md`, `docs/OPERATIONS.md`, `docs/TROUBLESHOOTING.md`, `docs/PROJECT.md` - broader operating references.
- `dev/active/README.md` - required three-file dev-docs pattern.

### Domain and persistence

- `Explore.Domain/Enums/DeploymentMode.cs` - `SingleTenant` and `MultiTenant` deployment posture.
- `Explore.Domain/Tenant.cs` - tenant aggregate.
- `Explore.Domain/TenantNavigationLink.cs` - tenant-managed navigation links.
- `Explore.Domain/Enums/RoleScopeEnum.cs` - Platform, Tenant, Organization, Group role scopes.
- `Explore.Domain/Actor.cs` - actor identity for User/Organization/Group/Bot publishers.
- `Explore.Domain/Event.cs` - event ownership includes `ActorId` and `TenantId`.
- `Explore.Domain/Organization.cs` - tenant-scoped organization entity, optionally linked to an actor; not a tenant proxy.
- `Explore.Domain/Settings/SettingScope.cs` - Instance, Tenant, Organization, Group, User settings scopes.
- `Explore.Domain/ConfigurationChangeLog.cs`, `Explore.Domain/Secrets/SecretBinding.cs`, `Explore.Domain/CustomPropertyProjectionDirtyScope.cs`, `Explore.Domain/RegistrationScope.cs`, `Explore.Domain/EventRegistrationIntent.cs`, `Explore.Domain/Notification.cs`, `Explore.Domain/Role.cs`, `Explore.Domain/Permission.cs`, `Explore.Domain/Policies/PolicyChangeOutbox.cs` - legitimate non-event scope/scope-id concepts that guardrail tests must not reject globally.
- `Explore.Domain/Enums/ConfigurationScopeEnum.cs`, `Explore.Domain/Enums/SecretScope.cs`, `Explore.Domain/Enums/CustomPropertyProjectionScopeType.cs`, `Explore.Domain/Enums/RegistrationScopeEnum.cs` - existing scope taxonomies unrelated to the forbidden event ownership scope model.
- `Explore.Persistence/ExploreDbContext.QueryFilters.cs` - tenant and soft-delete filters.
- `Explore.Persistence/Repositories/EventRepository.cs` - event persistence boundary.
- `Explore.Persistence/Repositories/TenantNavigationLinkRepository.cs` - tenant nav persistence.
- `Explore.Persistence/Seed/LookupTableSeeder.cs` - seed/reference data entry point.

### Application and filtering

- `Explore.Application/Specifications/Events/EventQuerySpecification.cs` - composable event query specification.
- `Explore.Application/Specifications/Events/EventFilter.cs` - direct filters including actor/audience/status/date/visibility.
- `Explore.Application/Specifications/Events/EventSubqueryFilter.cs` - category/tag/location/language/registration/temporal filters.
- `Explore.Application/Specifications/Events/IslamicAspectFilter.cs` - Islamic aspect filters.
- `Explore.Application/Specifications/Events/TechAspectFilter.cs` - tech aspect filters.
- `Explore.Application/Specifications/Events/EventCustomPropertyProjectionFilter.cs` - projected custom-property filters.
- `Explore.Application/Settings/Groups/EventListSettingGroup.cs` - event-list settings group.
- `Explore.Application/Services/EventActorResolver.cs` - resolves organization/group/personal publishing actors for event creation.
- `Explore.Application/DTOs/Event/CreateEventDto.cs` and `CreateEventWithSessionsDto.cs` - interactive create DTOs where optional taxonomy fields remain nullable and rich validation can live outside Domain entities.
- `Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs` and `CreateEventWithSessionsCommandHandler.cs` - Application handlers apply runtime defaults rather than Domain constructors carrying business defaults.
- `Explore.Application/DTOs/Onboarding/PublicExperienceSettingsDto.cs` - public-experience settings DTO.
- `Explore.Application/Features/PublicExperience/Requests/Queries/GetPublicExperienceSettingsQuery.cs` - public-experience query.
- `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs` - public-experience handler.
- `Explore.Application/Features/Events/Requests/Queries/GetEventListRequest.cs` - event-list query request; needs actor-backed filter inputs for organization-centric catalogs.
- `Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs` - event-list query handler; should map actor-backed inputs to existing specification filters.
- `Explore.Application/Settings/Groups/FooterSettingGroup.cs` and `Explore.Domain/Settings/Definitions/FooterSettingDefinitions.cs` - footer config shape, public setting keys, scopes, and governance lock semantics.
- `Explore.Application/DTOs/Footer/FooterConfigDto.cs` and `Explore.Application/DTOs/Footer/FooterSettingsDto.cs` - footer public read projections, distinct from footer setting definitions and write commands.
- `Explore.Application/Features/Footer/Handlers/Queries/GetFooterConfigQueryHandler.cs` - assembles public footer read projection.
- `Explore.Application/Features/Footer/Handlers/Queries/GetFooterGovernanceSettingsQueryHandler.cs` and `Explore.Application/Features/Footer/Handlers/Commands/UpdateTenantFooterSettingsCommandHandler.cs` - footer governance/write ownership remains outside the public shell.
- `Explore.Application/Caching/CacheTags.cs` - shared cache-tag vocabulary for cache invalidation.
- `Explore.Application/Notifications/PolicyChangedCacheInvalidationHandler.cs` - existing versioned-cache-key invalidation pattern to mirror for shell revision/cache behavior.

### API and Blazor

- `Explore.API/Controllers/PublicExperienceController.cs` - public-experience API boundary.
- `Explore.API/Controllers/InstanceOnboardingController.cs` - instance onboarding API boundary.
- `Explore.API/Controllers/EventController.cs` - event API boundary.
- `Explore.API/Models/EventFilterRequest.cs` - public/API event-list filter model; currently lacks first-class organization/group/actor filter inputs.
- `Explore.API/Controllers/FooterController.cs` - footer public read endpoint plus protected footer write/governance endpoints.
- `Explore.API/Controllers/TenantController.cs` - public tenant navigation endpoint with output caching.
- `Explore.API/Extensions/CachingExtensions.cs` - output-cache policies and HybridCache registration, including TenantNav.
- `Explore.API/Middleware/ETagMiddleware.cs` - weak ETag generation and `If-None-Match` 304 handling.
- `Explore.Persistence/Repositories/FooterLinkGroupRepository.cs` - tenant-first, instance-default fallback for footer link groups.
- `Explore.Blazor.Client/Routes.razor` - route registration for `/events` and organization routes.
- `Explore.Blazor.Client/Pages/HomeStart.razor` - current public home composition.
- `Explore.Blazor.Client/Pages/Events/EventList.razor` - `/events` page.
- `Explore.Blazor.Client/Pages/Events/EventList.razor.cs` - event-list behavior.
- `Explore.Blazor.Client/Pages/Events/Components/EventFilterBar.razor` - filter UI.
- `Explore.Blazor.Client/Pages/Events/Components/EventListCustomizationDrawer.razor` - event-list customization.
- `Explore.Blazor.Client/Pages/Organizations/OrganizationProfile.razor` - public organization profile.
- `Explore.Blazor.Client/Pages/Organizations/OrganizationDetails.razor` - authenticated organization operations.
- `Explore.Blazor.Client/Pages/Organizations/MyOrganizations.razor` - organization listing.
- `Explore.Blazor.Client/Layout/NavMenu.razor` - top navigation.
- `Explore.Blazor.Client/Components/Shell/AppSideNav.razor` - side navigation/discovery links.
- `Explore.Blazor.Client/Services/PublicExperienceService.cs` - client public-experience service.
- `Explore.Blazor.Client/Services/StartupRoutingService.cs` - startup route decisions.
- `Explore.Blazor.Client/Services/TenantNavigationService.cs` and `TenantNavLinksState` - tenant navigation flow that can consume generated links from typed presets.
- `Explore.Blazor.Client/Services/RuntimeRenderPolicyService.cs` - render policy service.
- `Explore.Blazor.Client/Pages/Onboarding/InstanceOnboarding.razor` - instance onboarding UI.
- `Explore.Blazor.Client/Pages/Onboarding/TenantOnboarding.razor` - tenant onboarding UI.

## Research Evidence

### Tavily

- Navigation should be clear, simple, shallow, and avoid too many levels.
- Community/self-hosted products must prioritize ease of use and clean admin/member UX.
- Highly customizable platforms can create delayed launches and admin decision fatigue.
- Enterprise SaaS IA should use progressive disclosure, clear hierarchy, and avoid overwhelming categories.
- Filters are essential for complex SaaS data, but must be implemented and designed carefully.

### Context7

- Blazor supports component composition and mixed render modes, fitting a configurable home/catalog posture.
- MudBlazor provides supported navigation, layout, table/grid, filtering, and search components.
- EF Core guidance supports global query filters, efficient querying, indexes, and keyset pagination.

### CTO review and external examples

- CTO approved the no-new-scope architecture direction but required a tighter implementation contract before coding.
- External examples support typed composition over free-form UX blobs: block/shell/read-model patterns, typed filter view models, and options validation are safer than arbitrary query-string or layout-builder configuration.
- The revised estimate is approximately 1.5-2.5 focused engineering weeks: backend contract/settings/API 2-4 days, Blazor home/nav/event-list UX 4-7 days, tests/regression 2-4 days, docs/polish 1-2 days.
- Follow-up CTO verdict says the plan is architecturally implementation-ready after tightening public-shell scope, schema/revision, guardrails, primary organization state, footer ownership, config/display separation, and delivery order.
- External contract examples support the revised direction: version public read contracts, expose revision/ETag for public projections, keep anonymous public shells tenant/global rather than personalized, and model footer as a read projection derived from owned config/state.
- Relevant source examples: `dotnet/aspnet-api-versioning` endpoint version mapping; OpenBullet2 `Config -> ConfigInfoDto` mapping; RFC 7232 and ASP.NET Core ETag/cache guidance; OrchardCore shell host/table resolution; Redux.NET/NexusMods footer view-model projections.
- Internal repo evidence supports the same shape: `PublicExperienceSettingsDto`/handler/controller are current public shell seeds; `FooterConfigDto`/footer handlers already split read projection from writes/governance; `TenantController` and `CachingExtensions` provide TenantNav output-cache precedent; `ETagMiddleware` provides HTTP revalidation precedent.

### UX agent

- Use filter-first information architecture instead of deep nav/workspace hierarchy.
- Use categories/tags as primary structure.
- Keep top-level navigation small and explicit.
- Bound customization to prevent admin UX debt.

## Implementation Constraints

- This plan is subordinate to the convention-first onboarding plan for first-run UX. Do not make `OrganizationCentric`, `PrimaryOrganizationId`, first-host selection, or `/onboarding/tenant` required for normal SingleTenant setup.
- `DiscoveryCentric` is the default public-experience posture for fastest launch; OrganizationCentric is optional advanced/post-launch configuration.
- No new domain operational scope entity.
- Do not treat `Organization` as `Tenant`; avoid tenant-substitute wording for organizations.
- Do not add `OrganizerScope`, `BusinessScope`, `Workspace`, `SubTenant`, `OrganizationScope`, `TenantWorkspace`, or `ScopeId` to event ownership paths.
- No runtime disabling of tenant query filter.
- Domain must remain dependency-free.
- Domain entities must not carry business defaults for public posture, section presets, organization posture, event visibility posture, or import convenience.
- Imports/future import endpoints must tolerate minimal common-denominator fields and should not require optional taxonomy, audience, custom-property, or organization-centric presentation fields.
- Application handlers must not use `ExploreDbContext` directly.
- Repositories return entities, not DTOs or `IQueryable`.
- Validators are manually instantiated in handlers.
- UI action affordances come from HAL/server state, not role/claim checks in Razor.
- Blazor components must respect render-mode constraints and avoid `HttpContext` in interactive paths.
- Backward compatibility is not required, but architecture quality is required.
- `PublicExperienceShellDto` should be the authoritative public Application read model for home/nav/catalog/footer rendering.
- Typed presets should generate URLs; raw query strings are not authoritative stored config.
- Anonymous public shell resolution must use Tenant + Instance public settings plus tenant-local referenced content only; no user/group/organization setting cascade may personalize public/SEO home, nav, catalog, or footer output.
- `PublicExperienceShellDto` must include `SchemaVersion` and `Revision`; revision/cache invalidation must account for public-experience settings, primary organization metadata, footer config, tenant navigation links, and event-section preset config.
- Persist versioned config records such as `PublicEventSectionPresetConfigV1`, not Blazor/API display DTOs; map config records through validated Application models before emitting public DTOs and generated URLs.
- Footer in the shell is read-only projection for public rendering; footer write models, templates, governance locks, link groups, and validation stay in the footer subsystem.
- Primary organization output should use explicit states: `Available`, `NotConfigured`, `Missing`, `Deleted`, `HiddenOrInactive`, `CrossTenantInvalid`, and `ActorUnavailable`.
- Guardrail tests must be precise/path-aware. They should fail on forbidden entity files/tables, forbidden ownership IDs on `Event`, `SubTenantId` on tenant-scoped entities, `ScopeId` in event ownership paths, and public-experience code treating `OrganizationId` as a tenant resolver input; they must allow legitimate existing scope concepts in settings, auth, registration, notifications, secrets, policies, governance, and custom-property projections.
- OrganizationCentric customization is bounded to hero/intro, upcoming events, featured sections, featured event, CTA links, contact/location, donation/volunteer, and footer.
- Accessibility acceptance criteria include one visible h1, preserved skip/main/header/nav/live regions, keyboard-reachable presets with accessible names, announced/clear active filters, distinct empty states, focus-visible styles, and RTL/logical CSS preservation.

## Filtering Stack Finding

The current stack already supports rich filtering and actor-owned event retrieval, which validates the no-OrganizerScope direction:

- `EventFilter.Actor(Guid)` exists in `Explore.Application/Specifications/Events/EventFilter.cs`.
- Category, tag, location, date, format, madhab, audience gender/age, status, registration mode, language, Islamic aspect, tech aspect, and custom-property filters already exist.
- `OrganizationProfile`, `GroupProfile`, and `OrganizationDetails` use actor-backed event retrieval paths.
- Gap to implement: expose actor-backed ownership filtering through the public event-list request/API/Blazor URL state so an organization-centric catalog can be expressed with `/events` plus filters instead of a workspace selector.
- This actor-backed `/events` ownership filtering is the first backend milestone before curated event-list UI presets.
- Concrete first milestone: add `ActorId`, `OrganizationId`, and `GroupId` to `EventFilterRequest` and `GetEventListRequest`; resolve organization/group IDs to actors in Application; map to `EventFilter.Actor(Guid)`; preserve tenant, visibility, and authorization behavior; update query-string round trip and generated client URL helpers; add no workspace/scope model.

Mapping examples:

- Youth: audience age + category/tag + date/location + optional custom-property filters.
- Sisters: audience gender + Islamic gender mode + category/tag + date/location.
- Education: event type + language + format + category/tag + optional custom-property filters.
- Community Services: category/tag + location + date + public discoverability + optional custom-property filters.

## Quick Resume

1. Re-read `convention-first-single-tenant-onboarding-plan.md` first, then this plan and tasks file.
2. Treat convention-first onboarding Phase 0/1/2 as prerequisites for any organization-centric first-run UI work: route mismatch fixed, `/onboarding/tenant` hidden/redirected in SingleTenant, deployment mode operator-controlled, Site Profile/smart defaults/preflight defined.
3. Continue organization-centric work as optional advanced/post-launch public-experience posture: anonymous shell resolves from Tenant + Instance public settings only, includes `SchemaVersion`/`Revision`, has explicit primary organization state, and separates persisted config records from display DTOs.
4. T2.3 actor-backed `/events` filtering is already implemented/verified in prior work; keep it as the backend ownership foundation and do not regress tenant validation.
5. Build the shell-driven Blazor read path and preset-generated URLs after backend filtering and default/read-only shell behavior are proven.
6. Add admin/post-launch editors only after convention-first onboarding baseline, backend contract, actor-backed filtering, shell read path, and preset rendering are working; editors persist versioned config records, not DTOs/raw query strings and must not be required for initial launch.
7. Keep the non-goal search active during implementation: no OrganizerScope, Workspace, BusinessScope, SubTenant, OrganizationScope, TenantWorkspace, or ScopeId-based ownership additions. Guardrails must be path-aware and must not ban legitimate existing scope concepts globally.
8. Preserve Domain purity: no business defaults for public posture, presets, visibility posture, organization posture, or import convenience.
9. Build Blazor from the typed shell, generated preset URLs, bounded blocks, footer read projection, HAL/server-authorized affordances, and accessibility acceptance criteria.
10. Run architecture, Application, Persistence, API, Blazor, accessibility/product tests, cache/revision tests, and full build before marking implementation complete.
