ABOUTME: Production self-hosting guide covering Docker Compose stack, configuration, and operations.
ABOUTME: Covers minimum viable stack, optional services, setup, migrations, health checks, backups, and upgrades.

# Self-Hosting Guide

> **Audience:** Operators and DevOps engineers  
> **Status:** Implemented  
> **Owner:** Platform/Ops  
> **Last Verified:** 2026-08-01  
> **Source Anchors:** `docker-compose.yml`, `Explore.AppHost/AppHost.cs`, `Explore.API/Program.cs`, `Explore.Blazor/Extensions/YarpProxyExtensions.cs`

---

## Overview

ISLAMU Event is designed for easy self-hosting. At its core, the platform requires only **three services** to run a fully functional instance:

| Service | What It Does |
|---|---|
| **PostgreSQL** | Single application database — stores all data |
| **API** (`islamu-event-api`) | REST API, runs EF Core migrations on startup, health checks, metrics |
| **Blazor BFF** (`islamu-event-ui`) | Web UI, embedded admin shell, and YARP reverse proxy to the API |

> [!IMPORTANT]
> **Database migrations are built into the API container.** The API runs EF Core migrations automatically on startup (outside `Testing` environment). You do **not** need a separate migration service for standard deployments.

Everything else — Redis, Keycloak, Cerbos, MinIO, Svix, Coop, AI, federation — is **optional** and can be added when your deployment needs it.

---

## Table of Contents

