# Deployment Modes & Customization

> **Project-Agnostic Operations & Deployment Guide**
>
> Placeholders use `{Placeholder}` syntax - see [TEMPLATE_GLOSSARY.md](TEMPLATE_GLOSSARY.md).

## Placeholder Substitutions

| Placeholder | Replace With | Example (ISLAMU Event) |
|-------------|--------------|------------------------|
| `{Project}` | Your solution name | `Explore` |
| `{Project}.Infrastructure` | Infrastructure project | `Explore.Infrastructure` |
| `{Instance Name}` | Your instance display name | `ISLAMU Event` |

---

This platform is designed to be **highly customizable** to support diverse deployment scenarios—from single-organization instances to full SaaS platforms serving multiple tenants. This section covers all customization options.

### Implementation Example: ISLAMU Event
The ISLAMU Event platform (project name: Explore) is the reference implementation of this system, designed for Islamic event discovery globally.

## Deployment Modes

The platform supports two primary deployment modes:

```
┌─────────────────────────────────────────────────────────────────────┐
│                      DEPLOYMENT MODES                               │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  MODE 1: Single-Instance Deployment                                 │
│  ─────────────────────────────────────                              │
│  • Multi-tenancy DISABLED                                           │
│  • One organization/community per deployment                        │
│  • Simpler configuration and maintenance                            │
│  • Example: ISLAMU's own Islamic events instance                    │
│  • Example: A university running their own event platform           │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │              Single Instance                                │    │
│  │  ┌─────────────────────────────────────────────────────┐    │    │
│  │  │  events.islamu.org                                  │    │    │
│  │  │  • All events in one space                          │    │    │
│  │  │  • Single admin team                                │    │    │
│  │  │  • Unified branding                                 │    │    │
│  │  └─────────────────────────────────────────────────────┘    │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                                                                     │
│  MODE 2: Multi-Tenant SaaS Deployment                               │
│  ────────────────────────────────────                               │
│  • Multi-tenancy ENABLED                                            │
│  • Multiple isolated organizations/communities                      │
│  • For SaaS providers offering ISLAMU Event as a service            │
│  • Each tenant has custom domain, branding, settings                │
│  • Shared infrastructure, isolated data                             │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │              Multi-Tenant SaaS Platform                     │    │
│  │  ┌───────────────┐ ┌───────────────┐ ┌───────────────┐      │    │
│  │  │ Tenant A      │ │ Tenant B      │ │ Tenant C      │      │    │
│  │  │ mosque-a.com  │ │ uni-events.eu │ │ community.org │      │    │
│  │  │ Own settings  │ │ Own settings  │ │ Own settings  │      │    │
│  │  │ Own branding  │ │ Own branding  │ │ Own branding  │      │    │
│  │  └───────────────┘ └───────────────┘ └───────────────┘      │    │
│  │                    Shared Infrastructure                    │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Environment Configuration for Deployment Mode

```yaml
# docker-compose.yml
services:
  explore-api:
    environment:
      # Deployment mode is resolved from SystemSettings at runtime
```

## Blazor Rendering Modes

The platform supports **three Blazor rendering modes** via runtime render-policy governance settings:

| Mode | Description | Use Case |
|------|-------------|----------|
| **InteractiveServer** | Server-side rendering | real-time updates |
| **InteractiveWebAssembly** | Client-side rendering in browser | Offline capability, reduced server load |
| **InteractiveAuto** | Server initially, then WebAssembly | Best of both worlds (recommended) |

### Configuration

Render policy is controlled by SystemSettings (`routing.render_policy.*`) and resolved at runtime by the Blazor client. See [RENDER_POLICIES.md](RENDER_POLICIES.md) for presets and route-group overrides.

## Observability Pipeline (Aspire + Serilog + OpenTelemetry + Loki)

The recommended production flow is:

```
App logs -> Microsoft.Extensions.Logging -> OpenTelemetry provider -> OTLP -> OpenTelemetry Collector -> Loki
```

### Required Rules

- Use Serilog for structured JSON logs and enrichers.
- Keep `UseSerilog(..., writeToProviders: true)` in app hosts so logs continue through the provider pipeline.
- Configure OTLP once via Aspire ServiceDefaults using `builder.Services.AddOpenTelemetry().UseOtlpExporter()`.
- Do not use `Serilog.Sinks.Loki` for this architecture.

### Critical OpenTelemetry Constraint

Do not mix these two patterns in the same app:

- Cross-cutting exporter: `UseOtlpExporter()`
- Signal-specific exporters: `AddOtlpExporter()` (logs, metrics, or traces)

Mixing both causes a runtime `NotSupportedException` in OpenTelemetry .NET.

### Environment Variables

```yaml
services:
  explore-api:
    environment:
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4318
      - OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf

  explore-blazor:
    environment:
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4318
      - OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
