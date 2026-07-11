// ABOUTME: Provider-neutral request to move an unreferenced backing object into quarantine.
// ABOUTME: Used only by policy-controlled reconciliation jobs after a dry-run report.

namespace Explore.Application.Models.Storage;

public sealed record FileStorageQuarantineInput(
    string ObjectKey,
    string Reason,
    DateTime UtcNow);
