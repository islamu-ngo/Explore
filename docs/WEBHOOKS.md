ABOUTME: Operator and integrator guide for outgoing ISLAMU Event webhooks.
ABOUTME: Covers LocalProvider, SvixProvider, signatures, security, configuration, and rollout behavior.

# Webhooks

> **Audience:** Operators | Integrators | Contributors | AI agents
> **Status:** Implemented
> **Owner:** Platform/Ops
> **Last Verified:** 2026-07-14
> **Source Anchors:** `Explore.Application/Webhooks/`, `Explore.Infrastructure/Webhooks/`, `Explore.Infrastructure/HealthChecks/`, `Explore.API/Controllers/WebhookController.cs`, `Explore.API/Controllers/IncomingWebhooksController.cs`, `Explore.AppHost/AppHost.cs`, `docker-compose.yml`, `.env.example`, `docs/API.md`, `docs/SECURITY-MODEL.md`

Webhooks in this document are outgoing product notifications sent by ISLAMU Event to external systems. Incoming provider callbacks are separate API ingestion routes; see [INTEGRATIONS.md](INTEGRATIONS.md).

## Delivery Model

Outgoing delivery is provider-based:

```text
domain/application event
  -> WebhookEventEnvelope
  -> webhook_messages canonical ledger row
  -> IWebhookDeliveryProvider
  -> Local, Svix, Composite, DryRun, or Disabled
```

ISLAMU owns the canonical event catalog and `webhook_messages` ledger even when Svix performs final delivery. That keeps audit, provider switching, payload retention, and local fallback under the application boundary.

## Provider Modes

| Mode | Behavior | Use |
|---|---|---|
| `Disabled` | Creates no outgoing delivery work. Incoming callbacks still work. | Minimal installs. |
| `Local` | Built-in endpoint CRUD, subscription filtering, signed POST, retry attempts, delivery logs, manual retry, and safety checks. | Self-hosters and simple integrations. |
| `Svix` | API publishes canonical messages to Svix; Svix owns endpoint fanout, delivery history, retries, and App Portal management. | Larger deployments and advanced webhook operations. |
| `Composite` | Uses canonical local audit plus Svix delivery path. | Advanced installs that need local visibility and Svix delivery. |
| `DryRun` | Creates canonical messages without outbound delivery. | Development and test validation. |

LocalProvider is intentionally not a Svix clone. It does not include transformations, OAuth or mTLS endpoint auth, a customer-facing advanced portal, FIFO endpoints, polling endpoints, or advanced analytics.

## Event Catalog And Payloads

`GET /api/webhooks/event-types` exposes the canonical event catalog with names, groups, schema versions, JSON Schema, and example envelopes. Initial public event types include event lifecycle, registration, report, moderation, and organization verification events.

Payloads use a stable envelope:

```json
{
  "id": "018f0000-0000-7000-8000-000000000001",
  "type": "event.published",
  "version": 1,
  "occurredAt": "2026-07-04T10:00:00Z",
  "tenantId": "018e4e5c-7f00-7000-8000-000000000001",
  "data": {}
}
```

Sensitive moderation payloads are minimized. Heavy-redaction events must not include unsafe event title, slug, URL, image URI, storage path/key, organizer identity, provider endpoint, raw provider error, or free-form unsafe content.

## LocalProvider Delivery

LocalProvider endpoints are managed through `/api/webhooks/endpoints`. Endpoint subscriptions filter by event type. Deliveries are HTTPS/HTTP POST requests with:

- `svix-id`
- `svix-timestamp`
- `svix-signature`

The signed content is:

```text
{svix-id}.{svix-timestamp}.{raw-body}
```

The signature is Svix-compatible HMAC-SHA256 over the raw body using `whsec_` secret material, so consumers can verify LocalProvider deliveries with Svix-compatible libraries. Verification must use fixed-time comparison and timestamp tolerance.

Retry schedule:

| Attempt | Delay |
|---:|---|
| 1 | immediately |
| 2 | 30 seconds |
| 3 | 5 minutes |
| 4 | 30 minutes |
| 5 | 2 hours |
| 6 | 6 hours |
| 7 | 12 hours |
| 8 | 24 hours |

`2xx` responses are success. Non-`2xx`, redirects, timeouts, and network failures are delivery failures. Attempts store bounded status, duration, HTTP status, safe failure category, and optional response preview only.

## LocalProvider Security Defaults

LocalProvider treats endpoint URLs as untrusted egress targets.

- Private, loopback, link-local, localhost, metadata, and internal DNS destinations are blocked by default.
- Redirects are disabled.
- Request and connect timeouts are bounded by `Webhooks:Local:*`.
- Response previews are bounded and never include full bodies by default.
- Logs, metrics, health checks, and ProblemDetails must not include payload JSON, endpoint secrets, full endpoint query strings, authorization headers, full responses, or raw exception text.

