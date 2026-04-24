// ABOUTME: Result of applying an operator-selected event-session template sync plan against current runtime state.
// ABOUTME: Separates applied keys, skipped keys, and structured conflicts while surfacing the new provenance version.

namespace Explore.Blazor.Client.Models.EventSessionTemplateSync;

public sealed record TemplateSyncOutcomeDto(
    IReadOnlyList<string> Applied,
    IReadOnlyList<string> Skipped,
    IReadOnlyList<SyncConflictDto> Conflicts,
    int NewProvenanceVersion,
    DateTimeOffset SyncedAt);
