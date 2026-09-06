---
description: Operate linked-user OAuth, governed publication, exact-collection ingestion, and tenant-gated discovery.
---
<!-- ABOUTME: Guides operators through AT Protocol login, publication and governed event ingestion. -->
<!-- ABOUTME: Explains database-backed login state, browser binding and key-persistence recovery requirements. -->

# AT Protocol & Bluesky Jetstream

ISLAMU Event implements a selective **AT Protocol (Bluesky)** federation integration. It is designed specifically for cross-platform event syndication and calendar interoperability, rather than operating as a general-purpose social network host.

---

## 1. Linked-User Authentication & Publication

* **OAuth Account Linking**: Authenticated users can link their Bluesky Decentralized Identifier (DID) to an existing local account (see [Authentication Architecture](../security-and-identity/authentication.md#linked-at-protocol-sign-in)).
* **Outbox-Backed Publication**: Outbound event publication is queued strictly via the transactional outbox after the primary database commit succeeds (see [Architecture & Request Flows](../getting-started/architecture-and-request-flows.md#2-write-command-flow)).
* **Supported Records**:
  * Calendar Events: Published to the user's repository under `community.lexicon.calendar.event`.
  * Attendance Intent: Published under `community.lexicon.calendar.rsvp` with status `#going`.

### Login state and browser binding

OAuth login state and one-time cross-domain handoffs use the primary database, not Redis or process-local memory. The BFF accesses them through a private authenticated API; no database credentials belong in the BFF. Existing durable OAuth sessions remain separate.

Use HTTPS for the public instance and every configured tenant login origin, including local development. Keep the same browser throughout login. A protected host-only proof cookie lasts fifteen minutes and supports parallel login attempts without being rewritten. State lasts at most ten minutes (or the shorter configured SDK lifetime), reserving two minutes for handoff. A handoff lasts at most two minutes and never outlives browser proof. Near proof expiry, wait for the returned `Retry-After` before starting another attempt; do not clear cookies underneath other pending logins.

For a custom-domain login, the canonical callback redirects an opaque code back to the initiating domain without signing the browser in on the canonical host. Opening that code in another browser cannot sign it in or consume the legitimate destination handoff.

If login state or a handoff expires while the API response is in transit, sign-in is rejected even if the browser proof is still valid. Start a fresh login; a successfully consumed code cannot be recovered or retried.

For Redis-free single-node hosting, persist the existing native BFF Data Protection key directory. Replicas need the same persistent key directory and application discriminator, with restricted permissions and encryption at rest. An explicitly configured cache connection still selects Redis for those protection keys; remove that selection when choosing a completely Redis-free deployment. Keep the OAuth signing key ring available across replicas and retain keys needed by outstanding flows.

Restart in-flight logins after upgrading from the old transient backend or losing their cookies/keys. The memory-store and configurable handoff-lifetime options are removed, without compatibility aliases. After an API/database outage or a lost consume response, restore dependencies and begin a new login rather than retrying the old callback. The current AT Protocol readiness check is passive; it does not yet prove an operational store round trip.

### Cleanup and retention

Keep the `atproto-transient-cleanup` scheduler job running even after disabling AT Protocol login. With the global scheduler enabled, it runs every minute without overlapping itself. Each pass deletes at most 500 rows per batch and five batches from each of the transient and assertion-replay tables: at most ten delete calls and 5,000 rows total. It captures one time, stops early when a batch is short, and adds no 24-hour grace period. There is no separate cleanup configuration switch.

Expired material is denied even when cleanup is delayed. Assertion replay claims remain through their complete acceptance window, including five seconds of clock skew. Cleanup deletes active database rows, not older backups; apply your backup retention and access-control policy separately. Preserve the shared protection keys needed for valid sessions.

Keep every BFF and API host clock synchronized within five seconds of trusted UTC and monitor that bound. Two hosts can then differ by at most ten seconds; cleanup retains replay claims for ten additional seconds so a faster host cannot reopen an assertion still accepted by a slower one. This does not extend assertion validity or OAuth-state/handoff lifetimes. If a host drifts outside the bound, restore clock synchronization before returning it to authentication traffic.

Monitor `explore.atproto.transient.cleanup_runs` and `explore.atproto.transient.cleanup_rows`. Their success/failure and store labels are fixed; row totals describe completed passes, not partial work from a failed pass. A lost delete acknowledgement stops the pass rather than retrying another batch; subsequent scheduled passes resume. Never add tenant/user identifiers, locators, assertions, payloads or key material to labels or support reports.

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
