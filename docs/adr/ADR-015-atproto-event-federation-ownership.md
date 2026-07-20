<!-- ABOUTME: Architectural decision record for AT Protocol event federation ownership and ordering. -->
<!-- ABOUTME: Defines canonical ingress, tenant presentation, fenced processing, and DB-first egress. -->

# ADR-015: AT Protocol Event Federation Ownership

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-18 |
| **Deciders** | ISLAMU Event Platform — Architecture, Security, Federation workstreams |
| **Supersedes** | Public direct `AtprotoRecord` mutation semantics |
| **Superseded by** | — |

## Context

AT Protocol federation has two independent data directions with different authorities. Jetstream supplies globally observed community event and RSVP records, while local event and registration lifecycles may publish records into a linked user's PDS. Treating either path as ordinary `AtprotoRecord` CRUD would let callers bypass tenant visibility, consent, lifecycle validation, transaction ordering, and replay protection.

The platform must also operate safely on multiple nodes. A consumer or outbox worker can crash after claiming work or after remote success but before local settlement. Capability, consent, aggregate version, and privacy state may change while work is queued. RSVP publication additionally depends on a settled event URI and CID.

## Decision

### Inbound ownership and presentation

1. Inbound event and RSVP records are global canonical observations identified by DID, collection, and record key. Each row carries the current monotonic source version/cursor; a tenant does not own or duplicate the canonical record.
2. Tenant visibility is represented separately through tenant presentation joins. The effective `federation.atproto_events_enabled` setting controls whether a tenant receives or presents inbound records; it does not create per-tenant copies or sockets.
3. `Atproto:Jetstream:AllowedDids` is the bounded curated ingress allowlist. An empty allowlist is deny-all: the subscriber does not open the stream, and the event source rejects an empty subscription before network access. Tenant governance controls presentation and whether any stream demand exists; it is not a second DID allowlist.
4. Exactly one logical Jetstream consumer owns the global stream. Multi-node hosts coordinate through a renewable lease carrying owner, generation/fence, and expiry. A stale generation cannot advance the cursor or settle materialization.
5. Accepted record materialization, dependent RSVP changes, tombstone effects, quarantine state, and cursor advancement commit atomically. A rejected or malformed envelope can be quarantined with bounded metadata in that same transaction; no path advances the cursor while losing its required record, tombstone, or quarantine effect.
6. Tombstones suppress or remove the canonical version and its dependent RSVP presentation. They never invoke local event lifecycle handlers or create outbound work. Locally owned URI/CID matches reconcile as echoes instead of producing duplicate federated events.

### Outbound ownership and ordering

1. The local event or registration lifecycle is the only outbound publication authority. Public `AtprotoRecord` POST, PUT, and DELETE endpoints, commands, DTOs, generated methods, and HAL affordances do not exist.
2. `federation.atproto_events_enabled` is the single capability for inbound tenant presentation/stream demand and eligible outbound enqueue. Outbound work also requires the owner's self-scoped consent and exact linked encrypted DID/PDS session. The `community_lexicon` profile relaxes only required local business fields; authorization, supplied-value validation, privacy, projection completeness, and record limits remain enforced.
3. Each outbound operation owns explicit tenant ID, platform user ID, owner DID, source entity kind and ID, source aggregate version, operation kind, immutable payload and hash, stable record key, and idempotency/supersession identity.
4. The locally published event transition and its initial immutable outbox operation commit in the same `IUnitOfWork` transaction. No PDS call occurs before that commit. Update, delete, redaction, and RSVP paths cannot synthesize an initial remote event record.
5. Every eligible public event, session, aspect, resolved lookup, EAV, and public-media value has one manifest disposition. Native lexicon fields map directly; every other public value is rendered deterministically into the single event `description`. Missing coverage, privacy failure, invalid record shape, or exact JSON/DAG-CBOR overflow prevents enqueue; nothing is truncated or silently omitted.
6. Aggregate version is authoritative. A newer operation supersedes stale create/update work for the same source. Visibility tightening, heavy redaction, erasure, cancellation, or deletion prevents an older publication payload from being delivered afterward.
7. Outbox claims use renewable owner/generation/expiry leases. Only the current fenced owner may settle. Crashed claims are reclaimable; stale workers cannot complete or overwrite a newer result.
8. Immediately before remote I/O, the worker rechecks the effective master capability, the owner's current self-scoped publication consent, exact session/account link, source version/currentness, visibility, redaction/erasure state, payload, and public-location disclosure. A failed recheck settles or defers the operation according to its safe classification without calling the PDS.
9. A stable DID/collection/record-key identity makes retries idempotent. Remote success is reconciled before settlement so a crash between those steps cannot create a second record.
10. Successful event delivery transactionally settles the local `AtprotoRecord` URI/CID and outbox result. RSVP create/update work remains unclaimable until that event URI and CID exist, and its subject `strongRef` uses those exact settled values. Outbound RSVP represents only a committed active registration as `community.lexicon.calendar.rsvp#going`; organizer approval states, `#interested`, and `#notgoing` are not local user intent.

