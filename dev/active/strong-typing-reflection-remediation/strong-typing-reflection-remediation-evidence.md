<!-- ABOUTME: Source-grounded evidence packet for the strong-typing and reflection-debt remediation plan. -->
<!-- ABOUTME: Separates verified repository facts from the submitted audit, external guidance, and design decisions. -->

# Strong Typing And Reflection Debt Remediation — Evidence Packet

Last Updated: 2026-08-30 Europe/Brussels

## Scope And Evidence Cutoff

- **Stable task name:** `strong-typing-reflection-remediation`
- **Evidence cutoff:** 2026-08-30
- **Original request:** Produce an implementation plan for the submitted repository-wide audit of hardcoded strings, weak typing, reflection-based tests, source scraping, security metadata literals, and domain primitive obsession. Preserve no backward compatibility because the repository is pre-release.
- **Planning-only boundary:** No runtime or test implementation is included in this work session.
- **Working tree:** `develop` was ahead of `origin/develop` by 11 commits and contained extensive unrelated tracked and untracked work before planning started. Several report-cited files were already modified or newly created. Planning writes are isolated to this new workstream and its I-VSD report.

## Evidence Method

The repository-required `code-review-graph` tools were not available in this tool session. The fallback evidence set used:

- bounded direct reads of owning source, test, documentation, rules, and skill files;
- AST-aware searches across `src/` and `tests/`;
- read-only specialist scouts for intent, security-catalog, reflection-test, Blazor-test, source-scraping, and primitive-value audits;
- official Microsoft and bUnit documentation;
- one attempted Roslyn workspace-symbol lookup, which timed out during solution initialization and was not treated as absence evidence.

No third-party implementation source, snippets, migrations, tests, or source-derived structure entered the implementation design.

## Intent And Governance Evidence

| Claim | Evidence | Status |
|---|---|---|
| Test reflection/source-scraping cleanup matches an existing intent. | `.agents/contract/intents.yaml::test-suite-rationalization` | Verified |
| That intent forbids product-source edits. | `test-suite-rationalization.paths_forbidden` includes `src/**`. | Verified |
| No existing intent covers a mixed product-source plus test strong-typing refactor. | Read-only intent scout over `.agents/contract/intents.yaml`; `.agents/contract/README.md` fallback/new-intent rules. | Verified |
| The intent references a missing triad. | Reads of `dev/active/test-suite-rationalization/*-{plan,context,tasks}.md` returned `ENOENT`. | Verified |
| A paused Blazor refactor overlaps broad hardcoded-string and test-architecture cleanup. | `dev/pause/blazor-clean-code-refactor/blazor-clean-code-refactor-context.md` | Verified; do not duplicate its broad service/UI decomposition |

## Verified Current-State Inventory

### Typed Or Already Remediated

| Area | Evidence | Current disposition |
|---|---|---|
| Ticketing recovery options/validator tests | `tests/Explore.Secrets.UnitTests/TicketingRecoveryOperatorContractTests.cs` directly constructs `TicketingRecoveryOperatorOptions`, directly instantiates `TicketingRecoveryOperatorOptionsValidator`, and directly calls the service and health check. | Submitted report is stale for this file; no reflection conversion remains. |
| Event add-on component tests | `tests/Explore.Blazor.Client.Tests/EventAddOnComponentTests.cs` uses typed generated DTOs, `IEventAddOnService`, and `Render<EventAddOnSelector>` / `Render<EventAddOnCatalogEditor>`. | Use as a typed bUnit sibling pattern; no conversion task for this file. |
| Ticket-transfer behavior tests | `tests/Explore.Blazor.Client.Tests/TicketTransferComponentTests.cs` already renders `TicketTransferPanel` through generic bUnit for behavior. | Remove only the redundant reflected existence/surface test. |
| Currency semantics | `src/Explore.Domain/ValueObjects/CurrencyMetadata.cs`, `Money.cs`, and `tests/Event.Domain.UnitTests/ValueObjects/MoneyTests.cs`. | Supported-code normalization, minor-unit scale, no-currency sentinel, and value equality already exist. |
| Country/email normalization for tenant operator identity | `src/Explore.Domain/ValueObjects/TenantDirectoryOperatorIdentity.cs`. | Capability-specific validation already exists; global country/email value objects are not justified. |
| Admission lookup and enum pairing | `src/Explore.Domain/AdmissionTicketLookups.cs`, `src/Explore.Domain/Enums/AdmissionTicket*Enum.cs`, `src/Explore.Persistence/Seed/LookupTableSeeder.cs`. | Intentional stable-ID enum plus persisted lookup/display authority; preserve and verify parity. |

