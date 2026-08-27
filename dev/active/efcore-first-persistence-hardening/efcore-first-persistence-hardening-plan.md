<!-- ABOUTME: Canonical implementation plan for enforcing EF Core-first multi-provider persistence. -->
<!-- ABOUTME: Defines behavior contracts, architecture decisions, phase exits, critical invariants, and release evidence. -->

# EF Core-First Persistence Hardening — Implementation Plan

Last Updated: 2026-08-27 Europe/Brussels

## 0. Planning Metadata

- **Original request:** fully remediate the audited persistence deviations so
  native EF Core/LINQ is always preferred, provider-supported APIs are the
  second rung, SQL is a true last resort, snake-case conventions own physical
  naming, and all supported databases retain enterprise-grade behavior without
  backward-compatibility shims.
- **Task directory:** `dev/active/efcore-first-persistence-hardening/`
- **Planning status:** Draft; ready for user review. Product implementation has
  not started.
- **Change classification:** Mixed.
  - **Behavioral Delta:** fixes provider portability defects, makes unsupported
    physical-name paths work through the EF model, and formalizes concurrency
    and migration outcomes across all engines.
  - **Non-Behavioral Delta:** removes redundant mappings, centralizes provider
    primitives, reduces internal API spread, and adds architecture enforcement.
- **Primary intent:** `update-repository-query` (`domain_state`).
- **Secondary intent:** `add-ef-migration` (`security`, Tier 1).
- **Critical overlays:** Tier 0 payment/inventory/refund/admission invariants and
  Tier 2 tenant/privacy-erasure invariants.
- **Evidence packet:**
  [`efcore-first-persistence-hardening-evidence.md`](efcore-first-persistence-hardening-evidence.md)
- **I-VSD assessment:**
  [`i-vsd-efcore-first-persistence-hardening.md`](../../../islamic-value-sensitive-design/i-vsd-efcore-first-persistence-hardening.md)
- **Operational artifacts:**
  [`efcore-first-persistence-hardening-context.md`](efcore-first-persistence-hardening-context.md)
  and
  [`efcore-first-persistence-hardening-tasks.md`](efcore-first-persistence-hardening-tasks.md).

## 1. Executive Summary

The repository has one EF Core persistence model and five relational provider
modes, but it currently permits repositories to bypass that model through raw
SQL, direct ADO commands, physical table-name strings, scattered provider-name
checks, and provider-specific branches that duplicate portable EF paths. This
creates a second, implicit database model that drifts from configured schemas,
the `ie_` prefix, tenant filters, soft-delete rules, naming conventions, and
provider translation behavior.

The target state is one enforceable persistence capability ladder:

1. native EF Core model metadata, tracked entities, LINQ, specifications,
   `ExecuteUpdate/Delete`, transactions, and concurrency tokens;
2. documented public provider APIs and provider-translated .NET expressions;
3. isolated, parameterized provider primitives only where the first two rungs
   cannot express the required engine behavior.

The implementation is not a mechanical search-and-replace. It is a
test-first, domain-by-domain hardening program. Payment, inventory, admission,
outbox, webhook, notification, privacy-erasure, and tenant-isolation invariants
must be locked with failing tests before changing the persistence seam. The
work then removes ordinary SQL, normalizes naming, isolates the remaining
engine primitives, simplifies migration extensibility, generates provider-owned
corrective migrations, and proves behavior on all five real engines.

## 2. Source-Grounded Current State Report

### 2.1 Current Architecture

- `ExploreDbContext` is the shared EF Core unit of persistence.
- `PrimaryDatabaseProviderComposition` selects PostgreSQL, SQLite, SQL Server,
  MariaDB, or MySQL and owns provider migration routing.
- `EFCore.NamingConventions` applies snake case in runtime and design-time
  contexts.
- PostgreSQL and SQL Server use a configurable model schema. SQLite, MariaDB,
  and MySQL use a deterministic `ie_` table prefix.
- Provider-owned migration assemblies already exist for the non-canonical
  providers, Data Protection, and retained authority storage.
- Repositories return entities and own persistence behavior behind Application
  contracts.
- Native set-based EF mutation is already established: the audit found 222
  `ExecuteUpdateAsync` and 75 `ExecuteDeleteAsync` sites.

### 2.2 Confirmed Healthy Foundations

- Context pooling uses property-injected tenant/current-user state.
- Named tenant and soft-delete filters fail closed by default.
- Explicit tenant bypass reasons are architecture-tested.
- Concurrency stamps, lease tokens, processing fences, unique constraints,
  transactions, outboxes, and idempotency records already model critical state.
- Provider composition and model-building integration tests already cover all
  five provider modes.
