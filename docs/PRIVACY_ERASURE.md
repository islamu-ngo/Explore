ABOUTME: Documents the shipped authority-first privacy-erasure workflow and its operator boundary.
ABOUTME: Focuses on replay, receipts, provider-work fences, bounded readiness, cleanup, and known gaps.

# Privacy Erasure

> **Audience:** Operators | Contributors | AI agents
> **Status:** Mixed
> **Owner:** Security / Platform
> **Last Verified:** 2026-07-25
> **Source Anchors:** `Explore.Application/Services/RetainedAuthorityPrivacyErasureWorkflow.cs`, `Explore.API/BackgroundServices/PrivacyErasureStartupGate.cs`, `Explore.API/HealthChecks/PrivacyErasureReadinessHealthCheck.cs`, `Explore.API/Controllers/PrivacyErasureController.cs`, `Explore.API/BackgroundServices/PrivacyErasureCredentialCleanupProcessor.cs`, `Explore.Infrastructure/PrivacyErasureCredentialCleanupService.cs`, `Explore.Persistence/Repositories/PrivacyErasureProviderWorkRepository.cs`, `Explore.Domain/PrivacyErasure*.cs`

## Workflow

The shipped flow is authority-first:

1. Record the immutable authority fact.
2. Fence the local saga and persist the short-lived receipt hash before any PII enumeration.
3. Apply local disposal work in the same serializable transaction that advances status.
4. Return receipt-authenticated status only after local commit.
5. Replay retained authority facts at startup before the host starts serving.
6. Process provider work after local settlement using fenced claims and lease tokens.
7. Reconcile unknown provider work explicitly.

## Fences

- The authority fact is immutable and ordered.
- The local saga stores a receipt hash and expiry before any provider work is attempted.
- Status access uses the receipt-authenticated path and stays non-cacheable.
- Startup replay blocks host start until the retained authority and local checkpoint agree.
- Provider work claims use serializable transactions, a monotonic fence token, and a lease token so stale workers cannot settle a newer claim.

## Readiness

The readiness check is intentionally bounded. It reports only:

- topology,
- restore replay protection,
- whether replay is caught up,
- aggregate due work,
- aggregate unknown work,
- aggregate dead-lettered work,
- aggregate cache-convergence backlog.

It does not expose identifiers, targets, payloads, credentials, connection details, or exception text.

## Cleanup

Cleanup is finite and bounded:

- expired receipt hashes are cleared in batches,
- expired provider locators are cleared in batches,
- dry-run mode is supported for both cleanup flows,
- completed provider work can be retired by bounded cleanup.

UUIDs and minimized authority facts remain linkable personal data until their approved retention expires; minimizing their shape does not anonymize them.

This page does not claim compaction or legal hold as shipped.

## Remaining Gaps

- Co-located authority does not provide restore replay protection for a full application restore.
- Unknown provider work still needs explicit reconciliation.
- Generalized compaction is not shipped.
- Legal hold is not shipped.

See also: [Security Model](SECURITY-MODEL.md) and [Operations](OPERATIONS.md).
