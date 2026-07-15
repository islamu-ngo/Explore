<!-- ABOUTME: Records the Phase 0B through Phase 3 webhook migration, timing, lock, and restore rehearsals. -->
<!-- ABOUTME: Binds release decisions to reproducible PostgreSQL 18 commands and semantic/data checks. -->

# Webhook Migration Rehearsal Evidence

Verified: 2026-07-14 Europe/Brussels

## Scope

- PostgreSQL image: `postgres:18-alpine`
- Starting migration: `20260712144721_AddManagedTenantProvisioningOperationOutboxPointer`
- Final migration: `20260714115533_EnforceWebhookConfigurationSnapshots`
- Last full volume/backup/restore rehearsal final migration:
  `20260714115533_EnforceWebhookConfigurationSnapshots`
- Deployment artifact: EF Core idempotent SQL generated with `dotnet ef migrations script --idempotent`
- Representative volume: 10,000 legacy incoming webhook rows, including 100 null legacy
  JSON payloads and 1,000 rows whose legacy `verified_at` was null
- Provider-link retirement volume: 10,002 legacy Svix links and messages, including one
  evidence-backed queued row, one unresolved row, and 10,000 pending rows

## Results

| Check | Result |
|---|---|
| Clean install | Passed in 13.179 seconds |
| Empty committed-baseline install | Passed in 1.838 seconds |
| Empty baseline-to-current upgrade | Passed in 0.703 seconds |
| 10,000-row baseline-to-current upgrade | Passed in under 1 second |
| Webhook semantic schema convergence | 890 catalog rows; SHA-256 `49b7c637bbbffec5a64cbee91cb1273bf11539befe0e8cf0faf9c496a5214d44` for clean, baseline, volume, and restore |
| Legacy payload classification | 10,000/10,000 `LEGACY_JSON_CANONICALIZED`; positive byte length and non-null verification time |
| Processing generation backfill | 10,000/10,000 rows at generation 1 |
| Lock observation | No waiting/held sampled lock on the three inbound webhook tables at 250 ms; the migration completed below the next sampling interval |
| Backup | PostgreSQL custom-format dump, 1.3 MiB, completed in 0.281 seconds |
| Restore | Clean-database restore completed in 3.876 seconds |
| Data checksum | Before/after count `10000`; MD5 `e0c37d4774413141bd9df31c4f503fdb` |
| Last full volume/restore migration history | 44 applied migrations through binding identity normalization; final migration identical before/after restore |
| Current source migration chain | 51 generated migrations; configuration-snapshot enforcement is final and EF reports no pending model changes |
| Restored idempotent reapply | Passed as a no-op in 0.366 seconds |
| Binding identity normalization Up/Down | Passed in 5.696 seconds; canonical completed-bootstrap identity applied, stale ownership invalidated, and exact prior state restored from migration audit evidence |
| 10,002-row provider-link end-to-end rehearsal | Passed in 2 minutes 34.211 seconds, including isolated database creation, current upgrade through identity normalization, lock monitoring, backup, restore, and verification |
| Provider-link classification | 10,002/10,002 delivery plans, provider publications, and normalized publication attempts; queued evidence preserved and unresolved rows require manual reconciliation |
| Legacy provider authority | One trimmed/normalized `LegacyUnverified` binding; disabled by default; provider endpoint identity copied to the endpoint snapshot |
| Provider-link lock observation | Zero waiting locks sampled every 250 ms across the source and destination webhook tables |
| Provider-link backup/restore | PostgreSQL custom-format dump and clean-database restore passed; restored publication ID/status checksum identical |
| Provider-link retirement | `webhook_provider_links` absent after upgrade and after restore |
| Full persistence regression | 329/329 passed against PostgreSQL 18 |
| Capability normalization | EF CLI generated the lookup table, unique code index, and bounded `0..4095` provider/governance bitmask constraints; lookup/DTO parity passes and `dotnet ef migrations has-pending-model-changes` reports no drift |
| Capability-aware historical boundaries | Passed: isolated databases migrate to the current chain, seed current lookups, use generated Down migrations to the pre-capability boundary, then replay the binding-identity or 10,002-row provider-link Up/backup/restore verification |
| Configuration-snapshot enforcement | EF CLI generated positive consumer/endpoint configuration versions, dedicated UTC endpoint secret activation, and the normalized pending-work-decision lookup; consumer and endpoint versions are optimistic-concurrency tokens in the generated designer/snapshot |
| Latest Phase 3 volume replay | Passed against `20260714115533_EnforceWebhookConfigurationSnapshots`: 10,002 snapshots/publications/attempts, zero sampled waiting locks, identical restored checksum, and no retired provider-link table after upgrade or restore |

