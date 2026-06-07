ABOUTME: Documents runtime configuration sources and precedence for API, BFF, and shared infrastructure.
ABOUTME: Focuses on non-inferable key names, mapping behavior, and settings cascade rules.

# Configuration

> **Audience:** Operators | Contributors | AI agents
> **Status:** Implemented
> **Owner:** Platform/Ops
> **Last Verified:** 2026-06-07
> **Source Anchors:** `Explore.API/Extensions/ConfigurationExtensions.cs`, `Explore.Blazor/Extensions/ConfigurationExtension.cs`, `Explore.Blazor/Extensions/YarpProxyExtensions.cs`, `Explore.Infrastructure/Services/HierarchicalSettingsResolver.cs`, `Explore.Infrastructure/Storage/LocalFileStorageProvider.cs`, `Explore.Infrastructure/Storage/S3ConfigResolver.cs`, `Explore.Infrastructure/StorageReconciliationSettings.cs`, `Explore.Infrastructure/Mail/SmtpConfigResolver.cs`, `Explore.Infrastructure/Services/SetupSecretProvider.cs`, `Explore.Domain/Constants/GovernanceSettingKeys.cs`, `Explore.Domain/Constants/InfrastructureSecretSettingKeys.cs`, `Explore.Domain/Secrets/SecretDefinitionRegistry.cs`, `docs/SECRETS.md`

## Runtime Configuration Sources

The system uses three configuration layers:

1. static app settings (`appsettings*.json`, environment variables, user secrets),
2. secret management (`AddInfisicalCompatibility` / `AddInfisicalBlazorCompatibility` + `AddSecretManagement`),
3. governance settings in database (`SystemSetting` + `TenantSetting`).

Secrets have an additional ownership contract that applies across the platform:

- **Application-managed** secrets/settings are saved by ISLAMU Event and editable from setup/admin UI. Saved database/application values are the runtime authority.
- **Deployment-managed** secrets/settings are controlled by environment variables, appsettings, or a configured secret provider. UI surfaces show read-only ownership badges and changes require provider refresh or redeploy/restart.
- **Deployment bootstrap** values may prefill onboarding/admin forms when no application-managed value exists. If the operator modifies and saves them, the saved application setting is used from then on.

Do not treat environment variables as absolute authority forever. In application-managed mode the precedence is: explicit saved application/database setting, then deployment bootstrap value, then default. In deployment-managed mode the selected external source is authoritative and application-managed DB values for that field are ignored.

## Deployment CI/CD Secrets

GitHub Actions deployment secrets are **workflow environment secrets**, not runtime app settings and not `SecretProvider` keys. Configure them in GitHub repository environments so production deployments can require approval before secrets are released to the deploy job.

| Environment | Secret / variable | Purpose |
|---|---|---|
| `production` secret | `COOLIFY_DEPLOY_API_WEBHOOK` | Coolify API application deployment webhook. |
| `production` secret | `COOLIFY_DEPLOY_UI_WEBHOOK` | Coolify UI application deployment webhook. |
| `production` secret | `COOLIFY_DEPLOY_TOKEN` | Bearer token used when invoking production Coolify webhooks. |
| `production` variable | `PRODUCTION_URL` | Public environment URL shown on the GitHub deployment environment. |
| `production` variable | `PRODUCTION_API_URL` | Required API base URL for production `/alive` and `/health` smoke checks. |
| `production` variable | `PRODUCTION_UI_URL` | Required UI base URL for production `/alive` and `/health` smoke checks. |
| `staging` secret | `COOLIFY_DEPLOY_API_STAGING_WEBHOOK` | Coolify staging API application deployment webhook. |
| `staging` secret | `COOLIFY_DEPLOY_UI_STAGING_WEBHOOK` | Coolify staging UI application deployment webhook. |
| `staging` secret | `COOLIFY_DEPLOY_TOKEN` | Bearer token used when invoking staging Coolify webhooks. |
| `staging` variable | `STAGING_URL` | Public environment URL shown on the GitHub deployment environment. |
| `staging` variable | `STAGING_API_URL` | Optional API base URL for `/alive` and `/health` smoke checks. |
| `staging` variable | `STAGING_UI_URL` | Optional UI base URL for `/alive` and `/health` smoke checks. |

Keep the existing secret names until the deploy workflows are consolidated. If names are normalized later, update the workflow and this table in the same change.

## Core Static Sections

Commonly consumed sections in code:

- `Keycloak:*` (authority, metadata, client IDs/secrets)
- `ConnectionStrings:DefaultConnection`
- `Cors:AllowedOrigins`
- `ForwardedHeadersTrust:*`
- `RateLimiting:*`
- `RequestTimeouts:*`
- `Cerbos:*`
- `Deployment:*`
- `Storage:Local:*` (deployment-managed local filesystem storage)
- `StorageReconciliation:*` (dry-run-first storage drift worker)
- `S3Settings:*` (fallback source for storage resolver)
- `SecretProvider:*`
- `SecretRefresh:*`
- `EmailDispatchProcessor:*` (Basic Dispatch Mode background worker)
- `EmailDispatchRabbitMq:*` (optional RabbitMQ Dispatch Mode transport foundation)
- `IdempotencyCleanup:*` (expired write-retry replay-cache cleanup)
- `AiRetentionCleanup:*` (scheduled tenant-scoped AI conversation retention cleanup)
- `AiProvider:*` (AI provider readiness/egress validation foundation)
- `Mcp:*` (optional Model Context Protocol adapter posture)
- `Persistence:*` (database runtime options)

### Persistence Configuration

| Key | Default | Description |
|---|---:|---|
| `Persistence:EnableRlsTenantSession` | `false` | Registers the PostgreSQL tenant-session interceptor that sets `app.current_tenant_id` when EF Core opens a connection. This does not enable RLS policies by itself; keep disabled outside prototype environments until runtime app-role, migration-role, admin/system-path, and table-policy rollout work is complete. |

### Storage Static Configuration

Local-first storage is deployment-managed. The filesystem root is bound from static configuration and is not a tenant/admin database setting.

| Key | Default | Description |
|---|---|---|
| `Storage:Local:RootPath` | provider default unless Compose/Aspire overrides | API-owned local storage root. Compose sets `/app/storage-data/local` and mounts it to `local_storage_data`; Aspire sets `storage-data/aspire-local` under the repository root. |
| `Storage:Local:CreateRootIfMissing` | `true` in Compose/Aspire overrides | Allows startup/health/provider code to create the local root when the deployment grants write permission. |

Optional S3-compatible storage still uses `S3Settings:*` as the runtime fallback source. Persisted `s3.*` and `s3.access_key_id`/`s3.secret_access_key` settings take precedence through `S3ConfigResolver` when S3-compatible storage is selected.

`StorageReconciliation:*` controls the API-hosted drift worker and is validated at startup:

| Key | Default | Description |
|---|---:|---|
| `StorageReconciliation:Enabled` | `true` | Enables the hosted worker and reconciliation health check. Disabled reports intentional degraded health. |
| `StorageReconciliation:DryRun` | `true` | Report-only mode. Mutating flags are ignored while dry-run is true. |
| `StorageReconciliation:InitialDelaySeconds` | `45` | Initial worker delay after API startup. |
| `StorageReconciliation:PollingIntervalMinutes` | `360` | Worker interval between passes. |
| `StorageReconciliation:BatchSize` | `500` | Maximum metadata rows or inventory objects per pass. |
| `StorageReconciliation:MissingObjectQuarantineGraceHours` | `24` | Age threshold before missing backing objects can quarantine metadata. |
| `StorageReconciliation:OrphanFileQuarantineGraceHours` | `24` | Age threshold before orphan local files can be moved to quarantine. |
| `StorageReconciliation:DeleteGraceHours` | `720` | Age threshold before delete-eligible metadata can be physically deleted and soft-deleted. |
| `StorageReconciliation:QuarantineMissingObjects` | `false` | Enables metadata quarantine for missing backing objects when `DryRun=false`. |
| `StorageReconciliation:QuarantineOrphanLocalFiles` | `false` | Enables local orphan quarantine when `DryRun=false`. |
| `StorageReconciliation:DeleteQuarantinedObjects` | `false` | Enables idempotent provider delete plus metadata soft-delete when `DryRun=false`. |

### AI Provider Static Configuration

`AiProvider:*` is the deployment/admin-controlled readiness and egress-validation surface for Infrastructure AI adapters. The governance keys below remain the tenant/runtime source for assistant availability; this static section lets operators validate provider wiring without putting endpoint URLs or credentials in browser-controlled request data.

| Key | Default | Description |
|---|---:|---|
| `AiProvider:Enabled` | `false` | Enables provider readiness evaluation. Disabled reports healthy-disabled and performs no provider network call. |
| `AiProvider:Provider` | `none` | Supported values: `none`, `fake`, `openai-compatible`, `openai-sdk`, and `azure-openai`. SDK-backed modes are opt-in; `openai-compatible` remains the generic/self-hosted fallback. |
| `AiProvider:EndpointUrl` | unset | Admin/deployment-controlled provider base URL. Required for `openai-compatible` and `azure-openai`; Azure OpenAI must use HTTPS. Must have no embedded credentials, query string, or fragment. Local/private endpoints require explicit opt-in. |
| `AiProvider:ApiKey` | unset | Sensitive provider credential. Never expose in browser payloads, health data, logs, metrics, traces, screenshots, or issue templates. |
| `AiProvider:ModelId` | unset | Default model identifier for the concrete adapter. For Azure OpenAI this is the deployment name passed to the Azure SDK. Health/metrics use only boolean presence flags, not the raw model ID. |
| `AiProvider:AzureCredentialMode` | `api-key` | Azure OpenAI credential mode: `api-key` or `default-azure-credential`. Prefer `default-azure-credential` for Azure-hosted deployments with managed identity. |
| `AiProvider:AzureTenantId` | unset | Optional tenant ID used to constrain `DefaultAzureCredential` for Azure OpenAI. Leave unset for the SDK default chain. |
| `AiProvider:AllowLocalProviderEndpoints` | `false` | Allows loopback/link-local/private provider URLs for deliberate self-hosted/local-model deployments. Keep `false` for public SaaS/provider endpoints. |
| `AiProvider:MaxInputTokens` | `8000` | Provider request input budget seed. Handlers must still enforce prompt/reference bounds. |
| `AiProvider:MaxOutputTokens` | `1024` | Provider response budget seed. |
| `AiProvider:Temperature` | `0.2` | Provider sampling temperature. Must be between 0 and 2. |
| `AiProvider:TimeoutSeconds` | `30` | Provider call timeout budget. Must be between 1 and 300. |
| `AiProvider:RetentionDays` | `30` | Retention seed; enforcement is separate from provider health. |
| `AiProvider:DailyMessageLimit` | `50` | Abuse/cost-control seed; enforcement is separate from provider health. |

### MCP Adapter Static Configuration

The Model Context Protocol adapter is optional and disabled by default. The API composes the official MCP SDK services, but maps the Streamable HTTP endpoint only when explicitly enabled. Use [MCP_DEBUGGING.md](MCP_DEBUGGING.md) for redacted local client templates and smoke guidance. It remains an adapter over the AI Tool Contract Registry rather than a second tool authority. The implementation wires configuration, health, endpoint registration, read-only registry discovery, first-class registry-projected proposal tools, safe AI conversation resources, a confirmation prompt, and proposal-first tool mutation through the normal MediatR/API confirmation path.

| Key | Default | Description |
|---|---:|---|
| `Mcp:Enabled` | `false` | Enables the API-hosted MCP endpoint. Keep disabled unless an operator intentionally exposes MCP. |
| `Mcp:EndpointPath` | `/mcp` | Route prefix for the Streamable HTTP MCP endpoint. Bare values such as `mcp` are normalized to `/mcp` before validation and endpoint mapping. |
| `Mcp:Stateless` | `true` | Uses stateless Streamable HTTP so API replicas do not require MCP session affinity. Startup validation rejects `false`. |
| `Mcp:EnableLegacySse` | `false` | Startup ceiling for future legacy-SSE governance only. Runtime legacy SSE remains unavailable and the health check reports `legacySseRuntimeEnabled=false`. |

Runtime MCP governance is stored in the hierarchical settings cascade. These values can disable an already mapped startup endpoint without restarting the API, but they cannot map a new endpoint when `Mcp:Enabled=false` at startup and cannot make endpoint path/stateless mode runtime-editable.

| Setting key | Default | Scope | Description |
|---|---:|---|---|
| `mcp.enabled` | `true` | Instance/Tenant | Runtime adapter switch. Effective MCP exposure is `Mcp:Enabled && resolved(mcp.enabled)`. Instance administrators can disable the adapter without changing route shape. |
| `mcp.enable_legacy_sse` | `false` | Instance/Tenant | Records legacy-SSE governance intent only. Current runtime keeps `legacySseRuntimeEnabled=false` even when startup and DB values are true. |
| `governance.lock_tenant_mcp` | `true` | Instance | Prevents tenant administrators in multi-tenant mode from overriding `mcp.enabled`. |
| `governance.lock_tenant_mcp_legacy_sse` | `true` | Instance | Prevents tenant administrators in multi-tenant mode from overriding `mcp.enable_legacy_sse`. |

MCP must not expose provider credentials, endpoint URLs, model IDs, prompts, provider responses, tool payloads, tenant IDs, or raw provider errors in configuration diagnostics, health data, logs, metrics, or browser responses. Mutating MCP tools must keep using the proposal/confirmation workflow and must never write repositories directly.

Operational expectations:

- Keep `Mcp:Enabled=false` unless an operator intentionally exposes the API-hosted endpoint to trusted MCP clients. Once the endpoint is mapped at startup, use `mcp.enabled=false` for runtime rollback without restart.
- Keep `Mcp:EndpointPath` and `Mcp:Stateless` startup-only. Runtime/admin settings must not change route shape or session posture after startup.
- Keep `Mcp:Stateless=true`; the initial adapter is designed for stateless Streamable HTTP and API replicas without MCP session affinity. Startup validation rejects `false`.
- Keep `Mcp:EnableLegacySse=false` in normal deployments. A true value is accepted only as a startup ceiling for future governance; the current runtime does not enable legacy SSE because the SDK's legacy mode requires stateful in-memory sessions and weaker backpressure than Streamable HTTP.
- Do not add a product `stdio` MCP host to the API deployment. `stdio` remains deferred by [ADR-011](adr/ADR-011-local-mcp-stdio-diagnostic-host.md) unless a future local-only diagnostic host is separately approved and verified.
- Treat MCP as API-key-first for external clients. The endpoint is mapped anonymously so SDK authorization filters can expose explicitly anonymous-safe registry discovery, while scope-aware MCP authorization policies and normal MediatR authorization still gate scoped operations. Valid `X-API-Key` requests with `mcp:read` can use MCP read resources, `mcp:propose` is required to discover/call proposal tools or proposal prompts, and no key, invalid keys, or revoked keys can use only anonymous-safe capabilities.
- Keep SDK registration explicit. The API host uses `WithTools<T>()`, `WithResources<T>()`, `WithPrompts<T>()`, and registry-projected tool options instead of assembly-wide discovery so transport/startup behavior remains reviewable and avoids avoidable reflection pressure. Native AOT compatibility is not promised until a dedicated publish profile is added and tested.
- Treat projected `propose_*` MCP tools as ergonomic wrappers only. Their payload fields come from ATCR JSON schemas, SDK annotations are hints, and execution still persists a proposed action for API/HAL confirmation.
- Treat MCP protocol evolution as ADR-gated. There is no configuration-only switch for stateful sessions, resource subscriptions, sampling, elicitation, roots, progress notifications, list-changed notifications, or client-specific compatibility shims.
- Verify `/health/ready` includes `mcp-adapter` before exposing MCP. Disabled MCP reports intentional degraded readiness posture; enabled MCP reports healthy configuration posture with `enabled`, `startupEnabled`, `runtimeEnabled`, `legacySseStartupCeiling`, `legacySseRuntimeRequested`, and `legacySseRuntimeEnabled` safe booleans, not tenant IDs or secrets.

The `ai-provider` readiness check reports safe booleans such as `endpointConfigured`, `apiKeyConfigured`, and `modelConfigured`; it never reports raw endpoint URLs, API keys, prompts, responses, model IDs, provider request IDs, or provider exception bodies.

### Cerbos Authorization Configuration

Cerbos runtime settings are the first implemented consumer of the shared secrets ownership metadata:

- `Cerbos:GrpcEndpoint` can prefill onboarding/admin forms as deployment bootstrap. Once an operator saves an application-managed endpoint, the saved setting takes precedence unless the key is explicitly deployment-managed.
- `Cerbos:AdminApi:*` configures policy package sync/status operations, not runtime authorization checks. Admin API credentials are secret-bearing and must be treated as write-only/redacted in UI and API responses.
- `Secrets:Ownership:DeploymentManagedKeys` can mark `cerbos.grpc_endpoint`, `Cerbos:AdminApi:AdminUsername`, `Cerbos:AdminApi:AdminPassword`, or `*` as deployment-managed. Deployment-managed fields are read-only in UI and ignore application-managed DB values for that field.
- Governance settings select the active provider (`AuthorizationProvider`), whether tenant customization is enabled, and per-tenant BYO values such as `cerbos.mode`, `cerbos.custom_endpoint`, `cerbos.failure_mode`, custom Admin API endpoint, and custom Admin API credentials.

Endpoint and secret safety rules:

- Non-local PDP/Admin API endpoints must use safe TLS-capable URLs. Unsafe endpoint changes are rejected before provider settings are persisted or sync/cache invalidation runs.
- Runtime failure logs must not include raw PDP/Admin API endpoints, Admin API credentials, JWTs/tokens, response bodies, or exception objects/messages.
- A tenant with `cerbos.mode=custom_endpoint` and a blank PDP endpoint remains in BYO mode. Runtime authorization applies the tenant `failure_mode` instead of falling back to the instance PDP, while any explicit BYO Admin API configuration is still preserved for package operations.
- `failure_mode=closed` activates provider-instance safe mode for local fallback decisions; `failure_mode=open` uses standard local RBAC fallback only for that tenant BYO failure path.

### Email Dispatch Scheduler Configuration

Basic Dispatch Mode uses PostgreSQL as the durable source of truth and the existing SMTP abstraction as the transport. It does **not** require RabbitMQ. Registration confirmation currently creates an `EmailDispatchOutbox` row in the registration transaction; the default TickerQ `email-dispatch-drain` cron job triggers the drain service, which claims due rows, rebinds tenant context, calls `IEmailService`, records attempts/receipts, and advances final delivery state.

Static dispatch settings bind from `EmailDispatchProcessor` and are validated at startup with `ValidateOnStart`:

| Key | Default | Description |
|---|---:|---|
| `Enabled` | `true` | Enables Basic Dispatch Mode. When disabled, the `email-dispatch` readiness check reports `Degraded` intentionally. |
| `Mode` | `TickerQ` | Selects the trigger mechanism: `TickerQ`, `HostedService`, or `Disabled`. `TickerQ` is the default scheduler; `HostedService` is a fallback timer wrapper over the same drain service. |
| `PollingIntervalSeconds` | `5` | Delay between polling loops. Must be greater than zero. |
| `BatchSize` | `50` | Maximum due outbox rows loaded per loop. Must be greater than zero. |
| `MaxAttemptCount` | `5` | Worker-level cap used with per-row `MaxAttempts` before dead-lettering. Must be greater than zero. |
| `InitialRetryDelaySeconds` | `5` | Base retry delay for failed SMTP dispatch. Must be greater than zero. |
| `MaxRetryDelaySeconds` | `3600` | Maximum retry delay cap. Must be greater than or equal to `InitialRetryDelaySeconds`. |
| `ProcessingLeaseTimeoutSeconds` | `900` | Maximum age for a `Processing` row before the recovery scan marks it `Unknown` for operator review. Must be greater than zero. |
| `ConsumerId` | machine name | Drain identity recorded in receipts and logs. Must not be blank. |
| `VerboseLogging` | `false` | Enables additional drain logs when troubleshooting. Logs must remain free of bodies, recipients, and secrets. |

SMTP settings still come from the `email.*` governance/secret keys resolved by `SmtpConfigResolver`; the dispatch processor does not introduce new SMTP credential keys. RabbitMQ Dispatch Mode is not part of Basic mode.

TickerQ host settings bind from `Scheduler:TickerQ`:

| Key | Default | Description |
|---|---:|---|
| `Enabled` | `true` | Enables the TickerQ scheduler host when `EmailDispatchProcessor:Mode=TickerQ`. If this is `false` while EmailDispatch is in `TickerQ` mode, `email-dispatch` readiness is unhealthy. |
| `Schema` | `ticker` | PostgreSQL schema for TickerQ operational tables. This is migration-backed and currently must remain `ticker`; changing it requires a matching scheduler migration strategy. |
| `MaxConcurrency` | processor count | Maximum TickerQ scheduler concurrency. Must be greater than zero. |
| `NodeIdentifier` | machine name | Scheduler node identity for multi-node diagnostics. Must not be blank. |
| `DashboardEnabled` | `false` | Enables the TickerQ dashboard. Keep disabled unless instance operators explicitly need scheduler internals. |
| `DashboardPath` | `/admin/scheduler` | Absolute non-root dashboard path when enabled. |
| `DashboardAuthorizationPolicy` | `tickerq_instance_admin` | Host authorization policy for the dashboard. Must not be blank or anonymous when dashboard is enabled. The API enforces this policy on the dashboard path before TickerQ serves dashboard content. |
| `DashboardSessionTimeoutMinutes` | `30` | Dashboard session timeout. Must be greater than zero when dashboard is enabled. |

TickerQ is scheduler state only. It must not contain email bodies, recipients, subjects, SMTP credentials, provider message IDs, raw exceptions, tenant secrets, or access tokens. The product/operator source of truth remains `EmailDispatchOutbox` and the HAL-gated EmailDispatch admin API, not the TickerQ dashboard.

### Email Dispatch RabbitMQ Configuration

RabbitMQ Dispatch Mode is optional transport infrastructure over the same PostgreSQL `EmailDispatchOutbox` state machine. The first RabbitMQ slice declares topology, publishes pointer-only dispatch messages with mandatory routing and publisher confirms, exposes a readiness check, and wires Aspire local development. It does **not** replace the Basic SMTP worker and does not make RabbitMQ required for non-Aspire/basic deployments.

Static RabbitMQ transport settings bind from `EmailDispatchRabbitMq` and are validated at startup with `ValidateOnStart`:

| Key | Default | Description |
|---|---|---|
| `Enabled` | `false` | Enables optional RabbitMQ Dispatch Mode transport checks/publishing. Disabled mode is health-safe and leaves Basic Dispatch Mode independent. |
| `ConnectionStringName` | `messaging` | Named connection string/resource used to resolve RabbitMQ, including Aspire `ConnectionStrings:messaging` or `MESSAGING_URI`. Must not be blank. |
| `ConnectionString` | unset | Optional direct AMQP URI override. Do not log or expose raw values. |
| `ExchangeName` | `explore.email-dispatch` | Durable direct exchange for pointer-only dispatch messages. |
| `DispatchQueueName` | `explore.email-dispatch.dispatch` | Durable dispatch queue bound to the exchange. |
| `DispatchRoutingKey` | `email-dispatch.dispatch` | Routing key used for mandatory pointer publishes. |
| `DeadLetterExchangeName` | `explore.email-dispatch.dlx` | Durable direct DLX for rejected/poison messages in later consumer slices. |
| `DeadLetterQueueName` | `explore.email-dispatch.dlq` | Durable DLQ bound to the DLX. |
| `DeadLetterRoutingKey` | `email-dispatch.dead-letter` | DLQ routing key configured on the dispatch queue. |
| `ParkingQueueName` | `explore.email-dispatch.parking` | Durable parking queue for future operator replay/parking tooling. |
| `ParkingRoutingKey` | `email-dispatch.parking` | Parking queue routing key. |
| `ClientProvidedName` | `explore-email-dispatch` | RabbitMQ client identity for broker/operator diagnostics. |
| `ConsumerId` | `explore-email-dispatch-rabbitmq-consumer` | Stable consumer identity that future manual-ack RabbitMQ deliveries record in `EmailDispatchReceipt`. Must not be blank. |
| `PrefetchCount` | `10` | Bounded unacknowledged delivery window for the future manual-ack consumer. Must be greater than zero; `0` is not allowed because RabbitMQ treats it as unbounded. |
| `DeadLetterReplayEnabled` | `false` | Enables the optional DLQ replay worker. Keep disabled until operators intentionally want RabbitMQ DLQ redrive/parking. |
| `DeadLetterReplayConsumerId` | `explore-email-dispatch-dlq-replay` | Stable consumer tag for the DLQ replay worker. Must not be blank. |
| `DeadLetterReplayPrefetchCount` | `5` | Bounded unacknowledged delivery window for DLQ replay. Must be greater than zero. |
| `PublishTimeoutSeconds` | `15` | Timeout around topology/publish confirm work. Must be greater than zero. |

The RabbitMQ payload is `EmailDispatchPointer`: tenant ID, stable `PublishEventId`, kind, source IDs, and optional event/registration/user IDs only. It intentionally excludes recipient email, subject, plain text body, HTML body, reply-to, provider message IDs, raw provider errors, and SMTP credentials. The DLQ replay worker validates pointer metadata against the PostgreSQL row before redriving; unsafe payloads are routed to the parking queue instead of being blindly replayed.

### Idempotency Cleanup Configuration

Write-operation idempotency uses PostgreSQL `idempotency_records` as a short-lived replay cache keyed by `(Idempotency-Key, TenantId)`. Expired records are not eligible for replay reads. The cleanup processor is an API-hosted background service that physically deletes only records whose `ExpiresAt` is older than the configured grace window.

Static cleanup settings bind from `IdempotencyCleanup` and are validated at startup with `ValidateOnStart`:

| Key | Default | Description |
|---|---:|---|
| `Enabled` | `true` | Enables the hosted cleanup loop. When disabled, the `idempotency-cleanup` readiness check reports `Degraded` intentionally. |
| `DryRun` | `false` | Counts eligible rows and emits metrics/logs without deleting. Use this before enabling destructive cleanup in a new environment. |
| `InitialDelaySeconds` | `30` | Delay before the first cleanup pass after API startup. Must be zero or greater. |
| `PollingIntervalMinutes` | `60` | Delay between cleanup passes. Must be greater than zero. |
| `BatchSize` | `500` | Maximum expired rows counted/deleted per pass. Must be greater than zero. |
| `ExpirationGraceHours` | `24` | Safety buffer after `ExpiresAt` before a row is eligible for physical delete. Must be zero or greater. |

Cleanup is instance/system-scoped because idempotency rows are ephemeral replay-cache entries, not tenant-owned source-of-truth or compliance evidence. Logs, health data, and metrics expose only bounded settings/counts; they must not include raw idempotency keys, request paths, response bodies, or tenant IDs.

### AI Retention Cleanup Configuration

AI assistant history retention is tenant-owned source-of-truth data, so the hosted cleanup worker iterates active tenants, binds tenant context per tenant, resolves each tenant's `ai_assistant.retention_days`, and then invokes the tenant-filtered redaction cleanup path. The worker does not bypass tenant filters and must not log prompts, provider responses, tool payloads, selected reference content, tenant IDs, or provider secrets.

Static scheduler settings bind from `AiRetentionCleanup` and are validated at startup with `ValidateOnStart`:

| Key | Default | Description |
|---|---:|---|
| `Enabled` | `true` | Enables the API-hosted AI retention cleanup loop. When disabled, the `ai-retention-cleanup` readiness check reports `Degraded` intentionally. |
| `DryRun` | `false` | Counts eligible expired AI conversations across active tenants without redacting or soft-deleting rows. Use this before destructive cleanup in a new environment. |
| `InitialDelaySeconds` | `30` | Delay before the first cleanup pass after API startup. Must be zero or greater. |
| `PollingIntervalMinutes` | `60` | Delay between cleanup passes. Must be greater than zero. |
| `MaxTenantsPerPass` | `100` | Maximum active tenant lookups processed per cleanup pass. Must be greater than zero. |

The per-tenant retention window still comes from the governance setting `ai_assistant.retention_days` (default 30 days). Static `AiRetentionCleanup:*` settings only control the scheduler posture, dry-run mode, and pass bounds.

## Secret Provider Configuration

`Explore.Secrets` binds provider config from `SecretProvider`:

- `SecretProvider:Provider` (default `None`)
- `SecretProvider:FailFast`
- `SecretProvider:Infisical:*` (project/client credentials, paths, environment)

Refresh behavior binds from `SecretRefresh` and runs via hosted `SecretRefreshService`.

## Setup Secret And Managed Provisioning Bootstrap

`SetupSecretProvider` reads setup and managed-provisioning bootstrap keys directly from configuration at API startup.

| Key | Default | Purpose |
|---|---|---|
| `SETUP_SECRET` | generated 32-character startup secret | Optional fixed setup secret for interactive first-run onboarding. When omitted and setup mode is active, API generates a temporary secret and logs it for the operator. |
| `SETUP_SECRET_REQUIRED` | `true` | Controls whether interactive setup endpoints can validate a setup secret. `false` is effective only when trusted managed provisioning is explicitly configured; otherwise the provider fails closed and still requires a setup secret. |
| `PROVISIONING_TRUSTED` | `false` | Must be `true` before managed-provider provisioning can disable interactive setup-secret validation. |
| `PROVISIONING_MODE` | unset | Trusted values are managed-provider modes such as `managed-provider`, `managed_provider`, `managed-hosting`, or `managed`. Other values do not disable setup-secret validation. |
| `MANAGED_CLIENT_EXTERNAL_PROVIDER` | unset | Stable external provider key for the managed provisioning operator, for example an ERP or hosting-provider key. Required when `SETUP_SECRET_REQUIRED=false`. |
| `PHYSICAL_TENANCY_MODE` | unset | Deployment posture such as shared database or dedicated deployment. Required when `SETUP_SECRET_REQUIRED=false` so the operator has declared the physical tenancy model. |

Important safety behavior:

- Omitted `SETUP_SECRET_REQUIRED` defaults to `true`.
- `SETUP_SECRET_REQUIRED=false` without all trusted managed-provisioning keys is ignored and the API still requires a setup secret.
- `SETUP_SECRET_REQUIRED=false` with trusted managed provisioning does **not** make setup-secret-protected endpoints public. `ValidateSecret` returns false and those endpoints reject anonymous/no-secret calls; managed provider automation must use the authorized provisioning endpoint instead.
- Raw setup secrets are not logged when interactive setup-secret validation is disabled.

## API Compatibility Mapping (Infisical -> .NET keys)

`Explore.API.Extensions.ConfigurationExtensions` maps compatibility names into canonical .NET keys. Most mappings use `TrySet`, so existing canonical keys are not overwritten; `CERBOS_GRPC_ENDPOINT` explicitly assigns `Cerbos:GrpcEndpoint` when present.

- `DEPLOYMENT_MODE` (Infisical `/api`) -> `Deployment:Mode` (`single_tenant`/`multi_tenant` normalized to `SingleTenant`/`MultiTenant`)
- `MCP_ENABLED`, `MCP_ENDPOINT_PATH`, `MCP_STATELESS`, `MCP_ENABLE_LEGACY_SSE` (Infisical `/api` or `/mcp`) -> `Mcp:Enabled`, `Mcp:EndpointPath`, `Mcp:Stateless`, `Mcp:EnableLegacySse`; bare endpoint paths such as `mcp` normalize to `/mcp`, and `MCP_ENABLE_LEGACY_SSE` is a startup ceiling only
- `KEYCLOAK_ENDPOINT` + `KEYCLOAK_REALM` (Infisical `/keycloak`) -> `Keycloak:Authority`, `Keycloak:MetadataAddress`, `Keycloak:AuthorizationUrl`
- Keycloak mapper defaults -> `Keycloak:Audience=islamu-event-api`, `Keycloak:RequireHttpsMetadata=true`
- `CERBOS_GRPC_ENDPOINT` (Infisical `/cerbos`) -> `Cerbos:GrpcEndpoint`
- S3 runtime values:
  - `ISLAMU_EVENT_REGION` -> `S3Settings:Region`
  - `ISLAMU_EVENT_PRIVATE_BUCKET_NAME` -> `S3Settings:BucketName`
  - `ISLAMU_EVENT_PRIVATE_ACCESS_KEY_ID` -> `S3Settings:AccessKeyId`
  - `ISLAMU_EVENT_PRIVATE_SECRET_ACCESS_KEY_ID` -> `S3Settings:SecretAccessKey`
  - `ISLAMU_EVENT_S3_ENDPOINT` -> `S3Settings:Endpoint`
  - `ISLAMU_EVENT_S3_PUBLIC_ENDPOINT` -> `S3Settings:PublicEndpoint`

Keycloak base URL: `KEYCLOAK_ENDPOINT` (Infisical `/keycloak`). No hardcoded fallback — if not set, Keycloak mapping is skipped.

Storage naming rules:

- local filesystem runtime settings use `Storage:Local:*`;
- local filesystem Compose/environment overrides use `Storage__Local__*`;
- optional S3-compatible runtime settings use `S3Settings:*`;
- optional S3-compatible Compose/environment overrides use `S3Settings__*`;
- reconciliation worker settings use `StorageReconciliation:*` or `StorageReconciliation__*`;
- Infisical/domain secret definitions use the `STORAGE_S3_*` key family under storage paths;
- do not expose or persist deployment-managed local filesystem paths through tenant/admin setting keys.

## Blazor Server Compatibility Mapping

`Explore.Blazor.Extensions.ConfigurationExtensions` maps Keycloak, Google, and API base URL keys for the BFF host.

API base URL: `API_ENDPOINT` (Infisical `/blazor`) maps into `ExploreApi:BaseUrl`. Runtime YARP resolution checks `ExploreApi:BaseUrl`, then Aspire service discovery keys `services__explore-api__https__0` and `services__explore-api__http__0`, then falls back to `https://localhost:7039/` when no value is configured at all.

Important behavior:

- `KEYCLOAK_CLIENT_ID` maps to `Keycloak:ClientId`; when a Keycloak authority is resolved and no client id is provided, the BFF defaults to `islamu-event-blazor`.
- `Keycloak:ClientSecret` is explicitly overridden when `KEYCLOAK_BLAZOR_CLIENT_SECRET` (Infisical) is present.
- `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET` map to `Google:ClientId` and `Google:ClientSecret`.

