<!-- ABOUTME: Hot execution ledger for the strong-typing and reflection-debt remediation workstream. -->
<!-- ABOUTME: Sequences invariant-first security, Domain, test, persistence, UI, and recurrence work. -->

# Strong Typing And Reflection Debt Remediation — Task Checklist

Last Updated: 2026-08-30 Europe/Brussels

## Status Summary

- **Overall status:** Approved; implementation not started
- **Completed:** 0/39 implementation tasks; phase verification tracked separately
- **Current priority:** Task 0.1 — add the mixed-source intent
- **Next recommended slice:** Phase 0 — Governance And Assurance Classification
- **I-VSD report:** `islamic-value-sensitive-design/i-vsd-strong-typing-reflection-remediation.md`
- **I-VSD reviewed input revision:** `sha256:1a2fa2e4cfaca23086cb49648c0111b5be9c68e85ab5abdddee08e20b1f9b157`
- **I-VSD status / disposition:** current / plan-aligned
- **CTO review:** Not reviewed
- **User approval:** Approved by the user on 2026-08-30 for this exact workstream revision
- **Primary intent:** new `strong-typing-refactor` intent created by Task 0.1
- **Change classification:** Behavioral Delta with behavior-preserving structural sub-slices

## Implementation Maintenance Rules

- Read this file and the workstream context first. Retrieve only the current plan phase and invalidated decisions.
- Do not reread unchanged plan/context/tasks after every task.
- Mark a substantial task `IN PROGRESS` when it spans multiple edits or a handoff.
- Check a substantial completed task immediately; reconcile small related tasks no later than phase end.
- Keep implementation completion separate from phase verification.
- Update context after a completed phase, decision, blocker, validation failure, material discovery, scope change, or handoff.
- Update the plan only when scope, architecture, sequencing, acceptance, risk, or validation strategy changes.
- Write red invariant tests before production changes for DID, identity, authorization, tenant, money, privacy, state-machine, concurrency, and HAL authority behavior.
- Use focused TUnit selectors only for the named red/green anchor. Run the phase Release build and full selected project once after phase implementation.
- No solution-level `dotnet test`, app/browser startup, Docker/Aspire startup beyond an existing test fixture, Playwright, source/prose assurance, fixed sleeps, or mock-mirroring.
- Never hand-edit OpenAPI, generated clients, migrations, designers, or model snapshots.
- Never revert, overwrite, stage, or claim unrelated dirty-tree changes.
- No backward-compatibility alias, overload, adapter, fallback, or deprecated route survives a completed phase.

## Phase 0 — Governance And Assurance Classification [NOT STARTED]

- [ ] **0.1 Add the mixed-source intent and verify contract routing selects `strong-typing-refactor`.**
  - **Files:** `.agents/contract/intents.yaml` (existing, already dirty outside this workstream), `.agents/contract/schema.json` only if existing fields cannot represent the route, `.agents/contract/README.md`, `.agents/benchmarks/cold-start-tasks.yaml`, `docs/GOVERNANCE.md`.
  - **Acceptance:** execute this bootstrap slice under the existing `create-agent-context-skill` intent; add a primary cross-cutting intent with exact paths, Tier 1 criticality, required skills/rules, minimum projects, docs, no-compatibility gate, generated-artifact prohibitions, and invariant-disposition acceptance; remove stale missing-triad references without creating another triad; both `dotnet run eng/agent-context/validate-contract.cs -- . --intent strong-typing-refactor` and unscoped `dotnet run eng/agent-context/validate-contract.cs -- .` exit zero; the benchmark scenario selects the new intent.
  - **Effort:** M
  - **Dependencies:** user approval
  - **Guidance:** Plan Decisions 5.1 and 5.9; `test-suite-rationalization` is related evidence, not a parent intent.

- [ ] **0.2 Refresh graph and AST evidence and verify every candidate has a semantic category and owning phase.**
  - **Files:** `.omo/evidence/<date>-strong-typing-reflection-remediation/blast-radius.yaml` (new implementation evidence), workstream context (status only), `tests/Shared/**`, relevant existing mutation projects and `tests/Event.Benchmarks/**` as transitive-consumer evidence, no persistent source-file debt allowlist.
  - **Acceptance:** capture callers, callees, flows, tests, benchmark/fixture consumers, mutation-project consumers, and current AST counts; classify each report item, the nonexistent `AddressGovernancePolicyTests.cs` citation, every transitive helper, and every current `docs/TESTING.md` reflection exception as typed behavior, compiled metadata, machine artifact, physical/protocol metadata, runtime dispatch, source/prose assurance, or cited-but-absent; verify every runtime-dispatch/source-assurance candidate maps to Tasks 2.x–9.x and every retained candidate has an invariant reason; add no new mutation wrapper project while mutation gating remains disabled.
  - **Effort:** M
  - **Dependencies:** 0.1
  - **Guidance:** Plan Sections 2.0, 5.2, 5.8; evidence must contain no source excerpts, secrets, PII, claim values, or DIDs.

