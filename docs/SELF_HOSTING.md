ABOUTME: Production self-hosting guide covering Docker Compose stack, configuration, and operations.
ABOUTME: Covers infrastructure services, setup secret, migrations, health checks, backups, and upgrades.

# Self-Hosting

> **Audience:** Operators
> **Status:** Implemented
> **Owner:** Platform/Ops
> **Last Verified:** 2026-05-06
> **Source Anchors:** `docker-compose.yml`, `Explore.AppHost/AppHost.cs`, `Event.MigrationService/`, `Explore.API/Program.cs`, `Explore.Infrastructure/Services/SetupSecretProvider.cs`, `Explore.Blazor/Extensions/YarpProxyExtensions.cs`

This guide covers running ISLAMU Event outside the Aspire developer loop. The repository `docker-compose.yml` is the current self-hosting source of truth.

## Runtime Topology

| Service | Required | Compose Name | Purpose | Public Port |
|---|---:|---|---|---|
| PostgreSQL | Yes | `postgres` | Application database | internal |
| Redis | Yes | `redis` | Distributed cache when configured | internal |
| Keycloak DB | Yes | `keycloak-db` | Keycloak PostgreSQL database | internal |
| Keycloak | Yes | `keycloak` | OIDC identity provider and realm import | `8080:8080` |
| API | Yes | `islamu-event-api` | REST API, migrations, health, metrics | `7039:8080` |
| Blazor BFF | Yes | `islamu-event-ui` | Server host and YARP proxy to API | `7002:8080` |
| MinIO | Optional | `minio`, `minio-init` | S3-compatible storage profile | `9005:9000`, `9006:9001` |
| Cerbos | Optional | `cerbos` | External authorization PDP profile | `3592:3592`, `3593:3593` |

Profiles:

- `storage` starts MinIO and creates the configured bucket.
- `authz` starts Cerbos for deployments that select Cerbos authorization.

## Start The Stack

1. Create an environment file with secrets required by `docker-compose.yml`.
2. Start the required stack:

   ```bash
docker compose up -d postgres redis keycloak-db keycloak islamu-event-api islamu-event-ui
   ```

3. Add optional storage when local S3/MinIO is needed:

   ```bash
   docker compose --profile storage up -d
   ```

4. Add optional Cerbos when using the Cerbos provider:

   ```bash
   docker compose --profile authz up -d
   ```

5. Open Blazor at `http://localhost:7002` and API at `http://localhost:7039`.

## Required Environment Keys

Use the key names consumed by the Compose file and source code. Do not invent generic aliases.

### Application Database Bootstrap

`Explore.Secrets/Bootstrap/BootstrapSecretLoader.cs` expects discrete PostgreSQL values. Do not provide only a URL-form database secret.

| Key | Purpose |
|---|---|
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | Container bootstrap for `postgres`. |
| `POSTGRESQL_HOST` | Application DB host, normally `postgres`. |
| `POSTGRESQL_PORT` | Application DB port, normally `5432`. |
| `POSTGRESQL_DATABASE` | Application DB name. |
| `POSTGRESQL_USERNAME` | Application DB username. |
| `POSTGRESQL_PASSWORD` | Application DB password. |

### Keycloak

| Key | Purpose |
|---|---|
| `KC_DB`, `KC_DB_URL`, `KC_DB_USERNAME`, `KC_DB_PASSWORD` | Keycloak database configuration. |
| `KEYCLOAK_ADMIN`, `KEYCLOAK_ADMIN_PASSWORD` | Initial Keycloak admin account. |
| `KEYCLOAK_ENDPOINT` | Base Keycloak endpoint used to derive API/Blazor authority values. |
| `KEYCLOAK_REALM` | Realm name. |
| `KEYCLOAK_BLAZOR_CLIENT_SECRET` | Blazor confidential client secret. |

### Storage

Runtime storage settings bind to `S3Settings:*`.

| Compose/API Key | Canonical .NET Key |
|---|---|
| `S3Settings__Endpoint` | `S3Settings:Endpoint` |
| `S3Settings__PublicEndpoint` | `S3Settings:PublicEndpoint` |
| `S3Settings__Region` | `S3Settings:Region` |
| `S3Settings__BucketName` | `S3Settings:BucketName` |
| `S3Settings__AccessKeyId` | `S3Settings:AccessKeyId` |
| `S3Settings__SecretAccessKey` | `S3Settings:SecretAccessKey` |

