<!-- ABOUTME: Task checklist for the full-property update sub-DTO migration workstream. -->
<!-- ABOUTME: Tracks completed slices, validation evidence, and remaining implementation surfaces. -->

# Full Property Update Sub-DTO Pattern - Task Checklist

Last Updated: 2026-06-28 Europe/Brussels

## Status Summary
- **Overall status:** Phase 1 foundation complete; User, Actor, Category, Location, LocationRoom, Organization, Group, Event, EventSession, EventAgendaItem, EventDay, EventSeries, EventRegistration, EventSessionLanguage, EventCategories, EventTags, and EventSessionSpeaker update contracts hardened
- **Completed:** 20/31
- **Current priority:** Start Phase 5 tenant/settings/template/custom-property surfaces.
- **Next recommended slice:** Begin Task 5.1 with tenant/settings/navigation/footer/storage surfaces.

## Implementation Maintenance Rules
- [ ] Before starting work, read plan/context/tasks.
- [ ] After each completed task, update this checklist immediately.
- [ ] If implementation changes scope or architecture, update the plan before continuing.
- [ ] If discoveries affect future work, update the context file.
- [ ] Final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.

## Phase 0: Plan Review And Baseline
- [x] **0.1 Confirm re-baselined plan status**
  - **Files:** this plan/context/tasks.
  - **Acceptance:** implementation agent records that CTO feedback is incorporated and user chose PATCH/no compatibility.
  - **Validation:** context handoff updated.
  - **Effort:** S
  - **Dependencies:** none.
- [x] **0.2 Confirm current repo state before first product edit**
  - **Files:** `Explore.Application/DTOs/**`, `Explore.Application/Features/**`, `Explore.API/Controllers/**`, `Explore.API/Hateoas/**`.
  - **Acceptance:** no stale planning assumptions are used blindly.
  - **Validation:** rerun inventory commands from plan Section 6.
  - **Effort:** S
  - **Dependencies:** 0.1.

## Phase 1: Inventory, Foundation, And Contract Baseline
- [x] **1.1 Build CTO-grade mutable update inventory**
  - **Files:** existing DTOs, features, controllers, HAL policies, repositories, tests; update dev docs.
  - **Acceptance:** table includes surface, current DTO, handler, controller route/verb, route name, auth, group authority, cache matrix, concurrency, transaction, audit, OpenAPI/client impact, disposition, notes.
  - **Validation:** `rg --files Explore.Application/DTOs | rg '/Update.*Dto\.cs$'`; `rg --files Explore.Application/Features | rg '/Update.*CommandHandler\.cs$'`; `rg --files Explore.API/Controllers`; inventory persisted in `dev/next/full-property-update-sub-dto/full-property-update-sub-dto-inventory.md`.
  - **Effort:** L
  - **Dependencies:** 0.2.
