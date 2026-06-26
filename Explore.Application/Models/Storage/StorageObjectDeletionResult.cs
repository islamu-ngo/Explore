// ABOUTME: Bounded result for provider-backed storage metadata deletion attempts.
// ABOUTME: Distinguishes completed, skipped, and failed deletes without exposing object keys.

namespace Explore.Application.Models.Storage;

public sealed record StorageObjectDeletionResult(
    int ScannedCount,
    int DeletedCount,
    int MissingKeyDeletedCount,
    int FailedCount)
{
    public bool CompletedWithoutFailures => FailedCount == 0;
}
