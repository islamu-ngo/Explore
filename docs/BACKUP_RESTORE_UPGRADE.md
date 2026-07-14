ABOUTME: Operator runbook for backups, restores, upgrades, and rollback decisions.
ABOUTME: Grounds release operations in Docker Compose, PostgreSQL, Keycloak, object storage, and migration behavior.

# Backup, Restore, And Upgrade

> **Audience:** Operators
> **Status:** Implemented
> **Owner:** Platform/Ops
> **Last Verified:** 2026-06-02
> **Source Anchors:** `docker-compose.yml`, `Event.MigrationService/Worker.cs`, `Explore.API/Program.cs`, `docs/SELF_HOSTING.md`, `docs/CONFIGURATION.md`, `docs/SECRETS.md`

This runbook covers self-hosted deployments using the repository Docker Compose topology. Treat every upgrade as a data operation first and an image rollout second.

## What Must Be Backed Up

| Asset | Compose Anchor | Why It Matters |
|---|---|---|
| Application PostgreSQL data | `postgres` volume `postgres_data` | Tenants, events, users, settings, outbox, and data-protection keys when stored through EF contexts. |
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
3. Create PostgreSQL backups for both databases:

   ```bash
   docker compose exec postgres sh -c 'pg_dump -U "$POSTGRES_USER" "$POSTGRES_DB"' > backup-explore.sql
   docker compose exec keycloak-db sh -c 'pg_dump -U "$POSTGRES_USER" "$POSTGRES_DB"' > backup-keycloak.sql
   ```

4. Back up object storage:
   - for local-first Compose, copy the API `local_storage_data` named volume or the deployment-managed `Storage:Local:RootPath`;
   - for Aspire/local-dev state that must be preserved, copy `storage-data/aspire-local`;
   - for local MinIO/S3-compatible mode, copy the `minio_data` volume or use an S3-compatible sync tool;
   - for external S3, use provider-native versioning or bucket replication.
5. Export secret-provider configuration or capture the exact secret paths and key names used by the release.
6. Store backups outside the host running Docker Compose.
7. Verify restore viability in a non-production environment before relying on the backup for an upgrade.

## Restore Procedure

1. Stop application traffic before restoring:

   ```bash
   docker compose down
   ```

2. Restore PostgreSQL data into clean volumes or clean databases.
3. Restore Keycloak data before starting Keycloak-dependent application services.
4. Restore object storage before user-facing traffic resumes. For local-first Compose, restore `local_storage_data` or the configured `Storage:Local:RootPath`; for S3-compatible mode, restore `minio_data` or the external bucket.
5. Restore the matching `.env` and secret-provider values.
6. Start dependencies first, then application services:

   ```bash
   docker compose up -d postgres redis keycloak-db keycloak
docker compose up -d islamu-event-api islamu-event-ui
   ```

7. Check `/alive` and `/health` on the API and Blazor hosts.
8. Validate login, tenant resolution, event browsing, and file access.

## Upgrade Procedure

1. Read `RELEASE_CHECKLIST.md` and the release notes for migration, config, security, and rollback changes.
2. Take and verify backups before pulling or building new images.
3. Pull or build the target images.
4. Start in a non-production environment with production-like secrets and data shape.
5. Apply migrations through the deployment path:
   - Aspire/local-dev uses `Event.MigrationService` before API/Blazor start.
   - Docker Compose does not currently include `Event.MigrationService`; `Explore.API` applies migrations on startup outside `Testing`.
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
| Secret/key rotation changed runtime identity | Restore matching secret-provider values before restarting old images. |

If release notes do not explicitly state that a rollback is image-only safe, assume a database restore is required.

## Verification Checklist

- [ ] Backups exist for application DB, Keycloak DB, object storage, and secrets.
- [ ] Restore was tested in non-production.
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