- [ ] **0.3 Add routing and taxonomy architecture contracts and verify synthetic violations fail for the intended reason.**
  - **Files:** `tests/Event.Architecture.Tests/*StrongTyping*` (new or existing focused class), `docs/TESTING.md`, `docs/QUICK_REFERENCE.md` only if a canonical invariant changes.
  - **Acceptance:** red fixtures fail when the new intent is incomplete/ambiguous or a test seam is misclassified; green contracts prove valid intent references, benchmark parity, no stale active-doc dependency, and the existing executable-architecture taxonomy extended with runtime-name behavior-dispatch and automated-enforcement rules; focused selector `/*/*/*StrongTypingIntentArchitectureTests/*` passes after the fix.
  - **Effort:** M
  - **Dependencies:** 0.1, 0.2
  - **Guidance:** Scenarios 3.7 and 3.8; do not enforce style by scraping product prose.

### Phase 0 Verification — Run Once After Tasks 0.1–0.3

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 1 — AT Protocol DID Semantic Boundary [NOT STARTED]

- [ ] **1.1 Red Phase: specify live DID ingress and erasure behavior against current string APIs and verify malformed input or direct tombstone mutation fails the desired contract.**
  - **Files:** `tests/Event.Domain.UnitTests/Entities/AtprotoIdentityDidBoundaryTests.cs` (new), `tests/Event.Domain.UnitTests/Entities/AtprotoIdentityLifecycleTests.cs`, `tests/Event.Domain.UnitTests/PrivacyErasureContractTests.cs`.
  - **Acceptance:** compiling tests bind Scenarios 3.5A–3.5C to exact case-sensitive value preservation, generic AT Protocol syntax, 2048-character bound, no query/fragment/whitespace/control input, redacted diagnostics, and aggregate-owned tombstone behavior; focused selector `/*/*/*AtprotoIdentityDidBoundaryTests/*` fails because the current string ingress accepts a prohibited value or permits direct erasure mutation.
  - **Effort:** M
  - **Dependencies:** Phase 0
  - **Guidance:** `IVSD-F003/M003`, `IVSD-F004/M004`; official AT Protocol DID profile, not a copied implementation.

- [ ] **1.2 Green Phase: implement the Domain DID value and aggregate-owned live/refresh/erasure transitions and verify the red anchors pass.**
  - **Files:** `src/Explore.Domain/ValueObjects/AtprotoDid.cs` (new), `src/Explore.Domain/AtprotoIdentity.cs`, minimal Domain callers/build fixes.
  - **Acceptance:** live DID parsing is explicit and exact; entity creation/refresh accepts `AtprotoDid`; scalar `Did` storage remains a private/controlled owner field; privacy erasure is an aggregate operation that emits an internal tombstone without parsing it as live; no dual string overload or implicit conversion exists; focused DID/lifecycle selectors pass.
  - **Effort:** L
  - **Dependencies:** 1.1
  - **Guidance:** Plan Decisions 5.6 and 5.7; Domain remains framework-free.

- [ ] **1.3 Migrate Domain identity constructors and documentation and verify all Domain live-DID callers use the semantic value without scalar storage changes.**
  - **Files:** graph/LSP-discovered `AtprotoIdentity` callers across `src/**`, all test projects, `tests/Event.Benchmarks/**`, fixtures/seeds, repositories, privacy erasure, serialization boundaries, `docs/RECORD_CONTRACTS.md`, `docs/DOMAIN.md`.
  - **Acceptance:** migrate every object initializer/direct DID mutation to the new factory/aggregate transition or exact scalar egress in one atomic cutover; Domain creation/refresh passes `AtprotoDid`; unsupported methods remain syntactically representable but support is evaluated outside the value; scalar wire/database/index shape is unchanged; no public setter, string overload, implicit conversion, or compatibility adapter remains; the solution-wide Release build and focused Domain DID/lifecycle tests pass.
  - **Effort:** XL
  - **Dependencies:** 1.2
  - **Guidance:** Tasks 2.4, 3.4, 5.5, and 6.5 add owning-layer adversarial behavior and model/wire evidence after this compilation-complete mechanical cutover.

### Phase 1 Verification — Run Once After Tasks 1.1–1.3

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 2 — Platform Identity Authority [NOT STARTED]

- [ ] **2.1 Red Phase: add hostile-principal matrices and verify conflicting, malformed, provider-linked, and excluded schemes expose current divergence.**
  - **Files:** existing/new focused tests under `tests/Explore.Infrastructure.Tests/Identity/`, API identity tests where HTTP behavior is required.
  - **Acceptance:** table-driven tests cover unauthenticated principals, every fallback position, conflicting GUIDs, malformed GUIDs, non-GUID provider subjects, `internal_user_id`, and excluded API-key/setup/scanner/managed/ATProto/receipt schemes; focused selector `/*/*/*PlatformIdentityPrincipalExtensionsTests/*` fails on at least one duplicated caller before migration.
  - **Effort:** L
  - **Dependencies:** Phase 1
  - **Guidance:** Scenarios 3.2A–3.2C; `IVSD-F002/M002`.

