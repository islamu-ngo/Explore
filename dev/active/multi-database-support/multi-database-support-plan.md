<!-- ABOUTME: Implementation plan for provider-selected PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL persistence. -->
<!-- ABOUTME: Defines structured configuration, provider composition, migrations, validation, deployment, and release evidence. -->

# Multi-Database Support Implementation Plan

**Last Updated:** 2026-08-08 Europe/Brussels

**Status:** Implementation complete; post-review hardening and release evidence reconciled

**Scope:** Primary application persistence and Data Protection, plus provider-neutral integration points consumed by the separately owned authority workstream

**Out of scope:** A universal SQL abstraction, runtime provider switching, mixed primary providers in one deployment, and selecting or changing the privacy-authority topology

## Re-baseline — 2026-08-08 Europe/Brussels

- **Reason:** Canonical verification and independent review exposed stale closeout text, an avoidable EF provider-cache contribution in a command-contract test, and an authority-topology ownership ambiguity.
- **What changed:** Server lock command tests now use native disconnected connections instead of constructing EF models, the shared real-provider contract exercises lock contention and release, and the authority boundary explicitly delegates topology semantics to the Optional Retained Erasure Authority workstream.
- **Plan impact:** Phases 0 through 7 remain complete. MDB-709 through MDB-713 close the implementation with scoped verification, manual SQLite QA, independent review, and precise residual-failure attribution.
- **Remaining work:** None in this workstream. Repository-wide architecture, API, and persistence-suite blockers remain owned by their originating workstreams and are recorded without weakening tests.

## Outcome

An operator selects one supported primary provider and supplies structured fields. Startup validates the configuration, code builds the provider-specific connection string internally, EF Core selects the matching provider and generated migrations, and the application behaves consistently across supported providers within documented deployment envelopes.

## Acceptance Criteria

1. No documented or executable operator path accepts a raw primary or authority connection string.
2. Runtime, design-time factories, MigrationService, Aspire, Compose, secret stores, and CI bind the same structured contract.
3. PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL each have explicit registration and generated migration ownership for application data and Data Protection.
4. A clean database migrates to the latest model on every provider without hand-edited migration output.
5. Portable application behavior is proven by shared tests; provider-specific behavior is isolated and tested on the real engine.
6. Invalid or incomplete structured settings fail before readiness with credential-safe diagnostics.
7. PostgreSQL remains a non-regressed production path.
8. Primary SQLite is explicitly constrained to one instance and a local persisted file.
9. Primary-provider selection never implicitly selects or changes authority topology. `EmbeddedSqlite`, `CoLocated`, and `ExternalDatabase` are explicit, mutually exclusive choices owned by the Optional Retained Erasure Authority workstream; embedded and external modes remain restore-independent, while `CoLocated` intentionally shares the primary restore lifecycle.
10. Deployment and recovery documentation explain provider selection, migration ownership, backup/restore, TLS, and rollback.

## Architecture

### Configuration flow

```text
operator fields / Aspire parameters / secret-store fields
                    |
                    v
          DatabaseOptions binding
                    |
          validation + normalization
                    |
                    v
       provider-native string builder
                    |
                    v
 process-local provider registration + derived connection string
```

The structured contract contains provider, host, port, database/path, schema, username, password, TLS mode, trust policy, and the bounded provider settings required by a selected engine. `Database:Schema` / `DATABASE_SCHEMA` defaults to `islamu_event` and is applied by PostgreSQL and SQL Server. Schema-less providers use the non-configurable `ie_` prefix. Server deployments instantiate that same shape for separate runtime and migrator credential roles; this is role separation, not a second connection configuration flow. Provider-specific builders map those values to Npgsql, Sqlite, SqlClient, or Microting-native syntax. Raw string passthrough and arbitrary fragments are forbidden.

### Persistence composition

- Keep shared EF entity configurations portable by default.
- Select provider registration once at startup.
- Introduce narrow capability services only for proven differences such as advisory locks, provider SQL, JSON translation, or migration operations.
- Keep repositories entity-returning and preserve tenant and soft-delete filters.
- Preserve application-level transactional invariants on all providers.

### Migration topology

Each provider owns generated application and Data Protection migrations. MigrationService is the sole production migration owner. Design-time factories use the same structured configuration logic and choose the correct migration assembly. Generated migrations and snapshots are never manually edited.

## Phase 0: Re-establish Baseline and Complete Inventory

### Work

- Restore a green Release build without folding unrelated fixes into this workstream.
- Inventory every PostgreSQL package, registration, factory, SQL fragment, function, extension, JSON mapping, index, lock, health check, deployment key, secret mapping, migration command, and test fixture.
- Classify each item as portable, provider-capability-specific, PostgreSQL enhancement, or unsupported.
- Record current migration histories and determine whether any pre-v1 histories are legally disposable.

### Exit criteria

- Green baseline.
- Inventory maps every affected path to a later phase.
- Migration-history policy is explicit and approved before regeneration.

## Phase 1: Structured Configuration Contract

### Work

- Define the closed provider enum and structured options model.
- Define runtime and migrator instances of the same structured model for server providers, with distinct credentials and shared endpoint projection where appropriate.
- Define provider-neutral TLS modes and bounded provider-specific settings.
- Implement validation matrices for server providers and primary SQLite.
- Implement provider-native connection-string construction with redacted diagnostics.
- Route `BootstrapSecretLoader`, runtime registration, design-time factories, MigrationService, and tests through the shared logic.
- Remove operator reliance on `ConnectionStrings:DefaultConnection`, raw factory `--connection`, hardcoded localhost strings, and legacy `POSTGRESQL_PUBLIC_URL` mapping.
- Define equivalent privacy-erasure-prefixed structured settings for external authority use, owned by the authority workstream.

