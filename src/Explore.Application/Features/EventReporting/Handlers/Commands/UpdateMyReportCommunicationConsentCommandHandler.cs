// ABOUTME: Updates communication consent only for the authenticated reporter's own event report.
// ABOUTME: Persists changed consent atomically and leaves unchanged requests audit-neutral.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventReporting.Requests.Commands;
using Explore.Application.Features.EventReporting.Validators;
using Explore.Application.Responses;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventReporting.Handlers.Commands;

public sealed class UpdateMyReportCommunicationConsentCommandHandler(
    IEventReportRepository eventReportRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    ICurrentUserService currentUserService,
    IAuthorizationProvider authorizationProvider,
    HybridCache cache,
    TimeProvider timeProvider)
    : IRequestHandler<UpdateMyReportCommunicationConsentCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateMyReportCommunicationConsentCommand request,
        CancellationToken cancellationToken)
    {
        var validationResult = await new UpdateMyReportCommunicationConsentCommandValidator()
            .ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Failure(
                request.ReportId,
                "Event report communication consent request is invalid.",
                validationResult.Errors.Select(error => error.ErrorMessage),
                EventReportFailureCodes.ValidationFailed);
        }

        var tenantId = tenantContext.TenantId;
        if (tenantId == Guid.Empty)
        {
            return Failure(
                request.ReportId,
                "Tenant context could not be resolved.",
                ["Tenant context is required."],
                EventReportFailureCodes.TenantUnresolved);
        }

        if (currentUserService.UserId is not { } reporterUserId)
        {
            return Failure(
                request.ReportId,
                "Reporter user could not be resolved.",
                ["Authenticated reporter user id is required."],
                EventReportFailureCodes.UserUnresolved);
        }

        var isAllowed = await authorizationProvider.IsAllowedAsync(
            ResourceKinds.User,
            reporterUserId.ToString(),
            AuthorizationActions.Users.Update,
            cancellationToken: cancellationToken);
        if (!isAllowed)
        {
            throw new AuthorizationException(
                ResourceKinds.User,
                AuthorizationActions.Users.Update);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var changed = false;
        var response = await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var report = await eventReportRepository.GetByIdForUpdateAsync(
                tenantId,
                request.ReportId,
                token);

            if (report is null
                || report.TenantId != tenantId
                || report.ReporterUserId != reporterUserId)
            {
                return Failure(
                    request.ReportId,
                    "Event report was not found.",
                    ["Event report was not found."],
                    EventReportFailureCodes.ReportNotFound);
            }

            var attemptChanged = report.ReportCaseUpdatesConsent != request.Request.ReportCaseUpdatesConsent
                || report.ReportFollowUpContactConsent != request.Request.ReportFollowUpContactConsent;
            if (!attemptChanged)
            {
                return Success(report.Id, "Event report communication consent is unchanged.");
            }

            changed = true;
            report.ChangeReporterCommunicationConsent(
                request.Request.ReportCaseUpdatesConsent,
                request.Request.ReportFollowUpContactConsent,
                now);
            await eventReportRepository.Update(report);

            return Success(report.Id, "Event report communication consent updated successfully.");
        }, cancellationToken);

        if (changed)
        {
            await cache.RemoveAsync(
                $"event-reporting:my-report:{tenantId:N}:{reporterUserId:N}:{request.ReportId:N}",
                cancellationToken);
        }

        return response;
    }

    private static BaseCommandResponse<Guid> Success(Guid id, string message) => new()
    {
        Success = true,
        Id = id,
        Message = message
    };

    private static BaseCommandResponse<Guid> Failure(
        Guid id,
        string message,
        IEnumerable<string> errors,
        string failureCode) => new()
        {
            Success = false,
            Id = id,
            Message = message,
            Errors = errors.ToList(),
            FailureCode = failureCode
        };

}
