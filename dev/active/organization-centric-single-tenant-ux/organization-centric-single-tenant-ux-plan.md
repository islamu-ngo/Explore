<!-- ABOUTME: Strategic implementation plan for organization-centric single-tenant UX without adding organizer/workspace domain scope. -->
<!-- ABOUTME: Uses existing tenant, organization, actor, category, tag, settings, and filtering architecture for simple self-hosted segmentation. -->

# Organization-Centric Single-Tenant UX - Implementation Plan

Last Updated: 2026-04-30

## Executive Summary

ISLAMU Event should support self-hosted deployments where one organization uses the platform entirely for its own events without making the product feel like a public marketplace or requiring an internal workspace model. The implementation direction is to keep `Tenant` as the governance and isolation boundary, keep `Organization`, `Group`, and `Actor` as the current in-tenant publishing model, and use typed public-experience contracts, curated navigation, organization-centric home routing, event filtering, category/tag taxonomy, audience fields, and custom-property projections for internal segmentation.

This plan explicitly rejects adding `OrganizerScope`, `BusinessScope`, `Workspace`, `SubTenant`, or equivalent domain entities. Youth, Sisters, Education, Community Services, and similar use cases should be represented through filters, categories, tags, groups, actors, typed event-section presets, navigation links, and optional custom properties because that keeps the user model learnable and avoids a certificate-level setup experience.

The CTO review approved the strategic direction with a stricter implementation contract. This revision therefore treats organization-centric mode as a bounded, typed, Application-owned public experience shell, not as a settings blob, mini-CMS, tenant proxy, or hidden scope layer.

## CTO Feedback Incorporation Summary

- Keep the no-Workspace/no-OrganizerScope decision unchanged.
- Clarify that `Organization` means an in-tenant event organizer/publisher represented through `Actor`, not a tenant substitute.
- Treat OrganizationCentric mode as “the public site emphasizes one configured primary organization,” while still allowing multiple organizations inside the tenant.
- Add a typed `PublicExperienceShellDto` read model so Blazor renders one server-shaped public shell instead of reconstructing UX posture from many independent settings.
- Add typed `PublicEventSectionPresetDto` filter presets and generate shareable query URLs from them; do not persist arbitrary query strings as the authoritative preset model.
- Make `PrimaryOrganizationId` selection, validation, and unavailable-state behavior explicit.
- Add an anonymous public-shell resolution policy: public SEO/home/nav/catalog output resolves from tenant + instance public settings and tenant-local referenced content only, not user/group/personalized setting scopes.
- Version the shell contract with `SchemaVersion` and `Revision` so cache keys, ETags, and invalidation can be deliberate rather than accidental.
- Split persisted versioned config records from Blazor-facing display DTOs; map config to validated Application models and then to public DTOs.
- Preserve the repo rule that Domain entities should not carry business defaults; defaults and create/import validation belong in Application handlers/validators or EF configuration where persistence defaults are truly needed.
- Promote accessibility from implicit design quality to explicit acceptance criteria and tests.
- Add precise guardrail tests against forbidden scope concepts and revise the estimate upward to an enterprise-grade implementation window.

## Current State Analysis

All file paths below were verified before inclusion.

### Existing governance and tenancy model

- `Explore.Domain/Enums/DeploymentMode.cs` already defines `SingleTenant` and `MultiTenant`. Single-tenant mode is a deployment posture, not a separate domain model.
- `Explore.Persistence/ExploreDbContext.QueryFilters.cs` enforces tenant isolation through EF Core global query filters. Runtime code must not disable the tenant filter.
- `docs/ARCHITECTURE.md` describes SingleTenant as resolving to a configured default tenant while MultiTenant resolves tenant context from host/header/subdomain.
- `docs/CONFIGURATION.md` documents runtime settings through static configuration, secret management, and governance database settings including `deployment.*`, `events.*`, `organizations.*`, `branding.*`, and `modules.*` keys.
- `docs/DOMAIN.md` confirms the current core model: `Tenant` governance, `Organization`/`Group`/`Actor` publishing identities, `Event` content, lookup classifications, module capabilities, and layered custom properties.

### Existing public experience and navigation surfaces

- `Explore.Blazor.Client/Pages/HomeStart.razor` currently embeds the event list as the public home experience.
- `Explore.Blazor.Client/Pages/Events/EventList.razor` and `Explore.Blazor.Client/Pages/Events/EventList.razor.cs` provide the primary `/events` discovery page.
- `Explore.Blazor.Client/Pages/Events/Components/EventFilterBar.razor` exposes event filtering and sorting affordances.
- `Explore.Blazor.Client/Pages/Events/Components/EventListCustomizationDrawer.razor` already supports event-list personalization controls.
- `Explore.Blazor.Client/Components/Shell/AppSideNav.razor` contains static discovery-oriented links such as Advanced Search, Recently Added, and Random.
- `Explore.Blazor.Client/Layout/NavMenu.razor` contains brand navigation, event search, dynamic tenant navigation links, Add Event, organization/group menus, and admin links.
- `Explore.Domain/TenantNavigationLink.cs` and `Explore.Persistence/Repositories/TenantNavigationLinkRepository.cs` already provide tenant-managed navigation links.
- `Explore.Blazor.Client/Services/PublicExperienceService.cs` and `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs` already centralize public experience settings.

### Existing organization surfaces

- `Explore.Blazor.Client/Pages/Organizations/OrganizationProfile.razor` provides the public organization profile surface.
- `Explore.Blazor.Client/Pages/Organizations/OrganizationDetails.razor` provides authenticated organization operations.
- `Explore.Blazor.Client/Pages/Organizations/MyOrganizations.razor` provides authenticated organization listing and management entry points.
- `Explore.Blazor.Client/Routes.razor` maps organization and event routes, including `/events` and organization profile/details routes.
- `Explore.Blazor.Client/Pages/Onboarding/InstanceOnboarding.razor` and `Explore.Blazor.Client/Pages/Onboarding/TenantOnboarding.razor` already collect deployment and public-experience preferences.