- Separate provider migrations follow EF's documented multiple-provider model.
- Existing portable EF implementations can replace several PostgreSQL-only
  branches without creating a new data-access abstraction.

### 2.3 Confirmed Defects And Gaps

- 51 EF raw-SQL calls occur across 26 non-generated persistence files.
- 24 direct ADO construction/`CommandText` markers occur across nine files.
- Cross-provider raw statements embed table names that disagree with
  configurable schemas or the `ie_` prefix.
- 228 literal table mappings, 79 literal column mappings, and 428 literal
  database index names duplicate or obscure convention ownership.
- Provider-name literals and engine switches leak into domain repositories.
- Raw SQL has no general architecture gate or exact machine-readable exception
  registry.
- The migration layer imports four provider/EF `.Internal` namespaces.
- The largest SQL concentration,
  `EmailDispatchOutboxRepository`, maintains provider-specialized and portable
  implementations in the same repository.
- Real-provider tests are broad but do not yet prove every replacement
  scenario, concurrency race, naming invariant, or migration lifecycle.

### 2.4 In Scope

- Non-generated `src/Explore.Persistence` runtime persistence.
- Provider composition and provider primitive boundaries.
- Entity configurations and model naming conventions.
- Primary, Data Protection, and retained-authority migration generation.
- `src/Explore.Secrets/Database` only where schema/provider composition must be
  preserved or tested.
- Persistence architecture and integration tests.
- Critical-path tests in Domain, Application, Infrastructure, and API where a
  persistence refactor could violate an existing invariant.
- Persistence, provider, migration, testing, operations, backup/restore, schema,
  and release documentation.

### 2.5 Out Of Scope

- New product capabilities, endpoints, UI, or public contract changes.
- Replacing EF Core or any currently supported database provider.
- Introducing a second ORM, micro-ORM, query builder, or provider-lock package.
- Hand-editing generated migrations or model snapshots.
- Weakening tenant filters, outbox semantics, erasure ordering, payment state,
  admission state, or concurrency fencing.
- Refactoring unrelated Application, Domain, API, or Blazor code.
- Editing the completed Registration Data Collection or plan-blocked Event
  Ticketing Lifecycle workstreams.

### 2.6 Open Questions

No material open question is deferred. Repository evidence resolved provider
scope, configurable-schema ownership, naming authority, migration ownership,
critical invariants, test strategy, and the SQL exception boundary.

Implementation-time operator approval is still required before recreating a
developer database. That approval does not change this plan's architecture or
task sequence.

### 2.7 Release & Changelog Strategy

- This work has no public HTTP/API contract delta, so
  `docs/API_CHANGELOG.md` SHALL remain unchanged unless implementation exposes
  an unexpected contract change.
- `docs/semantic_versioning/CHANGELOG.md` SHALL record persistence portability,
  migration, operator-action, and provider-support changes.
- `docs/RELEASE_CHECKLIST.md` SHALL require migration inspection, provider
  parity evidence, backup/restore evidence, and documented rollback.
- `schemas/islamu-event.md` SHALL be regenerated or updated from the final EF
  model and generated migrations.
- Any corrective migration requiring development database recreation MUST be
  called out in release notes and `docs/BACKUP_RESTORE_UPGRADE.md`.

## 3. Proposed Future State: Behavioral Contract & Scenarios

### 3.1 Normative Requirements

- **PERSIST-BR-001:** Every ordinary repository query and mutation SHALL use
  native EF Core/LINQ before any provider-specific mechanism.
- **PERSIST-BR-002:** A provider-specific mechanism MUST use a documented public
  provider API or provider translation before SQL is considered.
- **PERSIST-BR-003:** Raw SQL or direct ADO access MUST exist only inside the
  approved provider-primitive boundary and MUST have a capability test proving
  that the preceding rungs cannot preserve the required behavior.
- **PERSIST-BR-004:** Unavoidable SQL MUST parameterize values and MUST derive
  mapped identifiers from EF metadata plus `ISqlGenerationHelper`.
- **PERSIST-BR-005:** Repositories MUST NOT branch on provider-name strings or
  embed physical table, schema, column, key, index, or constraint names.
- **PERSIST-BR-006:** `UseSnakeCaseNamingConvention()` SHALL remain the default
  physical naming authority for every relational context.
- **PERSIST-BR-007:** Explicit relational names MUST represent a semantic
  exception that conventions cannot express and MUST be machine-reviewable.
- **PERSIST-BR-008:** Tenant and soft-delete filters, exact bypass reasons,
  repository entity returns, and explicit navigation loading MUST remain
  unchanged.
- **PERSIST-BR-009:** Payment, inventory, refund, admission, outbox, webhook,
  notification, idempotency, lease, and erasure transitions MUST preserve
  their current monotonic, atomic, and fenced outcomes under concurrency.
