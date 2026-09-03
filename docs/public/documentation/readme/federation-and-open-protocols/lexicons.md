---
description: Govern vendored AT Protocol schemas, exact identifiers, version changes, and privacy limits.
---

# Lexicons Reference & Governance

In the AT Protocol ecosystem, **Lexicons** are formal, versioned JSON schemas defining RPC methods and repository record formats. ISLAMU Event explicitly vendors and tests each supported lexicon schema, guaranteeing reliable type mapping and strict security boundaries.

---

## Supported Lexicon Collections

The platform limits ingestion and publication strictly to reviewed collections (see [AT Protocol & Bluesky Jetstream](at-protocol-and-bluesky-jetstream.md)):

| Lexicon Identifier | Supported Operations | Local Entity Mapping |
|---|---|---|
| `community.lexicon.calendar.event` | Inbound Ingestion & Outbound Publication | Core Event Aggregate & [Modular Aspects](../events-and-ticketing/modular-event-aspects.md) |
| `community.lexicon.calendar.rsvp` | Inbound Ingestion & Outbound (`#going`) | Event RSVP & Attendance Intent |

> [!IMPORTANT]
> Wildcard schema discovery is permanently disabled. Inbound records matching unvetted collections are silently ignored at the network layer.

---

## Fail-Closed Record Serialization

* If an event title, description, or venue location cannot be serialized without exceeding lexicon size limits or violating character formatting, outbound publication fails closed (the outbox job logs an error and aborts).
* The platform **never** silently truncates text or drops required fields to produce superficially valid records.
* Outbound fields strictly respect attendee privacy ceilings: private registration data and custom attendee answers are excluded from public lexicon records (see [Privacy Erasure & GDPR Compliance](../security-and-identity/privacy-erasure.md)).

---

## Related Guides & Next Steps

* **[AT Protocol & Bluesky Jetstream](at-protocol-and-bluesky-jetstream.md)** — Ingest firehose events and publish calendar records.
* **[Modular Event Aspects](../events-and-ticketing/modular-event-aspects.md)** — Understand relational sector fields and how they map to lexicons.
* **[Privacy Erasure & GDPR](../security-and-identity/privacy-erasure.md)** — Account deletion and federated record removal.
* **[Local Development](../contributing/local-development.md)** — Work with CarpaNet source generators in .NET.
