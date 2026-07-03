// ABOUTME: Action request and result models for Blazor moderation report workflows.
// ABOUTME: Keeps generated API DTOs behind the moderation service boundary.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.EventReporting;

public sealed record ModerationReportTriageActionRequest(
    Guid CaseId,
    Guid ExpectedCaseConcurrencyStamp,
    string QueueCode,
    EventReportPriority Priority);

public sealed record ModerationReportAssignActionRequest(
    Guid CaseId,
    Guid ExpectedCaseConcurrencyStamp,
    Guid AssigneeUserId);

public sealed record ModerationReportDecisionActionRequest(
    Guid CaseId,
    Guid ExpectedCaseConcurrencyStamp,
    EventReportDecisionKind DecisionKind,
    string ReasonCode,
    string? SafeNote,
    Guid? DuplicateGroupId);

public sealed record ModerationReportExecuteDecisionActionRequest(
    Guid CaseId,
    Guid DecisionId,
    Guid ExpectedCaseConcurrencyStamp,
    string? CorrelationId);

public sealed record ModerationReportActionResult(
    bool Success,
    Guid? Id,
    string Message,
    IReadOnlyList<string> Errors,
    string? FailureCode)
{
    public static ModerationReportActionResult Successful(BaseCommandResponseOfGuid response)
        => new(
            true,
            response.Id,
            string.IsNullOrWhiteSpace(response.Message) ? "Moderation report updated." : response.Message,
            [],
            response.FailureCode);

    public static ModerationReportActionResult Failed(
        string? message,
        IEnumerable<string>? errors = null,
        string? failureCode = null)
        => new(
            false,
            null,
            string.IsNullOrWhiteSpace(message) ? "Moderation report action failed." : message,
            errors?.Where(error => !string.IsNullOrWhiteSpace(error)).ToArray() ?? [],
            failureCode);
}
