ABOUTME: Integration callback guide for incoming provider webhooks and moderation connectors.
ABOUTME: Separates incoming signed callbacks from outgoing webhook delivery provider configuration.

# Integrations

> **Audience:** Operators | Integrators | Contributors | AI agents
> **Status:** Implemented
> **Owner:** Platform/Ops
> **Last Verified:** 2026-07-04
> **Source Anchors:** `Explore.API/Controllers/IncomingWebhooksController.cs`, `Explore.API/Controllers/ModerationIntegrationController.cs`, `Explore.API/Services/IncomingWebhookIntakeService.cs`, `Explore.API/Services/CoopWebhookSignatureValidator.cs`, `Explore.Infrastructure/Services/Moderation/`, `docs/API.md`, `docs/SECURITY-MODEL.md`

Incoming integration webhooks are provider callbacks received by ISLAMU Event. They are not controlled by the outgoing webhook provider mode.

```text
Provider callback
  -> raw body read
  -> provider signature/API-key verification
  -> incoming_webhook_messages idempotency row
  -> Application command or outbox-backed side effect
```

Outgoing product webhooks use `Local`, `Svix`, `Composite`, `DryRun`, or `Disabled`; see [WEBHOOKS.md](WEBHOOKS.md).

## Incoming Routes

| Route | Provider | Authentication | Behavior |
|---|---|---|---|
| `POST /api/integrations/moderation/coop/callback` | Coop-compatible review queue | API key policy plus timestamped HMAC-SHA256 over raw body | Atomically retains the verified callback and one unique effect pointer. A fenced worker revalidates the retained payload and invokes canonical decision execution; command success is required before receipt/pointer completion. |
| `POST /api/integrations/moderation/osprey/callback` | Osprey-compatible signal worker | API key policy | Records bounded moderation signals on the local report; it does not directly execute moderation actions. |
| `POST /api/integrations/svix/operational` | Svix operational webhook | `[AllowAnonymous]` at HTTP edge plus Svix-compatible signature verification | Verifies operational callbacks with `svix-id`, `svix-timestamp`, and `svix-signature`; tenant-addressed payloads are captured in the incoming ledger. |

These routes continue to work when outgoing webhooks are `Disabled`, `Local`, `Svix`, `Composite`, or `DryRun`.

Coop effect inspection and generation-checked dead-letter redrive are authenticated administrative operations under `/api/admin/incoming-webhook-effects`. Clients must use server-authored HAL affordances and must not receive callback bytes, hashes, signed provider decision IDs, or raw provider errors.

## Verification Rules

- Read the raw request body before JSON parsing.
- Verify signatures before processing side effects.
- Enforce configured max body sizes.
- Use provider message IDs as replay/idempotency keys.
- Acknowledge duplicate verified callbacks without replaying side effects.
- Persist only bounded intake metadata: tenant when present, provider, provider message ID, idempotency key, event type, payload hash, redacted headers, status, and failure category.
- Route side effects through Application commands or outbox-backed services; provider callbacks must not directly mutate sensitive aggregates.

ProblemDetails, logs, metrics, traces, screenshots, and issue templates must not include raw bodies, signature headers, authorization headers, provider secrets, API keys, bearer tokens, tenant/user identifiers, provider message IDs, or raw provider exceptions.

## Coop Flow

Coop integration is independent of outgoing product webhooks:

```text
User reports event
  -> local report, case, and provider-sync intent are stored
  -> optional Coop review-queue mirror runs from provider settings
  -> Coop sends signed callback
  -> ISLAMU verifies API key and HMAC signature
  -> incoming_webhook_messages records idempotency
  -> local report decision is created
  -> light moderation or heavy redaction command executes through existing local paths
```

The Coop mirror request is metadata-first. It excludes reporter text, reporter IP/User-Agent hashes, event titles, slugs, URLs, raw provider payloads, and provider secrets.

## Svix Operational Callback

Svix operational callbacks use Svix-compatible verification. Configure:

| Key | Purpose |
|---|---|
| `Webhooks:Svix:OperationalWebhookSecretRef` | Secret binding key used by the verifier. |
| `WEBHOOKS_SVIX_OPERATIONAL_WEBHOOK_SECRET` | Local/deployment environment value used by the development seeder when configured. |

This callback does not require `Webhooks:Provider=Svix`; it is an incoming integration endpoint, not the outgoing SvixProvider.

## Related

- [WEBHOOKS.md](WEBHOOKS.md)
- [API.md](API.md#incoming-integration-webhooks)
- [CONFIGURATION.md](CONFIGURATION.md)
- [SECURITY-MODEL.md](SECURITY-MODEL.md)
