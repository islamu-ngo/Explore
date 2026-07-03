// ABOUTME: API request body for triaging an event report case into a moderation queue.
// ABOUTME: Carries case concurrency and bounded workflow metadata for CQRS command mapping.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.EventReporting;

public sealed class TriageModerationReportRequestDto
{
    public Guid CaseId { get; init; }
    public Guid ExpectedCaseConcurrencyStamp { get; init; }
    public required string QueueCode { get; init; }
    public EventReportPriority Priority { get; init; }
}
