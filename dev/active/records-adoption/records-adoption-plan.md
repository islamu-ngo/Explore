<!-- ABOUTME: Implementation plan for ambitious, semantics-first C# record adoption across ISLAMU Event. -->
<!-- ABOUTME: Sequences horizontal Clean Architecture migration, trusted identity hardening, contract generation, and tests. -->

# Records Adoption — Implementation Plan

Last Updated: 2026-08-25 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Modernize suitable DTOs, MediatR requests, immutable outbox payloads, domain value objects, and immutable Blazor state with C# records while retaining classes where identity, lifecycle mutation, generated ownership, framework binding, or editable state makes class semantics correct.
- **Task directory:** `dev/active/records-adoption/`
- **Planning status:** Approved; implementation active in isolated worktree `/home/amir/ISLAMU/Github/Event-records-adoption`.
- **Matched intents:** `openapi-contract-change` and `add-ef-migration` from `.agents/contract/intents.yaml`, plus a fallback cross-layer refactor contract for immutable Application results and collection contracts.
- **Criticality:** The OpenAPI work is standard, while body-authority corrections and generated EF migrations activate Tier 1 Security guardrails. Those tasks require advanced-model implementation, invariant-breaker tests, fail-closed tenant behavior, multi-provider migration evidence, zero-PII checks where sensitive values are involved, scoped mutation evidence above 85% for changed security logic, and anonymized Epistemic MAD review.
- **Relevant skills:** `implementation-plan`, `i-vsd`, `grill-me`, `criticality-guardrail`, `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `auth-patterns`, `outbox-pattern`, `dotnet-efcore-guidelines`, `blazor-ui-conventions`, `ip-clean-room`, `agentic-research`, `ast-grep`, `epistemic-mad-review`.
- **Relevant rules:** `.agents/rules/application-layer.md`, `api-controllers.md`, `domain.md`, `blazor-client.md`, `tests.md`, `auth-trust-boundaries.md`, `work-criticality-matrix.md`, and `ip-clean-room.md`.
- **Primary layers:** Domain, Application, Persistence, API, generated OpenAPI/NSwag contract, and Blazor Client.
- **Complexity:** XL. The Application project alone has 657 source files matching `*Dto*.cs`; compiled request discovery, object initializers, AutoMapper projections, JSON source generation, OpenAPI generation, generated client code, and UI consumers create a repository-wide blast radius.
- **I-VSD Document:** [C# Records Adoption I-VSD consultation](../../../islamic-value-sensitive-design/i-vsd-records-adoption.md)
- **Grill-Me Intake:**
  - Accepted ambitious classification-first adoption rather than keyword uniformity.
  - Accepted explicit command `UserId` / `TenantId` only when authorization or business intent uses those facts; ambient trusted contexts remain valid where identity is not request data.
  - Accepted a permanent, shrinking architecture ratchet for MediatR requests, DTO classes, and body-authority dispositions.
  - Rejected feature-oriented vertical slices. Migration ownership is horizontal: Domain → Application → API/OpenAPI → generated client/Blazor, with downstream compilation repairs allowed before each phase gate.
  - The original bounded scope retained `BaseCommandResponse<T>` and limited collection hardening to converted contracts; the 2026-08-25 re-baseline supersedes both limits.
  - Explicitly approved intentional development-stage breaking API changes, provided tests, OpenAPI, generated clients, and release evidence move together by workstream completion. Compatibility shims are forbidden.
  - Re-baselined on 2026-08-25: the user approved implementing every previously deferred area. The workstream now includes immutable `BaseCommandResponse<T>` and result factories, a repository-wide immutable published-collection standard, evidenced `Money`/`GeoCoordinate`/temporal-range value concepts, generated EF migrations for resulting model changes, and generated NSwag record contracts.
  - Domain concepts remain evidence-driven: local calendar ranges and UTC instant ranges are distinct semantics, and no generic `DateRange` may erase that distinction.
  - Generated NSwag records must come from the pinned generator or a deterministic repository-owned generation extension. Hand-editing `EventApiClient.g.cs` remains forbidden.

## 1. Executive Summary

The workstream establishes C# records as the default for handwritten contracts whose meaning is immutable data plus value equality, then migrates the eligible repository surface in Clean Architecture order.

The outcome is not “records everywhere.” It is:

- concrete MediatR commands and queries expressed as immutable records;
- read/projection DTOs and client-owned HTTP bodies expressed as positional or nominal records according to binding and construction needs;
- trusted route, user, and tenant facts introduced from established server authorities rather than request bodies;
- immutable outbox payload snapshots separated from mutable persisted outbox entities;
- small domain value objects using record class or `readonly record struct` only when value/copy semantics fit;
- immutable Blazor result/filter/dialog snapshots expressed as records while mutable form/component state remains class-based and NSwag representation remains generator-owned;
- permanent architecture tests that reject new record-policy debt and force every retained class to have a current semantic reason.
- immutable Application command results created through explicit success/failure factories rather than public setter mutation;
- a repository-wide standard for published immutable collections, with defensive snapshots and explicit mutable-owner exclusions;
- evidenced `Money`, `GeoCoordinate`, local-date-range, and UTC-instant-range concepts replacing duplicated primitive pairs;
- generated EF migrations and provider snapshots for intentional value-object persistence changes;
- deterministic NSwag-generated record DTOs, with consumers migrated away from setter-dependent construction.

### Intended outcomes

- Make immutable intent and value semantics explicit.
- Remove body-owned current-user/current-tenant authority.
- Reduce object-initializer mutation and constructor boilerplate where doing so improves correctness.
- Use `with` expressions and value equality in adversarial tests to produce clear tampering and variant cases.
- Keep JSON, PATCH presence semantics, validation, OpenAPI, NSwag generation, HAL, and Clean Architecture boundaries deterministic.

### Non-goals

- No conversion of EF entities, outbox lifecycle entities, services, repositories, handlers, validators, controllers, or mutable Blazor edit state merely for consistency.
- No manual edits to `EventApiClient.g.cs`, generated OpenAPI, API inventory, EF migrations, or model snapshots.
- No unification of local calendar ranges with UTC instant ranges when their invariants differ.
- No conversion of internal mutable collections whose owning aggregate/service intentionally controls mutation.
- No dependency addition for NSwag record generation unless the outbound-license gate passes and the user separately approves the expansion.
- No product workflow or UI redesign.
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
| Generated NSwag DTO | Generated partial record after Phase 11 | Representation remains generator-owned; never hand-edit output. |
| `BaseCommandResponse<T>` | Immutable record/result contract after Phase 7 | Creation flows through explicit factories; no public mutable setters or mutable error collection exposure. |

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
11. `BaseCommandResponse<T>` becomes immutable only through test-first migration of factories, derived responses, serializers, and API ProblemDetails mapping.
12. Outbox/EF lifecycle entities remain classes.
13. Generated NSwag DTOs remain generated artifacts; Phase 11 changes their generated representation without hand-editing output.
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

### D6. Repository-wide published-collection immutability

- **Decision:** Apply one repository-wide standard to published immutable contracts: expose read-only collection abstractions, defensively snapshot caller-owned mutable inputs, and keep mutable backing collections private to intentional owners.
- **Why:** Records provide shallow immutability only.
- **Alternatives considered:** Ignore mutable members; replace every internal collection with an immutable package type.
- **Consequences:** Architecture tests must distinguish published immutable contracts from aggregate/service internals; serializers and generated clients require explicit compatibility tests.
- **Affected layers:** Domain, Application, API, Blazor, tests, and governance.

### D7. Preserve lifecycle and framework-owned mutation

- **Decision:** Retain EF/outbox entities, handlers, validators, controllers, and mutable UI state as classes. Migrate Application results and generated DTO representation only through their owning factory/generator boundaries.
- **Why:** Reference identity, lifecycle mutation, inheritance, and framework behavior remain intentional even after the expanded record adoption.
- **Alternatives considered:** Records everywhere; public setters on records; hand-edited generated records.
- **Consequences:** The permanent baseline documents semantic exclusions while Phases 7 and 11 remove the two previously deferred class categories.

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

### D10. Value concepts follow existing semantic pairs

- **Decision:** Introduce `Money` around normalized currency plus minor units, `GeoCoordinate` around bounded latitude/longitude, and separate local-date and UTC-instant range concepts where current invariants differ.
- **Why:** `EventTicketType`/`PaymentAttempt`, `LocationPii`, `Event`/`EventSeries`, and session/agenda models repeat these primitives, but a single generic range would collapse different temporal meanings.
- **Alternatives considered:** Keep primitive pairs; introduce one unconstrained generic `DateRange`; redesign unrelated payment/location workflows.
- **Consequences:** Domain tests define semantics first, then Application/Persistence callers migrate in bounded batches. No arithmetic or range operation is added unless a current caller needs it.
- **Affected layers:** Domain, Application, Persistence, serialization, and tests.

### D11. EF changes are generated and expand/contract safe

- **Decision:** Accept the schema/model changes produced by the approved value-object redesign, but generate every migration and provider snapshot from corrected Domain/EF configuration sources.
- **Why:** CLR value-object ownership may change EF metadata even when column names remain stable; hand-edited migrations are forbidden and multi-provider compatibility is a Tier 1 gate.
- **Alternatives considered:** Suppress all model changes; hand-edit snapshots; destructive one-step column replacement.
- **Consequences:** Phase 10 proves old-row compatibility, up/down/reapply behavior, nullability, constraints, and provider-specific output before accepting generated artifacts.
- **Affected layers:** Domain, Persistence, migration projects, schema docs, and tests.

### D12. Generated record DTOs stay generator-owned

- **Decision:** First prove whether pinned NSwag 14.6.3 exposes a deterministic record mode. If it does not, add a repository-owned deterministic generation extension under the existing client-generation boundary; never patch generated declarations manually.
- **Why:** `nswag.json` currently specifies `classStyle: Poco`, generated consumers rely on parameterless/object-initializer construction, and exact `partial class` assumptions exist in architecture tests.
- **Alternatives considered:** Keep POCOs; edit `EventApiClient.g.cs`; copy third-party templates; add an unreviewed generator dependency.
- **Consequences:** Phase 11 begins with compiled/source characterization, migrates all generated-contract consumers, runs generation twice for idempotency, and preserves JSON, HAL, PATCH, nullable, and client-method behavior.
- **Affected files:** `src/Explore.Blazor.Client/nswag.json`, `Explore.Blazor.Client.csproj`, generated client output, client consumers, architecture/API/Blazor tests, and generation docs.

## 6. Implementation Phases

### Phase 0: Architecture Policy And Candidate Baseline

- **Goal:** Establish a deterministic candidate/disposition inventory and permanent no-new-debt ratchet before production conversion.
- **Depends on:** No records-adoption task. Start is blocked until the owning paid-checkout work restores the pre-existing architecture-suite baseline to green.
- **Relevant files:** `tests/Event.Architecture.Tests/RecordContractArchitectureTests.cs`; `tests/Event.Architecture.Tests/Baselines/record-contract-class-baseline.json`; `tests/Event.Architecture.Tests/Baselines/http-body-authority-dispositions.json`; `docs/GOVERNANCE.md`, `.agents/rules/application-layer.md`, `.agents/rules/domain.md`, `.agents/rules/blazor-client.md`, `.agents/rules/tests.md`.
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

### Phase 1: Domain Value Semantics

- **Goal:** Convert only approved small Domain value types while preserving entity/reference identity and EF behavior.
- **Depends on:** Phase 0.
- **Relevant files:** bounded candidates from `src/Explore.Domain/ValueObjects/**/*.cs`; their existing consumers; `tests/Event.Domain.UnitTests/ValueObjects/RecordValueObjectContractTests.cs`; existing EF configurations only when a candidate is already mapped.
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

### Phase 6: Original-Wave Governance Closure And Release Contribution

- **Goal:** Close the original seven-phase record-adoption wave, synchronize its documentation, prove its architecture policy, and prepare its governed breaking-change contribution.
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

### Phase 7: Immutable Application Command Results

- **Phase status:** Complete — Tasks 7.1–7.3, the owned build gate, and the immutable-result API seam are green. The exact root build is blocked only by the unrelated untracked coordinate-authority syntax error; the full solution build excluding only that file through an external scoped target passes with zero errors. Blazor and persistence consumer projects build green, 2,561 Blazor tests pass with one pre-existing documented skip, and the exhaustive command-result mapper passes 97/97. The full API integration project was executed once: 2,486 passed, 1 skipped, and 30 unrelated shared-work failures were classified across admissions route RED tests, Quartz lifecycle, snapshot isolation, unavailable production secrets, and persistence-schema fixtures.
- **Goal:** Replace mutable `BaseCommandResponse<TKey>` construction with an immutable result contract and explicit success/failure factories while preserving RFC 7807 mapping and serialized response behavior.
- **Depends on:** Phase 6 complete. The exact Phase 4 gate was executed on `develop` and reported zero records-adoption failures, 122 unrelated failures, and three external secret/startup failures. The user explicitly directed direct `develop` continuation and removal of the isolated worktree; focused Phase 7 evidence may advance, but phase closure still requires the exact verification lane to be rerun and classified honestly.
- **Relevant files:** `src/Explore.Application/Responses/BaseCommandResponse.cs`; bounded derived responses and factories under `src/Explore.Application/**`; `src/Explore.Application/Serialization/ExploreJsonContext.cs`; `src/Explore.API/ExceptionHandling/CommandResponseResultMapper.cs` and `QuotaProblemDetailsFactory.cs`; `tests/Event.Application.UnitTests/Responses/BaseCommandResponseContractTests.cs`; `tests/Event.API.IntegrationTests/ExceptionHandling/CommandResponseResultMapperTests.cs`; existing architecture ratchets.
- **Related skills/rules:** `cqrs-mediatr-guidelines`, `clean-architecture-rules`, Application/API/Test rules.
- **Acceptance criteria:**
  - `BaseCommandResponse<TKey>` exposes no public mutable setters and no caller-mutable error collection.
  - Named success, validation, not-found, conflict, authorization, authentication, and quota factories create valid states only.
  - Derived response types and local handler/service factories use the immutable creation path.
  - `CommandResponseResultMapper` preserves successful bodies and all RFC 7807 status/code/detail/extension behavior.
  - System.Text.Json source generation and existing API response shape remain intentional and covered.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Revert the owning response contract and caller factories together if a valid result state cannot be represented without mutation. Do not restore public setters as a local compile fix; retain focused Red evidence and redesign the factory surface.

### Phase 8: Repository-Wide Published Collection Immutability — Complete

- **Phase status:** Complete — Tasks 8.1–8.3 and both phase gates are complete. The compiled ratchet covers 772 collection-bearing public records across Domain, Application, API, and Blazor; all 128 genuine mutable exposures were migrated to defensively copied read-only or immutable snapshots. The integrated collection ratchet passes 3/3, the exceptional baseline is empty, canonical docs plus five rule-twin pairs teach the enforced standard, and the root build passes with 0 errors. The full architecture project executed 462 tests: 456 passed, 1 documented skip, and 5 precisely classified unrelated failures in agent context, coordinate authority, generated quota OpenAPI, and EF/provider inventory.
- **Goal:** Extend defensive snapshot and read-only exposure rules from converted records to every published immutable contract while preserving intentional aggregate/service mutation.
- **Depends on:** Phase 7.
- **Relevant files:** immutable contract surfaces under `src/Explore.Domain`, `src/Explore.Application`, `src/Explore.API`, and `src/Explore.Blazor.Client`; existing serializer contexts; new/extended architecture ratchets; focused owner tests; canonical governance and twin path rules.
- **Related skills/rules:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `blazor-ui-conventions`, Domain/Application/API/Blazor/Test rules.
- **Acceptance criteria:**
  - Every published immutable contract with a collection has an explicit disposition: immutable/read-only snapshot, generated/framework-owned, or intentionally mutable owner.
  - Caller-owned lists, arrays, sets, and dictionaries cannot mutate an immutable contract after construction.
  - Aggregate and service internals may retain private mutable backing collections behind read-only views.
  - Equality tests never infer structural sequence equality from containing records.
  - JSON/AOT, mapping, HAL extension data, and generated-client behavior remain deterministic.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Reclassify a serializer/framework-owned member with evidence when read-only representation is unsupported. Do not expose mutable collections to avoid a mapping or deserialization repair; fix the owning factory/context instead.

### Phase 9: Domain Money Coordinates And Temporal Ranges — Complete

- **Phase status:** Complete — `Money`, `GeoCoordinate`, `LocalDateRange`, and `UtcInstantRange` are dependency-free sealed record values with private construction and narrow static factories. Ticket/payment factories, location privacy transitions, agenda scheduling, and fixed/open-ended/prayer-relative session scheduling now consume semantic values without compatibility overloads. Structural closure covers 41 ticket factories, 26 payment factories, and every selected schedule transition. The full Domain project passes 976/976. The exact root Release build compiled every owned project and failed only in two unrelated Infrastructure test constructors missing a newly required logger argument.
- **Goal:** Introduce evidenced domain value concepts for normalized money, geographic coordinates, local calendar ranges, and UTC instant ranges before persistence remapping.
- **Depends on:** Phase 8.
- **Relevant files:** `src/Explore.Domain/ValueObjects/Money.cs`, `GeoCoordinate.cs`, `LocalDateRange.cs`, and `UtcInstantRange.cs`; `CurrencyMetadata.cs`, `LocationPii.cs`, `Location.cs`, `EventTicketType.cs`, `PaymentAttempt.cs`, `Event.cs`, `EventSeries.cs`, `EventSession.cs`, and `EventAgendaItem.cs`; focused tests under `tests/Event.Domain.UnitTests/ValueObjects/`.
- **Related skills/rules:** `clean-architecture-rules`, `dotnet-efcore-guidelines`, Domain/Test rules.
- **Acceptance criteria:**
  - `Money` uses normalized currency and checked minor-unit semantics required by existing ticketing/payment callers.
  - `GeoCoordinate` enforces latitude/longitude bounds while preserving nullable location and PII-erasure behavior.
  - Local `DateOnly` ranges and UTC `DateTimeOffset` ranges remain separate concepts with explicit ordering/overlap invariants.
  - No EF, serializer, API, or Application dependency enters Domain.
  - Existing scalar persistence stays temporarily compilable until Phase 10 replaces it through generated migration work.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Keep a duplicated primitive pair only when Red tests prove the owners have different semantics. Do not add generic arithmetic/range APIs, implicit conversions, or compatibility aliases that current callers do not require.

### Phase 10: Generated EF Value Persistence Migration

- **Phase status:** In progress — two independent repository-grounded architecture reviews reject EF complex/owned/converter mappings: optional prices share one currency authority, temporal leaves participate in cross-owner indexes, and start-only sessions are not ranges. Domain values remain the semantic write boundary; EF retains explicit scalar relational leaves. Phase 10 adds four portable check constraints and generated five-provider migrations without column, key, index, nullability, or table changes.
- **Goal:** Enforce the Phase 9 value invariants over their owner-controlled scalar persistence leaves and generate data-preserving, reversible, multi-provider EF migrations and snapshots.
- **Depends on:** Phase 9.
- **Relevant files:** entity configurations for event, location/PII, ticketing, payment, series/session/agenda owners; `ExploreDbContext`; generated application/provider migration projects and model snapshots; `schemas/islamu-event.md`; migration and round-trip tests under `tests/Event.Persistence.IntegrationTests/`.
- **Related skills/rules:** `criticality-guardrail`, `dotnet-efcore-guidelines`, `clean-architecture-rules`, `epistemic-mad-review`, `.agents/rules/efcore-migrations.md`, Persistence/Test rules.
- **Acceptance criteria:**
  - EF intentionally maps the approved value concepts through existing scalar owner leaves; `Money`, `GeoCoordinate`, `LocalDateRange`, and `UtcInstantRange` do not become complex, owned, converted, or independently tracked persistence types.
  - Generated checks reject negative ticket amounts, partial/out-of-range coordinates, and reversed local date ranges without weakening tenant filters, privacy erasure, nullability, indexes, precision, or existing database checks.
  - Existing rows upgrade without data loss; rollback/reapply behavior is explicit and tested.
  - Every migration and snapshot is generated by repository `dotnet ef` workflows and inspected, never hand-edited.
  - PostgreSQL plus every shipped provider-specific model/migration path is intentionally generated or proven unaffected.
  - Tier 1 multi-provider and anonymized MAD evidence has no unresolved critical finding.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** If generation emits any column, key, index, table, raw-SQL, or unrelated operation, remove the unapplied generated migration, correct configuration/generation inputs, and regenerate. `Down()` removes only the four checks; malformed legacy rows block installation for explicit operator correction rather than being silently rewritten.

### Phase 11: Generated NSwag Records And Final Closure

- **Goal:** Generate record DTOs deterministically from the canonical OpenAPI contract, migrate client consumers, close all ratchets/docs, and prepare the final release contribution.
- **Depends on:** Phase 10.
- **Relevant files:** `src/Explore.Blazor.Client/nswag.json`, `Explore.Blazor.Client.csproj`, generated `Clients/EventApiClient.g.cs`, `dotnet-tools.json`, client services/helpers/serializer contexts, `.github/workflows/openapi-contract.yml`, generated-shape architecture tests, API/Blazor contract tests, governance docs, and a new final Tier 2 change fragment with an unclaimed ID.
- **Related skills/rules:** `blazor-ui-conventions`, `clean-architecture-rules`, `ip-clean-room`, `openapi-contract-change`, Blazor/API/Test/release rules.
- **Acceptance criteria:**
  - The pinned generator's native record capability is proven or a deterministic repository-owned generation extension is used without copied third-party templates or hand edits.
  - Generated DTOs are records only where JSON/HAL/PATCH/nullable/required/member-construction semantics remain correct; explicitly framework-required generated classes have reasoned exclusions.
  - Parameterless/object-initializer consumers migrate to generated constructors/init semantics.
  - OpenAPI, generated client, AOT JSON, HAL extension data, client methods, and second-run determinism are green.
  - Final architecture ratchets, documentation, I-VSD traceability, and Tier 2 release evidence include Phases 7–11.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Restore `classStyle: Poco` and the prior generated artifact through the generator if record output cannot preserve a required framework contract. Do not keep a partially transformed generated file, copied template, or nondeterministic post-processing step.

## 7. Testing Strategy

### 7.1 Test-first invariant anchors

- **Architecture:** `Event.Architecture.Tests` discovers request/DTO/body surfaces and enforces shrinking baselines.
- **Domain:** `Event.Domain.UnitTests` proves value-object invariants, equality, copy semantics, and invalid values.
- **Application:** `Event.Application.UnitTests` proves MediatR construction, authorization facts, mapping, JSON, PATCH groups, and payload round trips.
- **API:** `Event.API.IntegrationTests` proves body tampering cannot become current authority, model binding/validation remains correct, and OpenAPI reflects runtime contracts.
- **Blazor:** `Explore.Blazor.Client.Tests` proves generated-client serialization and immutable-versus-editable presentation semantics.
- **Application results:** Application/API tests prove immutable result factories, impossible-state prevention, error snapshot isolation, JSON behavior, and RFC 7807 mapping.
- **Published collections:** Owning-layer behavioral tests plus architecture ratchets prove defensive snapshots, read-only exposure, and explicit mutable-owner exclusions.
- **Domain values and persistence:** Domain tests prove money/coordinate/range invariants; Persistence tests prove round trips, old-row upgrade, rollback/reapply, constraints, tenant isolation, privacy erasure, and provider parity.
- **Generated records:** Architecture, API, and Blazor tests prove generator-owned record shape, JSON/HAL/PATCH semantics, consumer construction, and deterministic regeneration.

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
| 6 | `Event.Architecture.Tests` | Original-wave Clean Architecture and ratchet checkpoint. |
| 7 | `Event.API.IntegrationTests` | Immutable command-result bodies and RFC 7807 mapper behavior. |
| 8 | `Event.Architecture.Tests` | Cross-layer published-collection ownership and no-new-debt ratchet. |
| 9 | `Event.Domain.UnitTests` | Money, coordinate, and temporal-range invariants. |
| 10 | `Event.Persistence.IntegrationTests` | Generated migration, old-row upgrade, rollback/reapply, and provider model behavior. |
| 11 | `Event.Architecture.Tests` | Final generated-record ownership, deterministic contract shape, and all expanded ratchets. |

Tier 1 Task 4.4 adds one scoped Stryker mutation run and anonymized MAD review as mandatory criticality evidence. They are not substitutes for or repetitions of the phase-end project test.

## 8. Documentation, Configuration, And Operations Impact

### Documentation

- Update `docs/GOVERNANCE.md` with the authoritative type-selection policy.
- Update `docs/ARCHITECTURE.md` with horizontal ownership and immutable request flow.
- Update `docs/API.md` and mandatory `docs/API_CHANGELOG.md` for body/requiredness/nullability breaks and generation workflow.
- Update `docs/OUTBOX_PATTERN.md` only where payload-record guidance changes implemented reality.
- Update `docs/BLAZOR.md` with generated-class versus local-record ownership.
- Update `docs/DOMAIN.md` with money, coordinate, and distinct temporal-range semantics.
- Update `schemas/islamu-event.md` from the generated EF model/migration reality.
- Update matching `.agents/rules/*.md` so future agents apply the final rule.
- Keep the [I-VSD report](../../../islamic-value-sensitive-design/i-vsd-records-adoption.md) linked from all workstream artifacts.

### Configuration and generated artifacts

- No runtime secret, setting, or environment variable is planned.
- `stryker-config.json` is reused; modify only if a verified scoped configuration is required and remains general-purpose.
- `schemas/openapi_islamu-event.json`, `docs/API_CONTRACT_INVENTORY.md`, and `EventApiClient.g.cs` are generator outputs.
- EF migration and model-snapshot changes are expected in Phase 10 and must be generated from corrected Domain/Persistence sources for every applicable provider.
- `nswag.json` and the existing client-generation target change in Phase 11; generated DTO declarations remain generator-owned.
- No dependency is planned. If native NSwag cannot emit records, the owned generation extension must use repository/runtime capabilities unless a separately approved dependency passes outbound-license review.

### 8.1 Release & Changelog Strategy

This remains **Tier 2 — High-Impact / Breaking / Security / Migration / OpenAPI**:

- Preserve completed original-wave fragment `docs/releases/changes/CHG-2026-0010.yaml`.
- In Task 11.4, recheck and create a second unclaimed `docs/releases/changes/CHG-2026-XXXX.yaml` for immutable results, EF migration, and generated-record consumer impact.
- Proposed terminal commit composition remains `refactor(architecture)!: adopt semantics-first record contracts`, updated to teach the expanded migration.
- Required trailers: non-empty `BREAKING CHANGE:` and the final fragment's `Change-Id`.
- `Changelog: skip` is forbidden because the change is breaking and OpenAPI-visible.
- The fragment must classify:
  - **Breaking:** request body, requiredness, nullability, and generated-client changes;
  - **Security:** trusted user/tenant authority removal from bodies;
  - **Migration:** generated multi-provider database migration plus source/client migration;
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
| Immutable result migration permits contradictory states | Medium | High | Factory-only construction and mapper characterization | Result/ProblemDetails contract failure | 7.1–7.3 |
| Collection standard converts intentional mutable ownership | Medium | High | Explicit dispositions and owner-focused mutation tests | Aggregate/service behavior failure | 8.1–8.3 |
| Money/range abstraction erases distinct semantics | Medium | Critical | Separate currency/local-date/UTC-instant invariants | Mixed-currency or range test failure | 9.1–9.3 |
| Generated value-object migration loses or reinterprets stored data | Medium | Critical | Expand/contract, old-row fixtures, rollback/reapply, multi-provider MAD | Migration invariant failure | 10.1–10.4 |
| NSwag record output breaks HAL/PATCH/consumer construction | High | High | Capability proof, generated-contract Red tests, deterministic regeneration | Architecture/API/Blazor contract failure | 11.1–11.3 |
| Owned generator customization copies third-party expression | Low | Critical | Clean-room public-interface-only design and provenance review | IP governance blocker | 11.1–11.2 |

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
12. `BaseCommandResponse<TKey>` and all result factories are immutable and preserve RFC 7807/API behavior.
13. Every published immutable collection has defensive snapshot/read-only semantics or an explicit current exclusion.
14. `Money`, `GeoCoordinate`, local-date-range, and UTC-instant-range values replace their approved duplicated primitive boundaries.
15. Generated EF migrations preserve existing data, tenant/privacy constraints, rollback/reapply behavior, and applicable provider models.
16. NSwag generates deterministic record DTOs without hand edits, copied templates, or broken JSON/HAL/PATCH/client contracts.
17. The final expanded Tier 2 fragment validates, and `Remaining / Deferred Work` is empty.

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

The original candidate set and OpenAPI delta are now evidenced. The expanded workstream's highest risks are the broad direct-construction surface around `BaseCommandResponse<TKey>`, preserving distinct money/local-date/UTC semantics through EF flattening, and proving generated record DTOs without copying third-party templates or weakening HAL/PATCH serialization. Phases 7–11 bind each risk to Red-first contracts, generator-owned artifacts, and one phase-end gate.