```

### Loki/Collector Notes

- Send logs to Loki through OpenTelemetry Collector OTLP pipelines.
- Ensure Loki OTLP ingestion is enabled with structured metadata support (`allow_structured_metadata: true`).
- Keep Prometheus scraping enabled on `/metrics` for metric collection.

## Instance-Level Administration

Instance Administrators (not organization admins) can configure platform-wide behavior through the **Instance Settings** panel or environment variables.

### Organization & Event Publishing Policies

Configurable inside the **Instance Settings** panel in the webapp for instance administrator.

```
┌─────────────────────────────────────────────────────────────────────┐
│              INSTANCE-LEVEL POLICY CONFIGURATION                    │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ORGANIZATION CREATION POLICY                                       │
│  ────────────────────────────                                       │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  Option A: Open Registration (Default)                       │   │
│  │  • Anyone can create an organization                         │   │
│  │  • Organizations start as "Unverified"                       │   │
│  │  • Can publish events immediately                            │   │
│  │                                                              │   │
│  │  Option B: Approval Required (ISLAMU Instance)               │   │
│  │  • Anyone can REQUEST to create an organization              │   │
│  │  • Instance admin must APPROVE before org is active          │   │
│  │  • Only approved orgs can publish events                     │   │
│  │                                                              │   │
│  │  Option C: Invite Only                                       │   │
│  │  • Only instance admins can create organizations             │   │
│  │  • Most restrictive, for curated platforms                   │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  USER EVENT PUBLISHING POLICY                                       │
│  ────────────────────────────                                       │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  Option A: Users Can Publish (marked as "User Reported")     │   │
│  │  • Individual users can post events                          │   │
│  │  • Events flagged with "User Reported" badge                 │   │
│  │  • Subject to community moderation                           │   │
│  │                                                              │   │
│  │  Option B: Users Cannot Publish Directly                     │   │
│  │  • Only organizations can publish events                     │   │
│  │  • Higher quality control                                    │   │
│  │                                                              │   │
│  │  Option C: Users Publish with Approval                       │   │
│  │  • Users submit events for review                            │   │
│  │  • Instance moderators approve before publishing             │   │
│  │  • Balance between openness and quality                      │   │
│  │                                                              │   │
│  │  Option D: Users Publish without approval nor verification   │   │
│  │  • Users submit events                                       │   │
│  │  • fully open                                                │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```


## Cerbos Authorization Service

Cerbos is the Policy Decision Point (PDP) for fine-grained authorization. It runs as an opt-in Docker service.

### Deployment

```bash
# Start Cerbos alongside the platform
docker compose --profile authz up -d

# Verify health
curl http://localhost:3592/_cerbos/health
```

### Schema Initialization

For Tier 2+ deployments using PostgreSQL-backed policy storage:

```bash
# Initialize the Cerbos schema in a dedicated database or schema
psql -h localhost -U cerbos_user -d cerbos -f cerbos/init/cerbos-schema.sql
```

The schema creates 5 tables (`policy`, `policy_dependency`, `policy_ancestor`, `policy_revision`, `attr_schema_defs`) plus an audit trigger that tracks all policy changes.

### Configuration

Cerbos is configured via `cerbos/config/.cerbos.yaml`:

| Setting | Default | Description |
|---------|---------|-------------|
| `server.httpListenAddr` | `:3592` | HTTP API port |
| `server.grpcListenAddr` | `:3593` | gRPC port |
| `server.adminAPI.enabled` | `true` | Admin API for PolicySyncService |
| `storage.driver` | `overlay` | PG primary + disk fallback |
| `compile.cacheDuration` | `60s` | Policy compilation cache |

Environment variables:
- `CERBOS_ADMIN_USER` — Admin API username
- `CERBOS_ADMIN_PASSWORD_HASH` — Bcrypt hash of admin password
- `CERBOS_PG_URL` — PostgreSQL connection string for policy store

### Policy Management

**Base policies** (18 resource kinds) are shipped as YAML files in `cerbos/policies/` and mounted into the container. These define the default authorization rules for each resource type.

**Dynamic policies** are generated by `PolicySyncService` when role permissions change (e.g., admin creates a custom role). These are pushed to Cerbos via the Admin API and stored in PostgreSQL.

The overlay driver ensures: PostgreSQL policies take precedence → disk policies as fallback.

### Health Monitoring

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/_cerbos/health` | GET | Returns 200 if Cerbos is healthy |
| `/_cerbos/metrics` | GET | Prometheus metrics (if enabled) |

The `RuntimeAuthorizationProvider` has a Polly circuit breaker (trips after 50% failure rate over 30s, breaks for 15s). When tripped, all authorization falls back to `LocalAuthorizationProvider`.

### Backup & Restore

**Policy store backup:**
```bash
pg_dump -h localhost -U cerbos_user -d cerbos --schema=cerbos > cerbos-backup.sql
```

**Restore:**
```bash
psql -h localhost -U cerbos_user -d cerbos < cerbos-backup.sql
```

Base YAML policies are version-controlled in `cerbos/policies/` — no separate backup needed.

### Upgrade Procedure

1. Pull new Cerbos image: `docker pull ghcr.io/cerbos/cerbos:latest`
2. Rolling restart (Tier 2+): restart one instance at a time
3. Verify health after each restart: `curl http://<instance>:3592/_cerbos/health`
4. Run policy compilation check: `cerbos compile cerbos/policies/`

