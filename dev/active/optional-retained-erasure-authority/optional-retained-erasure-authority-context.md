<!-- ABOUTME: Quick-resume context for the canonical platform User-erasure authority workstream. -->
<!-- ABOUTME: Captures policy, topology, provider settlement, restore, enterprise operations, risks, and next action. -->

# Platform Privacy Erasure Authority — Context

Last Updated: 2026-07-31 Europe/Brussels

## Progress Snapshot

- Status: Phases 1 and 2 remain accepted. OREA-300 and the Phase 5 API/replay/readiness slice are implemented with focused green evidence; OREA-600 hosting/migration orchestration is implemented. OREA-310 provider-backed local clearing is real-PostgreSQL proven, while its first consolidated checkbox has repaired code/static evidence but remains runtime-blocked after the 2026-07-31 reboot because Docker Desktop's QEMU backend still cannot start. Specialized execution, remaining producer/worker fences, fresh-scope ownership reload, AI/Listmonk locator gaps, retention, DR, and full phase gates remain open.
- Active intent: `platform-privacy-erasure` now requires one authority-first workflow, `CoLocated` / `ExternalDatabase`, separate runtime/migrator credentials, and no Blazor authority secret.
- Workstream: canonical owner of complete platform User erasure, authority topology, receipt/status, provider settlement, replay, retention, and restore behavior.
- Supersedes: privacy-erasure implementation ownership in `.omo/plans/platform-wide-privacy-erasure-authority.md` and `dev/active/event-location-privacy/`.
- Runtime changes: configuration exposes only `CoLocated` / `ExternalDatabase`, rejects the legacy mode key, isolates the external connection, and registers one authority-first workflow. `CoLocatedPrivacyErasureAuthorityRepository` now appends through a short-lived `ExploreDbContext` and independently committed transaction while the application ledger remains the replay mirror/checkpoint.
- Verification: Phase 2 is independently confirmed. Current Phase 3 evidence includes provider-materialization characterization 8/8, capture-before-clear ordering and repository contracts 9/9, replay cache convergence 2/2, provider-work Domain lifecycle 10/10, locator protection 1/1, User-PII inventory/Clean Architecture 10/10, and no pending `ExploreDbContext` model changes. The repaired provider-clearing slice passes provider metadata 6/6, Actor lifecycle 18/18, Actor migration 2/2, and provider-work repository 1/1 against PostgreSQL 18; independent re-review confirmed it at 0.98. The OREA-420 review additionally passes durable outbox characterization 9/9, User read fence 2/2, dispatcher 18/18, readiness 4/4, UpdateUser producer fence 3/3, and Clean Architecture 15/15. The canonical root Release build passes with 0 errors. The full Application suite passes 2,943/2,945; its two failures are the documented unrelated EventLocation policy-state and email-metric isolation baselines. AI conversation exact-subject hard delete is implemented, but AI/Listmonk remote locators remain intentionally uninferred.

## Quick Resume

Start here:

1. Read `optional-retained-erasure-authority-plan.md` Sections 1–5 and 13.
2. Read the current `platform-privacy-erasure` intent in `.claude/contract/intents.yaml`.
3. Treat the recorded baseline and Phase 1 gate as complete.
4. Treat Phase 2 and its co-located, schema, and restore evidence records as complete.
5. Repair the host Docker Desktop/QEMU startup failure, require bounded `docker info` to return a populated Server section, then rerun `GlobalLocationPrivacyErasureTests` (required 5/5) and `ExternalDatabasePrivacyErasureAuthorityTests` (required 3/3) before marking the first OREA-310 checkbox. Do not automate kill/reset/prune. After that, continue specialized provider execution under OREA-400/410; do not invent AI or Listmonk locators.

The target is not “two durability modes.” It is one authority-first workflow with two authority-storage topologies:

- `CoLocated`: authority ledger and application mirror reside in the application database.
- `ExternalDatabase`: authority ledger resides in a separately connected database; the application mirror remains local.

Only the second topology can protect against restoring a pre-erasure application backup, and only if the authority database is outside the application database’s restore operation.

