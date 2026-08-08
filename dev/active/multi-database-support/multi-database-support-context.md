<!-- ABOUTME: Resumable context for adding provider-selected primary database support without exposing raw connection strings. -->
<!-- ABOUTME: Records verified repository state, settled decisions, dependencies, risks, and handoff guidance. -->

# Multi-Database Support Context

**Last Updated:** 2026-08-05 Europe/Brussels

**Status:** Implementation complete; final verification command lane executed, release evidence linked

**Target providers:** PostgreSQL, SQLite, SQL Server, MariaDB, MySQL

**Related workstream:** [Optional Retained Erasure Authority](../optional-retained-erasure-authority/optional-retained-erasure-authority-plan.md)

## Objective

Allow an operator to select a supported primary database through one provider-neutral, structured configuration contract. Application code validates that contract, builds the provider-specific connection string internally, selects the correct EF Core provider and generated migration assembly, and preserves the platform's behavioral invariants.

This workstream does not make the privacy erasure authority use the selected primary provider. Its default authority is a restore-isolated embedded SQLite file; its enterprise option remains a separate PostgreSQL database. The two workstreams share configuration conventions and release evidence but have independent storage lifecycles.

## Verified Current State

- One structured, role-aware database contract drives runtime, design-time factories, MigrationService, Aspire, Compose, tests, and CI without operator-supplied raw connection strings.
- PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL have explicit provider registration plus generated application and Data Protection migration ownership.
- PostgreSQL and SQL Server use `Database:Schema` / `DATABASE_SCHEMA`, defaulting to `islamu_event`; schema-less providers use the fixed non-configurable `ie_` prefix.
- Clean MigrationService runs, second-run idempotence, catalog inspection, runtime smoke, and the shared behavior contract passed on all five real provider engines.
- Provider-specific SQL is isolated behind explicit capability branches. A fresh production scan found no unguarded PostgreSQL runtime path for alternate providers.
- Primary SQLite is restricted to a durable local file and one application instance; its busy timeout, WAL initialization, recovery, and authority-file separation are covered.
- The privacy-erasure authority defaults to a separate embedded SQLite file and retains the external PostgreSQL enterprise topology.
- Deployment examples, health diagnostics, provider CI lanes, recovery guidance, and operator documentation use the structured contract.

## SESSION PROGRESS (2026-08-02 Europe/Brussels)

### ✅ COMPLETED

- MDB-001 through MDB-708 and MDB-D01 through MDB-D07 are implemented and backed by focused, provider, deployment, and recovery evidence under `.omo/evidence/`.
- All ten application/Data Protection provider migration models were regenerated through EF tooling and report no pending model changes.
- Real PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL runs applied migrations twice and passed runtime smoke plus the shared behavior contract.
- Late repository portability repairs cover email/outbox, fanout, registration inventory, idempotency, API quota, domain-host lookup, group hierarchy, ATProto replay, and custom-property projection locks.

### 🟡 IN PROGRESS

- MDB-709 and MDB-710: canonical Release build, all nine required project test commands, and release-evidence closure.

### ⏭️ NEXT

1. Obtain independent change and gate review when available (the active handoff currently has none).

### ⚠️ BLOCKERS

- None in the multi-database implementation. Final repository gates may still expose unrelated concurrent worktree failures; record them precisely without weakening tests.

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
- Enterprise: `ExternalDatabase`, initially PostgreSQL because current authority functions and ACL contracts are PostgreSQL-specific.
- The primary database stores only the authority replay checkpoint for authority state. Existing saga, outbox, and receipt records remain where their normal application transaction requires them.
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

Start with `multi-database-support-tasks.md`, then inspect executor session `ses_03c96c432ffeACCq72heVpiQ23` and the actual worktree diff. Parent orchestration session `ses_03c9767feffeWmWkC0034LJuks` did not receive a DoneClaim; reviewer session `ses_03c886502ffemjGbc7l0bXWRFR` produced a checklist only, not a verdict. Do not begin Phase 2 until Phase 1 is independently accepted. Update tasks immediately, context when evidence or decisions change, and the plan only when sequencing or architecture changes.

## Verification Constraint

The original 13-error baseline is resolved and the exact Release build has passed. The latest candidate evidence is incomplete: Secrets tests passed 205/205, the final focused persistence test result is missing, the full Persistence suite is red for apparently unrelated dirty-worktree failures, and manual QA/cleanup/independent review are absent. During implementation, run one Release build and at most one fastest relevant non-browser test project at each phase end, as required by the implementation-plan workflow.

## Handoff — 2026-08-02 Europe/Brussels

### Current State

- Phase 0 is verified and complete.
- Phase 1 has a substantial candidate implementation in the worktree but no accepted DoneClaim.
- No Phase 2 provider-composition work is approved to start.

### Modified Files

- `src/Explore.Secrets/Database/` — new structured provider contract, validation, builders, and redaction.
- `src/Explore.Secrets/Explore.Secrets.csproj`, `Directory.Packages.props`, and affected lock files — provider builder dependencies.
- `src/Explore.Persistence/PersistenceServicesRegistration.cs`, `ExploreDbContextFactory.cs`, and `DataProtectionKeyContextFactory.cs` — candidate shared runtime/design-time routing.
- `src/Event.MigrationService/Extensions/ConfigurationExtensions.cs` and `Program.cs` — candidate migrator routing and current PostgreSQL gate.
- `src/Explore.API/Extensions/TickerQSchedulerExtensions.cs` and `Scheduling/ApiTickerQDbContextFactory.cs` — candidate shared routing and hardcoded-localhost removal.
- `src/Explore.Secrets/Configuration/InfisicalConfigurationProvider.cs` — legacy public URL mapping removal.
- `tests/Event.Persistence.IntegrationTests/Database/PrimaryDatabaseConfigurationTests.cs` and `tests/Explore.Secrets.UnitTests/Bootstrap/BootstrapSecretLoaderTests.cs` — candidate contract and retained-bootstrap coverage.

### Validation

- `dotnet build --configuration Release --verbosity quiet` — passed.
- `dotnet test --project tests/Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release` — passed 205/205.
- `Event.Persistence.IntegrationTests` — not green; unrelated FK and migration-count failures reported. Focused MDB test rerun has no recorded final result.
- Manual external-process QA, artifact cleanup receipt, and independent acceptance — not completed.

### Notes For Next Contributor Or Agent

- Read `AGENTS.md`, persistence/migration/test rules, this context, the plan, and tasks before editing.
- Re-read every target file because concurrent agents are active.
- Never edit generated migrations or snapshots; concurrent changes there belong to another workstream.
- Do not stage, reset, or clean the shared worktree. Separate MDB evidence by explicit path and diff inspection.