### Existing event ownership, defaults, and import-relevant contracts

- `Explore.Domain/Event.cs` owns `ActorId` and `TenantId`; event publishing ownership is actor-backed.
- `Explore.Domain/Actor.cs` can represent user, organization, group, or bot publishing identities.
- `Explore.Domain/Organization.cs` is tenant-scoped and optionally linked to an actor; it is not a tenant proxy.
- `Explore.Application/Services/EventActorResolver.cs` centralizes organization/group/personal publishing resolution.
- `Explore.Application/DTOs/Event/CreateEventDto.cs` and `Explore.Application/DTOs/Event/CreateEventWithSessionsDto.cs` already keep publisher context optional while create validators enforce the stricter interactive-create rules.
- `Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs` and `CreateEventWithSessionsCommandHandler.cs` apply runtime defaults and ownership resolution in Application.
- `Explore.Persistence/Configurations/Entities/EventConfiguration.cs` contains persistence defaults such as `TotalViews` and `IsUserReported`.
- No dedicated event import endpoint was found. Current write routes are `POST /api/Event` and `POST /api/Event/with-sessions`; future import work must remain tolerant of minimal common-denominator event data.

### Existing filtering and segmentation model

- `Explore.Application/Specifications/Events/EventQuerySpecification.cs` composes event query filters immutably.
- `Explore.Application/Specifications/Events/EventFilter.cs` supports direct filters including actor, event type, format, madhab, audience age, audience gender, status, visibility, date range, free/paid, registration requirement, and public discoverability.
- `Explore.Application/Specifications/Events/EventSubqueryFilter.cs` supports category, tag, location, language, registration mode, and temporal filters.
- `Explore.Application/Specifications/Events/IslamicAspectFilter.cs` and `Explore.Application/Specifications/Events/TechAspectFilter.cs` support module-specific filtering.
- `Explore.Application/Specifications/Events/EventCustomPropertyProjectionFilter.cs` supports exact match, option match, text search, existence, boolean, number range, and date range filters over projected custom properties.
- Existing indexes in `Explore.Persistence` cover tenant-scoped event access, actor/date combinations, event categories, and custom-property projections; additional index work must be evidence-driven, not speculative.
- Current gap: `EventFilter.Actor(Guid)` exists in Application, and organization/group/profile pages use actor-backed event retrieval, but the public event-list request/API surface does not yet expose `OrganizationId`, `GroupId`, or `ActorId` as first-class filter inputs.

### Existing design constraints

- `CLAUDE.md`, `docs/ARCHITECTURE.md`, and loaded skills require Clean Architecture boundaries: Domain has no external dependencies; Application owns CQRS/specifications/DTO mapping; Persistence returns entities; API/Blazor are composition roots.
- UI actions must be gated by HAL links, not role checks in Razor components.
- Blazor defaults to InteractiveAuto/interactive execution paths, so components must not rely on `HttpContext`.
- Repositories must not return DTOs or `IQueryable` to Application.
- Domain entities should not accumulate business defaults for public experience, event presets, visibility posture, or import convenience. Interactive create flows can enforce richer DTO validation; import flows must accept valid minimal event data and enrich/normalize in Application.

## Terminology and Boundary Contract

### Tenant

`Tenant` remains the isolation, governance, module, policy, and runtime-resolution boundary. Tenant identity is resolved centrally and enforced by API authorization plus EF Core query filters. Organization-centric mode must never use `OrganizationId` as a tenant identifier and must never disable tenant filters to simulate organization scoping.

### Organization

`Organization` means an entity inside a tenant that can publish/manage events through an `Actor`. It may represent a mosque, school, nonprofit, committee, charity branch, or other event organizer. In a single-tenant deployment, the whole public site may be dedicated to one organization, but that is still a public UX posture over tenant-isolated data, not a redefinition of tenancy.

### Actor

`Actor` is the publishing/ownership identity used for event ownership and event-list filtering. `ActorId`, `OrganizationId`, and `GroupId` filters are ownership filters: they answer “who publishes/manages this event?” They must not be overloaded to mean category, program, audience, or section membership.

### Section / preset

Sections such as Youth, Sisters, Education, Family Events, Fundraising, and Community Services are curated public event presets built from category, tag, audience, event type, format, custom-property, and optionally ownership filters. They are presentation/read-model concepts, not Domain aggregates.

### Anonymous public shell resolution

Anonymous `PublicExperienceShellDto` resolution for public SEO surfaces uses tenant and instance public settings plus tenant-local referenced content only. User, group, or organization-specific settings must not change anonymous public home, navigation, footer, or event catalog output. Primary organization data may be included as referenced tenant-local content, but the primary organization is not an override scope for shell resolution. Personalized shells or dashboards for authenticated users are separate future work.

### Config record versus display DTO

Persisted public-experience configuration should use explicit versioned config records, not Blazor-facing DTOs. The intended pipeline is: `ConfigV1` stored in settings -> validated Application model -> public DTO/read model -> generated URLs/rendering. Display DTOs can evolve for Blazor needs without becoming the storage contract.

### Import versus interactive create

Interactive create endpoints may require rich fields through DTO validation so user-created events are high quality. Import flows must be able to accept the common denominator from other event platforms: name/title, description where available, start date/time, optional end date/time, optional location, optional source reference, and optional organizer mapping. Missing non-essential taxonomy, module fields, categories, tags, audiences, custom properties, or visibility enhancements should not block import success.

## Research Summary

### Tavily findings

- Self-hosted and community platform guidance consistently emphasizes simple navigation, low learning curve, clean administration, and avoiding deep hierarchy.
- Enterprise SaaS navigation guidance recommends clear hierarchy, progressive disclosure, and avoiding overwhelming users with too many navigation categories.
- Filter UX research emphasizes that filters must be predictable, visible where needed, and designed with implementation support so they work reliably.