Topology is only one part of this workstream. The same plan owns the User fence, policy-versioned saga, complete PII disposition inventory, atomic local erasure, short-lived receipt/status API, specialized provider work, anti-resurrection checks, retention/legal hold, and enterprise recovery contract.

## Key Files

### Governance and planning

- `.claude/contract/intents.yaml`
- `.omo/plans/platform-wide-privacy-erasure-authority.md`
- `dev/active/event-location-privacy/`
- `dev/active/optional-retained-erasure-authority/`

### Configuration and composition

- `src/Explore.Application/Configuration/PrivacyErasureDurabilityOptions.cs`
- `src/Explore.Application/ApplicationServicesRegistration.cs`
- `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs`
- `src/Explore.Persistence/PersistenceServicesRegistration.cs`
- `src/Explore.API/BackgroundServices/PrivacyErasureStartupGate.cs`

### Workflow and persistence

- `src/Explore.Application/Features/Users/Handlers/Commands/DeleteUserCommandHandler.cs`
- `src/Explore.Application/Services/ApplicationDatabasePrivacyErasureWorkflow.cs`
- `src/Explore.Application/Services/RetainedAuthorityPrivacyErasureWorkflow.cs`
- `src/Explore.Application/Services/PrivacyErasureApplier.cs`
- `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/ApplicationDatabasePrivacyErasureLedgerRepository.cs`
- `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/EfCorePrivacyErasureAuthorityRepository.cs`
- `src/Explore.Persistence/Privacy/ErasureAuthority/PrivacyErasureAuthorityDbContext.cs`
- `src/Explore.Persistence/Migrations/`
- `src/Explore.Persistence/Migrations/PrivacyErasureAuthority/`
- `src/Explore.Domain/PrivacyErasureSaga.cs`
- `src/Explore.Domain/PrivacyErasurePolicyCoverage.cs`
- `src/Explore.Domain/PrivacyErasureIntent.cs`
- `src/Explore.Domain/PrivacyErasureReplayCheckpoint.cs`
- `tests/Event.Architecture.Tests/Privacy/UserPiiInventory.cs`
- `tests/Event.Architecture.Tests/Privacy/UserPiiInventoryArchitectureTests.cs`

### Hosting and operations

- `src/Event.MigrationService/Program.cs`
- `src/Event.MigrationService/Worker.cs`
- `src/Explore.AppHost/AppHost.cs`
- `docker-compose.yml`
- `.env`
- `.env.example`
- `src/Explore.Secrets/Bootstrap/BootstrapSecretLoader.cs`

### Tests

- `tests/Event.Application.UnitTests/Configuration/PrivacyErasureDurabilityOptionsTests.cs`
- `tests/Event.Application.UnitTests/Configuration/PrivacyErasureModelCompositionTests.cs`
- `tests/Event.API.IntegrationTests/Privacy/PrivacyErasureStartupGateTests.cs`
- `tests/Event.Persistence.IntegrationTests/Privacy/GlobalLocationPrivacyErasureTests.cs`
- `tests/Event.Persistence.IntegrationTests/Privacy/PrivacyErasureAuthorityCompositionValidationTests.cs`
- `tests/Event.Persistence.IntegrationTests/Privacy/PrivacyErasureAuthorityDbContextFactoryTests.cs`

## Current-State Findings

