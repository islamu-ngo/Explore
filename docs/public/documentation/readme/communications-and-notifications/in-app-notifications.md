---
description: Operate the durable tenant-scoped inbox, fanout, SSE, and Web Push hints.
---

# In-App Notifications

The in-app inbox is the durable source of truth for user-visible notifications. Realtime transports only tell clients that state may have changed.

## Record model and actions

Notifications are authenticated, user-owned, and tenant-scoped. Supported operations are:

* list and detail reads;
* unread count;
* mark one or all as read;
* archive and unarchive;
* snooze and unsnooze;
* soft delete.

There is no mark-unread or permanent-delete endpoint.

## Fanout and deduplication

Publication and moderation fanout uses transactional outbox work and deterministic deduplication. This separates the business transition from delivery and allows safe replay after a worker or provider interruption.

Server-Sent Events are refresh hints. Browser Web Push carries generic refresh or navigation hints. Neither transport contains or replaces authoritative inbox state; clients reload the inbox before presenting final status.

## Channel boundaries

An in-app record becomes an email only when the originating notification intent explicitly creates both channels. The platform does not convert every inbox row to email.

Actor-subscription and light-moderation fanout are in-app only. Required heavy-moderation availability tracks explicit in-app and email delivery state.

## Operations

Monitor outbox/backlog, dedupe results, inbox state, and transport health separately. Recovery should restore the dependency, replay supported work, and verify the durable record rather than injecting a second notification manually.

Keep message bodies, recipient identity, device endpoints, tenant-private data, and push subscription material out of metrics and public support evidence.