### Context7 findings

- ASP.NET Core Blazor supports reusable component composition and mixed render modes, which fits a configurable organization-centric home and event catalog without a new domain model.
- MudBlazor provides native navigation, data grid/table, filtering, search, and layout components; implementation should use existing MudBlazor and project wrapper patterns instead of building a custom UI framework.
- EF Core documentation reinforces global query filters, efficient querying, indexes, and keyset pagination. Filtering changes must preserve tenant filters and use indexed predicates where volume matters.

### Additional UX research agent findings

- NN/g guidance supports filter categories that are appropriate, predictable, prioritized, and jargon-free.
- Navigation should remain shallow and explicit because clever labels, redundant links, and too many levels increase cognitive strain.
- Discourse-style category/tag organization is a better fit for community segmentation than nested workspace abstractions.
- GitBook-style site sections support organization-owned information architecture without inventing separate operational tenants.

## Proposed Future State

### Product posture

Add a configurable public-experience posture with two initial modes:

1. **DiscoveryCentric**: current marketplace/directory posture for public multi-organizer or ISLAMU-hosted all-events deployments. `/events` remains primary.
2. **OrganizationCentric**: default posture for self-hosted organization-owned deployments. The public home emphasizes the configured primary organization, featured/upcoming events, configured sections, and simple calls to action. `/events` remains available but is framed as the organization's calendar/catalog, not a marketplace.

This can be implemented as an Application/configuration concept such as `PublicExperienceMode`, not a domain aggregate. The exact enum/string location should align with existing public-experience DTO and settings patterns.

### Typed public-experience shell

Blazor should consume one Application-level read model for public UX posture instead of independently reconstructing behavior from public-experience settings, tenant navigation links, event-list settings, onboarding state, route services, local component state, and query strings.

Conceptual contract:

```csharp
public sealed record PublicExperienceShellDto(
    int SchemaVersion,
    string Revision,
    PublicExperienceMode Mode,
    PublicHomeDto Home,
    PublicNavigationDto Navigation,
    EventCatalogDto EventCatalog,
    IReadOnlyList<PublicEventSectionPresetDto> EventSections,
    PrimaryOrganizationDto? PrimaryOrganization,
    IReadOnlyList<PublicCtaDto> CallsToAction,
    FooterConfigDto Footer);
```

This DTO can compose existing `PublicExperienceSettingsDto`, footer config, tenant navigation links, and event-list settings internally, but the public UI should receive one authoritative shape. That keeps rendering simple and makes the backend the source of truth for UX posture.

`SchemaVersion` is the public read-model contract version. `Revision` is an opaque cache token derived from relevant setting versions, updated timestamps, tenant navigation changes, footer config changes, preset config changes, and primary organization metadata that affects the shell. Public shell handlers should use existing HybridCache patterns where appropriate, and HTTP responses can rely on existing ETag behavior once the shell response body changes with the revision.

Footer inclusion in the shell is a read projection for anonymous public rendering only. Footer writes, templates, governance locks, link-group management, and footer-specific validation remain owned by the footer subsystem.

### Typed event-section/filter-preset contract

Filter presets must be typed and validated. Query strings remain an output format for shareable URLs, not the source-of-truth storage format.

Conceptual contract:

```csharp
public sealed record PublicEventSectionPresetConfigV1(
    string Key,
    LocalizedTextConfig Label,
    Guid? ActorId,
    Guid? OrganizationId,
    Guid? GroupId,
    IReadOnlyList<Guid> CategoryIds,
    IReadOnlyList<Guid> TagIds,
    int? AudienceGenderId,
    int? AudienceAgeId,
    int? EventTypeId,
    int? FormatId,
    bool? PubliclyDiscoverableOnly,
    IReadOnlyList<CustomPropertyFilterConfigV1> CustomPropertyFilters,
    string? Icon,
    int SortOrder);

public sealed record PublicEventSectionPresetDto(
    string Key,
    string Label,
    string? Description,
    Guid? ActorId,
    Guid? OrganizationId,
    Guid? GroupId,
    IReadOnlyList<Guid> CategoryIds,
    IReadOnlyList<Guid> TagIds,
    int? AudienceGenderId,
    int? AudienceAgeId,
    int? EventTypeId,
    int? FormatId,
    bool? PubliclyDiscoverableOnly,
    IReadOnlyList<CustomPropertyFilterDto> CustomPropertyFilters,
    string? Icon,
    int SortOrder);
```

Preset validation requirements:

- `Key` is stable, unique per tenant public shell, URL-safe, and not derived from mutable labels.
- `Label` is required and localized/display-safe.
- Ownership fields (`ActorId`, `OrganizationId`, `GroupId`) are publisher filters only.
- Category/tag/audience/custom-property filters represent user-facing segmentation.
- Invalid IDs, deleted records, hidden records, or cross-tenant references are rejected or omitted by the Application layer before reaching Blazor.
- Query URLs are generated from typed presets through `EventFilterUrlHelper` or an Application-provided URL model.
- Persist `PublicEventSectionPresetConfigV1` or equivalent versioned config records; do not persist `PublicEventSectionPresetDto` directly.

### Primary organization contract and failure model

OrganizationCentric mode may emphasize one configured primary organization, but it must not assume the tenant only contains one organization.

Rules:

1. OrganizationCentric mode should prefer a valid `PrimaryOrganizationId` configured through onboarding/admin settings.
2. `PrimaryOrganizationId` must resolve to a visible, non-deleted, tenant-local organization with an actor where actor-backed event ownership is needed.
3. If no primary organization is configured, onboarding/admin UI must guide the admin to create or select one.
4. If the primary organization is missing, deleted, hidden, suspended, or outside the tenant, runtime returns an explicit state, degrades to a safe neutral home/onboarding state, and does not leak data.
5. If multiple organizations exist, OrganizationCentric mode still emphasizes the configured primary organization; other organizations remain valid event organizers where permitted.
6. Authorization to manage tenant public posture is separate from authorization to manage the organization itself. UI affordances must come from server/HAL state.

