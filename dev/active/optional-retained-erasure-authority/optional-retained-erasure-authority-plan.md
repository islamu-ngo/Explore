<!-- ABOUTME: Canonical implementation plan for platform-wide User erasure and its privacy-erasure authority. -->
<!-- ABOUTME: Owns policy, topology, persistence, replay, provider settlement, self-hosting, and restore guarantees. -->

# Platform Privacy Erasure Authority — Implementation Plan

Last Updated: 2026-07-22 Europe/Brussels

## 0. Plan Metadata

| Field | Value |
|---|---|
| Task ID | `optional-retained-erasure-authority` |
| Canonical intent | `platform-privacy-erasure` |
| Status | Active — Phases 1 and 2 accepted; Phase 3 User fence, saga, and complete local settlement in progress |
| Owner | Unassigned |
| Change type | Cross-layer privacy policy, erasure orchestration, infrastructure, API, persistence, hosting, tests, and operator documentation |
| Execution boundary | Phase-ordered implementation; current next dependency is the pre-enumeration User fence and policy-versioned saga in `OREA-300` |

### Scope

In scope:

- Maintain a machine-checked inventory of every durable local and external User-PII copy and one compiled disposition for each copy.
- Fence the User before erasure enumeration and prevent PII recreation while erasure is pending or complete.
- Apply all local dispositions, application mirror/checkpoint updates, provider work, cache invalidation intents, and receipt state atomically per authority fact.
- Expose truthful asynchronous deletion through `202 Accepted` and a short-lived, once-revealed receipt whose hash is stored for fixed-time verification.
- Settle external copies through specialized, idempotent, fenced provider outboxes after the local transaction commits.
- Remove `ApplicationDatabase` and `RetainedAuthority` as production behavior modes.
- Retain one authority-first erasure workflow and the application-database mirror/checkpoint.
- Support two explicit storage topologies: `CoLocated` and `ExternalDatabase`.
- Make a one-database installation simple while describing its weaker restore guarantee accurately.
- Make a two-database installation restore-safe when the authority database has an independent restore lifecycle.
- Wire API runtime and migration credentials separately for least privilege.
- Cover both topologies, including two independent PostgreSQL Testcontainers and a real pre-erasure application backup restore.
- Retain linkable authority data only through the maximum resurrection-capable backup horizon plus the approved safety margin; pseudonymize legal-hold evidence and destroy expired credentials/receipts.
- Document `.env`, `.env.example`, Docker Compose, Aspire, secrets, migration, backup, restore, upgrade, and health behavior.

Out of scope:

- A distributed transaction across the authority and application databases.
- Storing live PII, raw identifiers, selectors, or deletion payloads in the authority ledger.
- Claiming restore safety for a co-located authority or for two databases restored together.
- User-configurable SQL, arbitrary table targeting, or plugin-style erasure instructions.
- Destructive rewriting of existing migration history or deployed authority facts.
- Executable Organization or Tenant erasure before typed policies and handlers exist.
- Deleting upstream identities the platform does not own; revoke or unlink them instead.

### Inputs and overlaps

- Canonical intent: `.claude/contract/intents.yaml` → `platform-privacy-erasure`.
- Superseded broader plan and inherited evidence: `.omo/plans/platform-wide-privacy-erasure-authority.md`.
- Historical workstream being re-baselined: this `dev/active/optional-retained-erasure-authority/` directory.
- Related active planning: `dev/active/event-location-privacy/` owns only the EventLocation disclosure/remediation adapter consumed by this workstream.

This plan is the sole implementation authority for platform User erasure, authority storage, startup replay, provider settlement, receipt/status, retention, and restore behavior. The `.omo` plan remains historical evidence only after its verified facts and unfinished requirements are represented here.

The current intent describes one authority-first workflow with `CoLocated` as the default topology, `ExternalDatabase` as the explicit alternative, separate runtime/migrator credentials, and no Blazor authority secret. Phase 1 governance, inventory, topology, and ownership gates are complete; product implementation is proceeding through Phase 2 in dependency order.

## 1. Current-State Evidence