- **PERSIST-BR-010:** Generated migration files and snapshots MUST be produced
  by EF tooling and MUST NOT be patched by hand.
- **PERSIST-BR-011:** PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL SHALL
  produce equivalent domain outcomes for every shared persistence scenario.
- **PERSIST-BR-012:** SQL diagnostics, test evidence, logs, and metrics MUST
  remain free of connection secrets, SQL parameter values, PII, and tenant
  payloads.
- **PERSIST-BR-013:** Provider support SHALL be claimed only after real-engine
  model, migration, behavior, concurrency, and lifecycle evidence is green.
- **PERSIST-BR-014:** No compatibility shim, legacy physical-name alias, dual
  repository path, or fallback SQL branch SHALL be introduced solely to
  preserve development-era behavior.

### 3.2 Scenario PERSIST-S1 — Model-Owned Physical Naming

**GIVEN** any supported provider and a valid configured schema or fixed prefix  
**WHEN** the EF model is finalized, migrations are generated, and a repository
performs CRUD  
**THEN** table, schema, column, key, index, and constraint identifiers come from
the finalized model; no repository-local physical name is required.

### 3.3 Scenario PERSIST-S2 — Portable Conditional Mutation

**GIVEN** an active event session, agenda item, inventory hold, notification
run, or email state row  
**WHEN** a caller performs a conditional state transition through any supported
provider  
**THEN** EF executes a naming-safe conditional mutation, reports the affected
row count, and preserves the same success/conflict result on every provider.

### 3.4 Scenario PERSIST-S3 — Tenant And Soft-Delete Isolation

**GIVEN** same-key rows in two tenants and an active plus soft-deleted row  
**WHEN** a refactored query, set-based update, claim, or delete runs  
**THEN** only the explicitly authorized tenant and active rows participate,
and any cross-tenant worker uses its exact named bypass reason plus an exact
tenant predicate.

### 3.5 Scenario PERSIST-S4 — Contended Claim And Fence

**GIVEN** two independent contexts or processes racing for the same claim,
idempotency key, inventory capacity, payment effect, admission authority, or
outbox work  
**WHEN** both execute concurrently  
**THEN** at most one wins the protected transition, fences remain monotonic,
the loser receives the documented conflict/no-op outcome, and no duplicate
durable effect is created.

### 3.6 Scenario PERSIST-S5 — Critical-State Preservation

**GIVEN** payment/refund, inventory, admission, webhook, notification, and
privacy-erasure flows with existing durable state  
**WHEN** their persistence implementation is converted from SQL or provider
branches to the target architecture  
**THEN** financial state remains monotonic, inventory cannot oversell,
admission cannot duplicate authority, outboxes remain transactionally paired,
erasure remains authority-first, and erased PII cannot resurrect.

### 3.7 Scenario PERSIST-S6 — SQL Escape-Hatch Enforcement

**GIVEN** a contributor introduces `ExecuteSql*`, `FromSql*`, `SqlQuery*`,
`DbCommand.CommandText`, a provider-name literal, or a redundant literal table
mapping outside the approved boundary  
**WHEN** architecture tests run  
**THEN** the build fails with the violating file/type and the required
capability-ladder remedy.

### 3.8 Scenario PERSIST-S7 — Generated Provider Migrations

**GIVEN** the normalized model and each provider-owned migration target  
**WHEN** migrations are generated and applied to a fresh real engine  
**THEN** generated artifacts contain the convention-owned identifiers,
configurable schema or fixed prefix, reversible operations, matching snapshots,
and no pending model changes.

### 3.9 Scenario PERSIST-S8 — Provider Upgrade Compatibility

**GIVEN** pinned EF Core, Npgsql, Microting, naming-convention, SQL Server, and
SQLite package versions  
**WHEN** migration services and models are composed  
**THEN** public provider seams work, internal migration seams remain confined to
their exact adapter, and a constructor/service drift fails a focused
compatibility test rather than production startup.

### 3.10 Scenario PERSIST-S9 — Real-Engine Parity And Performance

**GIVEN** representative cardinality and concurrency for each converted
repository seam  
**WHEN** PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL execute the scenario  
**THEN** all domain outcomes match, query count and selected columns are
bounded, no client evaluation or N+1 appears, and performance remains within
the recorded phase baseline or an approved provider exception.

### 3.11 Scenario PERSIST-S10 — Zero-Sensitive Persistence Evidence

**GIVEN** a failed query, claim race, migration, or provider primitive  
**WHEN** logs, traces, metrics, test output, and review evidence are emitted  
**THEN** they contain operation codes and bounded counts only, never secrets,
connection strings, SQL parameter values, PII, or tenant payloads.