- [ ] **2.2 Consolidate Application claim spelling and resolution and verify one fallback implementation remains.**
  - **Files:** `src/Explore.Application/Authentication/PlatformIdentityClaimTypes.cs` (new), `PlatformIdentityPrincipalExtensions.cs`, `CurrentUserResolutionExtensions.cs`, focused Application/Infrastructure identity tests.
  - **Acceptance:** standard JWT names use framework constants, `internal_user_id` has one Application owner, the GUID fallback remains exact, provider-linked lookup remains separate, and no second platform-ID extraction helper exists in Application; focused canonical identity tests pass.
  - **Effort:** M
  - **Dependencies:** 2.1
  - **Guidance:** Do not move Admin or machine-auth claims into this catalog.

- [ ] **2.3 Migrate Infrastructure ambient-user consumers and verify identical resolved IDs and admin outcomes.**
  - **Files:** `src/Explore.Infrastructure/Identity/UserContext.cs`, `AdminContext.cs`, `AdminClaimsTransformation.cs`, `src/Explore.Infrastructure/Services/CurrentUserService.cs`, `src/Explore.Secrets/Providers/AuditingSecretProviderDecorator.cs` only where project references permit canonical semantics, owning tests.
  - **Acceptance:** ambient platform-user consumers delegate to the canonical authority; provider/session-specific readers remain named exceptions; `Explore.Secrets` uses its existing Application reference rather than a local copy; admin/cache outcomes preserve behavior; no raw claim value or claim inventory is logged; focused Infrastructure identity/admin tests pass.
  - **Effort:** L
  - **Dependencies:** 2.2
  - **Guidance:** If a project cannot reference Application, keep a purpose-specific protocol helper rather than copying platform identity logic.

- [ ] **2.4 Harden Infrastructure AT Protocol adapters and verify the Phase 1 cutover parses once, emits exact scalars, and never leaks through diagnostics.**
  - **Files:** graph-discovered AT Protocol Infrastructure adapters, identity/session transports, and owning `Explore.Infrastructure.Tests`.
  - **Acceptance:** untrusted live DID strings parse before trusted adapter behavior, exact `.Value` scalars cross external boundaries, method support remains adapter-owned, malformed values fail before provider calls, and logs/errors contain no raw DID.
  - **Effort:** L
  - **Dependencies:** Phase 1, 2.1
  - **Guidance:** Scenario 3.5; do not add an implicit conversion or normalize method-specific identity.

### Phase 2 Verification — Run Once After Tasks 2.1–2.4

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category!=Runtime]" --minimum-expected-tests 1`

## Phase 3 — API Authorization And Protocol Literals [NOT STARTED]

- [ ] **3.1 Characterize Admin, route, and header parity; add the failing DID ingress anchor and verify only the intended malformed-input case is red.**
  - **Files:** focused classes in `tests/Event.API.IntegrationTests/Features/`, existing `RouteNameCoverageTests.cs`, health response tests.
  - **Acceptance:** passing characterization tests capture current authorized/unauthorized cohorts, every route value including intentional field/value divergences, operation IDs, and standard/custom health headers; a separate compiling DID HTTP test rejects malformed/oversized input before dispatch and fails before Task 3.4.
  - **Effort:** M
  - **Dependencies:** Phase 2
  - **Guidance:** Scenarios 3.1, 3.3A, 3.8.

- [ ] **3.2 Add the API-local Admin policy and verify no authorization cohort widens or narrows.**
  - **Files:** new API-local policy/name owner under `src/Explore.API/Authentication/`, API service registration, `CustomPropertyDefinitionController.cs`, `EventCustomPropertyController.cs`, `EventSessionCustomPropertyController.cs`, `RegistrationAnswerFilesController.cs`, authorization/HAL tests.
  - **Acceptance:** repeated role attributes are replaced by one named policy preserving exact `Admin` role behavior, EndpointClassification remains Admin, MediatR/Cerbos/HAL checks remain defense in depth, and HTTP tests prove the same success/401/403 matrix.
  - **Effort:** M
  - **Dependencies:** 3.1
  - **Guidance:** No global `AppRoles` and no client role inference.

- [ ] **3.3 Normalize route, relation, and standard header names and verify OpenAPI, HAL, generated client, and response bytes do not drift.**
  - **Files:** `src/Explore.API/Hateoas/RouteNames.cs`, `src/Explore.API/Hateoas/Policies/OrganizationMemberLinkPolicy.cs`, `src/Explore.ServiceDefaults/HealthChecks/HealthCheckResponseWriter.cs`, route/HAL/health/OpenAPI tests.
  - **Acceptance:** an AST-aware rule handles both single-line and wrapped multi-line constants whose literal exactly equals the member name; intentional divergent field/value pairs remain explicit; the raw organization-member `"delete"` relation uses `LinkRelations.Delete`; standard headers use framework `HeaderNames`; `X-Health-Status` remains locally owned; route coverage/health/HAL tests pass; exact route/operation generator inputs are recorded for Phase 8.
  - **Effort:** XL
  - **Dependencies:** 3.1
  - **Guidance:** Use AST-aware preview before the mechanical route rewrite; never hand-edit generated files.