| Evidence | Current behavior | Consequence |
|---|---|---|
| `src/Explore.Application/Configuration/PrivacyErasureDurabilityOptions.cs` | Defines `ApplicationDatabase` and `RetainedAuthority`; defaults to `ApplicationDatabase` | Topology and durability behavior are conflated |
| `src/Explore.Application/ApplicationServicesRegistration.cs` | Selects between two workflow implementations | The production path can bypass authority-first replay semantics |
| `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs` | Registers replay only in retained mode | Startup recovery is mode-dependent |
| `src/Explore.Persistence/PersistenceServicesRegistration.cs` | Always registers the local mirror, but registers the authority context only in retained mode | The application mirror already exists and should remain |
| `src/Explore.API/BackgroundServices/PrivacyErasureStartupGate.cs` | Returns immediately in application-database mode | Co-located deployments do not verify pending facts before readiness |
| `src/Explore.Application/Services/RetainedAuthorityPrivacyErasureWorkflow.cs` | Appends authority intent, then replays it into the application database | This is the workflow shape to retain for both topologies |
| `src/Explore.Application/Services/PrivacyErasureApplier.cs` | Applies erasure, mirrors the fact locally, advances checkpoint, and emits outbox work in the application transaction | The requested application-side mirror is already part of the applier |
| `src/Explore.Application/Features/Users/Handlers/Commands/DeleteUserCommandHandler.cs` | User deletion is the current request boundary | Replace location-specific deletion behavior with one policy-versioned platform orchestrator and truthful asynchronous status |
| `src/Explore.Persistence/Repositories/UserLocationPrivacyErasureRepository.cs` and focused contract/integration tests | A named owner-bounded cross-tenant Location/Home disposition seam exists and has prior PostgreSQL evidence | Preserve its exact subject predicates and tenant-substitution protections as one adapter inside the complete policy, not as a second orchestrator |
| `src/Explore.Domain/PrivacyErasureSaga.cs`, `PrivacyErasurePolicyCoverage.cs`, `PrivacyErasureIntent.cs`, and `PrivacyErasureReplayCheckpoint.cs` | Generalized platform state is present in current source | Treat presence as partial implementation; acceptance requires fence, receipt, policy coverage, concurrency, expiry, and restore tests |
| `tests/Event.Architecture.Tests/Privacy/UserPiiInventory.cs` and `UserPiiInventoryArchitectureTests.cs` | A machine-checkable disposition inventory and selectors exist | Reconcile them with the current EF model and provider registries before any erasure family is accepted |
| Existing email, notification, web-push, webhook, storage, ATProto, Listmonk, Keycloak, and outbox implementations | Provider-specific lifecycle and retry patterns already exist | Reuse specialized clients/outboxes; do not introduce a generic provider plugin or inline network calls |
| `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/ApplicationDatabasePrivacyErasureLedgerRepository.cs` | Implements the application ledger against `ExploreDbContext` | It can remain the mirror and support a co-located authority adapter |
| `src/Explore.Persistence/Privacy/ErasureAuthority/PrivacyErasureAuthorityDbContext.cs` | Maps only the authority counter and intent ledger | External authority storage is already narrowly scoped |
| `tests/Event.Persistence.IntegrationTests/Privacy/PrivacyErasureAuthorityDatabaseContractTests.cs` and related composition/ownership/model tests | Dedicated authority schema, function-only runtime access, and topology composition have focused evidence | Reuse these proofs, but rerun them against the consolidated topology and migration contract before acceptance |
| Application and authority migrations | Both create `privacy_erasure_authority` tables; the dedicated migration also creates security-definer functions and ACLs | Running both migrations in one database would collide; co-located storage must be owned by the application migration |
| `src/Event.MigrationService/Program.cs` and `Worker.cs` | Migrate the application and data-protection contexts only | External authority migrations have no host-owned execution path yet |
| `.env`, `.env.example`, `docker-compose.yml` | Mention the old mode but do not fully map authority configuration into services | Self-hosted behavior is incomplete and easy to misconfigure |
| `src/Explore.Secrets/Bootstrap/BootstrapSecretLoader.cs` | Builds only the main PostgreSQL connection from `POSTGRESQL_*` | Duplicating the loader for authority storage would add unnecessary configuration machinery |
| `tests/Event.Persistence.IntegrationTests/Privacy/GlobalLocationPrivacyErasureTests.cs` | Already provisions separate application and authority PostgreSQL containers for retained tests | Two-container infrastructure should be extended rather than recreated |

### Baseline status

The planning session ran the canonical Release build on 2026-07-22: 26 projects built with 0 errors and 41 warnings. The warnings include pre-existing `NU1903` advisories for `System.Security.Cryptography.Xml` 10.0.7. The worktree contains unrelated user changes, so implementation must still record its own starting SHA/status and rerun the baseline before editing runtime files; it must not attribute unrelated failures or warnings to this workstream without scoped evidence.

## 2. Proposed End State

### 2.1 Complete platform User-erasure contract

The authority records one immutable, policy-versioned User intent. A compiled policy orchestrator then applies every classified disposition; the inventory proves completeness but never becomes executable SQL or runtime instructions.

The platform contract is:

1. Authenticate and authorize the deletion request.
2. Append or reuse one typed authority fact and establish the User fence before enumerating PII.
3. In one serializable application transaction, apply all local hard-delete/anonymize/retain dispositions, write the application mirror/checkpoint, materialize specialized provider work, invalidate cache authority, and persist receipt/status state.
4. Commit before any provider call. Specialized workers settle external work with lease fencing, idempotency, bounded retry/backoff, explicit `Unknown`, dead-letter visibility, and reconciliation.
5. Return `202 Accepted`, `Location`, and `Retry-After` with a once-revealed receipt. Persist only a fixed-time-verifiable hash; the status endpoint is `private, no-store` and remains usable after login removal until expiry.
6. Prevent PII-producing writes, workers, and cache rematerializers from recreating subject data while the fence is active.
7. On startup or restore, replay every authority fact not covered by the current policy version before traffic and ordinary workers.
8. Retain or pseudonymize evidence only under an explicit retention/legal-hold rule and purge linkable identifiers after the resurrection-capable backup horizon plus the approved margin.

Each durable PII family must be classified as exactly one of: hard delete, anonymize, bounded retain, or external action. `User` is the only executable subject kind. Organization and Tenant remain extension seams until separately approved typed policies and handlers exist.

### 2.2 One workflow, two storage topologies

```text
authorized erasure request
        |
        v
append immutable authority fact and commit it
        |
        v
replay/apply into application transaction
        |
        +--> remove or anonymize bounded PII
        +--> append/idempotently confirm application-side mirror
        +--> advance replay checkpoint
        +--> enqueue outbox follow-up work
```

The workflow is invariant. Only the `IPrivacyErasureAuthority` storage adapter changes:

| Topology | Authority adapter | Application mirror | Restore guarantee |
|---|---|---|---|
| `CoLocated` | Short-lived, separately committed adapter over the application database authority ledger | Same application ledger, idempotently confirmed by the applier | No protection from restoring the whole application database to a pre-erasure backup |
| `ExternalDatabase` | Dedicated `PrivacyErasureAuthorityDbContext` and append/read functions | Application database ledger | Replay-safe only when the authority database is not restored with the application database |

