// ABOUTME: Unit tests for processing signed Coop decision callbacks into local report decisions.
// ABOUTME: Verifies idempotency, tenant isolation, provider audit capture, and execution dispatch.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventReporting;
using Explore.Application.Features.EventReporting;
using Explore.Application.Features.EventReporting.Handlers.Commands;
using Explore.Application.Features.EventReporting.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventReporting.Commands;

public sealed class ProcessCoopDecisionCallbackCommandHandlerTests
{
    private readonly IEventReportRepository _eventReportRepository = Substitute.For<IEventReportRepository>();
    private readonly IGenericRepository<EventReportDecision, Guid> _decisionRepository = Substitute.For<IGenericRepository<EventReportDecision, Guid>>();
    private readonly IUnitOfWork _unitOfWork = new ImmediateUnitOfWork();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    public ProcessCoopDecisionCallbackCommandHandlerTests()
    {
        _eventReportRepository.Update(Arg.Any<EventReport>()).Returns(Task.CompletedTask);
        _decisionRepository.Create(Arg.Any<EventReportDecision>())
            .Returns(call => call.Arg<EventReportDecision>());
        _mediator.Send(Arg.Any<ExecuteReportDecisionCommand>(), Arg.Any<CancellationToken>())
            .Returns(call => Success(call.Arg<ExecuteReportDecisionCommand>().DecisionId));
    }

    [Test]
    public async Task Handle_WithLightModerationAction_RecordsCoopDecisionAndDispatchesExecution()
    {
        var tenantId = Guid.CreateVersion7();
        var report = CreateReport(tenantId);
        var reportCase = CreateCase(tenantId, report.Id);
        var initialCaseStamp = reportCase.ConcurrencyStamp;
        report.Cases.Add(reportCase);
        var capturedDecisions = new List<EventReportDecision>();
        ExecuteReportDecisionCommand? sentExecution = null;
        ConfigureTenantReport(tenantId, report);
        _decisionRepository.Create(Arg.Do<EventReportDecision>(decision => capturedDecisions.Add(decision)))
            .Returns(call => call.Arg<EventReportDecision>());
        _mediator.Send(Arg.Do<ExecuteReportDecisionCommand>(command => sentExecution = command), Arg.Any<CancellationToken>())
            .Returns(call => Success(call.Arg<ExecuteReportDecisionCommand>().DecisionId));

        var result = await CreateHandler().Handle(new ProcessCoopDecisionCallbackCommand
        {
            Request = new CoopDecisionCallbackRequestDto
            {
                TenantId = tenantId,
                ReportId = report.Id,
                EventId = report.EventId,
                CaseId = reportCase.Id,
                ExpectedCaseConcurrencyStamp = initialCaseStamp,
                ProviderDecisionId = "coop-decision-1",
                ProviderCaseId = "coop-case-1",
                ProviderUrl = "https://coop.example/cases/coop-case-1",
                CorrelationId = "coop-correlation-1",
                ReasonCode = "trust.policy",
                SafeNote = "Provider reviewed safe metadata.",
                Action = new CoopDecisionCallbackActionDto { Id = "light_moderate" },
                Policies = [new CoopDecisionCallbackPolicyDto { Id = "trust.policy", Name = "Trust policy" }],
                Rules = [new CoopDecisionCallbackRuleDto { Id = "event.public_content", Name = "Public content" }]
            }
        }, CancellationToken.None);

        var createdDecision = capturedDecisions.Single();
        var coopLink = report.ExternalLinks.Single();
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(createdDecision.Id);
        await Assert.That(report.Status).IsEqualTo(EventReportStatus.UnderReview);
        await Assert.That(reportCase.Status).IsEqualTo(EventReportCaseStatus.DecisionReady);
        await Assert.That(createdDecision.DecisionSource).IsEqualTo(EventReportDecisionSource.CoopReviewer);
        await Assert.That(createdDecision.DecisionKind).IsEqualTo(EventReportDecisionKind.LightModerate);
        await Assert.That(createdDecision.ModeratorUserId).IsNull();
        await Assert.That(createdDecision.ExternalDecisionId).IsEqualTo("coop-decision-1");
        await Assert.That(createdDecision.ProviderTargetScope).IsEqualTo(EventReportProviderTargetScope.Instance);
        await Assert.That(createdDecision.ProviderTargetId).IsEqualTo("instance");
        await Assert.That(createdDecision.ReasonCode).IsEqualTo("trust.policy");
        await Assert.That(coopLink.Provider).IsEqualTo(EventReportExternalProvider.Coop);
        await Assert.That(coopLink.ProviderTargetScope).IsEqualTo(EventReportProviderTargetScope.Instance);
        await Assert.That(coopLink.ProviderTargetId).IsEqualTo("instance");
        await Assert.That(coopLink.ProviderCaseId).IsEqualTo("coop-case-1");
        await Assert.That(coopLink.ProviderUrl).IsEqualTo("https://coop.example/cases/coop-case-1");
        await Assert.That(coopLink.SyncState).IsEqualTo(EventReportSyncState.Synced);
        await Assert.That(sentExecution).IsNotNull();
        await Assert.That(sentExecution!.DecisionId).IsEqualTo(createdDecision.Id);
        await Assert.That(sentExecution.ExpectedCaseConcurrencyStamp).IsEqualTo(initialCaseStamp);
        await _eventReportRepository.Received(1).Update(report);
    }