- [ ] **3.4 Migrate API DID ingress and verify the public route remains scalar while malformed values fail before MediatR/repository work.**
  - **Files:** `src/Explore.API/Controllers/ActorController.cs`, DID request mapping/ProblemDetails contracts, focused API tests.
  - **Acceptance:** the route and OpenAPI parameter remain string-shaped, valid values parse exactly once into the semantic value, invalid values return bounded RFC 7807 output without existence leakage, raw DIDs are absent from logs/errors, and Phase 8 owns byte-level generated client/OpenAPI determinism.
  - **Effort:** M
  - **Dependencies:** Phase 1, 3.1
  - **Guidance:** Scenarios 3.5 and 3.8; no route alias or compatibility parser.

- [ ] **3.5 Migrate API ambient-user consumers and verify identity, idempotency, support, and logging remain fail-closed.**
  - **Files:** `SupportAccessLinkPolicy.cs`, `IdempotencyRequestIdentity.cs`, `RequestLoggingMiddleware.cs`, `InstanceOnboardingController.cs`, `AdminCacheDiagnosticsController.cs`, graph-discovered API ambient identity callers and tests.
  - **Acceptance:** platform ID uses the canonical helper, provider-linked callers use the mediator resolver, diagnostics use owned/framework claim constants, production logs contain only bounded presence/reason metadata, idempotency partitions do not collapse distinct schemes, and focused API identity/support/idempotency tests pass.
  - **Effort:** L
  - **Dependencies:** Phase 2, 3.1
  - **Guidance:** Development-only diagnostics may expose intentionally requested values but must not become the identity authority.

### Phase 3 Verification — Run Once After Tasks 3.1–3.5

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 4 — BFF Claim And Configuration Boundaries [NOT STARTED]

- [ ] **4.1 Characterize opaque BFF subject/session partitions and configuration precedence and verify the migration has an exact behavior baseline.**
  - **Files:** focused BFF tests under `tests/Explore.Blazor.IntegrationTests/`.
  - **Acceptance:** tests bind provider subject vs session semantics, rate/setup/circuit behavior, and Keycloak config precedence; conflicting/missing purpose-specific claims fail closed; the focused baseline passes before structural helper migration and can detect fallback/partition drift.
  - **Effort:** L
  - **Dependencies:** Phases 2 and 3
  - **Guidance:** Scenario 3.2C; `IVSD-F002/M002`.

- [ ] **4.2 Add purpose-specific BFF principal/config helpers and verify token, rate, setup, and session behavior is unchanged.**
  - **Files:** `src/Event.Web.BffHosting/Security/EventBffPrincipalExtensions.cs` (new), `src/Event.Web.BffHosting/Security/EventBffHeaderNames.cs`, `src/Event.Web.BffHosting/Authentication/EventBffKeycloakAuthenticationOptions.cs`, `src/Explore.Blazor/Services/TokenCircuitHandler.cs`, `src/Explore.Blazor/Extensions/RateLimitingExtensions.cs`, `src/Explore.Blazor/Extensions/BffSetupSecretEndpoints.cs`, bounded BFF session/admin-claim files, owning BFF tests.
  - **Acceptance:** helpers expose explicit opaque subject/session/rate-partition purposes, use framework claim/header names, retain fallback/partition/config precedence, never claim platform GUID authority, and focused BFF tests pass without raw claim logging.
  - **Effort:** L
  - **Dependencies:** 4.1
  - **Guidance:** BFF cannot depend on Application identity code; semantic separation is intentional. Do not absorb paused `DynamicAuthSchemeManager` Task 16.7 or Phase 6A provider/concurrency work.

### Phase 4 Verification — Run Once After Tasks 4.1–4.2

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 5 — Typed Application Contract Tests [NOT STARTED]

- [ ] **5.1 Add direct typed admission anchors and verify they pass alongside the old runtime before deletion.**
  - **Files:** admission orchestration/contract test classes under `tests/Event.Application.UnitTests/Contracts/Admissions/`.
  - **Acceptance:** one direct typed anchor per issuance/check-in/revocation/recovery/provider-neutral cohort constructs public records/services/interfaces and asserts the same outcomes; focused class selectors pass while the old runtime still exists.
  - **Effort:** L
  - **Dependencies:** Phase 0
  - **Guidance:** `IVSD-F001/M001`, `IVSD-F004/M004`; behavior first, no constructor-call mirrors.