1. `PrivacyErasureDurabilityOptions` currently defines `ApplicationDatabase` and `RetainedAuthority`, with `ApplicationDatabase` as default.
2. Application DI selects between two workflow implementations; infrastructure DI registers replay only in retained mode.
3. The startup gate skips replay in application-database mode.
4. The retained workflow already has the desired shape: commit authority fact, then replay/apply.
5. `PrivacyErasureApplier` already writes the application-side mirror and checkpoint within the application transaction.
6. The application and dedicated authority migrations both own `privacy_erasure_authority` tables. Applying both migration sets to one database would collide.
7. The dedicated migration additionally owns PostgreSQL functions, roles, grants, and tamper-resistant access rules.
8. `Event.MigrationService` currently does not migrate `PrivacyErasureAuthorityDbContext`.
9. `.env` and `.env.example` mention the old mode, while Compose does not provide complete authority wiring.
10. The bootstrap secret loader is specialized for the main application PostgreSQL settings. The lean plan keeps authority as a named connection string supplied separately to API and migration processes.
11. Existing persistence integration infrastructure already starts two independent PostgreSQL containers. Extend it; do not create a competing fixture stack.
12. Some test assertions still reference `location_privacy_authority`; confirm and replace stale generalized-schema names during the persistence phase.
13. `tests/Event.Architecture.Tests/Privacy/UserPiiInventory.cs` classifies broad User-PII families across identity/authentication, tenancy/membership, registration/contact sharing, notifications/email/web-push, AI/webhook/report/audit/configuration, storage, federation, and external providers; completeness against current source must be re-proven.
14. Generalized authority, applier, startup-gate, saga/policy-coverage, and inventory pieces exist, but code presence is not consolidated-plan acceptance evidence.
15. Event Location contributes only a typed disposition adapter: exact subject/tenant predicates, owned Home/room tombstoning, affected `EventLocation` correction intents, stable idempotency, and integration tests. This workstream owns the platform orchestration.

## OREA-310 Remaining Disposition Matrix

Provider-independent dispositions implemented in the application transaction now cover `UserPii`, authentication tokens, actor identity links and key stores, owned Home/location data, memberships and roles, preferences, registrations, contact-share snapshots, local notifications, fanout cursors, idempotency responses, AI consent grants, user-owned external API keys, nullable report/moderation/audit references, and reporter-owned report evidence.

Remaining local/provider gaps:

| Family | Exact gap | Required next change |
|---|---|---|
| Provider-backed local copies | Implemented and PostgreSQL-proven for exact-subject ATProto ownership, two-tenant webhook snapshots, reporter-owned Osprey/Coop links, replay deduplication, and unrelated preservation; other listed families retain prior focused evidence | Continue broader rollback/former-membership convergence under OREA-320 and specialized execution under OREA-400/410 |
| AI/Listmonk copies | Current schemas lack trustworthy provider context/subscriber identifiers | Add typed source fields only when provider contracts define authoritative remote locators; do not infer them |

Resolved ownership/schema decisions:

- `OrganizationPii`, `Organization.WebsiteUrl`, and `OrganizationSetting.Value` are organization-owned data. `Organization.ActorId` is a publishing identity association, not User ownership, and ordinary organization membership never transfers organization data into a User erasure.
- Migration `20260723083304_AnonymizeRetainedAuditActors` permits exact-subject unlinking across retained audit families. Active support sessions are revoked before unlinking, shared review content uses the `Deleted user` label, and downgrade refuses to invent replacement User IDs after anonymization.

External locator gaps:

| Provider family | Current locator source | Why `PrivacyErasureProviderWork.TargetId` is insufficient |
|---|---|---|
| Keycloak | `UserExternalLogin.ProviderKey` | Provider key is a bounded string, not necessarily a GUID |
| ATProto | `ActorPii.Did` plus actor/user ownership | DID is a string and is cleared during local anonymization |
| Listmonk / SMTP | email-dispatch subscriber/delivery metadata | Subscriber address and provider message IDs are not GUID targets; SMTP recall is explicitly unsupported |
| Web Push | subscription endpoint | Endpoint is a secret-bearing URL string |
| Object storage | object key/provider URI | Remote delete requires the object key after local metadata is removed |
| Svix / webhook | provider event/application/endpoint identifiers | Provider identifiers and credential references are bounded strings |
| AI providers | provider conversation/response context | Current work row has no typed provider context reference |
| Osprey / Coop | event-report provider identifiers | Report GUID can remain the local target, but provider reconciliation identifiers are strings |

The minimal safe follow-up is an encrypted, short-lived typed locator owned by specialized provider work and destroyed after terminal settlement or expiry. Retaining ordinary source PII until remote completion would violate atomic local settlement.

