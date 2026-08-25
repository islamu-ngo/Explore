// ABOUTME: API request body for executing a captured moderation report decision.
// ABOUTME: Supplies case concurrency and optional correlation metadata for audit continuity.

namespace Explore.Application.DTOs.EventReporting;

public sealed record ExecuteModerationReportDecisionRequestDto
{
    public Guid CaseId { get; init; }
    public Guid DecisionId { get; init; }
    public Guid ExpectedCaseConcurrencyStamp { get; init; }
    public string? CorrelationId { get; init; }
}
