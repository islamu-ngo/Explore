<!-- ABOUTME: Active working context for the EF Core-first persistence hardening workstream. -->
<!-- ABOUTME: Records review revisions, planning outcome, resume state, constraints, baselines, risks, and handoff. -->

# EF Core-First Persistence Hardening — Context

Last Updated: 2026-08-28 Europe/Brussels

## Review State

- **Workstream state:** implementation delivered; Task 8.8 blocked on stale
  repository-wide test baseline
- **User approval:** explicit full-implementation approval recorded from thread
  goal `01a04531-89a1-73d4-803e-b8c163ed6068` on 2026-08-27
- **Plan revision:** SHA-256
  `f0de38888792404f8553384fe773d4fcc5e1b253c48066a33f9032709bb36d11`
- **Tasks revision:** SHA-256
  `0475813a170ef7d22191bd53978132962dc3ebd35c6950042bf3c4717879b091`
- **Evidence revision:** SHA-256
  `f5790b0a6a91a6d2de419598023b08af27f414883f0e049cee2f8bf974311a72`
- **Blast-radius evidence revision:** SHA-256
  `b46de61cad2d8acef3185606b3002dbd560bdff5ff4b536af4f4a499a6110167`
- **Baseline evidence revision:** SHA-256
  `9cff01971f5f0e116432a485dbc1d1b86667d76e21e274cf902c2796bce057a3`
- **I-VSD revision:** SHA-256
  `440403e44d204c745afa4daff6f26bfc0b870a0778ac59d4fe8ca7c17409b0ae`
- **I-VSD status / disposition:** current / plan-aligned
- **CTO review:** not requested
- **Product implementation:** authorized

## Final Verification Reconciliation (2026-08-28 Europe/Brussels)

### Delivered Behavior

- EF Core/LINQ and finalized metadata own ordinary repository persistence,
  physical identifiers, and uniqueness classification.
- Provider-specific SQL is isolated behind approved capability primitives.
- PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL generated initial
  application catalogs plus independent Data Protection and retained-authority
  catalogs are lifecycle-green in the recorded Phase 6/7 evidence.
- The final verification pass additionally fixed:
  - test-only EF provider-cache isolation for five-provider composition;
  - production-faithful PostgreSQL fixture schemas and canonical lookup seeding;
  - metadata-derived constraint/index assertions after naming normalization;
  - development-initial migration tests that still expected deleted history;
  - `EventCustomPropertyProjectionUpdater` user transactions, which now execute
    inside the configured EF execution strategy.

### Green Evidence Captured This Session

- Release solution build: 0 errors.
- Domain: 1,056/1,056.
- Application: 4,776/4,776.
- Infrastructure: 1,640/1,640.
- Persistence mutation contracts: 111/111; recorded Stryker score remains
  91.76%.
- Persistence architecture slices:
  capability boundary 16/16, tenant filters 4/4, migration composition 5/5,
  provider ownership 23/23, event-location contraction 10/10, configuration
  schema generation 8/8, schema artifacts 2/2.
- Real PostgreSQL and provider-focused slices include projection updater 8/8,
  event-location repository 12/12, provider composition 42/42, named locks
  12/12, generated contraction lifecycle 4/4, ATProto baseline 4/4, OAuth
  baseline 2/2, tenant membership removal 3/3, constraint applier 2/2,
  five-provider runtime behavior 3/3, and the affected API uniqueness path 1/1.
- Markdown relative links added by this workstream resolve; the only scanner
  matches were unchanged journal template placeholders. `git diff --check`
  exits zero.

### Active Task 8.8 Blocker

- The full `Event.Persistence.IntegrationTests` process still fails on a broad
  stale-fixture backlog outside the implemented persistence seams. Confirmed
  examples include session/scheduling tests that create `LocationId` and
  `RoomId` without the now-required event-scoped `EventLocationId`, old
  incremental-migration backfill assertions against the approved single
  generated initial catalog, and remaining test-local context builders that
  exhaust EF's internal provider cache only when every provider/model fixture
  shares one process.
- The full architecture result remains the exact inherited baseline:
  528 total, 518 succeeded, 9 failed, 1 skipped. Its failures are admission/API/
  Blazor/agent-context contracts outside this workstream; all persistence-owned
  architecture classes are green.
- The broad API project did not terminate after its known skipped OpenFeature
  shutdown-stress case. The directly affected real-PostgreSQL API path is green.
- No test was skipped, weakened, or converted to a false positive. Task 8.8
  remains unchecked until the repository-wide baseline itself exits zero.

### Handoff

- Resume only at Task 8.8.
- Treat remaining failures as a test-baseline reconciliation workstream:
  update fixtures to construct current valid aggregates and final generated
  catalogs; do not restore historical migrations or compatibility aliases.
- Preserve the verified product implementation and the focused green evidence
  above. Do not reopen Phases 0-7 unless a focused regression proves a product
  defect.

## SESSION PROGRESS (2026-08-27 Europe/Brussels)

### Implementation Authorization

- The active thread goal explicitly directs full implementation of this
  workstream.
- Approval evidence: thread goal
  `01a04531-89a1-73d4-803e-b8c163ed6068`, reconfirmed by the user's
  continuation request on 2026-08-27.
- No persistence product file was edited before this approval was recorded.

### Refreshed Persistence Impact Graph

- Refreshed against the code-review-graph index for `develop` at
  `558a23210522` (59,287 nodes, 1,521,870 edges, 7,819 files).
- Bounded depth-2 impact queries covered schedule, inventory,
  notification/email, webhook/federation, idempotency, payment/refund,
  admission, privacy authority, and provider/migration clusters.
- The graph confirmed cross-cutting dependencies on named tenant-filter
  bypasses, relational locks, `ExploreDbContext.SaveChangesAsync`, provider
  composition, and existing persistence/architecture test seams.
- Reviewer-readable graph evidence:
  [`.omo/evidence/20260827-efcore-first-persistence-hardening/blast-radius.yaml`](../../../.omo/evidence/20260827-efcore-first-persistence-hardening/blast-radius.yaml).
- Broad impact results were noisy and truncated; only extracted seams and
  test relationships were retained. Each implementation task still owns
  focused source and test inspection before editing.

### Implementation Baseline

- Release build:
  `dotnet build --configuration Release --verbosity quiet`
  exited zero with 0 errors and 2,732 inherited warnings in 25.53 seconds.
- Architecture baseline completed with nine inherited failures. They cover
  agent-context size/routes, admission repository/API/Blazor contracts, API
  size and enum registration, HATEOAS permission metadata, DTO naming, and
  tenant-ID OpenAPI leakage. None is owned by this persistence workstream.
- Persistence baseline was rerun with
  `--maximum-parallel-tests 1 --no-progress` after an initial parallel run
  produced EF service-provider-warning cascades.
