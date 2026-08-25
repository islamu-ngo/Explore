<!-- ABOUTME: Implementation plan for ambitious, semantics-first C# record adoption across ISLAMU Event. -->
<!-- ABOUTME: Sequences horizontal Clean Architecture migration, trusted identity hardening, contract generation, and tests. -->

# Records Adoption — Implementation Plan

Last Updated: 2026-08-24 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Modernize suitable DTOs, MediatR requests, immutable outbox payloads, domain value objects, and immutable Blazor state with C# records while retaining classes where identity, lifecycle mutation, generated ownership, framework binding, or editable state makes class semantics correct.
- **Task directory:** `dev/active/records-adoption/`
- **Planning status:** Approved; implementation active in isolated worktree `/home/amir/ISLAMU/Github/Event-records-adoption`.
- **Matched intent:** `openapi-contract-change` from `.agents/contract/intents.yaml`.
- **Criticality:** The matched intent is `standard`, but the body-authority and tenant/user identity corrections activate the Tier 1 Security guardrail for those tasks. Those tasks require advanced-model implementation, invariant-breaker tests, zero-PII checks, scoped mutation evidence above 85%, and anonymized Epistemic MAD review.
- **Relevant skills:** `implementation-plan`, `i-vsd`, `grill-me`, `criticality-guardrail`, `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `auth-patterns`, `outbox-pattern`, `dotnet-efcore-guidelines`, `blazor-ui-conventions`, `ip-clean-room`, `agentic-research`, `ast-grep`, `epistemic-mad-review`.
- **Relevant rules:** `.agents/rules/application-layer.md`, `api-controllers.md`, `domain.md`, `blazor-client.md`, `tests.md`, `auth-trust-boundaries.md`, `work-criticality-matrix.md`, and `ip-clean-room.md`.
- **Primary layers:** Domain, Application, API, generated OpenAPI/NSwag contract, and Blazor Client. Persistence is touched only when an existing value-object mapping or projection caller must compile; no schema change is planned.
- **Complexity:** XL. The Application project alone has 657 source files matching `*Dto*.cs`; compiled request discovery, object initializers, AutoMapper projections, JSON source generation, OpenAPI generation, generated client code, and UI consumers create a repository-wide blast radius.
- **I-VSD Document:** [C# Records Adoption I-VSD consultation](../../../islamic-value-sensitive-design/i-vsd-records-adoption.md)
- **Grill-Me Intake:**
  - Accepted ambitious classification-first adoption rather than keyword uniformity.
  - Accepted explicit command `UserId` / `TenantId` only when authorization or business intent uses those facts; ambient trusted contexts remain valid where identity is not request data.
  - Accepted a permanent, shrinking architecture ratchet for MediatR requests, DTO classes, and body-authority dispositions.
  - Rejected feature-oriented vertical slices. Migration ownership is horizontal: Domain → Application → API/OpenAPI → generated client/Blazor, with downstream compilation repairs allowed before each phase gate.
  - Accepted `BaseCommandResponse<T>` remaining a class; immutable result redesign is deferred.
  - Accepted serializer-compatible read-only collections and defensive copies within converted contracts, without a repository-wide collection rewrite.
  - Explicitly approved intentional development-stage breaking API changes, provided tests, OpenAPI, generated clients, and release evidence move together by workstream completion. Compatibility shims are forbidden.

## 1. Executive Summary

The workstream establishes C# records as the default for handwritten contracts whose meaning is immutable data plus value equality, then migrates the eligible repository surface in Clean Architecture order.

The outcome is not “records everywhere.” It is:

- concrete MediatR commands and queries expressed as immutable records;
- read/projection DTOs and client-owned HTTP bodies expressed as positional or nominal records according to binding and construction needs;
- trusted route, user, and tenant facts introduced from established server authorities rather than request bodies;
- immutable outbox payload snapshots separated from mutable persisted outbox entities;
- small domain value objects using record class or `readonly record struct` only when value/copy semantics fit;
- immutable Blazor result/filter/dialog snapshots expressed as records while generated NSwag DTOs and mutable form/component state remain classes;
- permanent architecture tests that reject new record-policy debt and force every retained class to have a current semantic reason.

### Intended outcomes

- Make immutable intent and value semantics explicit.
- Remove body-owned current-user/current-tenant authority.
- Reduce object-initializer mutation and constructor boilerplate where doing so improves correctness.
- Use `with` expressions and value equality in adversarial tests to produce clear tampering and variant cases.
- Keep JSON, PATCH presence semantics, validation, OpenAPI, NSwag generation, HAL, and Clean Architecture boundaries deterministic.

### Non-goals

- No conversion of EF entities, outbox lifecycle entities, services, repositories, handlers, validators, controllers, or mutable Blazor edit state merely for consistency.
- No manual edits to `EventApiClient.g.cs`, generated OpenAPI, API inventory, EF migrations, or model snapshots.
- No immutable redesign of `BaseCommandResponse<T>` in this workstream.
- No new money, coordinate, or date-range value object unless Phase 0 proves an existing duplicated concept and a separate task is approved.
- No dependency additions, database schema change, product behavior, or UI redesign.
- No backward-compatibility adapters, legacy constructors, duplicate JSON properties, or obsolete command aliases.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| The repository already uses records successfully for immutable CQRS contracts. | `src/Explore.Application/Features/Promotions/Requests/Commands/PromotionManagementCommands.cs`; `Features/RegistrationProviders/RegistrationProviderManagementRequests.cs` | High | Includes positional records, abstract record bases, `ISecureRequest`, and authorization facts. |
| Large legacy CQRS surfaces remain mutable classes. | `Features/Categories/Requests/Commands/CreateCategoryCommand.cs`; `UpdateCategoryCommand.cs`; `Features/Events/Requests/Commands/UpdateEventCommand.cs` | High | Commands use setters and object initializers. |
| The current Category create body contains a tenant authority field. | `DTOs/Category/CreateCategoryDto.cs` | High | `TenantId` is writable in the HTTP DTO. |
| Category create authorization and persistence use different tenant sources. | `CreateCategoryCommand.cs`; `CreateCategoryCommandHandler.cs`; `CategoryController.cs` | High | Authorization facts use `CategoryDto.TenantId`; persistence overwrites from `ITenantContext`. This is a fail-closed consistency debt, not just a record conversion. |
| Grouped PATCH contracts already avoid body identity and preserve absent/null semantics. | `DTOs/Category/UpdateCategoryDto.cs`; `Explore.API/Serialization/OptionalUpdateJsonConverterFactory.cs`; `CategoryController.Update` | High | Route ID and `If-Match` are authoritative; `OptionalUpdate<T>` represents explicit set/clear. |
| Read DTOs are frequently mutable classes. | `CategoryDto.cs`; `CategoryListDto.cs`; representative Application DTO inventory | High | Current properties use public setters. |
| The repository has a large mixed DTO surface. | Source-only filename inventory under `src/Explore.Application` | Medium | 657 files match `*Dto*.cs`; filename matching includes validators/helpers and is not an eligibility count. Phase 0 must produce semantic classification. |
| Generated client DTOs are NSwag-owned mutable partial classes. | `src/Explore.Blazor.Client/nswag.json`; `Clients/EventApiClient.g.cs` | High | `classStyle` is `Poco`; manual conversion would be overwritten. |
| Blazor JSON source generation explicitly registers generated DTOs. | `src/Explore.Blazor.Client/Serialization/AppJsonSerializerContext.cs` | High | Record conversion can affect source-generation metadata and consumers. |
| The mutable general outbox row is a lifecycle entity. | `src/Explore.Domain/OutboxMessage.cs`; `docs/OUTBOX_PATTERN.md` | High | Status, attempts, leases, completion, and dead-letter data intentionally mutate. |
| Immutable payload records already coexist with mutable outbox entities. | `RegistrationOrderOutboxMessageFactory.cs`; notification/federation payload records identified during inventory | High | The payload/entity separation is established precedent. |
| Record-specific tests already use `with` variants effectively. | `EventLocationDisclosureContractTests.cs`; registration, payment, federation, and privacy tests found during inventory | High | Existing tests mutate one fact while preserving the rest of a valid fixture. |
| OpenAPI and generated client outputs are governed as one deterministic boundary. | `.github/workflows/openapi-contract.yml`; `docs/API.md`; `schemas/openapi_islamu-event.json` | High | Relevant Application/API changes trigger schema, inventory, and NSwag drift checks. |
| Records are shallowly immutable and provide member-wise equality. | Microsoft Learn C# records documentation | High | Mutable referenced collections remain mutable; array/list equality is not structural sequence equality. |
| ASP.NET Core record binding has strict constructor and metadata rules. | Microsoft Learn ASP.NET Core model binding documentation | High | Exactly one public constructor; parameter/property name/type parity; validation metadata belongs on parameters. |
| System.Text.Json supports records but constructor parameters must match property names/types. | Microsoft Learn immutable serialization documentation | High | `[JsonPropertyName]` does not change constructor parameter matching. |
| EF Core entities should retain reference identity. | Microsoft Learn C# record documentation; current Domain entities | High | Constructor binding is supported, but records are inappropriate for tracked entities that depend on reference equality. |

### 2.2 Existing Implementation

#### Domain

- Domain entities and outbox lifecycle rows are reference-identity classes.
- `src/Explore.Domain/ValueObjects/` contains a mixed set of value-oriented types such as capability hashes, currency metadata, external URLs, UI palette values, and numeric helpers.
- Existing records model snapshots, authorization facts, discriminated outcomes, and pure evaluation inputs.

#### Application

- MediatR requests are mixed: newer feature families use records, while legacy CRUD and scheduler/settings/storage/event families use mutable classes.
- Handlers manually instantiate validators, map entities to DTOs, and often build `BaseCommandResponse<T>` incrementally.
- DTOs are mixed but predominantly mutable classes or classes with `init` properties.
- AutoMapper is registered assembly-wide and constructor/projection compatibility must be validated per DTO wave.
- `ExploreJsonContext` and other JSON source-generation catalogs explicitly enumerate transport types.

#### API

- Controllers are thin MediatR adapters, but many construct commands through object initializers.
- Current identity authority is centralized in `PlatformIdentityPrincipalExtensions`, `IUserContext`, `ITenantContext`, and `ExploreControllerBase` helpers.
- API body models, query models, and generated contracts are mixed classes and records. Inheritance, `IValidatableObject`, query/form binding, and custom converters make some classes semantically correct.
- PATCH contracts rely on route authority, strong `If-Match`, grouped nullable objects, and `OptionalUpdate<T>`.

#### Blazor

- The browser consumes the generated `IEventApiClient`; it must not reference Application/Domain types directly.
- NSwag-generated DTOs are classes by generator ownership.
- Local immutable snapshots/results and mutable edit/component state coexist.
- `AppJsonSerializerContext` and service adapters must remain aligned with regenerated DTOs.

### 2.3 Existing Tests And Verification Coverage

| Project/file | Current protection | Gap this workstream closes |
|---|---|---|
| `tests/Event.Architecture.Tests/ApiContractArchitectureTests.cs` | Public DTO/OpenAPI conventions | No repository-wide record/class disposition ratchet. |
| `AuthorizationSurfaceGuardrailTests.cs` | Compiled MediatR mutation/authorization inventory | Does not require immutable record requests. |
| `ApiLiabilityRatchetTests.cs` | Shrinking source debt baselines | No record-policy baseline. |
| `EventLocationDisclosureContractTests.cs` | Record declaration and validated immutable contract behavior | Local precedent only; not generalized. |
| `Event.Application.UnitTests` | Handlers, DTO validators, mapping, payload factories | Legacy construction often assumes setters/object initializers. |
| `Event.API.IntegrationTests` | Identity, tenant, controller, OpenAPI, and runtime HTTP behavior | No global body-authority spoofing catalog. |
| `Explore.Blazor.Client.Tests` | Generated-client serialization, validators, and immutable UI behavior | Does not classify mutable edit state versus immutable snapshots. |
| `.github/workflows/openapi-contract.yml` | Schema/client drift and determinism | Runs after changes; Red-phase contract specifications must be authored first. |
| `stryker-config.json` | Domain/Application mutation configuration with 85 high threshold | Identity/tenant record migration needs a scoped score above 85%. |
| Planning-time architecture baseline (2026-08-24) | 444 total: 442 passed, 1 skipped, 1 failed | Pre-existing/shared paid-checkout work added `PaidOrderAcceptanceSnapshot.MerchantDisplayName` and `.OperatorDisplayName` without matching PII inventory entries. The failing test file is unchanged; the new Domain type is untracked. Records-adoption implementation must wait for the owning work to restore a green baseline. |

### 2.4 Existing Documentation And Contracts

- `docs/QUICK_REFERENCE.md`, `ARCHITECTURE.md`, `CODEBASE_STRUCTURE.md`, `API.md`, `SECURITY-MODEL.md`, `AUTHORIZATION.md`, `AUTHORIZATION_PATTERNS.md`, `DOMAIN.md`, `OUTBOX_PATTERN.md`, `BLAZOR.md`, and `GOVERNANCE.md`.
- `schemas/openapi_islamu-event.json` and `docs/API_CONTRACT_INVENTORY.md` are generated authorities.
- `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs` is generated from OpenAPI by `nswag.json`.
- `docs/API_CHANGELOG.md` is mandatory for intentional pre-v1 breaking contract changes.
- [I-VSD records-adoption consultation](../../../islamic-value-sensitive-design/i-vsd-records-adoption.md) is the provider-responsibility gate.

### 2.5 Current Pain Points / Improvement Areas

1. Mutable request messages allow post-construction mutation across pipeline steps.
2. Public body DTOs can contain ambient authority fields even when trusted contexts override them later.
3. Setter-heavy read DTOs communicate mutability that consumers should not rely on.
4. Record adoption is inconsistent and not enforced, so new technical debt can appear while migration proceeds.
5. Long positional signatures can create same-typed argument swaps; nominal records are needed where names carry safety.
6. Record `ToString()` can expose sensitive members if whole requests are logged.
7. Collection-bearing records can still expose mutable state and non-structural sequence equality.
8. Generated/client drift can hide behind an apparently source-only DTO change.
9. A big-bang syntax codemod would combine semantic, security, model-binding, serializer, mapping, and UI failures into an unreviewable diff.

### 2.6 Unknowns After Investigation

| Unknown | Evidence searched | Owning task |
|---|---|---|
| Exact eligible versus retained class set for all Application DTOs and requests | Filename/declaration inventories, representative reads, compiled request tests | 0.1–0.2 |
| Every body property whose `UserId`/`TenantId` is current authority versus a legitimate target resource | Controllers, DTOs, authorization facts, API tests | 0.1–0.2, 4.1–4.2 |
| Every AutoMapper/constructor projection affected by positional conversion | Application profiles and caller inventory | 3.1, 3.5 |
| Which Domain value objects are EF-mapped or mutation-dependent | `ValueObjects/`, EF configuration, Persistence callers | 1.1–1.3 |
| Every collection that needs defensive copying rather than only a read-only interface | Candidate constructors and consumer mutations | 1.2, 2.3, 3.2–3.4, 5.3 |
| Full OpenAPI requiredness/nullability delta | Current schema and generated client | 4.1–4.3 |
| Scoped Stryker duration and surviving mutants for changed identity code | `stryker-config.json` | 4.4 |
| Resolution timing for the unrelated paid-checkout PII inventory failure | Architecture suite plus shared-workspace `git status` | Precondition to 0.1; owning workstream, not records adoption |

## 3. Proposed Future State

### 3.1 Type-selection policy

| Type purpose | Default target | Retain class when |
|---|---|---|
| Concrete MediatR command/query | `public sealed record` | No normal exception; a retained class requires an explicit, shrinking architecture-baseline reason. |
| Abstract MediatR hierarchy base | `public abstract record` | A non-record base is required by an external framework and documented. |
| Read/projection DTO | Sealed positional or nominal record | Identity/lifecycle mutation or framework population is intentional. |
| HTTP JSON body DTO | Sealed record containing only client-owned fields | Framework binding/validation requires class semantics. |
| PATCH body | Nominal record with `init` properties and `OptionalUpdate<T>` groups | A verified formatter/model-binding constraint makes a class safer. |
| Outbox payload snapshot | Sealed record | Payload is not actually immutable/versioned or contains lifecycle state. |
| Outbox/EF entity | Class | Always for this workstream. |
| Small value object | Sealed record class or `readonly record struct` | It is large/reference-rich, EF identity-bearing, or copy/value semantics are wrong. |
| Blazor immutable result/filter/dialog payload | Sealed record | It is generated or edit/component state. |
| Generated NSwag DTO | Generated class | Always; representation belongs to NSwag. |
| `BaseCommandResponse<T>` | Class | Retained for incremental mutation and failure helpers. |

### 3.2 Construction and equality

- Prefer positional records for short contracts with stable, semantically distinct parameters.
- Prefer nominal records with `required` / `init` properties for long contracts, optional members, PATCH semantics, attributes, and named-construction safety.
- Concrete records are sealed unless a verified record inheritance hierarchy requires otherwise.
- Record equality is used only where member-wise equality is correct.
- Collection-bearing contracts use serializer-compatible read-only members plus defensive copies when post-construction mutation would violate the contract. Tests never assume list/array sequence equality from record equality.
- No whole-record logging or destructuring is introduced.

### 3.3 Trusted request flow

```text
Client JSON body (client-owned fields only)
          +