## 4. Non-Negotiable Constraints

1. Repositories return entities, never DTOs or `IQueryable`.
2. Named tenant and soft-delete filters remain active unless the exact named
   filter is intentionally bypassed and tested.
3. Complex reads continue to use the established specification pattern.
4. Provider behavior remains inside Persistence; Application and Domain do not
   reference EF Core or provider packages.
5. Migration and snapshot files are generated only.
6. Existing critical transaction, outbox, idempotency, lease, and fencing
   boundaries are behavioral contracts, not implementation details.
7. No dependency is added without clean-room provenance and outbound-license
   approval.
8. No raw SQL is retained for convenience, code brevity, or assumed
   performance.
9. A specialized SQL path is not accepted without measured evidence and a
   real-engine invariant test.
10. Product code is not implemented until this plan is user-approved and the
    tasks/context/I-VSD revisions are aligned.

## 5. Architecture And Design Decisions

### ADR-PERSIST-001 — One EF Model, Provider Composition At The Edge

`ExploreDbContext` remains the shared model. Provider selection, migration
assembly routing, schema/prefix policy, and provider options remain in
`PrimaryDatabaseProviderComposition` and schema infrastructure. Repositories
express domain persistence through the shared model and do not own engine
selection.

### ADR-PERSIST-002 — Enforced Persistence Capability Ladder

The architecture suite owns a machine-consumed rule set:

1. EF Core/LINQ and model metadata;
2. public provider API or translated .NET expression;
3. approved provider primitive.

The initial rule includes an exact temporary inventory of existing violations
so implementation can remain green while entries are removed. New violations
are forbidden immediately. The final registry contains only approved provider
primitive types, not repository methods.

### ADR-PERSIST-003 — Native Concurrency Before Pessimistic SQL

Concurrency is expressed in this order:

1. conditional `ExecuteUpdate/Delete` plus affected-row checks;
2. tracked concurrency tokens and `DbUpdateConcurrencyException`;
3. unique constraints plus deterministic conflict classification;
4. explicit EF transactions and the least strong sufficient isolation level;
5. provider lock/`SKIP LOCKED` primitive only when the first four cannot
   preserve the measured concurrency contract.

This removes raw upsert and no-op update patterns where a native conditional
transition can provide the same fence.

### ADR-PERSIST-004 — Naming Convention As Physical Schema Authority

`EFCore.NamingConventions` remains pinned and enabled in every relational
context. Redundant `ToTable`, `HasColumnName`, `HasDatabaseName`, constraint
names, and provider-local naming strings are removed. Semantic mappings that
cannot be inferred—such as flattened owned-value prefixes—remain explicit and
are covered by model assertions.

Exception classification reads the finalized EF metadata instead of duplicating
constraint names in repository constants.

### ADR-PERSIST-005 — Narrow Provider Primitive Boundary

Unavoidable engine behavior moves under
`src/Explore.Persistence/Database/ProviderPrimitives/` or an equally narrow
existing infrastructure namespace. Approved categories are:

- advisory/application/session locks;
- `FOR UPDATE SKIP LOCKED` where a queue test proves necessity;
- SQLite connection PRAGMAs;
- PostgreSQL session `set_config`;
- retained-authority SECURITY DEFINER function invocation;
- database-authoritative clock reads where `TimeProvider` cannot satisfy the
  contract;
- provider migration SQL generation and database constraints with no public
  expression API.

The boundary stays internal to Persistence. It uses a small capability-focused
API rather than one interface and class per provider by default.

### ADR-PERSIST-006 — Metadata-Safe SQL

Any surviving relational statement resolves entity types, store objects,
schemas, table names, and columns from `IModel` and delimits them through
`ISqlGenerationHelper`. Callers pass property expressions or EF property names,
not physical column strings. Values use interpolation/parameters only.

### ADR-PERSIST-007 — Generated Multi-Provider Migration Ownership

Separate migration sets remain. Model/configuration fixes occur first, then EF
tooling generates every affected provider migration and snapshot. Historical
scaffold-time backfill logic is removed after generated corrective migrations
own the final transition.

The configurable-schema generator remains a narrow adapter while Npgsql's
public generator constructor depends on an internal options contract. Its
package coupling is explicit and compatibility-tested. Internal imports are
forbidden everywhere else.

### ADR-PERSIST-008 — Real Engines Are The Integration Boundary

SQLite/in-memory substitutes do not establish PostgreSQL, SQL Server, MariaDB,
or MySQL translation or concurrency behavior. Each shared scenario runs against
its real engine. Test doubles may isolate pure decision logic but never replace
the provider integration seam being asserted.

### ADR-PERSIST-009 — Development-Mode Breaking Cleanup

