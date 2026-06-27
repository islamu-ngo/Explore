# Full Property Update Sub-DTO Pattern - Implementation Plan

Last Updated: 2026-06-27 Europe/Brussels

## 0. Planning Metadata
- **Request:** Bring full per-property or per-logical-group partial update support to all update-eligible entities using the `Explore.Application/DTOs/User/` sub-DTO pattern: one update command/handler per entity, nullable update groups, manual validators, entity-returning repositories, handler-owned mapping, one save, and cache invalidation.
- **Task directory:** `dev/active/full-property-update-sub-dto/`
- **Planning status:** Draft
- **Matched intents:** `add-write-endpoint` and `add-cqrs-handler`.
- **Relevant skills:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`.
- **Relevant rules:** `.claude/rules/application-layer.md`, `.claude/rules/api-controllers.md`.
- **Primary layers touched:** Application, API, tests, docs. Persistence only where existing repositories lack entity update loading needed by handlers.
- **Estimated complexity:** XL. The repo has 55 `Update*Dto` files, 62 update command/handler files, 89 controllers, and many domain entities including lookups, projections, outbox rows, history/audit rows, and specialized lifecycle commands. This must be inventory-driven, not generated blindly.

## 1. Executive Summary
The target is a consistent update model where each update-eligible entity has one `Update{Entity}Command`, one `Update{Entity}CommandHandler`, and a wrapper `Update{Entity}Dto` whose nullable sub-DTO properties represent independently saveable update groups. A client can submit only the group it wants to change, such as `Names`, `ProfileImage`, `Title`, `Pricing`, `Schedule`, `Appearance`, or `ModerationMetadata`. The handler validates only present groups, loads the entity once, applies every present group, saves once, invalidates relevant HybridCache/output-cache keys or tags, and returns the existing command response shape for that feature.

This plan does not expose generic writes for every row in `Explore.Domain`. "All entities" is interpreted as all mutable API/Application update surfaces. Read-only lookups, projections, audit/history records, idempotency/outbox records, provider-owned state, and lifecycle operations that are domain actions remain excluded unless an existing public update command already owns them or a later approved task explicitly adds one.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log
| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| User already implements the requested canonical wrapper pattern. | Verified: `Explore.Application/DTOs/User/UpdateUserDto.cs`; `Explore.Application/Features/Users/Handlers/Commands/UpdateUserCommandHandler.cs`; `Explore.Application/Profiles/UserMappingProfile.cs`; `Explore.Application/DTOs/User/Validators/UpdateUserDtoValidator.cs`. | High | `Names` and `ProfileImage` are nullable groups; handler maps `Names`, updates actor/storage for `ProfileImage`, saves, removes `user:detail:{id}`. |
| Actor already partially follows the single-handler/null-group pattern. | Verified: `Explore.Application/Features/Actors/Requests/Commands/UpdateActorCommand.cs`; `Explore.Application/Features/Actors/Handlers/Commands/UpdateActorCommandHandler.cs`. | High | Has `ActorDto?` and `AppearanceDto?`, but `UpdateActorDto` is still broad and overlaps appearance fields. |
| Event has a null-group shell but still one broad `UpdateEventDto`. | Verified: `Explore.Application/Features/Events/Requests/Commands/UpdateEventCommand.cs`; `Explore.Application/Features/Events/Handlers/Commands/UpdateEventCommandHandler.cs`; `Explore.Application/DTOs/Event/Validators/UpdateEventDtoValidator.cs`. | High | Good command/handler shell; needs sub-DTO decomposition. |
| Many legacy handlers still validate and map a monolithic update DTO. | Verified: `UpdateCategoryCommandHandler`, `UpdateLocationCommandHandler`. | High | These must be converted to wrapper + sub-DTO groups. |
| Some update handlers use different response conventions. | Verified: `UpdateOrganizationCommandHandler` returns `Unit`; `UpdateOrganizationDetailsCommandHandler` returns `BaseCommandResponse<Guid>`. | High | Migration should preserve local controller contract unless explicitly changed. |
| API update routes are existing `PUT` routes with named route constants. | Verified: `UserController.UpdateUser`, `CategoryController.Update`, `OrganizationController.Update`. | High | Avoid route churn; update request body shape first. |
| Update coverage is incomplete. | Verified by search: `Event.Application.UnitTests` has update tests for Actor, EventAgendaItem, EventDay, EventRegistration, EventSessions, LocationRooms, but no `UpdateUserCommandHandlerTests`; CodeGraph reported no covering tests for `UpdateUserCommand`. | High | Add focused tests as part of migration. |
| Cache invalidation is inconsistent. | Verified by search: `RemoveAsync("categories:list:1:20")`, `RemoveAsync($"user:detail:{id}")`, `RemoveByTagAsync(CacheTags.EventListByTenant(...))`. | High | Each migrated handler needs explicit cache invalidation notes. |
| Several DTO files lack the required two-line `ABOUTME` header. | Verified: `Explore.Application/DTOs/User/UpdateUserDto.cs`, `Explore.Application/DTOs/Actor/UpdateActorDto.cs`. | High | New and touched files must be fixed. |

### 2.2 Existing Implementation
The current Application layer uses CQRS/MediatR with handlers in `Explore.Application/Features/**/Handlers/Commands` and request types in `Explore.Application/Features/**/Requests/Commands`. DTOs live in `Explore.Application/DTOs/{Entity}/`, validators usually live under `Validators/`, and mapping profiles live in `Explore.Application/Profiles/`.

The best existing pattern is `User`:
- `UpdateUserDto` contains `Guid Id`, `UpdateUserNamesDto? Names`, and `UpdateUserProfileImageDto? ProfileImage`.
- `UpdateUserDtoValidator` manually composes group validators and rejects an empty wrapper.
- `UpdateUserCommandHandler` manually creates the validator, loads `User`, maps `Names` through AutoMapper into `User`/`UserPii`, updates the linked `Actor` and `StorageObject` for profile images, saves via repositories, and removes `user:detail:{id}`.

`Actor` and `Event` have the one-handler shell but have not fully decomposed every mutable field into independently saveable groups. `Category` and `Location` still represent the older full-update style: validate a broad DTO, load by DTO ID, map the entire DTO, save.

### 2.3 Existing Tests And Verification Coverage
Verified test projects:
- `Event.Application.UnitTests/Event.Application.UnitTests.csproj`
- `Event.API.IntegrationTests/Event.API.IntegrationTests.csproj`
- `Event.Architecture.Tests/Event.Architecture.Tests.csproj`

Verified relevant test files include:
- `Event.Application.UnitTests/Features/Actors/Commands/UpdateActorCommandHandlerTests.cs`
- `Event.Application.UnitTests/Features/EventAgendaItems/Commands/UpdateEventAgendaItemCommandHandlerTests.cs`
- `Event.Application.UnitTests/Features/EventDays/Commands/UpdateEventDayCommandHandlerTests.cs`
- `Event.Application.UnitTests/Features/EventRegistrations/Commands/UpdateEventRegistrationCommandHandlerTests.cs`
- `Event.Application.UnitTests/Features/EventSessions/Commands/UpdateEventSessionCommandHandlerTests.cs`
- `Event.Application.UnitTests/Features/LocationRooms/Commands/UpdateLocationRoomCommandHandlerTests.cs`
- `Event.API.IntegrationTests/Features/AuthorizationIntegrationTests.cs`
- `Event.Architecture.Tests/CqrsPatternTests.cs`
- `Event.Architecture.Tests/CleanArchitectureTests.cs`
- `Event.Architecture.Tests/ApiContractArchitectureTests.cs`

Missing or under-covered: `UpdateUserCommandHandler`, many simple CRUD updates, route-level payload validation for new optional-group bodies, cache invalidation behavior, and empty-wrapper rejection.

### 2.4 Existing Documentation And Contracts
Relevant docs read:
- `AGENTS.md`
- `dev/active/README.md`
- `.claude/contract/intents.yaml`
- `docs/QUICK_REFERENCE.md`
- `docs/GOVERNANCE.md`
- `docs/ARCHITECTURE.md`
- `docs/API.md`
- `docs/SECURITY-MODEL.md`
- `docs/AUTHORIZATION.md`
- `docs/CODEBASE_STRUCTURE.md`
- `docs/OPERATIONS.md`

Intent-derived docs to update during implementation:
- `docs/API_CHANGELOG.md`
- Any generated OpenAPI/client artifacts if request DTO schemas change and the existing build process requires regeneration.

### 2.5 Current Pain Points / Improvement Areas
- Broad update DTOs make partial updates ambiguous. A client cannot reliably distinguish "leave unchanged" from "clear to null" when an entire DTO is required.
- Some handlers perform multiple saves inside branches, such as `UpdateOrganizationCommandHandler` for approval status. The target pattern should load once, apply every present group, and save once unless a repository transaction or dependent aggregate explicitly requires otherwise.
- Existing DTO headers are inconsistent with the two-line `ABOUTME` rule.
- Cache invalidation is feature-specific and not documented per handler, increasing stale detail/list risk.
- Current tests focus on selected commands; there is no cross-feature contract test enforcing "wrapper has at least one group" or "single update handler per entity".
- Response conventions vary (`Unit` versus `BaseCommandResponse<Guid>`). A broad migration must avoid accidental API breaking changes unless the endpoint already returns command responses.

### 2.6 Unknowns After Investigation
- Exact full mutable field inventory per entity is not yet enumerated. Implementation must create the inventory from current DTOs, domain entities, validators, mapping profiles, controllers, and authorization policies before editing each feature batch.
- Some nullable fields need explicit clear semantics. A nullable group means "group omitted", so fields inside a present group may need `OptionalValue<T>`/clear flags or an approved local pattern when null is a valid new value.
- Some existing update commands are domain actions rather than property updates, such as publish, archive, moderation, scheduling, template sync, role assignment windows, and settings batch updates. These should not be forced into the generic property-update pattern without preserving domain invariants.
- The OpenAPI/NSwag regeneration command was not run during planning. Implementation agents must verify the current client-generation workflow before changing public request schemas.

## 3. Proposed Future State
Every update-eligible entity uses this request shape:

```csharp
public sealed class UpdateCategoryDto
{
    public Guid Id { get; set; }
    public UpdateCategoryNameDto? Name { get; set; }
    public UpdateCategoryParentDto? Parent { get; set; }
    public UpdateCategoryPresentationDto? Presentation { get; set; }
}
```

Handler flow:
1. Manually instantiate `Update{Entity}DtoValidator`.
2. Validate wrapper ID and require at least one non-null group.
3. Load the domain entity by route/body ID through the repository.
4. For each present group, validate group-specific rules and apply only that group.
5. Use AutoMapper for straightforward DTO-to-entity maps and explicit code for relationship, clear-null, storage, schedule, tenant, or invariant-sensitive updates.
6. Save once through the entity repository.
7. Invalidate exact detail keys and relevant list tags/keys.
8. Return the existing response contract for the endpoint, preferably `BaseCommandResponse<TId>` where already used.

## 4. Non-Negotiable Constraints
- Repositories return entities, never DTOs.
- Validators are manually instantiated in handlers; no injected `IValidator<T>`.
- Application handlers do not depend on `ExploreDbContext`, API, Blazor, or persistence implementations.
- Write endpoints remain `[Authorize]` and retain endpoint classification.
- Resource authorization stays in CQRS request types and existing API policies.
- HAL links remain the UI source of truth; update affordances are not gated by local role checks.
- Tenant isolation remains central; no runtime `IgnoreQueryFilters()` except a reviewed, tested soft-delete-only case.
- New and touched files get two `ABOUTME` lines.
- Do not create generic writes for read-only lookup/projection/audit/outbox/provider-owned rows.
- Do not add compatibility shims unless the user explicitly approves them.

## 5. Architecture And Design Decisions

### Decision 1: Use wrapper DTOs with nullable sub-DTO groups
- **Why:** Matches the verified `User` precedent and lets clients request partial updates without sending unrelated required fields.
- **Alternatives considered:** separate endpoint per property; JSON Patch; broad nullable DTOs.
- **Consequences:** More DTO/validator files, but clearer contracts and safer validation.
- **Files/layers affected:** `Explore.Application/DTOs/**`, command handlers, controllers, API schema/tests.

### Decision 2: Keep one command and one handler per entity
- **Why:** This is the user's requested operational model and matches the existing `User`, `Actor`, and `Event` shells.
- **Alternatives considered:** one command/handler per property.
- **Consequences:** Handlers must stay organized with small private apply methods, but MediatR registration and route contracts remain simple.
- **Files/layers affected:** `Explore.Application/Features/**/Requests/Commands`, `Explore.Application/Features/**/Handlers/Commands`.

### Decision 3: Group by independently saveable invariant, not blindly by every C# property
- **Why:** Some properties must change atomically: names, coordinates/address, schedule/time zone projections, storage image links, pricing/currency, role windows, template definition/options.
- **Alternatives considered:** one DTO per scalar property.
- **Consequences:** The implementation must document group boundaries in code/tests. Most simple fields can still be one group per property.
- **Files/layers affected:** DTOs, validators, AutoMapper profiles, handler apply methods.

### Decision 4: Preserve existing routes and only change body schemas unless a route is missing
- **Why:** Named routes and HATEOAS policies depend on current route constants. Route churn would create broad API and Blazor client impact.
- **Alternatives considered:** new `/property-name` endpoints.
- **Consequences:** Existing `PUT` endpoints become partial-update capable by body shape. API changelog must call out request schema changes.
- **Files/layers affected:** `Explore.API/Controllers/**/*.cs`, generated OpenAPI/client contracts.

### Decision 5: Inventory and migrate in feature batches
- **Why:** The repo has many entity categories and inconsistent legacy patterns. An inventory-first migration prevents exposing forbidden writes.
- **Alternatives considered:** code generator/codemod across all DTOs.
- **Consequences:** More reviewable work, lower architectural risk.
- **Files/layers affected:** all update-eligible feature slices.

## 6. Implementation Phases

### Phase 1: Update Surface Inventory And Classification
- **Goal:** Produce an implementation inventory from existing update DTOs, handlers, controllers, route names, mapping profiles, validators, repositories, cache keys, and authorization policies.
- **Depends on:** Plan approval.
- **Relevant files:** `Explore.Application/DTOs/**`, `Explore.Application/Features/**/Requests/Commands/**`, `Explore.Application/Features/**/Handlers/Commands/**`, `Explore.API/Controllers/**`, `Explore.Application/Profiles/**`, `Explore.API/Hateoas/**`, tests.
- **Acceptance criteria:** Each domain/API surface is classified as `migrate`, `already compliant`, `specialized domain action`, `read-only/excluded`, or `needs user decision`.
- **Verification:** `rg --files` inventory commands; architecture tests after any context docs changes.
- **Rollback / failure handling:** No code edits in this phase except dev-doc updates.

#### Task 1.1: Build mutable update inventory
- **Type:** investigate / docs
- **Layer:** Application / API / Docs
- **Files:** existing `Explore.Application/DTOs/**`, `Explore.Application/Features/**`, `Explore.API/Controllers/**`; update this plan/context/tasks.
- **Description:** Create a table of every existing `Update*Dto`, `Update*Command`, `Update*CommandHandler`, controller update action, response type, authorization metadata, and cache invalidation behavior.
- **Acceptance Criteria:** Every current update surface has an implementation disposition.
- **Validation:** `rg --files Explore.Application/DTOs | rg '/Update.*Dto\.cs$'`; `rg --files Explore.Application/Features | rg '/Update.*CommandHandler\.cs$'`; `rg --files Explore.API/Controllers`.

#### Task 1.2: Define exclusion rules
- **Type:** docs
- **Layer:** Architecture / Security
- **Files:** this plan/context/tasks; maybe `docs/API_CHANGELOG.md` later.
- **Description:** Record excluded rows: lookup tables without write endpoints, projections, outbox/idempotency/audit/history, provider-owned state, and lifecycle commands that are not property updates.
- **Acceptance Criteria:** No implementation task says "make every domain file writable."
- **Validation:** Review against `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, and current controllers.

### Phase 2: Establish Canonical Pattern On User And Actor
- **Goal:** Treat `User` as the reference and finish `Actor` decomposition.
- **Depends on:** Phase 1.
- **Relevant files:** `Explore.Application/DTOs/User/**`, `Explore.Application/Features/Users/**`, `Explore.Application/DTOs/Actor/**`, `Explore.Application/Features/Actors/**`, `Explore.Application/Profiles/UserMappingProfile.cs`, actor mapping profile, related tests.
- **Acceptance criteria:** User and Actor both have wrapper validators, present-group validation, no broad overlapping groups, one save, cache invalidation, and tests.
- **Verification:** focused unit tests plus architecture tests.

#### Task 2.1: Harden User reference implementation
- **Type:** modify / test
- **Layer:** Application
- **Files:** existing `Explore.Application/DTOs/User/UpdateUserDto.cs`, `UpdateUserNamesDto.cs`, `UpdateUserProfileImageDto.cs`, `Validators/UpdateUserDtoValidator.cs`, `Features/Users/Handlers/Commands/UpdateUserCommandHandler.cs`; new `Event.Application.UnitTests/Features/Users/Commands/UpdateUserCommandHandlerTests.cs`.
- **Description:** Add missing `ABOUTME` headers if needed, ensure empty-wrapper validation and cache invalidation tests, and preserve actor/storage linkage behavior.
- **Acceptance Criteria:** Names-only update changes only names; profile-image-only update changes actor/storage link; empty wrapper fails; not-found returns failure; repository save is called once.
- **Validation:** `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~UpdateUserCommandHandlerTests`.

#### Task 2.2: Decompose Actor update groups
- **Type:** modify / test
- **Layer:** Application
- **Files:** existing `Explore.Application/DTOs/Actor/UpdateActorDto.cs`, `UpdateActorAppearanceDto.cs`, `Validators/*`, `Features/Actors/Requests/Commands/UpdateActorCommand.cs`, `Features/Actors/Handlers/Commands/UpdateActorCommandHandler.cs`, actor tests.
- **Description:** Split broad actor mutable fields into independently saveable groups such as identity/display, profile image, appearance, federation identifiers, and federation metadata. Keep actor ownership immutable.
- **Acceptance Criteria:** Each group is nullable, independently validatable, and independently updatable; no duplicated field ownership between groups.
- **Validation:** `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~UpdateActorCommandHandlerTests`.

### Phase 3: Migrate Core User-Facing Aggregates
- **Goal:** Convert high-traffic aggregates and their child entities before low-risk CRUD tables.
- **Depends on:** Phase 2.
- **Relevant files:** Event, EventSession, EventAgendaItem, EventDay, EventSeries, EventRegistration, Location, LocationRoom, Organization, Group, Tenant update slices.
- **Acceptance criteria:** Each migrated entity has a wrapper update DTO, nullable groups, handler-local validators, one handler, one load, one save, explicit cache invalidation, and tests for each group.
- **Verification:** focused `Event.Application.UnitTests` filters, `Event.API.IntegrationTests` route/body tests for representative endpoints, architecture tests.

#### Task 3.1: Decompose Event update
- **Type:** modify / test / docs
- **Layer:** Application / API
- **Files:** existing `Explore.Application/DTOs/Event/UpdateEventDto.cs`, validators, `UpdateEventCommand.cs`, `UpdateEventCommandHandler.cs`, `EventController.cs`, `EventMappingProfile.cs`, related tests.
- **Description:** Split broad event fields into groups such as title/content, classification/lookups, actor/ownership references, featured image, pricing, registration, visibility, external links, series, and schedule timezone/projection. Preserve `ApplyScheduleTimeZone` when schedule-affecting fields change.
- **Acceptance Criteria:** Each group can be submitted alone; list/detail cache invalidation remains `event:detail:{id}` plus tenant list tag; event authorization remains `ResourceKinds.Event` update.
- **Validation:** existing event command tests plus new group-specific tests.

#### Task 3.2: Decompose EventSession and schedule-related children
- **Type:** modify / test
- **Layer:** Application / API
- **Files:** `Explore.Application/DTOs/EventSession/**`, `Features/EventSessions/**`, `EventSessionController.cs`, related tests.
- **Description:** Split session metadata, schedule, room/group assignment, language/speaker links, Islamic aspect, lifecycle status, and publication-specific actions without flattening lifecycle commands into property updates.
- **Acceptance Criteria:** Draft/schedule/publish invariants stay intact; parent event cache invalidation stays intact.
- **Validation:** `UpdateEventSessionCommandHandlerTests`, schedule/lifecycle tests.

#### Task 3.3: Decompose Organization, Group, Tenant, Location, LocationRoom
- **Type:** modify / test
- **Layer:** Application / API
- **Files:** corresponding DTOs, commands, handlers, controllers, profiles, tests.
- **Description:** Convert broad profile/details DTOs into groups such as names, contact, address/geo, approval/status, appearance, policy/settings, navigation links, and storage settings where applicable.
- **Acceptance Criteria:** Existing admin-only status/policy boundaries remain separate or explicitly authorized groups; no tenant or org admin authority broadens.
- **Validation:** focused unit tests plus `AuthorizationIntegrationTests` for representative write routes.

### Phase 4: Migrate Simple CRUD And Link Entities
- **Goal:** Convert remaining update-eligible simple entities with lower domain complexity.
- **Depends on:** Phase 3.
- **Relevant files:** Category, Tag, CategoryTypeCategories, TagTypeTags, StorageObject, SyncState, IndexedDid, AtprotoRecord, UserExternalLogin, UserAuthenticationToken, templates, custom-property definitions, footer link/groups.
- **Acceptance criteria:** Every mutable field has an approved group; link/junction writes go through repositories; cache invalidation matches existing query keys.
- **Verification:** focused unit tests and representative API integration tests.

#### Task 4.1: Convert simple catalog entities
- **Type:** modify / test
- **Layer:** Application / API
- **Files:** `Category`, `Tag`, `Location`, `LocationRoom`, `StorageObject`, `SyncState`, `IndexedDid`, `AtprotoRecord` slices.
- **Description:** Convert full DTOs into wrapper/sub-DTO contracts while preserving existing controller route names.
- **Acceptance Criteria:** Each field/group updates independently and empty wrappers fail validation.
- **Validation:** feature-specific unit tests; API schema tests.

#### Task 4.2: Convert relationship/link update surfaces
- **Type:** modify / test
- **Layer:** Application / Persistence as needed
- **Files:** `EventCategories`, `EventTags`, `EventSessionLanguage`, `EventSessionSpeaker`, `CategoryTypeCategories`, `TagTypeTags`, member-role update slices.
- **Description:** Keep repository-mediated link writes and avoid direct navigation collection mutation.
- **Acceptance Criteria:** Link updates remain tenant-safe and repository-owned.
- **Validation:** existing and new unit/integration tests around link updates.

### Phase 5: API Contract, HAL, Client, And Docs Alignment
- **Goal:** Make public contracts and client expectations match the new wrapper DTO schemas.
- **Depends on:** Phases 2-4.
- **Relevant files:** `Explore.API/Controllers/**`, `Explore.API/Hateoas/**`, `RouteNames`, generated OpenAPI/client outputs if present, Blazor API service code if compile breaks, `docs/API_CHANGELOG.md`.
- **Acceptance criteria:** Existing route names stay stable, write endpoints remain authorized, request schemas show nullable groups, HAL update links continue to point to the same routes, Blazor compiles.
- **Verification:** `Event.API.IntegrationTests`, architecture tests, generated OpenAPI parity tests.

#### Task 5.1: Update controllers to accept wrapper DTOs consistently
- **Type:** modify / test
- **Layer:** API
- **Files:** update actions in `Explore.API/Controllers/**/*.cs`.
- **Description:** Ensure route ID/body ID checks use wrapper `Id`, command construction uses the new wrapper, and error metadata remains explicit.
- **Acceptance Criteria:** No write endpoint loses `[Authorize]`; route names unchanged unless approved; validation errors return ProblemDetails/command validation problem consistently.
- **Validation:** `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`.

#### Task 5.2: Update API docs and client generation
- **Type:** docs / generated contract
- **Layer:** Docs / API / Blazor
- **Files:** `docs/API_CHANGELOG.md`; generated client artifacts if the repo's OpenAPI workflow requires them.
- **Description:** Document breaking request-body schema changes and regenerate typed clients only through the existing project workflow.
- **Acceptance Criteria:** API changelog names the new wrapper/sub-DTO pattern and migration notes for clients.
- **Validation:** OpenAPI parity/contract tests and Blazor build.

### Phase 6: Cross-Cutting Verification And Cleanup
- **Goal:** Prove the migration is complete and enforceable.
- **Depends on:** Phases 1-5.
- **Relevant files:** tests, architecture tests, dev docs.
- **Acceptance criteria:** Build green; test projects pass individually; no touched C# file violates `ABOUTME`; no update handler injects validators; no repository returns DTOs; dev docs reflect final state.
- **Verification:** see Section 7 and Section 14.

## 7. Testing Strategy
- Unit tests per migrated handler: present group updates only intended fields; absent groups do not mutate; empty wrapper fails; invalid lookup fails; not-found fails; cache invalidation called; one repository update call where feasible.
- Validator tests: wrapper requires ID and at least one group; group validators enforce old full-DTO constraints for their owned fields.
- API integration tests: representative endpoints accept group-only bodies, reject ID mismatch, reject unauthenticated writes, and keep route names/OpenAPI contracts stable.
- Architecture tests: Clean Architecture, CQRS pattern, endpoint contract, route naming, context schema.
- Blazor/client compile: required if generated DTO shapes are consumed by Blazor.

Minimum commands:
```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