`CoLocated` is a supported simplicity topology, not a different erasure behavior. Its authority append must commit before the application mutation begins so an application-transaction rollback remains replayable. It must use a separate context/transaction boundary from the applier.

### 2.3 Configuration contract

Replace `PrivacyErasure:Durability:Mode` with:

```text
PrivacyErasure:Authority:Topology = CoLocated | ExternalDatabase
```

Rules:

- Default to `CoLocated` for direct developer and single-database self-hosting scenarios.
- Keep `.env` and `.env.example` explicit with `PRIVACY_ERASURE_AUTHORITY_TOPOLOGY=CoLocated` so operators see the choice.
- Do not infer `ExternalDatabase` from connection-string presence.
- Ignore stray external authority values in `CoLocated` and never open the external connection.
- Require an authority connection in `ExternalDatabase`; fail startup/migration with a bounded, actionable error when absent.
- Reject the legacy `PrivacyErasure:Durability:Mode` key with an upgrade message. Do not silently translate `ApplicationDatabase`, because silent translation can hide an intended restore guarantee.

Compose-facing variables:

```dotenv
PRIVACY_ERASURE_AUTHORITY_TOPOLOGY=CoLocated
PRIVACY_ERASURE_AUTHORITY_RUNTIME_CONNECTION_STRING=
PRIVACY_ERASURE_AUTHORITY_MIGRATOR_CONNECTION_STRING=
```

The two secret values map into the same internal .NET key in different processes:

| Process | Public/self-host variable | Internal key | Privilege |
|---|---|---|---|
| `Explore.API` | `PRIVACY_ERASURE_AUTHORITY_RUNTIME_CONNECTION_STRING` | `ConnectionStrings__PrivacyErasureAuthority` | Execute append/read functions only |
| `Event.MigrationService` | `PRIVACY_ERASURE_AUTHORITY_MIGRATOR_CONNECTION_STRING` | `ConnectionStrings__PrivacyErasureAuthority` | Schema migration and grant management |

This keeps the existing named connection contract, prevents the API from receiving DDL credentials, and avoids duplicating the main PostgreSQL secret-loader convention. Direct non-Compose deployments may set the internal .NET keys per process. Secret examples remain blank and must never include production credentials.

### 2.4 Migration ownership

- The application migration remains the sole owner of `privacy_erasure_authority` tables in `CoLocated` topology and of the application-side mirror in all topologies.
- The dedicated authority migration remains the sole owner of authority tables/functions/roles in the external database.
- `Event.MigrationService` conditionally migrates the external authority context before API readiness when topology is `ExternalDatabase`.
- The API runtime credential never applies migrations.
- No implementation may apply both the application migration and dedicated authority migration to the same physical database.
- This pre-v1 development workstream uses a documented reset-only upgrade policy. No compatibility shim or silent mode translation is required; reset eligibility, backup prerequisites, generated migration ownership, and the prohibition on agent-driven database/container/volume/backup deletion remain explicit.

### 2.5 Readiness and operations

The startup gate always invokes replay. In `CoLocated`, replay reads the local authority adapter. In `ExternalDatabase`, it reads the external authority. Readiness remains false until replay reaches the authority counter or an existing bounded policy explicitly permits degraded startup.

Health/diagnostic output exposes only:

- `topology`: `CoLocated` or `ExternalDatabase`;
- `restoreReplayProtection`: `false` for `CoLocated`; `true` for externally configured storage, with documentation clarifying the independent-restore-domain requirement;
- replay status/counter lag and last successful replay time where existing health conventions support them.

It must not expose hosts, database names, users, connection strings, identifiers, selectors, or erased values.

## 3. Architecture and Design Decisions

| ID | Decision | Rationale |
|---|---|---|
| D1 | Remove the behavior-mode enum and both mode names from production configuration | There is only one correct authority-first workflow; storage placement is the variable |
| D2 | Use explicit `CoLocated` / `ExternalDatabase` topology values | They describe deployment shape without overstating durability |
| D3 | Keep the application ledger/mirror in both topologies | It provides local audit/checkpoint/idempotency data and is already integrated with the applier |
| D4 | Use a separately committed co-located authority adapter | Preserves authority-first recovery after application transaction rollback without a distributed transaction |
| D5 | Do not run the dedicated authority migration in the application database | Existing table ownership overlaps and would collide |
| D6 | Separate runtime and migrator secrets at the host boundary | Enforces least privilege while reusing one internal named connection per process |
| D7 | Fail on a configured legacy mode key | Prevents silent safety downgrades during upgrade |
| D8 | Treat independent restore lifecycle as an operational prerequisite, not something a connection string can prove | Two databases on one restored cluster/volume may still share one failure domain |
| D9 | Extend existing TUnit/Testcontainers fixtures | Avoids duplicate infrastructure and keeps PostgreSQL behavior realistic |
| D10 | Use a machine-checked inventory for completeness, never runtime interpretation | New PII copies fail governance without creating an arbitrary deletion engine |
| D11 | Fence before enumeration and check the fence at shared PII-producing boundaries | Prevents races and post-erasure resurrection without scattered controller checks |
| D12 | Keep local disposition, mirror/checkpoint, provider work, cache authority, and receipt state in one serializable application transaction | Makes local completion truthful and crash-recoverable |
| D13 | Use specialized provider work and adapters | Preserves ownership, SSRF resistance, typed targets, and provider-specific unknown-outcome handling |
| D14 | Return asynchronous `202` with a short-lived once-revealed receipt | Login may be removed before external settlement; storing only a hash limits credential exposure |
| D15 | Delete only platform-managed upstream identities; revoke or unlink externally managed identities | Avoids destructive authority beyond platform ownership |
| D16 | Retain linkable authority identifiers only for the resurrection-capable backup horizon plus the approved margin | Restore protection has a finite operational boundary and must not become indefinite tracking |
| D17 | Compact only an expired contiguous authority prefix behind a metadata-only floor; reject application checkpoints below that floor before traffic | Preserves sequence monotonicity and bounded in-horizon restore protection without retaining a subject-bearing replay snapshot |
| D18 | Legal holds retain non-relinkable pseudonymized evidence with bounded reason codes, mandatory review/expiry, and no extension of the replay horizon | Preserves approved evidence without turning legal hold into indefinite subject tracking |
| D19 | Keep `ExternalDatabase` to `CoLocated` downgrade unsupported | Downgrade removes independent restore protection and has no approved safe automation contract |

