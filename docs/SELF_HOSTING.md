ABOUTME: Production self-hosting guide covering Docker Compose stack, configuration, and operations.
ABOUTME: Covers infrastructure services, setup secret, migrations, health checks, backups, and upgrades.

# Self-Hosting

> **Audience:** Operators
> **Status:** Implemented
> **Owner:** Platform/Ops
> **Last Verified:** 2026-07-03
> **Source Anchors:** `docker-compose.yml`, `Explore.AppHost/AppHost.cs`, `Event.MigrationService/`, `Explore.API/Program.cs`, `Explore.API/Controllers/InstanceSettingsController.cs`, `Explore.Infrastructure/Services/SetupSecretProvider.cs`, `Explore.Infrastructure/Services/Keycloak/KeycloakBootstrapService.cs`, `Explore.Blazor/Extensions/YarpProxyExtensions.cs`

This guide covers running ISLAMU Event outside the Aspire developer loop. The repository `docker-compose.yml` is the current self-hosting source of truth.

## Runtime Topology

| Service | Required | Compose Name | Purpose | Public Port |
|---|---:|---|---|---|
| PostgreSQL | Yes | `postgres` | Application database | internal |
| Redis | Yes | `redis` | Distributed cache when configured | internal |
| Mailpit | Local default | `mailpit` | Local SMTP capture and web UI for email testing | `1025:1025`, `8025:8025` |
| Keycloak DB | Yes | `keycloak-db` | Keycloak PostgreSQL database | internal |
| Keycloak | Yes | `keycloak` | OIDC identity provider and realm import | `8080:8080` |
| Keycloak Init | Yes | `keycloak-init` | One-shot client-secret synchronization after realm import | internal |
| API | Yes | `islamu-event-api` | REST API, migrations, health, metrics, local storage volume | `7039:8080` |
| Blazor BFF | Yes | `islamu-event-ui` | Server host, embedded admin-host shell, and YARP proxy to API | `7002:8080` |
| MinIO | Optional | `minio`, `minio-init` | S3-compatible storage profile when an instance selects optional S3 mode | `9005:9000`, `9006:9001` |
| Cerbos | Optional | `cerbos` | External authorization PDP profile | `3592:3592`, `3593:3593` |
| Svix | Optional | `svix-db`, `svix` | Outgoing webhook provider profile | `8071:8071` |
| Coop | Optional | `coop-db`, `coop` | Moderation review queue profile | `8082:8080` |
| Osprey coordinator | Optional | `osprey` | Roost Osprey coordinator profile | `19950:19950`, `19951:19951` |
| AI provider | Optional | external/self-hosted | Official, compatible, or fake AI assistant provider selected by `AiProvider:*` plus tenant governance settings | deployment-specific |

Profiles:

- `storage` starts MinIO and creates the configured bucket for optional S3-compatible mode. It is not required for local-first storage.
- `authz` starts Cerbos for deployments that select Cerbos authorization. It mounts `cerbos/config/.cerbos.yaml`, `cerbos/init/cerbos-schema.sql`, and `cerbos/policies/` from this repository so local infrastructure always uses the checked-in policy package. The Cerbos PostgreSQL profile uses the Postgres 18 parent data mount (`/var/lib/postgresql`) so local upgrades do not fail on the legacy `/var/lib/postgresql/data` mount boundary.
- `webhooks` starts local Svix and its PostgreSQL database for outgoing webhook-provider testing.
- `moderation` starts local Coop for moderation review-queue testing with an isolated PostgreSQL database. `COOP_UI_URL` defaults to `http://localhost:8082`, matching the local Coop port; the local profile also supplies Coop's `DATABASE_*`, `SESSION_SECRET`, `OTEL_SERVICE_NAME`, placeholder Scylla client settings, and no-op warehouse/analytics adapter defaults so the provider image can boot without ClickHouse or production secrets. Keep `REPORTING_MODE=LocalOnly` unless provider endpoints are intentionally enabled.
- `osprey` starts `ghcr.io/roostorg/osprey/osprey-coordinator:latest` by default. It exposes coordinator ports, not the app's HTTP-compatible `Reporting:Osprey` facade. Keep `REPORTING_OSPREY_ENABLED=false` unless you provide a compatible HTTP facade or adapter endpoint.