### Completed remote records

Disabling capability or revoking consent stops pending and future eligible remote I/O after the last-moment gate. It does not pretend that an already completed remote record disappeared and does not silently delete it. Completed records remain auditable and are changed only by an explicit authorized lifecycle or consent policy that produces a newer operation. Local business state remains authoritative when the remote provider is unavailable.

## Transaction boundaries

- Inbound: canonical record or tombstone, quarantine outcome when applicable, tenant-presentation effects, and cursor advance are one database transaction under the current consumer fence.
- Outbound enqueue: local lifecycle mutation and immutable outbox insertion are one database transaction.
- Outbound settlement: URI/CID reconciliation, `AtprotoRecord` linkage, outbox terminal state, and dependent RSVP release are one database transaction under the current worker fence.
- External CarpaNet/PDS calls occur outside database transactions and only after a committed, current claim passes all delivery gates.

## Failure behavior

- Missing capability, consent, linked session, current source version, or public disclosure denies remote I/O.
- Retryable provider failures release work with bounded backoff; permanent validation, ownership, or privacy failures settle with bounded classifications.
- Raw provider bodies, record payloads, credentials, DIDs, record keys, and private event data are excluded from logs, metrics, health responses, HAL, and ProblemDetails.
- Cursor or settlement writes from an expired fence fail closed.

## Alternatives considered

1. Per-tenant Jetstream consumers and record copies — rejected because they duplicate global state, multiply sockets, and create inconsistent tombstone/cursor ownership.
2. Public `AtprotoRecord` CRUD — rejected because it bypasses lifecycle, consent, tenant, privacy, version, and idempotency authorities.
3. Remote-first event creation or PDS calls inside the local transaction — rejected because either permits a PDS-only event or holds database transactions across an external dependency.
4. Best-effort cursor advancement and later materialization — rejected because a crash can permanently lose accepted commits.
5. Automatic deletion of completed records when capability or consent changes — rejected because revocation governs future authority and cannot safely rewrite already completed remote history without an explicit policy action.

## Consequences

- Read-only ATProto discovery remains available through tenant-scoped CQRS queries and HAL navigation.
- Persistence now encodes global inbound identity, typed event materialization, tenant presentation joins, payload-free quarantine, fenced leases, atomic checkpoints, immutable outbound payloads, version/supersession, dependencies, and URI/CID settlement.
- Event publication remains local-first and eventually consistent with the PDS; remote outage never rolls back a committed application event.
- RSVP publication has an explicit event-before-RSVP dependency and cannot fabricate a subject strong reference.
- Capability and consent changes are effective at the last safe boundary before remote I/O.

## Related

- `dev/report/atproto-report.md`
- `dev/active/atproto-auth/atproto-auth-plan.md`
- `dev/active/atproto-auth/atproto-auth-context.md`
- `dev/active/atproto-auth/atproto-auth-tasks.md`
- ADR-002: Outbox Pattern
- ADR-014: ATProto Session Trust Bridge