Current materialization boundary:

- `UserLocationPrivacyErasureRepository.GetProviderCandidatesAsync` enumerates only sources with an exact User ownership path and a usable remote locator: Keycloak external logins, ATProto actors, Web Push subscriptions, actor-owned storage objects, SMTP provider messages, user-owned webhook endpoints, and Osprey/Coop report links.
- `PrivacyErasureApplier` captures those candidates before local disposition methods run, protects locators with purpose-bound ASP.NET Core Data Protection, persists work in the caller's serializable transaction, and passes the real work count to `MarkLocalSettled`.
- `TargetId` remains the stable local source UUIDv7. Remote string/URL identifiers live only in versioned, cryptographically time-limited ciphertext with configurable `ProviderLocatorLifetime`; success, reconciliation, or expiry clears ciphertext.
- `PrivacyErasureProviderWorkRepository.AddMissingAsync` deduplicates the same semantic tuple enforced by the database unique index and returns the total represented work count to the saga, so replay does not depend on newly generated work IDs.
- Expired rows transition to bounded `locator_expired` dead-letter state before claiming. Migration `20260723091627_ExpirePrivacyErasureProviderLocators` updates the lifecycle constraint to allow dead-letter evidence without retained ciphertext.
- `PrivacyErasureApplier` flushes provider work before the dedicated adapter executes immediate local SQL. Local rows without usable remote locators are still erased: locator presence gates only provider-work creation, never local disposition.
- Provider-backed local dispositions hard-delete external logins, Web Push rows, notification-delivery links, and email snapshots; tombstone actor-owned storage; archive/scrub webhook endpoint, target, and consumer metadata while severing User ownership; and clear reporter-owned external report-link metadata. Actors without an `ActorPii` row still lose User/PDS/custody links.
- Final review kept phase boundaries explicit: the durable post-commit cache-convergence finding is resolved, while Keycloak platform-managed delete versus external unlink classification and provider adapters remain unresolved `OREA-410`. PostgreSQL acceptance now includes ATProto, Osprey/Coop, and webhook target-snapshot fixtures with exact-subject and unrelated-row assertions.
- OREA-420 cache convergence now uses a payload-free generic outbox row committed with local erasure. Its dispatcher retries user and broad event cache invalidation, readiness degrades on incomplete/dead-lettered convergence, and `GetUserRequestHandler` checks the persisted fence before and after cache access so a failed invalidation cannot serve stale profile PII. Independent review corrected the old fail-closed replay test to the durable-convergence contract and added direct cache-race and dead-letter replay coverage. Other producer/worker/remote-dispatch fences and fresh-scope ownership reload remain open.
- The first conflict-free local producer fence is implemented in `UpdateUserCommandHandler`: its transaction checks `IPrivacyErasureStateRepository` before loading or mutating the User, so fenced subjects cannot recreate names or profile-storage ownership and receive the existing non-disclosing not-found response. The regression test attempts both name recreation and actor/profile-storage relinking and proves all repositories remain untouched. Focused handler tests pass 3/3, the Application build is clean, Clean Architecture passes 15/15, the canonical root Release build passes with 0 errors, and independent scoped review passes. `SyncUserCommandHandler` remains untouched while the concurrent ATProto agent owns it.
- Registration creation now checks persisted erasure state before validation, before returning any pre-transaction result, and inside the serializable registration transaction. This prevents fenced Users from receiving detailed validation/state responses or recreating registration, notification, provider-outbox, consent, and webhook state through the covered pre-write races. Focused tests pass 21/21 after repairing the first review's validation-disclosure finding; the final independent review passes with no blocking findings.
- Storage upload-session creation now checks the captured authenticated User before validation/policy disclosure and again inside a serializable quota-reservation transaction. A fenced User cannot receive detailed early failures, replay an existing session, reserve quota, or persist filename/object metadata. Focused tests pass 28/28; `Explore.Application` builds with 0 warnings/0 errors, Clean Architecture passes 15/15, the canonical root Release build passes with 0 errors, and independent review passes with no blocking findings.
- Appearance-preference updates now check persisted state before validation, mask detailed raced failures, use a cancellable no-tracking theme lookup, and recheck inside one serializable transaction containing every `UserPreference` create/update/remove. Cache invalidation occurs only after a successful commit, and the transaction uses one retry-stable timestamp. Focused tests pass 11/11, Persistence builds with 0 errors, and independent re-review passes with no blocking findings.
- Current-user notification preference matrix updates now check persisted state before metadata/lock validation, make the final raced failure decision inside a serializable scope, and recheck before all user-scoped channel preference upserts. Focused tests pass 9/9, including two-cell serializable retry replay; independent re-review passes after repairing the original masking and retry-test findings. The Application build is clean and the architecture suite passes 301/302 with one documented skip; the latest root build is blocked only by concurrent ATProto tests referencing the absent `AtprotoThumbnailBlobGateway`, while the earlier integrated OREA root build passed.
- Provider-work `Unknown` reconciliation now persists under the exact lease fence and atomically advances saga progress only for the first completed reconciliation. The real PostgreSQL selector passes and independent verification is confirmed; the specialized provider worker, retry/backoff owner, and adapters remain open.
- Recipient materialization and legacy event fanout now fence before recipient PII reads and recheck inside their serializable graph/write boundaries. Skipped recipients advance cursors without creating notification, email, or Web Push rows. Focused and real persistence paths pass, with independent confirmation.
- Local webhook sending now reloads the active tenant/target/lease/fence claim and endpoint before any payload, secret, SSRF, or HTTP work. The real archive-after-claim PostgreSQL canary and independent review pass.
- AI run execution now fences the loaded conversation owner before provider work and the reloaded owner before provider-output persistence. The guard is independently confirmed, while AI local/provider disposition remains open.
- Web Push active-claim and direct notification orchestration fences are implemented and focused-unit green; their PostgreSQL selectors still require a current independent rerun before acceptance.
- Receipt OpenAPI parity is independently confirmed for the custom `ErasureReceipt` authorization scheme. The lifecycle-table schema artifact remains intentionally red because no canonical `schemas/islamu-event.md` generator exists; OREA-700 is still open.
- Storage finalization requires a dedicated follow-up rather than a guard-only patch. The current provider write sits between local transactions and the resulting `StorageObject` has no Actor ownership link, while erasure candidate capture requires `StorageObject.Actor.UserId`. The finalizer must durably couple exact User ownership and cleanup intent to remote-write uncertainty before it can truthfully enforce the OREA-420 fence.