Email dispatch modes:

- **Basic Dispatch Mode is implemented and requires no extra Compose profile.** API + PostgreSQL + configured SMTP are sufficient for registration confirmation email. By default, API-hosted TickerQ schedules `email-dispatch-drain`, which claims `EmailDispatchOutbox` rows from PostgreSQL through the shared drain service and sends through the SMTP abstraction. `EmailDispatchProcessor:Mode=HostedService` is a fallback trigger, not a separate business workflow.
- The Compose default SMTP target is local Mailpit: API containers use SMTP host `mailpit`, port `1025`, encryption `None`, and from-address `noreply@localhost`. Open the capture UI at `http://localhost:8025`. Replace `MAIL_SMTP_*` values before using a real external SMTP provider.
- **RabbitMQ Dispatch Mode is optional transport infrastructure.** The repository now has a local Aspire RabbitMQ resource plus API-side pointer publishing, topology declaration, health checks, manual-ack consumption, and DLQ replay/parking for pointer-only dispatch messages. The production Compose stack still does not require or start RabbitMQ by default. Enabling RabbitMQ outside Aspire requires an operator-provided broker connection string and an explicit `EmailDispatchRabbitMq:Enabled=true` opt-in. RabbitMQ shares the same PostgreSQL `EmailDispatchOutbox` state machine; it is transport only.

## Start The Stack

1. Create an environment file with secrets required by `docker-compose.yml`.

   ```bash
   cp .env.example .env
   ```

   `.env` is ignored by git and is used by Docker Compose for interpolation. `.env.example` contains disposable local defaults that should run a first local stack; replace them before using any shared, staged, or production environment. Before starting containers, inspect the resolved Compose model:

   ```bash
   docker compose config
   ```

2. Start the required stack:

   ```bash
   docker compose up -d postgres redis mailpit keycloak-db keycloak keycloak-init islamu-event-api islamu-event-ui
   ```

3. Add optional MinIO only when S3-compatible storage is selected or tested:

   ```bash
   docker compose --profile storage up -d
   ```

4. Add optional Cerbos when using the Cerbos provider:

   ```bash
   docker compose --profile authz up -d
   ```

5. Add optional Svix when using the outgoing webhook provider:

   ```bash
   docker compose --profile webhooks up -d
   ```

6. Add optional Coop for moderation integration testing:

   ```bash
   docker compose --profile moderation up -d
   ```

7. Add optional Osprey-compatible signal provider only after configuring an accessible image:

   ```bash
   docker compose --profile osprey up -d
   ```

8. Add the optional separate control-plane BFF only for multi-tenant operator deployments that want a distinct process/host:

   ```bash
   docker compose --profile control-plane up -d islamu-event-control-plane
   ```

9. Open Blazor at `http://localhost:7002`, the optional control plane at `http://localhost:7003`, and API at `http://localhost:7039`.

## Required Environment Keys

Use the key names consumed by the Compose file and source code. Do not invent generic aliases.

### Application Database Bootstrap

`Explore.Secrets/Bootstrap/BootstrapSecretLoader.cs` expects discrete PostgreSQL values. Do not provide only a URL-form database secret.

| Key | Purpose |
|---|---|
| `POSTGRESQL_HOST` | Application DB host, normally `postgres`. |
| `POSTGRESQL_PORT` | Application DB port, normally `5432`. |
| `POSTGRESQL_DATABASE` | Application DB name. |
| `POSTGRESQL_USERNAME` | Application DB username. |
| `POSTGRESQL_PASSWORD` | Application DB password. |

Compose derives the local `postgres` container bootstrap values from `POSTGRESQL_DATABASE`, `POSTGRESQL_USERNAME`, and `POSTGRESQL_PASSWORD` so the container and application use the same credentials.

### Keycloak