### Reflection Runtime Debt

| Area | Evidence | Protected invariant | Correct replacement seam |
|---|---|---|---|
| Admission Application contracts | `tests/Event.Application.UnitTests/Contracts/Admissions/Support/AdmissionContractRuntime.cs` and its orchestration/port-fake consumers. | Public request/result shapes, constructor dependencies, provider neutrality, service outcomes. | Directly construct the now-public records/services and call public methods/interfaces. Delete the runtime and reflection-backed port adapters only after typed replacements pass. |
| Event add-on Domain/persistence | `tests/Event.Persistence.IntegrationTests/EventAddOnPersistenceTests.cs::AddOnReflectionSurface`. | Money overflow, multi-item catalog behavior, inventory/replay/concurrency, tenant separation, fulfillment/refund. | Typed aggregate and repository calls plus real PostgreSQL behavior; retain only metadata checks that defend persistence invariants. |
| Ticketing recovery Domain/persistence | `tests/Event.Persistence.IntegrationTests/TicketingLifecycleRecoveryInvariantTests.cs::RecoveryReflectionSurface`. | Recovery state machine, bearer-generation fences, tenant replay, reissue uniqueness. | Typed state-machine and repository tests; preserve adversarial PostgreSQL behavior. |
| Ticket transfer Domain/persistence | `tests/Event.Persistence.IntegrationTests/TicketTransferConcurrencyTests.cs`. | Holder/credential authority, transfer state, shared fences, capability replay and tenant isolation. | Typed aggregate/repository behavior; keep compiled metadata checks for storage/privacy invariants. |
| Participant readiness | `tests/Event.Persistence.IntegrationTests/ParticipantAdmissionEligibilityPersistenceTests.cs`. | Readiness authority, PII minimization, tenant filters, shared assignment fence. | Replace CLR type/member lookup with `typeof`/`nameof`; keep metadata and behavioral tests where they prove distinct invariants. |
| Location address contract | `tests/Event.Application.UnitTests/Features/Locations/Commands/LocationAddressWriteContractTests.cs`. | Coordinate validity, tenant authority, atomic transitions, private-home semantics. | Direct `GeoCoordinate` and `Location` behavior for positive paths; retain narrow compiled negative-surface assertions only where forbidden API absence is the invariant. |

`tests/Event.Application.UnitTests/Features/Locations/AddressGovernancePolicyTests.cs`, cited by the submitted report, was not found at that path on the evidence cutoff.

### EF Core Metadata

| Pattern | Evidence | Disposition |
|---|---|---|
| CLR entity lookup by full-name string | `ParticipantAdmissionEligibilityPersistenceTests`, `EventAddOnPersistenceTests`, `TicketingLifecycleRecoveryInvariantTests`, `TicketTransferConcurrencyTests`. | Use `model.FindEntityType(typeof(TEntity))` when the CLR type exists. |
| CLR property lookup by literal string | `AdmissionCheckInPersistenceRedTests`, `AdmissionTicketPersistenceRedTests`, `RegistrationWorkflowPersistenceTests`, and other metadata tests. | Use `nameof(Entity.Member)` when the property is CLR-backed. |
| Shadow properties, generated columns, table/column/index/constraint names, annotations, and migration operation names | EF Core metadata and generated-provider tests. | Strings remain appropriate because the database/model artifact is the contract; centralize only when a production owner already exists. |
| Schema and provider behavior | `docs/TESTING.md`, `docs/RECORD_CONTRACTS.md`, and real-provider persistence tests. | Keep metadata checks as defense in depth and pair critical query/state behavior with real-provider tests. Do not replace everything with shallow `nameof` assertions. |

### Source-Scraping Debt

`tests/Event.Persistence.IntegrationTests/FairReturnWaitlistConcurrencyTests.cs` contains several distinct source-shaped checks:

- `LiteralQueueOrderIsPriorityThenEnqueueTimeThenStableId` reads `FairReturnWaitlistRepository.cs` and asserts LINQ method/property tokens;
- `CanonicalFenceOrdersSupplyQueueOfferBindingPaymentRefund` reads both the repository and `RelationalEntityRowFence.cs` to assert a helper name and `FOR UPDATE`;
- `CrashAndRaceTestsUseExactSignalsWithoutTimingLuck` reads its own test source and the repository to assert `TaskCompletionSource`, `AllArrived`, `Release`, absence of sleeps, and `SaveChangesAsync`;
- atomic primitive/provider replay checks combine reflected method/property existence with behavior.

The observable seam already exists:

- `src/Explore.Persistence/Repositories/FairReturnWaitlistRepository.cs::FindNextEntryAsync` orders `Priority DESC`, `EnqueuedAt ASC`, then `Id ASC`;
- `GetAccessAsync` computes the published queue position with the same order and commercial-equivalence predicates;
- `PostgreSqlContainerFixture` supplies migrations, constraints, lookup seeds, and deterministic reset;
- `WaitlistRaceGate` uses exact `TaskCompletionSource` arrival/release signals and contains no fixed sleeps.

The replacements must:

- seed explicit priorities, UTC timestamps, and UUIDs, verify positions and allocation through the real repository, and prove a second tenant cannot influence the order;
- prove row fencing through executed-command capture or the real race/single-winner behavior rather than SQL source tokens;
- delete the self-source determinism test because exact `TaskCompletionSource` gate behavior and the repository assurance policy are the executable evidence;
- convert atomic/replay method/property existence checks to direct typed operations plus compiled metadata only where storage shape is independently material.

AST inventory also found source reads outside this single test. Those candidates require semantic disposition rather than bulk deletion:

- machine-consumed generated contracts/schemas and policy files may be valid parser inputs;
- raw C#/Razor/CSS/prose token assurance is prohibited;
- compiled reflection, endpoint metadata, rendered semantics, model metadata, analyzers, or repository tools are preferred executable seams.

### Blazor Test Debt

| File/surface | Verified state | Plan disposition |
|---|---|---|
| `TenantDirectoryOperatorIdentitySectionTests.cs` | Uses reflected type/model creation and `DynamicComponent` for initial tests; later tests already use typed `TenantDirectoryOperatorIdentitySection`. | Convert all public component/model tests to typed construction and generic bUnit rendering; preserve exact HAL `edit` behavior. |
| `TenantDirectoryOperatorIdentityAdminServiceTests.cs` | Uses reflected service construction, method invocation, and property reads despite a public service/model. | Instantiate and invoke the public typed service/model directly. |
| `ParticipantReadinessComponentTests.cs` | Uses reflected component/service and dynamic rendering. | Convert to typed component/service/generated HAL DTOs. |
| `FairReturnWaitlistComponentTests.cs` | Uses reflection only to prove component/service/parameter existence; no rendered behavior. | Replace with typed `FairReturnWaitlistPanel` rendering and HAL-action behavior. |
| `TicketTransferComponentTests.cs` | One redundant reflected existence test; remaining tests are typed. | Delete/replace only the redundant structural test. |
| Other `DynamicComponent` uses | AST search returned 54 matches in 19 files. | Do not treat `DynamicComponent` itself as forbidden. Classify each use: public directly referenceable component tests migrate to generic rendering; true runtime-selected/page-layout composition may remain with typed parameter helpers or compiled architecture coverage. |

Official bUnit guidance states that the generic parameter builder is strongly typed and refactor-safe.

### Identity, Claims, Roles, Headers, And Routes

