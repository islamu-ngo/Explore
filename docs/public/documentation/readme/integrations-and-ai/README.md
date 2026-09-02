---
description: Webhooks, MCP, forms, moderation integrations, AI boundaries, and storage.
---

# Integrations & AI

Every integration has an explicit owner, trust boundary, credential authority, replay/idempotency contract, readiness signal, recovery procedure, and disable path. An external provider never gains broader identity, payment, moderation, or tenant authority by being enabled.

## In this section

* [Webhooks](webhooks.md) — incoming callbacks and outgoing delivery modes, including Local and self-hosted Svix.
* [MCP](mcp.md) — optional stateless Streamable HTTP, API-key scopes, tenant gates, and proposal-first mutations.
* [Google & Microsoft Forms](google-and-microsoft-forms.md) — Workspace Pub/Sub and Microsoft 365 Power Automate contracts.
* [Coop & Osprey](coop-and-osprey.md) — durable moderation intake and advisory signal boundaries.
* [Storage](storage.md) — local/S3-compatible providers, ID-bound access, and recovery.

Exercise one valid operation and one rejected or replayed operation for every enabled provider before production use.
