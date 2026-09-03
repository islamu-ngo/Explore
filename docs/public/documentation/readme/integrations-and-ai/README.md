---
description: Webhooks, MCP, forms, moderation integrations, AI boundaries, and storage.
---

# Integrations & AI

Every external integration maintains an explicit trust boundary, secret authority binding, replay/idempotency contract, readiness probe, and recovery runbook. Enabling a third-party provider never silently widens identity, payment, moderation, or multi-tenant boundaries.

---

## In this Section

* **[Webhooks](webhooks.md)** — Incoming payment/service callbacks and outgoing signed webhook delivery (Local HMAC vs. self-hosted Svix).
* **[Model Context Protocol (MCP)](mcp.md)** — AI agent connectivity via Streamable HTTP, API key scoping, tenant gates, and human-in-the-loop proposal flows.
* **[Google & Microsoft Forms](google-and-microsoft-forms.md)** — Ingest external questionnaires and surveys via Google Pub/Sub and Microsoft Power Automate.
* **[Coop & Osprey Moderation](coop-and-osprey.md)** — Connect external community moderation queues and AI safety evaluation coordinators.
* **[Storage Providers](storage.md)** — Local mounted filesystem vs. S3-compatible cloud object storage (MinIO, Cloudflare R2, AWS S3).

---

## Production Integration Invariant

Always test one valid operation and one rejected or replayed operation for every enabled external provider before opening public access.

---

## Related Guides & Next Steps

* **[Docker Compose Optional Profiles](../self-hosting/docker-compose.md#optional-service-profiles)** — Enable `storage`, `authz`, `webhooks`, and `moderation` profiles.
* **[Environment Variables Reference](../configuration-and-operations/environment-variables.md#11-advanced-outgoing-webhooks-svix-infrastructure)** — Environment dials for Svix, MinIO, Coop, and Osprey.
* **[Secrets Management](../configuration-and-operations/secrets.md)** — Store webhook signing secrets and S3 access keys safely.
* **[Paid Events & Payouts](../events-and-ticketing/paid-events-and-payouts.md)** — Stripe Connect webhook handling and payment finality.
