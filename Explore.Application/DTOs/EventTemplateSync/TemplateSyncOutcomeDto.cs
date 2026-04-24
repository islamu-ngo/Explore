// ABOUTME: Result of applying an operator-selected event-template sync plan against current runtime state.
// ABOUTME: Separates applied keys, skipped keys, and structured conflicts while surfacing the new provenance version.

namespace Explore.Application.DTOs.EventTemplateSync;

public sealed record TemplateSyncOutcomeDto(
    IReadOnlyList<string> Applied,
    IReadOnlyList<string> Skipped,
    IReadOnlyList<SyncConflictDto> Conflicts,
    int NewProvenanceVersion,
    DateTimeOffset SyncedAt);