| Key | Purpose |
|---|---|
| `KEYCLOAK_DB_DATABASE`, `KEYCLOAK_DB_USERNAME`, `KEYCLOAK_DB_PASSWORD` | Local Keycloak database bootstrap values. |
| `KEYCLOAK_ADMIN`, `KEYCLOAK_ADMIN_PASSWORD` | Initial Keycloak admin account. |
| `KEYCLOAK_ENDPOINT` | Base Keycloak endpoint used to derive API/Blazor authority values. |
| `KEYCLOAK_REALM` | Realm name. |
| `KEYCLOAK_BLAZOR_CLIENT_SECRET` | Required Blazor confidential client secret. `keycloak-init` writes this into the `islamu-event-blazor` Keycloak client after realm import. |
| `KEYCLOAK_CONTROL_PLANE_CLIENT_ID` | Optional separate control-plane confidential client id. Default: `islamu-event-control-plane`. |
| `KEYCLOAK_CONTROL_PLANE_CLIENT_SECRET` | Required only when the optional control-plane profile is enabled. `keycloak-init` writes this into the `islamu-event-control-plane` Keycloak client after realm import. |
| `KEYCLOAK_API_CLIENT_SECRET` | Optional legacy/future API resource-server client secret. Current bearer-token validation does not require it, and the checked-in realm export does not include a static API client secret. Set it only if a deployment intentionally makes the API client confidential. |
| `KEYCLOAK_INIT_ALLOW_DEFAULT_LOCAL_SECRET` | Optional local-development escape hatch. Set to `true` only for throwaway local stacks that intentionally use the static realm-export default secret. |

### Cerbos

Local Compose Cerbos uses repository-owned files:

- `cerbos/config/.cerbos.yaml` for Cerbos server configuration;
- `cerbos/init/cerbos-schema.sql` for local Cerbos PostgreSQL initialization;
- `cerbos/policies/` for derived roles, resource policies, and `_schemas/`.

For Coolify-managed external Cerbos, use [CERBOS_COOLIFY.md](CERBOS_COOLIFY.md) instead of copying Compose commands manually.

