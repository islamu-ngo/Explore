ABOUTME: Production self-hosting guide covering Docker Compose stack, configuration, and operations.
ABOUTME: Covers infrastructure services, setup secret, migrations, health checks, backups, and upgrades.

# Self-Hosting

> **Audience:** Operators
> **Status:** Implemented
> **Owner:** Platform/Ops
> **Last Verified:** 2026-06-02
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
| API | Yes | `islamu-event-api` | REST API, migrations, health, metrics, local storage volume | `7039:8080` |
| Blazor BFF | Yes | `islamu-event-ui` | Server host and YARP proxy to API | `7002:8080` |
| MinIO | Optional | `minio`, `minio-init` | S3-compatible storage profile when an instance selects optional S3 mode | `9005:9000`, `9006:9001` |
| Cerbos | Optional | `cerbos` | External authorization PDP profile | `3592:3592`, `3593:3593` |
| AI provider | Optional | external/self-hosted | OpenAI-compatible or fake AI assistant provider selected by `AiProvider:*` plus tenant governance settings | deployment-specific |

Profiles:

- `storage` starts MinIO and creates the configured bucket for optional S3-compatible mode. It is not required for local-first storage.
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

3. Add optional MinIO only when S3-compatible storage is selected or tested:

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

Local-first storage is available in the required API service without MinIO. Compose mounts the API path `/app/storage-data/local` to the durable named volume `local_storage_data`.

| Compose/API Key | Canonical .NET Key | Purpose |
|---|---|---|
| `LOCAL_STORAGE_ROOT_PATH` | `Storage:Local:RootPath` | Optional override for the API local storage root. The Compose default is `/app/storage-data/local`. |
| `LOCAL_STORAGE_CREATE_ROOT_IF_MISSING` | `Storage:Local:CreateRootIfMissing` | Optional override for allowing the API to create the root. The Compose default is `true`. |

Optional S3-compatible settings bind to `S3Settings:*`.

| Compose/API Key | Canonical .NET Key |
|---|---|
| `S3Settings__Endpoint` | `S3Settings:Endpoint` |
| `S3Settings__PublicEndpoint` | `S3Settings:PublicEndpoint` |
| `S3Settings__Region` | `S3Settings:Region` |
| `S3Settings__BucketName` | `S3Settings:BucketName` |
| `S3Settings__AccessKeyId` | `S3Settings:AccessKeyId` |
| `S3Settings__SecretAccessKey` | `S3Settings:SecretAccessKey` |

Infisical/domain secret definitions use the `STORAGE_S3_*` family under storage paths. Keep optional S3 docs and secret-provider values aligned with `S3Settings:*`; do not use stale generic S3 aliases such as `Storage__Endpoint`.

### Storage Reconciliation

The API starts storage reconciliation in enabled dry-run mode by default. Compose exposes the safety-critical flags through environment variables:

| Compose/API Key | Canonical .NET Key | Default |
|---|---|---:|
| `STORAGE_RECONCILIATION_ENABLED` | `StorageReconciliation:Enabled` | `true` |
| `STORAGE_RECONCILIATION_DRY_RUN` | `StorageReconciliation:DryRun` | `true` |
| `STORAGE_RECONCILIATION_QUARANTINE_MISSING_OBJECTS` | `StorageReconciliation:QuarantineMissingObjects` | `false` |
| `STORAGE_RECONCILIATION_QUARANTINE_ORPHAN_LOCAL_FILES` | `StorageReconciliation:QuarantineOrphanLocalFiles` | `false` |
| `STORAGE_RECONCILIATION_DELETE_QUARANTINED_OBJECTS` | `StorageReconciliation:DeleteQuarantinedObjects` | `false` |

Enable destructive cleanup only after reviewing dry-run output, confirming backups include the local storage volume or selected S3 bucket, and intentionally setting `StorageReconciliation:DryRun=false` plus the specific mutation flag.

### API And Blazor

| Key | Host | Purpose |
|---|---|---|
| `DEPLOYMENT_MODE` | API | Optional first-run mode; omit for single-tenant, set `multi_tenant` before first launch for multi-tenant. |
| `SETUP_SECRET` | API | Optional fixed setup secret. If absent, API generates and logs a temporary secret. |
| `SETUP_SECRET_REQUIRED` | API | Optional. Defaults to `true`; `false` only takes effect with trusted managed-provider provisioning keys and never makes setup endpoints anonymous. |
| `API_ENDPOINT` | Blazor | API base URL fallback for BFF proxying outside Aspire. |