Route / If-Match / trusted principal / resolved tenant
          |
          v
API, MCP, BFF, or internal trusted adapter
          |
          v
Immutable MediatR record containing only facts its intent uses
          |
          v
AuthorizationBehavior -> manually instantiated validator -> handler
```

- Current user identity comes from `PlatformIdentityPrincipalExtensions`, `IUserContext`, or established controller helpers.
- Current tenant identity comes from resolved `ITenantContext`.
- A command carries `UserId`/`TenantId` only when authorization facts or business behavior use it.
- A body may carry a target user/tenant identifier only when the operation explicitly manages that target and the disposition baseline documents the distinction.

## 4. Non-Negotiable Constraints

1. Domain remains free of MediatR, ASP.NET Core, EF Core, AutoMapper, and Blazor dependencies.
2. Application owns commands, queries, DTOs, validation, mapping, and payload contracts.
3. API owns model binding, trusted principal/tenant introduction, ProblemDetails, OpenAPI, and controller adapters.
4. Blazor consumes generated API contracts only; no Application/Domain references.
5. Repositories continue returning entities, never DTOs.
6. Validators remain manually instantiated.
7. Body DTOs never become authority for the current user or current tenant.
8. `ISecureRequest` facts remain fail-closed for empty/missing route, tenant, and user facts.
9. PATCH omission, explicit clear, route authority, and `If-Match` behavior remain explicit.
10. Generated OpenAPI, inventory, NSwag code, migrations, and model snapshots are never hand-edited.
11. `BaseCommandResponse<T>` remains a class.
12. Outbox/EF lifecycle entities remain classes.
13. Generated NSwag DTOs remain generated classes.
14. Mutable Blazor forms/component state remain classes.
15. No compatibility shims or obsolete aliases.
16. Every behavioral implementation task has a Red-phase specification before production edits.
17. Every phase runs one Release build and at most one selected non-browser test project once at phase end.
18. All new files begin with two `ABOUTME:` lines.

## 5. Architecture And Design Decisions

### D1. Semantics-first, ambitious adoption

- **Decision:** Convert every handwritten type for which immutable data and value equality are correct; retain classes only for a documented semantic/framework reason.
- **Why:** Makes type declarations communicate behavior while avoiding mutable records and entity-equality hazards.
- **Alternatives considered:** Records everywhere; compatibility-minimal pilot; no migration.
- **Consequences:** Requires a complete inventory and explicit retained-class baseline.
- **Affected layers:** All planned layers.

### D2. Horizontal Clean Architecture sequencing

- **Decision:** Implement by ownership: policy → Domain → Application requests → Application contracts/payloads → API/OpenAPI → generated client/Blazor → governance closeout.
- **Why:** User explicitly rejected feature vertical slices and prioritized clean layer ownership.
- **Alternatives considered:** Feature-by-feature vertical slices.
- **Consequences:** A phase may repair downstream callers to restore compilation, but design decisions remain owned by the inward layer. Intermediate phases are not independently releasable; only final workstream completion is release-ready.
- **Affected layers:** All.

### D3. Permanent shrinking architecture ratchet

- **Decision:** Add compiled/source architecture tests and committed reasoned baselines.
- **Why:** Migration without a ratchet permits new class debt and stale exceptions.
- **Alternatives considered:** One-time script/report; blanket test that all DTOs are records.
- **Consequences:** Every class exception has category, reason, owning layer, and removal trigger; stale/missing entries fail.
- **Affected files:** `tests/Event.Architecture.Tests/RecordContractArchitectureTests.cs` and new baseline JSON files.

### D4. Trusted facts are explicit only when semantically used

- **Decision:** Remove current-user/current-tenant authority from bodies. Carry trusted IDs in a command constructor only when authorization or business intent uses them; otherwise use established trusted contexts.
- **Why:** Avoids both over-posting and meaningless constructor parameters.
- **Alternatives considered:** Put both IDs on every command; read raw JWT claims in every controller; keep body IDs and overwrite later.
- **Consequences:** API/MCP/BFF/internal adapters must construct requests consistently, and invariant-breaker tests must cover each trust path.
- **Affected layers:** Application, API, MCP, BFF tests.

### D5. Positional versus nominal records

- **Decision:** Choose form by construction and framework semantics, not by an arbitrary parameter-count rule.
- **Why:** Long/same-typed positional constructors create silent argument-swap risk; PATCH and validation metadata often need named members.
- **Alternatives considered:** Positional records everywhere; nominal records everywhere.
- **Consequences:** Reviewers must justify positional order and metadata placement.
- **Affected layers:** Domain, Application, API, Blazor.

### D6. Bounded deep-immutability hardening

- **Decision:** Harden collection members within each converted immutable contract using read-only interfaces and defensive copies proven compatible with serializers/generators.
- **Why:** Records provide shallow immutability only.
- **Alternatives considered:** Ignore mutable members; repository-wide immutable-collection rewrite.
- **Consequences:** Some constructor/factory code changes beyond the declaration keyword are required.
- **Affected layers:** Domain, Application, Blazor.

### D7. Preserve lifecycle and generated classes

- **Decision:** Retain EF/outbox entities, handlers, validators, controllers, generated DTOs, mutable UI state, and `BaseCommandResponse<T>` as classes.
- **Why:** Reference identity, mutation, inheritance, generator ownership, or framework behavior is intentional.
- **Alternatives considered:** Mutable records; immutable result redesign.
- **Consequences:** The permanent baseline documents why these are not record-policy debt.

### D8. Breaking changes are explicit, not shimmed

- **Decision:** Accept pre-v1 contract breaks after user approval; update tests, generated artifacts, API changelog, and release fragment. Add no compatibility readers, constructors, aliases, or duplicate fields.
- **Why:** The repository is in development, and consistency is preferred over preserving debt.
- **Alternatives considered:** Compatibility adapters and dual contracts.
- **Consequences:** API and Blazor must deploy together at final workstream completion; old development clients are unsupported.

### D9. Record-aware tests assert behavior

- **Decision:** Use `with` variants for one-fact adversarial changes, equality tests where equality drives behavior, immutable-construction tests, JSON/OpenAPI round trips, PATCH omission/null cases, and body-authority attacks.
- **Why:** Compiler-tautology tests add no confidence.
- **Alternatives considered:** Assert every record has generated equality/`ToString()` only.
- **Consequences:** Record mechanics are tested through consumer-visible invariants.

## 6. Implementation Phases

### Phase 0: Architecture Policy And Candidate Baseline

- **Phase status:** Complete — exact ratchets, policy, Release build, and architecture suite independently verified.
- **Goal:** Establish a deterministic candidate/disposition inventory and permanent no-new-debt ratchet before production conversion.
- **Depends on:** No records-adoption task. Start is blocked until the owning paid-checkout work restores the pre-existing architecture-suite baseline to green.
- **Relevant files:** new `tests/Event.Architecture.Tests/RecordContractArchitectureTests.cs`; new `tests/Event.Architecture.Tests/Baselines/record-contract-class-baseline.json`; new `tests/Event.Architecture.Tests/Baselines/http-body-authority-dispositions.json`; existing `docs/GOVERNANCE.md`, `.agents/rules/application-layer.md`, `.agents/rules/domain.md`, `.agents/rules/blazor-client.md`, `.agents/rules/tests.md`.
- **Related skills/rules:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `auth-patterns`, `ast-grep`; Application/API/Domain/Blazor/Test rules.
- **Acceptance criteria:**
  - Compiled concrete `IRequest`/`IRequest<T>` types are discovered deterministically.
  - Every current non-record request and class DTO is classified with a reason and removal trigger.
  - Every HTTP body authority-like property has a current-authority removal or legitimate-target disposition.
  - New unclassified class requests/DTOs and stale baseline entries fail.
  - The policy distinguishes positional records, nominal records, record structs, retained classes, and generated types.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Revert only the new test/policy artifacts if discovery is nondeterministic. Do not weaken predicates to make the baseline smaller; correct classification or split a false-positive category.

#### Task 0.1: Author Failing Record And Body-Authority Ratchets
- **Status:** Complete — intentional RED independently confirmed.
- **Type:** create
- **Layer:** Tests
- **Files:** `tests/Event.Architecture.Tests/RecordContractArchitectureTests.cs` (new); both baseline JSON files (new, initially empty).
- **Description:** Discover compiled MediatR requests, Application DTO declarations, generated ownership, and API `[FromBody]` contracts. Add deterministic tests for record detection, baseline completeness/staleness, and authority-like property dispositions. Observe RED against current technical debt.
- **Acceptance Criteria:**
  - [x] Tests fail for current class requests and unclassified DTOs.
  - [x] Tests fail for undisposed body `TenantId`/`UserId`-shaped members.
  - [x] Generated NSwag output, validators, entities, and edit-state classes are excluded by explicit category rather than path accident.
  - [x] Record detection reuses the repository’s compiled `EqualityContract` precedent or an equally deterministic runtime check.
- **Dependencies:** Precondition: rerun the architecture baseline only after the owning paid-checkout work changes; it must be green before the first records-adoption test edit.
- **Effort:** L.
- **Required Skills/Rules:** `criticality-guardrail`, `auth-patterns`, `.agents/rules/tests.md`.

#### Task 0.2: Classify Current Contracts And Establish Shrinking Baselines
- **Status:** Complete.
- **Type:** investigate/modify
- **Layer:** Architecture
- **Files:** the two new baseline JSON files; `docs/GOVERNANCE.md`; `.agents/rules/application-layer.md`; `.agents/rules/domain.md`; `.agents/rules/blazor-client.md`; `.agents/rules/tests.md`.
- **Description:** Classify every discovered item as positional-record candidate, nominal-record candidate, record-struct candidate, or retained class with reason/removal trigger. Populate only current debt/exceptions, then make Task 0.1 green without hiding candidates.
- **Acceptance Criteria:**
  - [x] Every baseline entry names type, category, reason, owner, and removal trigger.
  - [x] No generated file or build output is included.
  - [x] Rules document concrete-record default, class exclusions, collection/equality caveats, and body-authority policy.
  - [x] A stale type or resolved class automatically fails until its entry is removed.
- **Dependencies:** 0.1.
- **Effort:** XL.
- **Required Skills/Rules:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `auth-patterns`, `ast-grep`.

### Phase 1: Domain Value Semantics

- **Phase status:** Complete — no production conversion required; Release build and 857 Domain tests independently verified.
- **Goal:** Convert only approved small Domain value types while preserving entity/reference identity and EF behavior.
- **Depends on:** Phase 0.
- **Relevant files:** bounded candidates from `src/Explore.Domain/ValueObjects/**/*.cs`; their existing consumers; new `tests/Event.Domain.UnitTests/ValueObjects/RecordValueObjectContractTests.cs`; existing EF configurations only when a candidate is already mapped.
- **Related skills/rules:** `clean-architecture-rules`, `dotnet-efcore-guidelines`, Domain/Test rules.
- **Acceptance criteria:**
  - Every converted type has intentional equality/copy semantics and invariant tests.
  - `readonly record struct` is used only for small self-contained values.
  - Domain entities and outbox lifecycle entities remain classes.
  - EF model shape remains unchanged; any detected model change blocks and requires separate `add-ef-migration` classification.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Revert an individual candidate to its prior class/struct when value equality, copying, EF mapping, or mutation behavior proves incorrect; keep its baseline reason rather than adding custom equality solely to force record use.

#### Task 1.1: Author Failing Value-Semantics Specifications
- **Status:** Complete — exhaustive characterization found no unresolved Domain class candidate.
- **Type:** create/modify
- **Layer:** Tests
- **Files:** `tests/Event.Domain.UnitTests/ValueObjects/RecordValueObjectContractTests.cs` (new); existing candidate-specific Domain tests.
- **Description:** From Phase 0 candidates, add RED specifications for construction invariants, intended equality/inequality, `with`-based one-fact variants, copy behavior, collection mutation boundaries, and invalid values.
- **Acceptance Criteria:**
  - [x] Each test can fail for the named regression.
  - [x] No test merely asserts compiler-generated prose or exact `ToString()` output.
  - [x] Sequence-equality applicability is explicit; the bounded declarations expose no collection state.
- **Dependencies:** 0.2.
- **Effort:** M.
- **Required Skills/Rules:** Domain/Test rules, `dotnet-efcore-guidelines`.

#### Task 1.2: Convert Approved Domain Value Types
- **Status:** Complete — verified no-op; all approved bounded types were already records.
- **Type:** modify
- **Layer:** Domain
- **Files:** exact candidate files emitted by the Phase 0 baseline under `src/Explore.Domain/ValueObjects/`; direct Domain/Application callers.
- **Description:** Use sealed record classes or `readonly record struct` according to semantics. Preserve constructor validation and use defensive copies for mutable referenced inputs where immutability is claimed.
- **Acceptance Criteria:**
  - [x] All Task 1.1 specifications pass.
  - [x] No Domain dependency is added.
  - [x] Entity, aggregate, outbox lifecycle, and large reference-rich types remain classes.
- **Dependencies:** 1.1.
- **Effort:** L.
- **Required Skills/Rules:** `clean-architecture-rules`, Domain rule.

#### Task 1.3: Repair Mappings And Remove Resolved Domain Baselines
- **Status:** Complete — verified no-op mapping/model closure.
- **Type:** modify
- **Layer:** Persistence/Application/Tests
- **Files:** only verified callers/configurations of converted value types; Phase 0 class baseline.
- **Description:** Update constructor call sites and existing EF conversions/configuration without changing schema. Remove resolved baseline entries.
- **Acceptance Criteria:**
  - [x] No migration or snapshot file is edited.
  - [x] EF metadata/model evidence shows no schema delta.
  - [x] Downstream compilation requires no repair and introduces no outward Domain reference.
- **Dependencies:** 1.2.
- **Effort:** M.
- **Required Skills/Rules:** `dotnet-efcore-guidelines`, `clean-architecture-rules`.

### Phase 2: Application MediatR Requests

- **Goal:** Make concrete commands and queries immutable records and align trusted authorization facts without redesigning handlers or responses.
- **Depends on:** Phase 1.
- **Relevant files:** all compiled request declarations under `src/Explore.Application/Features/**`; legacy request declarations discovered outside that path; validators/handlers; direct API/MCP/worker/test constructors; request baseline.
- **Related skills/rules:** `cqrs-mediatr-guidelines`, `auth-patterns`, `criticality-guardrail`, Application/API/Test rules.
- **Acceptance criteria:**
  - All eligible concrete `IRequest`/`IRequest<T>` types are sealed records.
  - Abstract request hierarchies are abstract records where inheritance is required.
  - Commands carry trusted IDs only when their authorization/business semantics use them.
  - `ISecureRequest` facts fail closed for empty or mismatched facts.
  - Handlers, manual validators, cancellation, caching, and `BaseCommandResponse<T>` behavior remain intact.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Keep a request as a reasoned baseline class only when a verified external framework requires class semantics. Do not add setters to a record merely to preserve object-initializer callers.

#### Task 2.1: Author Failing Request And Authorization Specifications
- **Status:** Complete — comprehensive request RED and fail-closed facts independently confirmed.
- **Type:** modify/create
- **Layer:** Tests
- **Files:** `RecordContractArchitectureTests.cs`; focused tests under `tests/Event.Application.UnitTests/Features/`; identity/authorization tests for affected requests.
- **Description:** Turn request-baseline candidates into RED batches. Use `with` variants to forge tenant/user/resource facts, prove immutable construction, and lock authorization-fact derivation before conversion.
- **Acceptance Criteria:**
  - [x] The compiled batch fails because 590 concrete requests remain classes.
  - [x] Wrong-tenant, empty-ID, and changed-resource variants fail closed where applicable.
  - [x] Sensitive request values are not asserted through logs or snapshots.
- **Dependencies:** 1.3.
- **Effort:** XL.
- **Required Skills/Rules:** `criticality-guardrail`, `auth-patterns`, `cqrs-mediatr-guidelines`.

#### Task 2.2: Convert Commands And Queries By Application Ownership
- **Type:** modify
- **Layer:** Application
- **Files:** request files identified by the compiled Phase 0 baseline; direct validators/handlers and downstream constructors.
- **Description:** Convert concrete requests to sealed positional or nominal records. Preserve abstract record hierarchies. Prefer command-owned intent fields; a nested Application request record is allowed when it avoids unsafe long positional signatures and contains no HTTP-only behavior.
- **Acceptance Criteria:**
  - [ ] Selected class requests are eliminated from the baseline.
  - [ ] No command has meaningless ambient IDs.
  - [ ] No body-sourced current authority survives in authorization facts.
  - [ ] Manual validator construction and handler contracts remain unchanged.
- **Dependencies:** 2.1.
- **Effort:** XL.
- **Required Skills/Rules:** `cqrs-mediatr-guidelines`, `auth-patterns`, Application rule.

#### Task 2.3: Harden Request Collections And Logging Boundaries
- **Type:** modify
- **Layer:** Application
- **Files:** collection-bearing converted requests; affected call sites; zero-PII test sinks.
- **Description:** Replace mutable published collections with serializer/consumer-compatible read-only contracts and defensive copies where required. Ensure logging uses bounded scalar fields rather than record interpolation/destructuring.
- **Acceptance Criteria:**
  - [ ] Mutation after request construction cannot alter values where the contract claims immutability.
  - [ ] Equality tests do not assume structural list/array equality.
  - [ ] Tier 1 request families emit no raw body, token, free text, user ID, or tenant ID in logs.
- **Dependencies:** 2.2.
- **Effort:** L.
- **Required Skills/Rules:** `criticality-guardrail`, Application/Test rules.

### Phase 3: Application DTOs And Immutable Payloads

- **Goal:** Convert eligible handwritten data contracts and immutable outbox payload snapshots while retaining lifecycle and framework classes.
- **Depends on:** Phase 2.
- **Relevant files:** `src/Explore.Application/DTOs/**/*.cs`; DTOs under `Features/**/DTOs`; `ExploreJsonContext`; AutoMapper profiles; outbox payload factories/contracts; downstream compile callers; DTO baseline.
- **Related skills/rules:** `cqrs-mediatr-guidelines`, `outbox-pattern`, `auth-patterns`, `dotnet-efcore-guidelines`, Application/Test rules.
- **Acceptance criteria:**
  - Read/projection DTOs and client-owned HTTP bodies are records unless a baseline reason remains.
  - PATCH records preserve omitted/set/clear semantics.
  - Current-user/current-tenant authority fields are removed; legitimate target identifiers are documented.
  - Immutable payload snapshots are records; outbox entities/processors remain classes.
  - AutoMapper and JSON source generation work with the chosen constructor/property form.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Prefer changing a positional candidate to a nominal record when mapping/serializer metadata requires named members. Revert to a reasoned class when framework behavior is genuinely class-oriented; do not weaken PATCH or validation semantics.

#### Task 3.1: Author Failing DTO Mapping And Serialization Specifications
- **Type:** create/modify
- **Layer:** Tests
- **Files:** focused tests under `tests/Event.Application.UnitTests/DTOs/`, mapping tests, payload-factory tests, JSON-context tests.
- **Description:** For each horizontal DTO category batch, add RED tests for immutable construction, intended equality, `with` variants, mapping/projection, JSON round trip, required/null behavior, PATCH omission/clear, and payload serialization.
- **Acceptance Criteria:**
  - [ ] Tests assert machine-consumed behavior, not declaration prose.
  - [ ] PATCH tests distinguish omitted, explicit null/clear, and replacement.
  - [ ] Payload tests preserve event type/version, idempotency inputs, and privacy-safe fields.
- **Dependencies:** 2.3.
- **Effort:** XL.
- **Required Skills/Rules:** `outbox-pattern`, Application/Test rules.

#### Task 3.2: Convert Read And Projection DTOs
- **Type:** modify
- **Layer:** Application
- **Files:** Phase 0 read/projection candidates under `DTOs/**` and `Features/**/DTOs`; mapping profiles; direct consumers.
- **Description:** Use positional records for short stable scalar projections and nominal records for long/optional/attribute-heavy contracts. Harden collections only where immutable projection semantics require it.
- **Acceptance Criteria:**
  - [ ] Every converted projection has intentional equality.
  - [ ] AutoMapper/LINQ construction remains translatable and deterministic.
  - [ ] HAL, ETag, pagination, and normalized lookup fields retain their intended wire meaning.
- **Dependencies:** 3.1.
- **Effort:** XL.
- **Required Skills/Rules:** `cqrs-mediatr-guidelines`, `clean-architecture-rules`.

#### Task 3.3: Convert HTTP Body DTOs And Remove Ambient Authority
- **Type:** modify
- **Layer:** Application
- **Files:** Phase 0 HTTP-body candidates and their validators; downstream API/MCP/BFF constructors needed for compilation.
- **Description:** Convert bodies to positional or nominal records. Remove current-user/current-tenant fields and route-owned IDs. Retain explicit target resource IDs only with a disposition. Put ASP.NET validation metadata on record constructor parameters when positional binding is used.
- **Acceptance Criteria:**
  - [ ] Bodies contain only client-owned fields.
  - [ ] PATCH bodies remain nominal records when presence semantics require it.
  - [ ] No record has multiple public constructors when ASP.NET record binding applies.
  - [ ] Validation messages and ProblemDetails inputs remain machine-equivalent or intentionally documented as breaking.
- **Dependencies:** 3.1, 2.2.
- **Effort:** XL.
- **Required Skills/Rules:** `auth-patterns`, `criticality-guardrail`, Application/API rules.

#### Task 3.4: Convert Immutable Outbox Payload Snapshots
- **Type:** modify
- **Layer:** Application
- **Files:** payload contracts/factories identified by Phase 0 under Application notification, registration, moderation, federation, integration, and webhook features; payload tests.
- **Description:** Convert point-in-time payload snapshots to sealed records without converting persisted queue/lifecycle entities. Preserve versioned JSON field names unless an intentional breaking payload change is explicitly documented.
- **Acceptance Criteria:**
  - [ ] Outbox entities, repositories, leases, retries, and processors remain classes.
  - [ ] Serialized payloads round-trip and retain idempotency/replay facts.
  - [ ] PII-bearing payloads are not exposed through record logging/`ToString()`.
  - [ ] No compatibility reader is added.
- **Dependencies:** 3.1.
- **Effort:** L.
- **Required Skills/Rules:** `outbox-pattern`, `criticality-guardrail`.

#### Task 3.5: Align Mapping, JSON Contexts, And Downstream Compilation
- **Type:** modify
- **Layer:** Application/Infrastructure/API/Tests
- **Files:** `src/Explore.Application/Profiles/**/*.cs`; `Serialization/ExploreJsonContext.cs`; verified caller files; DTO baseline.
- **Description:** Update named/constructor mappings, source-generation registrations, object-initializer call sites, and downstream consumers. Remove resolved baseline entries.
- **Acceptance Criteria:**
  - [ ] Application mapping configuration is valid.
  - [ ] System.Text.Json source generation includes every required record.
  - [ ] No generated file is hand-edited.
  - [ ] Retained class entries still have valid reasons.
- **Dependencies:** 3.2–3.4.
- **Effort:** XL.
- **Required Skills/Rules:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`.

