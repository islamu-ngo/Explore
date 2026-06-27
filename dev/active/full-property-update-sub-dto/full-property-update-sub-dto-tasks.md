# Full Property Update Sub-DTO Pattern - Task Checklist

Last Updated: 2026-06-27 Europe/Brussels

## Status Summary
- **Overall status:** Draft
- **Completed:** 0/24
- **Current priority:** User review, then Phase 1 inventory.
- **Next recommended slice:** Task 1.1 - build mutable update inventory.

## Implementation Maintenance Rules
- [ ] Before starting work, read plan/context/tasks.
- [ ] After each completed task, update this checklist immediately.
- [ ] If implementation changes scope or architecture, update the plan before continuing.
- [ ] If discoveries affect future work, update the context file.
- [ ] Final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.

## Phase 0: Plan Review And Baseline
- [ ] **0.1 User reviews plan and approves or corrects scope**
  - **Files:** this plan/context/tasks.
  - **Acceptance:** plan status changes from Draft to User-reviewed/Approved.
  - **Validation:** user confirmation or explicit correction captured in context.
  - **Effort:** S
  - **Dependencies:** none.
- [ ] **0.2 Implementation agent confirms current repo state before first edit**
  - **Files:** `Explore.Application/DTOs/**`, `Explore.Application/Features/**`, `Explore.API/Controllers/**`.
  - **Acceptance:** no stale planning assumptions are used blindly.
  - **Validation:** rerun inventory `rg` commands from plan Section 6.
  - **Effort:** S
  - **Dependencies:** 0.1.

## Phase 1: Inventory And Classification
- [ ] **1.1 Build mutable update inventory**
  - **Files:** existing `Explore.Application/DTOs/**`, `Explore.Application/Features/**`, `Explore.API/Controllers/**`; update dev docs.
  - **Acceptance:** every current update surface has a disposition: migrate, already compliant, specialized domain action, read-only/excluded, or needs user decision.
  - **Validation:** `rg --files Explore.Application/DTOs | rg '/Update.*Dto\.cs$'`; `rg --files Explore.Application/Features | rg '/Update.*CommandHandler\.cs$'`; `rg --files Explore.API/Controllers`.
  - **Effort:** M
  - **Dependencies:** 0.2.
