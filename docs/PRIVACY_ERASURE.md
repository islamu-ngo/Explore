<!-- ABOUTME: Canonical documentation for the Privacy Erasure Authority, its concrete meaning, and storage topologies. -->
<!-- ABOUTME: Explains EmbeddedSqlite, CoLocated, and ExternalDatabase modes and guides self-hosters through topology selection. -->

# Privacy Erasure Authority & Storage Topologies

> **Audience:** Operators | Self-Hosters | Contributors | AI agents
> **Status:** Implemented
> **Owner:** Security / Platform
> **Last Verified:** 2026-08-02
> **Source Anchors:** `src/Explore.Application/Configuration/PrivacyErasureOptions.cs`, `src/Explore.Application/Services/RetainedAuthorityPrivacyErasureWorkflow.cs`, `src/Explore.Application/Services/PrivacyErasureApplier.cs`, `src/Explore.Persistence/Privacy/ErasureAuthority/EmbeddedPrivacyErasureAuthorityStorage.cs`, `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/EmbeddedPrivacyErasureAuthorityRepository.cs`, `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/EfCorePrivacyErasureAuthorityRepository.cs`, `src/Explore.Persistence/Privacy/ErasureAuthority/PrivacyErasureAuthorityDbContext.cs`, `src/Explore.API/BackgroundServices/PrivacyErasureStartupGate.cs`, `src/Explore.API/HealthChecks/PrivacyErasureReadinessHealthCheck.cs`

---

## 1. Concrete Meaning of Privacy Erasure Authority

The **Privacy Erasure Authority** is the single platform-wide engine responsible for orchestrating User erasure, enforcing anti-resurrection fences, maintaining payload-free audit proofs, and settling external provider side-effects.

Rather than executing ad-hoc SQL deletions, the platform operates on an **authority-first, replayable contract**:

```text
erasure request -> append authority fact (committed first)
                   |
                   v
         establish user fence
                   |
                   v
 serializable application transaction:
   +--> apply local PII hard-deletes & anonymization
   +--> update local application mirror & checkpoint
   +--> materialize protected provider outbox work
   +--> invalidate cache authority
   +--> persist receipt hash
                   |
                   v
 return 202 Accepted + short-lived receipt
                   |
                   v
 post-commit specialized provider outbox settlement
```

### Core Invariants

1. **Authority-First Ordering**: An immutable, policy-versioned authority fact is recorded and committed *before* local PII disposal or remote provider work begins.
2. **User Fencing**: Establishing a fence prevents raced writes, background workers, or cache rematerializers from recreating PII for a pending or erased subject.
3. **Atomic Local Settlement**: Local PII hard-deletes, anonymizations, actor tombstones, application mirror/checkpoint updates, protected provider outbox materialization, and receipt hash persistence execute within a single serializable application transaction.
4. **Truthful Asynchronous Contract**: Initial requests return `202 Accepted` alongside a short-lived `ErasureReceipt` authentication token. Status polling (`GET /api/privacy-erasure/status`) requires the receipt token and returns non-cacheable (`private, no-store`) bounded phase codes.
5. **Startup & Restore Replay**: At application startup, the startup gate replays all authority facts missing from the local checkpoint before serving traffic.

---

## 2. Storage Topologies: `EmbeddedSqlite`, `CoLocated`, `ExternalDatabase`

The platform workflow code is **100% identical** regardless of deployment choice. The configuration setting `PrivacyErasure:Authority:Topology` (`PRIVACY_ERASURE_AUTHORITY_TOPOLOGY`) selects one of three persistence topologies for storing the authority ledger.

| Feature / Guarantee | `EmbeddedSqlite` Mode | `CoLocated` Mode | `ExternalDatabase` Mode |
|---|---|---|---|
| **Authority Database Placement** | Dedicated local SQLite file, default `/app/data/privacy_erasure_authority.db` | Primary application PostgreSQL/SQLite database | Separate, independently managed PostgreSQL database instance |
| **Connection Credentials** | None; protective filesystem permissions only | Application database credentials | Structured endpoint plus separate function-only runtime and migrator roles |
| **`restoreReplayProtection` Health Flag** | `true` when its dedicated file is kept outside the primary restore | `false`; authority follows primary restore lifecycle | `true` when the external database has an independent restore lifecycle |
| **Rollback Resilience (Local Tx Failure)** | **Yes** — authority append commits before the application transaction | **Yes** — authority append commits before the application transaction | **Yes** — authority append commits before the application transaction |
| **Stale Application Restore Protection** | **Yes** — when the authority file is not overwritten by the primary restore | **No** — not guaranteed beyond primary restore fidelity | **Yes** — untouched external authority replays missing erasures against restored primary DB |
| **Concurrency Ceiling** | Exactly one writer/API replica; private cache, WAL, bounded busy timeout | Application-primary limits | Normal PostgreSQL deployment limits and function ACLs |
| **Target Use Case** | Local development, CI, and single-replica self-hosting | Single-database deployments and operationally simple upgrades | Multi-replica/HA production and independently operated compliance storage |

