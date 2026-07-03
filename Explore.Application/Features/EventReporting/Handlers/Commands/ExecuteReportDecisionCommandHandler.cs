// ABOUTME: Executes captured report decisions and closes or requeues the local report case.
// ABOUTME: Reuses existing event moderation commands so enforcement records share the canonical audit path.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventReporting.Requests.Commands;
using Explore.Application.Features.EventReporting.Validators;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventReporting.Handlers.Commands;

public sealed class ExecuteReportDecisionCommandHandler(
    IEventReportRepository eventReportRepository,
    ITenantUserRepository tenantUserRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    ICurrentUserService currentUserService,
    IMediator mediator) : IRequestHandler<ExecuteReportDecisionCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(ExecuteReportDecisionCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await new ExecuteReportDecisionCommandValidator().ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Failure(
                request.DecisionId,
                "Event report decision execution request is invalid.",
                validationResult.Errors.Select(error => error.ErrorMessage),
                EventReportFailureCodes.ValidationFailed);
        }

        if (tenantContext.TenantId == Guid.Empty)
        {
            return Failure(request.DecisionId, "Tenant context could not be resolved.", ["Tenant context is required."], EventReportFailureCodes.TenantUnresolved);
        }

        var tenantId = tenantContext.TenantId;
        var preflightReport = await eventReportRepository.GetByIdAsync(tenantId, request.ReportId, cancellationToken);
        var preflight = ValidateTarget(preflightReport, request);
        if (!preflight.Response.Success)
        {
            return preflight.Response;
        }

        if (preflight.Decision!.DecisionSource == EventReportDecisionSource.LocalModerator)
        {
            if (currentUserService.UserId is not { } moderatorUserId)
            {
                return Failure(request.DecisionId, "Moderator user could not be resolved.", ["Authenticated moderator user id is required."], EventReportFailureCodes.UserUnresolved);
            }

            if (!await tenantUserRepository.IsActiveTenantUserAsync(tenantId, moderatorUserId, cancellationToken))
            {
                return Failure(request.DecisionId, "Moderator is not active in the current tenant.", ["Moderator must be an active tenant user."], EventReportFailureCodes.ModeratorUnavailable);
            }
        }

        if (preflight.Case!.Status == EventReportCaseStatus.Closed)
        {
            return Success(request.DecisionId, "Event report decision was already executed.");
        }

        if (preflight.Case.ConcurrencyStamp != request.ExpectedCaseConcurrencyStamp)
        {
            return Failure(request.DecisionId, "Event report case was changed by another request.", ["Refresh the report case and try again."], EventReportFailureCodes.CaseConcurrencyConflict);
        }

        var enforcementResponse = await ExecuteEnforcementAsync(preflight.Decision!, request, cancellationToken);
        if (!enforcementResponse.Success)
        {
            return Failure(
                request.DecisionId,
                "Event report decision enforcement failed.",
                enforcementResponse.Errors ?? ["Decision enforcement failed."],
                enforcementResponse.FailureCode ?? EventReportFailureCodes.DecisionExecutionFailed);
        }

        return await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var report = await eventReportRepository.GetByIdForUpdateAsync(tenantId, request.ReportId, token);
            var target = ValidateTarget(report, request);
            if (!target.Response.Success)
            {
                return target.Response;
            }

            if (target.Case!.Status == EventReportCaseStatus.Closed)
            {
                return Success(request.DecisionId, "Event report decision was already executed.");
            }

            if (target.Case.ConcurrencyStamp != request.ExpectedCaseConcurrencyStamp)
            {
                return Failure(request.DecisionId, "Event report case was changed by another request.", ["Refresh the report case and try again."], EventReportFailureCodes.CaseConcurrencyConflict);
            }

            ApplyCompletion(target.Report!, target.Case, target.Decision!, DateTime.UtcNow);
            await eventReportRepository.Update(target.Report!);

            return Success(request.DecisionId, "Event report decision executed successfully.");
        }, cancellationToken);
    }

    private async Task<BaseCommandResponse<Guid>> ExecuteEnforcementAsync(
        EventReportDecision decision,
        ExecuteReportDecisionCommand request,
        CancellationToken cancellationToken)
    {
        var correlationId = NormalizeCorrelationId(request);
        return decision.DecisionKind switch
        {
            EventReportDecisionKind.LightModerate => await mediator.Send(new ModerateEventCommand
            {
                Id = request.EventId,
                ReasonCode = decision.ReasonCode,
                CorrelationId = correlationId,
                SourceReportId = request.ReportId,
                SourceReportDecisionId = request.DecisionId
            }, cancellationToken),
            EventReportDecisionKind.HeavyRedact => await mediator.Send(new HeavyRedactEventCommand
            {
                Id = request.EventId,
                ReasonCode = decision.ReasonCode,
                CorrelationId = correlationId,
                SourceReportId = request.ReportId,
                SourceReportDecisionId = request.DecisionId
            }, cancellationToken),
            _ => Success(request.DecisionId, "No event moderation enforcement required.")
        };
    }

    private static void ApplyCompletion(
        EventReport report,
        EventReportCase reportCase,
        EventReportDecision decision,
        DateTime utcNow)
    {
        switch (decision.DecisionKind)
        {
            case EventReportDecisionKind.NoViolation:
            case EventReportDecisionKind.Duplicate:
            case EventReportDecisionKind.Escalate:
                reportCase.Close(utcNow);
                break;
            case EventReportDecisionKind.LightModerate:
            case EventReportDecisionKind.HeavyRedact:
            case EventReportDecisionKind.WarnOrganizer:
                report.UpdateStatus(EventReportStatus.Actioned, utcNow);
                reportCase.Close(utcNow);
                break;
            case EventReportDecisionKind.NeedsMoreInfo:
                report.UpdateStatus(EventReportStatus.UnderReview, utcNow);
                reportCase.MarkWaitingReporter(utcNow);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(decision), decision.DecisionKind, "Unsupported event report decision kind.");
        }
    }

    private static (BaseCommandResponse<Guid> Response, EventReport? Report, EventReportCase? Case, EventReportDecision? Decision) ValidateTarget(
        EventReport? report,
        ExecuteReportDecisionCommand request)
    {
        if (report is null)
        {
            return (Failure(request.DecisionId, "Event report was not found.", ["Event report was not found."], EventReportFailureCodes.ReportNotFound), null, null, null);
        }

        if (report.EventId != request.EventId)
        {
            return (Failure(request.DecisionId, "Event report does not belong to the requested event.", ["Event report does not belong to the requested event."], EventReportFailureCodes.EventMismatch), null, null, null);
        }

        var reportCase = report.Cases.FirstOrDefault(candidate => candidate.Id == request.CaseId);
        if (reportCase is null)
        {
            return (Failure(request.DecisionId, "Event report case was not found.", ["Event report case was not found."], EventReportFailureCodes.CaseNotFound), null, null, null);
        }

        if (reportCase.Status is not (EventReportCaseStatus.DecisionReady or EventReportCaseStatus.Closed))
        {
            return (Failure(request.DecisionId, "Only decision-ready report cases can be executed.", ["Only decision-ready report cases can be executed."], EventReportFailureCodes.CaseInvalidStatus), null, null, null);
        }

        var decision = report.Decisions.FirstOrDefault(candidate => candidate.Id == request.DecisionId);
        if (decision is null)
        {
            return (Failure(request.DecisionId, "Event report decision was not found.", ["Event report decision was not found."], EventReportFailureCodes.DecisionNotFound), null, null, null);
        }

        if (decision.ReportId != report.Id || decision.CaseId != reportCase.Id)
        {
            return (Failure(request.DecisionId, "Event report decision does not belong to the requested report case.", ["Event report decision does not belong to the requested report case."], EventReportFailureCodes.DecisionInvalid), null, null, null);
        }

        return (Success(request.DecisionId, "Event report decision target is valid."), report, reportCase, decision);
    }

    private static string NormalizeCorrelationId(ExecuteReportDecisionCommand request)
    {
        return string.IsNullOrWhiteSpace(request.CorrelationId)
            ? $"report-decision:{request.DecisionId:N}"
            : request.CorrelationId.Trim();
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