- The deterministic persistence run reproduced migration-lineage and generated
  schema debt before being stopped once repeated failures added no new root
  category:
  - stale provider-initial test paths;
  - pending model changes and missing generated predecessors;
  - incomplete application, Data Protection, and retained-authority catalogs;
  - provider model/snapshot and schema/prefix divergence;
  - missing historical backfills and semantic preflight/lifecycle failures;
  - more than twenty distinct EF internal service providers in full-project
    context construction;
  - shared PostgreSQL fixture schema fallout after migration setup fails.
- Exact ownership:
  - architecture failures: unrelated branch debt, rechecked only at Phase 8;
  - EF provider/service composition: Tasks 6.1–6.2;
  - model-differ/history drift: Tasks 6.3–6.4;
  - generated provider artifacts and catalogs: Tasks 6.5–6.12;
  - migration apply/rollback/reapply and pending model: Task 6.13;
  - real-engine provider parity: Tasks 7.1–7.5;
  - full green verification: Task 8.8.
- Evidence:
  [test-results.txt](../../../.omo/evidence/20260827-efcore-first-persistence-hardening/test-results.txt).
- Decision: the inherited red baseline is not waived. It is allowed to proceed
  only because source/test owners were unmodified by this workstream, root
  categories are deterministic and phase-owned, and every category remains a
  mandatory owning-phase and final gate.

### Machine-Readable Violation Baseline

- Captured code-only match and owner sets for raw EF APIs, direct ADO markers,
  table/column/index literals, repository provider branches, and provider/EF
  internal imports.
- The baseline excludes Markdown, plans, documentation, generated migration
  bodies, and `obj` output, so source prose cannot satisfy or perturb a rule.
- Exact small owner sets are enumerated. Large naming owner sets are frozen by
  count plus deterministic owner/match SHA-256 fingerprints.
- Current totals:
  - raw EF APIs: 51 matches across 26 owners;
  - direct ADO markers: 97 matches across 12 owners;
  - literal table mappings: 230 across 180 owners;
  - literal column mappings: 83 across 20 owners;
  - literal index names: 432 across 150 owners;
  - repository provider branches: 30 across 17 owners;
  - internal imports: four across two owners.
- Every category maps to exact removal tasks. Evidence:
  [violation-baseline.yaml](../../../.omo/evidence/20260827-efcore-first-persistence-hardening/violation-baseline.yaml),
  SHA-256
  `13021d48d10fc78531be21374801c0145928f504f70e4048538248c54a1ea086`.

### Red Invariant Breakers

- Task 0.5 added prefixed-SQLite move tests for `EventSessionRepository` and
  `EventAgendaItemRepository`.
- The focused class run executed three tests: one passed and both new tests
  failed at the intended physical-name seam.
- SQLite reported missing unprefixed `event_sessions` and
  `event_agenda_items`; the finalized model owns `ie_event_sessions` and
  `ie_event_agenda_items`.
- No corresponding production code has been edited. Evidence:
  [invariant-breaker-results.txt](../../../.omo/evidence/20260827-efcore-first-persistence-hardening/invariant-breaker-results.txt).
- Task 0.6 added an in-memory SQLite mutation control independent of the broken
  generated migration baseline. It proves the named tenant filter and exact
  replacement predicate each return only tenant A, while the predicate-removed
  bypass exposes tenant B and trips the invariant deterministically.
- Task 0.7 mapped existing independent-context race authorities for inventory,
  payment/outbox, admission fencing, idempotency, and erasure ordering. The
  idempotency race no longer relies on a fixed delay: a test-only EF command
  interceptor signals and gates the contender before the winner commits.
- The focused `IdempotencyRepositoryTests` PostgreSQL slice passed 2/2 after
  using non-retrying contexts for the deliberately caller-owned transaction.
  This preserves production retry configuration while proving exactly one
  durable claim owner and one persisted row.
- Task 0.8 added a Red diagnostic-sink invariant around the production
  `AdmissionDeliveryIntentDispatcher`. A synthetic downstream failure carries
  canaries for a SQL parameter value, connection credential, PII, tenant
  payload, and provider response body.
- The focused `AdmissionCompositeDispatchTests` slice executed two tests: the
  existing delivery path passed and the new zero-sensitive test failed at the
  intended seam because `ILogger` received the raw downstream exception and
  exported every canary. No production logging behavior has been changed yet.
- Task 0.9 added a test-only EF command interceptor that records operation
  codes and bounded numeric evidence only. It retains no SQL text, parameter
  names or values, connection strings, or entity identifiers.
- PostgreSQL baselines now cover email and notification paths, inventory hold
  expiry, concurrent webhook claims, and payment reconciliation claims.
  Measurements include command count, maximum projection width, maximum
  parameter count, command duration, wall duration, and result cardinality.
- Evidence:
  [query-baseline.yaml](../../../.omo/evidence/20260827-efcore-first-persistence-hardening/query-baseline.yaml).
- The inventory target passed. Four neighboring broad-class failures were
  classified as inherited transaction/race/schema debt and remain assigned to
  Tasks 4.1–4.2, 6.5, and 7.1 rather than being fixed opportunistically.
- Task 0.10 reconciled every Phase 0 scenario with its executable
  specification, evidence artifact, and exact downstream owner. Inherited red
  categories remain mandatory and are not waived.
- Phase 1 fingerprint enforcement found and corrected two Phase 0 evidence
  defects: table and index owner hashes had not been derived from the exact
  match set. Match hashes and counts were already correct; independent C# and
  POSIX derivations now agree on every physical-name owner fingerprint.
- Phase 1 added synthetic Red probes and shrinking Green gates for raw EF APIs,
  direct ADO, physical names, repository provider branches, and EF internal
  imports. The registry rejects new owners, increased counts, changed source,
  stale entries, duplicate entries, missing bounds, and unknown removal tasks.
- The focused gate passed 10/10. The full architecture suite reproduced the
  exact inherited baseline (512 total, 502 passed, 9 failed, 1 skipped) with
  zero new failures.
- Phase 1 gate evidence:
  [phase-1-gate.yaml](../../../.omo/evidence/20260827-efcore-first-persistence-hardening/phase-1-gate.yaml).
- Task 2.1 added a five-provider finalized naming matrix. It verifies custom
  schemas on PostgreSQL/SQL Server, schema-free `ie_` namespaces elsewhere,
  snake-case tables/columns, and canonical PK/AK/FK/index/check prefixes.
- The matrix is Red across all providers on existing explicit and provider-
  truncated names. A source companion is Red on 230 literal `ToTable` calls.
  Four missing provider migration files remain separately owned by Phase 6.
- Task 2.2 removed eight redundant table literals and nineteen explicit index
  names from actor, RBAC, tenant-user, authentication-token, PII extension, and
  account-authority mappings. Explicit alternate-key names were also returned
  to EF convention ownership.
- The shrinking registry now records 222 table literals and 413 explicit index
  names; its ten focused architecture gates remain green.