Operators can opt into specific private CIDRs with `Webhooks:Local:AllowedPrivateCidrs`, but this should stay empty for internet-facing SaaS.

## SvixProvider

SvixProvider uses the official Svix C# SDK. For self-hosted Svix, the SDK is configured with the deployment base URL through `SvixOptions`, while the API token stays server-side behind the ISLAMU secret resolver. Provider selection is guarded by an exact deployment-kind/environment/provider-version/capability-policy tuple backed by executed conformance evidence. Unknown and zero-evidence tuples fail startup validation and readiness before any secret is resolved.

Mapping:

| ISLAMU | Svix |
|---|---|
| webhook consumer / tenant | application UID |
| event type name | event type |
| webhook message id | event ID and idempotency key |
| payload JSON | message payload |
| payload retention | message retention period |

`POST /api/webhooks/svix/app-portal` creates a backend-generated, short-lived App Portal access URL. Blazor receives only the short-lived URL/token metadata; the Svix API token is never exposed to the browser.

## Local-Full Svix

`local-full` starts pinned `svix/svix-server:v1.96.1` through Aspire with PostgreSQL and Redis queue/cache. Both `SVIX_QUEUE_TYPE` and `SVIX_CACHE_TYPE` are `redis`: the queue carries provider work, while the shared cache preserves idempotency semantics across Svix replicas. Do not use the self-hosted in-memory cache in a multi-replica deployment. Aspire injects the current Svix HTTP endpoint and the proven `self-hosted`/`1.96.1`/`svix-self-hosted-1.96.1-v1` tuple into the API; use `aspire describe --format Json` to inspect the actual host port for the running session.

The live conformance matrix proves the twelve-hour idempotency behavior, duplicate event identity conflicts, response-loss ambiguity, credential rotation, and list/get consistency. Self-hosted v1.96.1 does not return message tags from list/get, so request-hash exact lookup is not enabled for that profile. An accepted request followed by response loss therefore remains manual reconciliation rather than being guessed or blindly retried.

## Self-Hosted Svix Authentication

The supported Svix profile is self-hosted only. Managed Svix SaaS has no selectable
conformance profile and is not a release gate. The managed conformance token fields in
`.env` and `.env.example` remain intentionally empty; neither Infisical nor the Aspire
container supplies cloud API keys.

Self-hosted authentication uses a JWT signed with the server's `SVIX_JWT_SECRET`.
The server does not publish a reusable client token during startup. Its bundled CLI can
generate an organization-scoped bearer token with `svix-server jwt generate`; when the
server runs in a container, execute that command inside the running container and place
the resulting token in `WEBHOOKS_SVIX_AUTH_TOKEN`. The token is credential material and
must not be written to logs, test reports, screenshots, or committed operator evidence.

Aspire cannot treat command output from an already-running container as an environment
input for the API resource. The local profile therefore reads a pre-generated matching
development JWT and signing secret from `.env`; `.env.example` carries the same safe
development pair. If either value is rotated, generate a new token from the container
configured with the new signing secret, update both values together, and restart the API.

The local AppHost supplies:

- `SVIX_JWT_SECRET=local-dev-svix-jwt-secret-change-me` to the Svix container.
- A matching development JWT in `WEBHOOKS_SVIX_AUTH_TOKEN` for the API.
- `WEBHOOKS_SVIX_OPERATIONAL_WEBHOOK_SECRET` for local Svix operational callback verification.
- `Webhooks:Svix:AuthTokenSecretRef=webhooks.svix.auth_token`.
- `Webhooks:Svix:OperationalWebhookSecretRef=webhooks.svix.operational_webhook_secret`.

Development database seeding creates missing instance-scoped secret bindings for those environment variables when values are configured. Rotate these values outside local development.

Docker Compose starts Svix only when the `webhooks` profile is enabled. The same canonical secret refs are documented in `.env.example`.

## Configuration