### Exit criteria

- One model and one construction path cover all primary composition surfaces.
- Invalid combinations fail before provider registration.
- No operator-facing raw connection-string input remains.

## Phase 2: Provider Composition and Portable Model

### Work

- Add startup provider selection and provider package registration.
- Audit entity mappings, value conversions, collations, timestamps, UUIDv7 handling, JSON, decimal precision, generated values, indexes, constraints, and query filters.
- Replace avoidable PostgreSQL assumptions with portable EF Core constructs.
- Keep PostgreSQL-native defenses behind narrow PostgreSQL capability implementations.
- Assert configurable schema behavior for PostgreSQL and SQL Server with `islamu_event` as the default, plus fixed `ie_` names where schemas are unavailable.
- Preserve entity-returning repositories and no-tracking read behavior.

### Exit criteria

- Shared model builds under each provider.
- Capability boundaries correspond only to observed differences.
- Architecture and repository invariants remain intact.

## Phase 3: PostgreSQL Baseline

### Work

- Move PostgreSQL registration into the new provider path without behavior regression.
- Generate or retain PostgreSQL application and Data Protection migrations according to the approved history policy.
- Prove clean migration, upgrade migration, tenant isolation, soft delete, JSON behavior, outbox processing, and PostgreSQL-native defenses.

### Exit criteria

- Existing PostgreSQL deployment remains operational through structured inputs.
- PostgreSQL serves as the behavioral reference for shared provider tests.

## Phase 4: Primary SQLite

### Work

- Add Microsoft SQLite provider registration and generated migration assemblies.
- Require a persisted local file path; reject host, network filesystem, and multi-replica claims.
- Set bounded busy timeout and WAL behavior appropriate to the primary SQLite envelope.
- Adapt unsupported schema, concurrency, SQL, JSON, and locking behavior through minimal capability implementations.
- Keep the primary file distinct from `privacy_erasure_authority.db` in naming, DI, migrations, volumes, and recovery.

### Exit criteria

- Clean and upgrade migration tests pass on a real file.
- Single-instance CRUD, tenant filters, transactions, outbox, and restart persistence pass.
- Unsupported deployment shapes fail validation or are explicitly blocked by operations guidance.

## Phase 5: SQL Server

### Work

- Add SqlClient provider registration and generated migration assemblies.
- Map TLS and trust settings explicitly.
- Resolve identifier, datetime, UUID, JSON, filtered-index, transaction, and provider-SQL differences.
- Add clean migration and real-engine behavioral coverage.

### Exit criteria

- SQL Server clean migration, application behavior, Data Protection, and restart tests pass on the real engine.

## Phase 6: MariaDB and MySQL

### Work

- Add `Microting.EntityFrameworkCore.MySql` 10.0.10 registration.
- Require and validate server flavor/version where provider behavior needs it.
- Generate separate MariaDB and MySQL application/Data Protection migration assemblies if generated output differs; combine only when evidence proves identical ownership is safe.
- Resolve identifier length, charset/collation, datetime precision, JSON, generated-value, locking, and migration differences.
- Test current supported MariaDB and MySQL engines independently.

### Exit criteria

- Both engines migrate cleanly and pass the shared behavioral suite.
- Flavor/version selection is deterministic and documented.

## Phase 7: Deployment, CI, and Operations

### Work

- Update Aspire AppHost, Compose, `.env.example`, secret-store mappings, deployment templates, and health checks to emit structured fields.
- Add provider CI lanes: PostgreSQL, file-backed SQLite, SQL Server, MariaDB, and MySQL.
- Run migrations against clean real engines and exercise a minimal runtime path in each lane.
- Document migration ownership, credentials, TLS, backup/restore, provider upgrades, rollback, and SQLite limitations.
- Coordinate authority lanes: embedded SQLite authority by default; external PostgreSQL authority in an enterprise lane.
- Verify a primary restore does not restore or replace the embedded authority file.

### Exit criteria

- Every supported topology has repeatable provisioning and recovery instructions.
- CI produces provider-specific migration and behavior evidence.
- No deployment example exposes raw connection strings as operator inputs.

## Testing Strategy

- Unit tests: structured option validation, default ports, TLS mapping, secret redaction, and provider-native builder output properties.
- Architecture tests: dependency direction, migration assembly ownership, and no DTO-returning repositories.
- Shared integration contract: CRUD, tenant isolation, soft delete, transactions, concurrency, outbox/idempotency, paging, and Data Protection.
- Provider integration: clean migrations, upgrade migrations, provider SQL/capabilities, restart persistence, and real-engine semantics.
- Deployment smoke: structured configuration to readiness on every supported topology.
- Recovery: provider restore plus independent embedded-authority replay behavior.

## Security and Operations

- Credentials stay in secret stores and never appear in logs, exceptions, snapshots, or generated docs.
- TLS defaults must be secure; certificate trust bypass is explicit and auditable.
- Runtime and migrator credentials remain separate where the topology supports it.
- Database files use restrictive permissions and durable local volumes.
- Provider and authority backups are independently named, scheduled, retained, restored, and tested.

## Documentation Updates

Update canonical architecture, operations, testing, deployment, environment, Aspire, secrets, migration, backup/restore, and provider support documentation as each phase lands. Delete or rewrite PostgreSQL-only instructions only when the replacement path is verified.

## Definition of Done

- All acceptance criteria are evidenced.
- All provider lanes and required architecture tests pass from a green baseline.
- Generated migration output is reproducible and untouched by hand.
- Structured configuration is the only operator contract.
- PostgreSQL has no regression and every other provider meets its documented envelope.
- Authority behavior matches the explicitly selected topology: restore-independent modes remain independently restorable and failure-closed, while `CoLocated` makes no restore-isolation claim.