### Phase 4: API Trust Boundary And OpenAPI Contract

- **Goal:** Make API-owned models record-appropriate, bind trusted facts at established boundaries, and regenerate the intentional breaking contract.
- **Depends on:** Phase 3.
- **Relevant files:** `src/Explore.API/Controllers/**/*.cs`; `src/Explore.API/Models/**/*.cs`; API serialization/OpenAPI catalogs; `schemas/openapi_islamu-event.json` (generated); `docs/API_CONTRACT_INVENTORY.md` (generated); `docs/API_CHANGELOG.md`; API integration/architecture tests.
- **Related skills/rules:** `auth-patterns`, `criticality-guardrail`, `cqrs-mediatr-guidelines`, `epistemic-mad-review`, API/Auth/Test rules.
- **Acceptance criteria:**
  - Controllers use route/context/principal authority and construct immutable commands.
  - Query/form/inheritance models remain classes when framework-populated mutability is correct.
  - JSON body record binding, validation, PATCH semantics, ProblemDetails, route names, operation IDs, and HAL are correct.
  - OpenAPI and contract inventory are regenerated, never hand-edited.
  - Tier 1 identity changes achieve scoped Stryker mutation score above 85% and pass anonymized MAD review.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Fix the source DTO/controller/model and regenerate. Never patch generated schema/client text. A discovered authorization mismatch blocks the phase; do not preserve the old body authority through a shim.