| Concern | Verified authority | Debt and required posture |
|---|---|---|
| Platform user identity | `src/Explore.Application/Authentication/PlatformIdentityPrincipalExtensions.cs` owns `sub -> nameidentifier -> sid -> internal_user_id`, GUID-only; `CurrentUserResolutionExtensions` owns provider-subject lookup. | Do not add a second generic `AppClaimTypes` identity chain. Migrate callers that are deriving platform identity to the canonical extension. Keep protocol/session claim reads purpose-specific. |
| Raw claim readers | AST search found 16 `FindFirst("sub")` matches in 14 source files, 17 `FindFirst("sid")` matches in 14 files, and 3 `FindFirst("internal_user_id")` matches in 3 files at the cutoff. | Classify each as platform identity, provider subject, session correlation, diagnostic evidence, or protocol validation before replacement. Fallback order must not change accidentally. |
| Admin claims | `src/Explore.Application/Authorization/AdminClaimTypes.cs`. | Existing database-enriched admin claim catalog remains authoritative for claim types; it is not a UI affordance source. |
| Machine-auth claims | `src/Explore.Application/Constants/ApiAuthenticationClaimTypes.cs`. | Keep separate from human/platform identity. |
| BFF privileged headers | `src/Event.Web.BffHosting/Security/EventBffHeaderNames.cs` plus `BffProxyHeaderSanitizer`. | Keep boundary-owned. Use framework `HeaderNames` for standard HTTP headers and existing local catalogs for product-specific headers. |
| Role literals | Four report-cited `[Authorize(Roles = "Admin")]` surfaces were verified. | Do not create a global role catalog as a substitute for resource authorization. Decide per endpoint whether coarse role gating is still the intended boundary; where retained, use one existing server-owned constant or named policy and preserve MediatR/Cerbos/HAL authorization. |
| Route names | `src/Explore.API/Hateoas/RouteNames.cs`; `RouteNameCoverageTests` proves every constant resolves exactly once and every named endpoint has a constant. | Normalize self-valued constants mechanically to `nameof(Member)` without changing values, routes, operation IDs, HAL links, or generated clients. Reflection in the coverage test is legitimate compiled catalog assurance. |
| Health headers | `src/Explore.ServiceDefaults/HealthChecks/HealthCheckResponseWriter.cs`. | Replace standard names with `Microsoft.Net.Http.Headers.HeaderNames`; retain product-specific `X-Health-Status` under a local owner. |
| Configuration keys | `EventBffKeycloakAuthenticationOptions.SectionName` plus interpolated child paths. | Hierarchical configuration paths are protocol keys, not Domain primitives. Use configuration binding/options and local constants where repeated; do not create a global string catalog. |

### Domain Primitive Disposition

| Candidate | Decision basis | Planning disposition |
|---|---|---|
| Currency code | Existing `CurrencyMetadata` + `Money`; broad wire/schema/persistence use; scalar-owner persistence policy. | Keep transport and persistence strings. Enter Domain behavior through existing `Money`/`CurrencyMetadata`; no global `CurrencyCode` migration. |
| Country code | Existing capability-specific normalization; transport/persistence scalar. | Keep string and centralized boundary validation. |
| Email | Multiple meanings across PII, verified identity, recipients, config, and integrations. | Keep edge strings. Introduce no universal email type; only a future capability-owned value may be justified by a named invariant. |
| AT Protocol DID | Immutable global federation credential identity with coherent parsing/normalization needs. | Introduce a narrowly scoped Domain value type at federation/authentication boundaries; retain external/generated JSON and scalar EF storage with explicit conversions. |
| Tenant slug | Routing identifier but broad transport use and no verified type-confusion defect. | Keep transport string; centralize normalization/validation first. Defer a branded type until a concrete invariant requires it. |
| Admission lookup/entity-enum mirrors | Stable enum IDs plus lookup FK/display metadata are deliberate. | Preserve both and strengthen compiled seed/parity tests if a gap exists. |

## Additional Verified Planning Inputs

