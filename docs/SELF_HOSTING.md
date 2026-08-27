ABOUTME: Production self-hosting guide covering split Compose and standalone image operations.
ABOUTME: Covers minimum viable stack, optional services, setup, migrations, health checks, backups, and upgrades.

# Self-Hosting Guide

> **Audience:** Operators and DevOps engineers  
> **Status:** Implemented  
> **Owner:** Platform/Ops  
> **Last Verified:** 2026-08-15
> **Source Anchors:** `docker-compose.yml`, `src/Event.Standalone/Dockerfile`, `src/Event.MigrationService/Dockerfile`, `src/Event.Standalone/Program.cs`, `src/Explore.API/Hosting/ApiHostStartupExtensions.cs`, `src/Explore.Application/Configuration/PromotionCodeLookupOptions.cs`, `src/Explore.Domain/Secrets/SecretDefinitionRegistry.cs`, `src/Explore.Infrastructure/Services/Registration/PromotionCodeDigestService.cs`

---

## Paid Checkout Ownership Gate

A self-host is an independent operator unless project governance has provisioned official status in server-owned instance configuration. Tenant administrators and browser requests cannot assert official status. Paid Checkout starts stopped: configure a deployment-specific operator identity, HTTPS origin and legal links, accountable complaint/refund/dispute/reconciliation owners, refund language, statement descriptor, charge type, and this deployment's own Stripe platform credentials under `Payments:CheckoutGovernance` and `Payments:OrganizerDirect`. Set startup-owned `ActivationStatus` to `approved` only after that evidence is complete, then use the authorized paid-checkout governance HAL resource to request and independently approve each tenant's initial durable sale control. Never reuse ISLAMU provider credentials or imply ISLAMU protection for an independent deployment.

## Overview

ISLAMU Event is designed for easy self-hosting. Its minimum operational stack
is the standalone distribution:

| Component | What It Does |
|---|---|
| **`Event.Standalone`** | Runs the ISLAMU Event API, background workers, Blazor BFF/UI, health endpoints, and startup lifecycle in one process and one container |
| **SQLite persistence** | Stores the primary application state and the separately governed embedded privacy-erasure authority in durable SQLite files; migrations run in-process before the HTTP listener starts |

No external database server, migration helper container, identity provider,
cache, object store, authorization PDP, SMTP server, webhook service, or other
sidecar is required for this minimum topology. The split deployment is a
separate packaging choice that runs `Event.MigrationService`, `Explore.API`,
`Explore.Blazor`, and a selected database as distinct processes.

### Why Self-Host? (Data Sovereignty & Strategic Value)

Commercial event SaaS platforms lock your community's data inside third-party databases, impose mandatory per-ticket fees, enforce vendor-controlled privacy policies, and restrict customization.

Self-hosting ISLAMU Event delivers a superior alternative:
* **Zero Platform Tax:** Eliminate recurring per-ticket commission fees or monthly registration volume penalties.
* **Complete Data Sovereignty:** You own your PostgreSQL/database infrastructure, attendee identities, and contact lists entirely.
* **Flexible Compliance & Privacy:** Built-in GDPR Privacy Erasure Authority supports three topologies: EmbeddedSqlite, CoLocated, and ExternalDatabase.
* **Full White-Label Independence:** Customize tenant domains, logos, navigation, and governance without third-party vendor watermarks.

> [!IMPORTANT]
> **Split deployments run `Event.MigrationService` before the API.** It applies
> application, Data Protection, and exactly one authority migration path selected
> by the configured topology. The standalone image is the documented in-process
> exception and applies the same responsibilities before its HTTP listener starts.
> The API separately owns the Quartz scheduler schema, which is idempotent DDL
> rather than an EF Core migration and is supported on every primary provider,
> including SQLite.

Everything outside `Event.Standalone` and its SQLite persistence — including
server databases, Redis or Valkey, Keycloak, Cerbos, MinIO/S3, SMTP or Mailpit,
Svix, Weblate, Formbricks, Coop, Osprey, AI providers, federation services, and
external observability backends — is **optional** and can be added only when a
deployment needs it.

### Third-party software and license boundary

The repository's AGPL-3.0-or-later license, and any alternative license offered
by ISLAMU, cover only ISLAMU-owned or properly sublicensable material. SQLite,
NuGet dependencies, base images, optional service images, datasets, fonts, and
other third-party materials retain their respective licenses, public-domain
status, and other applicable terms.
An integration, Compose profile, image reference, or configuration example does
not transfer ownership to ISLAMU or relicense the referenced software.

