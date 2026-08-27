<!-- ABOUTME: Hot execution ledger for the EF Core-first persistence hardening workstream. -->
<!-- ABOUTME: Sequences Red/Green provider, naming, concurrency, migration, documentation, and review work. -->

# EF Core-First Persistence Hardening — Tasks

Last Updated: 2026-08-27 Europe/Brussels

## Status Summary

- **Overall:** NOT STARTED
- **Implementation approval:** REQUIRED
- **Current phase:** Planning complete; wait for user review
- **Completed implementation tasks:** 0
- **Plan:**
  [`efcore-first-persistence-hardening-plan.md`](efcore-first-persistence-hardening-plan.md)
- **Context:**
  [`efcore-first-persistence-hardening-context.md`](efcore-first-persistence-hardening-context.md)
- **Evidence:**
  [`efcore-first-persistence-hardening-evidence.md`](efcore-first-persistence-hardening-evidence.md)
- **I-VSD:**
  [`i-vsd-efcore-first-persistence-hardening.md`](../../../islamic-value-sensitive-design/i-vsd-efcore-first-persistence-hardening.md)

## Implementation Maintenance Rules

- Start from the first unchecked task only after user approval.
- For every behavioral pair, observe the Red test fail for the named reason
  before editing production code.
- Mark a task complete immediately after its verification evidence is green.
- Run focused TUnit class slices during implementation; run full project
  commands only at phase exits.
- Update the plan only for strategy, scenario, phase, acceptance, release, or
  rollback changes.
- Update context for baselines, decisions, blockers, review revisions, and
  handoffs.
- Never edit a generated migration or snapshot.
- Never weaken tenant, critical-state, erasure, or concurrency tests.
- Revalidate I-VSD after any material mapped-plan change.

## Phase 0: Baseline And Invariant Breakers — NOT STARTED

- [ ] **0.1 Obtain explicit implementation approval.**  
  Verification: context records the approval revision; no product edit precedes it.

- [ ] **0.2 Refresh persistence impact graph.**  
  Scope: raw SQL owners, callers, callees, affected flows, provider composition,
  and tests.  
  Verification: context records bounded graph evidence for every repository cluster.

- [ ] **0.3 Establish clean code baseline.**  
  Verification: Release build plus currently required persistence and architecture
  tests exit zero; unrelated failures are recorded without repair.

- [ ] **0.4 Capture machine-readable violation baseline.**  
  Verification: exact raw API, direct ADO, provider literal, naming literal, and
  internal API owners are represented without source-prose matching.

- [ ] **0.5 RED provider physical-name mismatch tests.**  
  Scenarios: `PERSIST-S1`, `PERSIST-S2`.  
  Verification: schema/prefix-provider tests fail on the current hard-coded
  schedule, notification, or inventory statement.

- [ ] **0.6 RED tenant isolation mutation tests.**  
  Scenario: `PERSIST-S3`.  
  Verification: tests distinguish correct-tenant active rows from cross-tenant
  and soft-deleted rows and fail at the target seam.

- [ ] **0.7 RED critical concurrency invariant tests.**  
  Scenarios: `PERSIST-S4`, `PERSIST-S5`.  
  Verification: independent-context races cover inventory, payment/outbox,
  admission, idempotency, and erasure ordering before refactoring.

- [ ] **0.8 RED zero-sensitive evidence tests.**  
  Scenario: `PERSIST-S10`.  
  Verification: safe sinks reject parameter values, connection strings, PII,
  tenant payloads, and provider bodies.

- [ ] **0.9 Record performance and query baseline.**  
  Verification: representative email, notification, inventory, webhook, and
  claim paths record query count, selected columns, duration, and cardinality
  without sensitive values.

- [ ] **0.10 Complete Phase 0 verification gate.**  
  Verification: every planned behavioral cluster has a failing specification,
  baseline evidence, and named owner in context.

## Phase 1: Architecture Gates And Exception Registry — NOT STARTED

- [ ] **1.1 RED synthetic forbidden raw-SQL fixture.**  
  Scenario: `PERSIST-S6`.  
  Verification: the proposed architecture test fails on one test-owned
  `ExecuteSql*`, `FromSql*`, or `SqlQuery*` violation.