#### Task 4.1: Author Failing HTTP And Trust-Boundary Invariant Breakers
- **Type:** create/modify
- **Layer:** Tests
- **Files:** focused tests under `tests/Event.API.IntegrationTests/Features/`, `Authentication/`, and `Hosting/`; OpenAPI contract tests; body-authority disposition baseline.
- **Description:** Add RED tests for body tenant/user spoofing, wrong tenant header/context, missing identity, route/body conflict, PATCH absent/null/value behavior, positional validation metadata, stable ProblemDetails codes, and OpenAPI required/null schemas.
- **Acceptance Criteria:**
  - [ ] Forged body authority cannot affect command authorization or persistence.
  - [ ] Missing/conflicting trusted identity fails with existing 401/403/404 policy.
  - [ ] Legitimate target IDs remain usable only for their explicit operation.
  - [ ] Operation IDs and route names remain stable unless the breaking change explicitly documents them.
- **Dependencies:** 3.3.
- **Effort:** XL.
- **Required Skills/Rules:** `criticality-guardrail`, `auth-patterns`, API/Test rules.

#### Task 4.2: Refactor Controllers And API Models
- **Type:** modify
- **Layer:** API
- **Files:** affected controllers and `Models/**/*.cs` identified by Task 4.1; trusted context dependencies; direct MCP/BFF adapters where the same command is constructed.
- **Description:** Use `ExploreControllerBase`, `PlatformIdentityPrincipalExtensions`, `IUserContext`, `ITenantContext`, and route facts according to existing authority. Convert only immutable API-owned models; retain query/form/inheritance/`IValidatableObject` classes where class semantics fit.
- **Acceptance Criteria:**
  - [ ] Controllers never re-derive raw claims or resolve services from `RequestServices`.
  - [ ] No current tenant/user field is accepted from JSON.
  - [ ] Commands receive only semantically used trusted facts.
  - [ ] Write endpoints remain authorized and failures remain RFC 7807 ProblemDetails.
