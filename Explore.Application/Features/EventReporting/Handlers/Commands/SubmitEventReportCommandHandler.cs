// ABOUTME: Handles authenticated event-report submissions with duplicate, quota, and status checks.
// ABOUTME: Persists report metadata, target, encrypted evidence, local case, and outbox intent atomically.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.EventReporting.Policies;
using Explore.Application.Features.EventReporting.Requests.Commands;
using Explore.Application.Features.EventReporting.Validators;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Options;

namespace Explore.Application.Features.EventReporting.Handlers.Commands;

public sealed class SubmitEventReportCommandHandler(
    IEventRepository eventRepository,
    IEventReportRepository eventReportRepository,
    IGenericRepository<EventReportTarget, Guid> targetRepository,
    IGenericRepository<EventReportEvidence, Guid> evidenceRepository,
    IGenericRepository<EventReportCase, Guid> caseRepository,
    IActorRepository actorRepository,
    IOutboxRepository outboxRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    ICurrentUserService currentUserService,
    IEventReportEvidenceProtector evidenceProtector,
    BusinessMetrics metrics,
    IOptions<EventReportSubmissionOptions> optionsAccessor) : IRequestHandler<SubmitEventReportCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(SubmitEventReportCommand request, CancellationToken cancellationToken)
    {
        var options = NormalizeOptions(optionsAccessor.Value);
        var validationResult = await new SubmitEventReportCommandValidator(options).ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Failure(
                tenantId: null,
                Guid.Empty,
                "Event report submission failed due to validation errors.",
                validationResult.Errors.Select(error => error.ErrorMessage),
                EventReportReasonCodePolicy.InvalidReasonCodeFailureCode,
                "validation_failed");
        }

        if (tenantContext.TenantId == Guid.Empty)
        {
            return Failure(
                tenantId: null,
                Guid.Empty,
                "Tenant context could not be resolved.",
                ["Tenant context is required."],
                EventReportFailureCodes.TenantUnresolved,
                "tenant_unresolved");
        }

        var tenantId = tenantContext.TenantId;
        var reporterUserId = currentUserService.UserId;
        if (options.RequireAuthenticatedReporter && reporterUserId is null)
        {
            return Failure(
                tenantId,
                Guid.Empty,
                "Reporter user could not be resolved.",
                ["Authenticated reporter user id is required."],
                EventReportFailureCodes.UserUnresolved,
                "user_unresolved");
        }

        var @event = await eventRepository.GetById(request.Request.EventId);
        if (@event is null || @event.TenantId != tenantId)
        {
            return Failure(
                tenantId,
                Guid.Empty,
                "Event was not found.",
                ["Event was not found."],
                EventReportFailureCodes.EventNotFound,
                "event_not_found");
        }

        if (@event.EventStatusId != (int)EventStatusEnum.Published)
        {
            return Failure(
                tenantId,
                Guid.Empty,
                "Only published events can be reported.",
                ["Only published events can be reported."],
                EventReportFailureCodes.EventInvalidStatus,
                "invalid_status");
        }

        var reporterActor = reporterUserId.HasValue
            ? await actorRepository.GetActorByUserIdAndTenantId(reporterUserId.Value, tenantId)
            : null;

        if (reporterUserId.HasValue && reporterActor is null)
        {
            return Failure(
                tenantId,
                Guid.Empty,
                "Reporter actor could not be resolved.",
                ["Reporter actor is required."],
                EventReportFailureCodes.ReporterActorUnresolved,
                "actor_unresolved");
        }

        if (!EventReportReasonCodePolicy.TryNormalize(request.Request.ReasonCode, out var reasonCode, out var reasonError))
        {
            return Failure(
                tenantId,
                Guid.Empty,
                "Reason code is invalid.",
                [reasonError ?? "ReasonCode is invalid."],
                EventReportReasonCodePolicy.InvalidReasonCodeFailureCode,
                "validation_failed");
        }

        var now = DateTime.UtcNow;
        var duplicateWindowStart = now.AddHours(-options.DuplicateWindowHours);
        var duplicateExists = await eventReportRepository.ExistsByReporterAndEventAsync(
            tenantId,
            @event.Id,
            reporterUserId,
            reporterActor?.Id,
            request.ReporterIpHash,
            request.ReporterUserAgentHash,
            reasonCode,
            duplicateWindowStart,
            cancellationToken);

        if (duplicateExists)
        {
            return Failure(
                tenantId,
                Guid.Empty,
                "A matching event report was already submitted recently.",
                ["A matching report already exists in the duplicate prevention window."],
                EventReportFailureCodes.Duplicate,
                "duplicate");
        }

        var reporterWindowStart = now.AddHours(-1);
        var reporterCount = await eventReportRepository.CountByReporterSinceAsync(
            tenantId,
            reporterUserId,
            reporterActor?.Id,
            request.ReporterIpHash,
            request.ReporterUserAgentHash,
            reporterWindowStart,
            cancellationToken);

        if (options.MaxReportsPerUserPerHour > 0 && reporterCount >= options.MaxReportsPerUserPerHour)
        {
            metrics.RecordEventReportSubmission(tenantId.ToString(), "failed", FailureCodes.QuotaExceeded);
            var response = new BaseCommandResponse<Guid>();
            response.SetQuotaExceeded(
                "Reporter event-report quota exceeded.",
                new QuotaExceededDetails(
                    "reporting.max_reports_per_user_per_hour",
                    options.MaxReportsPerUserPerHour,
                    reporterCount,
                    reporterCount + 1,
                    "event_report_reporter",
                    tenantId));
            return response;
        }

        var reporterEventWindowStart = now.AddDays(-1);
        var reporterEventCount = await eventReportRepository.CountByReporterAndEventSinceAsync(
            tenantId,
            @event.Id,
            reporterUserId,
            reporterActor?.Id,
            request.ReporterIpHash,
            request.ReporterUserAgentHash,
            reporterEventWindowStart,
            cancellationToken);
        if (options.MaxReportsPerEventPerUserPerDay > 0 && reporterEventCount >= options.MaxReportsPerEventPerUserPerDay)
        {
            metrics.RecordEventReportSubmission(tenantId.ToString(), "failed", FailureCodes.QuotaExceeded);
            var response = new BaseCommandResponse<Guid>();
            response.SetQuotaExceeded(
                "Event report quota exceeded.",
                new QuotaExceededDetails(
                    "reporting.max_reports_per_event_per_user_per_day",
                    options.MaxReportsPerEventPerUserPerDay,
                    reporterEventCount,
                    reporterEventCount + 1,
                    "event_report_reporter_event",
                    tenantId));
            return response;
        }

        var priority = ResolvePriority(request.Request.SeverityHint);
        var encryptedReporterText = evidenceProtector.Protect(request.Request.ReporterText);
        var report = EventReport.Create(
            tenantId,
            @event.Id,
            reporterUserId,
            reporterActor?.Id,
            reporterUserId.HasValue ? EventReporterKind.AuthenticatedUser : EventReporterKind.Anonymous,
            EventReportSourceKind.UserReport,
            reasonCode,
            request.Request.SubcategoryCode,
            priority,
            request.Request.SeverityHint,
            request.Request.ReporterContactConsent && reporterUserId.HasValue,
            request.Request.ReporterLocale,
            request.ReporterIpHash,
            request.ReporterUserAgentHash,
            now);
        var target = EventReportTarget.CreateEventTarget(tenantId, report.Id, @event.Id);
        var evidence = EventReportEvidence.CreateReporterText(
            tenantId,
            report.Id,
            encryptedReporterText,
            EventReportEvidenceClassification.Sensitive,
            now.AddDays(options.ReporterTextRetentionDays),
            reporterUserId,
            now);
        var reportCase = EventReportCase.Create(
            tenantId,
            report.Id,
            options.DefaultQueueCode,
            priority,
            now.AddHours(options.CaseSlaHours),
            now);
        var outboxMessage = EventReportOutboxMessageFactory.CreateProviderSyncRequestedMessage(report, reportCase, request.CorrelationId);

        await unitOfWork.ExecuteInTransactionAsync(async _ =>
        {
            await eventReportRepository.Create(report);
            await targetRepository.Create(target);
            await evidenceRepository.Create(evidence);
            await caseRepository.Create(reportCase);
            await outboxRepository.Create(outboxMessage);
        }, cancellationToken);

        metrics.RecordEventReportSubmission(tenantId.ToString(), "succeeded");
        return Success(report.Id, "Event report submitted successfully.");
    }

    private static EventReportSubmissionOptions NormalizeOptions(EventReportSubmissionOptions options)
    {
        return new EventReportSubmissionOptions
        {
            RequireAuthenticatedReporter = options.RequireAuthenticatedReporter,
            MaxReportsPerUserPerHour = Math.Max(0, options.MaxReportsPerUserPerHour),
            MaxReportsPerEventPerUserPerDay = Math.Max(0, options.MaxReportsPerEventPerUserPerDay),
            DuplicateWindowHours = Math.Max(1, options.DuplicateWindowHours),
            ReporterTextRetentionDays = Math.Max(1, options.ReporterTextRetentionDays),
            MaxReporterTextLength = Math.Max(1, options.MaxReporterTextLength),
            DefaultQueueCode = string.IsNullOrWhiteSpace(options.DefaultQueueCode) ? "default" : options.DefaultQueueCode.Trim(),
            CaseSlaHours = Math.Max(1, options.CaseSlaHours),
            ReporterFingerprintPepper = string.IsNullOrWhiteSpace(options.ReporterFingerprintPepper)
                ? null
                : options.ReporterFingerprintPepper.Trim()
        };
    }

    private static EventReportPriority ResolvePriority(EventReportSeverityHint? severityHint)
    {
        return severityHint switch
        {
            EventReportSeverityHint.Low => EventReportPriority.Low,
            EventReportSeverityHint.High => EventReportPriority.High,
            EventReportSeverityHint.Critical => EventReportPriority.Urgent,
            _ => EventReportPriority.Normal
        };
    }

    private static BaseCommandResponse<Guid> Success(Guid id, string message) => new()
    {
        Success = true,
        Id = id,
        Message = message
    };

    private BaseCommandResponse<Guid> Failure(
        Guid? tenantId,
        Guid id,
        string message,
        IEnumerable<string> errors,
        string? failureCode,
        string failureCategory)
    {
        metrics.RecordEventReportSubmission(tenantId?.ToString(), "failed", failureCategory);

        return new BaseCommandResponse<Guid>
        {
            Success = false,
            Id = id,
            Message = message,
            Errors = errors.ToList(),
            FailureCode = failureCode
        };
    }
}
