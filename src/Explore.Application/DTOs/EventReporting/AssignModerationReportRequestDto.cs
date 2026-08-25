// ABOUTME: API request body for assigning an event report case to a tenant moderator.
// ABOUTME: Keeps assignment transport fields separate from the secured MediatR command.

namespace Explore.Application.DTOs.EventReporting;

public sealed record AssignModerationReportRequestDto
{
    public Guid CaseId { get; init; }
    public Guid ExpectedCaseConcurrencyStamp { get; init; }
    public Guid AssigneeUserId { get; init; }
}
