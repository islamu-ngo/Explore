<!-- ABOUTME: Docker Compose installation guide for the structured multi-database deployment. -->
<!-- ABOUTME: Covers MigrationService ordering, provider selection, and the independent privacy-erasure authority. -->

# Docker Compose

The repository Compose stack accepts structured database settings; it does not require operators to build or distribute a raw application connection string. Start from the repository `.env.example`, choose a provider, and keep runtime credentials separate from migrator credentials.

## Select the primary database

Set `DATABASE_PROVIDER` to one of `PostgreSql`, `Sqlite`, `SqlServer`, `MariaDb`, or `MySql`, then configure the endpoint and roles described in [Configuration](../../CONFIGURATION.md#persistence-configuration).

- Server providers use `DATABASE_HOST`, `DATABASE_PORT`, `DATABASE_NAME`, `DATABASE_TLS_MODE`, and the `DATABASE_RUNTIME_*` / `DATABASE_MIGRATOR_*` role variables.
- MariaDB and MySQL additionally require an exact `DATABASE_SERVER_FLAVOR` and positive `DATABASE_SERVER_VERSION` matching the server.
- SQLite uses a persisted absolute local path mounted into both MigrationService and API. Use one application replica, a local durable filesystem, and a file distinct from the privacy-erasure authority.

## Choose the instance namespace

Namespace selection is automatic from the provider:

- PostgreSQL and SQL Server use `DATABASE_SCHEMA` (default `islamu_event`) as
  the application namespace and create clean names such as
  `islamu_event.users`. Give each instance a distinct schema when sharing a
  database.
- SQLite, MariaDB, and MySQL always use the fixed `ie_` prefix, producing
  `ie_users`. The prefix is not configurable. Give each SQLite instance its own
  local file and each MariaDB/MySQL instance its own database.

Quartz scheduler tables are co-located in the application database under the
`QRTZ_` prefix. When sharing one database between ISLAMU instances, give each a
distinct `Scheduler:Quartz:SchedulerName`; otherwise use separate databases, or
set `Scheduler:Quartz:ClusteringEnabled=true` when the instances are meant to
cooperate as one scheduler cluster.

PostgreSQL remains the default Compose profile. The release test matrix records the exact engine versions currently exercised in CI; treat those as tested baselines, not as a promise that every other engine version is supported.

## Migrate before starting the API

Run the one-shot MigrationService first:

```bash
docker compose run --rm event-migrationservice
docker compose up -d
```

MigrationService selects the provider-specific application and Data Protection migration assemblies, initializes the configured privacy-erasure authority, and seeds governed data. Run the migration command a second time during an upgrade rehearsal to prove idempotency. A deployed API does not migrate those schemas. The API owns only the Quartz scheduler schema, which is applied as idempotent DDL on every supported provider.

## Protect the privacy-erasure authority

The default topology is `EmbeddedSqlite`. Mount `/app/data/privacy_erasure_authority.db` on its own durable volume, keep exactly one authority writer, and back up/restore it independently from the primary database. The authority initializer requires a local non-symlink path, private permissions, WAL, synchronous `FULL`, foreign keys, and a successful integrity check.

For a remote authority, select `ExternalDatabase` (`ERASURE_TOPOLOGY=ExternalDatabase`) and configure a distinct PostgreSQL endpoint and runtime/migrator roles under `Database:Erasure:*` / `DATABASE_ERASURE_*` (or Infisical path `/database/erasure`). Raw authority connection strings are not supported. `CoLocated` is an explicit alternative only for PostgreSQL or SQLite primary databases; it is not a valid substitute for the independent-restore guarantees of `ExternalDatabase`.

See [Self-hosting](../../SELF_HOSTING.md) for the complete service topology, [Secrets](../../SECRETS.md) for credential names, and [Backup, Restore, and Upgrade](../../BACKUP_RESTORE_UPGRADE.md) before production use.