Conceptual contract:

```csharp
public enum PrimaryOrganizationState
{
    Available,
    NotConfigured,
    Missing,
    Deleted,
    HiddenOrInactive,
    CrossTenantInvalid,
    ActorUnavailable
}

public sealed record PrimaryOrganizationDto(
    PrimaryOrganizationState State,
    Guid? OrganizationId,
    Guid? ActorId,
    string? DisplayName,
    string? Slug,
    string? RemediationMessage);
```

### Bounded home/navigation blocks

Organization-centric public customization is CMS-lite and bounded. Supported blocks should remain small and explicit:

- Hero / organization intro
- Upcoming events
- Featured event
- Featured event sections
- CTA links
- Contact/location
- Donation/volunteer link
- Footer

Do not add drag-and-drop layout, arbitrary CSS, arbitrary component composition, arbitrary HTML/script injection, or per-section render modes.

### Segmentation model without OrganizerScope

Use current concepts:

- **Organization**: in-tenant event organizer/publisher. In OrganizationCentric mode, one organization can be configured as the primary public emphasis, but organizations remain actors inside the tenant.
- **Group**: optional internal publishing/team model where the current group concept is already appropriate.
- **Actor**: existing publisher identity for event ownership and filtering.
- **Actor-backed ownership filters**: wire existing actor filtering into the public event-list query surface so organization and group event catalogs can be expressed without new scope tables. These filters mean publisher/owner, not audience/section.
- **Category/Tag**: primary user-facing segmentation for Youth, Sisters, Education, Community Services, fundraising, family, lectures, workshops, etc.
- **Audience fields**: built-in filters for gender, age, language, madhab, and similar culturally relevant constraints.
- **Custom property projections**: advanced structured filtering when built-in fields are insufficient, without schema churn.
- **Tenant navigation links with generated query strings**: curated links generated from typed presets instead of workspace switchers. Query strings are shareable transport, not authoritative configuration storage.

### User experience principles

- Single-organization deployments should feel like the organization's website with an events module.
- OrganizationCentric home must look intentionally owned: identity, mission/intro, upcoming activities, featured sections, CTAs, contact/location, and a clear event catalog path.
- Keep top-level navigation shallow: Home, Events/Calendar, Programs, About, Contact, Donate/Volunteer if configured.
- Avoid mandatory selectors like tenant/workspace/program switchers.
- Prefer curated filter chips and saved links over new hierarchy.
- Do not remove `/events`; demote and relabel it based on settings.
- Keep admin customization bounded: posture, labels, home route, primary organization, typed event-section presets, configured CTAs, and nav/footer links.

### Accessibility acceptance criteria

Organization-centric UX must meet the existing WCAG 2.2 AA posture documented in `docs/ACCESSIBILITY.md`.

- OrganizationCentric home has exactly one visible `<h1>` that identifies the organization or public site purpose.
- Shell keeps skip link, main landmark, header/nav semantics, and live-region behavior intact.
- Filter presets/chips are keyboard reachable and have accessible names that describe the resulting filter.
- Active filters are visually clear and announced through labels/live status where appropriate.
- Empty states explain whether there are no events, no events for the selected preset, or a missing/unconfigured primary organization.
- Navigation relabeling does not create duplicate ambiguous landmarks or indistinguishable links.
- RTL/logical CSS and focus-visible styles are preserved.

## Non-Goals

- No `OrganizerScope`, `BusinessScope`, `Workspace`, `SubTenant`, or equivalent domain entity/table/foreign key/query filter.
- No fake multi-tenancy inside a tenant.
- No mandatory internal workspace selector.
- No role/claim checks in Blazor UI in place of HAL links.
- No repository DTO returns, direct DbContext usage from Application handlers, or runtime tenant-filter disabling.
- No unbounded per-section layout/CSS customization that creates support and consistency debt.
- No arbitrary query-string blobs as authoritative public-section configuration.
- No Domain-layer business defaults for public experience, section presets, organization posture, event visibility posture, or import convenience.
- No wording or implementation that treats `Organization` as `Tenant` or as a tenant substitute.

## Implementation Phases by Clean Architecture Layers

### Phase 1 - Public-experience vocabulary and guardrails (M)

**Goal:** Establish vocabulary and settings without adding new operational scope entities.

- Add or extend public-experience setting definitions for organization-centric posture, public home emphasis, event catalog label, typed event-section presets, configured CTAs, and primary organization.
- Define a public-shell resolution policy: anonymous public shell output resolves from tenant + instance public settings and tenant-local referenced entities only; user/group-specific setting scopes are excluded from anonymous SEO/public surfaces.
- Define versioned config records for persisted public-experience configuration, including event-section preset config, and map them to public DTOs instead of storing display DTOs directly.
- Keep changes in settings/lookup/configuration patterns, not new aggregates.
- Update documentation comments and setting metadata so admins understand that segmentation is filter/category/tag based.
- Add convention/architecture guardrails for forbidden scope concepts and Domain-layer default drift using path/context-aware checks, not broad grep failures on valid existing `Scope` vocabulary.

**Expected files:**
- `Explore.Domain/Settings/Definitions/*`
- `Explore.Application/DTOs/Onboarding/PublicExperienceSettingsDto.cs`
- `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs`