No compatibility shim preserves redundant names, duplicate repository paths,
or old raw behavior. Tracked migration history still follows repository
governance: generated corrective migrations are used, merged generated files
are not hand-edited, and disposable databases may be recreated by operators
after backup/restore implications are documented.

## 6. Implementation Phases

### Phase 0 — Baseline, Impact Graph, And Invariant Breakers

Establish the clean code/build and relevant test baseline once. Capture the
current raw/naming/provider inventory as machine-readable evidence. Use the
knowledge graph to map callers, affected flows, and tests for each repository
cluster. Add failing invariant-breaker tests for physical naming, provider
parity, tenant isolation, critical concurrency, erasure ordering, and
zero-sensitive evidence before production edits.

**Exit criteria:** the baseline is recorded; every affected cluster has named
callers/tests; Red tests fail for the intended defect; unrelated failures are
identified without being fixed.

### Phase 1 — Architecture Gates And Exception Registry

Implement architecture rules for raw SQL APIs, direct commands, provider-name
literals, literal table mappings, and internal provider namespaces. Seed the
exact temporary violation inventory, prove a synthetic new violation fails,
and define the final approved provider-primitive boundary.

**Exit criteria:** new drift is blocked immediately; every existing violation
has an owner/removal phase; the registry contains machine-consumed identities,
not free-form prose.

### Phase 2 — Naming And Relational Model Normalization

Remove redundant table mappings by bounded domain cluster, align CLR/`DbSet`
names where the desired physical name differs, preserve semantic owned-value
column mappings, remove redundant index/constraint names, derive exception
classification names from metadata, and verify finalized metadata for all
providers.

**Exit criteria:** convention-generated naming is authoritative; all true
exceptions have model tests; configurable schemas and `ie_` prefixes are
unchanged; no repository depends on a physical name.

### Phase 3 — Ordinary Query And Mutation Conversion

Replace ordinary SQL and direct commands with LINQ, tracked entities,
specifications, `ExecuteUpdate/Delete`, and affected-row checks. Work by
critical domain cluster: schedule/location, inventory/registration,
notifications/email, webhook/ATProto, idempotency, payment/refund, admission,
and privacy authority. Remove duplicate PostgreSQL paths when the portable EF
path preserves the same contract.

**Exit criteria:** ordinary SQL is absent from repositories; every converted
cluster passes its Red/Green behavior, tenant, concurrency, and provider tests;
the temporary exception inventory shrinks at each cluster exit.

### Phase 4 — Concurrency, Upsert, Queue, And Lock Redesign

Replace raw upserts, no-op lock updates, and scattered advisory locks with
native conditional transitions, concurrency stamps, unique-conflict handling,
transactions, and isolation levels. Retain provider locks or `SKIP LOCKED` only
where a representative concurrent test proves native EF cannot meet the
contract or required throughput.

**Exit criteria:** claims and fences are deterministic under independent
contexts/processes; no duplicate durable effect appears; retained lock
primitives have necessity and provider tests.

### Phase 5 — Provider Primitive Isolation

Move remaining engine commands into capability-focused provider primitives,
centralize provider detection, derive identifiers from EF metadata, parameterize
values, and add unsupported-provider failure behavior. Keep SQLite PRAGMA,
PostgreSQL session state, retained-authority functions, and database locks
separate from domain repositories.

**Exit criteria:** raw APIs and direct ADO markers occur only in approved
primitive types; repository provider-name literals are zero; all primitive
tests pass on their actual engines.

### Phase 6 — Migration Infrastructure And Generated Artifacts

Remove obsolete `ApplicationMigrationsModelDiffer` compatibility backfills,
isolate configurable-schema generator package coupling, add focused
constructor/service compatibility tests, generate every affected provider
migration and snapshot through EF tooling, and inspect generated SQL.

**Exit criteria:** no generated file was hand-edited; each provider applies from
empty, rolls back within the supported generated boundary, reapplies, reports no
pending model changes, and owns the correct history table/schema/prefix.

### Phase 7 — Real-Engine Parity, Performance, And Mutation Quality

Run all behavioral scenarios against PostgreSQL, SQLite, SQL Server, MariaDB,
and MySQL. Capture query count, SQL shape without values, row count, allocation
or duration where material, and concurrent outcomes. Extend mutation scope to
owned portable persistence decision logic and achieve greater than 85% for the
critical owned slice.

**Exit criteria:** all five providers satisfy domain outcomes; performance
regressions are fixed or explicitly approved with evidence; the critical
persistence mutation score exceeds 85%; no sensitive evidence is emitted.

### Phase 8 — Documentation, Release Evidence, And Adversarial Review

