ABOUTME: Operator runbook for backups, restores, upgrades, and rollback decisions.
ABOUTME: Grounds release operations in Docker Compose, PostgreSQL, Keycloak, object storage, and migration behavior.

# Backup, Restore, And Upgrade

> **Audience:** Operators
> **Status:** Implemented
> **Owner:** Platform/Ops
> **Last Verified:** 2026-07-23
> **Source Anchors:** `docker-compose.yml`, `Event.MigrationService/Worker.cs`, `Explore.API/Program.cs`, `PrivacyErasureStartupGate.cs`, `PrivacyErasureReplayService.cs`, `GlobalLocationPrivacyErasureTests.cs`, `docs/SELF_HOSTING.md`, `docs/CONFIGURATION.md`, `docs/SECRETS.md`

This runbook covers self-hosted deployments using the repository Docker Compose topology. Treat every upgrade as a data operation first and an image rollout second.

## Pre-v1 Privacy-erasure Reset Policy

The removed `PrivacyErasure:Durability:Mode` contract has no compatibility
shim or silent translation. Its presence blocks startup. The only supported
transition for this pre-v1 development contract is an operator-managed reset,
and only when all of the following are true:

1. The deployment is explicitly confirmed to be pre-v1 and reset-eligible.
2. The operator accepts rebuilding the application database from the current
   generated application migration and, for `ExternalDatabase`, the dedicated
   authority migration in a different physical PostgreSQL database.
3. A backup or export exists for every value the operator must retain, and the
   operator has verified that the retained artifacts are readable or restorable.
4. The legacy key is removed and
   `PrivacyErasure:Authority:Topology=CoLocated|ExternalDatabase` is selected
   explicitly.

If any prerequisite is false or unknown, stop and preserve the database,
containers, volumes, and backups for a separately reviewed forward-migration
or fact-copy plan. Application code and implementation agents never perform
the reset or delete operator resources. A successful EF migration message is
not reset evidence: verify the target database identity, migration history,
`privacy_erasure_authority` tables, and, for an external authority, the approved
functions and function-only runtime grants before starting the API.

## What Must Be Backed Up

| Asset | Compose Anchor | Why It Matters |
|---|---|---|
| Application PostgreSQL data | `postgres` volume `postgres_data` | Tenants, events, users, settings, outbox, and data-protection keys when stored through EF contexts. |
| Privacy-erasure authority PostgreSQL data | `CoLocated`: application database; `ExternalDatabase`: independently operated database configured by `ConnectionStrings:PrivacyErasureAuthority` | Typed immutable erasure facts and the monotonic authority counter drive replay. Only an external authority excluded from the application restore operation protects against an older application restore. |
| Keycloak PostgreSQL data | `keycloak-db` volume `keycloak_data` | Realms, clients, roles, users, and login configuration. |
| Object storage | API volume `local_storage_data`; optional `minio` volume `minio_data` or external S3 bucket when selected | Uploaded files, images, and storage-backed assets. |
| Secrets and environment | `.env`, secret-provider project, Keycloak client secrets | Required to recreate the same runtime identity and storage bindings. |
| Release manifest | image tags, commit SHA, migration state, docs version | Required for rollback and audit. |

Do not treat Docker image tags alone as a backup. Database schema and secret-provider state must match the application version being restored.

## Backup Procedure

1. Announce a maintenance window or switch traffic to read-only behavior if available.
2. Record the current release manifest:
   - application image tags or local build commit;
   - `docker compose config --services` output;
   - current `.env` key names without printing secret values;
   - active `DEPLOYMENT_MODE` and optional profiles (`storage`, `authz`).
3. Create PostgreSQL backups for the application and Keycloak databases:

   ```bash
   docker compose exec postgres sh -c 'pg_dump -U "$POSTGRES_USER" "$POSTGRES_DB"' > backup-explore.sql
   docker compose exec keycloak-db sh -c 'pg_dump -U "$POSTGRES_USER" "$POSTGRES_DB"' > backup-keycloak.sql
   ```

4. Back up the retained erasure authority independently, preferably from a separate physical cluster and into a separate backup repository. Use its backup role, not the function-only runtime role, and use a custom-format dump so restore can be rehearsed:

   ```bash
   pg_dump --format=custom --file=backup-privacy-erasure-authority.dump "$REDACTED_AUTHORITY_ADMIN_DSN"
   ```

   Record the authority watermark, application watermark, backup timestamp, dump SHA-256, and restore drill identifier in the release manifest. Never record the DSN or opaque owner/location/intent IDs.