- [ ] **1.2 GREEN enforce raw-SQL boundary.**  
  Verification: architecture tests reject raw EF APIs outside the temporary
  exact registry and approved provider primitive namespace.

- [ ] **1.3 RED synthetic direct-ADO fixture.**  
  Scenario: `PERSIST-S6`.  
  Verification: architecture test fails on test-owned `DbCommand.CommandText`
  outside the approved boundary.

- [ ] **1.4 GREEN enforce direct-ADO boundary.**  
  Verification: direct commands are allowed only for exact temporary owners and
  approved provider primitives.

- [ ] **1.5 RED synthetic physical-name mapping fixture.**  
  Scenarios: `PERSIST-S1`, `PERSIST-S6`.  
  Verification: architecture/model test rejects a redundant literal
  `ToTable("...")`.

- [ ] **1.6 GREEN enforce table convention ownership.**  
  Verification: new literal table mappings fail; semantic model configuration
  overloads remain available.

- [ ] **1.7 RED provider-literal and internal-API fixtures.**  
  Scenario: `PERSIST-S8`.  
  Verification: tests fail on repository provider-name strings and `.Internal`
  imports outside the migration adapter.

- [ ] **1.8 GREEN enforce provider and internal seams.**  
  Verification: exact errors identify violating types and the permitted
  capability boundary.

- [ ] **1.9 Seed temporary violation registry.**  
  Verification: every current entry maps to one removal task; adding an
  unregistered violation remains red.

- [ ] **1.10 Complete Phase 1 verification gate.**  
  Verification: architecture project is green with the exact baseline registry,
  and synthetic regression tests prove every gate can fail.

## Phase 2: Naming And Model Normalization — NOT STARTED

- [ ] **2.1 RED finalized naming matrix tests.**  
  Scenarios: `PERSIST-S1`, `PERSIST-S7`.  
  Verification: tests assert convention-owned tables, columns, keys, indexes,
  constraints, configurable schemas, and `ie_` prefixes for all providers.

- [ ] **2.2 GREEN normalize tenancy and identity mappings.**  
  Scope: tenant, user, auth, actor, role, permission, and security
  configurations.  
  Verification: redundant table/index/constraint names are removed and model
  matrix remains green.

- [ ] **2.3 GREEN normalize organization and group mappings.**  
  Scope: organizations, groups, memberships, positions, and tenant links.  
  Verification: convention names match intended metadata on every provider.

- [ ] **2.4 GREEN normalize event and location mappings.**  
  Scope: events, sessions, agenda, days, locations, rooms, schedules, and
  publication state.  
  Verification: exclusion/check semantics remain intact and physical names are
  convention-owned.

- [ ] **2.5 GREEN normalize registration and admission mappings.**  
  Scope: orders, workflows, requirements, inventory, tickets, scanners, and
  authority rows.  
  Verification: alternate keys, concurrency stamps, and tenant-safe foreign
  keys remain unchanged.

- [ ] **2.6 GREEN normalize payment and refund mappings.**  
  Scope: payment attempts, checkout effects, refunds, disputes, and
  reconciliation rows.  
  Verification: monetary constraints and unique identities remain intact.

- [ ] **2.7 GREEN normalize notification and email mappings.**  
  Scope: fanout, deliveries, email outbox, attempts, receipts, and processor
  state.  
  Verification: status, lease, idempotency, and relationship metadata remain
  unchanged.

- [ ] **2.8 GREEN normalize webhook and federation mappings.**  
  Scope: incoming/outgoing webhooks, replay, publications, ATProtocol, PDS, and
  Jetstream rows.  
  Verification: provider identities and outbox uniqueness remain intact.

- [ ] **2.9 GREEN normalize settings and theme mappings.**  
  Scope: settings, policy, navigation, theme, appearance, and owned values.  
  Verification: semantic flattened column prefixes remain explicit; redundant
  mappings are removed.

- [ ] **2.10 RED metadata-derived constraint classification tests.**  
  Scenario: `PERSIST-S1`.  
  Verification: tests fail when repository exception classification depends on
  a duplicated physical constraint constant.

- [ ] **2.11 GREEN derive constraint names from metadata.**  
  Verification: PostgreSQL/domain conflict classification resolves finalized
  model metadata and remains provider-safe.