| Planning claim | Repository evidence | Disposition |
|---|---|---|
| Product-catalog pending-model parity already has an executable five-provider seam. | `tests/Event.Persistence.IntegrationTests/ConfigurationManifest/ConfigurationManifestAuditProviderMigrationTests.cs` runs PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL through `PrimaryDatabaseProviderComposition` and `HasPendingModelChanges()`. | Add the AT Protocol scalar-model assertion to this project/phase; do not invent a new model-test project. |
| DataProtection and privacy-erasure-authority catalogs are separate persistence owners. | `src/Explore.Persistence.DataProtection.Migrations.*` and `src/Explore.Persistence.PrivacyErasureAuthority.Migrations.Sqlite`; AT Protocol identity state belongs to the primary product catalog. | Exclude these catalogs from the DID pending-model assertion with an explicit ownership reason. |
| OpenAPI export is API-build owned. | `docs/API.md::OpenAPI Export And Client Generation`; `src/Explore.API/Explore.API.csproj` enables Release build-time OpenAPI generation into `schemas/`. | The canonical Release solution build refreshes the schema before client generation. |
| Client generation consumes the checked-in schema and is incremental. | `src/Explore.Blazor.Client/Explore.Blazor.Client.csproj::GenerateApiClientSource/GenerateApiClient`. | Compare canonical build output to a pre-phase hash; do not claim two consecutive incremental calls prove determinism. |
| Generated transformer tests are real and tracked. | `tests/Explore.GeneratedContracts.Tests/Explore.GeneratedContracts.Tests.csproj` references `eng/tools/Explore.GeneratedContracts`. | Use this existing project as the generated-contract phase gate. |
| API contract inventory is an explicit generator, not a product test. | `eng/tools/Explore.ApiContractInventory/Program.cs` consumes the OpenAPI schema and writes `docs/API_CONTRACT_INVENTORY.md`. | Run it after the canonical build and include the inventory in the hash comparison. |
| The test guide already defines executable architecture contracts and documents temporary reflection workarounds. | `docs/TESTING.md::Executable Architecture Contracts` and `Exceptions (Documented Workarounds)`. | Extend the existing taxonomy; migrate `AnalyticsInitializerTests`, `InvokeLoadEventsAsync`, and `SimulateTagToggle`, then remove obsolete exceptions. |
| Paused Blazor tasks conflict at exact, known points. | `dev/pause/blazor-clean-code-refactor/blazor-clean-code-refactor-tasks.md::Phase 16` and `Phase 6A`. | Supersede 16.1/16.6; retain 16.7 and Phase 6A as paused. |
| Mutation gating is disabled during greenfield development. | `AGENTS.md` Critical Rule 12 and Cold-Start Flow. | Add no new mutation wrapper project; existing mutation consumers remain compile-time blast-radius evidence only. |

## External Source Register

Accessed 2026-08-30. Only neutral framework/API facts were retained.

| Source | URL | Repository-relevant fact |
|---|---|---|
| Microsoft C# `nameof` reference | https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/nameof | `nameof` is a compile-time string constant intended for maintainable symbol names. |
| Microsoft EF Core testing strategy | https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy | Real-provider tests are needed for exact query translation/behavior; fake providers can diverge. |
| ASP.NET Core `HeaderNames` API | https://learn.microsoft.com/en-us/dotnet/api/microsoft.net.http.headers.headernames?view=aspnetcore-10.0 | Framework constants exist for standard HTTP headers including `Connection`, `Access-Control-Allow-Origin`, `Cache-Control`, and `Pragma`. |
| bUnit parameter guidance | https://bunit.dev/docs/providing-input/passing-parameters-to-components.html | Generic component parameter selectors are strongly typed and refactor-safe. |
| Microsoft .NET value-object guidance | https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/implement-value-objects | Value objects have no identity and are immutable; introduce them for semantic value behavior, not indiscriminately for every string. |
| W3C DID Core | https://www.w3.org/TR/did-core/ | A DID has a method and method-specific identifier; identifier syntax and method support are separate concerns. |
| Official AT Protocol DID profile | https://atproto.com/specs/did | AT Protocol uses DIDs as persistent account identifiers, currently supports `did:plc` and `did:web`, and applies a 2048-character syntax limit while allowing future method evolution. |

## Clean-Room And Dependency Decision

- External research used official documentation only.
- The handoff contains framework behavior and design constraints, not third-party source expression.
- No package, runtime dependency, generator, image, asset, font, or dataset is proposed.
- Planned code structure, naming, sequencing, tests, and ownership are repository-native.
- Dependency-license validation is not required unless implementation later adds or changes a dependency.

## Evidence Limits

- No runtime baseline, build, or test suite was executed because this session changes planning Markdown only.
- The current dirty working tree means some report citations may continue changing outside this planning workstream.
- Exact repository-wide counts of every legitimate and illegitimate reflection/source-read use remain a bounded implementation inventory task; this does not alter the target taxonomy or phase order.
- The unavailable knowledge graph must be rerun at implementation start for the final caller/callee/affected-flow record before product edits.
