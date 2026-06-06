// ABOUTME: Aggregate result for one AI retention cleanup scheduler pass across tenants.
// ABOUTME: Reports bounded counts only and intentionally excludes tenant IDs, prompts, and payloads.

namespace Explore.Application.Models;

public sealed record AiRetentionCleanupRunResult(
    DateTime UtcNow,
    int TenantCount,
    int SucceededTenantCount,
    int FailedTenantCount,
    int EligibleConversations,
    int RedactedConversations,
    int RedactedMessages,
    int RedactedRuns,
    int RedactedReferences,
    int RedactedProposedActions,
    int RedactedToolExecutions,
    bool DryRun);
