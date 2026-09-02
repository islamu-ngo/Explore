---
description: >-
  Synchronize registration subscribers to an optional external Listmonk
  instance.
---

# Listmonk

Listmonk integration is optional and disabled by default. ISLAMU Event does not bundle a Listmonk server in its local Docker Compose topology; the operator supplies and secures an external instance.

## Purpose

The integration synchronizes eligible registration subscribers to configured Listmonk lists. It is an external subscriber-synchronization destination, not the authoritative notification inbox or SMTP dispatch system.

## Configuration surface

Relevant deployment settings include:

* `LISTMONK_ENABLED`;
* `LISTMONK_INSTANCE_URL`;
* default list ID and preconfirmation choice;
* registration-sync enablement;
* username and API key secret bindings.

The administrative API exposes sanitized settings, grouped non-secret updates, dedicated credential rotation, and connection testing. Credentials remain in the selected secret authority.

## Delivery and recovery

Synchronization uses the native client/worker path with bounded retry and dead-letter recovery. A dedicated `listmonk-integration` readiness check reports whether the enabled destination is usable.

When a sync fails:

1. verify the instance URL, DNS/TLS, secret binding, and remote list;
2. run the supported connection test;
3. restore readiness;
4. inspect bounded retry/dead-letter metadata;
5. drain or replay through the supported operation;
6. verify one subscriber result without exposing address or profile data.

## Boundaries

Enabling Listmonk does not enable universal email marketing, create a bundled server, copy every user, or make Listmonk the source of consent or notification truth. Operators must define their own lawful basis, subscription policy, retention, recipient support, and provider security.