- Task 2.2 behavior slices reach the expected EF pending-model guard because
  naming changes intentionally precede generated corrective migrations. All
  nine zero-duration failures originate in fixture migration setup, not
  repository assertions; Phase 6 owns migration regeneration.
- The MySQL/MariaDB identifier limiter now emits canonical lowercase
  `pk_`/`ak_`/`ix_`/`fk_` prefixes and lowercase hash suffixes. This reduced
  each provider matrix from 1,022 violations to 131 without masking explicit
  mappings assigned to later Phase 2 domains.
- Task 2.3 returned core organization, group, membership, position, review,
  PII-extension, and tenant-evidence mappings to convention ownership. Policy
  and setting-owned values remain explicitly deferred to Task 2.9.
- The registry now records 219 table, 74 column, and 408 index literals. All
  ten architecture gates, all five provider model-build cases, and both
  MySQL/MariaDB bounded-identifier cases pass.
- Task 2.4 has normalized the core event aggregate, agenda, day, session,
  event-location, series, category-link, and location mappings. Twenty-six
  explicit index names and two literal tables are gone; semantic temporal,
  privacy, and room-overlap constraints remain, with canonical lowercase names.
- The in-progress registry state is 217 table, 74 column, and 382 index
  literals. Remaining Task 2.4 owners cover event/session custom properties,
  join rows, location audits/lookups/rooms, templates, and publication state.
- The location sub-slice is also normalized: audit history, address/privacy
  lookups, PII coordinates, and rooms retain all tenant, privacy, coordinate,
  capacity, and uniqueness semantics without redundant physical names.
- The latest in-progress registry state is 209 table, 74 column, and 376 index
  literals; both Task 2.4 batches compile with zero errors.
- Task 2.4 is complete across event/session aggregates, agenda/day/location
  scheduling, lookup and junction rows, custom properties, templates,
  moderation, public actions, and publication metadata.
- PostgreSQL proved that four custom-property option tables need two explicit
  canonical FK names each: its 63-character truncation otherwise aliases the
  parent-option and definition relationships. These are nonredundant collision
  guards; all other redundant names in the Task 2.4 scope were removed.
- The registry now records 187 table, 74 column, and 316 index literals.
  Architecture gates pass 10/10; provider model builds and portable location
  contracts pass 5/5 each; bounded MySQL/MariaDB custom-property FKs pass 2/2.
- Task 2.5 is in progress. Admission targets, check-in policy/events/state,
  scanner and recovery capabilities, admission tickets/credentials/delivery,
  and the first registration form/attempt/answer/finalization batch now use
  convention-owned table and index names.
- The in-progress Task 2.5 registry is 163 table, 74 column, and 289 index
  literals. All admission and registration lifecycle checks, computed keys,
  tenant-qualified foreign keys, concurrency stamps, and unique identities
  remain unchanged.
- Task 2.5 is complete across admission, ticket catalogs/types/entitlements,
  capacity and inventory holds, registration orders/participants/PII,
  forms/rules/answers/consent, workflows/requirements, provider bindings and
  effects, submissions/revisions, assignments, and normalized lookups.
- Task 2.5 removed 42 table literals, 16 explicit index names, and one
  redundant shadow-column mapping. The registry now records 121 table, 73
  column, and 273 index literals; monetary snapshots and semantic owned-value
  columns remain explicit.
- Task 2.5 focused verification passes: architecture 10/10, provider models
  5/5, registration forms 3/3, provider foundation 21/21, and admission
  recovery 1/1. The recovery contract now composes the production snake-case
  convention and asserts unique property tuples rather than duplicated
  physical index literals.
- Task 2.6 normalized payment attempts, checkout/reconciliation effects,
  organizer-provider connections, paid policy/governance and acceptance rows,
  disputes, refunds, promotions, contribution settings, and fee policies.
- Three concise organizer-provider FK names remain intentionally explicit:
  they prevent PostgreSQL truncation/collision on self-replacement and account
  operation relationships. The in-progress registry is 92 table, 73 column,
  and 270 index literals.
- Task 2.6 verification passes architecture 10/10, provider models 5/5,
  refund persistence 9/9, paid acceptance 1/1, and 22/24 payment persistence
  cases including monetary constraints, active-slot uniqueness, dispatch,
  reconciliation, stale-fence, and rollback behavior.
- Two payment cases expose an inherited nested-transaction defect:
  `EfCoreUnitOfWork.ExecuteSerializableAsync` opens the outer transaction and
  `RegistrationPaymentAttemptRepository.ClaimAsync` unconditionally opens
  another. Tasks 4.1-4.2 own the failing race/transaction redesign.
- Payment/refund SQLite fixtures now register the same named-lock and
  projection-lock transaction interceptors as production. This fixed an
  indefinite semaphore leak and made the 9/9 refund slice deterministic.
- Task 2.7 normalized notification intents, deliveries, fanout occurrence/run
  state, preferences, email outbox, attempts, receipts, and processor state.
  Convention ownership removed 20 table literals and 61 explicit index names.
  Eleven concise alternate-key or foreign-key names remain where PostgreSQL's
  63-character identifier limit would otherwise truncate or collide.
- Email SQLite fixtures now register both production transaction interceptors.
  This closes leaked named/projection lock semaphores and makes concurrent
  dispatch claims and global SMTP rate admission event-driven and repeatable.
- The Task 2.7 registry records 72 table, 73 column, and 209 index literals.
  Verification passes: build with zero errors, architecture 10/10, provider
  models 5/5, email repository contracts 4/4, and eligibility contracts 5/5.
- Task 2.8 normalized incoming/outgoing webhook inboxes, effect ledgers,
  consumer/provider configuration, local delivery, replay, publication,
  retention, ATProtocol record/projection/Jetstream state, and PDS outbox
  mappings. Shared webhook lookups now derive both tables and indexes from
  entity/DbSet conventions instead of constructor-supplied physical names.
- Task 2.8 removed 26 table literals and 92 explicit index names. The registry
  now records 46 table, 73 column, and 117 index literals. Build, architecture
  gates 10/10, provider models 5/5, webhook ownership metadata 2/2, and the two
  migration-independent webhook lookup tests pass.
- Webhook provider-binding and ATProtocol live-engine slices reach the expected
  pending-model migration guard before assertions. The lookup parity slice's
  only failure likewise finds no current `*_init.cs` migration baseline. These
  are generated-artifact gaps already owned by Phase 6, not model regressions.
- Task 2.9 normalized settings, policy sets, policy-change outbox, typed
  settings documents, theme catalogs, and user appearance mappings. It removed
  11 table literals, 29 convention-equivalent column mappings, and one index
  name while retaining PostgreSQL `xmin`, nested render-policy slots, and all
  theme/palette snapshot prefixes as semantic physical contracts.