Compose-managed Keycloak adds one bootstrap-specific rule: `docker/keycloak/keycloak-init.sh` writes `KEYCLOAK_BLAZOR_CLIENT_SECRET` into the imported `islamu-event-blazor` client before API/Blazor startup is allowed to complete. `KEYCLOAK_API_CLIENT_SECRET` is optional and only updates the `islamu-event-api` resource-server client when provided; the current API bearer-token validation path does not consume an API client secret. The Keycloak admin username/password are used only by the one-shot Compose init job and must not be stored as runtime application settings.
- `Keycloak:RequireHttpsMetadata` is set to `true` when Keycloak input is mapped.

External-Keycloak onboarding uses a different secret boundary. The setup UI can send a one-time Keycloak bootstrap username/password to `POST /api/InstanceOnboarding/auth-provider-configuration/keycloak-bootstrap` through the BFF. That credential is request-scoped input for the Infrastructure Keycloak Admin API adapter; it is not a configuration key, not a governance setting, not a secret-provider key, and not persisted by ISLAMU. Successful bootstrap persists only the normal runtime Keycloak auth-provider configuration: authority, Blazor client ID, and Blazor client secret.

External bootstrap URL safety is enforced before network calls. Keycloak base URLs must be absolute HTTP/HTTPS URLs without embedded user info, query string, or fragment. Literal localhost, loopback, link-local, unspecified, and multicast IP hosts are rejected by the Infrastructure adapter; self-hosted/internal DNS hostnames remain allowed so operators can use private Keycloak service names intentionally.

## Governance Settings (Database)

Governance keys are centralized in `Explore.Domain.Constants.GovernanceSettingKeys`.

Major groups:

- `deployment.*`
- `tenants.*`
- `routing.*` and `routing.render_policy.*`
- `events.*`
- `organizations.*`
- `modules.*`
- `branding.*`
- `domains.*`
- `email.*`
- `s3.*`
- `authorization.*`
- `cerbos.*`
- `analytics.*`
- `ai_assistant.*`
- `auth.*`
- `federation.*`
- `localization.*`

Values are stored as JSON-serialized strings in `SystemSetting.Value` and `TenantSetting.Value`.

Sensitive runtime credentials use a separate secret-setting key space. Do not expose actual values in documentation, logs, screenshots, or issue templates.

| Concern | Governance key family | Secret-bearing key family |
|---|---|---|
| SMTP | `email.*` | `email.smtp_username`, `email.smtp_password` |
| Optional S3-compatible storage | `s3.*` | `s3.access_key_id`, `s3.secret_access_key` |
| Authentication | `auth.*` | `auth.keycloak_client_secret`, `auth.google_client_secret` |
| Cerbos admin credentials | `cerbos.*` | `cerbos.custom_admin_username`, `cerbos.custom_admin_password` |
| AI assistant | `ai_assistant.*` | `ai_assistant.api_key` |

`SecretDefinitionRegistry` recognizes provider folders for `/api`, `/storage`, `/keycloak`, `/cerbos`, `/postgresql`, `/smtp`, `/analytics`, and `/ai`. Blazor maps Google client values from `/blazor`; do not claim Google is part of the current secret-catalog folder list unless the registry changes.

## AI Assistant Settings (Governance)

AI assistant configuration is governed through `ai_assistant.*` keys. The Application layer resolves these into `AiAssistantSettingGroup`; provider SDKs and concrete network clients stay behind Infrastructure adapters and must not leak into Domain, Application DTOs, browser responses, logs, or tests.

Canonical keys:

| Key | Type | Default | Description |
|---|---|---|---|
| `ai_assistant.enabled` | bool | `false` | Master enable switch. Disabled remains the safe default until provider health, egress validation, auth, quotas, and retention gates are implemented. |
| `ai_assistant.provider` | string | `"none"` | Tenant/runtime provider intent. Static `AiProvider:*` controls concrete Infrastructure wiring and currently supports `none`, `fake`, `openai-compatible`, `openai-sdk`, and `azure-openai`. `fake` is for deterministic tests/dev flows; real providers require model and credential configuration. |
| `ai_assistant.endpoint_url` | string | `""` | Provider base URL for self-hosted or OpenAI-compatible adapters. This is deployment/admin-controlled; browser or request payloads must never choose outbound provider hosts. |
| `ai_assistant.api_key` | string | `""` | Sensitive provider credential. Treat as write-only/redacted; never expose to Blazor, API responses, logs, screenshots, traces, or issue templates. |
| `ai_assistant.model_id` | string | `""` | Default model ID. Real providers are not considered configured unless both API key and model ID are present. |
| `ai_assistant.max_input_tokens` | int | `8000` | Prompt/context budget used before provider calls. Handlers must still enforce bounded context and prompt length. |
| `ai_assistant.max_output_tokens` | int | `1024` | Maximum requested provider completion size. |
| `ai_assistant.temperature` | decimal | `0.2` | Provider sampling temperature. Keep low for structured assistant workflows. |
| `ai_assistant.timeout_seconds` | int | `30` | Provider call timeout budget. Cancellation tokens must still flow through all calls. |
| `ai_assistant.retention_days` | int | `30` | Default persisted conversation retention window. The Phase 8 retention cleanup operation uses this tenant-resolved value to select expired conversations, supports dry-run, redacts prompt/action/reference/failure content, and soft-deletes expired conversation shells. Scheduling/operator automation is tracked separately from the cleanup primitive. |
| `ai_assistant.daily_message_limit` | int | `50` | Per-user daily assistant message limit enforced before provider calls. |
| `ai_assistant.daily_tenant_message_limit` | int | `1000` | Per-tenant daily assistant user-message limit enforced before provider calls. |
| `ai_assistant.concurrent_run_limit` | int | `1` | Per-user concurrent assistant run limit. Existing idempotency replays are allowed before this quota check. |
| `ai_assistant.selected_reference_limit` | int | `8` | Maximum selected references that future reference-aware prompts may pack into one request. |
| `ai_assistant.tool_proposals_enabled` | bool | `false` | Allows provider output to become persisted proposed actions only. Mutating tools still require server validation, HAL affordance checks, user confirmation, idempotency, and audit before execution. |
| `ai_assistant.streaming_enabled` | bool | `false` | Reserved for a future streaming transport. Current AI assistant run progress uses authenticated polling through `GET /api/ai/assistant/conversations/{conversationId}/runs/{runId}`; keep this disabled until streaming transport buffering, cancellation, timeout, authentication, logging safety, and polling fallback are explicitly implemented and verified. |
| `ai_assistant.allow_anonymous_access` | bool | `false` | Legacy/public-availability flag for safe bootstrap surfaces only. Private conversation/history/send/action endpoints must remain authenticated. |

Important notes:

- `AiAssistantSettingGroup.IsConfigured` treats `fake` as configured for deterministic tests, but `openai-compatible` requires both `ai_assistant.api_key` and `ai_assistant.model_id`.
- Provider output is untrusted data. It may produce structured action candidates, but those candidates must be persisted as proposals and require explicit confirmation before any write command runs.
- Do not log raw prompts, model responses, selected reference content, provider request IDs tied to content, endpoint credentials, or provider exception bodies.
- Provider endpoint URLs are deployment/admin-controlled. Browser payloads and per-request DTOs must never choose outbound provider hosts.
- Assistant send requests are also protected by API rate limiting under `RateLimiting:AiAssistant` and by Application-level per-user daily, per-tenant daily, and per-user concurrent-run quotas. Quota failures return safe ProblemDetails and do not call the provider.
- Tenant delegation/admin editing for the expanded provider/model/limit settings is intentionally separate from defining the keys. Do not assume a key is tenant-admin editable until the tenant policy service and UI explicitly expose it.

## Analytics Settings (Governance)

Analytics configuration is governed entirely through `analytics.*` keys.
The runtime abstraction is optional by design: instance admins can lock a shared provider,
leave settings unlocked so tenants can bring their own provider, or disable analytics entirely.

Canonical keys:

| Key | Type | Default | Description |
|---|---|---|---|
| `analytics.provider` | string | `"none"` | Active provider: `none`, `posthog`, `plausible`, `rybbit`, `rudderstack` |
| `analytics.enabled` | bool | `false` | Master enable switch for analytics emission |
| `analytics.consent_mode` | string | `"pseudonymous"` | Privacy mode: `anonymous`, `pseudonymous`, `identified` |
| `analytics.transport_mode` | string | `"direct"` | Browser transport: `direct`, `proxy`, `relay` |
| `analytics.api_key` | string | `""` | Public or write key used by the active provider |
| `analytics.endpoint_url` | string | `""` | Provider base URL, especially important for self-hosted deployments |
| `analytics.personal_api_key` | string | `""` | Sensitive key used for advanced provider features such as PostHog feature flags |

Important notes:

- `analytics.endpoint_url` is the canonical endpoint key. Do not introduce `analytics.endpoint`.
- There is no canonical `analytics.site_id` governance key in the current abstraction.
- The analytics settings follow the standard settings cascade: system setting -> tenant override -> system default.
- Sensitive keys should still be treated carefully in UI and operational workflows even when stored as governance values.
- `analytics.transport_mode=relay` is the only mode that does not require a browser-exposed `analytics.api_key`; the browser posts first-party events to `/api/a/t` and the server uses the resolved provider settings.
- `analytics.transport_mode=proxy` still uses the provider script/client in the browser, but the script host and ingest host should usually point at a first-party reverse-proxy path through `analytics.endpoint_url`.
- `analytics.consent_mode=identified` only enables identify semantics for providers that explicitly support them today (`posthog`, `rudderstack`).

Cookie consent and privacy governance keys:

| Key | Type | Default | Description |
|---|---|---|---|
| `analytics.global_disable_client_tracking` | bool | `false` | Emergency kill switch — disables all **browser-side** analytics immediately. Server-side relay endpoints and server analytics continue normally. Scope: browser SDK initialization only. |
| `analytics.cookie_consent_enabled` | bool | `false` | Whether the cookie consent banner is shown to end users |
| `analytics.decline_behavior` | enum | `"cookieless"` | What happens when a user declines consent: `disable` (no analytics) or `cookieless` (privacy-preserving analytics) |
| `analytics.consent_cookie_lifetime_days` | int | `180` | How long the consent preference cookie persists (ICO recommends 6 months) |
| `analytics.posthog_cookieless_mode` | enum | `"on_reject"` | PostHog cookieless mode: `off`, `always` (never stores on device), `on_reject` (cookieless after decline) |
| `analytics.posthog_person_profiles` | enum | `"identified_only"` | PostHog person profile creation: `always`, `identified_only`, `never` |
| `analytics.posthog_session_replay` | bool | `false` | PostHog session recording (non-essential, requires consent) |
| `analytics.posthog_autocapture` | bool | `false` | PostHog autocapture of clicks/inputs (non-essential) |
| `analytics.posthog_heatmaps` | bool | `false` | PostHog heatmap data collection (non-essential) |
| `analytics.posthog_toolbar` | bool | `false` | PostHog toolbar for on-page debugging |

Storage-mode-driven consent rules:

- The cookie banner requirement is **not** determined by provider name alone. It is determined by whether the provider's configured runtime mode stores or accesses non-essential data on the user's device.
- `plausible` and `rybbit`: cookieless by design, no banner required by default.
- `posthog` with `cookieless_mode=always`: no banner required (no device storage).
- `posthog` with `cookieless_mode=on_reject`: banner required; decline switches to cookieless analytics instead of total silence.
- `posthog` with `cookieless_mode=off` and any non-essential feature enabled: banner required.
- `rudderstack`: treated as "full consent required" for v1.
- The computed storage profile (`Cookieless`, `ConsentManaged`, `FullConsent`) drives all runtime behavior through `IAnalyticsRuntimeProfileResolver`.

Consent cookie design:

- Cookie name is tenant-scoped: `explore_cc_{stableShortKey}` where the stable key is derived from the first 8 hex characters of the tenant's immutable GUID (not the mutable subdomain slug). This prevents cookie orphaning when a tenant renames their subdomain.
- Cookie value is minimal: `accepted` or `declined` only. No timestamps, user IDs, or tracking data.
- Cookie scope: per effective public host/tenant experience. `SameSite=Lax`, `Secure`, `path=/`. Consent is not shared across subdomains or tenants.
- The consent cookie itself is classified as strictly necessary (remembering the user's choice).

Post-onboarding management note:

- Instance admins can update analytics governance values through `PUT /api/InstanceOnboarding/analytics-governance`.
- Instance admins can update authentication provider governance values through `PUT /api/instance/settings/auth-provider`.
- Instance admins can update the active authorization provider through `PUT /api/instance/settings/authz-provider`.
- Secret values (`keycloak`/`google` client secrets) continue to use secret-setting storage, not plain governance values.

## Settings Cascade Rules

`HierarchicalSettingsResolver` resolves settings through the current scope hierarchy:

1. instance/system setting,
2. tenant setting when a tenant scope is present and the instance setting is not locked,
3. organization setting when an organization scope is present and upstream settings allow delegation,
4. group setting when a group scope is present and upstream settings allow delegation,
5. user setting when a user scope is present and upstream settings allow delegation,
6. default setting definition value.

Cache behavior uses hierarchical cache keys such as `HierSettings:System` and scope-specific keys for tenant, organization, group, and user settings. The resolver honors lock flags so a higher-scope locked value prevents lower-scope overrides.

Runtime resolvers may add more specific precedence. For local storage, the deployment-managed `Storage:Local:*` section is the root authority. For S3, `S3ConfigResolver` reads database settings first (`s3.*` and `s3.access_key_id`/`s3.secret_access_key`) and falls back to `IConfiguration` (`S3Settings:*`). For SMTP, `SmtpConfigResolver` reads through the hierarchical settings resolver for governance and secret-bearing email keys.

## Deployment Mode Configuration

Static deployment config is bound from `Deployment` section (`DeploymentSettings`):

- `Mode`: `SingleTenant` or `MultiTenant` (default `SingleTenant`)
- `DefaultTenantId`
- `HidePlatformAdminInSingleTenant` (default `true`)
- `DefaultTenantSubdomain`

First-run onboarding mode is controlled only by API configuration. Set `DEPLOYMENT_MODE=multi_tenant` in the Infisical `/api` folder to show the multi-tenant onboarding flow. If `DEPLOYMENT_MODE` is absent, onboarding is single-tenant only. Invalid deployment-mode values fail safely to single-tenant setup.

Normal admin UI does not switch deployment mode after onboarding. Choose multi-tenant mode before first launch by setting the API environment value; otherwise the convention-first path launches a single-tenant site with the default tenant hidden internally.

## Reverse Proxy Trust Configuration

`Explore.API` binds trusted forwarded-header settings from `ForwardedHeadersTrust`:

- `ForwardLimit` (default `1`)
- `TrustLoopbackProxy` (useful for local/test proxy chains)
- `KnownProxies` (IP list)
- `KnownNetworks` (CIDR list)

Important behavior:

- if no trusted proxy boundary is configured, forwarded host/IP processing is disabled in the API host;
- host-derived tenant resolution and proxy-aware rate limiting rely on normalized request values after trusted forwarded-header processing, not on raw `X-Forwarded-*` headers.

Runtime nuance:

- Before onboarding completes, tenant resolution uses a single-tenant fallback so setup endpoints remain reachable.
- Onboarding persists the configured API deployment mode into the database.
- After onboarding, deployment mode is operator-controlled. Runtime admin switching is disabled in the normal governance UI; change mode only through an explicit operator migration path.

## Localization / TMS Settings (Governance)

Keys in `GovernanceSettingKeys.Localization`:

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `localization.default_language` | string | `"en"` | Default language code (ISO 639-1) |
| `localization.tms_provider` | int | `0` | TMS provider: 0=None (offline), 1=Tolgee, 2=Weblate |
| `localization.tms_api_url` | string | `null` | Base URL for the TMS REST API |
| `localization.tms_project_id` | string | `null` | TMS project identifier |
| `localization.tms_component` | string | `null` | Weblate component slug (Weblate only) |

Current localization/TMS settings are governance settings. Do not document TMS API keys or tokens as `SecretProvider`-backed until secret definitions or resolver support exist in source.

See [LOCALIZATION.md](LOCALIZATION.md) for full architecture.

## Custom Property Quotas (Governance)

Hard-limit quota definitions for Layer 3 custom properties (Rule 16). Each has a tenant-overridable default and a platform maximum.

| Key | Type | Default | Description |
|---|---|---|---|
| `custom_properties.max_definitions_per_tenant_per_entity_scope` | int | `500` | Max definitions per (Org/Group/Event). Max: 5000. |
| `custom_properties.max_definitions_per_event` | int | `100` | Max runtime definitions per Event. Max: 1000. |
| `custom_properties.max_definitions_per_event_session` | int | `50` | Max runtime definitions per Session. Max: 500. |
| `custom_properties.max_options_per_definition` | int | `200` | Max option rows per definition. Max: 2000. |
| `custom_properties.max_multi_value_rows_per_value` | int | `20` | Max rows for multi-valued property. Max: 200. |
| `custom_properties.projection_rebuild_batch_size` | int | `500` | Batch size for projection worker. Max: 5000. |
| `custom_properties.projection_discovery_enabled` | bool | `false` | Tenant feature flag for projection-backed search/filter. |

## Tenant Delegation & Locking (Governance)

| Key | Type | Default | Description |
|---|---|---|---|
| `governance.lock_tenant_smtp` | bool | `false` | Prevent tenant from overriding instance SMTP |
| `governance.lock_tenant_storage` | bool | `false` | Prevent tenant from overriding instance storage policy |
| `governance.lock_tenant_analytics` | bool | `false` | Prevent tenant from overriding instance analytics |
| `governance.lock_tenant_ai_assistant` | bool | `false` | Prevent tenant from overriding instance AI assistant |

## Event List Visibility (Governance)

| Key | Type | Default | Description |
|---|---|---|---|
| `event_list.browse_mode` | string | `"Standard"` | Default browse experience |
| `event_list.page_size` | int | `20` | Default items per page |
| `event_list.card.show_organizer` | bool | `true` | Show organization in cards |
| `event_list.card.show_price` | bool | `true` | Show price in cards |
| `event_list.card.show_tags` | bool | `true` | Show tags in cards |

## External API Key Defaults

Non-interactive callers use long-lived `{keyId}.{secret}` credentials. Per-key policy defaults are applied at create time by `ExternalApiKeyQuotaDefaults` and `ExternalApiKeyScopeCeiling`.

### Quota Defaults by Owner Type

| Owner Type | Default Period | Default Request Limit | Rationale |
|---|---|---|---|
| `User` (`1`) | `Daily` | `1,000` | Per-user automation, usually tied to a single developer |
| `Organization` (`2`) | `Monthly` | `10,000` | Team-scale automations and integrations |
| `Group` (`3`) | `Monthly` | `5,000` | Smaller scope than an org but shared by multiple members |
| `Tenant` (`4`) | `Monthly` | `50,000` | Tenant-wide admin automation |
| `InstanceAdmin` (`5`) | `None` | unlimited | Platform operator usage, rate-limited only by node-local policies |

All defaults are overridable per key via `PUT /api/ExternalApiKey/{id}`.

### Scope Ceilings by Owner Type

- `User`: `events:read`, `events:write`, `users:read`, `users:write`, `lookups:read`, `registrations:write`, `api-keys:manage`
- `Organization`: User scopes plus `organizations:read`, `organizations:write`
- `Group`: User scopes plus `groups:read`, `groups:write`
- `Tenant`: All of the above plus `admin:tenant`
- `InstanceAdmin`: All scopes including `admin:instance`

Validators (`CreateExternalApiKeyDtoValidator`, `UpdateExternalApiKeyPolicyDtoValidator`) reject requests containing scopes above the owner ceiling.

### Forwarded Headers Interaction

External API keys are often presented by callers behind reverse proxies. The `ForwardedHeadersTrust` settings (see above) determine which proxies are trusted to forward `X-Forwarded-For`/`X-Forwarded-Host`. When an API-key caller comes through a trusted proxy, the rate-limit partition key remains `api-key:{keyId}` — the forwarded IP is used only for `LastUsedIp` telemetry and logging.

When `TrustLoopbackProxy=true` (Aspire-style local development), loopback proxies are trusted for forwarded headers but untrusted proxies still have their `X-Forwarded-*` headers dropped before middleware sees them.

## Related

- [SECRETS.md](SECRETS.md)
- [SELF_HOSTING.md](SELF_HOSTING.md)
- [MULTI_TENANCY.md](MULTI_TENANCY.md)
- [RENDER_POLICIES.md](RENDER_POLICIES.md)
- [OPERATIONS.md](OPERATIONS.md)
- [CUSTOM_PROPERTIES.md](CUSTOM_PROPERTIES.md)
- [LOCALIZATION.md](LOCALIZATION.md)