- [ ] **2.12 Remove completed naming registry entries.**  
  Verification: no literal table mapping remains; remaining explicit column or
  index mappings are covered by semantic model tests.

- [ ] **2.13 Complete Phase 2 verification gate.**  
  Verification: provider model tests, naming architecture tests, pending-model
  detection, and focused critical model tests are green.

## Phase 3: Native EF Query And Mutation Conversion — NOT STARTED

- [ ] **3.1 RED schedule move provider tests.**  
  Scenarios: `PERSIST-S2`, `PERSIST-S3`.  
  Verification: session and agenda moves prove rows-affected, tenant, soft-delete,
  relationship, and prefix/schema behavior.

- [ ] **3.2 GREEN convert schedule move mutations.**  
  Scope: `EventSessionRepository` and `EventAgendaItemRepository`.  
  Verification: native `ExecuteUpdateAsync` replaces ordinary SQL and all
  schedule tests pass.

- [ ] **3.3 RED inventory transition provider tests.**  
  Scenarios: `PERSIST-S2`, `PERSIST-S4`, `PERSIST-S5`.  
  Verification: consume, release, expire, order reconciliation, and concurrency
  outcomes fail at current physical-name/raw seams.

- [ ] **3.4 GREEN convert inventory transitions.**  
  Scope: `RegistrationInventoryRepository`.  
  Verification: native conditional updates and tracked transactions preserve
  affected-row and order-state outcomes.

- [ ] **3.5 RED notification settlement provider tests.**  
  Scenarios: `PERSIST-S2`, `PERSIST-S3`.  
  Verification: superseded occurrence/run settlement proves equivalent
  timestamps, statuses, leases, and tenant predicates.

- [ ] **3.6 GREEN convert notification settlements.**  
  Scope: fanout occurrence and run repositories.  
  Verification: transactional EF updates replace ordinary multi-table SQL where
  semantics permit.

- [ ] **3.7 RED email outbox parity tests.**  
  Scenarios: `PERSIST-S2`, `PERSIST-S4`, `PERSIST-S5`.  
  Verification: PostgreSQL and portable branches produce identical claim,
  suppression, hysteresis, receipt, and retry outcomes.

- [ ] **3.8 GREEN unify email outbox mutations.**  
  Scope: `EmailDispatchOutboxRepository` and eligibility evaluator.  
  Verification: existing portable EF paths become canonical and duplicate
  ordinary PostgreSQL SQL is removed.

- [ ] **3.9 RED webhook persistence parity tests.**  
  Scenarios: `PERSIST-S3`, `PERSIST-S4`, `PERSIST-S5`.  
  Verification: incoming claims, effect outboxes, bulk replay, local targets,
  and provider publications preserve leases and exact tenant predicates.

- [ ] **3.10 GREEN convert ordinary webhook mutations.**  
  Verification: LINQ/tracked/set-based EF replaces ordinary SQL; measured
  queue-specific primitives remain deferred to Phase 4.

- [ ] **3.11 RED ATProtocol persistence parity tests.**  
  Scenarios: `PERSIST-S3`, `PERSIST-S4`.  
  Verification: Jetstream and PDS claim/fence/tombstone behavior is fixed before
  conversion.

- [ ] **3.12 GREEN convert ordinary ATProtocol mutations.**  
  Verification: portable EF owns ordinary transitions; advisory/fence
  primitives remain isolated for Phase 4.

- [ ] **3.13 RED idempotency insertion race tests.**  
  Scenario: `PERSIST-S4`.  
  Verification: two contexts contend for one key and expose the current raw
  upsert seam.

- [ ] **3.14 GREEN replace raw idempotency upsert.**  
  Verification: EF add/query plus unique-conflict handling returns one owner and
  one existing record without provider SQL.

- [ ] **3.15 RED retained-authority EF parity tests.**  
  Scenarios: `PERSIST-S4`, `PERSIST-S5`.  
  Verification: counter, append, retention, legal hold, and authority-first
  ordering are locked before conversion.

- [ ] **3.16 GREEN convert ordinary authority state changes.**  
  Verification: tracked EF and native conditional mutation replace ordinary
  counter/row SQL; SECURITY DEFINER invocation remains a Phase 5 primitive.