## Decisions

### Accepted

- This directory is the sole active implementation workstream for platform User erasure and its authority.
- `User` is the only executable subject kind; Organization and Tenant require separate typed policies and handlers.
- A machine-checked inventory proves completeness but never drives runtime SQL or arbitrary instructions.
- The DBML schema is maintained with its EF Core model or migration and guarded by the focused architecture contract; a partial snapshot parser is not a supported tool.
- Fence before enumeration; apply complete local dispositions, mirror/checkpoint, provider work, cache authority, and receipt state in one serializable application transaction.
- Return `202` with a once-revealed short-lived receipt; persist only its fixed-time-verifiable hash and serve status as `private, no-store`.
- Provider cleanup is specialized, idempotent, fenced, retry/unknown/dead-letter aware, and always after local commit.
- Delete only platform-managed upstream identities; revoke or unlink identities the platform does not own.
- Remove production behavior-mode selection.
- Introduce `PrivacyErasure:Authority:Topology` with `CoLocated` and `ExternalDatabase`.
- Default to `CoLocated`, and show that choice explicitly in sample env files.
- Reject a present legacy `PrivacyErasure:Durability:Mode` key with upgrade guidance.
- Never infer external topology from a connection string.
- Keep one authority-first workflow and run startup replay in both topologies.
- Keep the application-side mirror/checkpoint in both topologies.
- Use a separately committed, short-lived application context for the co-located authority append.
- Keep dedicated authority migrations out of the application database.
- Use different public runtime/migrator secret variables, each mapped to `ConnectionStrings__PrivacyErasureAuthority` only in its owning process.
- In Aspire, create the distinct authority PostgreSQL resource in every local-data profile when `ExternalDatabase` is selected; profiles without local data use operator-provided infrastructure.
- Test external restore safety with two independent PostgreSQL containers and a real pre-erasure application backup restore.
- Report `restoreReplayProtection=false` for co-located storage.
- Use a pre-v1 reset-only policy for the removed behavior-mode contract. Breaking compatibility is accepted; silent data loss and agent-driven deletion of operator resources are not.
- Retain linkable authority identifiers only through the maximum resurrection-capable backup horizon plus the approved margin; pseudonymize legal-hold evidence and destroy expired credentials/receipts.
- The approved retention baseline is 365 days plus a 30-day safety margin. Only an expired contiguous authority prefix may compact behind a metadata-only floor; counters never decrease and retained facts remain contiguous above the floor.
- A restored application checkpoint below the compaction floor is unsupported and must fail readiness before API/BFF/MCP/workers start. No subject-bearing replay snapshot or pseudonymous denylist is introduced.
- Legal holds preserve non-relinkable pseudonymized evidence under bounded reason codes with mandatory review/expiry; they do not extend the old-backup replay horizon. `ExternalDatabase` to `CoLocated` downgrade remains unsupported.