- [ ] **5.2 Convert every admission runtime consumer and reflection-backed port fake and verify invariant-equivalent typed coverage.**
  - **Files:** all `AdmissionContractRuntime` consumers and support fake files under `tests/Event.Application.UnitTests/Contracts/Admissions/**`.
  - **Acceptance:** no consumer uses string-selected types/methods/properties, `Activator`, `MethodInfo.Invoke`, async result reflection, or dynamic conversion; provider neutrality, dependencies, request/result data, and outcomes remain covered by public typed contracts.
  - **Effort:** XL
  - **Dependencies:** 5.1
  - **Guidance:** Migrate innermost contracts first and callers outward; no temporary compatibility helper survives the phase.

- [ ] **5.3 Delete the obsolete admission runtime/support dispatch engine and verify zero references remain.**
  - **Files:** `AdmissionContractRuntime.cs`, obsolete reflection support adapters, affected test project files/usings.
  - **Acceptance:** delete the runtime only after Task 5.2 passes; LSP/AST search finds zero consumers and no replacement runtime helper; focused admission selectors and compilation remain green.
  - **Effort:** M
  - **Dependencies:** 5.2
  - **Guidance:** No historical allowlist or shim.

- [ ] **5.4 Convert Location positive paths to direct values and verify narrow negative-surface contracts still fail on forbidden API exposure.**
  - **Files:** `LocationAddressWriteContractTests.cs`, related location tests, production visibility only if a true public seam is missing.
  - **Acceptance:** positive coordinate/address transitions directly use `GeoCoordinate`/`Location`; runtime name/factory invocation is removed; narrow compiled checks retain absence of raw coordinate setters/tenant body authority; trusted tenant, bounds, private-home, consent, and erasure behavior pass.
  - **Effort:** L
  - **Dependencies:** 5.1
  - **Guidance:** Reflection may remain only when forbidden public surface absence is the invariant.

- [ ] **5.5 Harden Application federation and privacy-erasure DID boundaries and verify the Phase 1 cutover preserves typed live identity plus authority-first tombstoning.**
  - **Files:** `src/Explore.Application/Services/PrivacyErasureApplier.cs`, graph-discovered AT Protocol Application requests/handlers/ports, owning Application tests, `docs/SECURITY-MODEL.md`.
  - **Acceptance:** Application inputs parse or carry `AtprotoDid` before Domain behavior, privacy erasure invokes the aggregate erasure transition instead of mutating identity fields, provider/wire egress emits exact scalar values, raw DIDs never enter logs/ProblemDetails, and focused Application privacy/federation tests pass.
  - **Effort:** L
  - **Dependencies:** Phase 1, 5.1
  - **Guidance:** Preserve authority-first commit, anti-resurrection, and purpose-bound authentication.

### Phase 5 Verification — Run Once After Tasks 5.1–5.5

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 6 — Persistence Behavior And Metadata [NOT STARTED]

- **Phase dependencies:** Phases 0, 1, and 5 must have passing phase gates before any Phase 6 task starts.

- [ ] **6.1 Red Phase: replace fair-return source tokens with deterministic PostgreSQL ordering/tenant scenarios and verify the new behavioral test detects the missing seam before cleanup.**
  - **Files:** `FairReturnWaitlistConcurrencyTests.cs`, existing fixture/seed helpers, no production query change unless behavior is wrong.
  - **Acceptance:** seed explicit priority, UTC enqueue time, stable UUID tie-breaks, and another tenant; assert queue positions and allocation order through the real repository; replace repository/row-fence `FOR UPDATE` source checks with executed-command or real race/fence evidence; delete the self-source `TaskCompletionSource`/`Task.Delay`/`Thread.Sleep`/`SaveChangesAsync` token test because the race tests and assurance policy are the executable proof; focused selectors fail when any order key, tenant predicate, lock/fence, or single-winner behavior is deliberately broken.
  - **Effort:** L
  - **Dependencies:** Phase 0
  - **Guidance:** Scenarios 3.4A–3.4C; no source read, fixed sleep, insertion-order assumption, or EF fake.

- [ ] **6.2 Convert add-on, recovery, and transfer reflection surfaces and verify money/state-machine/concurrency invariants remain stronger than the removed shape checks.**
  - **Files:** `EventAddOnPersistenceTests.cs`, `TicketingLifecycleRecoveryInvariantTests.cs`, `TicketTransferConcurrencyTests.cs`, associated test helpers and owning public types only if needed.
  - **Acceptance:** tests directly construct aggregates and call repositories; overflow, inventory, fulfillment, refund, recovery state, bearer rotation, transfer generation, replay, fences, and one-winner behavior remain; no reflected behavior dispatch remains.
  - **Effort:** XL
  - **Dependencies:** 6.1, Phase 1
  - **Guidance:** Keep real PostgreSQL and exact event gates; never weaken secret/PII absence.