- **Dependencies:** 4.1.
- **Effort:** XL.
- **Required Skills/Rules:** `auth-patterns`, `cqrs-mediatr-guidelines`, API/Auth rules.

#### Task 4.3: Regenerate And Document The API Contract
- **Type:** modify
- **Layer:** API/Docs
- **Files:** generated `schemas/openapi_islamu-event.json`; generated `docs/API_CONTRACT_INVENTORY.md`; `docs/API_CHANGELOG.md`; API OpenAPI catalogs/tests.
- **Description:** Build `Explore.API` to regenerate OpenAPI and run the inventory generator through the documented workflow. Record every intentional body/requiredness/nullability break.
- **Acceptance Criteria:**
  - [ ] Generated artifacts come only from documented generators.
  - [ ] OpenAPI body schemas omit current-authority fields.
  - [ ] Required/nullability metadata matches runtime binding.
  - [ ] `docs/API_CHANGELOG.md` explains migration with no compatibility alias.
- **Dependencies:** 4.2.
- **Effort:** L.
- **Required Skills/Rules:** API rule, `openapi-contract-change` intent.

#### Task 4.4: Close Tier 1 Mutation And Adversarial Review Evidence
- **Type:** investigate/modify
- **Layer:** Security/Tests
- **Files:** `stryker-config.json` (existing, modify only if scoped configuration is repository-approved); changed identity tests/code; new structured MAD findings artifact under `.omo/start-work/artifacts/records-adoption/phase4/`.
- **Description:** Run scoped Stryker evidence for changed identity/tenancy code using the existing configuration and require a score above 85%. Run 2–3 anonymized specialist reviews with Security weighted at 60%; every accepted defect must add a reproducible invariant-breaker test and correction before phase close.
- **Acceptance Criteria:**
  - [ ] Scoped mutation score is above 85%.
  - [ ] Review output contains no agent/model identity.
  - [ ] Security, API contract, and maintainability arguments are independently generated and weighted.
  - [ ] Accepted findings have tests and fixes; unresolved critical findings block.
- **Dependencies:** 4.1–4.3.
- **Effort:** L.
- **Required Skills/Rules:** `criticality-guardrail`, `epistemic-mad-review`, `auth-patterns`.

### Phase 5: Generated Client And Blazor Immutable State

- **Goal:** Regenerate the NSwag client, repair Blazor consumers, and convert only immutable presentation-owned snapshots.
- **Depends on:** Phase 4.
- **Relevant files:** `src/Explore.Blazor.Client/nswag.json`; generated `Clients/EventApiClient.g.cs`; `Serialization/AppJsonSerializerContext.cs`; services/validators; immutable model/filter/dialog candidates; mutable edit-state exclusions; Blazor tests.
- **Related skills/rules:** `blazor-ui-conventions`, `auth-patterns`, Blazor/Test rules.
- **Acceptance criteria:**
  - NSwag output is regenerated and remains generated classes.
  - Services compile against intentional contract breaks.
  - Immutable local result/filter/dialog state uses records.
  - Mutable forms/component edit models remain classes.
  - HAL remains the only UI action authority.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Correct the OpenAPI source and regenerate when client shape is wrong. Never patch `EventApiClient.g.cs`. Reclassify local state as a retained class when form binding or mutable identity is intentional.

#### Task 5.1: Author Failing Client Serialization And State Specifications
- **Type:** create/modify
- **Layer:** Tests
- **Files:** `tests/Explore.Blazor.Client.Tests/Services/EventApiClientSerializationTests.cs`; candidate model/component tests; validator tests.
- **Description:** Add RED tests for regenerated request/response JSON, required/null behavior, absence of authority fields, service construction, immutable snapshot equality/`with` variants, and mutable edit-state exclusions.
- **Acceptance Criteria:**
  - [ ] Tests fail against the stale generated client or mutable candidate state.
  - [ ] No UI test inspects roles/claims for actions.
  - [ ] Record tests assert consumer behavior, not compiler implementation.
- **Dependencies:** 4.3.
- **Effort:** L.
- **Required Skills/Rules:** `blazor-ui-conventions`, Blazor/Test rules.

#### Task 5.2: Regenerate NSwag And Repair Client Services
- **Type:** modify/generated
- **Layer:** Blazor
- **Files:** generated `Clients/EventApiClient.g.cs`; verified services/validators/components consuming changed contracts.
- **Description:** Run the documented `GenerateApiClient` target against the committed OpenAPI schema, then update handwritten consumers. Keep generated DTO class representation unchanged.
- **Acceptance Criteria:**
  - [ ] Generated client is deterministic from `nswag.json`.
  - [ ] No manual generated-code changes exist.
  - [ ] Services send only client-owned body data.
  - [ ] HAL extension data and ProblemDetails handling remain intact.