- [ ] **3.17 Remove completed repository exception entries.**  
  Verification: registry contains no converted repository/type and architecture
  tests remain green.

- [ ] **3.18 Complete Phase 3 verification gate.**  
  Verification: all converted domain-cluster classes, tenant tests, critical
  state tests, and persistence integration project are green.

## Phase 4: Concurrency, Upsert, Queue, And Lock Redesign — NOT STARTED

- [ ] **4.1 RED payment/refund race matrix.**  
  Scenarios: `PERSIST-S4`, `PERSIST-S5`.  
  Verification: claim, dispatch, reconciliation, refund capacity, and duplicate
  effect races fail deterministically before edits.

- [ ] **4.2 GREEN use native financial fences.**  
  Verification: conditional EF mutation, unique constraints, concurrency stamps,
  and transactions preserve monotonic financial state.

- [ ] **4.3 RED admission authority race matrix.**  
  Scenarios: `PERSIST-S4`, `PERSIST-S5`.  
  Verification: issuance, revocation, check-in, scanner capability, and target
  operations prove one authoritative transition.

- [ ] **4.4 GREEN replace avoidable row-fence SQL.**  
  Verification: native conditional/concurrency semantics replace SQL where
  equivalent; unavoidable row lock accepts property expressions and model
  metadata only.

- [ ] **4.5 RED queue claim starvation tests.**  
  Scenarios: `PERSIST-S4`, `PERSIST-S9`.  
  Verification: representative concurrent workers measure duplicate claims,
  starvation, throughput, and lease recovery.

- [ ] **4.6 GREEN select minimal queue strategy.**  
  Verification: native EF strategy is used when it satisfies the contract;
  retained `SKIP LOCKED`/lock primitive has measured necessity evidence.

- [ ] **4.7 RED relational named-lock lifecycle tests.**  
  Scenario: `PERSIST-S4`.  
  Verification: PostgreSQL, SQL Server, MariaDB/MySQL, and SQLite cover acquire,
  contention, transaction completion, rollback, cancellation, and disposal.

- [ ] **4.8 GREEN consolidate lock semantics.**  
  Verification: repositories call one capability-focused lock boundary and
  contain no provider SQL or provider-name strings.

- [ ] **4.9 RED database-clock contract tests.**  
  Scenarios: `PERSIST-S2`, `PERSIST-S4`.  
  Verification: tests distinguish application `TimeProvider` from genuinely
  database-authoritative timing.

- [ ] **4.10 GREEN remove avoidable clock SQL.**  
  Verification: `TimeProvider` or provider-translated expressions own ordinary
  time; one scalar primitive remains only where required.

- [ ] **4.11 Complete Phase 4 verification gate.**  
  Verification: critical concurrency slices, provider lock tests, and
  performance comparisons are green with no duplicate effects.

## Phase 5: Provider Primitive Isolation — NOT STARTED

- [ ] **5.1 RED final provider-boundary architecture test.**  
  Scenarios: `PERSIST-S6`, `PERSIST-S8`.  
  Verification: repository raw APIs, direct commands, provider literals, and
  physical identifiers all fail outside approved primitive types.

- [ ] **5.2 GREEN centralize provider detection.**  
  Verification: public `IsNpgsql`/`IsSqlite`/`IsSqlServer` checks or one
  project-owned provider classification replace repository literals.

- [ ] **5.3 GREEN isolate advisory/application locks.**  
  Verification: PostgreSQL, SQL Server, MariaDB/MySQL, and SQLite lock commands
  exist only in the lock primitive and pass lifecycle tests.

- [ ] **5.4 GREEN isolate pessimistic row locks.**  
  Verification: retained row-lock/`SKIP LOCKED` commands use model metadata,
  delimited identifiers, parameter values, and real-engine tests.

- [ ] **5.5 GREEN isolate SQLite initialization PRAGMAs.**  
  Verification: WAL initialization remains SQLite-only, idempotent, and tested
  through the provider primitive boundary.

- [ ] **5.6 GREEN isolate PostgreSQL tenant session state.**  
  Verification: pooled connections set/clear tenant state safely and tests prove
  no cross-tenant session leakage.

