// ABOUTME: History row DTO for prior event-session template sync executions reconstructed from AuditLog records.
// ABOUTME: Exposes operator-facing provenance versions, applied/skipped keys, conflicts, actor, and timestamp.

namespace Explore.Application.DTOs.EventSessionTemplateSync;

public sealed record EventSessionTemplateSyncHistoryItemDto(
    Guid EventSessionId,
    int BaseProvenanceVersion,
    int TargetTemplateVersion,
    IReadOnlyList<string> Applied,
    IReadOnlyList<string> Skipped,
    IReadOnlyList<SyncConflictDto> Conflicts,
    Guid? ActorId,
    DateTimeOffset SyncedAt);