### Troubleshooting

| Symptom | Cause | Resolution |
|---------|-------|------------|
| `Connection refused` on port 3592 | Cerbos not running | `docker compose --profile authz up -d` |
| Policy compile errors in CI | Invalid YAML syntax | Run `cerbos compile cerbos/policies/` locally |
| Circuit breaker tripped (logs: "Cerbos circuit open") | Cerbos unreachable or slow | Check Cerbos container logs, restart if needed |
| `authorization.provider = local` but Cerbos running | SystemSetting override | Update SystemSetting to `cerbos` via admin panel |
| PolicySyncService 401 Unauthorized | Wrong admin credentials | Verify `CERBOS_ADMIN_USER` and `CERBOS_ADMIN_PASSWORD_HASH` env vars |

For deployment tier guidance, see [DEPLOYMENT_TIERS.md](DEPLOYMENT_TIERS.md).

---

## Tenant-Level Configuration (BYOK - Bring Your Own Keys)

Tenant-level BYOK integrations are a **roadmap** capability.

**Current state**:

- Object storage integration exists (S3-compatible) via `{Project}.Infrastructure`.
- Other per-tenant integrations (analytics, payments, AI services, email/SMS routing) are not implemented yet.

### Implementation Example: ISLAMU Event
In the Explore project, object storage is implemented in `Explore.Infrastructure` with S3-compatible providers.

```
┌─────────────────────────────────────────────────────────────────────┐
│               TENANT BYOK INTEGRATIONS (PLANNED)                    │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ANALYTICS (planned)                 PAYMENTS (planned)              │
│  ─────────                          ────────                        │
│  • Google Analytics                 • Stripe                        │
│  • Plausible Analytics                                              │
│  • PostHog                                                          │
│                                                                     │
│                                                                     │
│                                                                     │
│                                                                     │
│  AI SERVICES (planned)                                               │
│  ───────────                                                        │
│  • OpenAI                                                           │
│  • Anthropic Claude                                                 │
│  • Azure OpenAI                                                     │
│  • Ollama (self-hosted)                                             │
│  • Custom LLM endpoint                                              │
│                                                                     │
│  EMAIL & SMS (planned)               STORAGE                          │
│  ───────────                        ───────                         │
│  • SendGrid                         • AWS S3                        │
│  • Mailgun                          • Azure Blob                    │
│  • Amazon SES                       • Google Cloud Storage          │
│  • Twilio (SMS)                     • MinIO (self-hosted)           │
│  • Custom SMTP                      • Local filesystem              │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```



## Organization-Level Integrations

Organization administrators can configure their own notification channels and webhooks for their organization's events.

```
┌─────────────────────────────────────────────────────────────────────┐
│              ORGANIZATION INTEGRATION OPTIONS                       │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  NOTIFICATION CHANNELS                                              │
│  ─────────────────────                                              │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  Slack Integration                                           │   │
│  │  • New event created → Post to channel                       │   │
│  │  • New registration → Notify organizers                      │   │
│  │  • Event reminder → Scheduled messages                       │   │
│  │  • Capacity alert → Warning when nearly full                 │   │
│  │                                                              │   │
│  │  Telegram Integration                                        │   │
│  │  • Bot notifications to group/channel                        │   │
│  │  • Event announcements                                       │   │
│  │  • Registration confirmations                                │   │
│  │                                                              │   │
│  │  Email Notifications                                         │   │
│  │  • Customizable templates                                    │   │
│  │  • Digest options (immediate/daily/weekly)                   │   │
│  │  • Role-based routing                                        │   │
│  │                                                              │   │
│  │  Discord Integration                                         │   │
│  │  • Webhook-based notifications                               │   │
│  │  • Rich embeds for events                                    │   │
│  │                                                              │   │
│  │  Matrix Integration                                          │   │
│  │  • Room notifications                                        │   │
│  │  • Decentralized messaging                                   │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  WEBHOOKS                                                           │
│  ────────                                                           │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Configurable webhooks for all event types:                  │   │
│  │                                                              │   │
│  │  Event Lifecycle:                                            │   │
│  │  • event.created        • event.published                    │   │
│  │  • event.updated        • event.cancelled                    │   │
│  │  • event.deleted        • event.started                      │   │
│  │  • event.ended          • event.reminder (configurable)      │   │
│  │                                                              │   │
│  │  Participation:                                              │   │
│  │  • participant.registered    • participant.cancelled         │   │
│  │  • participant.checked_in    • participant.waitlisted        │   │
│  │  • capacity.threshold_reached                                │   │
│  │                                                              │   │
│  │  Organization:                                               │   │
│  │  • organization.member_joined   • organization.member_left   │   │
│  │  • organization.verified        • organization.settings_changed│  │
│  │                                                              │   │
│  │  Moderation:                                                 │   │
│  │  • report.created       • report.resolved                    │   │
│  │  • comment.flagged      • content.removed                    │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```