- A five-provider metadata contract now checks every `UiThemePalette` property
  across live themes, presets, and user snapshots plus the nested onboarding
  render-policy slot. Task 2.9 verification passes build, architecture 10/10,
  provider models 5/5, and owned semantic-column contracts 5/5.
- The shrinking registry now records 35 table, 44 column, and 116 index
  literals.
- Task 2.10 added a synthetic duplicated-constraint fixture and a repository
  architecture gate. The synthetic probe passes 1/1; the runtime gate fails
  Red on 19 physical key/index/exclusion literals across repository exception
  classifiers, including registration, location, schedule, ticketing,
  notification, webhook, and web-push paths.
- Task 2.11 added `RelationalConstraintDescriptorResolver`, which resolves
  finalized primary-key, unique-index, qualified SQLite-column, and PostgreSQL
  exclusion identifiers from the active EF model. Registration, location,
  schedule, ticketing, notification, webhook, publication, and web-push
  classifiers now select constraints by entity/property metadata rather than
  duplicated physical strings.
- The classifier architecture gate is green 12/12 with zero repository
  physical identifiers. Provider descriptor models pass 5/5, registration
  provider classifiers 6/6, ticketing recognized/unrecognized translations
  2/2, and focused registration revision/narrowness contracts 2/2.
- Task 2.12 returned the final 35 table mappings to DbSet/entity conventions
  across AI, configuration manifests, custom properties, contact sharing,
  moderation, secrets, web push, privacy erasure, and the isolated erasure
  authority. The temporary physical-name registry is now empty.
- Source table ownership passes 1/1, provider models pass 5/5, build has zero
  errors, and the 12-test architecture gate is green. The authority ownership
  model remains green; its minimized-property test has an inherited assertion
  gap because `IsLegalHoldPseudonymized` is mapped but absent from the expected
  property list. Phase 7 owns the full privacy verification surface.
- Phase 2 exits with `.omo/evidence/20260827-efcore-first-persistence-hardening/phase-2-gate.yaml`.
  The Release build has zero errors; naming architecture is green 12/12;
  focused provider, owned-column, constraint-descriptor, webhook, registration,
  and email model contracts are green. The full architecture suite is the
  exact inherited baseline at 514 total, 504 passed, 9 failed, and 1 skipped.
- Pending-model detection correctly reports all five providers as pending.
  The full provider-model class passes 66/75; all nine failures are generated
  migration gaps assigned to Phase 6. No migration or snapshot was hand-edited,
  and diff hygiene is clean.
- Task 3.1 expanded the prefixed-SQLite schedule-move contract to cover session
  and agenda success relationships, exact affected-row failure, ambient-tenant
  isolation, and soft-deleted rows. The Red baseline is 7 total, 1 passed, and
  6 failed because both repositories address unprefixed physical table names.
- Task 3.2 replaced both schedule-move SQL statements with query-filtered
  `ExecuteUpdateAsync` mutations. The active transaction requirement and exact
  one-row conflict contract remain unchanged; ambient tenant and soft-delete
  filters now compose with explicit entity tenant/id predicates, and EF owns
  table/prefix/schema translation.
- Schedule move contracts pass 7/7 and the architecture gate passes 12/12.
  `EventAgendaItemRepository` leaves the raw-EF registry; the one remaining
  `EventSessionRepository` row-lock SQL call is reassigned to Task 5.4.
- Task 3.3 added file-backed SQLite consume/release contention contracts to
  the existing expiry/reconciliation provider matrix. Each transition proves
  exactly one winner, terminal timestamps/status, and whether capacity remains
  allocated or is released. The Red baseline is 9 total, 7 passed, and the two
  physical-table consume/release paths failed as intended.
- Task 3.4 removed the PostgreSQL-only expiry CTE/provider branch and the two
  physical-table consume/release statements. All providers now use conditional
  `ExecuteUpdateAsync`; expiry owns a serializable transaction plus named
  hold lease and atomically transitions the hold before reconciling the owning
  order. Cross-tenant worker calls bypass only the tenant filter and retain
  exact hold/order predicates.
- Inventory portability and contention contracts pass 9/9, build has zero
  errors, and architecture passes 12/12. The repository registry shrank from
  four raw-EF calls to its single pessimistic row lock, reassigned to Task 5.4.
- Task 3.5 added an isolated prefixed-SQLite settlement contract for
  superseded fanout runs. It locks wrong-tenant no-op behavior, exact affected
  rows, nonterminal completion, maximum persisted timestamps, lease clearing,
  and preservation of unrelated terminal evidence. The valid Red fixture fails
  only on the unprefixed `notification_fanout_runs` SQL table.
- Task 3.6 replaced fanout-run settlement SQL with one correlated
  `ExecuteUpdateAsync`. Exact tenant/occurrence predicates and the superseded
  occurrence guard remain server-side; terminal timestamps preserve the
  greatest of settlement, creation, start, and update times; leases are cleared
  only for pending/processing rows.
- The prefixed-SQLite settlement contract passes 1/1, build has zero errors,
  architecture passes 12/12, and `NotificationFanoutOccurrenceRepository`
  leaves the raw-EF registry.
- Task 3.7 revalidated the portable email outbox baseline: repository
  transitions pass 4/4 and eligibility/rate admission passes 5/5. These lock
  claim, suppression, receipt, retry, pause, provider-fence, and hysteresis
  outcomes. The Red implementation seam is the three duplicate PostgreSQL
  processor/global-rate/tenant-control upserts alongside already-green
  transactional portable paths.
- Task 3.8 made the transactional update-then-insert implementations canonical
  for processor pause, global SMTP rate override, and tenant pause control.
  Their execution strategy, transaction, named lock, timestamps, and returned
  persisted entities are unchanged; three PostgreSQL `ON CONFLICT` branches
  and physical names are gone.
- Email repository transitions pass 4/4, eligibility passes 5/5, build has zero
  errors, and architecture passes 12/12. The raw-EF registry retains only the
  stale-claim `FromSql` path assigned to Task 4.8; provider branches shrink
  from 30 to 27.
- Task 3.9 added an architecture invariant that requires incoming-effect,
  incoming-message, bulk-replay, local-target, and provider-publication
  repositories to delegate advisory locks to the shared provider boundary.
  Its Red run reports exactly those five repository owners while existing
  claim/lease/tenant outcome suites remain the behavioral parity baseline.
- Task 3.10 routed all five webhook advisory locks through
  `RelationalNamedLock.AcquireTransactionAsync`, preserving transaction-scoped
  keys while making PostgreSQL, SQL Server, MySQL/MariaDB, and SQLite lifecycle
  semantics boundary-owned. The bulk-replay `SKIP LOCKED` query remains as the
  measured queue primitive assigned to Task 4.6.
- Build has zero errors and architecture passes 13/13. Five raw-EF entries are
  removed and provider-branch fingerprints shrink from 27 calls/17 owners to
  22 calls/13 owners.