Update architecture, codebase insights, testing, operations, configuration,
backup/restore, release checklist, schema, and semantic changelog artifacts.
Run anonymized MAD review across architecture, security/privacy, critical-state,
provider/migration, and operations personas. Resolve every blocking finding and
revalidate I-VSD mappings against the final implementation revision.

**Exit criteria:** documentation matches shipped behavior; rollback and
operator actions are executable; MAD has no unresolved blocker; I-VSD is
current; full required verification is green.

## 7. Testing Strategy

### 7.1 Red-Before-Green Matrix

| Scenario | Red specification seam | Green evidence |
| --- | --- | --- |
| `PERSIST-S1` | Model metadata and raw physical-name provider tests | All provider models and affected CRUD paths use mapped identifiers |
| `PERSIST-S2` | Repository transition tests on prefix/schema providers | Native EF mutation returns identical success/conflict outcomes |
| `PERSIST-S3` | Cross-tenant and soft-delete invariant breakers | Exact tenant rows mutate; bypass reason remains explicit |
| `PERSIST-S4` | Two-context/process race tests | One winner, monotonic fence, no duplicate effect |
| `PERSIST-S5` | Payment/inventory/admission/outbox/erasure invariant breakers | Existing domain state machine and commit ordering remain intact |
| `PERSIST-S6` | Synthetic forbidden persistence fixture | Architecture test identifies and rejects the exact violation |
| `PERSIST-S7` | Pending-model/fresh-engine migration tests | Generated migration lifecycle and snapshots are green |
| `PERSIST-S8` | Provider migration-service composition tests | Package/service drift fails focused compatibility tests |
| `PERSIST-S9` | Representative provider matrix and query diagnostics | Equivalent outcomes and accepted performance envelope |
| `PERSIST-S10` | Sensitive log/trace/test sink assertions | Only bounded identifiers, codes, and counts are emitted |

Tests subscribe to exact events or state changes before triggering asynchronous
work. Fixed sleeps and timing-luck polling are forbidden.

### 7.2 Active Development Slices

During a domain cluster, run only the named TUnit test class with
`--treenode-filter`, `--minimum-expected-tests 1`, no progress UI, and bounded
parallelism. Do not rerun the unchanged full baseline after each edit.

### 7.3 Phase-Exit Verification

- `Event.Persistence.IntegrationTests` for repository, model, provider,
  migration, concurrency, and lifecycle behavior.
- `Event.Architecture.Tests` for capability-ladder, Clean Architecture,
  generated-migration, naming, and filter rules.
- `Event.Domain.UnitTests` and `Event.Application.UnitTests` for critical state
  and request/handler invariants touched by the persistence seam.
- `Explore.Infrastructure.Tests` for provider/outbox/telemetry integration
  affected by dispatch or reconciliation changes.
- `Event.API.IntegrationTests` only for externally observable critical flows
  whose persistence outcome changes.
- Release build at phase boundaries and PR completion.

### 7.4 Real-Engine Evidence

Use repository-supported engine fixtures/containers. Provider tests MUST cover:

- model creation and finalized names;
- fresh migration, rollback boundary, and reapply;
- representative CRUD and set-based mutation;
- unique conflict and optimistic concurrency;
- transaction and lock behavior;
- queue/lease race behavior;
- configurable schema or fixed prefix;
- Data Protection and retained-authority migration ownership where applicable.

### 7.5 Mutation And Adversarial Review

The existing Stryker configuration does not mutate Persistence. Add a bounded
persistence mutation target for capability-policy, naming-policy, concurrency,
and provider-neutral decision logic while excluding migrations and raw SQL
text. The Tier 0–2 owned slice MUST exceed 85%.

The final review uses anonymized MAD with weighted post-hoc voting and includes
at least architecture, concurrency/financial, tenant/privacy, provider/migration,
and operations viewpoints.

## 8. Documentation, Configuration, And Operations Impact

### Documentation

- `docs/ARCHITECTURE.md`: capability ladder and provider primitive boundary.
- `docs/CODEBASE_INSIGHTS.md`: naming ownership, SQL exception process, and
  provider traps.
- `docs/TESTING.md`: real-engine parity, architecture gate, and mutation slice.
- `docs/OPERATIONS.md`: migration lifecycle and provider evidence.
- `docs/CONFIGURATION.md`: confirm configurable schema and fixed-prefix behavior.
- `docs/BACKUP_RESTORE_UPGRADE.md`: corrective migration, developer database
  recreation, and rollback.
- `docs/RELEASE_CHECKLIST.md`: provider/migration/rollback evidence gates.
- `docs/semantic_versioning/CHANGELOG.md`: operator-visible hardening summary.
- `schemas/islamu-event.md`: final generated relational model.

### Configuration