## 8. Documentation, Configuration, And Operations Impact
- Update `docs/API_CHANGELOG.md` for request schema changes.
- Update these dev docs after each implementation slice.
- No expected EF migrations unless implementation discovers missing persistence fields or necessary concurrency columns.
- No expected Aspire/Docker/config changes.
- Generated API clients may need regeneration if the repo currently checks them in.

## 9. Security, Authorization, Privacy, And Abuse Considerations
- Writes remain `[Authorize]`.
- CQRS resource authorization remains on update commands via `IAuthorizedRequest`, `[AuthorizeResource]`, or `ISecureRequest`.
- User/self-service updates must continue to authorize against the target user resource.
- Tenant isolation is repository/EF-filter enforced; no broad `IgnoreQueryFilters()`.
- PII-bearing groups, such as user names, organization/location PII, emails, and profile images, need focused validators and no sensitive logging.
- Idempotency remains middleware-provided for write endpoints; do not bypass it with alternate routes.
- HAL links stay the client-side source of truth for update affordances.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations
- **Multi-tenancy:** Applicable. Tenant-scoped entities must keep tenant context and query filters intact.
- **Federation:** Applicable for `Actor`, `AtprotoRecord`, DID, PDS-related metadata. Do not let local property updates corrupt provider-owned or protocol-owned fields.
- **Localization:** Applicable where update DTOs include localized display names, culture, timezone, or settings. Validators should preserve existing culture/timezone rules.
- **Accessibility:** Not directly applicable unless Blazor edit forms change later. If UI is updated, use HAL affordances and standard form validation.
- **Product:** Applicable. This is a client ergonomics and correctness change: clients can save one field/group without resending a full stale object.