| Key | Purpose |
|---|---|
| `CERBOS_GRPC_ENDPOINT` | API PDP endpoint. Local profile default is `http://cerbos:3593`. |
| `CERBOS_HTTP_ENDPOINT` | API/Admin endpoint. Local profile default is `http://cerbos:3592`. |
| `CERBOS_USE_TLS` / `CERBOS_PLAINTEXT_MODE` | TLS posture for runtime API connections. Keep local defaults for Compose; use TLS in production. |
| `CERBOS_ADMIN_USERNAME` | Cerbos Admin API username. |
| `CERBOS_ADMIN_PASSWORD_HASH` | Base64 bcrypt hash consumed by the Cerbos server. Generate it with the process in [CERBOS_COOLIFY.md](CERBOS_COOLIFY.md#admin-password-hash). |
| `CERBOS_ADMIN_PASSWORD` | Plaintext Admin API password used only by `cerbosctl` policy sync/API package publishing. Keep blank unless sync is enabled. It must correspond to `CERBOS_ADMIN_PASSWORD_HASH`. |

### Storage

Local-first storage is available in the required API service without MinIO. Compose mounts the API path `/app/storage-data/local` to the durable named volume `local_storage_data`.

| Compose/API Key | Canonical .NET Key | Purpose |
|---|---|---|
| `LOCAL_STORAGE_ROOT_PATH` | `Storage:Local:RootPath` | Optional override for the API local storage root. The Compose default is `/app/storage-data/local`. |
| `LOCAL_STORAGE_CREATE_ROOT_IF_MISSING` | `Storage:Local:CreateRootIfMissing` | Optional override for allowing the API to create the root. The Compose default is `true`. |

Optional S3-compatible settings use the Infisical-compatible `STORAGE_S3_*` family. Compose maps those raw values into `S3Settings:*` for the API.

| Compose/API Key | Canonical .NET Key |
|---|---|
| `STORAGE_S3_ENDPOINT` | `S3Settings:Endpoint` |
| `STORAGE_S3_PUBLIC_ENDPOINT` | `S3Settings:PublicEndpoint` |
| `STORAGE_S3_REGION` | `S3Settings:Region` |
| `STORAGE_S3_BUCKET_NAME` | `S3Settings:BucketName` |
| `STORAGE_S3_ACCESS_KEY_ID` | `S3Settings:AccessKeyId` |
| `STORAGE_S3_SECRET_ACCESS_KEY` | `S3Settings:SecretAccessKey` |

Infisical/domain secret definitions use the `STORAGE_S3_*` family under storage paths. Keep optional S3 docs and secret-provider values aligned with those names; do not use stale generic S3 aliases such as `Storage__Endpoint`.

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
| `SETUP_SECRET` | API | Env-only optional fixed setup secret. If absent, API generates a temporary secret and prints it in API startup logs. |
| `SETUP_SECRET_REQUIRED` | API | Optional. Defaults to `true`; `false` only takes effect with trusted managed-provider provisioning keys and never makes setup endpoints anonymous. |
| `USE_COMMERCIAL_LUCKYPENNY`, `LUCKYPENNY_LICENSE_KEY`, `AUTOMAPPER_COMMERCIAL_VERSION`, `MEDIATR_COMMERCIAL_VERSION` | API | Optional commercial package/license controls. Keep disabled unless intentionally using commercial AutoMapper/MediatR builds. |
| `API_ENDPOINT` | Blazor | API base URL fallback for BFF proxying outside Aspire. |
| `CONTROL_PLANE_API_ENDPOINT` | Control Plane BFF | API base URL fallback for the optional separate control-plane BFF outside Aspire. Defaults to `http://islamu-event-api:8080/` in Compose. |

`docker-compose.yml` sets Blazor `API_ENDPOINT` with a default of `http://islamu-event-api:8080/`, matching the Compose API service name. Operators only need to override `API_ENDPOINT` when routing the BFF to a different API host.

`docker-compose.yml` sets the separate control-plane BFF `CONTROL_PLANE_API_ENDPOINT` and `ExploreApi__BaseUrl` with the same internal API default. Operators only need to override this when the control-plane BFF talks to a different API host.

### Multi-Tenant Hostnames And Reverse Proxy

Multi-tenant deployments use three host classes in front of the Blazor BFF:

| Host | Example | Proxy target | Notes |
|---|---|---|---|
| Public platform host | `events.example.org` | `islamu-event-ui:8080` | Configure as the public `PublicBaseUrl` / canonical URL. |
| Wildcard tenant host | `*.events.example.org` | `islamu-event-ui:8080` | Used for tenant subdomain resolution. Keep it on the same Blazor BFF entry point. |
| Dedicated admin host | `admin.example.org` | `islamu-event-ui:8080` | Configure the exact host or origin in `Bff:AdminHosts`; optional `Bff:AdminHostAllowedIpRanges` can restrict source networks. |

The reverse proxy must preserve the original host and scheme with trusted forwarded headers:

```text
X-Forwarded-Host: <browser-facing-host>
X-Forwarded-Proto: https
X-Forwarded-For: <client-ip>
```

Do not route `admin.example.org` through tenant wildcard DNS. The Blazor BFF classifies configured admin hosts after forwarded-header processing and skips tenant subdomain/custom-domain lookup for those hosts. If `Bff:AdminHostAllowedIpRanges` is configured, admin-host requests outside the allowed exact IP/CIDR ranges fail closed with `403`.

Tenant custom domains are operator/tenant-owned CNAMEs to the public edge target. Publish the CNAME target you expect tenants to use, but keep custom-domain validation separate from the dedicated admin host.

### Local SMTP Capture

The default Compose stack starts Mailpit without a profile so first-run registration and SMTP tests can capture messages locally instead of delivering real email.

| Key | Purpose |
|---|---|
| `MAILPIT_SMTP_PORT`, `MAILPIT_UI_PORT` | Host port bindings for Mailpit SMTP and the web UI. Defaults: `1025`, `8025`. |
| `MAILPIT_TAG` | Mailpit image tag. Defaults to `latest` for disposable local development. Pin a version for shared environments. |
| `MAILPIT_MAX_MESSAGES` | Maximum retained messages before pruning. Default: `5000`. |
| `MAIL_SMTP_HOST`, `MAIL_SMTP_PORT` | API SMTP bootstrap/source values. Local Compose defaults: `mailpit`, `1025`. |
| `MAIL_SMTP_ENCRYPTION` | SMTP security mode. Local Mailpit default: `None`. |
| `MAIL_SMTP_USERNAME`, `MAIL_SMTP_PASSWORD` | Optional credentials. Leave blank for local Mailpit. |
| `MAIL_SMTP_FROM_ADDRESS`, `MAIL_SMTP_FROM_NAME` | Default sender metadata for local SMTP testing. |

The `/smtp` Infisical folder and `.env` file use the same `MAIL_SMTP_*` names. The older `SMTP_*` environment aliases are still supplied by Compose for local compatibility, but new secret bindings should prefer `MAIL_SMTP_*`.

### AI Assistant

The AI assistant is optional. Self-hosted deployments can run with AI disabled, with the deterministic fake provider for smoke tests, with the first-class OpenAI Responses API provider, with the first-class Anthropic Messages API provider, with explicitly configured compatible endpoints, or with Azure OpenAI.

Compose accepts the Infisical-compatible `/ai` keys `AI_PROVIDER`, `AI_ENDPOINT`, `AI_MODEL_ID`, `AI_API_KEY`, and `AI_TOOL_PROPOSALS_ENABLED`; the API maps them to `AiProvider:*` during startup.

| Canonical .NET Key | Purpose |
|---|---|
| `AiProvider:Enabled` | Enables provider readiness evaluation. Disabled mode is health-safe and performs no provider call. |
| `AiProvider:Provider` | Supported values include `none`, `fake`, `openai`, `openai-compatible`, `anthropic`, `anthropic-compatible`, and `azure-openai`. Keep compatible providers for generic/self-hosted endpoints. |
| `AiProvider:EndpointUrl` | Provider base URL for OpenAI-compatible, Anthropic-compatible, or Azure OpenAI mode. The first-class `openai` and `anthropic` providers default to their official API base URLs when unset. Do not include credentials, query strings, or fragments. Azure OpenAI endpoints must use HTTPS. |
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
- MCP is optional but mapped by default. Keep startup `Mcp:Enabled=true` or unset for the API-hosted stateless Streamable HTTP MCP endpoint at `/mcp`; set `Mcp:Enabled=false` only when the deployment intentionally unmaps MCP. Instance runtime setting `mcp.enabled` can still disable the adapter without changing route shape. The adapter exposes safe registry discovery, first-class registry-projected proposal tools, conversation metadata resources, and proposal-first tool mutation; mutating MCP tools remain registry-backed and require the normal product/API confirmation path before side effects occur.
- For MCP, expose only the API endpoint configured by startup `Mcp:EndpointPath`, verify the `mcp-adapter` readiness check, and keep `Mcp:Stateless=true`. `Mcp:EnableLegacySse=true` is the default startup ceiling, but current runtime legacy SSE remains unavailable. MCP is API-key-first for external clients: configure a non-empty `X-API-Key`/`ISLAMU_EVENT_API_KEY` for scoped calls, grant `mcp:read` for generic read resources, grant `mcp:read` plus event read-equivalent scope authority for private event-management reads, grant `mcp:propose` for proposal tools/prompts, never send bearer and non-empty API-key credentials together, and expect no-key, blank-key, invalid-key, or revoked-key requests to see only anonymous-safe registry discovery. Instance administrators can lock tenant overrides with `governance.lock_tenant_mcp` and `governance.lock_tenant_mcp_legacy_sse`; tenant administrators can override only unlocked MCP runtime values. Self-hosters can disable MCP quickly by setting runtime `mcp.enabled=false`; set startup `Mcp:Enabled=false` and restart only when the endpoint must be unmapped.
- Do not run `stdio` or legacy SSE as product MCP transports from this stack. `stdio` remains deferred by [ADR-011](adr/ADR-011-local-mcp-stdio-diagnostic-host.md) unless a future local-only diagnostic host is separately approved, and legacy SSE remains unavailable even if `Mcp:EnableLegacySse=true` and `mcp.enable_legacy_sse=true` because stateful sessions and session affinity require a future ADR/test gate. Native AOT publication for the API host is also unverified until a dedicated publish profile proves the explicit SDK registrations and registry-projected tools survive trimming/AOT.
- Do not enable stateful sessions, sampling, elicitation, roots, completions, progress notifications, resource subscriptions, list-changed notifications, or client-specific compatibility shims for a self-hosted MCP client without a new ADR and release note. The default compatibility answer is to keep the current stateless proposal-first surface unchanged or explicitly disable MCP for that deployment.
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

The Compose file imports `./docker/keycloak/realm-export.json` into Keycloak, then runs the one-shot `keycloak-init` service. Aspire `local-full` imports the same checked-in realm export. The init job authenticates with the Compose Keycloak admin account, locates clients by `clientId`, and synchronizes the confidential public Blazor BFF client secret from `KEYCLOAK_BLAZOR_CLIENT_SECRET`. When the optional control-plane profile is used, it also synchronizes the dedicated `islamu-event-control-plane` confidential client from `KEYCLOAK_CONTROL_PLANE_CLIENT_SECRET`. Operators should set BFF client secrets once in environment variables or the configured secret provider; they should not manually edit the Keycloak UI to match the BFF secret. The API client is a bearer-only audience target in the checked-in realm export and has no static client secret by default.

Keycloak startup import is not a migration system. When a realm with the same name already exists in the persistent Keycloak database, startup import is skipped to avoid overwriting existing data. For disposable local stacks, remove the Keycloak database volume before expecting changes in `docker/keycloak/realm-export.json` to reapply.

For production, verify:

- realm name matches `KEYCLOAK_REALM`;
- Blazor client ID matches the configured client (`islamu-event-blazor` in Compose);
- control-plane client ID matches the configured client (`islamu-event-control-plane` in Compose) when the optional control-plane profile is enabled;
- redirect URIs and web origins match the public reverse-proxy host;
- API audience and metadata address match the Keycloak endpoint exposed to the API.

`KEYCLOAK_BLAZOR_CLIENT_SECRET` is fail-closed for Compose startup: `keycloak-init` exits non-zero when it is missing. `KEYCLOAK_CONTROL_PLANE_CLIENT_SECRET` is optional until the separate control-plane profile is enabled; the control-plane BFF itself fails closed at startup when neither environment nor Infisical supplies a client secret. To use the repository's static local defaults in disposable development, set `KEYCLOAK_INIT_ALLOW_DEFAULT_LOCAL_SECRET=true`. Do not enable that flag in production or shared environments. Rerun `docker compose run --rm keycloak-init` after rotating `KEYCLOAK_BLAZOR_CLIENT_SECRET` or `KEYCLOAK_CONTROL_PLANE_CLIENT_SECRET`. If a deployment intentionally sets `KEYCLOAK_API_CLIENT_SECRET` for a non-bearer-only API client, rerun the init job after rotating that value too.

### External Keycloak Bootstrap

When using an existing Keycloak deployment instead of the Compose-managed realm, first-run onboarding can configure the ISLAMU clients through the auth-provider setup page. Choose **Let ISLAMU configure Keycloak clients now** only when the operator has a temporary Keycloak admin or service account credential with enough permission to read the target realm, create the realm when using create mode, create or locate OIDC clients, and set client secrets.

The setup bootstrap path is protected by the setup secret before launch. The browser sends the one-time Keycloak bootstrap credential only to the Blazor BFF; the BFF strips any browser-controlled setup-secret header and forwards the trusted setup secret to the API. The API passes the credential to the Infrastructure Keycloak Admin API adapter for that request only. ISLAMU stores the resulting runtime OIDC configuration and Blazor client secret, but it does **not** store the Keycloak admin username/password, Keycloak access token, or raw Keycloak Admin API response body.

Recommended external-Keycloak operator flow:

1. Create a temporary Keycloak admin or service account scoped to the target realm-management operations.
2. In `/onboarding/auth-provider`, enable Keycloak and select **Let ISLAMU configure Keycloak clients now**.
3. Enter the Keycloak base URL, target realm, Blazor BFF client ID/secret, optional API client ID, an API client secret only when that client is intentionally confidential, and the temporary bootstrap credential.
4. Submit once. On success, the UI clears the one-time bootstrap credential fields and continues setup.
5. Disable or rotate the temporary Keycloak bootstrap credential after setup succeeds.

Use **Use an already configured Keycloak realm** when the operator has already created clients, redirect URIs, web origins, protocol mappers, and client secrets in Keycloak. In that mode ISLAMU only stores the runtime OIDC authority/client settings and does not call the Keycloak Admin API.

External bootstrap is idempotent for client lookup/update: rerunning setup against the same realm locates existing clients by `clientId` and updates their secrets. It does not delete existing realms, users, roles, or unrelated clients. Keep a Keycloak database backup before using create mode in shared environments.

### Post-Onboarding Keycloak Maintenance

After onboarding, instance administrators can use the admin auth-provider settings panel to diagnose and repair Keycloak drift without storing permanent Keycloak admin credentials.

Available operations:

- **Realm doctor** is read-only in basic mode and verifies saved runtime configuration plus OIDC discovery. With temporary admin credentials, it also checks realm/client availability, authorization-code settings, refresh-token settings, `offline_access` role/scope mappings, and the optional API audience target.
- **Sync preview** builds an additive `RealmSyncPlan`. Without temporary admin credentials it shows desired state only; with temporary credentials it compares the current realm and returns safe drift operations.
- **Sync apply** requires instance-admin authorization, temporary admin credentials, and explicit Keycloak backup confirmation. It only adds or updates ISLAMU-owned clients, scopes, protocol mappers, redirect URIs, web origins, and `offline_access` mappings. It does not delete realms, users, groups, unrelated clients, redirect origins, or operator-managed customizations.
- **Client-secret rotation** targets the configured Blazor confidential client. Application-managed rotation writes the new secret to Keycloak first and persists the ISLAMU runtime secret only after Keycloak accepts it. Deployment-managed secrets return operator instructions instead of being silently overwritten.

Temporary Keycloak admin usernames/passwords are used only for the active doctor, preview, apply, or rotation request. They must not be saved to appsettings, environment variables, Infisical, database settings, logs, screenshots, support bundles, or browser storage. Before applying a sync plan, back up the Keycloak PostgreSQL database and keep the backup until login and admin access have been verified after the repair.

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

## Support Access Operations

Admin support access is off by default and governed by instance settings under `support_access.*`. Keep `support_access.enabled=false` until operational approval, audit retention, and tenant trust communication are ready.

Operational controls:

- Use `support_access.enabled=false` as the global kill switch. It denies new sessions and runtime validation of forwarded support context.
- Keep `support_access.allow_write_mode=false` unless the deployment has an explicit break-glass approval process and alerting for write-capable sessions.
- Keep Redis or another distributed cache available for Blazor BFF instances so `IBffSupportAccessSessionStore` can bind the active session reference to the authenticated user and OIDC `sid`.
- Use the instance-admin support-access console to inspect session history, review audit evidence, and force-stop an active session during incident response.
- Tenant admins can review their tenant's support-access evidence from Tenant Administration -> Support Evidence. That tenant view is read-only and shows audit drill-in only when the API/HAL response grants the `audit-events` affordance.
- Backups must preserve `SupportAccessSession` and `SupportAccessAuditEvent` data with the application PostgreSQL database. Do not treat support-access audit evidence as disposable cache data.

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
- [CERBOS_COOLIFY.md](CERBOS_COOLIFY.md) — Coolify-specific Cerbos PDP deployment runbook.
- [SECURITY.md](SECURITY.md) — authentication and authorization architecture.
- [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md) — backup, restore, upgrade, rollback.