Rejected alternatives:

- Retain `ApplicationDatabase` as a fast path: rejected because it bypasses authority-first replay and duplicates behavior.
- Auto-enable external authority when a connection exists: rejected because a missing or misspelled secret could silently downgrade protection.
- Run both EF migration sets in one database: rejected because both own the same schema objects.
- Add a distributed transaction: rejected because it couples independent restore domains and adds failure modes without solving restored-backup resurrection.
- Add a second discrete `POSTGRESQL_*` secret loader: rejected unless implementation evidence proves the per-process named connection mapping insufficient.
- Add a generic provider plugin or arbitrary erasure instruction interpreter: rejected because compiled specialized handlers are safer and already match repository patterns.
- Perform provider cleanup inline with the request transaction: rejected because remote uncertainty must not roll back completed local erasure.
- Return synchronous deletion success before provider status is knowable: rejected because it would misrepresent completion.
- Retain a subject-bearing compaction snapshot: rejected because it would preserve the identifiers that bounded retention is intended to destroy.
- Recover an application backup whose checkpoint is below the compaction floor: rejected because the erased-subject identifiers required for safe replay have intentionally expired; readiness must fail closed instead.

## 4. Implementation Phases and Tasks

The checkbox ledger is canonical in `optional-retained-erasure-authority-tasks.md`. Each phase ends with exactly one root Release build and at most one relevant test project.

### Phase 1 — Governance, inventory, and contract semantics

Goal: establish one authoritative privacy-erasure contract before further runtime edits.

- **OREA-100:** Amend `platform-privacy-erasure` in `.claude/contract/intents.yaml` to own the complete User-erasure policy, provider work, receipt/status, authority topologies, retention, restore, and affected paths. Reconcile the machine-checked User-PII inventory against the current EF model and provider registries; every durable copy must have exactly one non-executable disposition and policy version.
- **OREA-110:** Replace durability-mode options with `CoLocated` / `ExternalDatabase` topology options, reject legacy mode keys, and prove exactly one authority-first workflow is selected. Generalized authority facts remain typed and bounded to `User`; arbitrary JSON, table/column selectors, and executable Organization/Tenant kinds are rejected.
- **OREA-120 — completed 2026-07-22:** Marked `.omo/plans/platform-wide-privacy-erasure-authority.md` historical and removed platform-erasure implementation ownership from the Event Location workstream, retaining only the typed EventLocation disposition/correction integration boundary.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

### Phase 2 — Persistence adapters and migration ownership

Goal: make both topology adapters satisfy the same authority contract without schema collision.

- **OREA-200:** Keep `ApplicationDatabasePrivacyErasureLedgerRepository` as the application mirror/checkpoint store. Implement the co-located authority adapter with a short-lived `ExploreDbContext` and separate commit boundary; retain the dedicated authority context/repository and function-only runtime ACL for `ExternalDatabase`.
- **OREA-210:** Make application migrations the sole owner of co-located authority tables and the dedicated authority migration the sole owner of external tables/functions/roles. Prevent both migration sets from targeting one physical database and implement the documented pre-v1 reset-only policy; breaking compatibility is permitted, silent data loss and agent-driven deletion of databases, containers, volumes, or backups are not.
- **OREA-220:** Extend existing PostgreSQL 18 Testcontainers fixtures for one-container and two-independent-container topologies, monotonic append, rollback/replay idempotency, ACL enforcement, and a real pre-erasure application backup restore.

Required integration scenarios:

- One container: authority append survives a forced application mutation rollback; replay later applies it once; mirror/checkpoint remain idempotent.
- One container: external connection values are not read in `CoLocated`.
- Two containers: runtime role cannot select/insert/update authority tables directly and can execute only approved functions.
- Two containers: concurrent appends allocate monotonic sequence values without duplicates.
- Migration composition: applying the co-located path never executes the dedicated authority migration against the application database.
- Two containers: create a real pre-erasure application backup, complete erasure, restore only the application database, replay from the untouched authority container, and prove re-erasure plus idempotency.
- Update `schemas/islamu-event.md`, `docs/SECURITY-MODEL.md`, `docs/BACKUP_RESTORE_UPGRADE.md`, and `docs/TESTING.md` with the persistence/restore behavior proven in this phase.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
```

### Phase 3 — User fence, saga, and complete local dispositions

Goal: make local User erasure complete, atomic, policy-versioned, and resistant to recreation.

- **OREA-300:** Implement or complete the User fence, saga, policy-version coverage, receipt hash/expiry, concurrency, and idempotent request state. The first authority append/reuse and fence establishment occur before PII enumeration.
- **OREA-310:** Apply inventory dispositions for identity/authentication, tenancy/membership/preferences, owned Home/location data, registration/contact sharing, notifications/email/web-push, AI/webhook/report/audit/configuration/idempotency, storage/federation, and shared authored content. Preserve only explicitly justified bounded outcomes and anonymize shared content instead of deleting unrelated users' data.
- **OREA-320:** In one serializable application transaction, apply all local dispositions, confirm the mirror/checkpoint, materialize specialized provider work and EventLocation correction intents, invalidate cache authority, and persist receipt/status. The Event Location adapter supplies exact subject/tenant predicates, Home/room tombstoning, affected `EventLocation` corrections, and stable idempotency; this workstream owns the orchestration and cross-family acceptance tests.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
```