---

## 3. Why `EmbeddedSqlite` Is The Default

The authority must survive a stale primary-database restore. A separate local
file is the smallest deployment that provides an independent restore boundary
without requiring a second database server.

### 1. Small Self-Hosting Footprint

`EmbeddedSqlite` requires no authority server or credential. It still uses the
same authority-first workflow, fencing, checkpoints, receipts, provider
outboxes, and replay gate as `ExternalDatabase`.

### 2. Architectural Integrity & Application Rollback Safety

The authority append commits through its dedicated context before the primary
application transaction. If local mutation rolls back, restart/retry sees the
retained fact and reapplies the erasure.

### 3. Independent Restore Boundary

Back up the embedded authority file separately and never overwrite it during a
primary restore. This preserves the facts needed to re-erase data resurrected
by an older primary backup.

### 4. Explicit Operational Ceiling

Embedded authority startup requires `WriterReplicaCount=1`, a local durable
path, private cache, WAL, and a bounded busy timeout. Multi-replica/HA
deployments must choose `ExternalDatabase`, while `CoLocated` is suitable when
single-database operating simplicity is preferred.

---

## 4. Self-Hoster Decision & Selection Guide

Use this decision matrix to select the right topology for your environment:

- **EmbeddedSqlite** when local one-writer, file-backed isolation is acceptable.
- **CoLocated** when you want single-database operation and do not require independent authority backup-restore.
- **ExternalDatabase** when you need isolated compliance storage and independent authority recovery.

### Guidance Summary

- **Choose `EmbeddedSqlite` if**:
  - You are running local development, automated CI test suites, or single-container self-hosting (`docker-compose.yml`).
  - You run exactly one writer/API replica and can provide local durable storage.
  - You can back up and restore the authority file independently from the primary database.

- **Choose `ExternalDatabase` if**:
  - You operate enterprise multi-tenant or production SaaS environments.
  - You maintain independent backup/restore schedules for primary vs compliance databases.
  - You require absolute guarantees that restoring an application database backup from 30 days ago will automatically re-erase all accounts deleted during those 30 days upon service startup.
- **Choose `CoLocated` if**:
  - You want a single primary database with fewer components.
  - Your topology or platform operations already treat app and authority recovery together.
  - You do not require independently operated compliance database recovery.

---

## 5. Configuration Reference

Set the following environment variables in `.env`:

```dotenv
# Topology Selection: EmbeddedSqlite (default) | CoLocated | ExternalDatabase
PRIVACY_ERASURE_AUTHORITY_TOPOLOGY=EmbeddedSqlite

PRIVACY_ERASURE_AUTHORITY_EMBEDDED_PATH=/app/data/privacy_erasure_authority.db
PRIVACY_ERASURE_AUTHORITY_WRITER_REPLICA_COUNT=1
PRIVACY_ERASURE_AUTHORITY_BUSY_TIMEOUT_SECONDS=30

# CoLocated reuses primary application credentials. External-only values are required
# ONLY when PRIVACY_ERASURE_AUTHORITY_TOPOLOGY=ExternalDatabase.
PRIVACY_ERASURE_AUTHORITY_HOST=privacy-erasure-db
PRIVACY_ERASURE_AUTHORITY_PORT=5432
PRIVACY_ERASURE_AUTHORITY_DATABASE=privacy_erasure
PRIVACY_ERASURE_AUTHORITY_TLS_MODE=Required
PRIVACY_ERASURE_AUTHORITY_TRUST_SERVER_CERTIFICATE=false
PRIVACY_ERASURE_AUTHORITY_RUNTIME_USERNAME=erasure_app
PRIVACY_ERASURE_AUTHORITY_RUNTIME_PASSWORD=...
PRIVACY_ERASURE_AUTHORITY_MIGRATOR_USERNAME=erasure_admin
PRIVACY_ERASURE_AUTHORITY_MIGRATOR_PASSWORD=...
```

The embedded path must be absolute and local; URI/network paths are rejected,
busy timeout must be `1..300`, and writer count must equal one. Mount it on a
dedicated volume with restrictive permissions and back it up separately.
Compose maps these aliases to `PrivacyErasureAuthorityEmbedded:Path`,
`WriterReplicaCount`, and `BusyTimeoutSeconds`; direct .NET environment
configuration uses double underscores for those section separators.
Raw authority connection strings and `PrivacyErasure:Durability:Mode` are
unsupported and block startup.

---

## 6. Related Documentation

- [Backup, Restore, and Upgrade Runbook](BACKUP_RESTORE_UPGRADE.md)
- [Self-Hosting Guide](SELF_HOSTING.md)
- [Configuration Reference](CONFIGURATION.md)
- [Secrets Management](SECRETS.md)
- [Security Model](SECURITY-MODEL.md)
- [Operations Guide](OPERATIONS.md)
