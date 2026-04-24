// ABOUTME: History row DTO for prior event template sync executions reconstructed from AuditLog records.
// ABOUTME: Exposes operator-facing provenance versions, applied/skipped keys, conflicts, actor, and timestamp.

namespace Explore.Blazor.Client.Models.EventTemplateSync;

public sealed record EventTemplateSyncHistoryItemDto(
    Guid EventId,
    int BaseProvenanceVersion,
    int TargetTemplateVersion,
    IReadOnlyList<string> Applied,
    IReadOnlyList<string> Skipped,
    IReadOnlyList<SyncConflictDto> Conflicts,
    Guid? ActorId,
    DateTimeOffset SyncedAt);