- Task 3.11 added the equivalent ATProtocol advisory-lock boundary invariant.
  Its Red run identifies the Jetstream consumer and PDS synchronization outbox
  repositories while existing fence, tombstone, claim, and snapshot tests keep
  their persistence outcomes fixed.
- Task 3.12 moved Jetstream consumer and PDS outbox claim locks to
  `RelationalNamedLock`, preserving keys and transaction ownership across all
  providers. The Jetstream commit-time fence self-update remains the isolated
  queue/fence primitive assigned to Task 4.6.
- Build has zero errors and architecture passes 14/14. Raw-EF ownership shrinks
  by two calls; provider branches shrink to 20 calls across 12 owners.
- Task 3.13 reuses the independent-context PostgreSQL insertion race hardened
  in Task 0.7: one uncommitted owner blocks the contender, the exact commit
  signal releases it, one owner/one existing result emerges, and exactly one
  durable key remains. The raw multi-provider upsert is the intended Red seam.
- Task 3.14 replaced the provider SQL generator/upsert matrix with EF add,
  conditional expired-row delete, and bounded unique-conflict recovery.
  Current owners are returned without mutation; expired keys can be replaced;
  provider-native unique violations converge on the durable winner.
- New file-backed SQLite tests pass 2/2 for deterministic two-context
  contention, exact one-row durability, expired replacement, and active replay.
  Build has zero errors, architecture passes 14/14, and
  `IdempotencyRepository` leaves the raw-EF registry. The PostgreSQL fixture
  test remains deferred only by Phase 6 pending migrations.
- Task 3.15 retains the authority counter/append, contiguous replay, retention,
  legal-hold pseudonymization, and authority-first ordering suites as
  Invariant-Breakers. They identify six ordinary counter/row SQL calls in the
  co-located and embedded repositories; SECURITY DEFINER calls are explicitly
  outside this conversion and remain assigned to Task 5.7.
- Task 3.16 replaced co-located counter initialization/row locks with a
  provider-neutral transaction named lock plus tracked singleton creation.
  Embedded SQLite uses one process-wide writer semaphore across repository
  instances. Both compaction paths now invoke the domain-owned
  `PseudonymizeForLegalHold` transition instead of physical UPDATE strings.
- Legal-hold domain invariants pass 1/1, both repositories have zero ordinary
  raw-EF calls, build has zero errors, and architecture passes 14/14. The
  retained SECURITY DEFINER repository remains untouched for Task 5.7.
- Task 3.17 removed or reassigned every completed Phase 3 registry owner.
  Event-day pessimistic locking is owned by Task 5.4; registration and email
  lock primitives by Task 4.8; no `removalTask: 3.*` entry remains. The
  reconciled architecture gate passes 14/14 and diff hygiene is clean.
- Task 3.18 captured `.omo/evidence/20260827-efcore-first-persistence-hardening/phase-3-gate.yaml`.
  The full 1,406-test persistence observation exposed the expected Phase 6
  migration cascade, two Phase 4 financial transaction failures, and retained
  authority assertions. It also found and drove two immediate corrections:
  legal-hold pseudonymization no longer mutates an EF alternate key, and all
  five provider models now use collision-safe canonical index/check names.
- Phase 3 owning slices are green: 30/30 repository/domain behaviors, 14/14
  architecture boundaries, and 5/5 finalized provider naming cases. The final
  full-project green requirement remains assigned to Task 8.8 after generated
  artifacts and later-phase invariants are completed.
- Task 4.1 adds event-released, independent-context payment races for attempt
  creation, reconciliation claiming, and checkout completion. Attempt and
  completion races already preserve one durable attempt/effect; reconciliation
  deterministically exposes SQLite lock contention. The existing real
  PostgreSQL refund-capacity race remains green 2/2. The full payment class
  records 24/27 green with the reconciliation race and two caller-owned
  transaction failures as Task 4.2's Red baseline.
- Task 4.2 makes payment-attempt creation transaction-composable: repository
  calls join a caller-owned serializable unit of work or create one only for
  direct use. Non-PostgreSQL reconciliation claims now use a conditional,
  batch-token EF update and reload, so stale workers receive no claim without
  tracked-entity lock contention. Refund capacity uses the shared
  `RelationalNamedLock` capability on every provider instead of repository
  `FOR UPDATE` SQL.
- The complete payment persistence class passes 27/27, the real PostgreSQL
  refund race passes 2/2, and the persistence architecture boundary passes
  14/14 with the refund raw-SQL registry entry removed.
- Task 4.3 maps the existing event-driven PostgreSQL admission Invariant
  Breakers into the required matrix: issuance versus duplicate issuance,
  cancellation/refund revocation versus loaded issuance, duplicate and
  undo/check-in races, one-time scanner capability issuance, and target-stop
  races against both scans and scanner issuance. Every case subscribes to the
  exact lock/command event before release and uses a bounded cancellation
  token; there are no timing sleeps. Execution remains intentionally assigned
  to Phase 7 after Phase 6 restores the shared PostgreSQL migration fixture.
- Task 4.4 replaces SQLite's synthetic row-lock SQL with filtered
  `ExecuteUpdateAsync` and changes every admission caller to pass a mapped
  property expression. The retained external-engine pessimistic primitive now
  derives table, schema, tenant column, and key column from EF metadata.
- Admission target materialization now acquires one transaction-scoped named
  lock per tenant/event, correctly fencing empty-set concurrent inserts without
  repository provider branches or physical SQL. Its focused contract passes
  1/1; architecture passes 14/14. The primitive's single unavoidable
  provider-lock command is explicitly assigned to Task 5.4.
- Task 4.5 adds a deterministic four-worker, four-row reconciliation queue
  barrier after candidate selection and before conditional mutation. It proves
  duplicate prevention but fails the no-starvation contract because all
  workers select the oldest row and three return empty rather than advancing.
  Existing contracts retain the 1,000-row PostgreSQL throughput budget and
  expired-lease recovery evidence. This is Task 4.6's Red seam.
- Task 4.6 keeps PostgreSQL's measured one-command `SKIP LOCKED` reconciliation
  claim and uses a bounded, collision-retrying conditional EF update for other
  providers. Under exact four-way contention each worker advances to the next
  due row, all four rows are claimed once, and the full payment class passes
  28/28.
- Webhook bulk replay retains `SKIP LOCKED` because its caller deliberately
  holds the selected operation through a transaction while scheduling targets;
  the primitive is assigned to Task 5.4. Jetstream's remaining raw call is a
  database-clock commit fence, not a queue claim, and is assigned to Task 4.10.
- Task 4.7 adds real-engine named-lock lifecycle contracts for contention,
  transaction commit, rollback, cancellation, and session-lease disposal.
  PostgreSQL plus refund races pass 3/3; SQLite/provider-command lifecycle
  passes 12/12; SQL Server and MySQL pass. MariaDB deterministically rejects
  the shared `GET_LOCK(..., -1)` timeout with a NULL result, establishing the
  Task 4.8 Red provider-semantics seam.