- [ ] **6.3 Convert CLR-backed EF metadata to `typeof`/`nameof` and verify physical/shadow metadata remains explicit.**
  - **Files:** participant readiness, admission check-in, admission ticket, registration workflow, add-on/recovery/transfer metadata tests.
  - **Acceptance:** CLR entities use `FindEntityType(typeof(...))`; CLR members use `nameof` or typed selectors where clearer; shadow/generated/physical names remain literal; tenant filters, keys, FKs, indexes, concurrency, annotations, PII/secret absence, and lookup/enum seed parity remain covered.
  - **Effort:** XL
  - **Dependencies:** 6.2
  - **Guidance:** Do not manufacture expression-tree helpers when a literal is the database contract.

- [ ] **6.4 Delete obsolete persistence reflection/source helpers and verify zero behavior-dispatch/source-scrape references and zero model drift.**
  - **Files:** test-local reflection surfaces, repository-root/source-read helpers, `LiteralQueueOrder` test-only mirror if no production consumer, affected tests/docs.
  - **Acceptance:** AST/LSP finds no runtime-name behavior dispatch in the migrated cohorts; source-scrape helpers and duplicate constants are removed; complete persistence project passes; generated model/snapshot diff remains zero.
  - **Effort:** L
  - **Dependencies:** 6.1–6.3
  - **Guidance:** A helper remains only if it inspects legitimate compiled metadata and has a named invariant.

- [ ] **6.5 Harden persistence DID lookups and verify five-provider product-catalog model parity with unchanged scalar columns, indexes, and generated artifacts.**
  - **Files:** graph-discovered AT Protocol repositories/configurations, `tests/Event.Persistence.IntegrationTests/Federation/AtprotoFederationPersistenceTests.cs`, `tests/Event.Persistence.IntegrationTests/ConfigurationManifest/ConfigurationManifestAuditProviderMigrationTests.cs` or a focused sibling using the same `PrimaryDatabaseProviderComposition`/`HasPendingModelChanges` seam; product-catalog migrations/snapshots are read-only evidence.
  - **Acceptance:** repository inputs accept `AtprotoDid` or its exact `.Value` at the port boundary, queries compare ordinal scalar values, PostgreSQL/SQLite/SQL Server/MariaDB/MySQL product-catalog models retain the existing column/index/filter shape and report no pending model changes, generated migration/snapshot hashes remain unchanged from the phase baseline, and DataProtection/privacy-authority catalogs are explicitly excluded because they do not persist AT Protocol identity state.
  - **Effort:** L
  - **Dependencies:** Phase 1
  - **Guidance:** Scenario 3.8; a model delta blocks the phase.

### Phase 6 Verification — Run Once After Tasks 6.1–6.5

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 7 — Generated Contract Determinism [NOT STARTED]

- **Phase dependencies:** Phases 1 and 3 must have passing phase gates before any Phase 7 task starts.

- [ ] **7.1 Add generated-contract regression coverage and verify DID, route, operation, and DTO wire shapes remain unchanged.**
  - **Files:** focused new/existing tests under `tests/Explore.GeneratedContracts.Tests/**`, generator inputs, generated OpenAPI/client as read-only evidence.
  - **Acceptance:** tests prove DID remains string-shaped on the wire, intentional route/operation identifiers retain exact values, no compatibility DTO/route appears, and a deliberate generated-shape mutation fails the focused contract.
  - **Effort:** L
  - **Dependencies:** Phases 1 and 3
  - **Guidance:** Machine-consumed generated output is a valid contract; never assert prose or hand-edit the product.