5. Back up object storage:
   - for local-first Compose, copy the API `local_storage_data` named volume or the deployment-managed `Storage:Local:RootPath`;
   - for Aspire/local-dev state that must be preserved, copy `storage-data/aspire-local`;
   - for local MinIO/S3-compatible mode, copy the `minio_data` volume or use an S3-compatible sync tool;
   - for external S3, use provider-native versioning or bucket replication.
6. Export secret-provider configuration or capture the exact secret paths and key names used by the release.
7. Store backups outside the host running Docker Compose. Keep the erasure-authority dump independent from application backup retention and for at least as long as any restorable application backup exists.
8. Verify both restores and the authority-over-application replay in a non-production environment before relying on the backups for an upgrade.

## Restore Procedure

Choose the contract before restoring:

- `CoLocated` has `restoreReplayProtection=false`. Back up and restore the
  application and co-located authority together. Its authority-first commit
  protects application-transaction rollback, not a whole-database restore to
  pre-erasure state. Do not claim that replay can recover authority facts that
  the same restore removed.
- `ExternalDatabase` has `restoreReplayProtection=true` as a capability flag,
  conditional on the authority database being outside the application restore
  operation. If both databases or their shared storage snapshot are restored
  together, the guarantee does not apply.

1. Stop application traffic before restoring:

   ```bash
   docker compose down
   ```

2. Restore and verify the retained erasure-authority database first. Never overwrite it with application backup content and never “repair” it by deleting or renumbering intents.
3. Restore application PostgreSQL data into a clean volume or clean database. It may legitimately contain no checkpoint or a checkpoint behind the authority watermark.
4. Restore Keycloak data before starting Keycloak-dependent application services.
5. Restore object storage before user-facing traffic resumes. For local-first Compose, restore `local_storage_data` or the configured `Storage:Local:RootPath`; for S3-compatible mode, restore `minio_data` or the external bucket.
6. Restore the matching `.env` and secret-provider values, including the retained-authority connection secret.
7. Start dependencies first, then the API. Do not start or expose the BFF until the API replay gate has succeeded:

   ```bash
   docker compose up -d postgres redis keycloak-db keycloak
   docker compose up -d islamu-event-api
   ```

8. Compare the privacy-erasure authority watermark with the application replay checkpoint. A fresh database must replay from zero; an older database must advance from its latest valid checkpoint. Confirm restored PII canaries are erased, local mirror/checkpoint rows converge once, cache invalidation completed, and PII-free outbox work exists before the first successful `/health` response.
9. Start the BFF only after the API is ready, then check `/alive` and `/health` on both hosts.
10. Let the normal idempotent outbox processor drain `LocationPiiErased` and `LocationPrivacyCorrectionRequested`. Inspect failed/dead-letter rows and verify downstream cache/search/index projections before reopening general traffic. No exact-location search index exists in the current release; do not invent an ad hoc rebuild path.
11. Validate login, tenant resolution, event browsing, and file access.

### Privacy-erasure retention and incident recovery

The authority currently has no update, delete, or pruning surface. Retain its immutable intents and counter indefinitely; at minimum, they must outlive every application backup, replica snapshot, object-store backup, and disaster-recovery artifact that could reintroduce pre-erasure state. Application checkpoints are also append-only evidence. General correction outbox retention follows the outbox runbook, but failed and dead-lettered corrections remain until operators reconcile them.

Below-floor compaction and DR rehearsals are still pending until shipped; do not infer RPO/RTO or compaction guarantees from this runbook.

If startup replay fails:

1. Keep the API and BFF out of service. A failed process, refused socket, and absent readiness response are the expected safe state.
2. Record only the bounded failure type, release identifier, authority/application watermarks, checkpoint count, and correction counts. Do not record connection details or opaque IDs.
3. For authority unavailability, restore connectivity or the independently verified authority backup, then retry startup.
4. If the authority watermark is behind the application checkpoint, or a sequence/checkpoint integrity check fails, stop. Restore an authority backup that contains the checkpointed fact and every later retained fact. Never bypass the gate, rewrite the counter, delete a local checkpoint, or synthesize an intent from restored application PII.
5. For application replay failure, restore the application backup into a clean database and overlay the unchanged authority again. The per-intent application transaction is atomic, so restart resumes from the last committed checkpoint without duplicate tombstones or outbox rows.
6. After replay succeeds, verify tombstones, cache invalidation, checkpoint equality, correction rows, and dead letters before starting the BFF or admitting traffic.

Prompt injection is not applicable to this recovery path: startup consumes typed PostgreSQL facts and configuration, not natural-language or model-provided instructions.

