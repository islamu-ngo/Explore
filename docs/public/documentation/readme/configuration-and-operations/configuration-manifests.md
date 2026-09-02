---
description: >-
  Bootstrap and export governed configuration without secrets, PII, or
  application data.
---

# Configuration Manifests

Configuration manifests move governed settings and typed configuration documents. They are not database dumps, secret bundles, subject-data exports, or topology snapshots.

## What belongs in a manifest

Only allowlisted instance/tenant settings and approved typed documents belong in the contract. Typical uses include reproducible policy, presentation, template, and feature configuration that the application explicitly recognizes.

## What is excluded

Manifests exclude:

* users and identity records;
* registrations, admissions, orders, payments, and refunds;
* attendee or subject PII;
* operational/outbox state;
* provider runtime bindings and connection strings;
* database or privacy-authority topology;
* credentials and secret values.

Whole-instance export preserves governed configuration only. It does not satisfy application-data backup or privacy-erasure export obligations.

## Safe application

Manifest file ingestion is a trust boundary. Restrict paths, ownership, permissions, and the principal allowed to apply a document. Validate schema, version, authority, and every included section before committing.

Apply outcomes are explicit. A failed contract, permission check, or section update must not be silently skipped into a partially trusted configuration. Preserve atomicity and report bounded problem details without echoing sensitive content.

## Operating procedure

1. Export or author an allowlisted manifest without secrets.
2. Review the diff and intended scope.
3. Back up the affected authoritative stores.
4. Apply in an isolated or staging environment first.
5. Verify settings, HAL affordances, public disclosures, and provider behavior.
6. Promote the same reviewed artifact and retain its version/checksum as operational evidence.

Use [Secrets](secrets.md) for credentials and [Backup, Restore & Upgrade](backup-restore-upgrade.md) for durable application state.