- [ ] **5.7 GREEN isolate retained-authority functions.**  
  Verification: SECURITY DEFINER calls remain function-only, parameterized,
  role-bounded, and covered by authority tests.

- [ ] **5.8 GREEN isolate migration preflight commands.**  
  Verification: MariaDB/MySQL non-transactional DDL preflight remains bounded,
  metadata-safe, mutation-free, and PII-free.

- [ ] **5.9 Replace physical column string parameters.**  
  Verification: row-fence and provider primitive callers pass property
  expressions or model property identities, never physical columns.

- [ ] **5.10 Remove final repository exception entries.**  
  Verification: raw/direct-ADO registry contains approved primitive and
  migration adapter types only.

- [ ] **5.11 Complete Phase 5 verification gate.**  
  Verification: architecture, provider primitive, tenant session, authority,
  and lock projects are green.

## Phase 6: Migration Infrastructure And Artifacts — NOT STARTED

- [ ] **6.1 RED migration service compatibility tests.**  
  Scenarios: `PERSIST-S7`, `PERSIST-S8`.  
  Verification: provider service composition, constructor assumptions,
  configurable schema, history ownership, and fixed prefix are asserted.

- [ ] **6.2 GREEN isolate provider generator internals.**  
  Verification: `.Internal` imports exist only in exact migration adapter files
  and focused tests fail on provider service drift.

- [ ] **6.3 RED obsolete model-differ behavior tests.**  
  Scenario: `PERSIST-S7`.  
  Verification: model/snapshot state proves historical scaffold-time backfill
  injection is no longer required after the corrective transition.

- [ ] **6.4 GREEN remove legacy model differ.**  
  Verification: public/generated migration paths own the transition and EF
  internal model-differ imports are gone.

- [ ] **6.5 Generate PostgreSQL corrective migrations.**  
  Verification: EF tooling updates migration and snapshot; no generated line is
  patched manually.

- [ ] **6.6 Generate SQLite corrective migrations.**  
  Verification: `ie_` identifiers, constraints, and snapshot are tool-generated
  and inspected.

- [ ] **6.7 Generate SQL Server corrective migrations.**  
  Verification: configurable schema, constraints, history table, and snapshot
  are tool-generated and inspected.

- [ ] **6.8 Generate MariaDB corrective migrations.**  
  Verification: prefix, identifier length, DDL preflight, constraints, and
  snapshot are tool-generated and inspected.

- [ ] **6.9 Generate MySQL corrective migrations.**  
  Verification: prefix, identifier length, DDL preflight, constraints, and
  snapshot are tool-generated and inspected.

- [ ] **6.10 Generate Data Protection migrations.**  
  Verification: every affected provider-owned Data Protection migration set is
  generated, inspected, and history-isolated.

- [ ] **6.11 Generate retained-authority migrations.**  
  Verification: embedded SQLite and co-located/provider authority targets are
  generated and preserve role/function boundaries.

- [ ] **6.12 Inspect generated Up and Down operations.**  
  Verification: schemas/prefixes, names, data transitions, reversibility,
  destructive operations, and provider SQL are documented without hand edits.

- [ ] **6.13 Run fresh migration lifecycle matrix.**  
  Verification: empty database apply, supported rollback boundary, reapply, and
  no pending model changes pass for every target.

- [ ] **6.14 Complete Phase 6 verification gate.**  
  Verification: migration ownership, architecture, model, lifecycle, and
  generated-artifact checks are green.

## Phase 7: Real-Engine Parity And Quality — NOT STARTED

- [ ] **7.1 Run PostgreSQL behavior matrix.**  
  Verification: Scenarios `PERSIST-S1` through `PERSIST-S10` applicable to
  PostgreSQL are green on the real engine.

- [ ] **7.2 Run SQLite behavior matrix.**  
  Verification: prefix, WAL, locking, mutation, migration, and critical outcome
  scenarios are green on real SQLite.

- [ ] **7.3 Run SQL Server behavior matrix.**  
  Verification: configurable schema, locks, functions, mutations, migrations,
  and critical outcomes are green on real SQL Server.

- [ ] **7.4 Run MariaDB behavior matrix.**  
  Verification: prefix, named locks, identifier limits, constraints, migrations,
  and critical outcomes are green on real MariaDB.

