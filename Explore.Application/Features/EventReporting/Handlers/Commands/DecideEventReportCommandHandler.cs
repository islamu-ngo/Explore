// ABOUTME: Handles local moderator decision capture for assigned event-report cases.
// ABOUTME: Records safe decision metadata, enforces assignment ownership, and marks decisions ready for execution.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventReporting.Requests.Commands;
using Explore.Application.Features.EventReporting.Validators;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventReporting.Handlers.Commands;

public sealed class DecideEventReportCommandHandler(
    IEventReportRepository eventReportRepository,
    IGenericRepository<EventReportDecision, Guid> decisionRepository,
    ITenantUserRepository tenantUserRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    ICurrentUserService currentUserService) : IRequestHandler<DecideEventReportCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(DecideEventReportCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await new DecideEventReportCommandValidator().ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Failure(
                request.ReportId,
                "Event report decision request is invalid.",
                validationResult.Errors.Select(error => error.ErrorMessage),
                EventReportFailureCodes.ValidationFailed);
        }

        if (tenantContext.TenantId == Guid.Empty)
        {
            return Failure(request.ReportId, "Tenant context could not be resolved.", ["Tenant context is required."], EventReportFailureCodes.TenantUnresolved);
        }

        if (currentUserService.UserId is not { } moderatorUserId)
        {
            return Failure(request.ReportId, "Moderator user could not be resolved.", ["Authenticated moderator user id is required."], EventReportFailureCodes.UserUnresolved);
        }

        var tenantId = tenantContext.TenantId;
        if (!await tenantUserRepository.IsActiveTenantUserAsync(tenantId, moderatorUserId, cancellationToken))
        {
            return Failure(request.ReportId, "Moderator is not active in the current tenant.", ["Moderator must be an active tenant user."], EventReportFailureCodes.ModeratorUnavailable);
        }

        return await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var report = await eventReportRepository.GetByIdForUpdateAsync(tenantId, request.ReportId, token);
            if (report is null)
            {
                return Failure(request.ReportId, "Event report was not found.", ["Event report was not found."], EventReportFailureCodes.ReportNotFound);
            }

            if (report.EventId != request.EventId)
            {
                return Failure(request.ReportId, "Event report does not belong to the requested event.", ["Event report does not belong to the requested event."], EventReportFailureCodes.EventMismatch);
            }

            var reportCase = report.Cases.FirstOrDefault(candidate => candidate.Id == request.CaseId);
            if (reportCase is null)
            {
                return Failure(request.ReportId, "Event report case was not found.", ["Event report case was not found."], EventReportFailureCodes.CaseNotFound);
            }

            if (reportCase.ConcurrencyStamp != request.ExpectedCaseConcurrencyStamp)
            {
                return Failure(request.ReportId, "Event report case was changed by another request.", ["Refresh the report case and try again."], EventReportFailureCodes.CaseConcurrencyConflict);
            }

            if (reportCase.Status != EventReportCaseStatus.Assigned)
            {
                return Failure(request.ReportId, "Only assigned report cases can receive a decision.", ["Only assigned report cases can receive a decision."], EventReportFailureCodes.CaseInvalidStatus);
            }

            if (reportCase.AssignedModeratorUserId != moderatorUserId)
            {
                return Failure(request.ReportId, "Report case is assigned to another moderator.", ["Only the assigned moderator can decide this report case."], EventReportFailureCodes.AssignmentMismatch);
            }

            if (report.IsTerminal)
            {
                return Failure(request.ReportId, "Terminal event reports cannot receive new decisions.", ["Terminal event reports cannot receive new decisions."], EventReportFailureCodes.ReportInvalidStatus);
            }

            if (request.DecisionKind == EventReportDecisionKind.Duplicate && request.DuplicateGroupId is null)
            {
                return Failure(request.ReportId, "Duplicate report decisions require a duplicate group.", ["DuplicateGroupId is required."], EventReportFailureCodes.DuplicateGroupRequired);
            }

            var now = DateTime.UtcNow;
            ApplyReportDecisionStatus(report, request, now);
            reportCase.MarkDecisionReady(now);

            var decision = EventReportDecision.Create(
                tenantId,
                reportCase.Id,
                report.Id,
                EventReportDecisionSource.LocalModerator,
                request.DecisionKind,
                request.ReasonCode,
                request.SafeNote,
                moderatorUserId,
                externalDecisionId: null,
                now);

            await eventReportRepository.Update(report);
            await decisionRepository.Create(decision);

            return Success(decision.Id, "Event report decision recorded successfully.");
        }, cancellationToken);
    }

    private static void ApplyReportDecisionStatus(EventReport report, DecideEventReportCommand request, DateTime utcNow)
    {
        switch (request.DecisionKind)
        {
            case EventReportDecisionKind.NoViolation:
                report.UpdateStatus(EventReportStatus.Dismissed, utcNow);
                break;
            case EventReportDecisionKind.Duplicate:
                report.MarkDuplicate(request.DuplicateGroupId!.Value, utcNow);
                break;
            case EventReportDecisionKind.Escalate:
                report.UpdateStatus(EventReportStatus.Escalated, utcNow);
                break;
            case EventReportDecisionKind.NeedsMoreInfo:
            case EventReportDecisionKind.LightModerate:
            case EventReportDecisionKind.HeavyRedact:
            case EventReportDecisionKind.WarnOrganizer:
                report.UpdateStatus(EventReportStatus.UnderReview, utcNow);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request), request.DecisionKind, "Unsupported event report decision kind.");
        }
    }

    private static BaseCommandResponse<Guid> Success(Guid id, string message) => new()
    {
        Success = true,
        Id = id,
        Message = message
    };

    private static BaseCommandResponse<Guid> Failure(Guid id, string message, IEnumerable<string> errors, string? failureCode = null) => new()
    {
        Success = false,
        Id = id,
        Message = message,
        Errors = errors.ToList(),
        FailureCode = failureCode
    };
}