### Phase 4 — Provider settlement and anti-resurrection enforcement

Goal: complete external cleanup without weakening local atomicity or allowing PII recreation.

- **OREA-400:** Add or complete specialized provider-work outboxes with typed targets, stable idempotency keys, lease fencing, bounded retry/backoff, explicit `Unknown`, dead-letter visibility, and reconciliation. No arbitrary payload or provider call is allowed in the request/application transaction.
- **OREA-410:** Implement specialized ownership-aware adapters for platform-managed identity deletion and external identity revoke/unlink, ATProto, Listmonk, object storage, web push, webhook/export projections, and every provider family present in the inventory. Wrong tenant/subject and untrusted endpoint inputs fail before I/O.
- **OREA-420:** Enforce the User fence at shared PII-producing write, worker, cache-rematerialization, and remote-dispatch boundaries. Cache invalidation failure must not serve stale subject PII; convergence work degrades readiness and alerts operators.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet
```

### Phase 5 — Receipt/status API, replay, and readiness

Goal: expose truthful deletion progress and prevent traffic before policy replay converges.

- **OREA-500:** Replace the location-specific deletion boundary with the platform orchestrator. `DELETE` returns `202 Accepted`, `Location`, `Retry-After`, and the receipt once; the `private, no-store` status route uses a dedicated receipt authorization scheme after login removal and exposes only bounded phase/outcome codes.
- **OREA-510:** Run replay in both topologies before API/BFF/MCP/ordinary workers and readiness. A fresh scope reloads persisted subject/tenant ownership for every replay or reconciliation attempt; caller-supplied identifiers are never authority. External unavailability, sequence gaps, corruption, or lag fail closed.
- **OREA-520:** Publish bounded readiness and metrics for topology, restore capability, replay lag, provider backlog, dead letters, and last success without identifiers, connection details, endpoints, payloads, or free-text errors; update `docs/OPERATIONS.md` and `docs/SECURITY-MODEL.md` with shipped behavior.

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
```

### Phase 6 — Self-hosting, secrets, retention, and disaster recovery

Goal: make one- and two-database deployments operable with explicit guarantees and recovery procedures.

- **OREA-600:** Teach `Event.MigrationService` to migrate the external authority only for `ExternalDatabase`; order Compose/Aspire migration before API readiness; wire explicit topology plus separate runtime/migrator secrets; pass neither authority secret to Blazor; preserve standard .NET configuration for non-Compose operators.
- **OREA-610:** Implement bounded authority/receipt/provider credential retention, dry-run cleanup, legal-hold pseudonymization, backup-horizon configuration, and secret rotation without identifier leakage.
- **OREA-620:** Document and test backup ordering, RPO/RTO, authority loss/corruption recovery, credential rotation, `CoLocated` to `ExternalDatabase` cutover, unsafe downgrade acknowledgement, forward repair, and old-backup restore. Do not claim `ExternalDatabase` restore safety unless its restore domain is independent.

Approved compaction execution order (2026-07-25):

1. Accept finite-expiry and receipt/locator-cleanup prerequisites through independent real-runtime gates.
2. PIN current replay/ACL/restore behavior and add failing floor, hold-evidence, and stale-checkpoint tests without committing a red tree.
3. Add one metadata-only monotonic floor record and one bounded, non-relinkable legal-hold evidence record with no subject-bearing fields.
4. Add separate additive application/external migrations and one constrained compaction function that advances the floor and removes only its expired contiguous prefix in one transaction. Reuse the existing function-only runtime and owner/migrator privilege model; add no digest chain, compactor role, topology latch, or hold-management API.
5. Keep compaction independent from receipt/locator cleanup and normal replay/apply. Add dry-run-first scheduling plus floor-aware startup/readiness validation; checkpoints below the floor fail before traffic.
6. Prove contiguous-prefix compaction, cancellation rollback, below-floor old-backup rejection, restart-based credential rotation, and `CoLocated` to `ExternalDatabase` forward copy against real PostgreSQL. `ExternalDatabase` to `CoLocated` remains unsupported by contract; add no downgrade workflow or override.
7. Manually converge DBML and operator contracts with their authoritative EF migrations/model snapshot, then run independent plan, security/database, real-runtime, and scope/migration-history closure reviews against one final snapshot.

The first successful floor advancement is a forward-only rollback barrier: old binaries, migration downgrade, and application checkpoints below the floor must fail closed. The authority counter never decreases, retained facts remain contiguous above the floor, and no subject-bearing replay snapshot, denylist, digest chain, or partial schema generator is permitted.

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Secrets.Tests/Explore.Secrets.Tests.csproj --configuration Release --verbosity quiet
```

### Phase 7 — Contract, documentation, and completeness convergence

Goal: leave one authoritative, testable enterprise contract with no obsolete ownership or terminology.

- **OREA-700:** Converge the PII inventory, schemas, OpenAPI/generated contracts, API changelog, configuration, privacy, security, self-hosting, deployment, backup/restore, secrets, outbox, operations, troubleshooting, and testing documentation. UUIDs remain linkable personal data; minimized does not mean anonymous.
  The DBML schema remains a maintained documentation artifact: update lifecycle tables and relationships with their EF Core model or migration, and retain the focused architecture contract rather than adding a partial generator.
- **OREA-710:** Remove obsolete location-specific authority names, legacy behavior-mode configuration, duplicate workstream ownership, and unclassified User-PII copies. Prove every current local/external copy maps to one implemented disposition and every producer maps to the shared fence.
- **OREA-720:** Record final evidence for normal deletion, concurrency, rollback, duplicate/ambiguous append, provider unknown/reconciliation, tenant substitution, receipt expiry, policy upgrade, both topologies, old-backup replay, unrelated-user preservation, and zero PII resurrection.

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
```