`docker-compose.yml` sets Blazor `API_ENDPOINT` with a default of `http://islamu-event-api:8080/`, matching the Compose API service name. Operators only need to override `API_ENDPOINT` when routing the BFF to a different API host.

### AI Assistant

The AI assistant is optional. Self-hosted deployments can run with AI disabled, with the deterministic fake provider for smoke tests, with an explicitly configured OpenAI-compatible provider, or with opt-in SDK-backed OpenAI/Azure OpenAI modes.

| Canonical .NET Key | Purpose |
|---|---|
| `AiProvider:Enabled` | Enables provider readiness evaluation. Disabled mode is health-safe and performs no provider call. |
| `AiProvider:Provider` | Supported values include `none`, `fake`, `openai-compatible`, `openai-sdk`, and `azure-openai`. Keep `openai-compatible` for generic/self-hosted OpenAI-compatible endpoints. |
| `AiProvider:EndpointUrl` | Provider base URL for OpenAI-compatible or Azure OpenAI mode. Do not include credentials, query strings, or fragments. Azure OpenAI endpoints must use HTTPS. |
| `AiProvider:ApiKey` | Sensitive provider credential. Never expose through logs, health data, metrics, browser payloads, issue templates, or screenshots. |
| `AiProvider:ModelId` | Default model identifier for provider calls. For Azure OpenAI this is the deployment name. Health data reports only configured/not-configured flags, not the raw value. |
| `AiProvider:AzureCredentialMode` | Azure OpenAI credential mode: `api-key` or `default-azure-credential`. Prefer `default-azure-credential` for Azure-hosted deployments with managed identity. |
| `AiProvider:AzureTenantId` | Optional tenant ID for `DefaultAzureCredential`. |
| `AiProvider:AllowLocalProviderEndpoints` | Explicit opt-in for loopback/private provider URLs in local-model deployments. Keep disabled for public SaaS providers. |
| `AiRetentionCleanup:*` | Static scheduler settings for tenant-scoped AI conversation retention cleanup. The per-tenant retention window remains `ai_assistant.retention_days`. |
| `Mcp:*` | Optional API-hosted Model Context Protocol adapter posture. Disabled by default. |

Operational notes:

- `/health` includes `ai-provider` and `ai-retention-cleanup`. Disabled AI provider mode is healthy; disabled retention cleanup is intentionally degraded.
- AI run progress uses authenticated polling through the API run-status route. Streaming is reserved and disabled until a future hardening slice implements transport, timeout, cancellation, logging, authentication, and fallback behavior.
- The Blazor UI gates assistant reference/proposal actions by API HAL links. Do not recreate role/claim checks in the browser.
- `AiRetentionCleanup:DryRun=true` is recommended before first enabling destructive AI history redaction in a new environment.
- MCP is optional. Keep startup `Mcp:Enabled=false` unless the deployment intentionally exposes the API-hosted stateless Streamable HTTP MCP endpoint. When startup mapping is enabled, instance runtime setting `mcp.enabled` can still disable the adapter without changing route shape. The adapter exposes safe registry discovery, first-class registry-projected proposal tools, conversation metadata resources, and proposal-first tool mutation; mutating MCP tools remain registry-backed and require the normal product/API confirmation path before side effects occur.
- When enabling MCP, expose only the API endpoint configured by startup `Mcp:EndpointPath`, verify the `mcp-adapter` readiness check, and keep `Mcp:Stateless=true` plus `Mcp:EnableLegacySse=false` for normal deployments. MCP is API-key-first for external clients: configure `X-API-Key`/`ISLAMU_EVENT_API_KEY`, grant `mcp:read` for read resources and `mcp:propose` for proposal tools/prompts, never send bearer and API-key credentials together, and expect no-key, invalid-key, or revoked-key requests to see only anonymous-safe registry discovery. Instance administrators can lock tenant overrides with `governance.lock_tenant_mcp` and `governance.lock_tenant_mcp_legacy_sse`; tenant administrators can override only unlocked MCP runtime values. Self-hosters can disable MCP quickly by setting runtime `mcp.enabled=false`; set startup `Mcp:Enabled=false` and restart only when the endpoint must be unmapped.
- Do not run `stdio` or legacy SSE as product MCP transports from this stack. `stdio` remains deferred by [ADR-011](adr/ADR-011-local-mcp-stdio-diagnostic-host.md) unless a future local-only diagnostic host is separately approved, and legacy SSE remains unavailable even if `Mcp:EnableLegacySse=true` and `mcp.enable_legacy_sse=true` because stateful sessions and session affinity require a future ADR/test gate. Native AOT publication for the API host is also unverified until a dedicated publish profile proves the explicit SDK registrations and registry-projected tools survive trimming/AOT.
- Do not enable stateful sessions, sampling, elicitation, roots, completions, progress notifications, resource subscriptions, list-changed notifications, or client-specific compatibility shims for a self-hosted MCP client without a new ADR and release note. The default compatibility answer is to keep MCP disabled or keep the current stateless proposal-first surface unchanged.
- MCP support requests must not include prompts, tool payloads, provider responses, tenant IDs, endpoint URLs, API keys, model secrets, or raw MCP request/response bodies. Use [MCP_DEBUGGING.md](MCP_DEBUGGING.md) for redacted local debugging and client-smoke steps.

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