- Task 4.8 routes registration fulfillment, finalization, submission-write,
  subscription-state, and email eligibility coordination through
  `RelationalNamedLock`. Email processor/tenant controls now use one
  lock-protected tracked EF get-or-create path on every provider; five
  PostgreSQL-only raw lock/upsert calls are removed.
- MariaDB/MySQL use a cancellation-controlled positive `GET_LOCK` timeout
  because MariaDB maps `-1` to NULL. Real SQL Server, MariaDB, and MySQL
  lifecycle contracts now pass 3/3; PostgreSQL passes 1/1 and SQLite/provider
  command contracts pass 12/12. Email eligibility and registration
  finalization each pass 5/5, architecture passes 14/14, and provider branches
  shrink from 17/11 to 11/8.
- Task 4.9 adds a runtime SMTP rate-authority contract with an application
  evaluation time fixed decades in the past. The persisted processor timestamp
  is proven to come from SQLite's database clock within the bounded operation
  interval, not from caller time. Together with the five-provider SQL selector
  matrix, this reserves database time for cross-worker rate/lease authority;
  ordinary repository timestamps remain covered by injected `TimeProvider`
  contracts.
- Task 4.10 moves the sole scalar database-clock read into
  `Schema/ProviderPrimitives/RelationalDatabaseClock`; repositories and
  services no longer own provider clock SQL or date-kind normalization.
  Email's organizer-authority and persisted SMTP rate decisions call this
  capability. Jetstream's PostgreSQL clock check remains an atomic conditional
  commit fence, not an avoidable scalar read, and is assigned to Task 5.10.
- The five-provider selector plus SQLite runtime suite passes 6/6,
  architecture passes 14/14, and diff hygiene is clean.
- Task 4.11 captures `.omo/evidence/20260827-efcore-first-persistence-hardening/phase-4-gate.yaml`.
  Release build has zero errors; payment/queue passes 28/28, PostgreSQL
  refund/lock passes 3/3, SQLite lock contracts pass 12/12, and SQL Server,
  MariaDB, and MySQL lifecycle plus row fences pass 6/6. Four workers drain
  four due rows with no duplicates or starvation.
- The PostgreSQL admission matrix remains authored but intentionally executes
  in Phase 7 after generated migrations restore its shared fixture. No
  migration or snapshot file was hand-edited.
- Task 5.1 adds a final non-registry architecture gate across persistence
  repositories, services, and retained-authority repositories. It directly
  rejects raw EF, direct ADO, provider branches, and physical mapping names,
  requiring all such capabilities to move behind approved primitive types.
  The focused gate fails deterministically against the remaining Phase 5
  exceptions and becomes the Green acceptance criterion for Task 5.10.
- Task 5.2 adds `RelationalProviderClassifier` as the single project-owned
  mapping from EF package provider names to PostgreSQL, SQLite, SQL Server, and
  MySQL capabilities. All 11 repository/service `ProviderName` and public
  provider-extension branches now compare the typed classification; the
  temporary provider-branch seam is empty and removed from the registry.
- Persistence and architecture projects build with zero errors, and the
  remaining internal-seam fingerprint test passes 1/1.
- Task 5.3 moves named-lock acquisition/release and transaction interceptors
  under `Database/ProviderPrimitives`. Projection shared/exclusive try-locks
  now delegate to `RelationalProjectionLock`, which exclusively owns
  PostgreSQL advisory, SQL Server application-lock, and MariaDB/MySQL
  `GET_LOCK` commands.
- Source scanning finds every advisory/application lock command only under the
  approved primitive path. Named-lock tests pass 12/12, projection portability
  passes 10/10, and the direct-ADO registry gate passes 1/1.
- Task 5.4 moves `RelationalEntityRowFence` under approved provider primitives;
  event-day, session, admission, and sorted inventory callers pass direct
  mapped property expressions. `RelationalSkipLockedQuery` now owns the two
  measured email-recovery and webhook-replay queue reads and resolves every
  table/column through finalized EF metadata and provider delimiters.
- No repository retains pessimistic SQL. Schedule passes 7/7, inventory passes
  9/9, the raw-EF registry gate passes 1/1, and the unchanged real external
  row-fence lifecycle remains green 3/3 providers from the Phase 4 gate.
- Task 5.5 moves `SqliteDatabaseInitializer` into the approved database
  primitive path. The WAL PRAGMA remains one idempotent, provider-gated command
  invoked only after schema creation/migration; no ordinary repository can own
  SQLite initialization SQL.
- Explore.Persistence builds with zero errors and the raw-EF registry gate
  remains green 1/1.
- Task 5.6 moves `PostgresTenantSessionInterceptor` under the approved security
  primitive path and expands the bounded primitive-prefix contract
  accordingly. The `set_config` command and parameter binding remain isolated
  from DbContext/repository code.
- Explore.Persistence builds with zero errors and the direct-ADO registry gate
  passes 1/1. Real forced-RLS pooled-connection execution remains assigned to
  Phase 7 after generated PostgreSQL migrations restore the shared fixture.
- Task 5.7 moves the PostgreSQL SECURITY DEFINER adapter plus embedded SQLite
  connection/storage commands under the approved retained-authority primitive
  path. Runtime repositories retain only ordinary tracked EF state changes;
  function-only role boundaries remain explicit in the adapter.
- Explore.Persistence builds with zero errors, embedded legal-hold recovery
  passes 1/1, composition passes 9/11, and the two expected failures are solely
  Phase 6 pending-model migrations. The direct-ADO registry gate passes 1/1.
- Task 5.8 moves `ExploreDatabaseMigrator`,
  `PostgresModelConstraintApplier`, and
  `SemanticValueConstraintMigrationPreflight` under approved schema provider
  primitives. MariaDB/MySQL preflight remains metadata-bounded, read-only,
  parameterized, and free of row values/PII.
- Persistence integration builds with zero errors; raw-EF and direct-ADO gates
  each pass 1/1. Both SQLite topology cases reach only the known Phase 6
  pending-model guard, proving composition while deferring lifecycle execution.
- Task 5.9 completes the property-expression boundary: all row-fence callers
  pass direct mapped CLR properties, and both retained queue primitives resolve
  their status/order/key columns from EF metadata. No caller supplies a table,
  schema, or physical column string.
- A new architecture regression scans every row-fence invocation and passes
  1/1, rejecting any future quoted physical-column argument.
- Task 5.10 empties both raw-EF and direct-ADO repository registries.
  Provider-optimized email and notification adapters, PostgreSQL payment
  reconciliation, and the metadata-derived Jetstream commit fence now live
  only under approved database primitive paths. The raw scanner now also
  detects generic `SqlQuery<T>` invocations.
- The final non-registry provider-boundary suite passes 16/16 and payment
  reconciliation passes 28/28. Notification's 17-case shared-engine class
  reaches only the known Phase 6 pending-model guard.
