// ABOUTME: Machine-readable sync conflict row describing why a requested template-sync key could not be applied.
// ABOUTME: Used for stale provenance, concurrent update, and protected local-state outcomes.

namespace Explore.Blazor.Client.Models.EventTemplateSync;

public sealed record SyncConflictDto(
    string Key,
    string Reason);