No new secret or runtime setting is planned. `Database:Schema` and provider
selection remain authoritative. Any new non-secret test setting must be
documented in the owning configuration schema; secrets remain Infisical or
`.env` only.

### Operations

Operators need:

- explicit provider migration commands;
- backup-before-migration instructions;
- fresh-engine and pending-model checks;
- development database recreation steps;
- rollback limits where a generated migration is intentionally destructive;
- provider-specific limitations that survived the capability ladder.

## 9. Islamic Value-Sensitive Design (I-VSD) & Moral Boundaries

The planning-mode report is authoritative for provider-responsibility reasoning.
The mappings are:

| Finding | Scenarios | Task mappings |
| --- | --- | --- |
| `IVSD-PERSIST-001` provider-independent correctness | `PERSIST-S1`, `PERSIST-S2`, `PERSIST-S9` | 2.1–2.13, 3.1–3.18, 7.1–7.7 |
| `IVSD-PERSIST-002` tenant and erasure safety | `PERSIST-S3`, `PERSIST-S5`, `PERSIST-S10` | 0.6, 0.8, 3.15–3.16, 5.6–5.7, 7.10 |
| `IVSD-PERSIST-003` financial/admission monotonicity | `PERSIST-S4`, `PERSIST-S5` | 0.7, 3.3–3.4, 4.1–4.10, 7.1–7.5 |
| `IVSD-PERSIST-004` inspectable escape hatches | `PERSIST-S6`, `PERSIST-S8` | 1.1–1.10, 5.1–5.11, 6.1–6.4 |
| `IVSD-PERSIST-005` truthful portability claims | `PERSIST-S7`, `PERSIST-S9` | 6.5–6.14, 7.1–7.11, 8.1–8.10 |

No implementation may claim security, reliability, or provider parity from code
review alone. Those claims require the named operational evidence.

## 10. Security, Authorization, Privacy, And Abuse Considerations

- Tenant filters remain fail-closed in pooled contexts.
- Cross-tenant workers retain exact named bypass reasons and exact tenant
  predicates.
- PostgreSQL session tenant state remains connection-scoped and tested for
  pooled-connection reuse.
- Raw SQL values remain parameters; identifier composition accepts model
  metadata only.
- SQL logs never enable sensitive-data logging in shared or production
  environments.
- Privacy erasure retains authority-first commit ordering and anti-resurrection
  fencing.
- Payment/refund/inventory/admission races receive adversarial tests before
  implementation.
- Execution strategies wrap retriable transactions correctly; retries never
  duplicate external effects or outbox records.
- No test fixture hard-codes secrets or production-like credentials.

## 11. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

- **Multi-tenancy:** directly in scope; all query/mutation changes preserve
  named filters and explicit bypass contracts.
- **Federation/ATProtocol:** persistence claims and PDS/Jetstream outboxes retain
  idempotency, lease fences, and tombstone behavior.
- **Localization/RTL/accessibility:** no UI or prose behavior changes; not
  directly applicable.
- **Product behavior:** no new feature or API affordance. Provider paths that
  currently fail because of physical-name drift become conformant.
- **Self-hosting:** all current provider choices and configurable schemas remain
  supported and evidence-backed.

## 12. Observability And Operations

- Record database operation category, provider code, affected-row count,
  duration, retry/conflict classification, and correlation identifier only.
- Never log SQL parameter values, connection strings, tenant payloads, email
  addresses, payment provider payloads, erasure subject identifiers, or
  webhook bodies.
- Capture query count and SQL shape through safe EF diagnostics in tests.
- Alert on migration failure, pending model, repeated concurrency conflict,
  claim starvation, and provider primitive failure using bounded labels.
- Provider-specific performance exceptions require a durable evidence link and
  an owner; comments or assumptions are insufficient.

## 13. Migration And Compatibility Plan

1. Fix entities, configurations, conventions, and migration extensions first.
2. Generate provider-owned corrective migrations using EF tooling.
3. Inspect generated operations, SQL, schemas/prefixes, history tables,
   constraints, and `Down` behavior without patching generated files.
4. Apply to fresh real engines, test the supported rollback boundary, reapply,
   and assert no pending model changes.
5. Preserve merged generated migration history. Remove only an unapplied
   development migration through EF tooling.
6. Recreate disposable development databases when generated breaking
   corrections make that the safest path; require operator confirmation.
7. Do not add old-name aliases, dual schemas, compatibility views, fallback
   repositories, or SQL branches.
8. Remove obsolete scaffold-time backfill customization only after the generated
   corrective migration owns the final schema/data transition.

Rollback is artifact-based: revert the verified source increment, regenerate
corrective migrations when required, restore the pre-migration backup for
destructive development resets, and rerun provider lifecycle evidence. Git
destructive commands and migration-file hand edits remain forbidden.

