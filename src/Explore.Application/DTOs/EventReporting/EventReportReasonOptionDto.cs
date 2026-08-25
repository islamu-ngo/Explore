// ABOUTME: Safe reporter-facing option DTO for event-report reason selection.
// ABOUTME: Exposes stable enum-backed reason IDs, codes, names, and short descriptions only.

namespace Explore.Application.DTOs.EventReporting;

public sealed record EventReportReasonOptionDto
{
    public int ReasonId { get; init; }
    public required string ReasonCode { get; init; }
    public required string ReasonName { get; init; }
    public required string Description { get; init; }
}