- [x] **1.2 Define exclusion and domain-action governance**
  - **Files:** dev docs; later `docs/API_CHANGELOG.md`.
  - **Acceptance:** publish/archive/cancel/moderation/schedule/template-sync/settings-batch/revoke/rotate/invite/role/member/ownership actions are explicitly retained as domain commands where appropriate.
  - **Validation:** reviewed `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, and current controller/handler surfaces for event lifecycle, moderation/redaction, event role assignment/ownership, template sync, purge/reset, and secret rotation; retained action governance recorded in `full-property-update-sub-dto-inventory.md`.
  - **Effort:** M
  - **Dependencies:** 1.1.
- [x] **1.3 Establish `OptionalUpdate<T>` clear-null foundation**
  - **Files:** likely new `Explore.Application/Models/Common/OptionalUpdate.cs` or inventory-approved location; validators/tests.
  - **Acceptance:** missing field, explicit set, and explicit clear are distinguishable; present group with no operations fails.
  - **Validation:** `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet` passes. `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/OptionalUpdateTests/*" --minimum-expected-tests 1` passes 7/7. `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/SyncUserCommandHandlerTests/*" --minimum-expected-tests 1` passes 3/3 after removing stale `UserDto.Username` initializers.
  - **Effort:** M
  - **Dependencies:** 1.1.
- [x] **1.4 Establish PATCH route and route-ID policy**
  - **Files:** controller conventions, `RouteNames`, API tests, HAL policy notes.
  - **Acceptance:** property updates use `PATCH /api/{resource}/{id}`; body IDs removed; old broad `PUT` property update bodies intentionally rejected.
  - **Validation:** User representative migrated to `PATCH /api/user/{id}` with route ID passed into `UpdateUserCommand.UserId`; `UpdateUserDto.Id` removed; API tests cover unauthenticated PATCH and old `PUT /api/user` method rejection.
  - **Effort:** M
  - **Dependencies:** 1.1.
- [x] **1.5 Baseline OpenAPI/generated-client workflow**
  - **Files:** API project, OpenAPI generation scripts/config, generated client artifact locations, docs.
  - **Acceptance:** exact OpenAPI generation command and artifact path are recorded; baseline can be regenerated/diffed.
  - **Validation:** `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` regenerated `schemas/openapi.json`; `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/ApiContractInventory_Generate_WritesMarkdownToDocs" --minimum-expected-tests 1` passed; `dotnet nswag run nswag.json` from `Explore.Blazor.Client/` regenerated `Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
  - **Effort:** M
  - **Dependencies:** 1.4.
- [x] **1.6 Establish concurrency strategy**
  - **Files:** Domain aggregate roots, EF configurations/migrations, exception handling, tests.
  - **Acceptance:** major aggregates use the inventory-approved existing convention where present; API uses `If-Match`; commands receive expected concurrency value; conflicts map to stable ProblemDetails.
  - **Validation:** User now implements `IConcurrencyAware`; `UserConfiguration` maps `ConcurrencyStamp` as an EF concurrency token; migration `AddUserConcurrencyStamp` backfills a non-empty stamp; `UserController` requires strong `If-Match`; `UpdateUserCommand.ExpectedConcurrencyStamp` reaches the handler; stale stamps throw `ConcurrencyConflictException`; focused Application/API/Blazor tests were added.
  - **Effort:** L
  - **Dependencies:** 1.1.
- [x] **1.7 Establish transaction/unit-of-work strategy**
  - **Files:** Application contracts, Persistence implementation, multi-repository handlers/tests.
  - **Acceptance:** any handler that modifies more than one aggregate/repository has explicit transaction/unit-of-work support.
  - **Validation:** Existing `EfCoreUnitOfWorkTests` cover commit, forced-failure rollback, generic return, nested transaction rejection, and concurrency exception translation against real Postgres. User profile image update now adopts `IUnitOfWork.ExecuteInTransactionAsync` for the User + Actor + StorageObject mutation and saves once through the user repository.
  - **Effort:** L
  - **Dependencies:** 1.1.
- [x] **1.8 Establish audit requirements for sensitive groups**
  - **Files:** audit infrastructure if present, handlers/tests/dev docs.
  - **Acceptance:** sensitive groups are tagged as audit-required or explicitly excluded with rationale.
  - **Validation:** Existing `AuditLog` usage was reviewed. `AuditLog` is tenant-scoped and `IAuditLogRepository.Create` saves immediately, so current self-service User `Names` and `ProfileImage` groups are classified as `IAuditableEntity` timestamp-audited only to preserve the single-save User update contract and avoid storing raw PII values. Tenant-scoped/admin-sensitive groups remain structured-audit-required in the inventory.
  - **Effort:** M
  - **Dependencies:** 1.1.

## Phase 2: Reference Implementation And Low-Risk Pilot
- [x] **2.1 Harden User reference implementation**
  - **Files:** `Explore.Application/DTOs/User/**`, `Explore.Application/Features/Users/**`, `Explore.API/Controllers/UserController.cs`, `Explore.Application/Profiles/UserMappingProfile.cs`, user tests.
  - **Acceptance:** PATCH route; route ID authoritative; no body ID; `OptionalUpdate<T>` for clearable fields; empty groups fail; transaction-protected Actor/StorageObject update; cache matrix tested; concurrency disposition applied.
  - **Validation:** route-ID PATCH, no body ID, empty wrapper rejection, strong `If-Match`, User concurrency stamp, User/Actor/StorageObject unit-of-work transaction, one final user repository save, cache invalidation-after-save, API missing-`If-Match` rejection, generated-client `If-Match` forwarding, and audit classification are implemented. User has no nullable/clearable update field in the current groups, so `OptionalUpdate<T>` is not applied inside User yet.
  - **Effort:** L
  - **Dependencies:** 1.3, 1.4, 1.6, 1.7.
- [x] **2.2 Decompose Actor update groups**
  - **Files:** `Explore.Application/DTOs/Actor/**`, `Explore.Application/Features/Actors/**`, actor controller/profile/tests.
  - **Acceptance:** identity/display, profile image, appearance, federation identifiers, federation metadata groups; group auth for federation/provider-sensitive metadata; storage updates transaction-protected.
  - **Validation:** `UpdateActorDto` now exposes `Profile`, `ProfileImage`, `Appearance`, `FederationIdentifiers`, and `FederationMetadata`; `OptionalUpdate<T>` is used for clearable fields; `ActorController` uses `PATCH /api/actor/{id}` with required strong `If-Match`; `Actor` has an EF concurrency token and `AddActorConcurrencyStamp`; handler validates manually, loads once, checks group authorization via `IAuthorizationProvider.IsAllowedBatchAsync` with `actorUpdateGroup` metadata, checks concurrency, updates tracked storage objects in `IUnitOfWork`, saves once, then invalidates `actor:detail:{id}`; HAL detail exposes `edit` as `PATCH`; OpenAPI/inventory/NSwag regenerated. `UpdateActorCommandHandlerTests` passed 7/7 including unauthorized mixed-payload atomic failure; `ActorControllerTests` passed 11/11; full build passed.
  - **Effort:** L
  - **Dependencies:** 2.1.
- [x] **2.3 Migrate Category or Tag as low-risk CRUD pilot**
  - **Files:** selected Category/Tag DTOs/features/controllers/HAL/tests/OpenAPI docs.
  - **Acceptance:** validates DTO shape, validators, PATCH controller, old body rejection, cache matrix, OpenAPI schema, generated client behavior.
  - **Validation:** Category selected as the pilot. `UpdateCategoryDto` now has `MasterCode`, `FullName`, and `Parent` groups; `Parent.ParentId` uses `OptionalUpdate<Guid?>`; `Category` implements `IConcurrencyAware`; migration `AddCategoryConcurrencyStamp` backfills a non-empty token; `CategoryController` uses `PATCH /api/category/{id}` with required strong `If-Match`; HAL edit emits `PATCH`; handler manually validates, loads once, checks concurrency, applies explicit property groups, saves once, and invalidates `categories:list:1:20`. Focused handler tests passed 5/5, `CategoryControllerTests` passed 11/11, Blazor Category/Admin service tests passed 14/14, API build passed, OpenAPI/inventory/NSwag were regenerated.
  - **Effort:** M
  - **Dependencies:** 2.2.
- [x] **2.4 Update API changelog for reference and pilot contracts**
  - **Files:** `docs/API_CHANGELOG.md`.
  - **Acceptance:** includes old `PUT`/body-ID examples and new `PATCH`/route-ID examples.
  - **Validation:** User, Actor, and Category breaking update contracts are documented with route/body/concurrency/generated-client/migration guidance.
  - **Effort:** S
  - **Dependencies:** 2.3.

## Phase 3: Medium-Risk Core Aggregates
- [x] **3.1 Migrate Location and LocationRoom**
  - **Files:** `Explore.Application/DTOs/Location/**`, `LocationRoom/**`, features, controllers, tests.
  - **Acceptance:** name/address/geo/room groups; PII/tenant rules preserved; cache matrix verified.
  - **Validation:** `UpdateLocationCommandHandlerTests` and `UpdateLocationRoomCommandHandlerTests` compiled and passed within the Application suite except for one unrelated `CreateEventDraftAiActionMapperTests.Map_WhenPayloadContainsIncompleteStructuredDetails_SkipsInvalidNestedRows` failure in the broader project run; `LocationControllerTests` and `LocationRoomControllerTests` passed 11/11; focused Blazor Location/Admin/LocationRoom service tests passed 18/18; API build, API contract inventory regeneration, and NSwag regeneration passed.
  - **Effort:** M
  - **Dependencies:** 2.3.
- [x] **3.2 Migrate Organization**
  - **Files:** `Explore.Application/DTOs/Organization/**`, `Explore.Application/Features/Organizations/**`, `Explore.API/Controllers/OrganizationController.cs`, tests.
  - **Acceptance:** details/property groups separated from approval/status domain action or admin-only group; group auth; sensitive groups audited.
  - **Validation:** `UpdateOrganizationCommandHandlerTests` added for empty wrapper, OrgAdmin group authorization, stale concurrency, single-group update, explicit website clear, no-op website group rejection, save/cache behavior. `OrganizationControllerTests` and `AuthorizationIntegrationTests` updated for PATCH/no old PUT. `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet`, API contract inventory generation, and NSwag regeneration passed.
  - **Effort:** L
  - **Dependencies:** 3.1.
- [x] **3.3 Migrate Group**
  - **Files:** `Explore.Application/DTOs/Group/**`, `Explore.Application/Features/Groups/**`, `Explore.API/Controllers/GroupController.cs`, tests.
  - **Acceptance:** metadata, approval/status, membership-adjacent groups stay authorized and tenant-safe.
  - **Validation:** `GroupHierarchyCommandHandlerTests` passed 10/10; `GroupControllerTests` passed 7/7; `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet`, API contract inventory regeneration, and NSwag regeneration passed.
  - **Effort:** L
  - **Dependencies:** 3.2.

## Phase 4: High-Risk Event And Program Aggregates
- [x] **4.1 Decompose Event update**
  - **Files:** `Explore.Application/DTOs/Event/**`, `Explore.Application/Features/Events/**`, `Explore.API/Controllers/EventController.cs`, `Explore.Application/Profiles/EventMappingProfile.cs`, event tests.
  - **Acceptance:** title/content, classification, actor/ownership, featured image, pricing, registration, visibility, external links, series, and timezone/projection groups; group auth; concurrency; cache matrix for detail/list/search/calendar/HAL.
  - **Validation:** `UpdateEventDto` now exposes property groups and `OptionalUpdate<T>` clear semantics for nullable values; `UpdateEventCommand` carries route `EventId`, `ExpectedConcurrencyStamp`, and grouped payload; `UpdateEventCommandHandler` manually validates, loads once through `GetScheduleGraphForUpdateAsync`, checks concurrency, applies present groups through private methods, preserves schedule timezone projection, saves once, and invalidates `event:detail:{id}` plus `CacheTags.EventListByTenant(tenantId)`. `EventController` now uses `PATCH /api/event/{id}` with required strong `If-Match`, and HAL edit links emit `PATCH`. Focused validation passed: `EventListCacheInvalidationCommandHandlerTests` 2/2; `EventServiceTests` 59/59; `Update_WithoutIfMatch_ReturnsBadRequest` 1/1; `Update_WithOldPutRoute_ReturnsMethodNotAllowed` 1/1; `ApiContractInventory_Generate_WritesMarkdownToDocs` 1/1; `Explore.Blazor.Client.Tests` and `Event.API.IntegrationTests` builds passed. Full `EventControllerRealRuntimeTests` class still has an unrelated existing create-draft persistence failure: `Create_WithDraftWithoutSessions_ReturnsCreatedAndPersistsEmptyProgramDraft` expected 0 sessions and found 1.
  - **Effort:** XL
  - **Dependencies:** 3.3.
- [x] **4.2 Decompose EventSession update**
  - **Files:** `Explore.Application/DTOs/EventSession/**`, `Explore.Application/Features/EventSessions/**`, `Explore.API/Controllers/EventSessionController.cs`, event session tests.
  - **Acceptance:** metadata, schedule, room/group assignment, language/speaker links, Islamic aspect; lifecycle actions stay separate where appropriate.
  - **Validation:** `UpdateEventSessionDto` now exposes nullable groups for parent event, schedule, location, featured image, room, sort order, title, kind, description, slug, max audience attendees, registration mode, price, currency code, and Islamic aspect. `UpdateEventSessionCommand` carries route `EventSessionId`, `ExpectedConcurrencyStamp`, and grouped payload. `UpdateEventSessionCommandHandler` manually validates, loads once, checks concurrency, rejects cross-tenant parent events, applies explicit groups, reprojects schedule/day links when schedule or parent event changes, updates session plus Islamic aspect inside `IUnitOfWork`, maps room overlap conflicts, saves once, and invalidates affected event detail/list caches. `EventSessionController` now uses `PATCH /api/eventsession/{id}` with required strong `If-Match`, and HAL edit links emit `PATCH`. Focused validation passed: `UpdateEventSessionCommandHandlerTests` 4/4; `EventServiceTests.UpdateSessionAsync_MapsComposerRequestToGeneratedDto` 1/1; `EventSessionControllerTests` 17/17; `ApiContractInventory_Generate_WritesMarkdownToDocs` 1/1; Application/API/Blazor test project builds passed; NSwag regenerated `Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
  - **Effort:** XL
  - **Dependencies:** 4.1.
- [x] **4.3 Migrate Event child and relationship surfaces**
  - **Files:** EventAgendaItem, EventDay, EventSeries, EventRegistration, EventCategories, EventTags, EventSessionLanguage, EventSessionSpeaker.
  - **Acceptance:** relationship writes repository-mediated; parent event cache invalidation verified; no navigation collection mutation.
  - **Validation:** focused Application/API/Blazor tests per public-route slice; Application/Persistence builds and focused Application tests for Application-only link surfaces.
  - **Slice progress:** EventAgendaItem is implemented as a grouped `PATCH /api/eventagendaitem/{id}` contract with route ID authority, required strong `If-Match`, nullable groups for event/title/description/schedule/location/room/kind/sort order, `OptionalUpdate<T>` clear semantics for description/location/room/kind, manual handler validation, one entity load, concurrency check, tenant-safe parent event/location/room validation, schedule projection/day relinking, one repository save, and affected parent event cache invalidation. EventDay is implemented as grouped `PATCH /api/eventday/{id}` with route ID authority, required strong `If-Match`, nullable groups for event/localDate/label/description/bannerText/bannerImage/publication/sortOrder/registration, `OptionalUpdate<T>` clear semantics for label/description/bannerText/bannerImage, manual validation, one entity load, concurrency check, tenant-safe parent event validation, event-day date uniqueness, one repository save, and affected parent event cache invalidation. EventSeries is implemented as grouped `PATCH /api/eventseries/{id}` with route ID authority, required strong `If-Match`, nullable groups for title/description/slug/featuredImage/publication, `OptionalUpdate<T>` clear semantics for description/slug/featuredImage, manual validation, one entity load with child events, concurrency check, one repository save, and affected child event/detail-list cache invalidation. EventRegistration is implemented as grouped `PATCH /api/eventregistration/{id}` with route ID authority, required strong `If-Match`, nullable groups for user/session/intent/approvalStatus/atprotoRecord, `OptionalUpdate<T>` clear semantics for intent/approvalStatus/atprotoRecord, manual validation, one entity load, concurrency check, tenant-safe session reassignment, intent/event consistency validation, one repository save, and affected parent event cache invalidation. EventSessionLanguage is implemented as grouped `PATCH /api/eventsessionlanguage/{id}` with route ID authority, required strong `If-Match`, nullable groups for session/language, manual validation, one entity load, concurrency check, tenant-safe session reassignment, duplicate session-language validation, one repository save, and affected parent event cache invalidation. EventCategories, EventTags, and EventSessionSpeaker have no public API controllers in the current codebase, but their Application update commands are now grouped, route-ID/expected-stamp shaped, manually validated, repository-mediated, concurrency checked, duplicate checked, saved once, and parent-event cache invalidating; `AddEventCategoryAndTagConcurrencyStamps` and `AddEventSessionSpeakerConcurrencyStamp` add non-empty concurrency stamps to those junction tables. EventAgendaItem, EventDay, EventRegistration, and EventSessionLanguage HAL/API route names now emit or expose `PATCH`; EventSeries has no dedicated HAL link policy in the current codebase. Blazor/generated clients send quoted concurrency stamps where consumed, and OpenAPI/NSwag artifacts were regenerated for public-route slices. Task 4.3 is complete.
  - **Effort:** L
  - **Dependencies:** 4.2.

## Phase 5: Tenant, Settings, Templates, Custom Properties, And Remaining Mutables
- [ ] **5.1 Migrate Tenant/settings/navigation/footer/storage surfaces**
  - **Files:** Tenant, TenantStorageSettings, Footer, Settings feature slices/controllers/tests.
  - **Acceptance:** governance locks, tenant isolation, group auth, audit, cache invalidation, concurrency/transaction strategy applied.
  - **Validation:** focused unit/API/persistence tests.
  - **Effort:** XL
  - **Dependencies:** 4.3.
- [ ] **5.2 Migrate template and custom-property definition updates**
  - **Files:** EventTemplate, EventSessionTemplate, CustomPropertyDefinition, EventCustomProperty, EventSessionCustomProperty DTO/features/controllers/tests.
  - **Acceptance:** definition metadata/options/sync-sensitive updates preserve template sync, projection, concurrency, and audit semantics.
  - **Validation:** existing concurrency tests plus new group tests.
  - **Effort:** XL
  - **Dependencies:** 5.1.
- [ ] **5.3 Migrate remaining simple/specialized mutables from inventory**
  - **Files:** StorageObject, SyncState, IndexedDid, AtprotoRecord, UserExternalLogin, UserAuthenticationToken, ExternalApiKey policy, Localization, Appearance, remaining classified surfaces.
  - **Acceptance:** all update-eligible surfaces handled or explicitly deferred with rationale.
  - **Validation:** focused tests per surface.
  - **Effort:** L
  - **Dependencies:** 5.2.
- [ ] **5.4 Record and test domain actions retained outside property updates**
  - **Files:** dev docs; tests/docs if discovered.
  - **Acceptance:** retained action commands are documented with reason, auth, audit, cache invalidation, and tests.
  - **Validation:** dev docs review and relevant action tests.
  - **Effort:** M
  - **Dependencies:** 5.3.

## Phase 6: API Contract, HAL, Client, And Docs Finalization
- [ ] **6.1 Verify HAL update links target PATCH routes**
  - **Files:** `Explore.API/Hateoas/**`, `RouteNames`, controller routes.
  - **Acceptance:** edit/update affordance links remain server-authorized and point to canonical PATCH routes.
  - **Validation:** HATEOAS/API integration tests.
  - **Effort:** M
  - **Dependencies:** Phases 2-5.
- [ ] **6.2 Regenerate/diff OpenAPI and generated clients**
  - **Files:** OpenAPI artifacts, generated clients, Blazor services as needed.
  - **Acceptance:** request schemas show nullable groups and `OptionalUpdate<T>` correctly; old broad DTO contract removed; generated client compiles.
  - **Validation:** OpenAPI parity/contract tests and build.
  - **Effort:** L
  - **Dependencies:** 6.1.
- [ ] **6.3 Update API docs, changelog, and self-hoster release notes**
  - **Files:** `docs/API_CHANGELOG.md`, possibly `docs/API.md`, release/self-hosting notes if migrations exist.
  - **Acceptance:** breaking changes, old/new examples, concurrency/If-Match, and migration/reset notes documented.
  - **Validation:** docs review.
  - **Effort:** M
  - **Dependencies:** 6.2.

## Phase 7: Cross-Cutting Verification
- [ ] **7.1 Run Application unit test suite**
  - **Validation:** `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`.
  - **Dependencies:** implementation batches complete.
- [ ] **7.2 Run API integration test suite**
  - **Validation:** `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`.
  - **Dependencies:** 7.1.
- [ ] **7.3 Run Persistence integration tests if concurrency/transaction migrations changed persistence**
  - **Validation:** `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`.
  - **Dependencies:** 7.2.
- [ ] **7.4 Run architecture/context tests**
  - **Validation:** `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`.
  - **Dependencies:** 7.3.
- [ ] **7.5 Run release build**
  - **Validation:** `dotnet build --configuration Release --verbosity quiet`.
  - **Dependencies:** 7.4.
- [ ] **7.6 Refresh dev docs and final handoff**
  - **Files:** plan/context/tasks.
  - **Acceptance:** docs reflect final implementation state, validation, remaining work, and handoff.
  - **Validation:** manual review against completed work.
  - **Dependencies:** 7.5.

## Verification Checklist
- [ ] LSP diagnostics clean for modified files.
- [ ] OpenAPI baseline/regeneration command recorded and used.
- [ ] `PATCH` routes and HAL links verified.
- [ ] Old broad `PUT` update shapes rejected.
- [ ] Group-level authorization tests pass.
- [ ] Concurrency conflict tests pass where required.
- [ ] Multi-repository transaction rollback tests pass where required.
- [ ] Cache invalidation matrix tests pass.
- [ ] Audit tests pass for sensitive groups.
- [ ] `dotnet build --configuration Release --verbosity quiet` passes.
- [ ] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passes.
- [ ] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` passes.
- [ ] `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passes if persistence changed.
- [ ] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passes.
- [ ] API changelog and release notes updated.
- [ ] Dev docs refreshed with final state and remaining work.

## Remaining / Deferred Work
- OpenAPI generation, API contract inventory regeneration, and NSwag client regeneration were executed for the User PATCH contract slice.
- Existing transaction/unit-of-work abstraction is verified as present, rollback-tested in Persistence integration tests, and adopted by the User profile update handler; per-handler migration usage remains to be enforced during later entity migrations.
- Existing concurrency conventions are verified as partial/inconsistent; canonical PATCH/If-Match behavior is implemented for User and remains to be rolled out per inventory.
- Existing audit infrastructure is verified as present; User self-service groups are timestamp-audited only, while tenant-scoped/admin-sensitive groups remain structured-audit-required per inventory.
- `OptionalUpdate<T>` foundation is implemented, Application build-verified, and focused Application unit tests pass.
- No backward compatibility support is planned for old `PUT` property update bodies.
- Slice verification passed: focused OptionalUpdate tests from the previous foundation slice; UpdateUser command contract/handler tests 5/5; UserController integration tests 7/7; UserService tests 16/16; API contract inventory generation 1/1; Architecture tests 197/198 with one existing skipped test; full Release build.
- Persistence integration was attempted because this slice added an EF migration. It failed 1/179 on `EventSessionStatus_ShouldSeedAllEightRows` expecting 8 rows but finding 10; this is unrelated to the User concurrency migration and should be handled separately.
- Current Task 4.3 sub-slice verification: EventAgendaItem, EventDay, EventSeries, and EventRegistration project builds passed for Application, API, Blazor client, Blazor client tests, Application unit tests, and API integration tests; `dotnet nswag run nswag.json` passed from `Explore.Blazor.Client/`; focused EventRegistration handler tests passed 6/6, validator tests passed 4/4, API controller PATCH/If-Match tests passed, Blazor update service tests passed 2/2, and API contract inventory regeneration passed. Full Application tests previously failed only on unrelated `CreateEventDraftAiActionMapperTests.Map_WhenPayloadContainsIncompleteStructuredDetails_SkipsInvalidNestedRows`; full API integration tests previously failed in unrelated existing areas including ProblemDetails snapshots, storage settings network calls, generated smoke PUT expectations, template sync metadata, AI flow publishing setup, controller null-HttpContext problem factory paths, actor subscription/user external login JSON errors, and organization HAL auth; full Blazor client tests previously failed in unrelated existing UI/service-registration areas.
