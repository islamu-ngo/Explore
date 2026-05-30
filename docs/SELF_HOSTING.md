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
| Keycloak Init | Yes | `keycloak-init` | One-shot client-secret synchronization after realm import | internal |
| API | Yes | `islamu-event-api` | REST API, migrations, health, metrics | `7039:8080` |
| Blazor BFF | Yes | `islamu-event-ui` | Server host and YARP proxy to API | `7002:8080` |
| MinIO | Optional | `minio`, `minio-init` | S3-compatible storage profile | `9005:9000`, `9006:9001` |
| Cerbos | Optional | `cerbos` | External authorization PDP profile | `3592:3592`, `3593:3593` |

Profiles:

- `storage` starts MinIO and creates the configured bucket.
- `authz` starts Cerbos for deployments that select Cerbos authorization.

Email dispatch modes:

- **Basic Dispatch Mode is implemented and requires no extra Compose profile.** API + PostgreSQL + configured SMTP are sufficient for registration confirmation email. By default, API-hosted TickerQ schedules `email-dispatch-drain`, which claims `EmailDispatchOutbox` rows from PostgreSQL through the shared drain service and sends through the SMTP abstraction. `EmailDispatchProcessor:Mode=HostedService` is a fallback trigger, not a separate business workflow.
- **RabbitMQ Dispatch Mode is optional transport infrastructure.** The repository now has a local Aspire RabbitMQ resource and an API-side publisher/topology/health foundation for pointer-only dispatch messages, but the production Compose stack still does not require or start RabbitMQ. Do not require RabbitMQ for self-hosting until a dedicated Compose profile, manual-ack consumer, DLQ replay/parking flow, and RabbitMQ integration tests are added. RabbitMQ shares the same PostgreSQL `EmailDispatchOutbox` state machine; it is transport only.

## Start The Stack

1. Create an environment file with secrets required by `docker-compose.yml`.
2. Start the required stack:

   ```bash
docker compose up -d postgres redis keycloak-db keycloak keycloak-init islamu-event-api islamu-event-ui
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
| `KEYCLOAK_BLAZOR_CLIENT_SECRET` | Required Blazor confidential client secret. `keycloak-init` writes this into the `islamu-event-blazor` Keycloak client after realm import. |
| `KEYCLOAK_API_CLIENT_SECRET` | Optional API resource-server client secret. Current bearer-token validation does not require it; if set, `keycloak-init` writes it into the `islamu-event-api` Keycloak client. |
| `KEYCLOAK_INIT_ALLOW_DEFAULT_LOCAL_SECRET` | Optional local-development escape hatch. Set to `true` only for throwaway local stacks that intentionally use the static realm-export default secret. |

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
| `SETUP_SECRET_REQUIRED` | API | Optional. Defaults to `true`; `false` only takes effect with trusted managed-provider provisioning keys and never makes setup endpoints anonymous. |
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

Managed hosting operators that provision through the authorized managed-provider endpoint can set `SETUP_SECRET_REQUIRED=false` only together with `PROVISIONING_TRUSTED=true`, a managed `PROVISIONING_MODE`, `MANAGED_CLIENT_EXTERNAL_PROVIDER`, and `PHYSICAL_TENANCY_MODE`. In that mode the interactive setup-secret lane is not public: setup-secret-protected endpoints still reject missing or invalid secrets, and provider automation must authenticate as the platform/operator path.

## Keycloak Realm

The Compose file imports `./docker/keycloak/realm-export.json` into Keycloak, then runs the one-shot `keycloak-init` service. The init job authenticates with the Compose Keycloak admin account, locates clients by `clientId`, and synchronizes the confidential Blazor BFF client secret from `KEYCLOAK_BLAZOR_CLIENT_SECRET`. Operators should set the secret once in environment variables or the configured secret provider; they should not manually edit the Keycloak UI to match the BFF secret.

For production, verify:

- realm name matches `KEYCLOAK_REALM`;
- Blazor client ID matches the configured client (`islamu-event-blazor` in Compose);
- redirect URIs and web origins match the public reverse-proxy host;
- API audience and metadata address match the Keycloak endpoint exposed to the API.

`KEYCLOAK_BLAZOR_CLIENT_SECRET` is fail-closed for Compose startup: `keycloak-init` exits non-zero when it is missing. To use the repository's static local default in disposable development, set `KEYCLOAK_INIT_ALLOW_DEFAULT_LOCAL_SECRET=true`. Do not enable that flag in production or shared environments. Rerun `docker compose run --rm keycloak-init` after rotating `KEYCLOAK_BLAZOR_CLIENT_SECRET` or optional `KEYCLOAK_API_CLIENT_SECRET`.

## Migrations

There are two migration paths:

| Path | Applies To | Behavior |
|---|---|---|
| `Event.MigrationService` | Aspire/local-dev orchestration | Applies `ExploreDbContext` and data-protection migrations, seeds, then exits before API/Blazor start. |
| `Explore.API` startup | Docker Compose and direct API hosting | Runs EF migrations and database seeding on startup outside `Testing`. |

The production Compose file does not currently start `Event.MigrationService` as a separate container. Do not document Compose as if the migration service runs there unless the Compose file is changed with it.

### Creating Initial Migrations From Scratch

When bootstrapping EF Core migrations from a clean repository state, run the commands from the repository root in this order:

1. Create the data-protection migration first so the dedicated key-store context keeps its own migration history and output folder:

   ```bash
   dotnet ef migrations add init --context DataProtectionKeyContext --project Explore.Persistence --startup-project Explore.API --output-dir Migrations/DataProtection
   ```

2. Create the primary application schema migration for `ExploreDbContext`:

   ```bash
   dotnet ef migrations add init --context ExploreDbContext --project Explore.Persistence --startup-project Explore.API
   ```

Do not reverse this order when starting from scratch; both contexts are part of the supported schema bootstrap path.

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

Basic Email Dispatch readiness is reported by the API `email-dispatch` health check. `Degraded` means dispatch is intentionally disabled. `Unhealthy` means the selected trigger is not usable, for example `EmailDispatchProcessor:Mode=TickerQ` while `Scheduler:TickerQ:Enabled=false`. RabbitMQ is not part of Basic Dispatch Mode readiness.

Optional RabbitMQ dispatch readiness is reported separately by `email-dispatch-rabbitmq`. With `EmailDispatchRabbitMq:Enabled=false` the check is healthy without requiring a broker. If an operator explicitly enables RabbitMQ mode, broker or topology failures make readiness unhealthy because the selected transport cannot safely publish pointer events.

Expired write-retry replay-cache cleanup is reported by `idempotency-cleanup`. `Healthy` means cleanup is enabled in delete or dry-run mode. `Degraded` means cleanup is intentionally disabled; expired keys remain ineligible for replay, but physical cleanup is paused.

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