**Acceptance criteria:**
- Public experience settings can represent discovery-centric and organization-centric posture.
- No new Domain entity named Scope, Workspace, BusinessScope, OrganizerScope, or SubTenant exists.
- Existing tenant and soft-delete query filter behavior is unchanged.
- Setting metadata and docs explain that Organization is an in-tenant event organizer/actor, not a tenant substitute.
- Persisted public-experience settings use versioned config records; Blazor-facing DTOs are not the storage contract.
- Architecture/convention tests fail if this feature introduces forbidden entity files, migration tables, event ownership IDs, or organization-as-tenant resolver paths; they do not fail on legitimate existing setting, authorization, registration, projection, or governance scope vocabulary.

### Phase 2 - Backend contract and actor-backed filtering (L)

**Goal:** Prove the backend read contract and actor-backed event ownership filtering before UI or admin-editor work.

- Introduce or extend a single typed `PublicExperienceShellDto` that includes `SchemaVersion`, `Revision`, mode, home composition, navigation, event catalog labels/options, event-section presets, primary organization, CTAs, and footer read projection.
- Resolve anonymous shell data from tenant + instance public settings and tenant-local referenced content only.
- Expose actor-backed filtering in the event-list request/API contract as the first implementation milestone so organization/group catalogs can use the same list page and filter bar.
- Add `ActorId`, `OrganizationId`, and `GroupId` request/API filter inputs; resolve organization/group to actor IDs in Application; map to `EventFilter.Actor(Guid)` or equivalent specification primitives.
- Preserve private/unauthorized event visibility rules and query-string round-trip behavior.
- Represent filter presets as versioned config records mapped to validated Application models, then public DTOs and generated URLs.
- Define primary-organization unavailable behavior in Application before Blazor rendering.
- Preserve import-friendly optional data rules: do not hard-require taxonomy/module/audience fields in Domain or shared import contracts; enforce rich interactive-create requirements through create DTO validators.
- Keep handlers cancellation-aware and validation local to handlers.
- Use HybridCache consistently if existing public-experience queries already cache settings, and invalidate the public shell cache when relevant public-experience settings, primary organization metadata, footer config, tenant navigation links, or event-section preset config changes.

**Expected files:**
- `Explore.Application/Features/PublicExperience/Requests/Queries/GetPublicExperienceSettingsQuery.cs`
- `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs`
- `Explore.Application/Settings/Groups/EventListSettingGroup.cs`
- `Explore.Application/Specifications/Events/EventQuerySpecification.cs`
- `Explore.Application/Features/Events/Requests/Queries/GetEventListRequest.cs`
- `Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs`

**Acceptance criteria:**
- Application returns DTOs, never entities, to API/Blazor.
- Public shell is typed, versioned, server-shaped, and revisioned; Blazor does not reconstruct posture from independent settings blobs.
- Anonymous shell output is stable for public SEO surfaces and is not personalized by user/group-specific setting scopes.
- Footer data inside the shell is a read projection; footer write ownership stays with the footer subsystem.
- Filter presets are mapped from versioned config records to existing specification primitives and generated shareable query URLs.
- Public event-list queries can filter by actor-backed organization/group ownership without introducing `WorkspaceId` or scope IDs.
- Primary organization missing/deleted/hidden/cross-tenant/actor-unavailable cases produce explicit `PrimaryOrganizationState` values plus safe neutral/onboarding rendering data.
- Application tests prove minimal import/create-contract paths are not broken by organization-centric required fields.
- No repository returns DTO or `IQueryable`.

### Phase 3 - Shell defaults, API, and authorization/HATEOAS alignment (M)

**Goal:** Expose the backend contract through API and seed/default configuration before Blazor polish or admin editors.

- Extend `Explore.API/Controllers/PublicExperienceController.cs` response shape through Application DTOs.
- Seed or derive a default OrganizationCentric shell for a configured primary organization so the read path can be proven without full admin UI.
- Keep anonymous public reads bounded to the typed public shell; privileged posture/preset writes remain authorized and tenant-scoped when added later.
- Update Cerbos/local authorization policies if any new write/admin action is introduced during this phase.
- Ensure HAL materialization continues to be the source of truth for UI action affordances.

**Expected files:**
- `Explore.API/Controllers/PublicExperienceController.cs`
- `Explore.API/Controllers/InstanceOnboardingController.cs`
- `Explore.API/Controllers/EventController.cs`
- `Explore.API/Models/EventFilterRequest.cs`
- Existing authorization policy/configuration files discovered during implementation

**Acceptance criteria:**
- Public read endpoints remain safe for anonymous users where appropriate.
- Any writes introduced in this phase are authorized and resource-scoped; otherwise the phase stays read-only/default-config focused.
- Blazor receives affordances through HAL or existing typed client responses, not local role guessing.
- Organization management permissions are not inferred from tenant posture-management permissions.

### Phase 4 - Persistence and migration strategy (S/M)

**Goal:** Persist settings and keep filters performant without broad schema expansion.

- Prefer existing `SystemSetting`, `TenantSetting`, and tenant navigation link storage.
- Add EF migration only if setting seed data, lookup rows, or indexes must change.
- Add indexes only after verifying query shape and missing coverage.
- Preserve named tenant and soft-delete query filters.
- Store versioned typed config records through existing settings infrastructure; do not persist raw query strings or Blazor-facing display DTOs as authoritative configuration.
- Keep persistence defaults limited to genuine persistence concerns such as counters/flags; business defaults remain in Application.

**Expected files:**
- `Explore.Persistence/Seed/LookupTableSeeder.cs`
- `Explore.Persistence/ExploreDbContext.DbSets.cs`
- `Explore.Persistence/Repositories/EventRepository.cs`
- `Explore.Persistence/Migrations/*` if seed/index/schema changes are required

**Acceptance criteria:**
- No new workspace/scope table is created.
- Existing event/category/tag/custom-property indexes are reused where possible.
- Any new migration is small, focused, and includes generated SQL review.
- Existing imports or future import endpoints can omit non-essential taxonomy and organization-centric UX fields without persistence failure.
- Config migration/versioning is explicit for serialized public-experience records.

