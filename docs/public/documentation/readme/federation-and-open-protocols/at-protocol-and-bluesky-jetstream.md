---
description: Operate linked-user OAuth, governed publication, exact-collection ingestion, and tenant-gated discovery.
---

# AT Protocol & Bluesky Jetstream

ISLAMU Event implements a selective **AT Protocol (Bluesky)** federation integration. It is designed specifically for cross-platform event syndication and calendar interoperability, rather than operating as a general-purpose social network host.

---

## 1. Linked-User Authentication & Publication

* **OAuth Account Linking**: Authenticated users can link their Bluesky Decentralized Identifier (DID) to an existing local account (see [Authentication Architecture](../security-and-identity/authentication.md#linked-at-protocol-sign-in)).
* **Outbox-Backed Publication**: Outbound event publication is queued strictly via the transactional outbox after the primary database commit succeeds (see [Architecture & Request Flows](../getting-started/architecture-and-request-flows.md#2-write-command-flow)).
* **Supported Records**:
  * Calendar Events: Published to the user's repository under `community.lexicon.calendar.event`.
  * Attendance Intent: Published under `community.lexicon.calendar.rsvp` with status `#going`.

---

## 2. CarpaNet Jetstream Ingestion & Cursor Settlement

External community events are ingested via the Bluesky Jetstream firehose:
* **Exact Collections**: Subscriptions ingest only recognized [Lexicons](lexicons.md):
  * `community.lexicon.calendar.event`
  * `community.lexicon.calendar.rsvp`
* **Atomic Cursor Settlement**: Inbound event records and the stream playback cursor commit atomically in a single transaction. A cursor never advances without durable local record creation, preventing dropped events during worker restarts.
* **No Echo Loops**: Ingested records are never re-broadcast to the outbound outbox.

---

## 3. Multi-Tenant Governance

Ingested external events are subject to tenant federation policies (see [Multi-Tenancy](../security-and-identity/multi-tenancy.md)):
* Tenant administrators control whether external federated events appear in local community search listings.
* Federated records display clear attribution links back to their origin Bluesky post.

---

## Related Guides & Next Steps

* **[Lexicons Reference](lexicons.md)** — Review JSON schemas and field definitions for calendar records.
* **[Authentication Architecture](../security-and-identity/authentication.md#linked-at-protocol-sign-in)** — Connect AT Protocol DIDs with Keycloak accounts.
* **[Multi-Tenancy Architecture](../security-and-identity/multi-tenancy.md)** — Tenant boundaries and discovery policies.
* **[Architecture & Request Flows](../getting-started/architecture-and-request-flows.md)** — Transactional outbox pattern for event delivery.