## 14. Risk Register

| Risk | Likelihood | Impact | Mitigation |
| --- | --- | --- | --- |
| Portable EF conversion weakens a race fence | Medium | Critical | Red two-context/process invariant tests before each Green edit |
| Naming cleanup produces destructive generated operations | High | High | Inspect provider migrations, use disposable dev DBs, preserve generated governance |
| SQL removal regresses queue throughput | Medium | High | Representative baseline and provider-specific measured exception gate |
| Microting translation differs from SQL Server/Npgsql | High | High | Real MariaDB/MySQL tests; no inference from another provider |
| Internal migration constructor changes on package update | Medium | High | One adapter, pinned versions, focused composition compatibility test |
| Exception registry becomes a permanent waiver list | Medium | High | Phase ownership, zero repository entries at final gate |
| Refactor disables tenant filter or changes bypass | Low | Critical | Architecture and cross-tenant invariant breakers |
| Payment/refund/admission duplicate effects | Low | Critical | Unique constraints, transaction/outbox assertions, concurrency MAD review |
| Migration matrix causes verification sprawl | Medium | Medium | TUnit slices during work; full matrix only at phase exits |
| Diagnostic evidence exposes sensitive values | Low | Critical | Safe sinks and explicit zero-sensitive assertions |

## 15. Success Metrics And Definition Of Done

- Zero ordinary raw-SQL or direct ADO sites in repositories.
- Zero repository provider-name literals.
- Zero repository physical table/column/constraint identifiers.
- Every literal table mapping removed; semantic column/index exceptions covered
  by model tests.
- Raw APIs occur only in approved provider primitive and migration adapter
  types.
- Provider/EF `.Internal` imports occur only in the unavoidable migration
  adapter and are compatibility-tested.
- All five provider models, migrations, shared behavior, and concurrency
  scenarios pass on real engines.
- Tenant, soft-delete, payment, inventory, admission, outbox, idempotency,
  webhook, notification, and erasure invariants remain green.
- No pending model changes exist for any migration target.
- Critical owned persistence mutation score exceeds 85%.
- Build, required test projects, documentation checks, generated-artifact
  inspection, and anonymized MAD review are green.
- I-VSD status is `current` and disposition is `plan-aligned`.
- No compatibility shim, new ORM, or unapproved dependency is introduced.

## 16. Implementation Agent Contract — KEEP DEV DOCS CURRENT

- Start from
  [`efcore-first-persistence-hardening-context.md`](efcore-first-persistence-hardening-context.md)
  and the first unchecked item in
  [`efcore-first-persistence-hardening-tasks.md`](efcore-first-persistence-hardening-tasks.md).
- Load only the plan section named by the current task.
- Write the named failing test before behavior-changing production code and
  observe the intended failure.
- Mark each task immediately when its evidence is green.
- Update this plan only when strategy, scenario, phase ordering, acceptance,
  release, or rollback changes.
- Update context for baselines, blockers, decisions, review revisions, and
  session handoff.
- Never implement around a failing invariant, weaken a test, or broaden scope to
  unrelated cleanup.
- Never edit generated migrations or snapshots.
- Never retain raw SQL without satisfying the capability ladder and exception
  boundary.
- Revalidate I-VSD when any mapped scenario, provider support, critical
  invariant, migration strategy, or release claim changes.

## 17. Progress Reporting Contract

Implementation reports SHALL use:

```text
Phase: <phase name>
Implemented: <observable outcome>
Invariant evidence: <Red/Green test and provider>
Validation: <exact command and result>
Plan: updated | unchanged
Context: updated | unchanged
Tasks: reconciled
I-VSD: current | refreshed | stale
Risks or blockers: <specific evidence>
Next: <next unchecked task>
Docs updated: yes/no with reason
```

Status messages never imply completion before real-engine behavior and phase
exit evidence are green.

## 18. Potential Risks & Unknowns

The hardest technical risk is concurrency equivalence: several SQL paths exist
because they combine selection, locking, mutation, and returned identifiers in
one engine statement. EF Core cannot express every such shape. The plan avoids
pretending otherwise: it first proves the required domain outcome and throughput,
then uses the smallest native EF transaction that satisfies it, and retains a
provider primitive only when real evidence shows the native path cannot.

The second risk is migration extensibility. Npgsql's public migration generator
currently takes an internal options dependency, and Microting is explicitly a
fast-moving provider. The final architecture may therefore retain one isolated
version-coupled adapter. That is an acknowledged provider boundary, not a
general license for internal APIs.

No unresolved unknown currently changes scope, architecture, task breakdown, or
verification strategy.
