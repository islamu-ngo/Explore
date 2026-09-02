---
description: >-
  Operate linked-user OAuth, governed publication, exact-collection ingestion,
  and tenant-gated discovery.
---

# AT Protocol & Bluesky Jetstream

ISLAMU Event implements a selective AT Protocol integration. It is not an ActivityPub server, PDS, AppView, bridge, or general-purpose social protocol host.

## Current capability

Implemented behavior includes:

* AT Protocol OAuth for users already linked locally;
* governed outbound event and RSVP publication;
* exact-collection CarpaNet Jetstream ingestion;
* tenant-gated discovery;
* safe HAL source links;
* database-first outbox delivery;
* administrator and user capability controls.

Authentication and federation are independent. AT Protocol sign-in does not create a local account through email matching, enable ingestion, or grant publication consent.

## Outbound publication

Local lifecycle state is authoritative. Publication is queued only after durable local state commits, then delivered from the outbox.

One event record carries native protocol fields plus deterministic description coverage for supported local fields. Publication is rejected rather than enqueued when a required field cannot be represented, privacy policy fails, a URI/value is invalid, or the payload exceeds the limit. Data is not silently truncated.

Outbound RSVP is deliberately narrower and currently emits only `#going`.

## Jetstream ingestion

Subscriptions use exact collections:

* `community.lexicon.calendar.event`;
* `community.lexicon.calendar.rsvp`.

The canonical inbound record, tenant-local event/session materialization, and cursor settlement commit atomically. A cursor cannot advance without corresponding local state, and an inbound record does not trigger an outbound echo.

Network visibility does not automatically make a record visible or actionable in every tenant.

## Acceptance

Verify user linking and consent, one valid and rejected outbound event, cursor restart, duplicate inbound delivery, tenant discovery policy, no outbound echo, and HAL source-link privacy. Document queue recovery and the exact enabled collections.
