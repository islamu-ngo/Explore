---
description: Operate the split service topology with PostgreSQL and ordered migrations.
---

# Docker Compose

Docker Compose is the supported split topology for operators who want independently managed application and infrastructure services. PostgreSQL is the default database.

## Prepare configuration

```bash
git clone <repository-url>
cd Event
cp .env.example .env
openssl rand -hex 32
# Store the result in .env as KEYCLOAK_BLAZOR_CLIENT_SECRET.
docker compose config --quiet
```

Treat `.env.example` as a schema. Replace sample values, select the intended database provider, and provide separate runtime and migrator credentials through the documented structured `DATABASE_*` settings. Do not invent a raw connection-string shortcut.

MariaDB and MySQL require the exact server flavor and version. SQLite in split mode requires a persisted absolute path and one API replica.

## Migrate before startup

```bash
docker compose run --rm event-migrationservice
docker compose up -d
```

The one-shot migration service must exit successfully before the API or UI starts. It initializes application, Data Protection, privacy-erasure, and governed seed state. Re-running it during an upgrade rehearsal should be idempotent.

The default local endpoints are:

| Service        | URL                     |
| -------------- | ----------------------- |
| UI             | `http://localhost:7002` |
| API            | `http://localhost:7039` |
| Mailpit        | `http://localhost:8025` |
| Keycloak admin | `http://localhost:8080` |

## Production boundaries

Put public HTTP surfaces behind TLS and a reverse proxy. Do not expose database, migration, Keycloak administration, Mailpit, Cerbos administration, or internal provider services publicly.

Persist every database and service volume. Back up application data, Data Protection keys, the selected privacy-erasure authority topology, object storage, and provider/outbox state. Validate a restore rather than treating a successful backup command as proof.

## Acceptance

Confirm rendered Compose configuration, migration completion, healthy API/UI, Keycloak login, authorization decisions, tenant resolution, representative reads and writes, and configured email/webhook delivery. Record pinned image digests and a rollback or forward-recovery decision before upgrades.