The amended intent may mandate additional project checks for release closeout. Distribute them across the owning phases when the intent is updated; do not add a test-only phase or duplicate project runs without a concrete coverage reason.

## 5. Data and Control Flow

### Erasure request

1. Authorization and current privacy-erasure scope checks complete.
2. The workflow creates or reuses one typed, payload-free, policy-versioned User intent and stable receipt state.
3. The selected authority adapter appends idempotently and commits; the application establishes the User fence before enumerating PII.
4. The applier opens one serializable application transaction and applies every inventory-owned local disposition.
5. The transaction confirms the mirror/checkpoint, persists provider work and EventLocation corrections, invalidates cache authority, and advances receipt status.
6. The transaction commits; the API returns `202` with the once-revealed receipt, and later replay of the same fact is a no-op.
7. Specialized fenced workers settle provider work after commit. Terminal, retryable, and unknown outcomes update bounded receipt status without restoring local PII.
8. Shared write/worker boundaries reject or suppress any attempt to recreate PII for the fenced/deleted User.

### Startup

1. Hosting validates topology and required secrets.
2. The migration service applies the application migrations and, only for `ExternalDatabase`, dedicated authority migrations with the migrator credential.
3. API replay reads authority facts through the runtime adapter.
4. Readiness opens only after replay catches up or an already-approved bounded policy says otherwise.

### Restore

- `CoLocated`: restoring the application database also restores/removes the authority facts and mirror together. No replay source exists for erasures after that backup.
- `ExternalDatabase`: restoring only the application database moves its checkpoint/mirror backward while authority facts remain forward. Startup replay reapplies the missing erasures.

## 6. Testing and Acceptance Strategy

### Unit and composition acceptance

- `User` is the only executable subject kind; arbitrary metadata and future subject kinds fail closed.
- Every current EF/provider PII copy and producer has exactly one machine-checked policy mapping.
- Only `CoLocated` and `ExternalDatabase` parse.
- Default is `CoLocated` and is explicit in sample self-host configuration.
- A legacy mode key produces an actionable upgrade failure.
- A stray authority connection never changes topology.
- `ExternalDatabase` without its process-appropriate connection fails before serving traffic.
- Exactly one workflow is registered in both topologies.
- Fence, receipt, policy version, concurrency, expiry, and idempotent retry semantics are deterministic.

### PostgreSQL/Testcontainers acceptance

- Tests use PostgreSQL `18-alpine`, matching the existing fixture convention.
- The one-database suite uses one PostgreSQL container and no external authority connection.
- The two-database suite uses two independent PostgreSQL containers, not merely two schemas.
- Backup/restore proof uses an actual database backup/restore or an equivalently faithful PostgreSQL restore operation; deleting rows to imitate restore is insufficient.
- Fixtures expose lifecycle and connection details without leaking passwords into test output.
- Parallel tests use isolated databases/fixtures and deterministic cleanup.

### Security acceptance

- Authority rows contain no live PII or reversible selector material.
- Receipts are revealed once, stored only as fixed-time-verifiable hashes, expire on schedule, and never enter logs/metrics/traces.
- External runtime credentials cannot perform DDL or direct table DML/select.
- Migrator credentials are not injected into API or Blazor services.
- Exceptions and health payloads redact connection details.
- No provider call or external authority call runs inside the serializable application transaction.
- Tenant/subject ownership is reloaded from persistence for every delivery/reconciliation; wrong-tenant substitution fails before mutation or I/O.
- The fence prevents PII recreation across handlers, workers, cache rematerializers, and remote dispatch.

### Acceptance matrix

| Scenario | CoLocated | ExternalDatabase |
|---|---:|---:|
| Normal erasure | Required | Required |
| Forced application transaction rollback then replay | Required | Required |
| Startup catch-up | Required | Required |
| Duplicate replay/idempotency | Required | Required |
| Concurrent monotonic append | Required | Required |
| External runtime ACL | N/A | Required |
| Restore pre-erasure application backup | Documented limitation | Required proof |
| Missing authority connection | Not read | Fail closed |
| Authority unavailable | N/A | Readiness blocked |
| Complete local User-PII disposition | Required | Required |
| Provider timeout/ambiguous outcome | Fenced `Unknown` and reconciliation | Fenced `Unknown` and reconciliation |
| Receipt replay/expiry/wrong credential | Fail indistinguishably | Fail indistinguishably |
| Post-erasure PII recreation attempt | Denied/suppressed | Denied/suppressed |
| Tenant/subject substitution | Fail closed | Fail closed |

## 7. Documentation, Configuration, and Operational Updates

Implementation must update these sources together:

| Artifact | Required content |
|---|---|
| `.env` | Local topology selection and safe/blank authority secret placeholders; preserve unrelated user values |
| `.env.example` | Copyable defaults, allowed values, when each secret is required, no real credentials |
| `docker-compose.yml` | Per-process mapping and migration-before-readiness dependency |
| `src/Explore.AppHost/AppHost.cs` | One-resource and distinct-authority-resource examples/conditional wiring |
| `docs/CONFIGURATION.md` | Canonical keys, precedence, validation, topology semantics |
| `docs/SECRETS.md` | Runtime vs migrator ownership, provider paths/names, rotation and redaction |
| `docs/SELF_HOSTING.md` | One-database quick start and two-database secure deployment |
| `docs/DEPLOYMENT_MODES.md` and `docs/DEPLOYMENT_TIERS.md` | Service wiring, migration ordering, readiness behavior by supported deployment shape |
| `docs/BACKUP_RESTORE_UPGRADE.md` | Independent restore boundary, restore runbook, topology upgrade/downgrade constraints |
| `docs/OPERATIONS.md` | Health fields, replay monitoring, failure response |
| `docs/TESTING.md` | One- and two-container commands/scenarios |
| `docs/SECURITY-MODEL.md` and `docs/OPERATIONS.md` | One workflow, mirror semantics, guarantees, non-guarantees, startup, and recovery |
| `schemas/islamu-event.md` and `docs/DOMAIN.md` | Application mirror vs external authority ownership and domain vocabulary |
| Machine User-PII inventory and architecture selectors | Complete local/external copy classification, producer/fence mapping, disposition owner, retention, provider action, and policy version |
| OpenAPI/API changelog/generated client | `202`, receipt authorization, status/expiry/error/cache contract; generated artifacts are never hand-edited |
| `docs/OUTBOX_PATTERN.md` | Specialized provider-work fencing, idempotency, retry, `Unknown`, dead-letter, and reconciliation semantics |

Operator documentation must use “co-located” for storage placement and reserve “restore-safe” for deployments whose authority backup/restore lifecycle is actually independent.

## 8. Security and Privacy Constraints

- Authority facts remain payload-free and bounded by compiled application handlers.
- The external authority is append/read-only through reviewed database functions for runtime.
- No connection string or secret appears in source, examples, logs, exception messages, health, or test snapshots.
- Failure is closed: selected external topology without a reachable authority does not accept erasure-dependent traffic or advertise readiness.
- Existing authorization, idempotency, serializable transaction, and transactional outbox invariants remain intact.
- The application mirror is not a substitute for an independently retained authority.

## 9. Failure Handling and Observability

| Failure | Expected behavior | Operator signal |
|---|---|---|
| Invalid topology or legacy mode key | Startup/configuration fails | Bounded configuration error with replacement key |
| Missing external connection | Startup/migration fails before traffic | Missing key name only, no value |
| External authority unavailable | No fact append; readiness/replay fails closed | Health status and structured failure category |
| Authority append succeeds, application apply fails | Fact remains pending and replayable | Replay lag increments; retry is idempotent |
| Application database restored behind | External replay reapplies missing facts | Checkpoint lag then convergence |
| Both databases restored behind together | Guarantee unavailable | Runbook explicitly identifies unsupported restore operation |
| Provider timeout or ambiguous acknowledgement | Local erasure remains committed; provider work becomes `Unknown` until reconciliation | Bounded backlog/dead-letter category, never provider payload |
| Receipt is invalid, replayed, or expired | Deny indistinguishably without subject disclosure | Bounded authentication failure category |
| Fenced User attempts a PII-producing write | Reject or suppress before persistence/provider I/O | Bounded fence-denial counter |
| Cache invalidation fails | Exact subject PII cannot be served from stale cache; convergence work retries and readiness degrades | Retry backlog plus operator alert |

Metrics/logging should reuse existing observability conventions and include topology and numeric lag only. Do not label metrics with identifiers, selectors, connection data, or error messages that can explode cardinality.

## 10. Migration and Rollout

1. Treat the current product as pre-v1 development software: the supported upgrade policy for the removed behavior modes is reset-only, with no compatibility shim or silent translation.
2. Document reset eligibility, backup/export prerequisites, exact generated migration ownership, and how operators preserve any data they are not permitted to discard. Implementation agents never delete databases, containers, volumes, or backups.
3. Reject legacy mode keys with actionable replacement guidance; start the target model explicitly as `CoLocated` or `ExternalDatabase`.
4. For an external deployment, provision authority database/roles, run migration service with migrator credentials, then start API with runtime credentials.
5. To move from co-located to external after the target model is live, establish the external authority and copy/append existing payload-free facts monotonically before switching topology; test the cutover and rollback boundary.
6. Do not support external-to-co-located downgrade as safety-neutral. It removes restore protection and requires explicit operator acknowledgement.
7. Binary rollback after an authority fact exists must preserve newer authority facts and use forward repair; it must never resurrect erased PII.

## 11. Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Operators assume two DB names on one restored cluster are independent | State restore-domain requirement in env comments, health docs, and restore runbook |
| Co-located append joins the application transaction and rolls back | Dedicated short-lived context plus rollback/replay integration test |
| Both migration sets target one database | Topology-aware migration registration and composition test |
| Missing secret silently downgrades to co-located | Explicit topology; never infer from connection presence; fail closed |
| Runtime receives migrator privilege | Separate host variables mapped per process; ACL integration test |
| Legacy configuration changes behavior unnoticed | Reject old key with direct upgrade guidance |
| Backup/restore test is too synthetic | Use PostgreSQL backup/restore against two containers |
| Existing active plans contradict the new target | Governance synchronization is Phase 1 and a release gate |
| Large dirty worktree obscures causality | Record baseline status/SHA; limit edits to intent-authorized paths; inspect scoped diffs |
| A durable User-PII copy or producer is missed | Machine-check inventory and producer registry against EF/provider surfaces; fail architecture tests on unclassified additions |
| Provider ambiguity is treated as success or blindly retried | Persist `Unknown`, use stable idempotency/fencing, and require reconciliation evidence |
| Receipt or telemetry becomes a new disclosure channel | Store only receipt hash, use `no-store`, closed outcome codes, bounded labels, and no identifiers/free text |
| Tenant/subject substitution corrupts another user's data | Reload persisted ownership in a fresh scope for every delivery/reconciliation and test hostile substitutions |
| Pre-v1 reset guidance causes silent data loss | Require explicit eligibility/backup documentation and forbid implementation agents from deleting operator data |

## 12. Definition of Done