Infisical/domain secret definitions use the `STORAGE_S3_*` family under storage paths. Keep docs and secret-provider values aligned with `S3Settings:*`; do not use stale `Storage__*` keys.

### API And Blazor

| Key | Host | Purpose |
|---|---|---|
| `DEPLOYMENT_MODE` | API | Optional first-run mode; omit for single-tenant, set `multi_tenant` before first launch for multi-tenant. |
| `SETUP_SECRET` | API | Optional fixed setup secret. If absent, API generates and logs a temporary secret. |
| `API_ENDPOINT` | Blazor | API base URL fallback for BFF proxying outside Aspire. |

`docker-compose.yml` sets Blazor `API_ENDPOINT` with a default of `http://islamu-event-api:8080/`, matching the Compose API service name. Operators only need to override `API_ENDPOINT` when routing the BFF to a different API host.

## First-Run Setup Secret

If `SETUP_SECRET` is unset and setup mode is active, the API generates a 32-character setup secret, logs it at startup, and accepts it for 60 minutes. Use that secret to complete the setup flow:

1. Setup Secret
2. Admin Auth
3. Site Profile
4. Preflight
5. Launch

The validation endpoint is `POST /api/InstanceOnboarding/validate-secret`. The setup-secret rate-limit policy allows only a small number of attempts per minute; repeated failures should be treated as operator or credential errors, not retried blindly.

If the generated secret expires before launch, restart `islamu-event-api` and use the newly logged secret.

## Keycloak Realm

The Compose file imports `./docker/keycloak/realm-export.json` into Keycloak. For production, verify:

- realm name matches `KEYCLOAK_REALM`;
- Blazor client ID matches the configured client (`islamu-event-blazor` in Compose);
- redirect URIs and web origins match the public reverse-proxy host;
- API audience and metadata address match the Keycloak endpoint exposed to the API.

## Migrations

There are two migration paths:

| Path | Applies To | Behavior |
|---|---|---|
| `Event.MigrationService` | Aspire/local-dev orchestration | Applies `ExploreDbContext` and data-protection migrations, seeds, then exits before API/Blazor start. |
| `Explore.API` startup | Docker Compose and direct API hosting | Runs EF migrations and database seeding on startup outside `Testing`. |

The production Compose file does not currently start `Event.MigrationService` as a separate container. Do not document Compose as if the migration service runs there unless the Compose file is changed with it.

## Reverse Proxy

Place TLS termination in front of `islamu-event-ui` and route browser traffic to port `8080` inside the container. The Blazor BFF proxies API calls; browsers should not need direct API access.

Minimum proxy requirements:

- preserve `Host`;
- forward `X-Forwarded-For` and `X-Forwarded-Proto`;
- configure `ForwardedHeadersTrust` in the API before relying on forwarded host/IP values;
- use the same public origin in Keycloak redirect URIs and web origins.

## Health Checks

| Endpoint | Host | Purpose |
|---|---|---|
| `/alive` | API, Blazor | Liveness probe. |
| `/health` | API, Blazor | Readiness probe for dependencies and shutdown state. |
| `/metrics` | API | Prometheus metrics endpoint. |

Treat `Unhealthy` as non-deployable. Treat `Degraded` as acceptable only when the response identifies an optional dependency that is intentionally disabled.

## Backup And Upgrade

Before every upgrade:

1. Back up application PostgreSQL data.
2. Back up Keycloak PostgreSQL data.
3. Back up object storage if `storage` is enabled.
4. Record image tags, commit SHA, enabled Compose profiles, and secret-provider key names.
5. Read release notes for migrations, config changes, rollback constraints, and docs impact.

Use [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md) for the full runbook and [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md) before tagging or deploying a release.

## Related

- [CONFIGURATION.md](CONFIGURATION.md) — runtime configuration sources and key mappings.
- [SECRETS.md](SECRETS.md) — secret provider behavior and key mapping.
- [OPERATIONS.md](OPERATIONS.md) — health, startup, shutdown, and runtime safeguards.
- [SECURITY.md](SECURITY.md) — authentication and authorization architecture.
- [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md) — backup, restore, upgrade, rollback.