## 11. Observability And Operations
- Keep existing request logging and ProblemDetails behavior.
- Do not log raw DTO payloads, PII, storage object keys, JWTs, API keys, or provider details.
- Cache invalidation failures should follow current handler exception behavior; do not swallow unless a local precedent exists.
- No new health checks or metrics are required unless implementation adds background migration/backfill work, which is not expected.

## 12. Migration And Compatibility Plan
This is a pre-v1 project, so do not add compatibility shims by default. The change is API request-body breaking for clients that currently send broad update DTOs. Preserve routes and route names to minimize blast radius, update API changelog, regenerate clients if applicable, and update Blazor compile errors as part of the implementation.

No data migration is expected because the change is request-contract and handler-application behavior, not storage schema.

## 13. Risk Register
| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| "All entities" interpreted too broadly and exposes internal rows. | Medium | High | Inventory/exclusion phase; require existing API/update intent before adding writes. | New controller/action for lookup/outbox/audit without policy. | Task 1.2 |
| Null semantics become ambiguous for nullable fields. | High | High | Use present group as update intent; define clear-null pattern per group before implementation. | Tests cannot distinguish omit versus clear. | Tasks 2-4 |
| Validators lose old constraints during DTO split. | Medium | High | Port every old rule into exactly one group validator; add validator tests. | Existing invalid payload starts passing. | Tasks 2-4 |
| Cache invalidation becomes stale or too broad. | Medium | Medium | Inventory existing keys/tags and assert invalidation in tests. | Detail/list reads stale after update. | Tasks 2-4 |
| Handler becomes too large. | Medium | Medium | Use small private `ApplyX` methods and group validators; do not create extra handlers. | Hard-to-review handler with duplicated logic. | Tasks 2-4 |
| API/Blazor generated clients break late. | Medium | Medium | Run build and API integration tests after each batch; regenerate clients through project workflow. | Compile errors or OpenAPI parity failures. | Task 5.2 |