- [ ] Canonical intent and all active planning sources describe one workflow and two topologies.
- [ ] This plan is the sole active owner of platform User erasure, receipt/status, provider settlement, replay, retention, and restore behavior; Event Location owns only its typed adapter and corrections.
- [ ] Every durable local/external User-PII copy and producer is machine-classified with one implemented disposition/fence owner.
- [ ] User fence, saga, policy version, receipt hash/expiry, concurrency, and idempotency are implemented without PII-bearing status state.
- [ ] All local dispositions, mirror/checkpoint, provider work, cache authority, EventLocation corrections, and receipt state commit atomically per fact.
- [ ] Provider work is specialized, fenced, idempotent, retry/unknown/dead-letter aware, and executed only after local commit.
- [ ] `202` and receipt-authorized `private, no-store` status accurately represent local and provider settlement.
- [ ] No production configuration or dependency registration uses `ApplicationDatabase` / `RetainedAuthority` as behavior modes.
- [ ] Application-side authority mirror/checkpoint remains active in both topologies.
- [ ] Co-located authority append commits independently before application mutation.
- [ ] External authority migration is owned by `Event.MigrationService` and API uses runtime-only credentials.
- [ ] Startup replay runs in both topologies and fails closed for external authority failures.
- [ ] `.env`, `.env.example`, Compose, Aspire, and direct .NET keys are documented and tested.
- [ ] One-container and two-independent-container Testcontainers suites pass.
- [ ] A restored pre-erasure application backup is re-erased from an untouched external authority database.
- [ ] Documentation clearly states that co-located storage cannot survive whole-database restore.
- [ ] The documented pre-v1 reset-only policy has no compatibility shim, silent translation, or agent-driven operator-data deletion.
- [ ] Retention/legal-hold cleanup, credential rotation, authority recovery, topology cutover, RPO/RTO, and unsafe downgrade behavior are documented and tested.
- [ ] Required builds/tests and canonical privacy model checks pass with evidence recorded in `tasks.md`.
- [ ] Scoped diff has no secrets, destructive migrations, PII-bearing authority fields, or unrelated edits.

## 13. Implementation-Agent Contract

Before editing:

1. Read `AGENTS.md`, `docs/QUICK_REFERENCE.md`, the amended `platform-privacy-erasure` intent, and every matching `.claude/rules/*.md` file.
2. Load and follow `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `outbox-pattern`, and `auth-patterns` where their paths/concerns apply.
3. Use the code-review graph before text/file scanning, then inspect source directly only where graph evidence is insufficient.
4. Record baseline SHA, worktree status, and build result in `optional-retained-erasure-authority-tasks.md`.
5. Preserve unrelated user changes and stop if an in-scope file has conflicting uncommitted edits.

While implementing:

- Work in phase order and keep `tasks.md` current after each material task.
- Treat the application ledger as the mirror in both topologies.
- Keep authority append outside the application mutation transaction.
- Keep application mutations, mirror, checkpoint, and outbox atomic.
- Never change historical migrations destructively.
- Update docs/config in the same task as the behavior they describe.
- Add failure-path and idempotency tests with each behavior change.

Before handoff:

- Run each phase gate and the remaining canonical intent checks once.
- Record exact commands, results, and any pre-existing failures in `tasks.md`.
- Review the scoped diff for secrets, PII, privilege leakage, migration collisions, and terminology drift.
- Update all three workstream artifacts so another agent can resume without chat history.

## 14. Progress Reporting Contract

`optional-retained-erasure-authority-tasks.md` is the hot execution ledger.

- Mark a checkbox only when its implementation and colocated tests/docs are complete.
- At every phase gate, record date, commit/SHA if available, commands, pass/fail, and unresolved evidence.
- If design evidence invalidates a decision, update this plan first, add a decision note to the context file, and then continue.
- Put durable, non-obvious findings in `dev/_journal/journal.md` using the canonical finding workflow.
- Never record credentials, raw PII, or private connection details in planning artifacts.

## 15. Maintenance Contract

These three files must remain synchronized:

- `optional-retained-erasure-authority-plan.md`: stable architecture, decisions, phases, acceptance criteria.
- `optional-retained-erasure-authority-context.md`: quick resume, evidence, current decisions, risks, and handoff.
- `optional-retained-erasure-authority-tasks.md`: live checkboxes and verification evidence.

Update the plan when scope, architecture, phases, or acceptance criteria change. Update context when evidence, decisions, constraints, or handoff state changes. Update tasks after every completed task, blocker, or verification run. A changed runtime/config/doc path that is not reflected in the ledger is incomplete work.

## 16. Planning Quality Gates

- [x] Reused the existing stable workstream directory instead of creating a duplicate.
- [x] Verified current options, DI, replay gate, migration ownership, environment wiring, and Testcontainers surfaces.
- [x] Distinguished storage topology from restore guarantee.
- [x] Assigned docs and tests to behavior phases rather than separate documentation/test phases.
- [x] Defined exactly one root Release build and at most one project test at each phase gate.
- [x] Included migration, rollback, secrets, readiness, observability, and independent-restore constraints.
- [ ] Implementation evidence and commands populated in `tasks.md`.

## 17. Potential Additional Risks Discovered During Implementation

Implementation must explicitly investigate and record:

- whether the current co-located repository can be safely reused through a factory-created context or needs a dedicated adapter class;
- whether current health infrastructure can express replay capability without creating a new endpoint;
- whether Compose currently has a migration-service lifecycle suitable for authority ordering or needs the smallest new service definition;
- whether external authority cutover requires sequence reseeding or fact-copy tooling;
- whether deployed environments already rely on the old mode key and need a documented transition window.

These are bounded implementation questions, not permission to add new abstraction layers or expand the privacy-erasure domain.
