<!-- ABOUTME: Resumable context for adding provider-selected primary database support without exposing raw connection strings. -->
<!-- ABOUTME: Records verified repository state, settled decisions, dependencies, risks, and handoff guidance. -->

# Multi-Database Support Context

**Last Updated:** 2026-08-02 Europe/Brussels

**Status:** Phase 0 complete; Phase 1 candidate implementation awaiting verification

**Target providers:** PostgreSQL, SQLite, SQL Server, MariaDB, MySQL

**Related workstream:** [Optional Retained Erasure Authority](../optional-retained-erasure-authority/optional-retained-erasure-authority-plan.md)

## Objective

Allow an operator to select a supported primary database through one provider-neutral, structured configuration contract. Application code validates that contract, builds the provider-specific connection string internally, selects the correct EF Core provider and generated migration assembly, and preserves the platform's behavioral invariants.

This workstream does not make the privacy erasure authority use the selected primary provider. Its default authority is a restore-isolated embedded SQLite file; its enterprise option remains a separate PostgreSQL database. The two workstreams share configuration conventions and release evidence but have independent storage lifecycles.

## Verified Current State

- The Release baseline is green after isolated Refit 14 and Microsoft.OpenApi 2.7.5 compatibility repairs; those prerequisite fixes are outside MDB architecture.
- A Phase 1 candidate exists under `src/Explore.Secrets/Database/` with closed provider, role, TLS, and server-flavor types; structured options; validation; native connection-string builders; and credential-safe summaries.
- Candidate routing now covers `PersistenceServicesRegistration`, `ExploreDbContextFactory`, `DataProtectionKeyContextFactory`, MigrationService, and TickerQ runtime/design-time composition.
- The candidate removes the hardcoded TickerQ localhost connection and the executable `POSTGRESQL_PUBLIC_URL` secret mapping.
- EF provider registration remains PostgreSQL-only by design until Phase 2; non-PostgreSQL selection must fail before `UseNpgsql` rather than imply working provider support.
- Compose, `.env.example`, Aspire AppHost, health checks, operational documentation, and provider CI still assume PostgreSQL.
- PostgreSQL-specific SQL, functions, extensions, JSON behavior, indexes, migrations, tests, and operational assumptions are distributed across Persistence, Infrastructure, API, MigrationService, deployment, and CI.
- The repository still has no accepted production SQLite, SQL Server, MariaDB, or MySQL path.
- `Microting.EntityFrameworkCore.MySql` 10.0.10 is the selected EF Core provider for both MariaDB and MySQL; Pomelo is not part of the target.

## SESSION PROGRESS (2026-08-02 Europe/Brussels)

### ✅ COMPLETED

- MDB-001 through MDB-006: green Release baseline, repository inventory, classification, migration-history policy, and Phase 0 evidence.
- Phase 1 candidate source and contract tests were written in executor session `ses_03c96c432ffeACCq72heVpiQ23`.
- `dotnet build --configuration Release --verbosity quiet` exited 0 after the candidate changes.
- `Explore.Secrets.UnitTests` passed 205/205 in Release configuration.

### 🟡 IN PROGRESS

- MDB-100 through MDB-111 have candidate code but are not accepted: the executor never returned a DoneClaim and independent verification did not run.
- `PrimaryDatabaseConfigurationTests` were added, but the final focused rerun result is absent from the session record.

### ⏭️ NEXT

1. Re-read the candidate diff and verify it contains only MDB-owned changes in the dirty shared worktree.
2. Run the focused `PrimaryDatabaseConfigurationTests`, then the narrow touched-project Release builds/tests.
3. Execute the planned external-process configuration QA, capture credential-safe evidence, remove temporary assets, and obtain independent review.
4. Mark MDB-100 through MDB-112 only from confirmed evidence; otherwise repair the candidate first.

### ⚠️ BLOCKERS

- The full Persistence integration suite currently has unrelated dirty-worktree failures: scheduling/event-session foreign-key violations and migration-baseline assertions expecting two migrations while finding three.
- The shared worktree contains extensive concurrent registration and persistence changes, including generated migrations/snapshots. Do not revert, edit, or attribute those files to MDB.

## Settled Decisions

### One structured operator contract

Operators configure fields, never a connection string or free-form connection fragment:

| Field | Requirement |
|---|---|
| `Provider` | Closed enum: `PostgreSql`, `Sqlite`, `SqlServer`, `MariaDb`, `MySql` |
| `Host` | Required for server providers; forbidden for primary SQLite |
| `Port` | Optional; provider default when omitted |
| `Database` | Database name for server providers; persisted file path for primary SQLite |
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
- The database object namespace is fixed: PostgreSQL schema `islamu_event`; non-schema providers use the fixed `islamu_event_` object prefix where needed. Operators cannot customize it.
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
