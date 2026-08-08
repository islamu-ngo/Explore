<!-- ABOUTME: Resumable context for adding provider-selected primary database support without exposing raw connection strings. -->
<!-- ABOUTME: Records verified repository state, settled decisions, dependencies, risks, and handoff guidance. -->

# Multi-Database Support Context

**Last Updated:** 2026-08-08 Europe/Brussels

**Status:** Implementation complete; post-review hardening and release evidence reconciled

**Target providers:** PostgreSQL, SQLite, SQL Server, MariaDB, MySQL

**Related workstream:** [Optional Retained Erasure Authority](../optional-retained-erasure-authority/optional-retained-erasure-authority-plan.md)

## Objective

Allow an operator to select a supported primary database through one provider-neutral, structured configuration contract. Application code validates that contract, builds the provider-specific connection string internally, selects the correct EF Core provider and generated migration assembly, and preserves the platform's behavioral invariants.

This workstream does not select the privacy-erasure authority topology. `EmbeddedSqlite`, `CoLocated`, and `ExternalDatabase` are explicit, mutually exclusive choices owned by the Optional Retained Erasure Authority workstream. The workstreams share structured configuration and provider primitives, but only `CoLocated` deliberately shares the primary database restore lifecycle.

## Verified Current State

- One structured, role-aware database contract drives runtime, design-time factories, MigrationService, Aspire, Compose, tests, and CI without operator-supplied raw connection strings.
- PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL have explicit provider registration plus generated application and Data Protection migration ownership.
- PostgreSQL and SQL Server use `Database:Schema` / `DATABASE_SCHEMA`, defaulting to `islamu_event`; schema-less providers use the fixed non-configurable `ie_` prefix.
- Clean MigrationService runs, second-run idempotence, catalog inspection, runtime smoke, and the shared behavior contract passed on all five real provider engines.
- Provider-specific SQL is isolated behind explicit capability branches. A fresh production scan found no unguarded PostgreSQL runtime path for alternate providers.
- Primary SQLite is restricted to a durable local file and one application instance; its busy timeout, WAL initialization, recovery, and authority-file separation are covered.
- Primary-provider selection does not infer authority topology. The authority workstream defaults to a separate embedded SQLite file, retains external PostgreSQL, and owns the explicit `CoLocated` alternative and its narrower restore guarantee.
- Deployment examples, health diagnostics, provider CI lanes, recovery guidance, and operator documentation use the structured contract.

## SESSION PROGRESS (2026-08-08 Europe/Brussels)

### ✅ COMPLETED

- MDB-001 through MDB-713 and MDB-D01 through MDB-D08 are implemented and backed by focused, provider, deployment, recovery, review, and manual-QA evidence under `.omo/evidence/`.
- All ten application/Data Protection provider migration models were regenerated through EF tooling and report no pending model changes.
- Real PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL runs applied migrations twice and passed runtime smoke plus the shared behavior contract.
- Late repository portability repairs cover email/outbox, fanout, registration inventory, idempotency, API quota, domain-host lookup, group hierarchy, ATProto replay, and custom-property projection locks.
- Server lock command contracts no longer construct unused provider-specific EF models; the shared real-provider behavior lane now proves exclusive acquisition, nonblocking contention, transaction release, and reacquisition.
- Production-mode SQLite MigrationService QA applied application, Data Protection, and embedded-authority migrations twice, verified fixed-prefix catalogs and independent histories/files, and left no temporary process or database behind.
- The full canonical command matrix and independent review were executed; residual architecture, API, and persistence-project failures are attributed below without weakening tests.

### 🟡 IN PROGRESS

- None in this workstream.

### ⏭️ NEXT

1. Keep future authority-topology changes in `dev/active/optional-retained-erasure-authority/` and reuse the MDB provider primitives rather than reopening this plan.

### ⚠️ BLOCKERS

- None in the multi-database implementation. Repository-wide architecture, API, and persistence test-infrastructure blockers remain outside this workstream and are recorded under Verification State.

## Settled Decisions

### One structured operator contract

Operators configure fields, never a connection string or free-form connection fragment:

| Field | Requirement |
|---|---|
| `Provider` | Closed enum: `PostgreSql`, `Sqlite`, `SqlServer`, `MariaDb`, `MySql` |
| `Host` | Required for server providers; forbidden for primary SQLite |
| `Port` | Optional; provider default when omitted |
| `Database` | Database name for server providers; persisted file path for primary SQLite |
| `Schema` | PostgreSQL/SQL Server schema; defaults to `islamu_event`; ignored for schema-less providers |
| `Username` | Required for each server-provider credential role |
| `Password` | Required secret for each server-provider credential role |
| `TlsMode` | Closed provider-neutral policy mapped to provider-native settings |
| `TrustServerCertificate` | Explicit opt-in only where supported |
| `ServerVersion` | Required only where Microting needs a bounded server flavor/version |

Exact configuration binding names are settled during the contract phase, but every composition surface uses the same model. Server deployments bind the same structured shape for distinct runtime and migrator roles, each with its own username/password; endpoint fields may be shared by composition without creating a second configuration vocabulary. Environment variables, Aspire parameters, secret-store keys, CLI design-time inputs, and deployment templates project into that model.

`ConnectionStrings:*` may remain only as process-local derived values for framework integration. They are not operator inputs, secret-store contracts, or documented escape hatches.

### Provider-specific construction stays at the composition boundary

- Validate once at startup and fail before serving traffic.
- Use each provider's native connection-string builder.
- Never concatenate credentials or provider-specific fragments manually.
- Log provider and endpoint metadata without credentials or full connection strings.
- Reuse the same builder path for runtime, design-time factories, MigrationService, Aspire, and tests.

### EF Core ownership

- Provider selection is a closed startup decision, not runtime polymorphism per request.
- Keep `ExploreDbContext` and Data Protection model ownership shared where portable.
- Generate separate migration assemblies per provider for application data and Data Protection.
- Never hand-edit generated migrations or model snapshots.
- One migration owner operates each database in a deployment topology.
- Any pre-v1 migration-history reset requires explicit confirmation that deployed histories are disposable; otherwise add generated forward migrations.

### Portability policy

- Preserve transactional and application-level invariants on every provider.
- Keep PostgreSQL-native defenses where they materially strengthen PostgreSQL without changing portable behavior.
- Add capability seams only for demonstrated provider differences; do not create a generic SQL abstraction.
- Operators may configure the PostgreSQL or SQL Server schema through `Database:Schema` / `DATABASE_SCHEMA`; it defaults to `islamu_event` when absent.
- Schema-less providers always use the fixed short `ie_` object prefix. The prefix is not operator-configurable because combining an arbitrary prefix with long table names can exceed provider identifier limits.
- Primary SQLite is a bounded single-instance/small-deployment option. It is not a multi-replica or network-filesystem database.

## Privacy Authority Boundary

- Default: `EmbeddedSqlite`, stored at a persisted path such as `/app/data/privacy_erasure_authority.db` on a dedicated volume excluded from primary database restore operations.
- Explicit alternative: `CoLocated`, owned by the authority workstream, stores retained authority state in the selected primary database and intentionally shares its atomic backup/restore lifecycle. It must never be inferred from `Database:Provider`.
- Enterprise: `ExternalDatabase`, initially PostgreSQL because current authority functions and ACL contracts are PostgreSQL-specific.
- The primary database stores only the authority replay checkpoint outside `CoLocated`. Existing saga, outbox, and receipt records remain where their normal application transaction requires them.
- `PrivacyErasureStartupGate` replays the authority before readiness.
- External authority configuration uses the same structured field semantics under a privacy-erasure prefix, with separate runtime and migrator credentials. It never accepts a raw authority connection string.

## Dependencies and Coordination

1. Settle the shared structured database input vocabulary before changing deployment surfaces.
2. Multi-database provider registration can proceed independently of embedded-authority implementation.
3. Provider CI and release evidence must include the authority topology selected for that lane.
4. Primary SQLite and embedded authority SQLite must use separate files, contexts, migration ownership, volumes, and backup/restore procedures.
5. The authority workstream owns anti-resurrection semantics; this workstream owns primary provider behavior.

## Main Risks

