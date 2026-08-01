<!-- ABOUTME: Canonical documentation for the Privacy Erasure Authority, its concrete meaning, and storage topologies. -->
<!-- ABOUTME: Explains CoLocated vs ExternalDatabase modes, why CoLocated is useful, and guides self-hosters through topology selection. -->

# Privacy Erasure Authority & Storage Topologies

> **Audience:** Operators | Self-Hosters | Contributors | AI agents
> **Status:** Implemented
> **Owner:** Security / Platform
> **Last Verified:** 2026-08-01
> **Source Anchors:** `src/Explore.Application/Configuration/PrivacyErasureOptions.cs`, `src/Explore.Application/Services/RetainedAuthorityPrivacyErasureWorkflow.cs`, `src/Explore.Application/Services/PrivacyErasureApplier.cs`, `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/CoLocatedPrivacyErasureAuthorityRepository.cs`, `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/EfCorePrivacyErasureAuthorityRepository.cs`, `src/Explore.Persistence/Privacy/ErasureAuthority/PrivacyErasureAuthorityDbContext.cs`, `src/Explore.API/BackgroundServices/PrivacyErasureStartupGate.cs`, `src/Explore.API/HealthChecks/PrivacyErasureReadinessHealthCheck.cs`

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

## 2. Storage Topologies: `CoLocated` vs `ExternalDatabase`

The platform workflow code is **100% identical** regardless of deployment choice. The configuration setting `PrivacyErasure:Authority:Topology` (`PRIVACY_ERASURE_AUTHORITY_TOPOLOGY`) selects between two persistence topologies for storing the authority ledger.

| Feature / Guarantee | `CoLocated` Mode | `ExternalDatabase` Mode |
|---|---|---|
| **Authority Database Placement** | Inside the primary application PostgreSQL database | Separate, independently managed PostgreSQL database instance |
| **Connection Credentials** | Reuses primary application database connection | Separate DSN (`ConnectionStrings:PrivacyErasureAuthority`) with function-only ACLs for API |
| **`restoreReplayProtection` Health Flag** | `false` | `true` (when authority DB has an independent restore lifecycle) |
| **Rollback Resilience (Local Tx Failure)** | **Yes** — authority appends commit via separate context before app transaction | **Yes** — authority appends commit via external connection before app transaction |
| **Stale Application Restore Protection** | **No** — restoring an old app backup also restores old authority ledger | **Yes** — untouched external authority replays missing erasures against restored app DB |
| **Infrastructure Overhead** | Zero — single PostgreSQL container/database | High — dual PostgreSQL instances, separate migration pipeline, independent backup lifecycle |
| **Target Use Case** | Local dev, CI/CD, single-container self-hosting | High-availability production, enterprise SaaS, strict anti-resurrection DR |

---

## 3. Why `CoLocated` Mode Is Essential

A common question from operators is:

> *"If `ExternalDatabase` mode is what actually preserves erasure when restoring a stale application backup, why does `CoLocated` mode even exist? Is `CoLocated` only useful as a stepping stone toward `ExternalDatabase`?"*

While `CoLocated` mode does provide a clean forward-migration path to `ExternalDatabase`, it serves **four major operational and architectural purposes** in its own right:

### 1. Zero-DevOps Developer Experience & Small-Scale Self-Hosting
- **Zero Friction**: Requiring a second independent PostgreSQL instance, separate connection credentials, distinct migration services, and complex Compose profiles for local development, CI testing, or single-container self-hosting would add massive operational friction.
- **Single-Codebase Uniformity**: `CoLocated` allows single-database deployments to execute the **exact same platform codebase**—including fencing, sagas, policy coverage, receipt generation, provider outboxes, and startup replay gates—without maintaining a separate "simplified" code path.

