// ABOUTME: Unit tests for processing Osprey signal callbacks into local event reports.
// ABOUTME: Verifies idempotency, tenant ownership, signal persistence, and urgent-priority promotion.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventReporting;
using Explore.Application.Features.EventReporting;
using Explore.Application.Features.EventReporting.Handlers.Commands;
using Explore.Application.Features.EventReporting.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventReporting.Commands;

public sealed class RecordOspreySignalCallbackCommandHandlerTests
{
    private readonly IEventReportRepository _eventReportRepository = Substitute.For<IEventReportRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    public RecordOspreySignalCallbackCommandHandlerTests()
    {
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>();
                return operation(CancellationToken.None);
            });

        _eventReportRepository.Update(Arg.Any<EventReport>()).Returns(Task.CompletedTask);
    }

    [Test]
    public async Task Handle_WithHeavyRedactionRecommendation_RecordsSignalAndPromotesCaseUrgency()
    {
        var tenantId = Guid.CreateVersion7();
        var report = CreateReport(tenantId);
        var reportCase = CreateCase(tenantId, report.Id);
        report.Cases.Add(reportCase);
        ConfigureTenantReport(tenantId, report);

        var result = await CreateHandler().Handle(new RecordOspreySignalCallbackCommand
        {
            Request = new OspreySignalCallbackRequestDto
            {
                TenantId = tenantId,
                ReportId = report.Id,
                EventId = report.EventId,
                CaseId = reportCase.Id,
                ProviderSignalId = "osp-signal-1",
                CorrelationId = "corr-osprey-1",
                Signals =
                [
                    new OspreySignalCallbackItemDto
                    {
                        SignalType = "policy_match",
                        PolicyCode = "trust.high_risk",
                        Score = 0.91m,
                        Verdict = "likely_violation",
                        RecommendedAction = "recommend_heavy_redact",
                        SafeSummary = "Matched a high risk policy.",
                        ExternalSignalId = "osp-signal-1",
                        CorrelationId = "corr-osprey-1",
                        CreatedAtUtc = new DateTime(2026, 7, 2, 11, 0, 0, DateTimeKind.Utc)
                    }
                ]
            }
        }, CancellationToken.None);

        var signal = report.Signals.Single();
        var link = report.ExternalLinks.Single();
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(report.Id);
        await Assert.That(signal.Provider).IsEqualTo(EventReportSignalProvider.Osprey);
        await Assert.That(signal.ProviderTargetScope).IsEqualTo(EventReportProviderTargetScope.Instance);
        await Assert.That(signal.ProviderTargetId).IsEqualTo("instance");
        await Assert.That(signal.ExternalSignalId).IsEqualTo("osp-signal-1");
        await Assert.That(signal.Verdict).IsEqualTo(EventReportSignalVerdict.LikelyViolation);
        await Assert.That(signal.RecommendedAction).IsEqualTo(EventReportRecommendedAction.HeavyRedact);
        await Assert.That(report.Priority).IsEqualTo(EventReportPriority.Urgent);
        await Assert.That(reportCase.Priority).IsEqualTo(EventReportPriority.Urgent);
        await Assert.That(link.Provider).IsEqualTo(EventReportExternalProvider.Osprey);
        await Assert.That(link.ProviderTargetScope).IsEqualTo(EventReportProviderTargetScope.Instance);
        await Assert.That(link.ProviderTargetId).IsEqualTo("instance");
        await Assert.That(link.ProviderSignalId).IsEqualTo("osp-signal-1");
        await Assert.That(link.SyncState).IsEqualTo(EventReportSyncState.Synced);
        await _eventReportRepository.Received(1).Update(report);
    }

    [Test]
    public async Task Handle_WhenSignalAlreadyExists_ReturnsIdempotentSuccessWithoutSaving()
    {
        var tenantId = Guid.CreateVersion7();
        var report = CreateReport(tenantId);
        var reportCase = CreateCase(tenantId, report.Id);
        var syncedAtUtc = new DateTime(2026, 7, 2, 11, 5, 0, DateTimeKind.Utc);
        report.ChangePriority(EventReportPriority.Urgent, syncedAtUtc);
        reportCase.ChangePriority(EventReportPriority.Urgent, syncedAtUtc);
        report.Cases.Add(reportCase);
        report.Signals.Add(EventReportSignal.Create(
            tenantId,
            report.Id,
            report.EventId,
            EventReportSignalProvider.Osprey,
            "policy_match",
            "trust.high_risk",
            0.91m,
            EventReportSignalVerdict.LikelyViolation,
            EventReportRecommendedAction.HeavyRedact,
            "Matched a high risk policy.",
            "osp-signal-1",
            "corr-osprey-1"));
        var externalLink = EventReportExternalLink.CreatePending(
            tenantId,
            report.Id,
            reportCase.Id,
            EventReportExternalProvider.Osprey,
            "corr-osprey-1");
        externalLink.MarkSynced(null, "osp-signal-1", null, syncedAtUtc);
        report.ExternalLinks.Add(externalLink);
        ConfigureTenantReport(tenantId, report);

        var result = await CreateHandler().Handle(new RecordOspreySignalCallbackCommand
        {
            Request = new OspreySignalCallbackRequestDto
            {
                TenantId = tenantId,
                ReportId = report.Id,
                EventId = report.EventId,
                CaseId = reportCase.Id,
                ProviderSignalId = "osp-signal-1",
                CorrelationId = "corr-osprey-1",
                Signals =
                [
                    new OspreySignalCallbackItemDto
                    {
                        SignalType = "policy_match",
                        PolicyCode = "trust.high_risk",
                        Score = 0.91m,
                        Verdict = "likely_violation",
                        RecommendedAction = "recommend_heavy_redact",
                        ExternalSignalId = "osp-signal-1",
                        CorrelationId = "corr-osprey-1"
                    }
                ]
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(report.Signals).Count().IsEqualTo(1);
        await Assert.That(report.ExternalLinks).Count().IsEqualTo(1);
        await _eventReportRepository.DidNotReceive().Update(Arg.Any<EventReport>());
    }

    [Test]
    public async Task Handle_WithTenantProviderTarget_RecordsTenantScopedSignalAndLink()
    {
        var tenantId = Guid.CreateVersion7();
        var tenantTargetId = tenantId.ToString("N");
        var report = CreateReport(tenantId);
        var reportCase = CreateCase(tenantId, report.Id);
        report.Cases.Add(reportCase);
        ConfigureTenantReport(tenantId, report);

        var result = await CreateHandler().Handle(new RecordOspreySignalCallbackCommand
        {
            Request = new OspreySignalCallbackRequestDto
            {
                TenantId = tenantId,
                ReportId = report.Id,
                EventId = report.EventId,
                CaseId = reportCase.Id,
                ProviderTargetScope = "tenant",
                ProviderTargetId = tenantTargetId,
                ProviderSignalId = "osp-signal-tenant",
                CorrelationId = "corr-osprey-tenant",
                Signals =
                [
                    new OspreySignalCallbackItemDto
                    {
                        SignalType = "policy_match",
                        PolicyCode = "trust.high_risk",
                        ExternalSignalId = "osp-signal-tenant",
                        CorrelationId = "corr-osprey-tenant"
                    }
                ]
            }
        }, CancellationToken.None);

        var signal = report.Signals.Single();
        var link = report.ExternalLinks.Single();
        await Assert.That(result.Success).IsTrue();
        await Assert.That(signal.ProviderTargetScope).IsEqualTo(EventReportProviderTargetScope.Tenant);
        await Assert.That(signal.ProviderTargetId).IsEqualTo(tenantTargetId);
        await Assert.That(link.ProviderTargetScope).IsEqualTo(EventReportProviderTargetScope.Tenant);
        await Assert.That(link.ProviderTargetId).IsEqualTo(tenantTargetId);
    }

    [Test]
    public async Task Handle_WhenProviderTargetConflictsWithExistingLink_FailsClosed()
    {
        var tenantId = Guid.CreateVersion7();
        var report = CreateReport(tenantId);
        var reportCase = CreateCase(tenantId, report.Id);
        report.Cases.Add(reportCase);
        var tenantLink = EventReportExternalLink.CreatePending(
            tenantId,
            report.Id,
            reportCase.Id,
            EventReportExternalProvider.Osprey,
            "corr-osprey-1",
            providerTargetScope: EventReportProviderTargetScope.Tenant,
            providerTargetId: tenantId.ToString("N"));
        tenantLink.MarkSynced(null, "osp-signal-1", null, DateTime.UtcNow);
        report.ExternalLinks.Add(tenantLink);
        ConfigureTenantReport(tenantId, report);

        var result = await CreateHandler().Handle(new RecordOspreySignalCallbackCommand
        {
            Request = new OspreySignalCallbackRequestDto
            {
                TenantId = tenantId,
                ReportId = report.Id,
                EventId = report.EventId,
                CaseId = reportCase.Id,
                ProviderSignalId = "osp-signal-1",
                CorrelationId = "corr-osprey-1",
                Signals =
                [
                    new OspreySignalCallbackItemDto
                    {
                        SignalType = "policy_match",
                        PolicyCode = "trust.high_risk",
                        ExternalSignalId = "osp-signal-1",
                        CorrelationId = "corr-osprey-1"
                    }
                ]
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.ValidationFailed);
        await _eventReportRepository.DidNotReceive().Update(Arg.Any<EventReport>());
    }

    [Test]
    public async Task Handle_WhenPayloadTenantDiffersFromAmbientTenant_ReturnsTenantFailure()
    {
        var ambientTenantId = Guid.CreateVersion7();
        _tenantContext.TenantId.Returns(ambientTenantId);

        var result = await CreateHandler().Handle(new RecordOspreySignalCallbackCommand
        {
            Request = new OspreySignalCallbackRequestDto
            {
                TenantId = Guid.CreateVersion7(),
                ReportId = Guid.CreateVersion7(),
                EventId = Guid.CreateVersion7(),
                Signals =
                [
                    new OspreySignalCallbackItemDto
                    {
                        SignalType = "policy_match",
                        PolicyCode = "trust.high_risk"
                    }
                ]
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.TenantUnresolved);
        await _eventReportRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenReportEventDoesNotMatchPayload_ReturnsEventMismatch()
    {
        var tenantId = Guid.CreateVersion7();
        var report = CreateReport(tenantId);
        report.Cases.Add(CreateCase(tenantId, report.Id));
        ConfigureTenantReport(tenantId, report);

        var result = await CreateHandler().Handle(new RecordOspreySignalCallbackCommand
        {
            Request = new OspreySignalCallbackRequestDto
            {
                TenantId = tenantId,
                ReportId = report.Id,
                EventId = Guid.CreateVersion7(),
                Signals =
                [
                    new OspreySignalCallbackItemDto
                    {
                        SignalType = "policy_match",
                        PolicyCode = "trust.high_risk"
                    }
                ]
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.EventMismatch);
        await _eventReportRepository.DidNotReceive().Update(Arg.Any<EventReport>());
    }

    private RecordOspreySignalCallbackCommandHandler CreateHandler() => new(
        _eventReportRepository,
        _unitOfWork,
        _tenantContext);

    private void ConfigureTenantReport(Guid tenantId, EventReport report)
    {
        _tenantContext.TenantId.Returns(tenantId);
        _eventReportRepository.GetByIdForUpdateAsync(tenantId, report.Id, Arg.Any<CancellationToken>()).Returns(report);
    }

    private static EventReport CreateReport(Guid tenantId)
    {
        return EventReport.Create(
            tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            null,
            EventReporterKind.AuthenticatedUser,
            EventReportSourceKind.UserReport,
            "spam",
            subcategoryCode: null,
            EventReportPriority.Normal,
            severityHint: null,
            reporterContactConsent: false,
            reporterLocale: null,
            reporterIpHash: null,
            reporterUserAgentHash: null);
    }

    private static EventReportCase CreateCase(Guid tenantId, Guid reportId)
    {
        return EventReportCase.Create(
            tenantId,
            reportId,
            "default",
            EventReportPriority.Normal,
            DateTime.UtcNow.AddDays(1));
    }
}