### External Keycloak Bootstrap

When using an existing Keycloak deployment instead of the Compose-managed realm, first-run onboarding can configure the ISLAMU clients through the auth-provider setup page. Choose **Let ISLAMU configure Keycloak clients now** only when the operator has a temporary Keycloak admin or service account credential with enough permission to read the target realm, create the realm when using create mode, create or locate OIDC clients, and set client secrets.

The setup bootstrap path is protected by the setup secret before launch. The browser sends the one-time Keycloak bootstrap credential only to the Blazor BFF; the BFF strips any browser-controlled setup-secret header and forwards the trusted setup secret to the API. The API passes the credential to the Infrastructure Keycloak Admin API adapter for that request only. ISLAMU stores the resulting runtime OIDC configuration and Blazor client secret, but it does **not** store the Keycloak admin username/password, Keycloak access token, or raw Keycloak Admin API response body.

Recommended external-Keycloak operator flow:

1. Create a temporary Keycloak admin or service account scoped to the target realm-management operations.
2. In `/onboarding/auth-provider`, enable Keycloak and select **Let ISLAMU configure Keycloak clients now**.
3. Enter the Keycloak base URL, target realm, Blazor BFF client ID/secret, optional API client ID/secret, and the temporary bootstrap credential.
4. Submit once. On success, the UI clears the one-time bootstrap credential fields and continues setup.
5. Disable or rotate the temporary Keycloak bootstrap credential after setup succeeds.

Use **Use an already configured Keycloak realm** when the operator has already created clients, redirect URIs, web origins, protocol mappers, and client secrets in Keycloak. In that mode ISLAMU only stores the runtime OIDC authority/client settings and does not call the Keycloak Admin API.

External bootstrap is idempotent for client lookup/update: rerunning setup against the same realm locates existing clients by `clientId` and updates their secrets. It does not delete existing realms, users, roles, or unrelated clients. Keep a Keycloak database backup before using create mode in shared environments.

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

Storage readiness is reported by `storage`. Local-first deployments do not need MinIO/S3 for this check; the API verifies the selected local provider can write to the API-owned data root. S3-compatible readiness is probed only when the instance selects that provider.

Storage reconciliation posture is reported by `storage-reconciliation`. `Healthy` means the worker is enabled in dry-run or mutation mode. `Degraded` means reconciliation is intentionally disabled. Invalid reconciliation settings fail startup through options validation.

## Backup And Upgrade

Before every upgrade:

1. Back up application PostgreSQL data.
2. Back up Keycloak PostgreSQL data.
3. Back up object storage. For local-first Compose, include the `local_storage_data` volume. If the optional `storage` profile or external S3-compatible provider is selected, include `minio_data` or the provider bucket as well.
4. Record image tags, commit SHA, enabled Compose profiles, and secret-provider key names.
5. Read release notes for migrations, config changes, rollback constraints, and docs impact.

Use [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md) for the full runbook and [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md) before tagging or deploying a release.

## Related

- [CONFIGURATION.md](CONFIGURATION.md) — runtime configuration sources and key mappings.
- [SECRETS.md](SECRETS.md) — secret provider behavior and key mapping.
- [OPERATIONS.md](OPERATIONS.md) — health, startup, shutdown, and runtime safeguards.
- [SECURITY.md](SECURITY.md) — authentication and authorization architecture.
- [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md) — backup, restore, upgrade, rollback.