| Key | Default | Notes |
|---|---|---|
| `Webhooks:Enabled` | `true` | Master switch for outgoing product webhooks. |
| `Webhooks:Provider` | `Local` | `Disabled`, `Local`, `Svix`, `Composite`, or `DryRun`. |
| `Webhooks:AllowTenantOverride` | `true` | Allows tenant-level provider posture where supported. |
| `Webhooks:DefaultPayloadRetentionDays` | `14` | Default canonical payload retention window. |
| `Webhooks:Local:MaxAttempts` | `8` | Local retry ceiling. |
| `Webhooks:Local:TimeoutSeconds` | `15` | Total LocalProvider request timeout. |
| `Webhooks:Local:ConnectTimeoutSeconds` | `3` | LocalProvider connect timeout. |
| `Webhooks:Local:BlockPrivateNetworks` | `true` | SSRF protection default. |
| `Webhooks:Svix:BaseUrl` | unset | Set for self-hosted Svix, for example `http://localhost:8071` in Aspire or `http://svix:8071` in Compose. |
| `Webhooks:Svix:Environment` | `production` | Exact conformance environment identifier; Compose/Aspire use `self-hosted`. |
| `Webhooks:Svix:ProviderVersion` | `managed-api-v1` | Exact conformance version identifier; Compose/Aspire use pinned `1.96.1`. |
| `Webhooks:Svix:CapabilityPolicyVersion` | `svix-managed-api-v1` | Exact versioned capability policy; Compose/Aspire use `svix-self-hosted-1.96.1-v1`. |
| `Webhooks:Svix:AuthTokenSecretRef` | `webhooks.svix.auth_token` | Server-side Svix API token secret binding. |
| `Webhooks:Svix:OperationalWebhookSecretRef` | `webhooks.svix.operational_webhook_secret` | Secret used to verify incoming Svix operational callbacks. |
| `Webhooks:Svix:AppPortalEnabled` | `true` | Allows backend App Portal URL generation. |
| `Webhooks:Svix:SyncEventTypesOnStartup` | `true` | Syncs canonical event types to Svix on API startup in Svix/Composite mode. |

Environment variables used by local profiles:

| Variable | Purpose |
|---|---|
| `WEBHOOKS_PROVIDER` | Compose-friendly provider mode. |
| `WEBHOOKS_SVIX_BASE_URL` | Compose-friendly Svix base URL. |
| `WEBHOOKS_SVIX_ENVIRONMENT`, `WEBHOOKS_SVIX_PROVIDER_VERSION`, `WEBHOOKS_SVIX_CAPABILITY_POLICY_VERSION` | Exact supported self-hosted conformance tuple. |
| `WEBHOOKS_SVIX_AUTH_TOKEN_SECRET_REF` | Canonical auth token secret ref. |
| `WEBHOOKS_SVIX_OPERATIONAL_WEBHOOK_SECRET_REF` | Canonical operational webhook secret ref. |
| `WEBHOOKS_SVIX_AUTH_TOKEN` | Dev/deployment source value for the Svix API token binding. |
| `WEBHOOKS_SVIX_OPERATIONAL_WEBHOOK_SECRET` | Dev/deployment source value for the Svix operational callback secret binding. |
| `SVIX_TAG`, `SVIX_DB_DSN`, `SVIX_REDIS_DSN`, `SVIX_QUEUE_TYPE`, `SVIX_CACHE_TYPE`, `SVIX_JWT_SECRET` | Pinned Svix server image and container configuration. Queue and cache must both use shared Redis for the supported profile. |

## Health, Metrics, And Retention

Readiness checks:

- `webhook-local-delivery` reports LocalProvider queue backlog, stale sending leases, and processor settings.
- `webhook-svix-provider` reports the safe conformance tuple, evidence revision/count, exact-lookup availability, provider selection, App Portal/event-type-sync flags, and whether server-side Svix secrets resolve. It does not expose tokens, secret refs, or provider URLs and does not perform an outbound network probe.

Business metrics use bounded labels:

- `explore.webhooks.messages_created`
- `explore.webhooks.delivery_attempts`
- `explore.webhooks.delivery_success`
- `explore.webhooks.delivery_failure`
- `explore.webhooks.endpoint_disabled`
- `explore.webhooks.manual_retries`
- `explore.webhooks.provider_publish_failure`

Payload retention is stored per message as `payload_retention_until`. The repository operation `IWebhookMessageRepository.ClearExpiredPayloadsAsync` clears expired payload bodies in bounded batches while preserving audit rows and hashes. Run cleanup only from trusted scheduler/operator code that keeps tenant scoping and safe logging intact.

## Provider Switching

Switches affect new messages only. Existing local attempts and Svix delivery history remain where they were created.

Local to Svix:

1. Configure and health-check Svix.
2. Ensure `webhooks.svix.auth_token` resolves server-side.
3. Sync event types.
4. Create or map Svix applications for consumers.
5. Move endpoints into Svix App Portal or require consumers to reconfigure there.
6. Switch `Webhooks:Provider` to `Svix` or `Composite`.

Svix to Local:

1. Switch `Webhooks:Provider` to `Local`.
2. Recreate LocalProvider endpoints and subscriptions.
3. Keep external Svix delivery history read-only in Svix.
4. Continue using ISLAMU `webhook_messages` as the canonical audit ledger.

Do not promise perfect migration of Svix-only features such as transformations, OAuth, mTLS, endpoint throttling, or portal-only endpoint configuration.

## Related

- [API.md](API.md#webhook-management)
- [INTEGRATIONS.md](INTEGRATIONS.md)
- [CONFIGURATION.md](CONFIGURATION.md)
- [OPERATIONS.md](OPERATIONS.md)
- [SECURITY-MODEL.md](SECURITY-MODEL.md)
- [Svix quickstart](https://docs.svix.com/quickstart)
- [Svix App Portal](https://docs.svix.com/app-portal)