- **Dependencies:** 5.1.
- **Effort:** XL.
- **Required Skills/Rules:** `blazor-ui-conventions`, API/Blazor rules.

#### Task 5.3: Convert Immutable Presentation Models
- **Type:** modify
- **Layer:** Blazor
- **Files:** Phase 0 Blazor result/filter/dialog candidates; direct component/service consumers; retained-class baseline.
- **Description:** Convert immutable local snapshots to sealed records, harden collection members where needed, and use `with` updates where replacement semantics improve state handling. Retain form/edit/component identity models as classes.
- **Acceptance Criteria:**
  - [ ] Equality/rerender behavior is intentional.
  - [ ] Mutable edit state is not converted.
  - [ ] No generated DTO is manually wrapped solely to make it a record.
  - [ ] Accessibility and HAL behavior are unchanged.
- **Dependencies:** 5.1–5.2.
- **Effort:** L.
- **Required Skills/Rules:** `blazor-ui-conventions`, Blazor rule.

#### Task 5.4: Align Blazor JSON Source Generation
- **Type:** modify
- **Layer:** Blazor
- **Files:** `src/Explore.Blazor.Client/Serialization/AppJsonSerializerContext.cs`; affected serialization tests; baseline.
- **Description:** Register local records and regenerated DTOs needed for AOT-safe serialization, remove stale entries, and clear resolved presentation baseline debt.
- **Acceptance Criteria:**
  - [ ] AOT JSON context covers every used contract.
  - [ ] No provider credential contract is added.
  - [ ] Source-generated round trips match service settings.
- **Dependencies:** 5.2–5.3.
- **Effort:** M.
- **Required Skills/Rules:** `blazor-ui-conventions`, Blazor/Test rules.

### Phase 6: Governance Closure And Release Contribution

- **Goal:** Close all eligible debt, synchronize documentation, prove final architecture policy, and prepare the governed breaking-change contribution.
- **Depends on:** Phase 5.
- **Relevant files:** record ratchet/baselines; `docs/GOVERNANCE.md`, `ARCHITECTURE.md`, `API.md`, `API_CHANGELOG.md`, `OUTBOX_PATTERN.md`, `BLAZOR.md`, relevant `.agents/rules/*.md`; new `docs/releases/changes/CHG-2026-0010.yaml`.
- **Related skills/rules:** `review-pr`, `criticality-guardrail`, `epistemic-mad-review`, release governance, all matched rules.
- **Acceptance criteria:**
  - No eligible MediatR request remains a class.
  - Every retained DTO/API/UI class has a current reason and no stale baseline.
  - Generated artifacts are current and deterministic.
  - Documentation teaches the classification and trust-boundary policy.
  - Intent-mandated `Event.Architecture.Tests` passes.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** A failing final ratchet returns work to the owning earlier phase; do not add unexplained baseline entries. A release-fragment validation failure blocks contribution preparation.

#### Task 6.1: Tighten Final Ratchets And Remove Resolved Debt
- **Type:** modify
- **Layer:** Tests/Architecture
- **Files:** `RecordContractArchitectureTests.cs`; both baseline JSON files.
- **Description:** Author the final RED expectations for zero eligible request-class debt, no stale exceptions, no current-authority body properties, and complete retained-class reasons; then remove only genuinely resolved entries.
- **Acceptance Criteria:**
  - [ ] Concrete MediatR class baseline is empty unless an externally imposed exception is approved and documented.
  - [ ] DTO baseline contains only semantic/framework/generated exclusions.
  - [ ] Body dispositions contain no current-authority exceptions.
  - [ ] New debt fails without a baseline update and review.
- **Dependencies:** 5.4.
- **Effort:** M.
- **Required Skills/Rules:** `review-pr`, Architecture/Test rules.

#### Task 6.2: Synchronize Architecture And Contributor Documentation
- **Type:** modify
- **Layer:** Docs
- **Files:** `docs/GOVERNANCE.md`; `docs/ARCHITECTURE.md`; `docs/API.md`; `docs/API_CHANGELOG.md`; `docs/OUTBOX_PATTERN.md`; `docs/BLAZOR.md`; relevant `.agents/rules/*.md`; I-VSD link.
- **Description:** Document final declaration policy, trusted request flow, generated ownership, PATCH/model-binding rules, collection/equality caveats, outbox payload/entity split, Blazor state split, and no-shim migration.
- **Acceptance Criteria:**
  - [ ] Docs describe implemented reality, not roadmap claims.
  - [ ] All links and source-of-truth statements agree.
  - [ ] No documentation claims deep immutability, automatic thread safety, or structural collection equality.
- **Dependencies:** 6.1.
- **Effort:** M.
- **Required Skills/Rules:** all matched docs/rules.

#### Task 6.3: Changelog Contribution And Final Commit Composition
- **Type:** create/compose
- **Layer:** Release
- **Files:** `docs/releases/changes/CHG-2026-0010.yaml` (new); no commit is created without explicit user authorization.
- **Description:** Create the append-only Tier 2 fragment covering Breaking, Security, Migration, Configuration, OpenAPI, and Operator impacts. Validate it through repository release policy. Compose—but do not execute without approval—the terminal commit:
  `refactor(architecture)!: adopt semantics-first record contracts`
  with `BREAKING CHANGE:` and `Change-Id: CHG-2026-0010`.
- **Acceptance Criteria:**
  - [ ] Fragment uses an unclaimed stable Change ID and valid `architecture` scope.
  - [ ] All six impact dispositions have references and truthful detail.
  - [ ] `ReleaseInputPolicy` validation passes.
  - [ ] Breaking commit is not marked `Changelog: skip`.
  - [ ] No commit, tag, push, or publish occurs unless the user explicitly requests it.
- **Dependencies:** 6.2 and all functional tasks.
- **Effort:** S.
- **Required Skills/Rules:** release governance, `conventional-commit`.

## 7. Testing Strategy

### 7.1 Test-first invariant anchors

- **Architecture:** `Event.Architecture.Tests` discovers request/DTO/body surfaces and enforces shrinking baselines.
- **Domain:** `Event.Domain.UnitTests` proves value-object invariants, equality, copy semantics, and invalid values.
- **Application:** `Event.Application.UnitTests` proves MediatR construction, authorization facts, mapping, JSON, PATCH groups, and payload round trips.
- **API:** `Event.API.IntegrationTests` proves body tampering cannot become current authority, model binding/validation remains correct, and OpenAPI reflects runtime contracts.
- **Blazor:** `Explore.Blazor.Client.Tests` proves generated-client serialization and immutable-versus-editable presentation semantics.

### 7.2 Record-aware adversarial scenarios

1. Use `with` to change only tenant/user/resource facts on an otherwise valid command and prove denial.
2. Send body fields matching former current-authority names and prove they are rejected or absent from schema.
3. Compare equal scalar records and deliberately unequal variants where equality is consumed.
4. Mutate an input list after construction and prove an immutable snapshot is unchanged.
5. Prove array/list sequence comparisons explicitly; never infer deep equality from the containing record.
6. Deserialize missing, null, malformed, and extra PATCH members.
7. Verify constructor-parameter validation metadata produces the expected ProblemDetails.
8. Round-trip versioned outbox payloads without logging their sensitive contents.
9. Regenerate OpenAPI/NSwag twice and require no second-run drift.
10. Scan captured logs for body, token, free-text, current-user, and tenant values in Tier 1 slices.

### 7.3 Phase verification lanes

| Phase | One selected test project | Reason |
|---|---|---|
| 0 | `Event.Architecture.Tests` | Ratchet and baseline behavior. |
| 1 | `Event.Domain.UnitTests` | Value semantics and invariants. |
| 2 | `Event.Application.UnitTests` | CQRS request/handler/authorization behavior. |
| 3 | `Event.Application.UnitTests` | Different reason: DTO mapping, JSON, PATCH, and payload behavior. |
| 4 | `Event.API.IntegrationTests` | Intent-mandated live HTTP/model-binding/OpenAPI boundary. |
| 5 | `Explore.Blazor.Client.Tests` | Generated-client and local presentation state. |
| 6 | `Event.Architecture.Tests` | Intent-mandated final Clean Architecture and ratchet proof. |

Tier 1 Task 4.4 adds one scoped Stryker mutation run and anonymized MAD review as mandatory criticality evidence. They are not substitutes for or repetitions of the phase-end project test.

## 8. Documentation, Configuration, And Operations Impact

### Documentation

- Update `docs/GOVERNANCE.md` with the authoritative type-selection policy.
- Update `docs/ARCHITECTURE.md` with horizontal ownership and immutable request flow.
- Update `docs/API.md` and mandatory `docs/API_CHANGELOG.md` for body/requiredness/nullability breaks and generation workflow.
- Update `docs/OUTBOX_PATTERN.md` only where payload-record guidance changes implemented reality.
- Update `docs/BLAZOR.md` with generated-class versus local-record ownership.
- Update matching `.agents/rules/*.md` so future agents apply the final rule.
- Keep the [I-VSD report](../../../islamic-value-sensitive-design/i-vsd-records-adoption.md) linked from all workstream artifacts.

### Configuration and generated artifacts

- No runtime setting or environment variable is planned.
- `stryker-config.json` is reused; modify only if a verified scoped configuration is required and remains general-purpose.
- `schemas/openapi_islamu-event.json`, `docs/API_CONTRACT_INVENTORY.md`, and `EventApiClient.g.cs` are generator outputs.
- No EF migration or model snapshot is expected.

### 8.1 Release & Changelog Strategy