### Phase 5 - Shell-driven Blazor organization-centric read path (L/XL)

**Goal:** Make self-hosted single-organization deployments feel owned, simple, and filter-driven.

- Update `HomeStart.razor` or routing service behavior so OrganizationCentric mode renders from `PublicExperienceShellDto`.
- Add `PublicExperienceService.GetShellAsync` or equivalent typed read path; keep existing settings calls only where still needed for non-shell concerns.
- Add organization-centric home composition using existing organization profile, upcoming events, featured categories/tags, and configured CTAs.
- Update `NavMenu.razor` and `AppSideNav.razor` to hide/demote marketplace-style links in OrganizationCentric mode.
- Keep `/events` but relabel it through settings as Calendar, Programs, Activities, or Events.
- Add curated filter chips/presets to event list based on typed presets with generated query strings.
- Ensure Add Event and admin actions continue to rely on HAL links or existing authorized endpoints.
- Add explicit organization-centric empty states for missing primary organization, no upcoming events, and no matches for a selected section.
- Keep customization within supported blocks: hero/intro, upcoming events, featured event, featured sections, CTAs, contact/location, donation/volunteer, footer.

**Expected files:**
- `Explore.Blazor.Client/Pages/HomeStart.razor`
- `Explore.Blazor.Client/Services/StartupRoutingService.cs`
- `Explore.Blazor.Client/Services/PublicExperienceService.cs`
- `Explore.Blazor.Client/Layout/NavMenu.razor`
- `Explore.Blazor.Client/Components/Shell/AppSideNav.razor`
- `Explore.Blazor.Client/Pages/Events/EventList.razor`
- `Explore.Blazor.Client/Pages/Events/EventList.razor.cs`
- `Explore.Blazor.Client/Pages/Events/Components/EventFilterBar.razor`
- `Explore.Blazor.Client/Pages/Organizations/OrganizationProfile.razor`
- `Explore.Blazor.Client/Pages/Onboarding/InstanceOnboarding.razor`
- `Explore.Blazor.Client/Pages/Onboarding/TenantOnboarding.razor`

**Acceptance criteria:**
- OrganizationCentric mode has no mandatory workspace selector.
- Navigation remains shallow and admin-configurable.
- DiscoveryCentric behavior remains available as a separate posture, even though backward compatibility is not a constraint.
- Render-mode rules are respected; no `HttpContext` dependency is introduced in interactive client paths.
- Accessibility criteria are met for heading hierarchy, keyboard-reachable presets, active-filter labels, empty states, landmarks, focus, and RTL/logical CSS.

### Phase 5b - Admin/onboarding editor after read-path proof (M)

**Goal:** Add editing flows only after the shell contract, defaults, actor-backed filtering, and Blazor read path are proven.

- Let admins choose public-experience mode, primary organization, event catalog label, bounded CTA/home blocks, and typed event-section preset config.
- Reuse existing onboarding/admin/settings surfaces where possible.
- Validate tenant-local references in Application and reject or omit invalid/deleted/hidden/cross-tenant references before persistence.
- Keep form complexity subordinate to the already-proven backend contract; do not design a new scope model to satisfy editor convenience.

**Expected files:**
- `Explore.API/Controllers/InstanceOnboardingController.cs`
- `Explore.API/Controllers/TenantOnboardingController.cs`
- `Explore.Blazor.Client/Pages/Onboarding/InstanceOnboarding.razor`
- `Explore.Blazor.Client/Pages/Onboarding/TenantOnboarding.razor`
- Existing settings/admin components discovered during implementation

**Acceptance criteria:**
- Admin writes are authorized, tenant-scoped, and validation-backed.
- Editor persists versioned config records, not display DTOs or raw query strings.
- UI uses project components and MudBlazor v9 APIs.
- No admin form introduces workspace/scope language or `Organization` as tenant substitute.

### Phase 6 - Tests and verification (M/L)

**Goal:** Prove the implementation is tenant-safe, filter-correct, and UI-consistent.

- Add Application tests for public-experience DTO mapping and filter preset translation.
- Add Persistence tests for filter queries if new query paths or indexes are introduced.
- Add API integration tests for public settings reads and authorized settings writes.
- Add architecture/convention tests for forbidden scope concepts, Organization != Tenant terminology, and Domain-layer default drift.
- Add product/UX tests for both public postures, `/events` reachability, relabeled event catalog, curated section URLs, primary-organization failure states, accessibility, and HAL-gated actions.
- Add Blazor tests for organization-centric nav/home behavior and HAL-gated actions.
- Run architecture tests and full build.

