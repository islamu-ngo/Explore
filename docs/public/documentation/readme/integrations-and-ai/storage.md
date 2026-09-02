---
description: >-
  Choose local or S3-compatible object storage and preserve metadata-backed
  authorization and recovery.
---

# Storage

Storage is local-first and provider-neutral. An optional S3-compatible provider is available when explicitly selected and configured.

## Object authority

New storage flows use metadata-backed storage objects and the established provider abstraction. The application database owns the metadata needed to authorize and locate an object; the provider owns its bytes.

Public reads are bound to a storage-object ID and the current resource policy. S3-compatible presigned downloads are generated only after ID-based authorization. They are not an upload path and should be short-lived according to deployment policy.

Never construct public object URLs from a bucket/key submitted by a caller or expose provider access credentials to the browser.

## Provider selection

* **Local:** keep data on a durable mounted filesystem and include it in backup/restore.
* **S3-compatible:** configure endpoint/bucket/policy/credentials through supported settings and secret authorities; assess TLS, residency, retention, versioning, and provider recovery.

Health and logs must not disclose private filesystem paths, object keys, bucket names, endpoints, access keys, or presigned URLs.

## Backup and restore

Back up object bytes together with database metadata and the application version that understands it. A bucket backup without metadata, or database restore without the corresponding objects, is incomplete.

Restore in isolation, verify representative private/public access, missing-object behavior, tenant boundaries, and presigned download expiry. Do not use a configuration-manifest export as an object-data backup.

## Acceptance

Upload through an implemented application workflow, read through authorized object ID, deny another tenant/principal, verify restart persistence, restore both metadata and bytes, and confirm provider failures produce bounded errors without leaking location or credentials.
