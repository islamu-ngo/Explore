---
description: >-
  Operate incoming callbacks and outgoing product webhooks as separate trust
  boundaries.
---

# Webhooks

Incoming provider callbacks and outgoing product webhooks are separate systems. They use different authentication, delivery, replay, and recovery contracts. Changing the outgoing mode never enables or disables incoming intake.

## Outgoing modes

| Mode        | Purpose                                                   |
| ----------- | --------------------------------------------------------- |
| `Disabled`  | No outgoing delivery                                      |
| `Local`     | Smallest self-hosted signed-delivery path                 |
| `Svix`      | Self-hosted Svix v1.96.1 with PostgreSQL and shared Redis |
| `Composite` | Explicit multi-provider routing                           |
| `DryRun`    | Evaluate without external delivery                        |

Managed Svix SaaS is not a supported selectable conformance profile.

## Local delivery

Local mode signs Svix-compatible HMAC envelopes, performs eight bounded attempts, and treats redirects, timeouts, and non-2xx responses as failures. Redirects remain disabled. Private, loopback, link-local, and cloud-metadata targets are blocked by default to reduce SSRF risk.

The local dispatcher is intentionally not a complete Svix clone. ISLAMU Event retains the canonical event catalog and delivery ledger. Provider changes apply to new messages and do not rewrite historical authority.

## Incoming callbacks

Incoming routes include payments, registration providers, Coop, Osprey-related processing, and Svix operational callbacks. Each has its own signature or identity check, replay window, idempotency key, correlation, durable intake, and effect application.

Do not treat a browser redirect, arbitrary callback body, or correlation value as identity or terminal business truth unless the specific provider contract makes it authoritative.

## Operations

Use `/health/webhooks/local`, `/health/webhooks/svix`, and bounded `/metrics`. For every endpoint, record owner, authentication authority, event types, idempotency/replay behavior, secret binding, disable path, and dead-letter/recovery procedure. Exercise one valid delivery and one rejected or replayed delivery before production.