## Upgrade Procedure

1. Read `RELEASE_CHECKLIST.md` and the release notes for migration, config, security, and rollback changes.
2. Take and verify backups before pulling or building new images.
3. Pull or build the target images.
4. Start in a non-production environment with production-like secrets and data shape.
5. Apply migrations through the deployment path:
   - Aspire/local-dev uses `Event.MigrationService` before API/Blazor start.
   - Docker Compose does not lack `Event.MigrationService`: the `privacy-erasure-external` profile includes the one-shot service for `PRIVACY_ERASURE_AUTHORITY_TOPOLOGY=ExternalDatabase`; otherwise `Explore.API` applies application migrations on startup outside `Testing`.
6. Start the upgraded stack:

   ```bash
   docker compose up -d
   ```

7. Verify health:
   - API `/alive` returns healthy;
   - API `/health` is healthy, or degraded only for an optional dependency that is intentionally disabled;
   - Blazor can reach the API through its configured base URL.
8. Watch logs for migration failures, Keycloak discovery failures, secret-provider failures, storage readiness/reconciliation drift, and Cerbos readiness failures.
9. Keep the previous images and backups until smoke tests pass.

### Webhook redesign migration gate

For webhook redesign schema slices, use a reviewed EF Core idempotent SQL script and a
write-maintenance window. Apply the script with `ON_ERROR_STOP`; never allow a failed SQL
statement to be followed by application startup. The release evidence must record:

1. the exact committed migration used as the upgrade starting point;
2. clean-install and committed-baseline-upgrade semantic schema signatures;
3. representative legacy-row counts, classification counts, timing, and lock samples;
4. a custom-format `pg_dump` restored into a clean database;
5. matching migration history, row count, deterministic data checksum, constraints, and indexes;
6. a successful idempotent reapply on the restored database.

Use a 15-minute write window plus a 15-minute restore buffer for the currently measured
10,000-row fixture. Rehearse with production-like volume before release and enlarge the
window from measured evidence. If the projected run exceeds five minutes or lock sampling
shows waiting writers, stop the rollout and split resumable data backfill from blocking DDL.

Before traffic resumes, restore the verified backup on any failed validation. After traffic
resumes, pause writers and deploy a new additive forward migration. Do not use destructive
rollback for evidence-bearing webhook rows and do not guess provider success during repair.

## Rollback Rules

Rollback depends on whether migrations are reversible:

| Situation | Rollback Action |
|---|---|
| No schema/data migration ran | Revert images and restart services. |
| Backward-compatible migration ran | Revert images only if release notes say the old version tolerates the new schema. |
| Destructive or non-reversible migration ran | Restore database/object-storage backups and then revert images. |
| Storage reconciliation mutations ran against the wrong object store | Stop write traffic, disable destructive reconciliation flags, restore database and object-storage backups from the same manifest, then restart with dry-run reconciliation. |
| Secret/key rotation changed runtime identity | Restore matching secret-provider values before restarting old images. Rotation is restart-based today; do not claim live reload. |

If release notes do not explicitly state that a rollback is image-only safe, assume a database restore is required.

## Verification Checklist

- [ ] Backups exist for application DB, Keycloak DB, object storage, and secrets.
- [ ] Restore was tested in non-production.
- [ ] The retained erasure authority was backed up and restored independently, and its watermark/hash are recorded without identifiers or connection details.
- [ ] For `ExternalDatabase`, API startup replay advanced the application checkpoint to the untouched authority watermark; restored PII canaries are absent and outbox evidence is present once before BFF startup.
- [ ] For `CoLocated`, the restore record states `restoreReplayProtection=false` and makes no old-backup replay claim.
- [ ] `docker compose ps` shows required services running.
- [ ] API `/alive` and `/health` return expected status.
- [ ] Blazor loads and can proxy API requests.
- [ ] Initial admin login works through Keycloak.
- [ ] Object uploads/downloads work with local-first storage or the intentionally selected S3-compatible provider.
- [ ] Storage reconciliation is either dry-run/healthy or intentionally disabled/degraded, and destructive flags match the release plan.
- [ ] Cerbos readiness is healthy when `authz` profile/provider is enabled.
- [ ] Release notes and docs impact are complete.

## Related

- [SELF_HOSTING.md](SELF_HOSTING.md) — runtime topology and environment keys.
- [CONFIGURATION.md](CONFIGURATION.md) — canonical configuration sections.
- [SECRETS.md](SECRETS.md) — secret-provider behavior.
- [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md) — release documentation contract.