- [ ] **7.5 Run MySQL behavior matrix.**  
  Verification: prefix, named locks, identifier limits, constraints, migrations,
  and critical outcomes are green on real MySQL.

- [ ] **7.6 Compare portable query performance.**  
  Scenario: `PERSIST-S9`.  
  Verification: query count, row count, selected columns, duration, allocation,
  and concurrency throughput meet the recorded envelope or an approved evidence
  exception.

- [ ] **7.7 Eliminate client evaluation and N-plus-one.**  
  Verification: safe EF diagnostics show server translation, intended query
  count, and no loop-driven lazy loading.

- [ ] **7.8 Add bounded persistence mutation configuration.**  
  Verification: owned capability, naming, concurrency, and provider-neutral
  logic is included; migrations and SQL text are excluded.

- [ ] **7.9 Achieve critical mutation score above 85%.**  
  Verification: Stryker evidence reports greater than 85% for the owned critical
  persistence slice.

- [ ] **7.10 Verify zero-sensitive operational evidence.**  
  Scenario: `PERSIST-S10`.  
  Verification: logs, traces, metrics, test output, and saved review evidence
  contain no prohibited sensitive values.

- [ ] **7.11 Complete Phase 7 verification gate.**  
  Verification: five-engine parity, performance, query diagnostics, mutation,
  and sensitive-evidence gates are green.

## Phase 8: Documentation, Release, And Review — NOT STARTED

- [ ] **8.1 Update persistence architecture documentation.**  
  Scope: `docs/ARCHITECTURE.md` and `docs/CODEBASE_INSIGHTS.md`.  
  Verification: capability ladder, naming ownership, provider primitives, and
  exception process match implementation.

- [ ] **8.2 Update testing and operations documentation.**  
  Scope: `docs/TESTING.md` and `docs/OPERATIONS.md`.  
  Verification: focused slices, real-engine matrix, migration lifecycle,
  mutation, and diagnostics are reproducible.

- [ ] **8.3 Update configuration and recovery documentation.**  
  Scope: `docs/CONFIGURATION.md` and `docs/BACKUP_RESTORE_UPGRADE.md`.  
  Verification: provider/schema/prefix behavior, backup, database recreation,
  rollback, and failure recovery are operator-executable.

- [ ] **8.4 Update schema and release artifacts.**  
  Scope: `schemas/islamu-event.md`, `docs/RELEASE_CHECKLIST.md`, and
  `docs/semantic_versioning/CHANGELOG.md`.  
  Verification: generated model, provider evidence, operator actions, and
  release limitations are accurate.

- [ ] **8.5 Confirm API changelog non-applicability.**  
  Verification: `docs/API_CHANGELOG.md` remains unchanged unless an actual
  public contract delta is proven and documented.

- [ ] **8.6 Run anonymized epistemic MAD review.**  
  Verification: architecture, provider/migration, concurrency/financial,
  tenant/privacy, and operations personas vote; blocking findings are resolved.

- [ ] **8.7 Revalidate I-VSD final mappings.**  
  Verification: exact implementation/plan/tasks/context revisions are recorded;
  every `IVSD-PERSIST-*` finding maps to green evidence; status is current and
  disposition is plan-aligned.

- [ ] **8.8 Run final required build and tests.**  
  Verification: Release build and all intent/criticality-owned test projects
  exit zero.

- [ ] **8.9 Run markdown links and diff hygiene.**  
  Verification: documentation links resolve and `git diff --check` exits zero.

- [ ] **8.10 Reconcile final task and context state.**  
  Verification: all completed tasks are checked, context contains final evidence
  and handoff, plan reflects only actual strategy changes, and no stale blocker
  remains.

## Remaining / Deferred Work

None. A new provider, provider extension dependency, removal of configurable
schemas, or replacement of tracked migration history is a separate
Project-Steward decision and not silently deferred into this workstream.

## Synchronization Rules

- `tasks.md` is the only implementation status ledger.
- `context.md` is the only session/handoff/baseline/blocker ledger.
- `plan.md` is the only architecture and phase-design authority.
- The I-VSD report is the provider-responsibility authority.
- Strategy changes update plan first, then tasks/context, then I-VSD.
- Ordinary task completion updates tasks immediately and context at phase
  boundaries or handoff.