Operators must review the exact version they select. Distributors that mirror,
bundle, or deliver third-party binaries or container images must also satisfy
the applicable attribution, notice, corresponding-source, patent, trademark,
commercial-use, and redistribution obligations. An ISLAMU commercial agreement
must identify those materials separately and must not restrict rights granted
directly by their upstream licenses. See
[CI/CD Governance](CI_CD_GOVERNANCE.md#standalone-and-optional-service-license-boundary)
for the release evidence required before ISLAMU redistributes an optional image.

Registration-provider support is provider-neutral and does not require an extra container unless an operator selects a self-hosted provider. Store provider API tokens and webhook secrets as tenant-scoped secret bindings (`registration_provider.api_token`, `registration_provider.webhook_secret`) with a bounded qualifier when multiple tenant connections use the same key. Approved browser/embed origins are managed as connection data through the API, not `.env` iframe snippets.

### Optional Formbricks mirror profile

Formbricks is optional and never starts with the default Compose stack. To run the pinned self-hosted topology, generate the five required secrets documented in `.env.example`, then start its profile:

```bash
docker compose --profile formbricks config --quiet
docker compose --profile formbricks up -d formbricks
```

The profile starts isolated PostgreSQL and Valkey storage, one-shot Formbricks and Hub migrations, Hub, Cube, and Formbricks `5.2.2` at `http://localhost:3005`. Application and Hub images are digest-pinned; PostgreSQL, Valkey, and Cube state use dedicated `formbricks_*` volumes. Stop only this optional topology with `docker compose --profile formbricks down`.

Configure the provider binding with collection mode `MirrorOnly` (`MIRROR_ONLY`, ID `4`) when ISLAMU Event must remain canonical. The accepted native submission commits first; the existing provider write-effect worker then sends only mapped fields whose form definition has `IsProviderTransferAllowed=true`. Provider failure cannot roll back the ISLAMU Event submission. File answers and multilingual form provisioning are not advertised by the pinned Formbricks tuple.

### Current host topology support

The supported Docker Compose deployment is `Split`: one migration process, an
API on `http://localhost:7039`, and a Blazor BFF on
`http://localhost:7002`. It remains the default deployment topology.

Local Aspire also supports an opt-in one-process `Standalone` topology. Set
`Hosting:Topology=Standalone` (environment form `Hosting__Topology=Standalone`);
it exposes both the UI and `/api/*` on
`https://localhost:7180` (HTTP via `WithHttpEndpoint(name: "http")`, dynamically assigned), waits for the migration
service and selected local infrastructure itself, and registers Keycloak
callbacks against that combined browser endpoint. The default omitted value is
`Split`, which keeps API `https://localhost:7039` and BFF
`https://localhost:7177` as separate local development endpoints.

AppHost's Standalone HTTP endpoint is dynamic/non-guaranteed internal HTTP through
`WithHttpEndpoint(name: "http")`; HTTPS is explicitly `https://localhost:7180`.
When launching `Event.Standalone` directly with its checked-in launch profiles,
HTTP is `http://localhost:5180` and the HTTPS profile also binds
`https://localhost:7180`.

`Event.Standalone` owns API startup, workers, readiness, and graceful shutdown
once. Its `/api/*` bridge is the Combined BFF/API trust boundary: it converts
the BFF cookie session only into a server-held bearer request after antiforgery
and header sanitation; it does not register YARP or create a loopback/self-
proxy, and API authentication remains the controller authority.

`CONTROL_PLANE_PUBLIC_ORIGIN` must be the browser-facing control-plane origin.
In Aspire it is forwarded to the selected host and becomes
`Bff__AdminHosts__0`; when starting a host outside Aspire, configure both the
public origin and the exact BFF admin host yourself. The origin is not derived
from a container or service-discovery address.

> [!IMPORTANT]
> The Split stack remains `docker-compose.yml`. Standalone is one Docker image,
> run directly with `docker run --env-file .env`; there is no standalone Compose
> descriptor or helper container. It defaults to SQLite and is independent of
> the Aspire-only `Hosting:Topology` selector.

The three application composition roots (`Explore.API`, `Explore.Blazor`, and `Event.Standalone`) preserve one API contract: use `/api/...` with non-URL API versioning (`Accept`, `?api-version=`, or `X-Api-Version`); do not add a path-version segment (see [the support matrix](ARCHITECTURE.md#hosting-topology)). To return from the opt-in one-process host, restart AppHost in its Split default; this topology rollback does not alter persisted data.

---

## Table of Contents

1. [Quick Start (5 Minutes)](#quick-start-5-minutes)
2. [Standalone Container Quick Start](#standalone-container-quick-start)
3. [Architecture Overview](#architecture-overview)
4. [Service Reference](#service-reference)
5. [Deployment Tiers](#deployment-tiers)
6. [Environment Configuration](#environment-configuration)
7. [First-Run Setup](#first-run-setup)
8. [Identity Provider (Keycloak)](#identity-provider-keycloak)
9. [Optional Services](#optional-services)
10. [Reverse Proxy & TLS](#reverse-proxy--tls)
11. [Multi-Tenant Deployment](#multi-tenant-deployment)
12. [Email Configuration](#email-configuration)
13. [Health Checks & Monitoring](#health-checks--monitoring)
14. [Backup & Upgrade](#backup--upgrade)
15. [Standalone Backup, Restore, And Changeover](#standalone-backup-restore-and-changeover)
16. [Troubleshooting](#troubleshooting)
17. [Related Documentation](#related-documentation)

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

**3. Choose your setup-secret source in `.env`:**

```bash
# Optional: leave blank to use the generated setup_data volume file
SETUP_SECRET=my-secure-setup-secret
```

**4. Verify the resolved configuration:**

```bash
docker compose config
```

**5. Migrate, then start the core stack:**

```bash
docker compose run --rm event-migrationservice
docker compose up -d
```

If `SETUP_SECRET` is empty, retrieve the generated value from the API's
dedicated bootstrap volume. The logs contain this command, never the value:

```bash
docker cp islamu-event-api:/app/bootstrap/setup-secret ./setup-secret
chmod 600 ./setup-secret
```

The one-shot migration process must exit successfully before the API starts.
The default Compose database is PostgreSQL; its `.env` `DATABASE_*` values are
Compose interpolation inputs that map to the native structured keys inside the
service definitions. They are not application environment aliases.
This Compose procedure is intentionally Split-only; do not set
`Hosting__Topology=Standalone` expecting the existing Compose services to
combine.

**6. Open the application:**

| Service | URL |
|---|---|
| **Web UI** | `http://localhost:7002` |
| **API** | `http://localhost:7039` |
| **Mailpit** (email viewer) | `http://localhost:8025` |
| **Keycloak Admin** | `http://localhost:8080` |
| **Formbricks** (optional `formbricks` profile) | `http://localhost:3005` |

**7. Complete first-run setup** at `http://localhost:7002/setup` using the explicit or retrieved secret, then remove the host copy.

> [!TIP]
> For the absolute minimal deployment (no Keycloak, no Redis, no Mailpit), see [Deployment Tiers → Tier 1: Bare Minimum](#tier-1-bare-minimum).

## Standalone Container Quick Start

Build one image, create one durable data volume, and run the image with the
repository `.env` file. The image owns `/app/data` as its non-root UID, applies
application, Data Protection, and privacy-authority migrations before accepting
traffic, then starts the combined API/BFF host. Both SQLite files live in the
same volume by default: `/app/data/islamu_event.db` and
`/app/data/privacy_erasure_authority.db`.

```bash
docker build -t islamu/event-standalone -f src/Event.Standalone/Dockerfile .
docker volume create event_standalone_data
docker run --rm --name islamu-event-standalone \
  --env-file .env \
  --mount source=event_standalone_data,target=/app/data \
  -p 8080:8080 \
  islamu/event-standalone
```

The web listener is published on `http://localhost:8080` by default. After the
services are running, probe it from the host:

```bash
curl --fail http://localhost:8080/health
```

When `.env` contains an empty `SETUP_SECRET`, the image creates one random
secret in the existing data volume. Logs show only how to retrieve it:

```bash
docker logs islamu-event-standalone
docker cp islamu-event-standalone:/app/data/setup-secret ./setup-secret
chmod 600 ./setup-secret
```

Use the copied value at `http://localhost:8080/setup`, then remove the local
copy. The volume copy survives container restarts but is deleted automatically
when onboarding completes. A non-empty `SETUP_SECRET` in `.env` overrides this
behavior and removes any stale generated file.

The process exits before binding port `8080` if migration or seeding fails. No
second container, init container, or shell entrypoint is involved.

### Build and run the web image directly

Build the web image explicitly:

```bash
docker build -t islamu/event-standalone -f src/Event.Standalone/Dockerfile .
```

Create the named volume once. Docker initializes it from the image's app-owned
`/app/data` directory, so a root `volume-init` helper is unnecessary:

```bash
docker volume create event_standalone_data
```

Then start the single container. Native .NET keys such as
`Database__Provider` in `.env` override the image's SQLite defaults when needed:

```bash
docker run --rm --name islamu-event-standalone --env-file .env \
  --mount source=event_standalone_data,target=/app/data \
  -p 8080:8080 islamu/event-standalone
```

`linux/amd64` is the initial supported image target. `linux/arm64`, Kubernetes,
and Helm packaging are deferred; do not infer their support from this image.

---

## Architecture Overview

The minimum standalone topology is deliberately small:

```text
Browser / API client
        |
        v
Event.Standalone
  - Blazor BFF/UI
  - ISLAMU Event API and workers
        |
        v
SQLite persistence
```

The following diagram describes the separate split topology:

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
                    │  • Startup gates; deployed schema is external│
                    │  • Background workers & outbox processing    │
                    │  • Health checks, metrics, MCP adapter       │
                    └──────┬──────────────────────┬────────────────┘
                           │                      │
                  ┌────────▼────────┐    ┌────────▼────────────────┐
                  │ Primary DB      │    │   Optional Services     │
                  │  (selected)     │    │  Redis, Keycloak,       │
                  │                 │    │  Cerbos, MinIO, Svix…   │
                  └─────────────────┘    └─────────────────────────┘
```

**Key design principles:**

- **Browsers talk only to the Blazor BFF.** The BFF proxies API calls via YARP; clients should not need direct API access.
- **The API is the API-host composition root** for Domain, Application, Persistence, and Infrastructure layers; `Event.Standalone` reuses that host module when Aspire selects the optional one-process topology.
- **The primary datastore is selected explicitly.** PostgreSQL, SQLite, SQL
  Server, MariaDB, and MySQL support the application and Data Protection models
  but use separate generated migration sets.
- **Privacy-erasure authority storage is configurable.**
  `EmbeddedSqlite` uses the dedicated local
  `/app/data/privacy_erasure_authority.db` file; `CoLocated` stores authority
  tables in the primary application database; `ExternalDatabase` uses a separate
  PostgreSQL database.

---

## Service Reference

### Standalone Minimum

These are the only operational components required by the standalone
distribution:

| Component | Purpose | External process required? |
|---|---|---|
| `Event.Standalone` | Combined API, workers, Blazor BFF/UI, health, and in-process migration owner | One ISLAMU Event process |
| SQLite persistence | Primary and embedded privacy-authority durable files | No; embedded in the standalone process |

### Split Deployment Components

The split packaging runs these components separately. They are not additional
requirements of the standalone distribution:

| Service | Compose Name | Purpose | Default Port |
|---|---|---|---|
| Selected primary database | `postgres` in the default Compose topology | Application, outbox, and Data Protection state | provider-specific |
| Migration Service | `event-migrationservice` | One-shot provider-specific schema migration and seed | — |
| API | `islamu-event-api` | REST API, workers, health, metrics | `7039:8080` |
| Blazor BFF | `islamu-event-ui` | Web UI, admin shell, YARP proxy to API | `7002:8080` |

### Compose Convenience Services (Optional Product Capabilities)

These are included in the current split `docker compose up` convenience stack,
but they are not requirements of ISLAMU Event or its standalone distribution:

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
| `privacy-erasure-external` | `privacy-erasure-db` | External PostgreSQL topology for privacy-erasure authority |
| `localization` | `weblate-db`, `weblate` | Optional translation-management service |
| `formbricks` | Formbricks, Hub, Cube, PostgreSQL, and Valkey services | Optional registration-form mirror provider |

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
| `Event.Standalone` API + Blazor BFF/UI process | ✅ Yes |
| Durable SQLite volume | ✅ Yes |
| External identity provider, including Keycloak | Optional; required only when interactive OIDC login is enabled |
| Redis or Valkey | Optional |
| SMTP or Mailpit | Optional; needed only for email delivery/testing |
| External database server and every other service | Optional |

```bash
# Minimal start — one application container and one durable SQLite volume
docker build -t islamu/event-standalone -f src/Event.Standalone/Dockerfile .
docker volume create event_standalone_data
docker run --rm --name islamu-event-standalone \
  --env-file .env \
  --mount source=event_standalone_data,target=/app/data \
  -p 8080:8080 islamu/event-standalone
```

- **Deployment mode:** Single-tenant (default, `DEPLOYMENT_MODE` unset).
- **Authorization:** Built-in local authorization (`AUTHORIZATION_PROVIDER` unset or `local`).
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
- Separated database clusters for application data, external erasure authority,
  identity (Keycloak), and policy storage
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

For the Split `docker-compose.yml`, these are Compose `.env` interpolation
inputs. The application receives the mapped native `Database__*` keys, not
`DATABASE_*` compatibility aliases. The standalone descriptor uses the native
key contract in [Configuration](CONFIGURATION.md#persistence-configuration)
and calls its database-name interpolation input `DATABASE_NAME`.

| Variable | Default | Purpose |
|---|---|---|
| `DATABASE_PROVIDER` | `PostgreSql` | `PostgreSql`, `Sqlite`, `SqlServer`, `MariaDb`, or `MySql` |
| `DATABASE_HOST` | `postgres` | Required for server providers; omit for SQLite |
| `DATABASE_PORT` | provider default | Optional server port (`5432`, `1433`, or `3306`) |
| `DATABASE_NAME` | `islamu_event_db` | Server database name, or persisted local SQLite file path |
| `DATABASE_SCHEMA` | `islamu_event` | PostgreSQL or SQL Server namespace; tables remain clean, for example `islamu_event.users`. SQLite, MariaDB, and MySQL always use  prefix ie_ like `ie_users` regardless of this value. No prefix override is supported. |
| `DATABASE_TLS_MODE` | `Prefer` in local Compose | `Prefer`, `Required`, or `Disabled`; production server deployments should use `Required` |
| `DATABASE_TRUST_SERVER_CERTIFICATE` | `false` | Certificate bypass accepted only with required TLS; development only |
| `DATABASE_RUNTIME_USERNAME`, `DATABASE_RUNTIME_PASSWORD` | `explore` locally | Runtime role; forbidden for SQLite |
| `DATABASE_MIGRATOR_USERNAME`, `DATABASE_MIGRATOR_PASSWORD` | `explore` locally | Migration role; forbidden for SQLite and kept out of API/Blazor |
| `DATABASE_SERVER_FLAVOR`, `DATABASE_SERVER_VERSION` | blank | Required only for MariaDB/MySQL and must match the selected engine |
| `KEYCLOAK_BLAZOR_CLIENT_SECRET` | *(none — must generate)* | OIDC confidential client secret |
| `KEYCLOAK_ENDPOINT` | `http://keycloak.localhost:8080` | Keycloak base URL |
| `KEYCLOAK_REALM` | `ISLAMU` | Keycloak realm name |

> [!IMPORTANT]
> `KEYCLOAK_BLAZOR_CLIENT_SECRET` has no default. Generate it with `openssl rand -hex 32` and add it to your `.env` before starting.

### Database namespace and multiple instances

The namespace rule is automatic; operators do not select a prefix:

| Provider | Application boundary | Example | Recommended instance layout |
|---|---|---|---|
| PostgreSQL | `DATABASE_NAME` + `DATABASE_SCHEMA` | `islamu_event.users` | Different schemas may share one database; give each instance a distinct `Scheduler:Quartz:SchedulerName` so the co-located `QRTZ_` scheduler tables are not contended. |
| SQL Server | `DATABASE_NAME` + `DATABASE_SCHEMA` | `islamu_event.users` | Assign a distinct schema to each instance sharing a database. |
| SQLite | Durable local `DATABASE_NAME` file + forced `ie_` prefix | `ie_users` | One file and one application replica per instance. |
| MariaDB / MySQL | `DATABASE_NAME` + forced `ie_` prefix | `ie_users` | Create a separate database per instance on the same server; the prefix is an additional collision guard. |

For PostgreSQL or SQL Server, changing `DATABASE_SCHEMA` changes the target
namespace and removes the need for prefixed table names. MigrationService
creates/migrates that namespace with clean names. It does not move data from a
previous schema, so back up and explicitly export/import existing data before a
schema change. Runtime and migrator services must receive the same schema.

For SQLite, MariaDB, and MySQL, `DATABASE_SCHEMA` remains part of the uniform
validated configuration but does not affect table placement. The application
forces `ie_`; `DATABASE_PREFIX`, `DATABASE_RUNTIME_PREFIX`,
`DATABASE_MIGRATOR_PREFIX`, `Database:Prefix`, `Database:Runtime:Prefix`, and
`Database:Migrator:Prefix` are rejected.

The API and MigrationService construct native connection strings from these
validated fields. Do not pre-construct or inject a raw application connection
string.

SQLite requires additional deployment discipline: use a local durable path,
mount the same primary file into MigrationService and API, never use a network
filesystem or in-memory URI, and run exactly one API instance. The connection
uses a 30-second busy timeout and MigrationService enables WAL after migration.
The primary file must not be `/app/data/privacy_erasure_authority.db`.

### Recommended Variables

| Variable | Default | Purpose |
|---|---|---|
| `SETUP_SECRET` | *(generated file fallback)* | Optional explicit operator secret; a non-empty value always overrides generated-file behavior |
| `SETUP_SECRET_FILE` | Temp default; Compose: `/app/bootstrap/setup-secret`; standalone: `/app/data/setup-secret` | Owner-only generated secret path; Compose and standalone map it to their named data volumes |
| `DEPLOYMENT_MODE` | *(blank = single-tenant)* | Set `multi_tenant` before first launch for multi-tenant |
| `CONTROL_PLANE_PUBLIC_ORIGIN` | — | Control-plane admin host for BFF admin-host registration |
| `Hosting__Topology` | `Split` when running Aspire | AppHost-only local composition selector: `Split` or opt-in `Standalone`; ignored by the current Compose topology |
| `Bff__AdminHosts__0` | AppHost derives it from `CONTROL_PLANE_PUBLIC_ORIGIN` | Exact standalone/BFF admin host when starting the process without AppHost |

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

Promotion codes require the native non-secret setting `Promotions__CodeLookup__ActiveKeyVersion` (default `1`) plus an instance `SecretBinding` for `promotions.code_lookup_hmac_key` with the matching qualifier, such as `v1`. An environment-backed initial binding may point at `PROMOTIONS_CODE_LOOKUP_HMAC_KEY`; its value is standard Base64 for at least 32 random bytes. Infisical uses `/promotions/PROMOTIONS_CODE_LOOKUP_HMAC_KEY`, and application-managed deployments may instead use an inline-encrypted binding.

The checked-in Split Compose file passes an explicit environment allow-list and does not currently forward these promotion names from `.env`. Setting them in `.env` alone therefore has no effect: provision the binding through the secret control plane, or explicitly inject the native option and chosen version-specific secret variable into the API container in your deployment configuration. Standalone/direct-process deployments may supply the native names directly. The resolver deliberately has no source fallback. Missing bindings, missing values, invalid Base64, and values shorter than 32 bytes fail closed when a code is published, rotated, or applied.

For HMAC-key rotation, create a distinct source coordinate for the new value, add the new qualified binding first, change the active version, restart API replicas, and keep every older key whose version is still referenced by an active promotion code. Do not overwrite a key in place or point overlapping qualifiers at one mutable environment variable. Organizer-facing code rotation is separate: it retires the old code and shows the replacement plaintext once in Studio. Neither flow enables Stripe capture or any Phase 18 payment runtime.

---

## First-Run Setup

After starting the stack for the first time:

### 1. Set Your Setup Secret

Leave `SETUP_SECRET` empty for a single API instance to generate one owner-only file, then follow the logged `docker cp` instruction. Split Compose persists it in `setup_data`; standalone persists it in `event_standalone_data`; a direct unmounted API uses `/tmp/islamu-event/setup-secret` and may generate a replacement after container recreation. Raw values are never printed. Read-only or rolling/multi-replica deployments must provide one explicit shared `SETUP_SECRET` through their platform secret manager.

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

### Address Geocoding (Photon)

**Default behavior:** `GEOCODING_PROVIDER=None`. The application performs local
governed address lookup only and makes no outbound geocoding request.

Production Photon is opt-in. The operator must own the deployment or have a
separate production contract for it. ISLAMU does not provision a Photon
container, download OpenStreetMap planet data, or fall back to the public
`photon.komoot.io` demonstration endpoint.

Photon 1.0 requires Java 21 or a separately reviewed immutable image and uses
OpenSearch. Upstream estimates a 2026 planet index at roughly 95 GB, growing
about ten percent per year, with SSD/NVMe storage and at least 64 GB RAM
recommended for smooth planet-scale operation. Atomic dataset replacement
requires room for both current and staged indexes, so capacity planning must
reserve approximately twice the active dataset plus backup and filesystem
headroom. A country or regional index can be substantially smaller, but its
actual import duration, disk, memory, latency, and concurrency remain
operator-benchmarked facts rather than application defaults.

Before setting `GEOCODING_PROVIDER=Photon`, record a deployment manifest outside
application configuration containing:

- the approved Photon release and immutable image or binary digest;
- the dataset extract, import process, dataset timestamp, and checksums;
- OpenStreetMap/ODbL attribution and data-offer responsibilities;
- measured capacity and the configured request/result/response-size bounds;
- refresh cadence, maintenance window, and rollback procedure;
- backup and restore evidence for configuration and dataset artifacts;
- the operator owner, approval date, and activation/change record.

Start from
[`docs/examples/photon-deployment-manifest.example.yaml`](examples/photon-deployment-manifest.example.yaml),
store the populated manifest in the operator's controlled evidence system, and
reference it from the activation change. The checked-in file is a schema
example only; do not put production endpoints, tenant data, queries, addresses,
tokens, or credentials in it.

Only executable settings belong in `.env`: endpoint, language, country
allowlist, result/response ceilings, and the active dataset fingerprint. There
is no Photon API key. The endpoint must be HTTPS and `/status` must be reachable
within the bounded readiness timeout before activation.

For a regional deployment:

1. Pin a Photon release artifact and SHA-256 digest. Select a version-compatible
   immutable country dump, or produce a filtered import from an operator-owned
   Nominatim/JSON source using the same country and language filters on every
   refresh.
2. Record the source extract, acquisition timestamp, source and manifest
   checksums, imported languages/countries, and ODbL attribution/data-offer
   location in the deployment manifest.
3. Import into a new directory. Never import over the active `photon_data`
   directory; Photon documents that this corrupts the database.
4. Benchmark representative non-production queries within the application's
   five-second total budget and configured result/response ceilings. Verify
   query-free `/status`, TLS, capacity, and telemetry redaction.
5. Switch the HTTPS endpoint or atomically swap the staged directory, restart
   Photon, verify readiness and application local/provider behavior, then retain
   the previous index until the rollback window closes.

`local-full` may opt into an independently pinned local Photon process by
setting the same `GEOCODING_*` contract before starting AppHost. AppHost
forwards those values to split and standalone application hosts but does not
download Java, binaries, images, or datasets. `local-default`, `local-core`,
and `local-lite` remain lightweight and provider-disabled unless an operator
explicitly supplies the full contract. Local endpoints still require HTTPS;
terminate TLS at an operator-controlled local reverse proxy.

Refreshing the dataset is a controlled deployment: stage and checksum the new
dataset, benchmark it, verify `/status`, switch traffic, retain the previous
dataset for rollback, and record restore evidence. Never auto-download
`planet-latest`, `latest` aliases, or mutable unverified images during
application startup.

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
| `CERBOS_ADMIN_PASSWORD` | Plaintext Admin API password for server-side or `cerbosctl` policy sync |

When `CERBOS_ADMIN_USERNAME` and `CERBOS_ADMIN_PASSWORD` are both present, onboarding uses them server-side and collapses the optional one-time credential form. If the pair is absent, enter it in onboarding for a single sync; even when deployment secrets exist, expand **Use one-time credentials instead** to override them for that request. One-time values are not saved and are cleared after the call. Use TLS for a remote Admin API, rotate the plaintext secret and its matching `CERBOS_ADMIN_PASSWORD_HASH` together, and rerun sync after rotation. Database backup/restore does not capture these credentials; restore them through the deployment secret provider or enter them again for one sync.

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
- Current provider-specific application migrations for ATProto records

For full details, see [FEDERATION.md](FEDERATION.md) and [CONFIGURATION.md](CONFIGURATION.md#at-protocol-events-governance-and-workers).

### Privacy Erasure Authority Topology

The Privacy Erasure Authority orchestrates GDPR erasures and anti-resurrection replay protection. It supports three topologies:

| Topology | Supported Primary Databases | Placement, operator backup units, and `restoreReplayProtection` |
|---|---|---|
| **`EmbeddedSqlite`** *(Default)* | **All 5 providers** (PostgreSQL, SQLite, SQL Server, MariaDB, MySQL) | Dedicated local file (`/app/data/privacy_erasure_authority.db`); back up it and the primary database separately. Independent restore boundary with single-writer WAL mode; `true` when the authority file is excluded from the primary restore. |
| **`CoLocated`** | **PostgreSQL or SQLite only** | Primary application database; one primary backup includes authority rows and `restoreReplayProtection` is `false`. |
| **`ExternalDatabase`** | **All 5 providers** (Authority DB itself is separate PostgreSQL) | Separate dedicated PostgreSQL database with function-only runtime role (`SECURITY DEFINER`) and distinct migrator role; back up it separately from the primary database, with `restoreReplayProtection` `true` when independently restored. |

> [!IMPORTANT]
> **CoLocated Provider Restrictions:** `CoLocated` authority requires provider-native row-locking or WAL writer serialization semantics implemented only for PostgreSQL and SQLite. If `CoLocated` is configured with `SqlServer`, `MariaDb`, or `MySql`, the application **fails closed during composition before opening network connections or performing adapter/database I/O**. Diagnostic errors contain bounded safe remediation instructions and **never expose database credentials, connection strings, or host details**. Unsupported or removed authority contracts have no fallback, translation, dual write, or compatibility shim.

**Standalone image behavior:** `EmbeddedSqlite` stores the authority ledger at
`/app/data/privacy_erasure_authority.db` beside the primary file in the single
`event_standalone_data` volume. Restrict filesystem access to the application
identity and archive the authority file separately. It uses private cache, WAL,
a bounded busy timeout, and a single writer. It must never be the primary SQLite
file or be restored merely because the primary database is restored.

For `ExternalDatabase` topology:

```bash
ERASURE_TOPOLOGY=ExternalDatabase \
docker compose --profile privacy-erasure-external up -d
```

This starts a separate authority PostgreSQL container. Supply structured endpoint fields
and separate runtime/migrator roles, then run `event-migrationservice` before
the API.

For `CoLocated`, no separate authority target is configured and it shares the
primary `PostgreSql` or `Sqlite` database. Other primary providers fail closed. Raw authority connection strings are not supported.

The authority reports `authorityHighWater`, `authorityRetainedFloor`, and a
bounded `replayReasonCode` through readiness. Retention maintenance must first
run its dry-run evaluation with the complete explicit legal-hold sequence set.
Apply removes only an expired contiguous prefix and moves the floor atomically;
held rows keep sequence continuity with randomized audit tokens. After the floor
moves, primary backups with checkpoints below it are intentionally not
restorable. Preserve a verified primary backup at or above the floor and never
repair an incident with direct authority-table DML.

See [PRIVACY_ERASURE.md](PRIVACY_ERASURE.md) for full architectural guidance.

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

**RabbitMQ dispatch** is optional transport infrastructure. Basic dispatch
requires no broker. The Quartz scheduler provides durable dispatch scheduling on
every primary provider, including standalone SQLite; `HostedService` remains
available as a scheduler-free timer. Enable RabbitMQ only if you have an
operator-provided broker and set
`EmailDispatchRabbitMq:Enabled=true`.

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

### Background Scheduler Monitoring

Background work — email dispatch drain, retention sweeps, storage reconciliation
— runs on the Quartz.NET scheduler. Three optional operator surfaces expose it,
all disabled by default so a deployment adds no admin attack surface it did not
ask for.

**Recommended for most deployments — the admin UI.** Set:

```bash
Scheduler__Quartz__AdminApiEnabled=true
```

A **Background Scheduler** entry then appears under Instance Settings →
Operations for instance administrators. It shows whether the scheduler is
running or in standby, how many jobs are registered, paused, and executing, and
a table of every job with its owner, trigger state, schedule, and next/previous
run times. This works in **both** deployment topologies (split API + Blazor, and
the standalone container).

Controls (pause/resume the scheduler, pause/resume/run a single job) stay hidden
until you also set:

```bash
Scheduler__Quartz__AdminApiReadOnly=false
```

Leaving the default `true` in production is deliberate: read-only mode withholds
the control affordances from the API response, so the UI cannot present a button
that would be refused.

**Standalone only — the upstream Quartz dashboard.** The combined container can
additionally mount Quartz.NET's own dashboard, which adds calendars, execution
history, and live logs:

```bash
Scheduler__Quartz__DashboardEnabled=true
```

It is served at `/quartz` and requires an instance administrator session. This
option exists only in the standalone container, where the web UI and the
scheduler share one process. In split deployments the API host runs no web UI,
so use the admin UI above.

**For scripted checks**, `Scheduler__Quartz__StatusEndpointEnabled=true` exposes
a single read-only JSON document at `/admin/scheduler` instead of a UI.

> Note: the scheduler surfaces report scheduling state only — job names,
> trigger states, and fire times. They never expose email bodies, recipients,
> provider credentials, or tenant content. Email delivery state remains owned by
> the EmailDispatch admin API, not the scheduler.

### Support Access

Admin support access is off by default (`support_access.enabled=false`). Enable it only after operational approval, audit retention, and tenant trust communication are ready. Keep `support_access.allow_write_mode=false` unless you have an explicit break-glass approval process.

---

## Backup & Upgrade

### Pre-Upgrade Checklist

Before every upgrade:

1. ✅ Back up the selected primary database with its provider-native tool
2. ✅ Back up the authority according to topology: the dedicated `EmbeddedSqlite` file separately, `CoLocated` rows in the primary backup, or the independently managed external PostgreSQL authority database separately
3. ✅ Back up Keycloak PostgreSQL data (if using local Keycloak)
4. ✅ Back up object storage — `local_storage_data` volume, `minio_data`, or S3 bucket
5. ✅ Record image tags, commit SHA, enabled Compose profiles, and secret-provider key names
6. ✅ Read release notes for migrations, config changes, and rollback constraints

Do not back up `setup_data` or a generated setup-secret file. The file is
bootstrap-only and is removed automatically when onboarding completes.

### Migrations

| Path | When Used | Behavior |
|---|---|---|
| API startup in Development | Contributor convenience | Applies the application migration and seed; not a deployment contract |
| `Event.MigrationService` | Every deployed provider | Applies application, Data Protection, and exactly one selected authority migration path, enables SQLite WAL, seeds, then exits before API/Blazor start |

> [!NOTE]
> Run `Event.MigrationService` for every deployed provider and require exit code
> zero before starting API replicas. The standalone image is the documented
> in-process exception and performs the same work before binding HTTP. Running it
> again is the supported idempotency check: EF applies only pending migrations and
> seeding repairs only missing rows.

### Creating Migrations from Scratch

Migration and snapshot files are generated artifacts. PostgreSQL uses the
`Explore.Persistence` assembly; SQLite, SQL Server, MariaDB, and MySQL use the
matching `Explore.Persistence.Migrations.{Provider}` and
`Explore.Persistence.DataProtection.Migrations.{Provider}` projects. Generate
or remove an unapplied migration only with `dotnet ef`; never edit its C# or
snapshot output. See [OPERATIONS.md](OPERATIONS.md#api-startup-behavior) for the
provider-specific ownership contract. Application and Data Protection
provider-native assemblies and their migration history tables remain distinct,
as does the one authority path selected for a deployment; do not merge histories
or run more than one authority path.

For the full backup and restore runbook, see [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md).  
For the release checklist, see [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md).

## Standalone Backup, Restore, And Changeover

### Cold SQLite backup

Take cold backups only: stop the web service first so no process writes either
SQLite database while its primary file, WAL, and shared-memory sidecars are
archived. The two archives are intentionally separate because the primary and
privacy-erasure authority databases have independent restore lifecycles.

```bash
mkdir -p backups
docker stop islamu-event-standalone
docker run --rm --mount source=event_standalone_data,target=/app/data -v "$(pwd)/backups:/backup" busybox:1.37.0-musl sh -c 'set -eu; cd /app/data; tar -czf /backup/event-primary-$(date +%Y%m%dT%H%M%SZ).tgz islamu_event.db*'
docker run --rm --mount source=event_standalone_data,target=/app/data -v "$(pwd)/backups:/backup" busybox:1.37.0-musl sh -c 'set -eu; cd /app/data; tar -czf /backup/privacy-erasure-authority-$(date +%Y%m%dT%H%M%SZ).tgz privacy_erasure_authority.db*'
```

Store and verify both archives away from the Docker host. The temporary BusyBox
commands are cold-backup tooling, not services in the standalone deployment;
the chiseled application image intentionally has no shell, `sqlite3`, or `curl`.

### Cold SQLite restore

> [!WARNING]
> Restore overwrites the selected database and its `-wal`/`-shm` sidecars.
> Stop the web service, preserve the current archives, and restore the primary
> and authority archives only when their independent recovery decision permits
> it. Do not replace the whole named volume or treat an authority archive as a
> primary-database archive.

Set each archive basename to the exact cold backup you selected. The commands
validate and list the selected archive before removing any database file:

```bash
export PRIMARY_ARCHIVE='event-primary-20260808T000000Z.tgz'
export AUTHORITY_ARCHIVE='privacy-erasure-authority-20260808T000000Z.tgz'
docker stop islamu-event-standalone
docker run --rm -e PRIMARY_ARCHIVE --mount source=event_standalone_data,target=/app/data -v "$(pwd)/backups:/backup" busybox:1.37.0-musl sh -c 'set -eu; case "$PRIMARY_ARCHIVE" in ""|.|..|*/*) exit 64 ;; event-primary-*.tgz) ;; *) exit 64 ;; esac; archive="/backup/$PRIMARY_ARCHIVE"; test -f "$archive"; tar -tzf "$archive"; rm -f /app/data/islamu_event.db /app/data/islamu_event.db-wal /app/data/islamu_event.db-shm; tar -xzf "$archive" -C /app/data; chown 1654:1654 /app/data/islamu_event.db*'
docker run --rm -e AUTHORITY_ARCHIVE --mount source=event_standalone_data,target=/app/data -v "$(pwd)/backups:/backup" busybox:1.37.0-musl sh -c 'set -eu; case "$AUTHORITY_ARCHIVE" in ""|.|..|*/*) exit 64 ;; privacy-erasure-authority-*.tgz) ;; *) exit 64 ;; esac; archive="/backup/$AUTHORITY_ARCHIVE"; test -f "$archive"; tar -tzf "$archive"; rm -f /app/data/privacy_erasure_authority.db /app/data/privacy_erasure_authority.db-wal /app/data/privacy_erasure_authority.db-shm; tar -xzf "$archive" -C /app/data; chown 1654:1654 /app/data/privacy_erasure_authority.db*'
docker run -d --name islamu-event-standalone --env-file .env --mount source=event_standalone_data,target=/app/data -p 8080:8080 islamu/event-standalone
curl --fail http://localhost:8080/health
```

The restarted standalone process reapplies pending migrations before serving.
Verify its logs and the host-side `/health` result; neither `sqlite3` nor `curl`
is expected inside the runtime image.

### Rollback and provider switching

To roll back an image, stop the web service, restore the compatible cold
archives, then start the previous image through the same migration-first flow.
Do not downgrade a database merely by changing an image tag. For a provider
switch, take and verify independent primary and authority backups, provision
the target server database, supply its native `Database__*` fields and separate
runtime/migrator credentials as described in [Configuration](CONFIGURATION.md#standalone-provider-overrides), run the migration service, and start the
web service. Changing `Database__Provider` does not convert existing SQLite
data; use a separately planned export/import procedure.

---

## Troubleshooting

### Common Issues

| Problem | Cause | Fix |
|---|---|---|
| `unauthorized_client` from Keycloak | `KEYCLOAK_BLAZOR_CLIENT_SECRET` mismatch | Rerun `docker compose run --rm keycloak-init` |
| OIDC redirect URI errors behind proxy | Missing `X-Forwarded-Proto`/`X-Forwarded-Host` | Configure reverse proxy to forward these headers |
| Setup secret not working | Wrong explicit value, replaced/missing generated file, or session expired | Rerun the logged `docker cp` instruction while onboarding is incomplete; split uses `/app/bootstrap/setup-secret`, standalone uses `/app/data/setup-secret`, and a direct API defaults to `/tmp/islamu-event/setup-secret`. Re-enter after session expiry. |
| API fails because setup-secret storage is not writable | Read-only filesystem and no writable bootstrap mount | Supply an explicit shared `SETUP_SECRET`, or mount one writable single-instance bootstrap path. Do not print the generated value or add a helper container. |
| Cerbos denying all requests | PDP is down or unreachable | Cerbos fails closed. Check `CERBOS_GRPC_ENDPOINT` or switch to `AUTHORIZATION_PROVIDER=local` |
| Realm export changes not applied | Keycloak skips import when realm exists | Remove the `keycloak_data` volume and restart |
| `429` responses | Rate limiting or OpenGraph image saturation | Review rate-limit configuration and dispatch capacity |
| Storage health unhealthy | Selected provider can't write to data root | Check volume mounts and file permissions |
| Standalone exits before opening port `8080` | Schema, authority migration, or seeding failed | Read the one container log, correct the native structured settings or target database, then rerun the image |
| SQLite permission denied | An existing `/app/data` volume was created with incompatible ownership | Recreate only an empty disposable volume, or correct the existing volume ownership to UID/GID `1654`; do not run the application image as root |
| `/health` fails on port `8080` | Startup migration is still failing or a runtime dependency is unready | Read the standalone container log, then probe `curl --fail http://localhost:8080/health` from the host |
| Database provider validation fails | Missing/invalid native `Database__*` field, wrong TLS policy, or MariaDB flavor/version omitted | Use the provider matrix in [Configuration](CONFIGURATION.md#standalone-provider-overrides); direct image execution does not translate legacy `DATABASE_*` aliases |
| Promotion create, publish, rotation, or apply fails after a key change | Missing qualified binding, active-version mismatch, malformed Base64, short key, or an older active code still references a removed version | Restore the exact `v{version}` instance binding, verify the secret decodes to at least 32 bytes, restart after changing `Promotions__CodeLookup__ActiveKeyVersion`, and retain old versions until no active code references them. Never log or paste the key, digest, or a real attendee code. |

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

## Current And Planned Features

### Paid Events And Stripe Connect

ADR-022 approves Stripe Connect `OrganizerDirect` as the first paid-event profile. Each deployment supplies and operates its own instance/server-only `payments.stripe.platform_secret_key` and `payments.stripe.webhook_secret`, mapped from Infisical `/stripe/STRIPE_PLATFORM_SECRET_KEY` and `/stripe/STRIPE_WEBHOOK_SECRET`, plus `Payments:OrganizerDirect:ProviderCode` and `ConnectPlatformId`. Set `PublicBaseUrl` to the public HTTPS base URL, including the normalized application subpath when deployed below the origin root. Keep `Payments__Stripe__AllowedCheckoutHosts__0=checkout.stripe.com` unless Stripe documents another exact host for the deployment, and choose `Payments__Stripe__Mode=Test` or `Live` with matching secrets and webhook endpoint mode.

Phase 18 implements durable Stripe direct-charge Checkout, signed Connect payment intake, authoritative reconciliation, and paid-order finalization. Instance policy sets the ceiling; tenant policy may only narrow it. Paid publication requires current server preflight, disclosures, configured commerce, and a ready connection for the event's exact organizer actor, never an administrator fallback merchant. Split API/BFF deployments must keep Redis available for one-time Checkout tickets; standalone keeps the bounded ticket state in-process and leaves Redis optional. The `payment-reconciliation-drain` Quartz job must remain enabled for Checkout dispatch and asynchronous provider truth. Disabling new paid-event policy or withholding Stripe configuration leaves free events unaffected. Refunds, disputes, admission, QR/check-in, transfers, payouts, and legal/tax/invoice support remain out of scope. `ProtectedDelayedPayout` is not escrow and remains unavailable without its separate approvals.

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