- Task 5.11 captures `.omo/evidence/20260827-efcore-first-persistence-hardening/phase-5-gate.yaml`.
  Release build has zero errors; repository raw EF, direct ADO, physical-name,
  and provider-branch registries are empty. Capability gates remain green
  across locks, projection coordination, schedule/inventory fences, payment,
  and embedded authority recovery.
- Only four generated-provider internal imports across two owners remain; both
  are explicitly assigned to Tasks 6.2 and 6.4. Diff hygiene is clean and no
  migration/snapshot artifact was hand-edited.
- Task 6.1 adds a five-provider migration service compatibility matrix. It
  resolves `IMigrationsSqlGenerator`, `IHistoryRepository`, and
  `IMigrationsAssembly`, then pins the project-owned adapter type and public
  constructor shape. PostgreSQL passes; SQLite, SQL Server, MariaDB, and MySQL
  deterministically expose absent provider migration assemblies in the
  architecture harness, establishing Task 6.2's Red composition seam.
- Task 6.2 moves configurable provider SQL generators under approved schema
  primitives and adds all four external migration assemblies to the
  architecture compatibility harness. Internal provider option imports are now
  excluded only for that exact adapter path; the temporary seam shrinks to the
  legacy model differ's two imports in one owner.
- All five migration generators, history repositories, migration assemblies,
  and constructor shapes resolve 5/5. The remaining internal fingerprint
  passes 1/1 and is owned solely by Task 6.4.
- Task 6.3 adds a five-provider service contract requiring the provider's
  standard model differ rather than project-owned scaffold-time SQL injection.
  All 5/5 cases fail against `ApplicationMigrationsModelDiffer`, proving the
  historical snapshot/backfill adapter remains active before Task 6.4.
- Task 6.4 deletes `ApplicationMigrationsModelDiffer` and removes its service
  replacement. All five providers now resolve their standard EF model differ;
  corrective generated migrations, rather than scaffold-time injected SQL,
  own the transition.
- The provider model-differ matrix passes 5/5, the internal seam registry is
  empty, and its runtime fingerprint gate passes 1/1.
- Tasks 6.5 through 6.9 first generated provider corrections, but fresh
  execution exposed historical snapshot/physical-schema divergence. Those
  unapplied development corrections and their invalid predecessor chains were
  removed through EF tooling; the authoritative application artifacts are the
  five generated `InitialApplication` migrations recorded under Task 6.13.
- Task 6.10 scaffolded all five Data Protection targets to test ownership.
  Every generated `Up`/`Down` was empty and every snapshot unchanged, proving
  the hardening model does not affect Data Protection. The empty development
  migrations were removed with `dotnet ef migrations remove --force` rather
  than hand-deleted. Pending-model detection reports no changes for all five
  Data Protection providers.
- Task 6.11 generated the standalone PostgreSQL
  `20260828045714_EfCoreFirstPersistenceHardeningAuthority`, co-located
  PostgreSQL `20260828045723_EfCoreFirstPersistenceHardeningAuthority`, and
  embedded SQLite
  `20260828031242_EfCoreFirstPersistenceHardeningAuthority` migrations and
  snapshots. All three context projects build cleanly and report no pending
  model changes; role/function and embedded history ownership remain separate.
- Task 6.13 found that convention pluralization had renamed the PostgreSQL
  authority counter table while function SQL still targeted its singular
  contract name. `PrivacyErasureAuthorityDatabaseContract.CounterTable` is now
  the model and SQL source of truth; both PostgreSQL authority corrections were
  regenerated through EF and pass their real lifecycle tests.
- Task 6.12 originally recorded generated correction counts in
  `.omo/evidence/20260827-efcore-first-persistence-hardening/phase-6-generated-inspection.yaml`.
  That evidence is explicitly superseded for application providers; retained-
  authority inspection remains valid. The authoritative application operation
  and lifecycle evidence is now `phase-6-rebaseline-lifecycle.yaml`.
- Task 6.13 fresh-database execution invalidated the operation-only Task 6.12
  conclusion for application catalogs. The PostgreSQL snapshot claimed an
  index already dropped by an earlier migration, omitted tables never emitted
  by any migration, and retained alternate-key names whose dependent foreign
  keys EF did not schedule for replacement. The generated correction therefore
  failed successively on a dependent key and nonexistent index.
- Because the application is explicitly in development with no backward-
  compatibility requirement, Task 6.13 is rebaselining each application
  provider catalog through `dotnet ef migrations remove --force`, followed by
  one generated `InitialApplication`. Data Protection and retained-authority
  histories remain independent and are not being reset. Task 6.12 evidence is
  superseded for application migrations until the new initials are inspected.
- PostgreSQL now owns generated migration
  `20260828035010_InitialApplication`; pending-model detection is clean and
  `GeneratedInitMigrationBehaviorTests` passes 6/6, including empty apply,
  rollback to zero, reapply, lookup seeding, Data Protection ownership, and
  retained-authority isolation.
- The external generated initials are SQLite
  `20260828040252_InitialApplication`, SQL Server
  `20260828040310_InitialApplication`, MariaDB
  `20260828040320_InitialApplication`, and MySQL
  `20260828040329_InitialApplication`.
- The five-provider model/snapshot matrix passes 75/75 and configuration-
  manifest migration ownership passes 5/5 against the rebaselined catalogs.
- Task 6.13 passes apply, rollback-to-zero or the supported authority boundary,
  reapply, and pending-model checks for all five application catalogs, all
  five Data Protection catalogs, and all three retained-authority topologies.
  Real PostgreSQL, SQL Server, MariaDB, MySQL, and SQLite lifecycle evidence is
  recorded in `phase-6-rebaseline-lifecycle.yaml`.
- Task 6.14 closes Phase 6 with 64/64 architecture checks, 105/105
  model/catalog checks, all 13 migration targets lifecycle-green, zero pending
  models, a zero-error Release build, and clean diff hygiene. The aggregate is
  `.omo/evidence/20260827-efcore-first-persistence-hardening/phase-6-gate.yaml`.
- Phase 0 gate evidence:
  [phase-0-gate.yaml](../../../.omo/evidence/20260827-efcore-first-persistence-hardening/phase-0-gate.yaml).

### Planning Outcome

- Classified the work under `update-repository-query` with
  `add-ef-migration` and Tier 0 payment/inventory/admission plus Tier 2
  privacy/tenant overlays.
- Revalidated the persistence audit baseline:
  - 51 EF raw-SQL sites across 26 files;
  - 24 direct ADO markers across nine files;
  - 228 literal table mappings;
  - 79 literal column mappings;
  - 428 literal index names;
  - four provider/EF `.Internal` imports.
- Confirmed the solution uses one ORM, EF Core, with five provider modes.
- Confirmed configurable schemas and schema-less provider prefixes are public
  operator contracts and remain supported.