The first physical signature comparison differed only in column ordinal positions because
`pg_restore` recreates columns in dump order. The semantic signature intentionally compares
column names/types/nullability/defaults, constraints, and indexes; all four databases match.

The provider-link rehearsal starts at
`20260713232047_NormalizeWebhookProviderPublicationAttemptOutcomes`, runs the generated
prepare/backfill/retire sequence in one bounded upgrade, and aborts before table removal for
unsupported providers, conflicting consumer ownership, ambiguous application/endpoint/message
identities, blank external identifiers, or evidence with no durable destination.

The subsequent identity-normalization migration requires exactly one completed
`instance_bootstrap_states` row whenever bindings exist. It records reversible audit evidence,
recomputes `islamu-{instance:N}-consumer-{consumer:N}`, invalidates stale verification, and
never contacts the provider. Its canonical UUID selection uses portable ordered-row SQL rather
than a PostgreSQL-unsupported UUID aggregate.

The final capability-normalization migration is schema-only. It creates the independent
`webhook_provider_capabilities` metadata table and constrains both persisted capability masks to
the twelve known single-bit values represented by `0..4095`. Runtime seeding remains idempotent
and owns the twelve lookup rows; provider proof remains versioned application authority rather
than mutable model seed data. Migration creation, removal of the empty predecessor, snapshot
updates, and model-drift verification were performed exclusively through `dotnet ef`.

The final configuration-snapshot migration is additive. It adds positive configuration-version
columns to consumers and endpoints, a dedicated UTC signing-secret activation timestamp, and the
normalized `webhook_pending_work_decisions` lookup with a unique code index. EF concurrency-token
metadata does not require extra PostgreSQL DDL, but is present in the CLI-generated designer and
model snapshot. The migration was removed and regenerated through `dotnet ef` after that metadata
was finalized; no migration, designer, or snapshot file was hand-edited. Real PostgreSQL tests
prove simultaneous writers cannot overwrite either endpoint configuration or consumer provider
mode, and the 10,002-row historical-boundary/backup/restore rehearsal passes through this migration.

The first full persistence run after capability normalization passed 327/329. Both failures were
historical-boundary setup failures: the current runtime seeder correctly queried
`webhook_provider_capabilities`, but those isolated databases had intentionally stopped before
the migration that creates it. The tests now migrate and seed at the current schema, clear the
tracker, and use EF's generated Down chain to reach the intended historical boundary. This keeps
runtime seeding strict and also verifies rollback compatibility. The two focused reruns and the
complete 329-case PostgreSQL lane pass; no migration source or snapshot was hand-edited.

## Operator Decision

- Require a 15-minute webhook write-maintenance window and a further 15-minute restore buffer.
- Rehearse production-like counts before release. Stop and split resumable backfill from DDL
  if projected execution exceeds five minutes or lock sampling shows waiting writers.
- Before reopening traffic, restore the verified backup on any failed check.
- After traffic resumes, forward-fix with a new additive migration while writers are paused;
  do not destructively roll back or delete unresolved evidence.

## Cleanup

The disposable container, databases, dump, generated scripts, and temporary credentials are
deleted after the final evidence capture. No production or shared development database is used.