### Rejected

- Keeping `ApplicationDatabase` as a production workflow mode.
- Auto-selecting external storage when a connection exists.
- Applying the dedicated authority migration to the application database.
- Giving API runtime credentials migration privileges.
- Using a distributed transaction.
- Claiming that a second database automatically means an independent restore domain.
- Rebuilding Testcontainers infrastructure already present in the persistence integration project.
- Generic provider plugins, arbitrary JSON/table/column erasure instructions, and reflection-driven destructive SQL.
- Inline provider calls or synchronous claims that external deletion is complete.
- Compatibility shims for the old behavior-mode configuration.

## Configuration Target

Public self-host variables:

```dotenv
PRIVACY_ERASURE_AUTHORITY_TOPOLOGY=CoLocated
PRIVACY_ERASURE_AUTHORITY_RUNTIME_CONNECTION_STRING=
PRIVACY_ERASURE_AUTHORITY_MIGRATOR_CONNECTION_STRING=
```

Process mapping:

- API receives topology plus runtime authority connection.
- Migration service receives topology plus migrator authority connection.
- Blazor receives neither authority secret.
- In `CoLocated`, neither authority connection is required or opened.
- In `ExternalDatabase`, the process-specific connection is mandatory and errors must name only the missing configuration key.

## Canonical Erasure Flow

1. Authorize the request, append/reuse one typed policy-versioned authority fact, and fence the User before PII enumeration.
2. Apply every classified local disposition in one serializable application transaction.
3. In that transaction, confirm the application mirror/checkpoint, materialize specialized provider work and EventLocation corrections, invalidate cache authority, and persist receipt/status.
4. Commit locally, return `202` with the once-revealed receipt, then let specialized fenced workers settle remote work.
5. Keep `Unknown` provider outcomes reconcilable; never restore local PII or claim remote completion prematurely.
6. Before traffic after startup/restore, replay every authority fact not covered by the current policy version and reject PII recreation for fenced/deleted users.

## Invariants and Constraints

- Authority facts contain bounded metadata only, never live PII, identifiers, selectors, or payloads.
- Every durable User-PII copy and producer maps to exactly one compiled disposition/fence owner in the machine inventory.
- The User is fenced before PII enumeration; shared write, worker, cache, and dispatch boundaries prevent recreation.
- Append authority fact before application mutation; provider calls stay outside the application transaction.
- Application dispositions, mirror, checkpoint, provider work, cache authority, EventLocation corrections, and receipt state remain atomic and serializable.
- Replay is idempotent and monotonic.
- Repositories return entities, not DTOs.
- EF migrations are additive; never edit deployed migration history destructively.
- External runtime access remains function-only; migrator credentials stay outside API/Blazor.
- Logs, health, errors, examples, tests, and planning artifacts never expose credentials or PII.
- Preserve unrelated dirty-worktree changes.
- Every changed source file must retain the two-line `ABOUTME:` header requirement.
- Every delivery and reconciliation opens a fresh scope and reloads persisted tenant/subject ownership; queued/caller identifiers are never authority.
- Sensitive caches are `no-store` or partitioned by tenant/subject/policy version; failed invalidation cannot serve stale PII.