1. [Quick Start (5 Minutes)](#quick-start-5-minutes)
2. [Architecture Overview](#architecture-overview)
3. [Service Reference](#service-reference)
4. [Deployment Tiers](#deployment-tiers)
5. [Environment Configuration](#environment-configuration)
6. [First-Run Setup](#first-run-setup)
7. [Identity Provider (Keycloak)](#identity-provider-keycloak)
8. [Optional Services](#optional-services)
9. [Reverse Proxy & TLS](#reverse-proxy--tls)
10. [Multi-Tenant Deployment](#multi-tenant-deployment)
11. [Email Configuration](#email-configuration)
12. [Health Checks & Monitoring](#health-checks--monitoring)
13. [Backup & Upgrade](#backup--upgrade)
14. [Troubleshooting](#troubleshooting)
15. [Related Documentation](#related-documentation)

---

## Quick Start (5 Minutes)

### Prerequisites

- Docker and Docker Compose v2
- A machine with at least 2 GB RAM

### Steps

**1. Clone and create your environment file:**

```bash
git clone <repository-url>
cd Event
cp .env.example .env
```

**2. Generate a Keycloak client secret (required):**

```bash
# Generate and paste into .env as KEYCLOAK_BLAZOR_CLIENT_SECRET
openssl rand -hex 32
```

**3. Set your setup secret in `.env`:**

```bash
# Add to .env — you'll use this to complete onboarding at /setup
SETUP_SECRET=my-secure-setup-secret
```

**4. Verify the resolved configuration:**

```bash
docker compose config
```

**5. Start the core stack:**

```bash
docker compose up -d
```

This starts all default services: PostgreSQL, Keycloak, Mailpit (local email capture), the API, and the Blazor UI.

**6. Open the application:**

| Service | URL |
|---|---|
| **Web UI** | `http://localhost:7002` |
| **API** | `http://localhost:7039` |
| **Mailpit** (email viewer) | `http://localhost:8025` |
| **Keycloak Admin** | `http://localhost:8080` |

**7. Complete first-run setup** at `http://localhost:7002/setup` using your `SETUP_SECRET`.

> [!TIP]
> For the absolute minimal deployment (no Keycloak, no Redis, no Mailpit), see [Deployment Tiers → Tier 1: Bare Minimum](#tier-1-bare-minimum).

---

## Architecture Overview

```
                    ┌──────────────────────────────────────────────────┐
                    │              Browser / Clients                   │
                    └──────────────────┬───────────────────────────────┘
                                       │
                              ┌────────▼─────────┐
                              │   Reverse Proxy   │  ← TLS termination
                              │  (Nginx/Caddy/…)  │     (production)
                              └────────┬──────────┘
                                       │
                    ┌──────────────────▼───────────────────────────┐
                    │          Blazor BFF (islamu-event-ui)         │
                    │  • Server-side Blazor rendering               │
                    │  • OIDC cookie auth with identity provider    │
                    │  • YARP reverse proxy → API                   │
                    │  • Admin shell for control-plane hosts        │
                    └──────────────────┬───────────────────────────┘
                                       │ (internal HTTP)
                    ┌──────────────────▼───────────────────────────┐
                    │            API (islamu-event-api)             │
                    │  • REST API (MediatR/CQRS)                   │
                    │  • EF Core migrations on startup             │
                    │  • Background workers & outbox processing    │
                    │  • Health checks, metrics, MCP adapter       │
                    └──────┬──────────────────────┬────────────────┘
                           │                      │
                  ┌────────▼────────┐    ┌────────▼────────────────┐
                  │   PostgreSQL    │    │   Optional Services     │
                  │  (single DB)    │    │  Redis, Keycloak,       │
                  │                 │    │  Cerbos, MinIO, Svix…   │
                  └─────────────────┘    └─────────────────────────┘
```

**Key design principles:**

- **Browsers talk only to the Blazor BFF.** The BFF proxies API calls via YARP; clients should not need direct API access.
- **The API is the single composition root** for Domain, Application, Persistence, and Infrastructure layers.
- **PostgreSQL is the only required datastore.** All application data, outbox state, privacy-erasure authority (colocated mode), and data-protection keys live in one database.

---

## Service Reference

### Required Services

These three services are the minimum to run ISLAMU Event:

| Service | Compose Name | Purpose | Default Port |
|---|---|---|---|
| PostgreSQL | `postgres` | Application database | internal `5432` |
| API | `islamu-event-api` | REST API, migrations, workers, health, metrics | `7039:8080` |
| Blazor BFF | `islamu-event-ui` | Web UI, admin shell, YARP proxy to API | `7002:8080` |

### Default Services (started without profiles)

These are included in the default `docker compose up` and are recommended for a functional deployment but not strictly required for the application to boot:

| Service | Compose Name | Purpose | Default Port | Notes |
|---|---|---|---|---|
| Redis | `redis` | Distributed cache (HybridCache L2), sessions | internal | Optional for single-node; recommended for multi-replica |
| Keycloak DB | `keycloak-db` | Identity provider database | internal | Only needed with local Keycloak |
| Keycloak | `keycloak` | OIDC identity provider, realm import | `8080:8080` | Can use an external Keycloak instead |
| Keycloak Init | `keycloak-init` | One-shot client-secret sync after realm import | — | Runs once at startup |
| Mailpit | `mailpit` | Local SMTP capture for email testing | `1025`, `8025` | Replace with real SMTP in production |

### Optional Services (via Compose profiles)

Enable these only when needed. Each has its own Compose profile:

| Profile | Services Started | Purpose |
|---|---|---|
| `storage` | `minio`, `minio-init` | S3-compatible object storage (default is local filesystem) |
| `authz` | `cerbos-db`, `cerbos`, `cerbos-policy-sync` | External authorization PDP for fine-grained policies |
| `webhooks` | `svix-db`, `svix` | Outgoing webhook provider |
| `moderation` | `coop-db`, `coop-migrations`, `coop`, `coop-client` | Content moderation review queue |
| `osprey` | `osprey` | Roost Osprey coordinator for signal evaluation |
| `privacy-erasure-external` | `privacy-erasure-db`, `event-migrationservice` | External DB topology for privacy-erasure authority |

```bash
# Example: add S3 storage
docker compose --profile storage up -d

# Example: add Cerbos authorization
AUTHORIZATION_PROVIDER=cerbos docker compose --profile authz up -d

# Example: add webhooks
docker compose --profile webhooks up -d
```

---

## Deployment Tiers

Choose the tier that matches your needs. The same application runs at every tier — only infrastructure and configuration change.

### Tier 1: Bare Minimum

**Target:** Small community, single operator, minimal maintenance.

| Component | Required? |
|---|---|
| PostgreSQL | ✅ Yes |
| API | ✅ Yes |
| Blazor BFF | ✅ Yes |
| Keycloak | Recommended (or use external OIDC provider) |
| Redis | Optional (not needed for single-node) |
| SMTP / Mailpit | Optional (needed only for email features) |
| Everything else | Not needed |

```bash
# Minimal start — just the core + Keycloak for auth
docker compose up -d postgres keycloak-db keycloak keycloak-init islamu-event-api islamu-event-ui
```

- **Deployment mode:** Single-tenant (default, `DEPLOYMENT_MODE` unset).
- **Authorization:** Local RBAC (`AUTHORIZATION_PROVIDER` unset or `local`).
- **Storage:** Local filesystem (no MinIO needed).
- **Email:** Disabled or use Mailpit for testing.

### Tier 2: Community Platform

**Target:** Production multi-tenant platform with tenant boundaries and policy controls.

- Everything from Tier 1, **plus:**
- `DEPLOYMENT_MODE=multi_tenant` set before first launch
- Redis for distributed cache and sessions across replicas
- Cerbos PDP for fine-grained authorization (`--profile authz`)
- Real SMTP provider (replace Mailpit)
- Reverse proxy with TLS

### Tier 3: Ummah-Scale

**Target:** High availability, strict isolation, compliance.

- Everything from Tier 2, **plus:**
- Cerbos HA cluster behind a dedicated load balancer
- Separated database clusters for application data, identity (Keycloak), and policy storage
- Centralized observability stack (Prometheus, Loki)
- Multi-replica API and Blazor with shared Redis

> [!NOTE]
> No feature rewrite is required to move between tiers. Scale by adding infrastructure and updating configuration.

---

## Environment Configuration

### Creating Your `.env` File

```bash
cp .env.example .env
```

The `.env` file is used by Docker Compose for variable interpolation. It is `.gitignore`d. The `.env.example` file contains safe local defaults.

> [!WARNING]
> Replace all default credentials before deploying to any shared, staged, or production environment.

### Required Variables

These must be set for the application to start:

| Variable | Default | Purpose |
|---|---|---|
| `POSTGRESQL_HOST` | `postgres` | Database host |
| `POSTGRESQL_PORT` | `5432` | Database port |
| `POSTGRESQL_DATABASE` | `islamu_event_db` | Database name |
| `POSTGRESQL_USERNAME` | `explore` | Database username |
| `POSTGRESQL_PASSWORD` | `explore` | Database password |
| `KEYCLOAK_BLAZOR_CLIENT_SECRET` | *(none — must generate)* | OIDC confidential client secret |
| `KEYCLOAK_ENDPOINT` | `http://keycloak.localhost:8080` | Keycloak base URL |
| `KEYCLOAK_REALM` | `ISLAMU` | Keycloak realm name |

> [!IMPORTANT]
> `KEYCLOAK_BLAZOR_CLIENT_SECRET` has no default. Generate it with `openssl rand -hex 32` and add it to your `.env` before starting.

The API reads PostgreSQL credentials as discrete variables via `BootstrapSecretLoader`. Do **not** pre-construct a single `ConnectionStrings:DefaultConnection` URL-form string.

### Recommended Variables

| Variable | Default | Purpose |
|---|---|---|
| `SETUP_SECRET` | *(internal random fallback)* | Operator secret for first-run onboarding at `/setup` |
| `DEPLOYMENT_MODE` | *(blank = single-tenant)* | Set `multi_tenant` before first launch for multi-tenant |
| `CONTROL_PLANE_PUBLIC_ORIGIN` | — | Control-plane admin host for BFF admin-host registration |

### Optional Variables

| Variable | Default | Purpose |
|---|---|---|
| `AUTHORIZATION_PROVIDER` | *(blank = Local-first)* | `local` for Local RBAC, `cerbos` for Cerbos PDP |
| `AI_PROVIDER` | `none` | AI assistant provider: `none`, `fake`, `openai`, `anthropic`, `azure-openai`, or `-compatible` variants |
| `MCP_ENABLED` | `true` | Enable/disable the MCP adapter at `/mcp` |
| `WEBHOOKS_PROVIDER` | `Local` | `Local`, `Svix`, or `Composite` |
| `REPORTING_MODE` | `LocalOnly` | Reporting mode |
| `SECRET_PROVIDER` | `None` | Set `Infisical` to enable Infisical secret loading |

For the complete variable reference, see [CONFIGURATION.md](CONFIGURATION.md).

---

## First-Run Setup

After starting the stack for the first time:

### 1. Set Your Setup Secret

Configure `SETUP_SECRET` in your `.env` before starting the API. If absent, the API uses an internal random fallback that is never printed — you must set `SETUP_SECRET` and restart to use the `/setup` flow.

### 2. Open the Setup Wizard

Navigate to `http://localhost:7002/setup`.

### 3. Complete Onboarding

The setup wizard guides you through:

1. **Submit the setup secret** — entered through the BFF-mediated gateway (the browser never sends it directly to the API).
2. **Configure authentication** — verify Keycloak realm/clients or set up an external provider.
3. **Resolve preflight checks** — warnings don't block launch; serious issues require acknowledgement.
4. **Launch the instance** — this is a one-time action that locks the setup gate.

> [!NOTE]
> **Setup session expires after 30 minutes** of inactivity. Successful setup actions extend the session. If it expires, re-enter your `SETUP_SECRET`; restarting the API is unnecessary unless the value itself changed.

### Rate Limiting

The setup-secret validation endpoint (`POST /api/InstanceOnboarding/validate-secret`) has rate limiting. Repeated failures should be treated as credential errors, not retried blindly.

### Managed Provider Mode

For automated/managed deployments, `SETUP_SECRET_REQUIRED=false` is allowed **only** when all of these are set:

- `PROVISIONING_TRUSTED=true`
- `PROVISIONING_MODE` = `managed-provider`, `managed_provider`, `managed-hosting`, or `managed`
- `MANAGED_CLIENT_EXTERNAL_PROVIDER` (stable partner ID)
- `PHYSICAL_TENANCY_MODE` (deployment posture marker)

Setup endpoints still require setup-token authority — they never become anonymous.

---

## Identity Provider (Keycloak)

### Local Keycloak (Default Compose Stack)

The default stack includes a local Keycloak instance with automatic realm setup:

1. **`keycloak`** — starts with the checked-in `docker/keycloak/realm-export.json`.
2. **`keycloak-init`** — one-shot job that syncs `KEYCLOAK_BLAZOR_CLIENT_SECRET` into the `islamu-event-blazor` client.

The realm export contains only `islamu-event-blazor` (BFF client) and `islamu-event-api` (bearer-only audience). No confidential client secrets are checked in.

> [!WARNING]
> Keycloak startup import is **not** a migration system. If a realm with the same name already exists, the import is skipped. For disposable local stacks, remove the Keycloak database volume to reapply changes from the realm export.

**After rotating secrets or changing callback overrides:**

```bash
docker compose run --rm keycloak-init
```

**Keycloak environment variables:**

| Variable | Purpose |
|---|---|
| `KEYCLOAK_DB_DATABASE`, `KEYCLOAK_DB_USERNAME`, `KEYCLOAK_DB_PASSWORD` | Local Keycloak database bootstrap |
| `KEYCLOAK_ADMIN`, `KEYCLOAK_ADMIN_PASSWORD` | Initial admin account |
| `KEYCLOAK_ENDPOINT` | Base Keycloak URL for API/Blazor authority |
| `KEYCLOAK_REALM` | Realm name (default: `ISLAMU`) |
| `KEYCLOAK_BLAZOR_CLIENT_ID` | BFF OIDC client ID (default: `islamu-event-blazor`) |
| `KEYCLOAK_BLAZOR_CLIENT_SECRET` | **Required** — BFF confidential client secret |
| `KEYCLOAK_BLAZOR_REDIRECT_URIS` | Optional JSON array of exact BFF login callbacks |
| `KEYCLOAK_BLAZOR_WEB_ORIGINS` | Optional JSON array of exact BFF origins |
| `KEYCLOAK_BLAZOR_LOGOUT_REDIRECT_URIS` | Optional `##`-separated list of logout callbacks |

### Production Keycloak Checklist

Before going to production, verify:

- Realm name matches `KEYCLOAK_REALM`
- Blazor client ID matches configured client
- Login callbacks are exact `<public-origin>/signin-oidc` URIs
- Logout callbacks are exact `<public-origin>/signout-callback-oidc` URIs
- Web origins are exact public origins (no `+` or wildcard entries)
- API audience and metadata address match the Keycloak endpoint exposed to API
- Keycloak SMTP is configured before exposing self-registration
- Password policy enforces 12+ characters with username/email/history checks

### Using an External Keycloak

Instead of the Compose-managed Keycloak, you can use an existing deployment:

1. Create a temporary admin or service account scoped to the target realm.
2. During setup, choose **"Let ISLAMU configure Keycloak clients now"**.
3. Enter the Keycloak URL, realm, client IDs/secrets, and temporary credentials.
4. Submit once. On success, the UI clears the bootstrap credentials.
5. Disable or rotate the temporary credentials after setup.

Alternatively, choose **"Use an already configured Keycloak realm"** if you've already created clients, redirect URIs, and secrets in Keycloak manually.

### Post-Onboarding Maintenance

After onboarding, instance administrators can use the admin auth-provider settings panel:

- **Realm Doctor** — read-only verification of OIDC discovery and realm health
- **Sync Preview/Apply** — additive-only drift repair for ISLAMU-owned clients
- **Client Secret Rotation** — rotates the Blazor confidential client secret

> [!CAUTION]
> Temporary Keycloak admin credentials are used only for the active operation. They must never be saved to environment files, logs, screenshots, or browser storage.

---

## Optional Services

### Storage (MinIO / S3)

**Default behavior:** Local filesystem storage at `/app/storage-data/local` (mounted as Docker volume `local_storage_data`). No MinIO or S3 required.

**To enable S3-compatible storage:**

```bash
docker compose --profile storage up -d
```

| Variable | Purpose |
|---|---|
| `STORAGE_S3_ENDPOINT` | S3-compatible endpoint URL |
| `STORAGE_S3_PUBLIC_ENDPOINT` | Public-facing endpoint for file URLs |
| `STORAGE_S3_REGION` | S3 region |
| `STORAGE_S3_BUCKET_NAME` | Bucket name |
| `STORAGE_S3_ACCESS_KEY_ID` | Access key |
| `STORAGE_S3_SECRET_ACCESS_KEY` | Secret key |

**Storage reconciliation** runs in dry-run mode by default. Enable destructive cleanup only after reviewing dry-run output and confirming backups:

| Variable | Default | Purpose |
|---|---|---|
| `STORAGE_RECONCILIATION_ENABLED` | `true` | Enable reconciliation worker |
| `STORAGE_RECONCILIATION_DRY_RUN` | `true` | Safe mode — logs only, no changes |
| `STORAGE_RECONCILIATION_QUARANTINE_MISSING_OBJECTS` | `false` | Quarantine DB records with missing files |
| `STORAGE_RECONCILIATION_QUARANTINE_ORPHAN_LOCAL_FILES` | `false` | Quarantine files without DB records |
| `STORAGE_RECONCILIATION_DELETE_QUARANTINED_OBJECTS` | `false` | Delete quarantined items |

### Authorization (Cerbos)

**Default behavior:** Local RBAC — no external authorization service needed.

**To enable Cerbos PDP:**

```bash
AUTHORIZATION_PROVIDER=cerbos docker compose --profile authz up -d
```

| Variable | Purpose |
|---|---|
| `AUTHORIZATION_PROVIDER` | `local` or `cerbos` |
| `CERBOS_GRPC_ENDPOINT` | PDP gRPC endpoint (default: `http://cerbos:3593`) |
| `CERBOS_HTTP_ENDPOINT` | PDP HTTP endpoint (default: `http://cerbos:3592`) |
| `CERBOS_ADMIN_USERNAME` | Cerbos Admin API username |
| `CERBOS_ADMIN_PASSWORD_HASH` | Base64 bcrypt hash for Cerbos server |
| `CERBOS_ADMIN_PASSWORD` | Plaintext password for `cerbosctl` policy sync |

> [!WARNING]
> When `AUTHORIZATION_PROVIDER=cerbos`, Cerbos PDP outages **fail closed** (deny all requests). The system does **not** automatically fall back to local RBAC. Set `AUTHORIZATION_PROVIDER=local` explicitly if you need to bypass Cerbos.

For Coolify-managed Cerbos, see [CERBOS_COOLIFY.md](CERBOS_COOLIFY.md).

### AI Assistant

**Default behavior:** Disabled. The application runs without AI features.

| Variable | Purpose |
|---|---|
| `AiProvider:Enabled` | Enable AI provider readiness evaluation |
| `AiProvider:Provider` | `none`, `fake`, `openai`, `openai-compatible`, `anthropic`, `anthropic-compatible`, `azure-openai` |
| `AiProvider:EndpointUrl` | Provider base URL (official providers have built-in defaults) |
| `AiProvider:ApiKey` | Provider API key (never expose in logs or health data) |
| `AiProvider:ModelId` | Default model identifier |

Compose accepts Infisical-compatible keys: `AI_PROVIDER`, `AI_ENDPOINT`, `AI_MODEL_ID`, `AI_API_KEY`.

### MCP Adapter

The API-hosted Streamable HTTP MCP adapter is **mapped by default** at `/mcp`. Disable with `Mcp:Enabled=false` or the runtime setting `mcp.enabled=false`.

MCP uses API-key authentication for external clients. Configure `ISLAMU_EVENT_API_KEY` for scoped calls with `mcp:read` and/or `mcp:propose` grants.

For MCP debugging, see [MCP_DEBUGGING.md](MCP_DEBUGGING.md).

### Webhooks (Svix)

```bash
docker compose --profile webhooks up -d
```

Set `WEBHOOKS_PROVIDER=Svix` to route outgoing webhooks through Svix instead of the built-in local provider.

### Moderation (Coop)

```bash
docker compose --profile moderation up -d
```

Keep `REPORTING_MODE=LocalOnly` unless Coop provider endpoints are intentionally enabled.

### AT Protocol Federation

Federation is **disabled by default** (`federation.atproto_events_enabled=false`). Enabling it activates Jetstream discovery and outbound event/RSVP publication. This is an advanced feature requiring:

- AT Protocol OAuth keys (`ATPROTO_OAUTH_CLIENT_PRIVATE_JWKS`, `ATPROTO_SESSION_ENCRYPTION_KEYRING`, `ATPROTO_SESSION_JWT_PRIVATE_JWKS`)
- Public HTTPS origin (`Atproto:PublicUrl`)
- PostgreSQL schema migrations for ATProto records

For full details, see [FEDERATION.md](FEDERATION.md) and [CONFIGURATION.md](CONFIGURATION.md#at-protocol-events-governance-and-workers).

### Privacy Erasure External Topology

**Default behavior:** Colocated mode — the privacy-erasure authority ledger runs in the main PostgreSQL database with zero extra infrastructure.

For external database topology:

```bash
PRIVACY_ERASURE_AUTHORITY_TOPOLOGY=ExternalDatabase \
docker compose --profile privacy-erasure-external up -d
```

This starts a separate authority PostgreSQL and the `event-migrationservice`. Supply separate runtime and migrator connection strings. See [PRIVACY_ERASURE.md](PRIVACY_ERASURE.md) for guidance.

---

## Reverse Proxy & TLS

For production, place a TLS-terminating reverse proxy (Nginx, Caddy, Traefik, etc.) in front of the Blazor BFF.

### Minimum Requirements

| Requirement | Details |
|---|---|
| Route target | `islamu-event-ui:8080` |
| Preserve `Host` header | Required for tenant resolution |
| Forward headers | `X-Forwarded-For`, `X-Forwarded-Proto`, `X-Forwarded-Host` |
| Keycloak redirect URIs | Must match the public origin seen by browsers |

```text
# Required forwarded headers
X-Forwarded-Host: <browser-facing-host>
X-Forwarded-Proto: https
X-Forwarded-For: <client-ip>
```

> [!IMPORTANT]
> Browsers should talk **only** to the Blazor BFF. The BFF proxies API calls internally. Direct browser-to-API access is not required.

---

## Multi-Tenant Deployment

Set `DEPLOYMENT_MODE=multi_tenant` **before first-run onboarding**. This cannot be changed from the admin UI after setup.

### DNS and Host Configuration

Multi-tenant deployments use three host classes:

| Host Type | Example | Configuration |
|---|---|---|
| Public platform host | `events.example.org` | Set as `PublicBaseUrl` |
| Wildcard tenant host | `*.events.example.org` | Used for subdomain-based tenant resolution |
| Dedicated admin host | `admin.example.org` | Set in `Bff:AdminHosts` |

All three route to the same Blazor BFF entry point (`islamu-event-ui:8080`).

> [!WARNING]
> Do **not** route the admin host through tenant wildcard DNS. The BFF classifies admin hosts separately and skips tenant subdomain/custom-domain lookup for those hosts.

Optional: Use `Bff:AdminHostAllowedIpRanges` to restrict admin-host access to specific IP/CIDR ranges. Requests outside the allowed ranges get `403`.

### Tenant Custom Domains

Tenant custom domains are operator/tenant-owned CNAMEs pointing to the public edge target. Keep custom-domain validation separate from the dedicated admin host.

---

## Email Configuration

### Local Development (Mailpit)

The default stack includes Mailpit for local email capture:

| Variable | Default | Purpose |
|---|---|---|
| `MAIL_SMTP_HOST` | `mailpit` | SMTP host |
| `MAIL_SMTP_PORT` | `1025` | SMTP port |
| `MAIL_SMTP_ENCRYPTION` | `None` | SMTP security mode |
| `MAIL_SMTP_FROM_ADDRESS` | `noreply@localhost` | Sender address |

View captured emails at `http://localhost:8025`.

### Production SMTP

Replace the Mailpit values with your SMTP provider credentials:

| Variable | Purpose |
|---|---|
| `MAIL_SMTP_HOST` | SMTP server hostname |
| `MAIL_SMTP_PORT` | SMTP port (typically `587` for STARTTLS) |
| `MAIL_SMTP_ENCRYPTION` | `None`, `StartTls`, or `SslOnConnect` |
| `MAIL_SMTP_USERNAME` | SMTP username |
| `MAIL_SMTP_PASSWORD` | SMTP password |
| `MAIL_SMTP_FROM_ADDRESS` | Sender email address |
| `MAIL_SMTP_FROM_NAME` | Sender display name |

### Email Dispatch Configuration

The API uses a PostgreSQL-based outbox pattern for email dispatch. Configure these for capacity:

| Variable | Default | Purpose |
|---|---|---|
| `EmailDispatchProcessor:MaxConcurrentDispatches` | `8` | Global concurrent dispatch limit |
| `EmailDispatchProcessor:MaxConcurrentDispatchesPerTenant` | `2` | Per-tenant concurrent limit |
| `EmailDispatchProcessor:GlobalSmtpRateLimitPerMinute` | `120` | Global SMTP rate limit |
| `EmailDispatchProcessor:TenantSmtpRateLimitPerMinute` | `30` | Per-tenant SMTP rate limit |

> [!TIP]
> Before testing external SMTP, use Mailpit first: send one product dispatch, confirm the outbox settles, then inspect the captured message at `http://localhost:8025`.

**RabbitMQ dispatch** is optional transport infrastructure. The default PostgreSQL-only dispatch mode requires no message broker. Enable RabbitMQ only if you have an operator-provided broker and set `EmailDispatchRabbitMq:Enabled=true`.

---

## Health Checks & Monitoring

| Endpoint | Host | Purpose |
|---|---|---|
| `/alive` | API, Blazor | Liveness probe — is the process running? |
| `/health` | API, Blazor | Readiness probe — are dependencies healthy? |
| `/metrics` | API | Prometheus metrics |

**Interpreting health status:**

| Status | Meaning | Action |
|---|---|---|
| `Healthy` | All dependencies ready | Good to serve traffic |
| `Degraded` | Optional dependency intentionally disabled | Acceptable when expected |
| `Unhealthy` | Critical dependency unavailable | **Do not serve traffic** |

**Notable health checks:**

| Check | Notes |
|---|---|
| `ai-provider` | Disabled AI is healthy (dormant state) |
| `ai-retention-cleanup` | Disabled cleanup is intentionally degraded |
| `email-dispatch` | Degraded if dispatch is paused or disabled |
| `storage` | Verifies selected provider can write to data root |
| `storage-reconciliation` | Healthy in dry-run mode; degraded if disabled |
| `atproto-authentication` | Disabled AT Protocol login is healthy dormant state |
| `idempotency-cleanup` | Healthy in delete or dry-run mode |

### Support Access

Admin support access is off by default (`support_access.enabled=false`). Enable it only after operational approval, audit retention, and tenant trust communication are ready. Keep `support_access.allow_write_mode=false` unless you have an explicit break-glass approval process.

---

## Backup & Upgrade

### Pre-Upgrade Checklist

Before every upgrade:

1. ✅ Back up application PostgreSQL data
2. ✅ Back up Keycloak PostgreSQL data (if using local Keycloak)
3. ✅ Back up object storage — `local_storage_data` volume, `minio_data`, or S3 bucket
4. ✅ Record image tags, commit SHA, enabled Compose profiles, and secret-provider key names
5. ✅ Read release notes for migrations, config changes, and rollback constraints

### Migrations

| Path | When Used | Behavior |
|---|---|---|
| API startup | Docker Compose / direct hosting | Runs EF Core migrations and seeding automatically on startup |
| `Event.MigrationService` | Aspire / local dev orchestration | Applies migrations and seeds, then exits before API/Blazor start |

> [!NOTE]
> The API handles migrations automatically. You do **not** need `Event.MigrationService` for standard Docker Compose deployments. It is only needed for the `privacy-erasure-external` Compose profile.

### Creating Migrations from Scratch

If bootstrapping from a clean repository:

```bash
# 1. Data-protection migration first
dotnet ef migrations add init \
  --context DataProtectionKeyContext \
  --project Explore.Persistence \
  --startup-project Explore.API \
  --output-dir Migrations/DataProtection

# 2. Primary application schema
dotnet ef migrations add init \
  --context ExploreDbContext \
  --project Explore.Persistence \
  --startup-project Explore.API
```

For the full backup and restore runbook, see [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md).  
For the release checklist, see [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md).

---

## Troubleshooting

### Common Issues

| Problem | Cause | Fix |
|---|---|---|
| `unauthorized_client` from Keycloak | `KEYCLOAK_BLAZOR_CLIENT_SECRET` mismatch | Rerun `docker compose run --rm keycloak-init` |
| OIDC redirect URI errors behind proxy | Missing `X-Forwarded-Proto`/`X-Forwarded-Host` | Configure reverse proxy to forward these headers |
| Setup secret not working | Empty `SETUP_SECRET` or session expired | Set `SETUP_SECRET` in `.env` and restart API; re-enter if session expired |
| Cerbos denying all requests | PDP is down or unreachable | Cerbos fails closed. Check `CERBOS_GRPC_ENDPOINT` or switch to `AUTHORIZATION_PROVIDER=local` |
| Realm export changes not applied | Keycloak skips import when realm exists | Remove the `keycloak_data` volume and restart |
| `429` responses | Rate limiting or OpenGraph image saturation | Review rate-limit configuration and dispatch capacity |
| Storage health unhealthy | Selected provider can't write to data root | Check volume mounts and file permissions |

### Diagnostic Tool

Run the built-in diagnostic CLI:

```bash
dotnet run --project Explore.Diagnostic/Explore.Diagnostic.csproj -- --root .
```

### Triage Order

1. Check health endpoints: `GET /health`, `GET /alive`
2. Review startup logs for migration or seeding errors
3. Verify deployment mode: is the correct `DEPLOYMENT_MODE` set?
4. Check BFF auth status: `GET /auth/status`
5. Review Keycloak client configuration and secrets

For comprehensive troubleshooting, see [TROUBLESHOOTING.md](TROUBLESHOOTING.md).

---

## Planned Features

### PostGIS Proximity Discovery

Exact proximity discovery is **planned, not implemented**. The current database uses plain `postgres:18-alpine` — no PostGIS extension or spatial endpoints ship today. No operator action is needed.

[ADR-013](adr/ADR-013-postgis-proximity-discovery.md) defines the future package. Failure or rollback returns the product to `area_only` mode — it never starts a browser-side or in-memory distance fallback.

---

## Related Documentation

| Document | Purpose |
|---|---|
| [CONFIGURATION.md](CONFIGURATION.md) | Full runtime configuration reference and key mappings |
| [SECRETS.md](SECRETS.md) | Secret provider behavior and key mapping |
| [OPERATIONS.md](OPERATIONS.md) | Health, startup, shutdown, and runtime safeguards |
| [DEPLOYMENT_MODES.md](DEPLOYMENT_MODES.md) | Single-tenant vs multi-tenant mode details |
| [DEPLOYMENT_TIERS.md](DEPLOYMENT_TIERS.md) | Infrastructure maturity tiers |
| [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md) | Full backup, restore, and upgrade runbook |
| [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md) | Pre-release validation checklist |
| [CERBOS_COOLIFY.md](CERBOS_COOLIFY.md) | Coolify-specific Cerbos deployment |
| [FEDERATION.md](FEDERATION.md) | AT Protocol federation configuration |
| [PRIVACY_ERASURE.md](PRIVACY_ERASURE.md) | Privacy-erasure authority topologies |
| [TROUBLESHOOTING.md](TROUBLESHOOTING.md) | Comprehensive troubleshooting guide |
| [MCP_DEBUGGING.md](MCP_DEBUGGING.md) | MCP adapter debugging |
| [SECURITY-MODEL.md](SECURITY-MODEL.md) | Authentication and authorization architecture |
