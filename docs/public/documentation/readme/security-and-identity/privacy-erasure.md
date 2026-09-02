---
description: >-
  Operate the anti-resurrection authority, receipt flow, provider work, and
  recovery topology.
---

# Privacy Erasure

Privacy erasure is an authority-first durable workflow designed to prevent deleted subject data from returning after retries or a stale-primary restore.

## State flow

1. Establish a durable user anti-resurrection fence.
2. Settle local erasure state in one serializable transaction.
3. Continue external provider cleanup asynchronously.
4. Return `202 Accepted` with a one-time receipt.
5. Replay authority state during startup before serving traffic.

The API uses `DELETE /api/user` with a UUIDv7 `Idempotency-Key`. The response supplies `Location: /api/privacy-erasure/status` and `Retry-After: 5`. Status requests use `Authorization: ErasureReceipt <receipt>`.

Receipts are private, no-store capabilities. Missing, invalid, expired, or wrong receipts collapse to the same unauthorized response and must not reveal whether a subject exists.

## Authority topologies

| Topology           | Store                        | Operational boundary                                      |
| ------------------ | ---------------------------- | --------------------------------------------------------- |
| `EmbeddedSqlite`   | Dedicated local SQLite file  | Smallest path; back up separately; one writer/API replica |
| `CoLocated`        | Primary PostgreSQL or SQLite | Simpler, without independent stale-primary protection     |
| `ExternalDatabase` | Separate PostgreSQL          | Multi-replica/HA and independent recovery                 |

Unsupported combinations fail before sockets or database I/O and do not reveal connection details.

## Recovery

The erasure authority is not ordinary application data. Preserve it according to topology, restore it before opening traffic, and verify startup replay re-establishes fences. Configuration manifests exclude subject data, erasure data, users, payments, operational state, and secrets; they do not replace this backup.

## Acceptance

Test idempotent repeated requests, indistinguishable invalid receipts, restart replay, pending provider work, and a stale-primary restore against the preserved authority. Keep receipts, subject identifiers, provider payloads, and PII out of telemetry and support evidence.