## 14. Success Metrics And Definition Of Done
- Every update-eligible entity has an inventory disposition.
- Every migrated entity has a wrapper update DTO with nullable sub-DTO groups.
- Every migrated handler manually instantiates validators, validates only present groups, loads entity once, saves once, and invalidates relevant cache.
- No repository returns DTOs or exposes `IQueryable`.
- Existing write endpoints remain authorized and route names remain stable unless approved.
- Tests prove at least one group-only update per migrated entity and empty-wrapper rejection.
- Required commands pass:
```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

## 15. Implementation Agent Contract - KEEP DEV DOCS CURRENT
Future agents implementing this plan MUST:
1. Read this plan, `full-property-update-sub-dto-context.md`, and `full-property-update-sub-dto-tasks.md` before editing.
2. Start with the inventory task unless the user explicitly directs a narrower slice.
3. Update all three dev docs after each meaningful implementation slice.
4. Preserve Clean Architecture, manual validators, entity-returning repositories, and HAL affordance rules.
5. Do not report completion unless docs, tests, and current state agree.
6. In final implementation summaries, teach the user what changed: DTO shape, validators, command/handler flow, mapping, cache invalidation, authorization, tests, and remaining work.

## 16. Progress Reporting Contract
Use this structure after implementation slices:
- **Implemented:** explain the DTO groups, command/handler control flow, validation, mapping, cache invalidation, and API route behavior.
- **Verified:** exact build/tests run.
- **Remaining:** incomplete entity batches or deferred risks.
- **Next:** next concrete slice.
- **Docs updated:** yes/no with reason.

## 17. Potential Risks & Unknowns
The hardest part is not creating DTO classes; it is preserving semantics while splitting broad DTOs. Nullable group presence solves partial update intent, but nullable fields inside a present group need deliberate clear semantics. Event/session scheduling, storage-object relationships, organization/group/tenant authority, and federation-owned actor metadata are the highest-risk areas because updates can trigger projections, cache/list visibility, authorization scope, or external protocol expectations.
