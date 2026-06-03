// ABOUTME: Result model for one tenant-scoped AI assistant retention cleanup pass.
// ABOUTME: Reports cutoff, redacted row counts, and dry-run mode without exposing prompt content.

namespace Explore.Application.Models;

public sealed record AiRetentionCleanupResult(
    DateTime CutoffUtc,
    int RetentionDays,
    int EligibleConversations,
    int RedactedConversations,
    int RedactedMessages,
    int RedactedRuns,
    int RedactedReferences,
    int RedactedProposedActions,
    int RedactedToolExecutions,
    bool DryRun);
