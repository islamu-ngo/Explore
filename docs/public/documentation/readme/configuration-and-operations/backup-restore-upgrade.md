---
description: >-
  Protect every durable authority and upgrade pre-1.0 deployments with rehearsed
  recovery.
---

# Backup, Restore & Upgrade

A backup is complete only when every durable authority is included and the result has been restored successfully in isolation.

## Inventory durable state

Protect:

1. the primary application database;
2. Data Protection keys/state;
3. the privacy-erasure authority store;
4. local files or the selected S3-compatible bucket plus storage metadata;
5. separately hosted provider, queue, webhook, or outbox state;
6. non-secret configuration needed to reconnect services.

Encrypt backups, restrict access, define retention, and record version and integrity evidence.

## Privacy-erasure topology

| Topology           | Backup consequence                                                                                |
| ------------------ | ------------------------------------------------------------------------------------------------- |
| `EmbeddedSqlite`   | Preserve the dedicated authority file separately from the primary DB; use one writer/API replica. |
| `CoLocated`        | PostgreSQL or SQLite only; simpler operations but no independent stale-primary protection.        |
| `ExternalDatabase` | Preserve the separate PostgreSQL authority and its runtime/migrator roles.                        |

Restoring the application DB without the independent erasure authority can resurrect erased state. Startup replay must restore the anti-resurrection fence before traffic is served.

## Restore rehearsal

Restore into an isolated network, apply the matching application version, verify migrations and integrity, and exercise identity, authorization, tenancy, representative data, storage, outboxes, and erasure status. Do not contact real recipients/providers during a rehearsal unless the environment is deliberately isolated and approved.

## Upgrade procedure

The project is pre-1.0 and the API is version `0.1`.

1. Pin target application versions and image digests.
2. Read release notes and `docs/API_CHANGELOG.md`.
3. Record current non-secret configuration.
4. Back up every authority and prove the restore.
5. Rehearse migrations in staging or an isolated restore.
6. Decide rollback versus forward recovery before starting.
7. Run the migration service before API/UI in split deployments.
8. Verify health, authentication, authorization, tenant resolution, reads/writes, outboxes, integrations, and erasure status.

A schema migration may make binary rollback unsafe. Prefer the explicitly rehearsed recovery path rather than improvising after a partial upgrade.