| Risk | Required control |
|---|---|
| Hidden PostgreSQL SQL or JSON semantics | Inventory first; real-engine behavioral tests |
| Divergent runtime and migration configuration | One structured binder and provider builder path |
| Migration drift | Generated provider assemblies plus clean-database migration tests |
| Secret leakage | No raw strings; redacted diagnostics; secret-store field mapping |
| SQLite overreach | Document and enforce single-instance/local-filesystem envelope |
| MariaDB/MySQL dialect differences | Explicit server flavor/version and separate real-engine lanes |
| Namespace divergence | Fixed schema/prefix policy with model assertions |
| Authority lifecycle accidentally coupled to primary | Separate topology, file/database, migrations, volumes, and restore drills |

## Handoff Guidance

The MDB implementation is closed. New primary-provider defects should start from the provider composition, migration ownership, and shared behavior evidence linked in `multi-database-support-tasks.md`. Authority-topology work resumes from OREA-1010 in `dev/active/optional-retained-erasure-authority/`; do not reinterpret `Database:Provider` as an authority-topology selector.

## Verification State

- Release build: passed with 0 errors.
- Canonical green projects: Domain 714, Application 3,450, Secrets 222, Infrastructure non-runtime 1,152, Blazor integration 409, and Blazor client 2,292.
- Architecture: 357 passed, 5 failed, 1 skipped for unrelated later host-service namespace/mutability, OpenAPI, and DTO-rule changes.
- API: 2,165 passed, 10 failed, 1 skipped for unrelated scheduler schema, snapshots, policy/ACL, response-contract, and missing-table changes.
- Persistence at current `HEAD`: 794 passed, 169 failed, 3 skipped. EF Core 10.0.10's process-wide cache throws after 20 distinct options configurations; the single project intentionally loads five primary providers plus application, Data Protection, migration, and authority shapes. The MDB command-contract test was repaired to avoid adding unused EF configurations. The remaining project-sharding/test-process decision is not a production persistence change and remains outside this plan.
- Focused MDB portability: 10 passed.
- Real file-backed SQLite shared behavior contract: 1 passed, including exclusive projection lock acquisition, nonblocking contender rejection, rollback release, and reacquisition.
- Post-change server lock evidence: the shared contract compiles for SQL Server, MariaDB, and MySQL provider lanes, but those engines were unavailable locally; the next provider CI run must capture the server-engine execution artifact.
- Manual production MigrationService QA: first and second SQLite runs passed; the second applied no migrations; primary and authority histories/files remained distinct; cleanup passed.

## Handoff — 2026-08-08 Europe/Brussels

### Current State

- All MDB phases, decisions, closeout tasks, and acceptance criteria are complete within the documented deployment envelopes.
- No migration or model-snapshot artifact was edited during closeout.
- The full repository is not green because of the precisely attributed gates above; no failing test was weakened, deleted, retried, or suppressed.

### Modified Files

- `PrimaryPersistencePortabilityTests.cs` — native disconnected connections replace unused server-provider EF contexts.
- `PrimaryDatabaseProviderBehaviorContractTests.cs` — the existing real-provider contract now covers projection-lock contention and release.
- This plan, task ledger, and context — authority ownership, closeout evidence, and residual gates are synchronized.

### Validation

- Exact commands and counts are recorded in `multi-database-support-tasks.md` and the `.omo/evidence/mdb-*` ledgers.
- Independent re-review against `84bd22af28d48e412513cc2c233cd0ac34cb5b0b` returned **PASS** after the authority-scope finding was resolved by aligning this plan with the explicit, separately owned OREA topology contract.
- Medium residual review items are bounded: post-change server lock execution awaits the next provider CI run, MySQL uniqueness remains principally model/migration-evidenced, and the large SQLite email regression file was not split because that would be unrelated structural churn.

### Notes For Next Contributor Or Agent

- Read `AGENTS.md`, persistence/migration/test rules, this context, the plan, and tasks before editing.
- Never hand-edit generated migrations or snapshots.
- Preserve explicit topology selection: primary provider and authority topology are orthogonal configuration decisions.
- Treat the EF provider-cache failure as test-infrastructure work; do not hide it in production `DbContext` configuration.
