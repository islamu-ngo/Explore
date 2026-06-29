# Full Property Update Sub-DTO Pattern - Implementation Plan

Last Updated: 2026-06-27 Europe/Brussels

## 0. Planning Metadata
- **Request:** Bring full per-property or per-logical-group partial update support to all update-eligible entities using the `Explore.Application/DTOs/User/` sub-DTO pattern, tightened by CTO feedback: PATCH endpoints, route ID as authoritative, explicit clear-null semantics, group-level authorization, optimistic concurrency, transaction boundaries, cache invalidation matrices, OpenAPI baselines, and audit requirements.
- **Task directory:** `dev/next/full-property-update-sub-dto/`
- **Planning status:** Approved direction; Phase 1 foundation complete; User, Actor, Category, Location, LocationRoom, Organization, and Group migrations complete.
- **Matched intents:** `add-write-endpoint`, `add-cqrs-handler`; possible `add-ef-migration` for concurrency columns on mutable aggregate roots.
- **Relevant skills:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`, `senior-cto-feedback`.
- **Relevant rules:** `.claude/rules/application-layer.md`, `.claude/rules/api-controllers.md`; add `.claude/rules/efcore-migrations.md` if concurrency migrations are introduced.
- **External docs/research used:** Context7 official ASP.NET Core docs for `[HttpPatch]`/JSON Patch endpoint shape; Context7 official EF Core docs for application-managed concurrency tokens, `DbUpdateConcurrencyException`, and explicit transactions; Tavily search for REST PATCH/PUT semantics, ETag/If-Match concurrency, explicit null handling, and enterprise API contract guidance.
- **Primary layers touched:** Application, API, tests, docs, generated contracts. Persistence is touched where concurrency tokens, repository update loading, or transaction/unit-of-work support are required.
- **Estimated complexity:** XL. The repo has 55 `Update*Dto` files, 62 update command/handler files, 89 controllers, and many domain entities including lookups, projections, outbox rows, history/audit rows, and specialized lifecycle commands. This is an API contract, authorization, concurrency, cache, and domain-invariant refactor, not a DTO-only cleanup.
- **Phase 1 inventory artifact:** `dev/next/full-property-update-sub-dto/full-property-update-sub-dto-inventory.md`.

## 1. Executive Summary
The target architecture is a clean partial-update contract: update-eligible resources expose canonical `PATCH /api/{resource}/{id}` endpoints. The route ID is authoritative; request bodies do not carry entity IDs. Internally, each update-eligible entity keeps one `Update{Entity}Command` and one `Update{Entity}CommandHandler`. The command receives the route ID, the wrapper update DTO, and concurrency information from `If-Match` or an equivalent command value. The wrapper DTO has nullable logical groups, and each present group represents explicit update intent.

For nullable or clearable fields inside a present group, this workstream standardizes on `OptionalUpdate<T>`:

```csharp
public readonly record struct OptionalUpdate<T>(bool HasValue, T? Value);
```

Implemented foundation:
- `Explore.Application/Models/Common/OptionalUpdate.cs`
- `Event.Application.UnitTests/Models/Common/OptionalUpdateTests.cs`

Missing group means "no intent to update that group." Present group with no actual field operation fails validation. `OptionalUpdate<T>.HasValue == true` means the field is explicitly updated, and `Value == null` means clear the value when business rules allow clearing.

This workstream intentionally breaks old full-update request bodies and old `PUT` partial-update semantics. The project is pre-v1 and in active development; compatibility shims are not allowed unless the user explicitly approves them later. OpenAPI, generated clients, API changelog, and self-hoster release notes must move with the contract.

Out of scope: generic writes for read-only lookups, projections, audit/history records, idempotency/outbox records, provider-owned state, and true lifecycle/domain actions such as publish, archive, moderation, template sync, revoke, rotate, invite, transfer ownership, or role/member actions.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log
| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| User already implements the closest wrapper/sub-DTO pattern. | Verified: `Explore.Application/DTOs/User/UpdateUserDto.cs`; `Explore.Application/Features/Users/Handlers/Commands/UpdateUserCommandHandler.cs`; `Explore.Application/Profiles/UserMappingProfile.cs`; `Explore.Application/DTOs/User/Validators/UpdateUserDtoValidator.cs`. | High | User still uses `PUT` and body ID today; implementation must convert the contract to route-ID PATCH. |
| Actor has a one-handler/null-group shell but still has broad groups. | Verified: `Explore.Application/Features/Actors/Requests/Commands/UpdateActorCommand.cs`; `Explore.Application/Features/Actors/Handlers/Commands/UpdateActorCommandHandler.cs`; `Explore.Application/DTOs/Actor/UpdateActorDto.cs`. | High | Early stress test because actor includes federation and storage metadata. |
| Event has a null-group shell but still one broad `UpdateEventDto`. | Verified: `Explore.Application/Features/Events/Requests/Commands/UpdateEventCommand.cs`; `Explore.Application/Features/Events/Handlers/Commands/UpdateEventCommandHandler.cs`; `Explore.Application/DTOs/Event/Validators/UpdateEventDtoValidator.cs`. | High | High-risk migration because of schedule projection, cache tags, visibility, registration, and authorization. |
| Many legacy handlers validate/map monolithic update DTOs. | Verified: `UpdateCategoryCommandHandler`, `UpdateLocationCommandHandler`. | High | Category/Tag should be first low-risk non-reference migrations before Event. |
| Some update handlers use different response conventions. | Verified: `UpdateOrganizationCommandHandler` returns `Unit`; `UpdateOrganizationDetailsCommandHandler` returns `BaseCommandResponse<Guid>`. | High | Migration may standardize to `BaseCommandResponse<TId>` only when API contract is intentionally changed. |
| API update routes are currently `PUT` routes with named route constants. | Verified: `UserController.UpdateUser`, `CategoryController.Update`, `OrganizationController.Update`. | High | CTO decision supersedes prior route-preservation plan: migrate property updates to canonical PATCH routes and update HAL links/OpenAPI. |
| Update coverage is incomplete. | Verified by search: update tests exist for selected features but no `UpdateUserCommandHandlerTests`; CodeGraph reported no covering tests for `UpdateUserCommand`. | High | Tests must expand before/with each migration batch. |
| Cache invalidation is inconsistent and undocumented per group. | Verified by search: `RemoveAsync("categories:list:1:20")`, `RemoveAsync($"user:detail:{id}")`, `RemoveByTagAsync(CacheTags.EventListByTenant(...))`. | High | Inventory must include an invalidation matrix before code changes. |
| Several DTO files lack the required two-line `ABOUTME` header. | Verified: `Explore.Application/DTOs/User/UpdateUserDto.cs`, `Explore.Application/DTOs/Actor/UpdateActorDto.cs`. | High | New/touched files must be corrected. |
| ASP.NET Core supports PATCH endpoint routing. | Context7: `/dotnet/aspnetcore.docs` returned `[HttpPatch("{id}")]` and `MapPatch("/todoitems/{id}")` examples. | High | We are not required to use JSON Patch; we will use command-shaped PATCH bodies with explicit group semantics. |
| EF Core supports application-managed concurrency tokens and explicit transactions. | Context7: `/dotnet/entityframework.docs` returned `IsConcurrencyToken()`, `[ConcurrencyCheck]`, `DbUpdateConcurrencyException`, and manual transaction examples. | High | Supports portable `Version`/token design and explicit transaction requirements. |
| OpenAPI, NSwag, UoW, audit, and cache-tag foundations already exist. | Verified in `full-property-update-sub-dto-inventory.md`: `Explore.API/Explore.API.csproj`, `Explore.Blazor.Client/nswag.json`, `IUnitOfWork`, `IConcurrencyAware`, `ExploreDbContext.SaveChanges.cs`, `CacheTags.cs`. | High | Implementation should standardize and reuse existing foundations, not create parallel ones. |
| `OptionalUpdate<T>` foundation exists for clear-null semantics. | Added: `Explore.Application/Models/Common/OptionalUpdate.cs`; tests added: `Event.Application.UnitTests/Models/Common/OptionalUpdateTests.cs`; Application build passes and focused `OptionalUpdateTests` pass 7/7. | High | Use for nullable/clearable fields inside present update groups. |
| Representative PATCH/route-ID/concurrency contract is implemented for User. | `UserController.UpdateUser` uses `PATCH /api/user/{id}` with required strong `If-Match`; `UpdateUserDto.Id` removed; `UpdateUserCommand.UserId` and `ExpectedConcurrencyStamp` drive handler loading/concurrency; OpenAPI and NSwag client regenerated. | High | Remaining User hardening still needs audit classification, rollback persistence coverage, and broader `OptionalUpdate<T>` usage for clearable fields. |

### 2.2 Existing Implementation
The Application layer uses CQRS/MediatR with handlers in `Explore.Application/Features/**/Handlers/Commands` and request types in `Explore.Application/Features/**/Requests/Commands`. DTOs live in `Explore.Application/DTOs/{Entity}/`, validators usually live under `Validators/`, mapping profiles live in `Explore.Application/Profiles/`, and controllers in `Explore.API/Controllers/**` dispatch MediatR commands.

The best current pattern is `User`: wrapper DTO, nullable groups, manually instantiated validator, entity load, group-specific mapping or explicit relationship update, repository save, and HybridCache invalidation. The implementation still needs the new architecture amendments: route-ID PATCH, no body ID, `OptionalUpdate<T>` for clearable fields, group-level authorization when group authority differs, concurrency checks, and transaction boundaries for multi-repository updates.

### 2.3 Existing Tests And Verification Coverage
Verified test projects:
- `Event.Application.UnitTests/Event.Application.UnitTests.csproj`
- `Event.API.IntegrationTests/Event.API.IntegrationTests.csproj`
- `Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj` if concurrency migrations/repository transaction behavior are introduced
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

Missing/under-covered: `UpdateUserCommandHandler`, route-level PATCH payload validation, old broad DTO rejection, group-level authorization failures, mixed-payload atomicity, concurrency conflicts, transaction rollback across repositories, cache invalidation per group, and OpenAPI schema diffs.

### 2.4 Existing Documentation And Contracts
Relevant repo docs read:
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

Docs/contracts to update during implementation:
- `docs/API_CHANGELOG.md`
- `docs/API.md` if route semantics are documented
- `docs/SELF_HOSTING.md` or release notes if concurrency migrations are added
- generated OpenAPI/client artifacts if checked in by the repo workflow
- HAL policy tests/docs where update link relations now target PATCH

### 2.5 Current Pain Points / Improvement Areas
- Existing broad DTOs blur "not sent", "clear to null", and "invalid null".
- Current `PUT` update routes imply full replacement while handlers behave as partial updates.
- Command-level authorization is not enough when one mixed payload can contain ordinary and admin-only groups.
- Large aggregate handlers can grow into unmaintainable blocks unless one private `ApplyX` method per group is enforced.
- Multi-repository updates currently rely on implicit behavior; partial writes need explicit transaction/unit-of-work boundaries.
- Cache invalidation is feature-specific and not documented per update group.
- Generated API/OpenAPI contract workflow is now proven for the representative User PATCH contract.
- Major mutable aggregates lack a standardized optimistic concurrency strategy.

### 2.6 Unknowns After Investigation
- The OpenAPI/client workflow is discovered, recorded in `full-property-update-sub-dto-inventory.md`, and executed for the representative User PATCH contract. Broader entity baselines still need to be captured before batch migrations.
- Existing transaction/unit-of-work support is present; each migrated multi-repository handler must explicitly adopt it.
- Existing EF concurrency patterns are mixed: `Guid ConcurrencyStamp`, `long Version`, and PostgreSQL `xmin` all exist. Prefer existing aggregate conventions before adding new fields.
- Existing audit infrastructure is present; sensitive groups still need per-group audit requirements.

## 3. Proposed Future State

### API Contract
Canonical property update route:

```http
PATCH /api/categories/{id}
If-Match: "01978f42-2f00-7b4d-9d7e-b7d5f1a9a001"
Content-Type: application/json
```

Body:

```json
{
  "name": {
    "value": "Community"
  },
  "description": {
    "text": {
      "hasValue": true,
      "value": null
    }
  }
}
```

Rules:
- Route ID is authoritative.
- Body IDs are removed.
- Missing group means no update intent.
- Present group must contain at least one actual update operation.
- Nullable/clearable fields use `OptionalUpdate<T>`.
- Old broad DTO bodies are rejected; no compatibility shim.

### Application Contract
Example command:

```csharp
public sealed record UpdateCategoryCommand(
    Guid CategoryId,
    UpdateCategoryDto Update,
    Guid? ExpectedConcurrencyStamp
) : IRequest<BaseCommandResponse<Guid>>, ISecureRequest;
```

Handler flow:
1. Manually instantiate `Update{Entity}DtoValidator`.
2. Validate wrapper has at least one group and each present group has at least one operation.
3. Load entity by route ID.
4. Verify optimistic concurrency for configured aggregates.
5. Run command/resource authorization, then group-level authorization for every present group.
6. Apply all groups through one private `ApplyX` method per group.
7. For multi-repository updates, execute inside explicit transaction/unit-of-work boundary.
8. Save once after all validation/auth passes.
9. Invalidate cache according to the entity's group cache matrix after successful save only.
10. Emit audit event for sensitive groups where required.

### Handler Discipline
- One public handler per update-eligible entity.
- One private `ApplyX` method per group.
- No direct mapping of the full wrapper DTO to an entity.
- AutoMapper allowed only for group DTO to entity/value-object mapping when fields are straightforward.
- No repository save inside group methods unless explicitly documented and transaction-protected.
- No navigation collection mutation for link/junction writes.
- Group validators are local and explicit.

## 4. Non-Negotiable Constraints
- Property updates use `PATCH`, not `PUT`.
- Route ID is authoritative; update body DTOs do not carry IDs.
- Repositories return entities, never DTOs.
- Validators are manually instantiated in handlers; no injected `IValidator<T>`.
- Application handlers do not depend on `ExploreDbContext`, API, Blazor, or persistence implementations.
- Write endpoints remain `[Authorize]` and retain endpoint classification.
- Resource authorization stays in CQRS request types and existing API policies; group-level authorization is added where groups have different authority.
- HAL links remain the UI source of truth; update affordances are not gated by local role checks.
- Tenant isolation remains central; no runtime broad `IgnoreQueryFilters()`.
- Tenant ID is never trusted from update bodies.
- New and touched files get two `ABOUTME` lines.
- Do not create generic writes for read-only lookup/projection/audit/outbox/provider-owned rows.
- Domain actions remain separate commands.
- Multi-repository updates require explicit transaction/unit-of-work boundaries.
- Major mutable aggregates require optimistic concurrency or an explicit documented rejection with CTO-level rationale.
- OpenAPI/client contracts and API changelog must be updated with every public update schema change.

## 5. Architecture And Design Decisions

### Decision 1: Use PATCH with route ID as authoritative
- **Decision:** Convert property updates to `PATCH /api/{resource}/{id}`. Remove IDs from update request bodies.
- **Why:** The new contract applies partial grouped changes, so PATCH is the clean HTTP semantic. Route/body ID mismatch handling disappears. OpenAPI and generated clients become cleaner.
- **Alternatives considered:** Keep `PUT` with body IDs; separate endpoint per property; JSON Patch.
- **Consequences:** Breaking API route/verb/body changes are intentional. HAL link policies and route names must be updated. Old broad DTO bodies are rejected.
- **Files/layers affected:** `Explore.API/Controllers/**/*.cs`, `RouteNames`, `Explore.API/Hateoas/**`, OpenAPI/generated clients, Blazor service callers, API tests.

### Decision 2: Use `OptionalUpdate<T>` for nullable/clearable fields
- **Decision:** Standardize explicit field operations with `OptionalUpdate<T>(bool HasValue, T? Value)` for nullable/clearable fields only.
- **Why:** It distinguishes omitted field from explicit clear, which plain nullable properties cannot do reliably.
- **Alternatives considered:** property-specific `ClearX` flags; JSON Merge Patch; JSON Patch.
- **Consequences:** More explicit DTOs and validators. Present group with no `HasValue` operations fails validation. Non-nullable fields can remain normal properties.
- **Files/layers affected:** shared Application DTO helper, validators, OpenAPI schema, generated clients.

### Decision 3: Keep one command and one handler per update-eligible entity, with strict private apply methods
- **Decision:** Keep one public MediatR command/handler per update-eligible entity, but enforce one private `ApplyX` method per group.
- **Why:** Matches the user's requested architecture while controlling handler growth.
- **Alternatives considered:** one command/handler per group; service-per-group.
- **Consequences:** Large handlers must remain organized and testable. Shared helper methods are allowed only when they reduce real duplication and do not hide authorization or domain rules.
- **Files/layers affected:** Application feature commands/handlers/tests.

### Decision 4: Group by independently saveable invariant
- **Decision:** Group fields by business invariant, not blindly by scalar property.
- **Why:** Some fields must change atomically: names, address/geo, schedule/timezone projections, pricing/currency, visibility/publication, storage image links, template definitions/options, and policy/settings documents.
- **Alternatives considered:** one DTO per property.
- **Consequences:** Inventory must document group boundaries and why each group is independently saveable.
- **Files/layers affected:** DTOs, validators, mapping profiles, handler apply methods.

### Decision 5: Add group-level authorization for mixed-authority payloads
- **Decision:** Every present group with authority different from the base resource update requires group authorization before any mutation is applied.
- **Why:** A single payload can mix organizer-editable fields with admin-only moderation, ownership, visibility, settings, or provider metadata fields.
- **Alternatives considered:** rely only on command-level authorization.
- **Consequences:** Mixed payload fails atomically if any group is unauthorized. Save and cache invalidation do not run on authorization failure.
- **Files/layers affected:** Application authorization helpers/policies, handlers, tests, Cerbos/local parity where needed.

### Decision 6: Add optimistic concurrency for major mutable aggregates
- **Decision:** Use the existing `Guid ConcurrencyStamp`/`IConcurrencyAware` convention for aggregates that already follow or naturally fit that model; keep existing `long Version`/provider-specific exceptions only where already justified by the inventory. Expose concurrency through strong `If-Match` for API correctness and pass the parsed stamp/version into commands for handlers/tests/generated clients.
- **Why:** Prevent lost updates in admin dashboards and generated clients while reusing the repository's existing `ExploreDbContext.SaveChanges` concurrency-stamp refresh behavior instead of introducing a parallel convention.
- **Alternatives considered:** SQL Server-style rowversion; new universal `long Version`; no concurrency.
- **Consequences:** High-risk aggregates need EF concurrency-token configuration, migrations where the token is missing, conflict-to-ProblemDetails mapping, and tests. Low-risk entities can explicitly reject concurrency only after inventory with rationale.
- **Files/layers affected:** Domain entities, EF configurations/migrations, repositories, exception handling, API controllers, tests.

### Decision 7: Require explicit transaction/unit-of-work boundaries for multi-repository updates
- **Decision:** Any update handler modifying more than one aggregate/repository must run inside an explicit application transaction boundary or documented repository-level unit of work.
- **Why:** Partial writes are unacceptable for self-hostable enterprise software.
- **Alternatives considered:** rely on implicit DbContext lifetime/save behavior.
- **Consequences:** Inventory must find or create a transaction abstraction in the correct layer. External HTTP/email/broker calls must not happen inside DB transactions.
- **Files/layers affected:** Application contracts, Persistence implementation, multi-repository handlers, tests.

### Decision 8: Add cache invalidation matrix per migrated entity
- **Decision:** Inventory and implementation must document group-to-cache invalidation before code changes.
- **Why:** Different groups affect detail, list, search, calendar, HAL affordance, and generated read-model caches differently.
- **Alternatives considered:** invalidate all cache for every update.
- **Consequences:** More inventory work and tests, lower stale-data risk.
- **Files/layers affected:** handlers, cache helpers, tests, dev docs.

### Decision 9: Treat OpenAPI/generated clients as build artifacts
- **Decision:** Baseline OpenAPI before Phase 2, regenerate/diff after each API batch, and update generated clients where checked in.
- **Why:** Breaking contract changes are acceptable, but hidden generated-client drift is not.
- **Alternatives considered:** defer client generation until the end.
- **Consequences:** Each API batch includes contract verification and changelog updates.
- **Files/layers affected:** API project, OpenAPI artifacts, generated clients, Blazor service compile, docs.

### Decision 10: Audit sensitive update groups
- **Decision:** Sensitive groups require audit-event design or explicit exclusion.
- **Why:** Enterprise admin surfaces need traceability for role/member, tenant settings, organization approval, visibility/publication, API key metadata, provider settings, moderation, and PII changes.
- **Alternatives considered:** rely only on generic request logs.
- **Consequences:** Audit requirements are part of inventory, not cleanup.
- **Files/layers affected:** Application audit contracts/services if present, handlers, tests, docs.

## 6. Implementation Phases

### Phase 1: Inventory, Foundation Decisions, And Contract Baseline
- **Goal:** Produce a decision-complete implementation inventory before product code changes.
- **Depends on:** User approval of this re-baselined plan.
- **Relevant files:** `Explore.Application/DTOs/**`, `Explore.Application/Features/**`, `Explore.API/Controllers/**`, `Explore.API/Hateoas/**`, `Explore.Application/Profiles/**`, `Explore.Persistence/**`, `Event.*Tests/**`, generated contract locations.
- **Acceptance criteria:** Inventory table includes: surface, current DTO, handler, controller route/verb, route name, auth, group authority, cache matrix, concurrency disposition, transaction need, audit need, OpenAPI/client impact, disposition, and notes.
- **Verification:** inventory `rg` commands, OpenAPI baseline command discovered and recorded, architecture tests if docs/rules are changed.

#### Task 1.1: Build mutable update inventory
- **Type:** investigate / docs
- **Layer:** Application / API / Persistence / Docs
- **Files:** existing update DTOs, commands, handlers, controllers, HAL policies, repositories, tests; update plan/context/tasks.
- **Description:** Create the implementation inventory table. Include the CTO-required columns: route/verb, auth, group authority, cache matrix, concurrency, transaction, audit, OpenAPI/client, disposition.
- **Acceptance Criteria:** Every current update surface has a disposition: `migrate-to-patch`, `already-reference-needs-hardening`, `specialized-domain-action`, `read-only/excluded`, or `needs-user-decision`.
- **Validation:** `rg --files Explore.Application/DTOs | rg '/Update.*Dto\.cs$'`; `rg --files Explore.Application/Features | rg '/Update.*CommandHandler\.cs$'`; `rg --files Explore.API/Controllers`.

#### Task 1.2: Define exclusion and domain-action governance
- **Type:** docs / architecture
- **Layer:** Application / API / Security
- **Files:** dev docs; later `docs/API_CHANGELOG.md`.
- **Description:** Document read-only/internal exclusions and domain actions that stay separate.
- **Acceptance Criteria:** Publish/archive/cancel/moderation/schedule/template-sync/settings-batch/revoke/rotate/invite/role/member/ownership actions are not collapsed into property update groups.
- **Validation:** Review against current controllers, `docs/SECURITY-MODEL.md`, and `docs/AUTHORIZATION.md`.

#### Task 1.3: Establish shared DTO/update policy
- **Type:** create / test
- **Layer:** Application
- **Files:** new shared Application DTO helper location to be chosen during inventory, likely `Explore.Application/Models/Common/OptionalUpdate.cs`; validator tests.
- **Description:** Implement or plan the shared `OptionalUpdate<T>` type and validation helpers for "present group has operation" checks.
- **Acceptance Criteria:** Nullable field semantics are standardized before any DTO split.
- **Validation:** focused unit tests for serialization/deserialization behavior if needed and validator behavior.

#### Task 1.4: Establish PATCH/If-Match API policy and OpenAPI baseline
- **Type:** investigate / docs / test
- **Layer:** API / Docs
- **Files:** `Explore.API/Controllers/**`, OpenAPI generation scripts/config, API tests, `docs/API_CHANGELOG.md`.
- **Description:** Verify the repo's OpenAPI generation workflow, save/diff baseline artifact per existing conventions, and define controller policy for `PATCH /{id}` plus `If-Match`.
- **Acceptance Criteria:** Implementation agents know the exact OpenAPI command, artifact path, and route/operation naming convention before changing routes.
- **Validation:** run the discovered OpenAPI generation command or record blocker if it does not exist.

#### Task 1.5: Establish concurrency and transaction foundation
- **Type:** investigate / architecture / persistence
- **Layer:** Domain / Application / Persistence
- **Files:** Domain aggregate roots, EF configurations, repositories, existing exception handling, tests.
- **Description:** Inventory existing concurrency/transaction abstractions; prefer the existing `IConcurrencyAware`/`ConcurrencyStamp` convention where it fits; define conflict-to-ProblemDetails behavior; define transaction abstraction for multi-repository updates.
- **Acceptance Criteria:** Major aggregate concurrency and multi-repository transaction strategy is decided before Phase 2.
- **Validation:** focused persistence/application tests or documented no-code foundation decision if only planning in this slice.

### Phase 2: Reference Implementation And Low-Risk Pilot
- **Goal:** Prove the new API/Application contract on User, Actor, and one simple CRUD entity before Event.
- **Depends on:** Phase 1.
- **Acceptance criteria:** PATCH route ID, no body ID, optional update semantics, group auth where needed, concurrency where applicable, transaction where needed, cache matrix, OpenAPI diff, and tests are all proven.

#### Task 2.1: Harden User reference implementation
- **Type:** modify / test
- **Layer:** Application / API
- **Files:** `Explore.Application/DTOs/User/**`, `Explore.Application/Features/Users/**`, `Explore.API/Controllers/UserController.cs`, `Explore.Application/Profiles/UserMappingProfile.cs`, new/updated user tests.
- **Description:** Convert current user update to route-ID PATCH, remove body ID, add missing `ABOUTME`, apply `OptionalUpdate<T>` for clearable fields, add transaction boundary for User + Actor + StorageObject update, and add user-profile concurrency if classified high-risk.
- **Acceptance Criteria:** names-only and profile-image-only updates work; explicit clear works where allowed; empty group fails; old broad/body-ID shape is rejected; no partial User/Actor/StorageObject write; cache invalidation follows matrix.
- **Validation:** focused user command/controller tests.

#### Task 2.2: Decompose Actor update groups
- **Type:** modify / test
- **Layer:** Application / API
- **Files:** `Explore.Application/DTOs/Actor/**`, `Explore.Application/Features/Actors/**`, actor controller/profile/tests.
- **Description:** Split actor groups: identity/display, profile image, appearance, federation identifiers, federation metadata. Apply group-level auth for federation/provider-sensitive metadata.
- **Acceptance Criteria:** No duplicated field ownership; unauthorized federation group fails atomically; storage relationship updates are transaction-protected.
- **Validation:** `UpdateActorCommandHandlerTests` plus API PATCH tests.

#### Task 2.3: Migrate Category or Tag as low-risk CRUD pilot
- **Type:** modify / test / docs
- **Layer:** Application / API
- **Files:** Category or Tag DTOs/features/controllers/HAL/tests/OpenAPI changelog.
- **Description:** Use a simple entity to validate DTO shape, validators, PATCH controller policy, OpenAPI schema, cache matrix, and old broad DTO rejection before Event.
- **Acceptance Criteria:** Low-risk pilot passes all mandatory handler/controller/OpenAPI tests and becomes the template for simple entities.
- **Validation:** focused unit/API/OpenAPI tests and build.

### Phase 3: Medium-Risk Core Aggregates
- **Goal:** Migrate common user-facing aggregates after the pilot is proven.
- **Depends on:** Phase 2.

#### Task 3.1: Migrate Location and LocationRoom
- **Files:** `Explore.Application/DTOs/Location/**`, `LocationRoom/**`, features, controllers, tests.
- **Acceptance Criteria:** name/address/geo/room fields update independently; PII and tenant rules are preserved; cache matrix verified.

#### Task 3.2: Migrate Organization
- **Files:** `Explore.Application/DTOs/Organization/**`, `Explore.Application/Features/Organizations/**`, `Explore.API/Controllers/OrganizationController.cs`, tests.
- **Acceptance Criteria:** profile/details groups and approval/status domain actions are separated; admin-only groups require group auth; sensitive approval/PII groups audited.

#### Task 3.3: Migrate Group
- **Files:** `Explore.Application/DTOs/Group/**`, `Explore.Application/Features/Groups/**`, `Explore.API/Controllers/GroupController.cs`, tests.
- **Acceptance Criteria:** metadata, approval/status, membership-adjacent groups stay authorized and tenant-safe.

### Phase 4: High-Risk Event And Program Aggregates
- **Goal:** Migrate Event only after reference, pilot, and medium-risk patterns are stable.
- **Depends on:** Phase 3.

#### Task 4.1: Decompose Event update
- **Files:** `Explore.Application/DTOs/Event/**`, `Explore.Application/Features/Events/**`, `Explore.API/Controllers/EventController.cs`, `Explore.Application/Profiles/EventMappingProfile.cs`, event tests.
- **Description:** Split title/content, classification/lookups, actor/ownership, featured image, pricing, registration, visibility, external links, series, and timezone/projection groups. Preserve schedule projection and list/detail cache invalidation.
- **Acceptance Criteria:** group-level auth handles ownership, visibility, moderation/promoted/featured fields; concurrency conflicts return conflict/precondition failure; cache matrix covers detail/list/search/calendar/HAL-affecting groups.

#### Task 4.2: Decompose EventSession update
- **Files:** `Explore.Application/DTOs/EventSession/**`, `Explore.Application/Features/EventSessions/**`, `Explore.API/Controllers/EventSessionController.cs`, event session tests.
- **Acceptance Criteria:** metadata, schedule, room/group assignment, language/speaker links, Islamic aspect, and lifecycle actions preserve invariants; publish/schedule lifecycle commands stay separate where appropriate.

#### Task 4.3: Migrate Event children and relationship surfaces
- **Files:** EventAgendaItem, EventDay, EventSeries, EventRegistration, EventCategories, EventTags, EventSessionLanguage, EventSessionSpeaker.
- **Acceptance Criteria:** link writes remain repository-mediated and tenant-safe; parent event cache invalidation is verified.

### Phase 5: Tenant, Settings, Templates, Custom Properties, And Specialized Mutables
- **Goal:** Migrate the remaining update-eligible high-governance surfaces.
- **Depends on:** Phase 4.
- **Acceptance criteria:** governance locks, tenant isolation, audit, group auth, OpenAPI docs, and concurrency/transaction strategy are proven per surface.

#### Task 5.1: Migrate Tenant/settings/navigation/footer/storage surfaces
#### Task 5.2: Migrate template and custom-property definition updates
#### Task 5.3: Migrate remaining simple/specialized mutables classified in inventory
#### Task 5.4: Record and test domain actions retained outside property updates

## 7. Testing Strategy
Mandatory per handler:
- empty wrapper fails;
- present group with no field operation fails;
- valid single group succeeds;
- multiple valid groups succeed atomically;
- invalid group prevents all changes;
- unauthorized group prevents all changes;
- not found returns correct failure;
- concurrency mismatch returns conflict/precondition failure where configured;
- save called once;
- multi-repository update rolls back or does not persist partial changes;
- cache invalidation happens after successful save only;
- cache invalidation does not happen on validation/auth/concurrency failure.

Mandatory per controller:
- unauthenticated PATCH rejected;
- route ID is authoritative and no body ID is accepted;
- old broad DTO shape rejected;
- invalid group body returns ProblemDetails/validation problem;
- `If-Match`/expected concurrency behavior tested where required;
- OpenAPI schema exposes nullable groups and `OptionalUpdate<T>` correctly;
- HAL update links target PATCH route names.

High-risk aggregate tests:
- Event/EventSession schedule and timezone projection;
- tenant/wrong-tenant relationship IDs fail closed;
- group-level auth for ownership, visibility, moderation, settings, provider/federation metadata;
- audit event emitted for sensitive groups;
- cache matrix assertions for detail/list/search/calendar/HAL-affecting groups.

Minimum commands:
```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Add when persistence/concurrency migrations are introduced:
```bash
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
```

## 8. Documentation, Configuration, And Operations Impact
- Update `docs/API_CHANGELOG.md` for every route/body/schema break.
- Update `docs/API.md` if controller semantics or OpenAPI workflow docs need correction.
- Add self-hoster release notes/migration notes if concurrency columns/migrations are added.
- Document old payload versus new PATCH payload examples.
- Regenerate/diff OpenAPI and generated clients after each API batch.
- No Aspire/Docker config changes are expected unless OpenAPI generation or migrations require new tooling steps.

## 9. Security, Authorization, Privacy, And Abuse Considerations
- Writes remain `[Authorize]`.
- CQRS resource authorization remains on commands via `IAuthorizedRequest`, `[AuthorizeResource]`, or `ISecureRequest`.
- Group-level authorization is mandatory for groups with stricter authority than base update.
- Mixed payload fails atomically if any group is unauthorized.
- Tenant ID is never accepted from update bodies.
- Cross-tenant relationship IDs fail closed.
- PII, API keys, provider identifiers, moderation notes, storage references, emails, addresses, and federation metadata are not logged as DTO payloads.
- Sensitive groups require audit event design or explicit exclusion.
- Idempotency middleware remains available for unsafe methods; do not bypass it with alternate routes.
- HAL remains the UI source of truth for update affordances.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations
- **Multi-tenancy:** Applicable and high-risk. Tenant context is API-authoritative; query filters stay active; cache keys/tags include tenant context where tenant-specific.
- **Federation:** Applicable for Actor/DID/ATProto/PDS metadata. Provider-owned or protocol-owned fields need explicit group auth and audit.
- **Localization:** Applicable for localized display names, culture, timezone, and text fields. Use `OptionalUpdate<T>` for clearable localized text.
- **Accessibility:** Not directly applicable unless Blazor edit forms change later. If UI changes, use HAL affordances and standard form validation.
- **Product:** Applicable. The new contract improves generated client ergonomics and reduces stale-form overwrites.

## 11. Observability And Operations
- Keep structured request logging and ProblemDetails behavior.
- Map concurrency conflicts to a stable RFC 7807 ProblemDetails response.
- Audit sensitive group changes through existing audit infrastructure if present; otherwise add an explicit task before sensitive groups migrate.
- Do not log raw DTOs or sensitive field values.
- No new health checks are expected unless new background/audit/outbox work is introduced.

## 12. Migration And Compatibility Plan
Compatibility position:
- Old broad `PUT` property-update bodies are intentionally removed.
- New property updates use canonical `PATCH /api/{resource}/{id}`.
- Route ID is authoritative; body IDs are removed.
- No backward compatibility shim will be added.

Impact:
- Generated clients must be regenerated.
- Blazor services/components using old update DTOs must be updated.
- API changelog and release notes must include old/new examples.
- EF migrations may be required for concurrency `Version` fields.

Migration:
- No data backfill is expected for DTO-only surfaces.
- If `Version` columns are added, migrations must initialize existing rows deterministically and document downgrade/reset caveats for development/self-hosters.

## 13. Risk Register
| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| Generic writes exposed for internal rows. | Medium | High | Inventory/exclusion governance before DTO work. | New PATCH route for lookup/outbox/audit/projection without policy. | 1.1/1.2 |
| Nullable-field semantics remain ambiguous. | Medium | High | Standardize `OptionalUpdate<T>` before DTO split. | Tests cannot distinguish omit vs clear. | 1.3 |
| PATCH route migration misses HAL/OpenAPI/client update. | Medium | High | OpenAPI baseline/diff and HAL tests per batch. | Broken generated client or stale link relation. | 1.4/5.x |
| Lost updates remain possible. | Medium | High | Add `Version`/If-Match for major aggregates. | Concurrency test fails or absent. | 1.5 |
| Group-level authorization gap broadens authority. | Medium | High | Inventory group authority and test unauthorized mixed payloads. | Unauthorized group persists or save called. | all phases |
| Multi-repository update partially persists. | Medium | High | Explicit transaction/unit-of-work boundary. | Forced failure leaves partial data. | 1.5 and affected handlers |
| Cache invalidation stale/too broad. | Medium | Medium | Per-entity group cache matrix and tests. | Detail/list/search stale after update. | 1.1/all phases |
| Handler becomes unmaintainable. | Medium | Medium | One private `ApplyX` per group; no full wrapper mapping. | Large unstructured handler or duplicated rules. | all phases |
| Sensitive changes lack auditability. | Medium | Medium | Audit requirement in inventory and tests. | Admin/security group change not traceable. | 1.1/all sensitive groups |

## 14. Success Metrics And Definition Of Done
- Every update-eligible surface has a completed inventory row.
- Property updates use `PATCH /api/{resource}/{id}`.
- Body IDs are removed from update DTOs.
- Clearable fields use `OptionalUpdate<T>`.
- Present groups without operations fail validation.
- Group-level authorization exists and is tested where group authority differs.
- Major mutable aggregates have concurrency checks or documented CTO-approved rejection.
- Multi-repository handlers use explicit transaction/unit-of-work boundaries.
- Each migrated entity has a cache invalidation matrix and tests.
- Sensitive groups have audit handling or documented exclusion.
- OpenAPI/client artifacts are regenerated/diffed per batch.
- No repository returns DTOs or exposes `IQueryable`.
- Required build/tests pass.

## 15. Implementation Agent Contract - KEEP DEV DOCS CURRENT
Future agents implementing this plan MUST:
1. Read this plan, `full-property-update-sub-dto-context.md`, and `full-property-update-sub-dto-tasks.md` before editing.
2. Start with Phase 1. Do not generate DTOs before inventory/foundation decisions are complete.
3. Update all three dev docs after each meaningful implementation slice.
4. Preserve Clean Architecture, manual validators, entity-returning repositories, HAL affordance rules, tenant isolation, group authorization, transaction boundaries, and concurrency strategy.
5. Do not report completion unless docs, tests, OpenAPI/client state, and current code agree.
6. In final implementation summaries, teach the user what changed: PATCH route, DTO groups, optional update semantics, validator flow, group authorization, transaction/concurrency behavior, mapping, cache invalidation, audit behavior, tests, and remaining work.

## 16. Progress Reporting Contract
Use this structure after implementation slices:
- **Implemented:** explain PATCH route/body contract, DTO groups, optional-update semantics, command/handler flow, validation, group authorization, transaction/concurrency behavior, mapping, cache invalidation, audit handling, and OpenAPI/client changes.
- **Verified:** exact build/tests/OpenAPI generation run.
- **Remaining:** incomplete entity batches, deferred surfaces, or risks.
- **Next:** next concrete slice.
- **Docs updated:** yes/no with reason.

## 17. Potential Risks & Unknowns
The critical risk is letting this become a DTO refactor instead of a complete update architecture. The implementation must treat partial updates as an API contract, authorization, concurrency, cache, transaction, audit, and domain-invariant project. Event/session scheduling, storage-object relationships, organization/group/tenant authority, and federation-owned actor metadata are the highest-risk surfaces because updates can affect projections, caches, HAL affordances, tenant isolation, external protocol expectations, and operator auditability.