**Required verification commands:**

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.CleanArchitectureTests
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.CqrsPatternTests
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.BlazorClientArchitectureTests
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.AuthorizationParityTests
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet build --configuration Release --verbosity quiet
```

## Detailed Tasks with Acceptance Criteria

### T1 - Define public-experience posture settings

- **Priority:** High
- **Effort:** M
- **Dependencies:** None
- **Relevant skills:** clean-architecture-rules, cqrs-mediatr-guidelines
- **Acceptance criteria:** Settings support DiscoveryCentric and OrganizationCentric posture, configured `PrimaryOrganizationId`, bounded CTAs/home blocks, and versioned event-section preset config records; anonymous shell resolution is documented as tenant + instance public settings plus tenant-local referenced content only; docs/comments state `Organization` is an in-tenant actor-backed publisher/organizer and not a tenant; no new domain scope entity is introduced.

### T2 - Extend public-experience query contract

- **Priority:** High
- **Effort:** L
- **Dependencies:** T1
- **Relevant skills:** cqrs-mediatr-guidelines
- **Acceptance criteria:** `PublicExperienceShellDto` exposes `SchemaVersion`, `Revision`, posture, home composition, navigation, event catalog, explicit `PrimaryOrganizationState`, footer read projection, CTAs, and typed `PublicEventSectionPresetDto` values; handler maps tenant/instance public config through existing resolver/service patterns; cancellation tokens and validation conventions are followed; Blazor receives one server-shaped public shell rather than reconstructing a UX blob client-side; shell cache invalidates when relevant public-experience settings, primary organization metadata, footer config, tenant navigation links, or event-section preset config changes.

### T2a - Wire actor-backed filters into public event list

- **Priority:** High
- **Effort:** M
- **Dependencies:** T1
- **Relevant skills:** cqrs-mediatr-guidelines, auth-patterns, dotnet-efcore-guidelines
- **Acceptance criteria:** This is the first code milestone before shell polish or admin UI. `GetEventListRequest` and `EventFilterRequest` can carry `ActorId`, `OrganizationId`, and `GroupId` catalog filters mapped to existing `EventFilter.Actor(Guid)` or equivalent actor resolution; organization/group-to-actor resolution is Application-owned; unauthorized/private/cross-tenant events remain hidden; query-string round trip is tested; no new scope/workspace ID is added.

### T2b - Define primary organization and import/default failure behavior

- **Priority:** High
- **Effort:** M
- **Dependencies:** T1, T2
- **Relevant skills:** clean-architecture-rules, cqrs-mediatr-guidelines, dotnet-efcore-guidelines
- **Acceptance criteria:** Application defines `PrimaryOrganizationState` cases for `Available`, `NotConfigured`, `Missing`, `Deleted`, `HiddenOrInactive`, `CrossTenantInvalid`, and `ActorUnavailable`; shell rendering data includes safe neutral/onboarding behavior for non-available states; Application tests prove minimal event-create/import-shaped inputs can omit non-essential taxonomy/audience/custom-property/organization-centric fields; no Domain entity gains business defaults for public posture, event visibility posture, presets, or import convenience.

### T3 - Add admin/onboarding configuration UI

- **Priority:** High
- **Effort:** M
- **Dependencies:** T2, T2a, T4, T5, T6
- **Relevant skills:** blazor-ui-conventions, auth-patterns
- **Acceptance criteria:** Implemented after backend/read-path proof. Instance/tenant onboarding lets admins choose organization-centric posture, labels, primary organization, bounded CTAs/home blocks, and typed curated preset config; writes are authorized and tenant-scoped; UI uses project components and MudBlazor v9 APIs; invalid cross-tenant/deleted/hidden references are rejected or omitted by Application; the editor persists versioned config records, not Blazor display DTOs or raw query strings.

### T4 - Implement organization-centric home

- **Priority:** High
- **Effort:** L
- **Dependencies:** T2
- **Relevant skills:** blazor-ui-conventions
- **Acceptance criteria:** Single-organization-emphasis deployments render from `PublicExperienceShellDto` through a typed shell client method with a primary-organization-first home, upcoming events, featured event/sections, CTAs, contact/location, donation/volunteer, and clear route behavior; non-available primary organization states use safe neutral/onboarding UI; `/events` remains reachable.

### T5 - Refactor navigation posture

- **Priority:** High
- **Effort:** M
- **Dependencies:** T2
- **Relevant skills:** blazor-ui-conventions, auth-patterns
- **Acceptance criteria:** Marketplace-style links are hidden/demoted in OrganizationCentric mode; tenant navigation links and generated URLs from typed presets drive section shortcuts; event catalog can be labeled Calendar/Programs/Activities/Events; action affordances remain HAL/server-driven and organization-management permissions are not inferred from posture-management permissions.

### T6 - Add curated filter presets to event list

- **Priority:** High
- **Effort:** L
- **Dependencies:** T2, T5
- **Relevant skills:** cqrs-mediatr-guidelines, blazor-ui-conventions, dotnet-efcore-guidelines
- **Acceptance criteria:** Typed presets use existing actor ownership, category, tag, audience, event type, format, date, and custom-property filters; query strings are generated/shareable but are not the authoritative persisted config; no workspace selector exists; filters are predictable, keyboard reachable, accessibly named, and visually/semantically announce active state.

### T7 - Validate persistence and indexes

- **Priority:** Medium
- **Effort:** M
- **Dependencies:** T6
- **Relevant skills:** dotnet-efcore-guidelines
- **Acceptance criteria:** Query plans/index coverage are reviewed for common presets; versioned config records are stored through existing settings infrastructure, not raw query strings or Blazor-facing DTOs; config migration/versioning is explicit; new indexes are added only when evidence shows a gap; migrations are focused and tenant-safe; persistence defaults remain limited to genuine persistence concerns.

### T8 - Update authorization policy coverage if new writes exist

- **Priority:** High
- **Effort:** M
- **Dependencies:** T3
- **Relevant skills:** auth-patterns
- **Acceptance criteria:** New settings/navigation write operations are protected by roles/resource authorization; Cerbos/local parity tests cover the action; UI does not inspect roles directly.

### T9 - Test organization-centric and discovery-centric behavior

- **Priority:** High
- **Effort:** L
- **Dependencies:** T1-T8
- **Relevant skills:** clean-architecture-rules, blazor-ui-conventions, cqrs-mediatr-guidelines
- **Acceptance criteria:** Unit, integration, architecture, and Blazor tests pass; build passes; tests assert no workspace/scope model is required, `Organization` is not treated as `Tenant`, forbidden names/IDs are absent from event ownership paths through precise file/schema/API checks, legitimate existing scope vocabulary is not blocked, Domain business-default drift is caught, both public postures work, anonymous shell resolution excludes user/group personalization, shell schema/revision/cache invalidation works, `/events` is reachable/relabelable, curated URLs work, primary-organization enum states are safe, accessibility criteria pass, and HAL-gated actions remain server-authorized.

### T10 - Update documentation

- **Priority:** Medium
- **Effort:** S
- **Dependencies:** T1-T9
- **Relevant skills:** agentic-research
- **Acceptance criteria:** Docs explain single-tenant does not mean marketplace UX, `Organization` is an actor-backed in-tenant publisher/organizer rather than the tenant, segmentation uses ownership filters plus categories/tags/audience/custom properties/groups, imports tolerate minimal common-denominator event fields, defaults belong in Application/validators or EF persistence configuration as appropriate, and non-goals explicitly reject OrganizerScope/Workspace/SubTenant concepts.

## Risk Assessment and Mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Settings sprawl recreates complexity in another form | High | Limit configuration to posture, labels, primary organization, typed event-section presets, bounded CTAs/home blocks, footer, and nav links. Avoid arbitrary layout builders, raw query-string blobs, custom HTML, arbitrary CSS, or mini-CMS behavior. |
| Public shell uses the full settings cascade and becomes personalized by accident | High | Anonymous shell resolution is tenant + instance public settings only, with tenant-local referenced entities. User/group-specific settings require a future personalized shell/dashboard contract. |
| Shell cache serves stale public UX | High | Include `SchemaVersion` and `Revision`; derive revision from relevant settings/navigation/footer/preset/primary-organization updates; use HybridCache and existing ETag behavior deliberately. |
| Organization gets mistaken for Tenant | High | Keep glossary and tests explicit: Tenant is governance/isolation; Organization is an in-tenant actor-backed publisher/organizer. Reject tenant-substitute wording and forbid new organization-as-scope IDs in event ownership paths. |
| Filters become too advanced for ordinary admins | High | Ship typed curated presets, validation, generated URLs, and plain-language labels. Keep advanced custom properties optional and hide implementation details from admins. |
| Import friction increases because rich UX fields become required | High | Keep common-denominator event fields valid for imports; enforce stricter interactive-create requirements in Application DTO validators; block Domain-layer business defaults and required taxonomy drift. |
| Primary organization is missing, hidden, deleted, suspended, or cross-tenant | High | Resolve primary organization in Application and return a safe neutral/onboarding shell with no cross-tenant leakage; expose admin remediation affordances only through authorized/HAL-controlled paths. |
| Organization-centric home duplicates organization profile logic | Medium | Compose existing organization profile/event components where possible; avoid forking separate implementations. |
| Event filtering becomes slow at scale | Medium | Use existing indexes, EF keyset pagination guidance, and evidence-driven index additions. |
| Authorization drift between API and UI | High | Keep HAL/server authorization as source of truth and add parity tests for new actions. |
| Multi-tenant public directory UX regresses | Medium | Keep DiscoveryCentric posture and test both postures. |
| Developers accidentally introduce Workspace/Scope during implementation | High | Add context-aware non-goal checks in code review and tests: forbidden entity files/tables/IDs and event ownership paths fail, while valid existing settings/authorization/registration/projection scope vocabulary is allowed. |
| Admin UI drives the architecture instead of the contract | Medium | Deliver actor-backed filtering, shell defaults, and shell-driven Blazor read path before admin/onboarding editors. |
| Accessibility regresses when public navigation/home changes | High | Promote WCAG 2.2 AA acceptance criteria to product tests: heading hierarchy, skip/main/nav landmarks, keyboard presets, active-filter announcements, empty states, focus visibility, and RTL/logical CSS. |

## Success Metrics

- A self-hosted single-organization instance can launch with an organization-first homepage without custom code.
- Admins can expose Youth, Sisters, Education, Community Services, or similar sections using typed presets over actor ownership, categories, tags, audience fields, custom properties, and navigation links.
- Users can find events through shallow navigation and clear filters without understanding tenants or workspaces.
- `/events` remains available but can be labeled and framed as Calendar/Programs/Activities.
- `PublicExperienceShellDto` is the authoritative public read model for Blazor home/nav/catalog/footer rendering.
- Anonymous public shell rendering is stable and resolved from tenant/instance public settings plus tenant-local referenced content only.
- Shell responses carry schema version and revision/cache tokens, and public UX changes invalidate predictably.
- Persisted public-experience configuration uses versioned config records mapped to Application DTOs; Blazor-facing DTOs are not storage contracts.
- Missing or invalid primary organization references degrade safely without data leakage.
- Minimal import/create-contract paths are not broken by organization-centric taxonomy or presentation fields.
- Organization-centric screens meet the documented accessibility acceptance criteria.
- No new OrganizerScope/BusinessScope/Workspace/SubTenant domain model exists.
- Architecture, authorization, application, persistence, API, Blazor, and build verification pass.

## Required Resources and Dependencies

- Existing settings hierarchy and public-experience query flow.
- Existing tenant navigation link management.
- Existing event specification filters and custom-property projections.
- Existing Actor/Organization/Group publisher model and `Event.ActorId` ownership.
- Existing organization profile/details surfaces.
- Existing footer configuration flow for bounded footer composition.
- MudBlazor v9 and project shared component conventions.
- EF Core PostgreSQL indexing and query-filter discipline.

## Effort Estimate

- **Planning/docs:** Completed in this task.
- **Backend contract/settings/API:** 2-4 focused engineering days for `PublicExperienceShellDto`, typed presets, primary organization resolution, actor-backed `/events` filtering, authorized writes, and settings persistence.
- **Blazor home/nav/event-list UX:** 4-7 focused engineering days for shell-driven home composition, navigation demotion/relabeling, curated filters, bounded blocks, empty states, and accessibility polish.
- **Tests/regression hardening:** 2-4 focused engineering days for architecture guardrails, Application/API/Persistence/Blazor tests, authorization parity, accessibility/product tests, and build verification.
- **Docs/polish:** 1-2 focused engineering days for implementation notes, admin-facing wording, and migration/rollout documentation.
- **Total:** Approximately 1.5-2.5 focused engineering weeks for enterprise-grade implementation with tests and documentation.
