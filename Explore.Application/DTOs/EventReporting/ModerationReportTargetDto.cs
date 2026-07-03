// ABOUTME: Management projection for a reported target reference.
// ABOUTME: Identifies the event-level or future sub-resource target without exposing unrelated entity payloads.

namespace Explore.Application.DTOs.EventReporting;

public sealed class ModerationReportTargetDto
{
    public Guid Id { get; init; }
    public Guid ReportId { get; init; }
    public int TargetKindId { get; init; }
    public required string TargetKindCode { get; init; }
    public required string TargetKindName { get; init; }
    public Guid TargetId { get; init; }
    public string? FieldPath { get; init; }
    public Guid? StorageObjectId { get; init; }
}