- [ ] **1.2 Define exclusion rules**
  - **Files:** dev docs; maybe `docs/API_CHANGELOG.md` later.
  - **Acceptance:** no task exposes generic writes for lookup/projection/audit/outbox/idempotency/provider-owned rows.
  - **Validation:** review against `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, existing controllers.
  - **Effort:** S
  - **Dependencies:** 1.1.
- [ ] **1.3 Decide clear-null representation**
  - **Files:** plan/context/tasks and first affected DTOs.
  - **Acceptance:** implementation has a documented way to distinguish group omitted from "clear this nullable field".
  - **Validation:** validator/unit test examples for at least one nullable field.
  - **Effort:** M
  - **Dependencies:** 1.1.

## Phase 2: Canonical Reference And Actor
- [ ] **2.1 Harden User reference implementation**
  - **Files:** `Explore.Application/DTOs/User/**`, `Explore.Application/Features/Users/**`, `Explore.Application/Profiles/UserMappingProfile.cs`, new `Event.Application.UnitTests/Features/Users/Commands/UpdateUserCommandHandlerTests.cs`.
  - **Acceptance:** names-only and profile-image-only updates work independently; empty wrapper fails; cache invalidation tested; touched files have `ABOUTME`.
  - **Validation:** `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~UpdateUserCommandHandlerTests`.
  - **Effort:** M
  - **Dependencies:** 1.3.
- [ ] **2.2 Decompose Actor update groups**
  - **Files:** `Explore.Application/DTOs/Actor/**`, `Explore.Application/Features/Actors/**`, actor profile/mapping/tests.
  - **Acceptance:** actor identity/display, profile image, appearance, federation identifiers, and federation metadata are independently updatable with no duplicated field ownership.
  - **Validation:** `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~UpdateActorCommandHandlerTests`.
  - **Effort:** L
  - **Dependencies:** 2.1.

## Phase 3: Core User-Facing Aggregates
- [ ] **3.1 Decompose Event update DTO and handler**
  - **Files:** `Explore.Application/DTOs/Event/**`, `Explore.Application/Features/Events/**`, `Explore.Application/Profiles/EventMappingProfile.cs`, `Explore.API/Controllers/EventController.cs`, event tests.
  - **Acceptance:** title/content, classification, actor/reference, featured image, pricing, registration, visibility, external links, series, and timezone/projection groups update independently.
  - **Validation:** event command/validator tests plus architecture tests.
  - **Effort:** XL
  - **Dependencies:** 2.2.
- [ ] **3.2 Decompose EventSession update**
  - **Files:** `Explore.Application/DTOs/EventSession/**`, `Explore.Application/Features/EventSessions/**`, `Explore.API/Controllers/EventSessionController.cs`, event session tests.
  - **Acceptance:** metadata, schedule, room/group assignment, language/speaker links, Islamic aspect, and lifecycle-related fields preserve invariants.
  - **Validation:** `UpdateEventSessionCommandHandlerTests` and schedule/lifecycle tests.
  - **Effort:** XL
  - **Dependencies:** 3.1.
- [ ] **3.3 Decompose EventAgendaItem, EventDay, EventSeries, EventRegistration**
  - **Files:** corresponding DTOs/features/controllers/tests.
  - **Acceptance:** each field/logical group updates independently; parent event caches are invalidated where needed.
  - **Validation:** feature-specific unit tests.
  - **Effort:** L
  - **Dependencies:** 3.1.
- [ ] **3.4 Decompose Organization update**
  - **Files:** `Explore.Application/DTOs/Organization/**`, `Explore.Application/Features/Organizations/**`, `Explore.API/Controllers/OrganizationController.cs`, organization tests.
  - **Acceptance:** details and approval/status authority remain correctly separated; route contract remains stable unless approved.
  - **Validation:** unit tests plus `AuthorizationIntegrationTests` representative coverage.
  - **Effort:** L
  - **Dependencies:** 2.2.
- [ ] **3.5 Decompose Group update**
  - **Files:** `Explore.Application/DTOs/Group/**`, `Explore.Application/Features/Groups/**`, `Explore.API/Controllers/GroupController.cs`, group tests.
  - **Acceptance:** metadata, approval/status, and member-affecting updates stay authorized and tenant-safe.
  - **Validation:** group unit/integration tests.
  - **Effort:** L
  - **Dependencies:** 3.4.
- [ ] **3.6 Decompose Tenant update surfaces**
  - **Files:** `Explore.Application/DTOs/Tenant/**`, `Explore.Application/Features/Tenants/**`, tenant settings/storage/navigation update slices.
  - **Acceptance:** tenant policy/settings/navigation/storage groups preserve governance locks and authorization boundaries.
  - **Validation:** tenant/update tests and architecture tests.
  - **Effort:** XL
  - **Dependencies:** 3.4.
- [ ] **3.7 Decompose Location and LocationRoom updates**
  - **Files:** `Explore.Application/DTOs/Location/**`, `LocationRoom/**`, features, controllers, tests.
  - **Acceptance:** name/address/geo/room capacity or presentation fields update independently and PII rules are preserved.
  - **Validation:** `UpdateLocationRoomCommandHandlerTests` plus new Location tests.
  - **Effort:** M
  - **Dependencies:** 2.2.

## Phase 4: Simple CRUD, Link Entities, Templates, And Specialized Mutables
- [ ] **4.1 Convert catalog/simple entities**
  - **Files:** Category, Tag, StorageObject, SyncState, IndexedDid, AtprotoRecord DTO/features/controllers/tests.
  - **Acceptance:** simple update DTOs become wrappers with group DTOs; empty wrappers fail.
  - **Validation:** feature unit tests and API contract tests.
  - **Effort:** L
  - **Dependencies:** 3.7.
- [ ] **4.2 Convert relationship/link update surfaces**
  - **Files:** EventCategories, EventTags, EventSessionLanguage, EventSessionSpeaker, CategoryTypeCategories, TagTypeTags, member-role update slices.
  - **Acceptance:** link writes remain repository-mediated and tenant-safe.
  - **Validation:** unit/integration tests for link updates.
  - **Effort:** L
  - **Dependencies:** 4.1.
- [ ] **4.3 Convert template/custom-property definition updates**
  - **Files:** EventTemplate, EventSessionTemplate, CustomPropertyDefinition, EventCustomProperty, EventSessionCustomProperty DTO/features/controllers/tests.
  - **Acceptance:** definition metadata/options/sync-sensitive updates preserve template sync and projection semantics.
  - **Validation:** existing custom-property/template concurrency tests plus new group tests.
  - **Effort:** XL
  - **Dependencies:** 4.2.
- [ ] **4.4 Convert footer, localization, appearance, external API key policy, and settings update surfaces where classified as property updates**
  - **Files:** Footer, Localization, Appearance, ExternalApiKey, Settings feature slices.
  - **Acceptance:** governance locks, owner scopes, and admin-only policies remain enforced.
  - **Validation:** focused unit/integration tests.
  - **Effort:** L
  - **Dependencies:** 4.3.
- [ ] **4.5 Record specialized domain actions that stay separate**
  - **Files:** dev docs; possibly tests/docs if discovered.
  - **Acceptance:** publish/archive/cancel/moderation/schedule/template-sync/settings-batch commands are documented as action commands where appropriate, not forced into per-property updates.
  - **Validation:** dev docs review.
  - **Effort:** S
  - **Dependencies:** 4.4.

## Phase 5: API Contract, HAL, Client, And Docs
- [ ] **5.1 Update controllers to accept wrapper DTOs consistently**
  - **Files:** `Explore.API/Controllers/**/*.cs`.
  - **Acceptance:** route ID/body ID checks use wrapper IDs; write endpoints keep `[Authorize]`; route names unchanged unless approved.
  - **Validation:** `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`.
  - **Effort:** L
  - **Dependencies:** Phases 2-4 as each batch lands.
- [ ] **5.2 Verify HAL update links still point to valid update routes**
  - **Files:** `Explore.API/Hateoas/**`, `RouteNames`, controller routes.
  - **Acceptance:** edit/update affordance links remain server-authorized and stable.
  - **Validation:** HATEOAS/API integration tests.
  - **Effort:** M
  - **Dependencies:** 5.1.
- [ ] **5.3 Update API changelog and regenerate client contracts if required**
  - **Files:** `docs/API_CHANGELOG.md`, generated client artifacts if present.
  - **Acceptance:** client-facing breaking request schema changes are documented; generated clients compile if regenerated.
  - **Validation:** OpenAPI parity tests and build.
  - **Effort:** M
  - **Dependencies:** 5.1.

## Phase 6: Cross-Cutting Verification
- [ ] **6.1 Run Application unit test suite**
  - **Files:** test output only unless failures require fixes.
  - **Acceptance:** `Event.Application.UnitTests` passes.
  - **Validation:** `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`.
  - **Effort:** M
  - **Dependencies:** implementation batches complete.
- [ ] **6.2 Run API integration test suite**
  - **Files:** test output only unless failures require fixes.
  - **Acceptance:** `Event.API.IntegrationTests` passes.
  - **Validation:** `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`.
  - **Effort:** M
  - **Dependencies:** 6.1.
- [ ] **6.3 Run architecture/context tests**
  - **Files:** test output only unless failures require fixes.
  - **Acceptance:** `Event.Architecture.Tests` passes.
  - **Validation:** `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`.
  - **Effort:** M
  - **Dependencies:** 6.2.
- [ ] **6.4 Run release build**
  - **Files:** test output only unless failures require fixes.
  - **Acceptance:** release build passes.
  - **Validation:** `dotnet build --configuration Release --verbosity quiet`.
  - **Effort:** M
  - **Dependencies:** 6.3.
- [ ] **6.5 Refresh dev docs and final handoff**
  - **Files:** this plan/context/tasks.
  - **Acceptance:** docs reflect final implementation state, validation, remaining work, and handoff.
  - **Validation:** manual review against completed work.
  - **Effort:** S
  - **Dependencies:** 6.4.

## Verification Checklist
- [ ] LSP diagnostics clean for modified files.
- [ ] `dotnet build --configuration Release --verbosity quiet` passes.
- [ ] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passes.
- [ ] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` passes.
- [ ] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passes.
- [ ] API changelog updated for request schema changes.
- [ ] Dev docs refreshed with final state and remaining work.

## Remaining / Deferred Work
- Exact generated-client workflow remains to be verified during implementation.
- Clear-null semantics must be resolved before splitting nullable-field groups.
- Specialized domain actions must be inventoried and explicitly documented as retained outside the generic property-update pattern.