- [ ] **7.2 Run the canonical API-first generation chain and verify schema, client, and inventory remain byte-identical to the phase baseline.**
  - **Files:** `schemas/openapi_islamu-event.json`, `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `docs/API_CONTRACT_INVENTORY.md`, `eng/tools/Explore.ApiContractInventory/**`, existing MSBuild/NSwag/Roslyn inputs.
  - **Acceptance:** capture pre-phase hashes; run `dotnet msbuild src/Explore.API/Explore.API.csproj -target:GenerateOpenApiDocuments -property:Configuration=Release`, then `dotnet msbuild src/Explore.Blazor.Client/Explore.Blazor.Client.csproj -target:GenerateApiClient -property:Configuration=Release`, then `dotnet run --project eng/tools/Explore.ApiContractInventory/Explore.ApiContractInventory.csproj --configuration Release --no-launch-profile`; schema/client/inventory hashes remain identical to baseline; the phase still ends with one Release solution build and one generated-contract project gate; any intentional drift triggers plan/I-VSD/release re-baselining.
  - **Effort:** M
  - **Dependencies:** 7.1
  - **Guidance:** Generated files are outputs, never patch targets.

### Phase 7 Verification — Run Once After Tasks 7.1–7.2

- [ ] `dotnet msbuild src/Explore.API/Explore.API.csproj -target:GenerateOpenApiDocuments -property:Configuration=Release`
- [ ] `dotnet msbuild src/Explore.Blazor.Client/Explore.Blazor.Client.csproj -target:GenerateApiClient -property:Configuration=Release`
- [ ] `dotnet run --project eng/tools/Explore.ApiContractInventory/Explore.ApiContractInventory.csproj --configuration Release --no-launch-profile`
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.GeneratedContracts.Tests/Explore.GeneratedContracts.Tests.csproj --configuration Release --verbosity quiet`

## Phase 8 — Typed Blazor Contract Tests [NOT STARTED]

- **Phase dependencies:** Phases 4 and 7 must have passing phase gates before any Phase 8 task starts.

- [ ] **8.1 Convert tenant directory-operator component tests and verify typed parameters preserve HAL, read-only, focus, live-region, conflict, and validation behavior.**
  - **Files:** `TenantDirectoryOperatorIdentitySectionTests.cs`, exact public component/model/service types.
  - **Acceptance:** remove runtime type/model creation, dynamic parameter dictionary, property setter reflection, and DispatchProxy; use generic rendering and direct models/services; focused selector passes every existing semantic assertion.
  - **Effort:** L
  - **Dependencies:** Phase 4
  - **Guidance:** Scenario 3.6; do not weaken accessibility or mock-mirror internal calls.

- [ ] **8.2 Convert tenant directory-operator service tests and verify exact HAL edit mapping, grouped patch, revision, and conflict behavior through the public service.**
  - **Files:** `TenantDirectoryOperatorIdentityAdminServiceTests.cs`, public service/model/result types.
  - **Acceptance:** instantiate/invoke/read typed services/results directly; remove reflected logger construction, method invocation, and property access; retain exact relation/method/href checks and safe conflict reload behavior.
  - **Effort:** M
  - **Dependencies:** 8.1
  - **Guidance:** Generated client contracts remain generator-owned.

- [ ] **8.3 Convert participant-readiness and waitlist tests and remove redundant transfer existence reflection while verifying HAL-driven rendered behavior.**
  - **Files:** `ParticipantReadinessComponentTests.cs`, `FairReturnWaitlistComponentTests.cs`, `TicketTransferComponentTests.cs`, typed add-on/purchase tests as precedents.
  - **Acceptance:** public components/services use generic rendering and typed HAL DTOs; waitlist gains real rendered action/state coverage; transfer keeps behavior tests and deletes only the redundant shape test; exact relations, bounded status, pending-action, focus, and one-time secret behavior remain.
  - **Effort:** L
  - **Dependencies:** 8.1
  - **Guidance:** `IVSD-F006/M006`.

- [ ] **8.4 Delete the shallow client auth-state claim wrapper and verify dock persistence uses server/framework-confirmed authentication only.**
  - **Files:** `src/Explore.Blazor.Client/Services/AuthStateService.cs`, `src/Explore.Blazor.Client/Contracts/Providers/IAuthStateService.cs`, `src/Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs`, `src/Explore.Blazor.Client/Services/Interop/ServerBackedDockLayoutPersistence.cs`, their client tests.
  - **Acceptance:** unused user/tenant claim APIs and raw claim/error logging are deleted, dock persistence depends on established authentication state or existing shell context, anonymous/authenticated behavior remains covered, and no compatibility interface remains.
  - **Effort:** M
  - **Dependencies:** Phase 4
  - **Guidance:** Apply the deletion test: a pass-through service that adds no invariant is removed, not renamed.

- [ ] **8.5 Remove local organization-member authority inference and verify only exact HAL relations render edit/delete actions.**
  - **Files:** `src/Explore.Blazor.Client/Pages/Organizations/OrganizationMembers.razor.cs`, matching Razor/test files, paused Blazor context/tasks.
  - **Acceptance:** first add a compiling component scenario where the exact HAL relation is present but local role/current-user inference suppresses it and verify it fails; then remove `AuthenticationStateProvider`, raw current-user claim state, and role-ID action checks from mutation visibility; typed tests prove relations alone control actions and preserve self/creator server policy; update paused workstream ownership without duplicating architecture.
  - **Effort:** M
  - **Dependencies:** Phase 4
  - **Guidance:** Missing server affordance is a server contract defect, never a reason for local role checks.

- [ ] **8.6 Classify remaining test-only `DynamicComponent`/runtime type uses and verify compile-time dependencies are migrated without changing legitimate runtime composition.**
  - **Files:** bounded Phase 0 inventory under `tests/Explore.Blazor.Client.Tests/**`, including `AnalyticsInitializerTests.cs` and the documented `InvokeLoadEventsAsync` / `SimulateTagToggle` reflection workarounds; `docs/TESTING.md`; paused Blazor context/tasks.
  - **Acceptance:** every public compile-time test dependency uses direct type/typed parameters; `AnalyticsInitializer` is exercised through its owning rendered parent/compiled seam; non-public load/tag actions are exercised through public rendered behavior; obsolete documented exceptions are removed; true descriptor/runtime-selected composition uses `typeof`/`nameof` or owning parent behavior without string production type names; no component is made public solely for testing; paused Tasks 16.1 and 16.6 are marked superseded by this workstream while Task 16.7 and Phase 6A remain paused.
  - **Effort:** XL
  - **Dependencies:** 8.1–8.5
  - **Guidance:** Do not absorb broad component/service decomposition, styling, localization, or visual work.

### Phase 8 Verification — Run Once After Tasks 8.1–8.6

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Phase 9 — Recurrence Guard And Release Closure [NOT STARTED]

- **Phase dependencies:** Phases 0–8 must have passing phase gates before any Phase 9 task starts.

- [ ] **9.1 Red Phase: add synthetic Roslyn audit fixtures and verify prohibited runtime dispatch/source assurance fails while legitimate metadata/artifact parsing passes.**
  - **Files:** new rule tests in `tests/Event.Architecture.Tests/`, minimal new `eng/tools/Explore.AssuranceAudit/` contracts if required for compilation.
  - **Acceptance:** red fixtures cover reflective behavior invocation/construction, string-selected production types, raw C#/Razor/CSS/Markdown/prose reads, and token/regex file assertions; green fixtures cover `typeof`/endpoint/EF metadata and structured JSON/YAML/schema/project parsing; no real file allowlist appears.
  - **Effort:** L
  - **Dependencies:** Phases 0–8
  - **Guidance:** Scenario 3.7; tool diagnostics may show bounded syntax location, never source dumps or sensitive values.

- [ ] **9.2 Implement the repository-owned assurance audit and verify deterministic category/location diagnostics.**
  - **Files:** new `eng/tools/Explore.AssuranceAudit/**` or smallest existing Roslyn host, solution/tool registration, intent verification commands, architecture tests.
  - **Acceptance:** reuse repository-owned SDK Roslyn without a new incompatible dependency; scan governed test projects deterministically; reject prohibited categories; permit semantic categories; output ordinally sorted bounded diagnostics; synthetic rule tests pass.
  - **Effort:** XL
  - **Dependencies:** 9.1
  - **Guidance:** This is an analyzer/tool, not a product test that scrapes source for business behavior.

- [ ] **9.3 Migrate remaining prohibited assurance and enable the guard and verify zero findings, zero generated/model drift, and plan-aligned I-VSD/docs.**
  - **Files:** remaining candidates from Task 0.2, canonical docs, workstream triad, I-VSD report, exact generated/model check inputs.
  - **Acceptance:** every prohibited raw-source/prose/runtime-dispatch candidate has a stronger executable seam or intentionally removed non-contract; audit exits zero; generated/OpenAPI/EF expected-zero checks pass; docs describe actual final ownership; all `IVSD-*` IDs map to completed scenarios/tasks; Tier 1/Tier 2 anonymized review returns no blocking finding.
  - **Effort:** XL
  - **Dependencies:** 9.2
  - **Guidance:** No debt baseline/allowlist and no late coverage deletion.

- [ ] **9.4 Changelog contribution and final commit composition: create/validate the change fragment and verify release impacts and terminal footer are complete; commit only with explicit user authorization.**
  - **Files:** generated `docs/releases/changes/CHG-<ULID>.yaml`, final workstream docs, no Git history mutation.
  - **Acceptance:** run repository `create-change` with type `refactor` and scope `architecture`; complete Breaking/Security/Migration/Configuration/OpenAPI/Operator dispositions; validate release policy; prepare `refactor(architecture): replace stringly typed assurance seams` plus generated `Change-Id`; do not execute `git commit` unless explicitly requested.
  - **Effort:** M
  - **Dependencies:** 9.3 and all prior phase verification gates
  - **Guidance:** Final task of final phase; no amend, rebase, force-push, or compatibility release note.

### Phase 9 Verification — Run Once After Tasks 9.1–9.4

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Remaining / Deferred Work

- **Paused Blazor clean-code program:** broad service decomposition, localization, styling, render-mode migration, `DynamicAuthSchemeManager` Task 16.7/Phase 6A, and unrelated DynamicComponent production design remain in `dev/pause/blazor-clean-code-refactor/`. Paused Tasks 16.1 and 16.6 are superseded and exact ownership is recorded during Tasks 8.5 and 8.6.
- **Additional value objects:** currency, country, email, and tenant slug remain scalar under existing scoped validators. Reconsider only after a concrete invalid-state/type-confusion invariant is evidenced.
- **Additional DID methods:** syntax may represent future/unsupported methods; support policy remains federation-adapter-owned.
- **Public API or schema change:** not planned. Any discovered need requires plan, I-VSD, OpenAPI, release, and task re-baselining before implementation.

## Final Definition Of Done

- All 39 implementation tasks and every phase gate are checked.
- Report inventory and transitive helper consumers have final dispositions.
- Critical invariant replacements passed before old helpers were deleted.
- Assurance audit is enabled with zero findings and no historical allowlist.
- Platform identity, purpose-bound schemes, DID, tenant, authorization, HAL, privacy, money, state-machine, and concurrency scenarios pass.
- Generated artifacts and EF model show expected zero drift.
- Workstream plan/context/tasks and I-VSD report agree with repository state.
- Unrelated dirty-tree changes remain untouched.