## Restore Guarantee Boundary

`CoLocated` protects against an application transaction failure after the authority append because the append commits separately. It does not protect against restoring the entire application database from a backup created before the erasure.

`ExternalDatabase` protects against that restore only when the application database is restored without also rolling back the authority database. Two database names on the same PostgreSQL server are operationally valid, but a cluster/volume snapshot that restores both together defeats the guarantee.

## Validation Baseline

Planning evidence inspected:

- options and DI selection;
- workflow/applier/repository boundaries;
- application and dedicated authority migrations;
- migration-service registration;
- env/Compose/AppHost/secrets surfaces;
- unit, API integration, and persistence Testcontainers coverage;
- canonical intent, rules, operations, testing, and implementation-plan guidance.

Planning-session baseline:

```bash
dotnet build --configuration Release --verbosity quiet
```

Result: passed on 2026-07-22 with 26 projects, 0 errors, and 41 warnings. The warnings include pre-existing `NU1903` advisories for `System.Security.Cryptography.Xml` 10.0.7. Record a fresh result plus starting SHA/status in `optional-retained-erasure-authority-tasks.md` immediately before runtime edits.

The Senior CTO planning hook is a known non-green baseline: 286 architecture tests, 282 passed, 3 unrelated existing failures, and 1 skipped. The failures concern repository naming, the organization-centric scope-file guardrail finding multiple matches, and explicit HATEOAS permission metadata in existing EventReport/EmailDispatch policies.

## Risks to Watch

- A co-located adapter accidentally sharing the applier transaction.
- Authority/app migrations targeting the same physical database.
- Old mode keys silently changing the deployment guarantee.
- External secrets being passed to the wrong process.
- A synthetic restore test that only deletes rows instead of exercising PostgreSQL restore behavior.
- Active docs/plans retaining contradictory `ApplicationDatabase` guidance.
- Existing deployments needing sequence/fact migration during co-located-to-external cutover.
- An unclassified PII family or producer escaping erasure/fence coverage.
- Provider `Unknown` being mistaken for success or blindly retried.
- Receipt, health, or telemetry becoming a new identifier disclosure surface.
- Authority loss/corruption, credential rotation, topology cutover, unsafe downgrade, or RPO/RTO procedures remaining untested.
- The pre-v1 reset-only policy being documented ambiguously enough to cause operator data loss.

## Handoff

The OREA-310 provider-clearing canary is independently accepted. The first broader local-disposition checkbox now has repaired ATProto inventory paths, valid Actor fixture ownership, persisted authentication/tenant/Home assertions, the corrected six-argument external-authority call, and append-only EventLocation audit handling. Its focused inventory gate passes 1/1 and independent source review confirms the repair, but the 2026-07-31 post-reboot `docker info` probe still returns no Server because Docker Desktop's QEMU backend exits during startup. The mandatory repeated-failure reviewer rejects completion with `needs-human-review` at 0.99 confidence. Do not automate kill/reset/prune or dispatch another selector. Resume only after a human repairs Docker Desktop/QEMU and a bounded `docker info` returns a populated Server section, then require GlobalLocation 5/5 and external authority 3/3. `RegistrationOrderPii` remains the next-checkbox implementation gap and must not be falsely classified. AI/Listmonk locators remain intentionally uninferred.

## Maintenance Contract

- Keep progress and the next action at the top.
- Add only evidence needed to resume; durable discoveries belong in `dev/_journal/journal.md`.
- Synchronize terminology and status with the plan and tasks ledger after each phase.
- Never store secret values, private connection details, or erased data here.