- Confirmed no overlapping active persistence-hardening workstream exists.
- Bound the completed Registration Data Collection workstream as critical-domain
  authority.
- Bound the plan-blocked Event Ticketing Lifecycle workstream as a downstream
  consumer of these guardrails.
- Researched official EF Core, Npgsql, PostgreSQL, naming-convention, and
  Microting behavior.
- Wrote the evidence packet, implementation plan, tasks ledger, context, and
  plan-aligned planning-mode I-VSD report.
- Made no runtime, migration, test, configuration, or existing workstream edit.

### Planning Decisions

- Retain PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL.
- Retain configurable schemas for PostgreSQL/SQL Server and `ie_` prefixes for
  schema-less providers.
- Retain `EFCore.NamingConventions` as the physical naming authority.
- Enforce EF Core/LINQ first, provider public APIs second, and isolated
  parameterized SQL last.
- Remove ordinary SQL and provider branching from repositories.
- Preserve critical concurrency/transaction/outbox/erasure behavior through
  Red-before-Green invariant tests.
- Keep generated migration ownership and do not hand-edit tracked migration
  history.
- Permit disposable development database recreation only with operator
  approval.
- Isolate unavoidable provider-internal migration constructor coupling and
  package-version test it.
- Add no new dependency.

## Quick Resume

1. Read this context file.
2. Read the first unchecked task in
   [`efcore-first-persistence-hardening-tasks.md`](efcore-first-persistence-hardening-tasks.md).
3. Continue at Task 8.8 and run the final required build and tests.
4. Retrieve only the plan section named by the active task.
5. Before product edits, establish
   the clean baseline once.
6. Do not start with global search-and-replace, migration generation, or SQL
   deletion.

## Key Files And Responsibilities

| File or area | Responsibility |
| --- | --- |
| `efcore-first-persistence-hardening-plan.md` | Behavior contract, architecture, phases, risk, release, and rollback |
| `efcore-first-persistence-hardening-tasks.md` | Sole granular execution/status ledger |
| `efcore-first-persistence-hardening-evidence.md` | Shared repository and external research evidence |
| `islamic-value-sensitive-design/i-vsd-efcore-first-persistence-hardening.md` | Provider-responsibility findings and plan alignment |
| `src/Explore.Persistence/Repositories/` | Domain-facing repository implementations to normalize |
| `src/Explore.Persistence/Configurations/` | Convention and semantic model mappings |
| `src/Explore.Persistence/Database/` | Provider composition, locks, row fences, and database primitives |
| `src/Explore.Persistence/Schema/` | Model namespace, provider constraints, migration extensions, and identifier policy |
| `src/Explore.Persistence/Privacy/ErasureAuthority/` | Retained authority models, functions, and storage boundaries |
| `tests/Event.Architecture.Tests/` | Capability ladder, naming, filters, migration, and Clean Architecture gates |
| `tests/Event.Persistence.IntegrationTests/` | Real model, repository, provider, migration, and concurrency evidence |

## Key Decisions

### Capability Ladder

Native EF Core and LINQ are mandatory first. Public provider APIs/translations
are second. SQL is allowed only through approved provider primitives after a
capability gap is proven.

### Naming

Snake-case conventions own ordinary physical names. Redundant `ToTable`,
`HasColumnName`, `HasDatabaseName`, and constraint strings are removed.
Semantic owned-value flattening and provider constraints remain explicit only
when conventions cannot express them.

### Concurrency

Use conditional set-based mutation, concurrency tokens, unique constraints,
transactions, and appropriate isolation before pessimistic SQL. Retain
provider lock or `SKIP LOCKED` behavior only with race and performance evidence.

### Migrations

Fix model/configuration first, generate every affected provider artifact, and
never patch generated output. Existing merged history remains intact.
Development databases are disposable only through an explicit operator action.

### Critical Invariants

Payment/refund state remains monotonic; inventory cannot oversell; admission
authority cannot duplicate; outboxes remain transactionally paired; tenant
filters fail closed; erasure remains authority-first and anti-resurrection
fenced; telemetry remains zero-PII.

## Constraints And Rules To Remember

- Repositories return entities and never expose `IQueryable`.
- Complex reads use the established specification pattern.
- Context pooling keeps scoped tenant/current-user dependencies property-injected.
- Named tenant and soft-delete filters remain active by default.
- Cross-tenant operations use exact named bypass reasons and exact tenant
  predicates.
- Migration and snapshots are generated artifacts.
- No new ORM, micro-ORM, provider extension, compatibility shim, or hand-written
  migration.
- No secret, connection string, PII, payment payload, erasure subject, or SQL
  parameter value in logs or evidence.
- Fixed sleeps and timing-luck polling are forbidden in concurrency tests.
- Architecture gates must be capable of failing on a synthetic regression.
- Real-engine evidence is required for provider claims.
- Final Tier 0–2 review requires mutation evidence above 85% for owned critical
  persistence logic and anonymized MAD review.

## Validation Baseline

### Planning Validation

- Repository evidence and official documentation inspected.
- Plan paths, test projects, provider migration projects, documentation targets,
  and package versions verified.
- Runtime build/tests intentionally not run because this turn changed planning
  markdown only.
- Triad separation, all ten scenario mappings, all five I-VSD finding mappings,
  exact revision bindings, relative links, and diff whitespace are green.

### Implementation Baseline

Established with a green Release build and inherited deterministic test
failures mapped to Tasks 6.1–7.5 and 8.8. See the session baseline section and
reviewer-readable test evidence.

## Current Known Risks / Unknowns

### Risks

- SQL paths combining lock, mutation, and returned identifiers may need a
  retained provider primitive after native concurrency design is measured.
- Naming cleanup may generate destructive provider migration operations.
- Microting translation and migration behavior may differ materially from
  Npgsql/SQL Server.
- Npgsql's public migration generator constructor currently depends on an
  internal provider options contract.
- A temporary raw-SQL registry could become permanent unless every entry is
  phase-owned and removed.

### Unknowns

No unknown changes scope, architecture, phase ordering, or verification.
Performance envelopes and the final approved SQL exception set are
implementation evidence, not deferred planning decisions.

## Related Workstreams

- `dev/active/registration-data-collection/`: complete behavioral authority;
  do not edit for this workstream.
- `dev/active/event-ticketing-lifecycle/`: plan-blocked downstream consumer;
  do not begin its persistence implementation before the guardrail phase is
  available.

## Handoff Notes

**Current workstream:** implementation approved; Phase 0 baseline work in
progress.

**Next owner:** implementation agent.

**Start at:** Task 0.4 in the tasks ledger.

**Do not start with:** migration generation, repository-wide replacements,
deleting SQL, package changes, or developer database recreation.

**First implementation evidence:** approval revision, refreshed impact graph,
classified inherited baseline, and failing provider/invariant tests.

**Planning docs changed:** yes; new task-owned evidence, plan, tasks, context,
and I-VSD report only.