### 2. Architectural Integrity & Application Rollback Safety
- **Authority-First Guarantee**: Even in `CoLocated` mode, authority facts are appended using a separate, short-lived `ExploreDbContext` instance that commits **before** the main application transaction runs.
- **Rollback Resilience**: If application-side mutations fail, throw an exception, or roll back due to a serialization deadlock, the authority fact remains committed. Upon retry or startup, the replay engine detects the pending authority fact and re-applies local erasure. Transaction failure can never lose erasure intent.

### 3. Immediate Compliance Without Multi-Cluster DR Overhead
- Small self-hosters and organizations operating a single unified backup pipeline (where application database backups are created and restored in lockstep with strict retention windows) do not perform uncoordinated partial database restores.
- `CoLocated` provides complete, atomic local PII destruction, tombstoning, receipt generation, and post-commit provider outbox settlement without requiring multi-database infrastructure management.

### 4. Seamless Forward Topology Cutover
- Deployments can launch on Day 1 using `CoLocated` mode with zero infra overhead.
- When compliance requirements, SLAs, or enterprise DR mandates grow, operators can seamlessly perform a forward topology cutover by setting `PRIVACY_ERASURE_AUTHORITY_TOPOLOGY=ExternalDatabase`, seeding the external database with existing payload-free authority facts, and deploying the dedicated authority migration service.

---

## 4. Self-Hoster Decision & Selection Guide

Use this decision matrix to select the right topology for your environment:

```text
                          Do you require protection against
                       stale application backup restores?
                                       |
                     +-----------------+-----------------+
                     |                                   |
                    YES                                  NO
                     |                                   |
       Do you operate a separate             Use PRIVACY_ERASURE_AUTHORITY_TOPOLOGY
       PostgreSQL instance with an           = CoLocated
       independent backup lifecycle?         (Single DB, zero DevOps, full local
                     |                       erasure compliance & rollback safety)
           +---------+---------+
           |                   |
          YES                  NO
           |                   |
    Use ExternalDatabase    Deploy separate DB
    (Set connection         instance first, then
    strings & migration)    use ExternalDatabase
```

### Guidance Summary

- **Choose `CoLocated` if**:
  - You are running local development, automated CI test suites, or single-container self-hosting (`docker-compose.yml`).
  - You want single-database simplicity with minimal memory footprint and zero extra database maintenance.
  - Your disaster recovery procedures restore the entire infrastructure state in lockstep rather than doing uncoordinated application database restores.

- **Choose `ExternalDatabase` if**:
  - You operate enterprise multi-tenant or production SaaS environments.
  - You maintain independent backup/restore schedules for primary vs compliance databases.
  - You require absolute guarantees that restoring an application database backup from 30 days ago will automatically re-erase all accounts deleted during those 30 days upon service startup.

---

## 5. Configuration Reference

Set the following environment variables in `.env`:

```dotenv
# Topology Selection: CoLocated (default) | ExternalDatabase
PRIVACY_ERASURE_AUTHORITY_TOPOLOGY=CoLocated

# Required ONLY when PRIVACY_ERASURE_AUTHORITY_TOPOLOGY=ExternalDatabase:
# API Process (Runtime-only, function-exec privilege)
PRIVACY_ERASURE_AUTHORITY_RUNTIME_CONNECTION_STRING=Host=privacy-erasure-db;Database=privacy_erasure;Username=erasure_app;Password=...

# Migration Service Process (DDL & Grant privilege)
PRIVACY_ERASURE_AUTHORITY_MIGRATOR_CONNECTION_STRING=Host=privacy-erasure-db;Database=privacy_erasure;Username=erasure_admin;Password=...
```

*Note: Legacy key `PrivacyErasure:Durability:Mode` is obsolete and will block application startup if present.*

---

## 6. Related Documentation

- [Backup, Restore, and Upgrade Runbook](BACKUP_RESTORE_UPGRADE.md)
- [Self-Hosting Guide](SELF_HOSTING.md)
- [Configuration Reference](CONFIGURATION.md)
- [Secrets Management](SECRETS.md)
- [Security Model](SECURITY-MODEL.md)
- [Operations Guide](OPERATIONS.md)
