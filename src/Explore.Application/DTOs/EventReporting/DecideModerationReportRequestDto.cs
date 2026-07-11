// ABOUTME: API request body for recording a local moderation decision on a report case.
// ABOUTME: Includes only safe decision metadata and duplicate grouping information.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.EventReporting;

public sealed class DecideModerationReportRequestDto
{
    public Guid CaseId { get; init; }
    public Guid ExpectedCaseConcurrencyStamp { get; init; }
    public EventReportDecisionKind DecisionKind { get; init; }
    public required string ReasonCode { get; init; }
    public string? SafeNote { get; init; }
    public Guid? DuplicateGroupId { get; init; }
}