This is **Tier 2 — High-Impact / Breaking / Security / OpenAPI**:

- Create `docs/releases/changes/CHG-2026-0010.yaml` in the final task.
- Proposed terminal commit composition: `refactor(architecture)!: adopt semantics-first record contracts`.
- Required trailers: non-empty `BREAKING CHANGE:` and `Change-Id: CHG-2026-0010`.
- `Changelog: skip` is forbidden because the change is breaking and OpenAPI-visible.
- The fragment must classify:
  - **Breaking:** request body, requiredness, nullability, and generated-client changes;
  - **Security:** trusted user/tenant authority removal from bodies;
  - **Migration:** no database migration; source/client migration required;
  - **Configuration:** not applicable unless Stryker/generator settings change;
  - **OpenAPI:** regenerated schema/inventory/client;
  - **Operator:** coordinated API/Blazor deployment and no old-client support.

## 9. Islamic Value-Sensitive Design (I-VSD) & Moral Boundaries

See [I-VSD Provider-Responsibility Consultation: C# Records Adoption](../../../islamic-value-sensitive-design/i-vsd-records-adoption.md).

| Principle / stakeholder | Provider-controlled risk | Mitigation | Evidence / task | Uncertainty |
|---|---|---|---|---|
| Trust, Justice, Rights of People / tenants and users | Body-controlled current identity crosses authority boundary | Trusted context/route binding and forged-body tests | 0.1–0.2, 2.1–2.2, 3.3, 4.1–4.2 | Full body catalog produced in Phase 0. |
| Truthfulness / maintainers | “Record” falsely implies deep immutability/thread safety | Collection policy and accurate docs | 1.1–1.2, 2.3, 3.1–3.4, 6.2 | Candidate-specific copying requirements. |
| Non-harm, Promise-keeping / API consumers | Silent model-binding, validation, or generated-client drift | Red contract tests and deterministic generation | 3.1, 4.1–4.3, 5.1–5.2 | Exact OpenAPI diff unknown until Phase 4. |
| Avoiding spying / data subjects | Generated `ToString()` or destructuring leaks PII | Zero-PII tests and bounded logging | 2.3, 3.4, 4.4 | Sensitive contract inventory from Phase 0. |
| Excellence / maintainers and self-hosters | Big-bang unclassified codemod is unauditable | Horizontal phases, shrinking ratchet, final architecture gate | all phases | XL scope may reveal more retained classes. |

No religious-legal ruling is requested or issued. Finance, moderation, monetization, and religious-content policy are not applicable. Security/privacy concerns require technical and, where jurisdictionally relevant, legal review; no scholarly escalation is currently necessary.

## 10. Security, Authorization, Privacy, And Abuse Considerations

- **Authentication:** No JWT validation flow changes. Never add a second claim-extraction helper.
- **Authorization:** Preserve `AuthorizationBehavior`, `ISecureRequest`, resource attributes, Cerbos/fallback parity, and fail-closed facts.
- **Tenant isolation:** `ITenantContext` remains authoritative; no body tenant or broad query-filter bypass.
- **Body over-posting:** Remove current-authority fields and classify legitimate target identifiers.
- **HAL:** Record conversion does not change `_links` as UI action authority.
- **ProblemDetails:** Record/model-binding failures remain RFC 7807 with stable machine codes.
- **Privacy:** Do not log records wholesale. Outbox payload and DTO tests use synthetic non-PII values.
- **Abuse/rate limiting/idempotency:** Not behaviorally changed; API tests ensure attributes/headers remain.
- **Mutation/review:** Tier 1 identity slices require >85% scoped mutation evidence and anonymized MAD review.

## 11. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

| Concern | Classification | Rationale |
|---|---|---|
| Multi-tenancy | Applicable, critical | Body tenant debt and command facts are in scope; forged/missing tenant must fail closed. |
| Federation | Applicable to construction callers only | MCP/federation commands and payload snapshots may be records; DID/PDS authority does not change. |
| Localization | Needs bounded verification | DTO requiredness and generated clients may affect localized forms; no resource/copy change is planned. |
| Accessibility | Not behaviorally changed | No UI layout or interaction redesign; immutable state conversion must not alter rendering or focus behavior. |
| Product behavior | Not applicable | Type semantics and trust boundaries only; no new capability or workflow. |
| Self-hosting | Applicable operationally | API and Blazor must upgrade together; no mandatory external service or dependency is added. |

## 12. Observability And Operations

- Do not log or destructure complete command/DTO/payload records.
- Keep existing bounded event names, failure codes, and correlation IDs.
- No new metric, trace, health check, dashboard, or alert is needed solely for syntax migration.
- Record binding/serialization failures continue through current ProblemDetails and request logging.
- Operators receive API changelog/release-fragment guidance for coordinated API/Blazor upgrades.
- Generated contract drift remains visible through the existing workflow.

## 13. Migration And Compatibility Plan

- **Database/schema:** No migration planned. An EF model delta discovered in Phase 1 blocks and requires separate migration intent/classification.
- **Source migration:** Object initializers and property assignments become constructor/named-record construction in the owning horizontal phase.
- **API migration:** Intentional pre-v1 body/requiredness/nullability breaks are accepted and documented. No duplicate fields, aliases, custom compatibility converters, or obsolete constructors.
- **Generated client:** Regenerate from the final OpenAPI schema; never hand-edit.
- **Outbox:** Declaration-only conversions preserve payload field names/version where possible. If a payload shape must break, use its existing version/event-type mechanism and document the development queue drain/reset requirement; do not add a compatibility reader or automatically destroy queued data.
- **Deployment:** Final release requires coordinated API and Blazor deployment. Intermediate horizontal phases are implementation checkpoints, not supported release points.
- **Rollback:** Source rollback is forward-fix only after deployment because old generated clients are unsupported. Database rollback is not expected. Do not perform destructive data reset without explicit approval.

## 14. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection signal | Owner/task |
|---|---:|---:|---|---|---|
| Candidate inventory misses declarations or generated ownership | Medium | High | Compiled discovery + source classification + stale baseline tests | Unclassified/stale test failure | 0.1–0.2 |
| Body target ID is mistaken for current authority or vice versa | Medium | Critical | Disposition manifest, controller/caller tracing, forged-body tests | Wrong-tenant/authorization test failure | 0.1–0.2, 4.1–4.2 |
| Positional same-type arguments are swapped | Medium | High | Nominal records for long/ambiguous contracts; named arguments | Semantic unit test failure | 2.2, 3.2–3.3 |
| ASP.NET validation metadata is left on properties | Medium | High | Parameter metadata tests and OpenAPI requiredness checks | Unexpected 200/400 or schema delta | 3.1, 4.1 |
| AutoMapper/LINQ projection cannot use constructor | Medium | High | Mapping/projection tests; switch to nominal record | Mapping configuration/runtime test failure | 3.1–3.2, 3.5 |
| Collection remains mutable after record construction | High | Medium | Defensive copies where invariant requires; mutation tests | Post-construction mutation changes state | 1.1–1.2, 2.3, 3.1–3.4, 5.3 |
| Record equality is assumed to compare collection contents | Medium | Medium | Explicit sequence assertions/comparers | Equality test disagreement | all record test tasks |
| Generated `ToString()` leaks sensitive data | Medium | Critical | No whole-record logging; zero-PII sink tests | Captured sensitive value in logs | 2.3, 3.4, 4.4 |
| OpenAPI and NSwag artifacts drift | High | High | Documented generation and deterministic second run | CI drift or client serialization failure | 4.3, 5.2 |
| EF value-object mapping changes schema | Low | High | Model evidence; block/reclassify migration | Model snapshot/migration diff | 1.3 |
| Horizontal phase is accidentally treated as releasable | Medium | High | Context/tasks mark workstream incomplete until Phase 6 | API/client version mismatch | all phases |
| XL migration hides review defects | High | High | Shrinking batches within layer phase, Tier 1 MAD, one gate per phase | Baseline growth, mutation survivors, review finding | 0.2, 4.4, 6.1 |

## 15. Success Metrics And Definition Of Done

1. Every eligible concrete MediatR request is a record; retained exceptions are externally justified and ratcheted.
2. Every handwritten DTO class is either converted or has a current semantic/framework reason and removal trigger.
3. Every API body contains client-owned fields only; no current-authority disposition remains.
4. Record-aware tests use equality/`with`/immutable construction only where behavior benefits.
5. Collection-bearing immutable contracts cannot be altered through retained mutable inputs where that invariant is claimed.
6. EF/outbox entities, handlers, validators, controllers, generated DTOs, mutable UI state, and `BaseCommandResponse<T>` remain correctly class-based.
7. OpenAPI, API inventory, and NSwag client regenerate deterministically with documented breaking changes.
8. Tier 1 changed identity code has scoped Stryker mutation score above 85%, zero-PII log evidence, and accepted anonymized MAD findings resolved.
9. Every phase’s one Release build and selected test project pass once.
10. Final `Event.Architecture.Tests` passes with no stale baseline.
11. `CHG-2026-0010.yaml` validates, and no compatibility shim or unauthorized commit exists.

## 16. Implementation Agent Contract — KEEP DEV DOCS CURRENT

1. At first implementation start or cold resume, read `records-adoption-context.md` and the current item in `records-adoption-tasks.md`, then retrieve only the required plan heading.
2. Maintain a `path + heading/symbol + revision` ledger; do not reread unchanged artifacts in one session.
3. Start from the highest-priority unchecked task unless the user overrides it.
4. Treat `records-adoption-tasks.md` as the hot ledger. Mark substantial tasks in progress and check them immediately when implementation acceptance passes; reconcile small tasks by phase end.
5. Keep implementation-task completion separate from phase verification.
6. Update completed count, current priority, next slice, discovered tasks, deferred work, and date whenever task state changes.
7. Update context after a phase, decision, blocker, failed validation, material discovery, or handoff.
8. Update this plan only when scope, architecture, sequence, acceptance, risk, or validation changes.
9. Record failed validation and recovery action without checking the phase complete.
10. Before pause/compaction/transfer/PR, reconcile tasks, add a dated handoff, and identify unrelated dirty files.
11. Run phase verification only after all phase tasks: one Release build and one selected project test. Do not start the app, browser, Docker, Aspire, or live services.
12. Never hand-edit generated OpenAPI, API inventory, NSwag client, EF migrations, or snapshots.
13. Never add a baseline entry solely to turn a failing ratchet green; every retained class needs an evidence-backed reason.
14. Never report completion when repository reality and the ledger disagree.

Every implementation summary must teach:

- what changed and why;
- the record/class selection and Clean Architecture owner;
- important contracts, handlers, controllers, generators, and consumers;
- identity/tenant and serialization flow;
- record equality/collection/`with` semantics used;
- tests and generated artifacts verified;
- remaining tasks and dev-doc state.

## 17. Progress Reporting Contract

```text
Implemented: developer teaching summary
Verified: exact evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: tasks yes/no; context/plan updated or unchanged with reason
```

## 18. Potential Risks & Unknowns

The most likely failure is treating the Phase 0 baseline as an administrative inventory instead of a semantic contract. If retained-class reasons are vague, generated ownership is inferred by path alone, or current-authority identifiers are classified without tracing their callers, the migration can become keyword churn while preserving the security and mutation debt it was meant to remove.

The second risk is horizontal sequencing scale. Clean Architecture ownership is clear, but hundreds of downstream constructors and generated-client consumers may make one layer phase too large for a single contributor. Implementation may split a phase into smaller **batches of the same layer/category** while keeping one phase-end gate and without reverting to feature vertical slices.

The exact candidate set, OpenAPI delta, and mutation survivors are intentionally assigned to bounded implementation tasks rather than guessed in this plan.

## 19. Session Handoff — 2026-08-24 Europe/Brussels

### Resume location and safety

- **Isolated implementation worktree:** `/home/amir/ISLAMU/Github/Event-records-adoption`
- **Original detached base:** `aa74b645c`
- **Shared main worktree:** contains unrelated payment/privacy work and must not be modified, reverted, or used for records-adoption verification.
- **Live work:** no child agent, build, test, generator, Stryker, API host, or other persistent process remains active.
- **Git:** no commit, tag, push, or publish was performed or authorized.
- **Dirty scope:** the isolated worktree intentionally contains the accumulated records-adoption changes from Phases 0–4. Do not discard or overwrite them.

### Authoritative implementation state

The repository evidence below is newer than some task-status checkboxes and the older resume context. Treat this handoff as the authoritative resume point, then reconcile `records-adoption-tasks.md`, `records-adoption-context.md`, this plan's task statuses, and the native todo list before resuming implementation.

- **Phases 0–3:** implemented and independently verified.
- **Phase 4 Tasks 4.1–4.3:** implemented and independently verified.
- **Task 4.4:** in progress.
  - The scoped Tier 1 Stryker run is complete and passed.
  - The required anonymized Epistemic MAD review has **not** started.
  - The Phase 4 full `Event.API.IntegrationTests` gate has **not** yet run after MAD closeout.
- **Phases 5–6:** not started.

### Implemented contract state

- `806/806` compiled concrete MediatR requests are records; request classes: `0`.
- Public mutable request setters were reduced from `19` to `6`; all six are required by `IEventSessionLifecycleTransitionCommand`.
- `816/816` eligible handwritten Application contracts are records.
- The Application class baseline contains exactly `8` retained `BaseCommandResponse<>` hierarchy classes.
- All `28/28` HTTP/input DTO candidates are sealed nominal records.
- All `8/8` current-user/current-tenant body authority members were removed.
- The body-authority baseline contains exactly `7` legitimate target identifiers and no current-authority exceptions.
- All `13/13` eligible handwritten API boundary models are sealed records.
- Outbox classification is `49` immutable payload records, `5` lifecycle/state classes, and `1` static helper.
- `NotificationFanoutSnapshotV1` defensively snapshots both recipient arrays.
- `PerformanceBehavior` logs bounded request type and elapsed metadata only; it does not destructure whole requests.

### Generated contract state

The following were regenerated only through repository-owned generators:

- `artifacts/openapi/Explore.json`
- `artifacts/api-inventory/Explore.inventory.json`
- `src/Explore.Blazor.Client/Api/ExploreApiClient.g.cs`

Generator commands already run successfully:

```bash
.ci/scripts/Generate-OpenApi.sh --write
.ci/scripts/Generate-ApiSurfaceInventory.sh --write
.ci/scripts/Generate-OpenApi.sh --check
.ci/scripts/Generate-ApiSurfaceInventory.sh --check
```

Current generated-contract evidence:

- all `28/28` input record schemas are present;
- all `8/8` removed authority properties are absent;
- all `7/7` legitimate target identifiers remain;
- all `13/13` handwritten API record schemas are represented;
- OpenAPI and inventory check modes are idempotent;
- runtime and checked-in OpenAPI contract tests pass.

`docs/API_CONTRACT.md` records the implemented record-input ownership, authority-versus-target distinction, and generator commands.

### Latest verification evidence

- Phase 3 Release solution build: passed with `0` errors.
- Full `Event.Application.UnitTests`: `4,022/4,022` passed.
- Record architecture tests: `10/10` passed.
- Mapping/JSON/PATCH tests: `12/12` passed.
- HTTP input authority tests: `10/10` passed.
- Task 2 authorization tests: `12/12` passed.
- API trust-boundary invariant tests: `9/9` passed.
- API record contract tests: `5/5` passed.
- OpenAPI contract tests: `4/4` passed.
- Source-generated JSON-context tests: `10/10` passed.
- API record/authority endpoint tests: `18/18` passed.
- LSP diagnostics on changed scopes: `0` errors.
- Generator idempotency and `git diff --check`: passed.

Tier 1 mutation result:

- scope: `DeleteUserController`, `RegisterUserController`, and `RegisterTenantController`;
- total mutants: `36`;
- killed: `32`;
- survived: `2`;
- no coverage: `2`;
- timeout: `0`;
- score: `88.89%`, above the required `85%`;
- report: `artifacts/stryker-output/records-adoption-tier1/reports/mutation-report.html`;
- evidence: `.omo/start-work/evidence/records-adoption/4.4-mutation.md`.

The surviving/uncovered mutants were reviewed as response-logging/correlation helpers and synthetic rollback branches; none bypass current-authority removal or trusted-context selection. No threshold weakening or test exclusion was used.

### Exact next work

1. Reconcile the stale plan/context/tasks/native-todo statuses to the authoritative state above. Do not rerun completed implementation waves.
2. Resume **Task 4.4** by loading `.agents/skills/epistemic-mad-review/SKILL.md`.
3. Run MAD Round A with five independent, read-only reviewers covering:
   - Domain/Clean Architecture;
   - Security/authorization and tenant isolation;
   - data/serialization/OpenAPI/generated-client contracts;
   - tests/mutation/nondeterminism;
   - operations/maintainability/generator workflow.
4. Before Round B, remove reviewer identity, model/provider, task/session metadata, timestamps, and token data. Label only `Review A` through `Review E`.
5. Present the anonymous findings to each reviewer in a different deterministic order with a recorded fixed seed. Require agree/partially-agree/disagree, evidence, missing risk, and ranked votes.
6. Perform Round C weighted adjudication based on demonstrated expertise and direct repository evidence. Preserve all security/privacy dissent even when minority-held.
7. Write the durable review artifact under `.omo/start-work/artifacts/records-adoption/phase4/` with reviewed files/flows, commands, anonymous critiques, votes, adjudication, majority conclusions, dissent, residual risks, and follow-up tasks. Do not include agent/model identity.
8. For every accepted defect, add a reproducible invariant-breaker test first, fix it minimally, and rerun only affected focused checks.
9. Independently verify the Stryker evidence and MAD artifact.
10. Run the Phase 4 closeout gates once after all accepted findings are resolved:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
```

11. Mark Task 4.4 and Phase 4 complete only after both gates pass.
12. Continue with Task 5.1, **Author Failing Client Serialization And State Specifications**.

### Important evidence paths

- `.omo/start-work/evidence/records-adoption/3.5-final-integration.md`
- `.omo/start-work/evidence/records-adoption/4.1-http-invariants.md`
- `.omo/start-work/evidence/records-adoption/4.1-openapi-red.md`
- `.omo/start-work/evidence/records-adoption/4.2-api-refactor.md`
- `.omo/start-work/evidence/records-adoption/4.3-contract-generation.md`
- `.omo/start-work/evidence/records-adoption/4.4-mutation.md`
- `tests/Event.Architecture.Tests/RecordContractArchitectureTests.cs`
- `tests/Event.Architecture.Tests/ApiRecordContractArchitectureTests.cs`
- `tests/Event.Architecture.Tests/RecordInputOpenApiContractTests.cs`
- `tests/Event.Architecture.Tests/Baselines/record-contract-class-baseline.json`
- `tests/Event.Architecture.Tests/Baselines/http-body-authority-dispositions.json`

### Known constraints and non-blockers

- AnySearch MCP and Context7 MCP were not registered; official Microsoft documentation and available repository evidence were used instead.
- The unrelated main-worktree PII inventory failure remains outside this workstream and does not block verification in the isolated worktree.
- Do not hand-edit generated OpenAPI, inventory, NSwag client, EF migrations, or model snapshots.
- Do not add compatibility aliases, constructors, duplicate JSON fields, or old-client shims.
- Do not start Phase 5 before Task 4.4 MAD and the full Phase 4 gate are complete.
