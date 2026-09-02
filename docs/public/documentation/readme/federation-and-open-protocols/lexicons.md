---
description: >-
  Govern vendored AT Protocol schemas, exact identifiers, version changes, and
  privacy limits.
---

# Lexicons

Lexicons are executable interoperability contracts. ISLAMU Event vendors and reviews the exact schemas it supports instead of discovering arbitrary schemas at runtime.

## Supported collections

Inbound handling is limited to:

* `community.lexicon.calendar.event`;
* `community.lexicon.calendar.rsvp`.

Wildcard collection subscriptions and runtime lexicon discovery are disabled. Outbound RSVP support is narrower than inbound understanding and emits only `#going`.

## Governance

A lexicon change must preserve exact public standard identifiers where interoperability requires them while keeping application design, implementation, tests, and documentation independently authored.

Review together:

1. field type, cardinality, format, and size limits;
2. URI and reference semantics;
3. privacy/exposure rules for every outbound field;
4. inbound materialization and duplicate behavior;
5. cursor and restart compatibility;
6. application API and generated contract changes;
7. pre-1.0 migration and rollback impact.

Pin the application version and test ingest and publish paths before enabling a new protocol revision.

## Fail-closed representation

If supported local data cannot be represented without violating the schema, size limit, URI rules, or privacy policy, outbound publication is not enqueued. The platform does not drop or truncate fields to create a superficially valid record.

## Current limits

Vendored lexicons do not make ISLAMU Event a PDS, AppView, ActivityPub endpoint, bridge, unrestricted social collection host, or runtime schema registry. Public deployment claims should name the exact enabled collections and actions.