    [Test]
    public async Task Handle_WhenDecisionAlreadyExistsAndCaseClosed_ReturnsIdempotentSuccessWithoutExecution()
    {
        var tenantId = Guid.CreateVersion7();
        var report = CreateReport(tenantId);
        var reportCase = CreateCase(tenantId, report.Id);
        reportCase.MarkDecisionReady(DateTime.UtcNow);
        reportCase.Close(DateTime.UtcNow);
        var existingDecision = EventReportDecision.Create(
            tenantId,
            reportCase.Id,
            report.Id,
            EventReportDecisionSource.CoopReviewer,
            EventReportDecisionKind.LightModerate,
            "trust.policy",
            safeNote: null,
            moderatorUserId: null,
            externalDecisionId: "coop-decision-1",
            providerTargetScope: EventReportProviderTargetScope.Instance,
            providerTargetId: "instance");
        report.Cases.Add(reportCase);
        report.Decisions.Add(existingDecision);
        ConfigureTenantReport(tenantId, report);

        var result = await CreateHandler().Handle(new ProcessCoopDecisionCallbackCommand
        {
            Request = new CoopDecisionCallbackRequestDto
            {
                TenantId = tenantId,
                ReportId = report.Id,
                EventId = report.EventId,
                CaseId = reportCase.Id,
                ProviderDecisionId = "coop-decision-1",
                Action = new CoopDecisionCallbackActionDto { Id = "light_moderate" }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(existingDecision.Id);
        await _decisionRepository.DidNotReceive().Create(Arg.Any<EventReportDecision>());
        await _eventReportRepository.DidNotReceive().Update(Arg.Any<EventReport>());
        await _mediator.DidNotReceive().Send(Arg.Any<ExecuteReportDecisionCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithTenantProviderTarget_RecordsTenantScopedDecisionAndLink()
    {
        var tenantId = Guid.CreateVersion7();
        var report = CreateReport(tenantId);
        var reportCase = CreateCase(tenantId, report.Id);
        var tenantTargetId = tenantId.ToString("N");
        report.Cases.Add(reportCase);
        var capturedDecisions = new List<EventReportDecision>();
        ConfigureTenantReport(tenantId, report);
        _decisionRepository.Create(Arg.Do<EventReportDecision>(decision => capturedDecisions.Add(decision)))
            .Returns(call => call.Arg<EventReportDecision>());

        var result = await CreateHandler().Handle(new ProcessCoopDecisionCallbackCommand
        {
            Request = new CoopDecisionCallbackRequestDto
            {
                TenantId = tenantId,
                ReportId = report.Id,
                EventId = report.EventId,
                CaseId = reportCase.Id,
                ProviderTargetScope = "tenant",
                ProviderTargetId = tenantTargetId,
                ProviderDecisionId = "coop-decision-tenant",
                ProviderCaseId = "coop-case-tenant",
                CorrelationId = "coop-correlation-tenant",
                Action = new CoopDecisionCallbackActionDto { Id = "light_moderate" }
            }
        }, CancellationToken.None);

        var createdDecision = capturedDecisions.Single();
        var coopLink = report.ExternalLinks.Single();
        await Assert.That(result.Success).IsTrue();
        await Assert.That(createdDecision.ProviderTargetScope).IsEqualTo(EventReportProviderTargetScope.Tenant);
        await Assert.That(createdDecision.ProviderTargetId).IsEqualTo(tenantTargetId);
        await Assert.That(coopLink.ProviderTargetScope).IsEqualTo(EventReportProviderTargetScope.Tenant);
        await Assert.That(coopLink.ProviderTargetId).IsEqualTo(tenantTargetId);
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
            EventReportExternalProvider.Coop,
            "coop-correlation-1",
            providerTargetScope: EventReportProviderTargetScope.Tenant,
            providerTargetId: tenantId.ToString("N"));
        tenantLink.MarkSynced("coop-case-1", null, null, DateTime.UtcNow);
        report.ExternalLinks.Add(tenantLink);
        ConfigureTenantReport(tenantId, report);

        var result = await CreateHandler().Handle(new ProcessCoopDecisionCallbackCommand
        {
            Request = new CoopDecisionCallbackRequestDto
            {
                TenantId = tenantId,
                ReportId = report.Id,
                EventId = report.EventId,
                CaseId = reportCase.Id,
                ProviderDecisionId = "coop-decision-1",
                ProviderCaseId = "coop-case-1",
                CorrelationId = "coop-correlation-1",
                Action = new CoopDecisionCallbackActionDto { Id = "light_moderate" }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.ValidationFailed);
        await _decisionRepository.DidNotReceive().Create(Arg.Any<EventReportDecision>());
        await _eventReportRepository.DidNotReceive().Update(Arg.Any<EventReport>());
        await _mediator.DidNotReceive().Send(Arg.Any<ExecuteReportDecisionCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenPayloadTenantDiffersFromAmbientTenant_ReturnsTenantFailure()
    {
        var ambientTenantId = Guid.CreateVersion7();
        _tenantContext.TenantId.Returns(ambientTenantId);

        var result = await CreateHandler().Handle(new ProcessCoopDecisionCallbackCommand
        {
            Request = new CoopDecisionCallbackRequestDto
            {
                TenantId = Guid.CreateVersion7(),
                ReportId = Guid.CreateVersion7(),
                EventId = Guid.CreateVersion7(),
                CaseId = Guid.CreateVersion7(),
                Action = new CoopDecisionCallbackActionDto { Id = "light_moderate" }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.TenantUnresolved);
        await _eventReportRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Send(Arg.Any<ExecuteReportDecisionCommand>(), Arg.Any<CancellationToken>());
    }

    private ProcessCoopDecisionCallbackCommandHandler CreateHandler() => new(
        _eventReportRepository,
        _decisionRepository,
        _unitOfWork,
        _tenantContext,
        _mediator);

    private void ConfigureTenantReport(Guid tenantId, EventReport report)
    {
        _tenantContext.TenantId.Returns(tenantId);
        _eventReportRepository.GetByIdForUpdateAsync(tenantId, report.Id, Arg.Any<CancellationToken>()).Returns(report);
    }

    private static EventReport CreateReport(Guid tenantId) => EventReport.Create(
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

    private static EventReportCase CreateCase(Guid tenantId, Guid reportId) => EventReportCase.Create(
        tenantId,
        reportId,
        "default",
        EventReportPriority.Normal,
        DateTime.UtcNow.AddDays(1));

    private static BaseCommandResponse<Guid> Success(Guid id) => new()
    {
        Success = true,
        Id = id,
        Message = "Succeeded"
    };

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default) =>
            operation(ct);

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) =>
            operation(ct);
    }
}
