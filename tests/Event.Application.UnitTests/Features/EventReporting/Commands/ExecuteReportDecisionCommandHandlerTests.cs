// ABOUTME: Unit tests for executing captured local event-report decisions.
// ABOUTME: Verifies moderation command delegation, case closure, idempotency, and stale concurrency handling.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventReporting;
using Explore.Application.Features.EventReporting.Handlers.Commands;
using Explore.Application.Features.EventReporting.Requests.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventReporting.Commands;

public sealed class ExecuteReportDecisionCommandHandlerTests
{
    private readonly IEventReportRepository _eventReportRepository = Substitute.For<IEventReportRepository>();
    private readonly ITenantUserRepository _tenantUserRepository = Substitute.For<ITenantUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    public ExecuteReportDecisionCommandHandlerTests()
    {
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>();
                return operation(CancellationToken.None);
            });

        _eventReportRepository.Update(Arg.Any<EventReport>()).Returns(Task.CompletedTask);
        _mediator.Send(Arg.Any<ModerateEventCommand>(), Arg.Any<CancellationToken>())
            .Returns(call => Success(call.Arg<ModerateEventCommand>().Id));
        _mediator.Send(Arg.Any<HeavyRedactEventCommand>(), Arg.Any<CancellationToken>())
            .Returns(call => Success(call.Arg<HeavyRedactEventCommand>().Id));
    }

    [Test]
    public async Task Handle_WithLightModerationDecision_DispatchesModerationCommandAndClosesCase()
    {
        var tenantId = Guid.CreateVersion7();
        var moderatorUserId = Guid.CreateVersion7();
        var report = CreateReportWithDecision(tenantId, moderatorUserId, EventReportDecisionKind.LightModerate);
        var caseItem = report.Cases.Single();
        var decision = report.Decisions.Single();
        ModerateEventCommand? sentCommand = null;

        ConfigureActiveModerator(tenantId, moderatorUserId);
        _eventReportRepository.GetByIdAsync(tenantId, report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _eventReportRepository.GetByIdForUpdateAsync(tenantId, report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _mediator.Send(Arg.Do<ModerateEventCommand>(command => sentCommand = command), Arg.Any<CancellationToken>())
            .Returns(Success(report.EventId));

        var result = await CreateHandler().Handle(new ExecuteReportDecisionCommand
        {
            EventId = report.EventId,
            ReportId = report.Id,
            CaseId = caseItem.Id,
            DecisionId = decision.Id,
            ExpectedCaseConcurrencyStamp = caseItem.ConcurrencyStamp,
            CorrelationId = "exec-123"
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(decision.Id);
        await Assert.That(report.Status).IsEqualTo(EventReportStatus.Actioned);
        await Assert.That(caseItem.Status).IsEqualTo(EventReportCaseStatus.Closed);
        await Assert.That(sentCommand).IsNotNull();
        await Assert.That(sentCommand!.Id).IsEqualTo(report.EventId);
        await Assert.That(sentCommand.ReasonCode).IsEqualTo(decision.ReasonCode);
        await Assert.That(sentCommand.CorrelationId).IsEqualTo("exec-123");
        await Assert.That(sentCommand.SourceReportId).IsEqualTo(report.Id);
        await Assert.That(sentCommand.SourceReportDecisionId).IsEqualTo(decision.Id);
        await _eventReportRepository.Received(1).Update(report);
    }

    [Test]
    public async Task Handle_WithNoViolationDecision_ClosesCaseWithoutModerationCommand()
    {
        var tenantId = Guid.CreateVersion7();
        var moderatorUserId = Guid.CreateVersion7();
        var report = CreateReportWithDecision(tenantId, moderatorUserId, EventReportDecisionKind.NoViolation);
        report.UpdateStatus(EventReportStatus.Dismissed, DateTime.UtcNow);
        var caseItem = report.Cases.Single();
        var decision = report.Decisions.Single();

        ConfigureActiveModerator(tenantId, moderatorUserId);
        _eventReportRepository.GetByIdAsync(tenantId, report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _eventReportRepository.GetByIdForUpdateAsync(tenantId, report.Id, Arg.Any<CancellationToken>()).Returns(report);

        var result = await CreateHandler().Handle(new ExecuteReportDecisionCommand
        {
            EventId = report.EventId,
            ReportId = report.Id,
            CaseId = caseItem.Id,
            DecisionId = decision.Id,
            ExpectedCaseConcurrencyStamp = caseItem.ConcurrencyStamp
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(caseItem.Status).IsEqualTo(EventReportCaseStatus.Closed);
        await Assert.That(report.Status).IsEqualTo(EventReportStatus.Dismissed);
        await _mediator.DidNotReceive().Send(Arg.Any<ModerateEventCommand>(), Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Send(Arg.Any<HeavyRedactEventCommand>(), Arg.Any<CancellationToken>());
        await _eventReportRepository.Received(1).Update(report);
    }

    [Test]
    public async Task Handle_WithCoopDecisionAndNoCurrentUser_DispatchesModerationWithoutTenantUserLookup()
    {
        var tenantId = Guid.CreateVersion7();
        var report = CreateReportWithDecision(
            tenantId,
            moderatorUserId: null,
            EventReportDecisionSource.CoopReviewer,
            EventReportDecisionKind.LightModerate);
        var caseItem = report.Cases.Single();
        var decision = report.Decisions.Single();
        ModerateEventCommand? sentCommand = null;

        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns((Guid?)null);
        _eventReportRepository.GetByIdAsync(tenantId, report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _eventReportRepository.GetByIdForUpdateAsync(tenantId, report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _mediator.Send(Arg.Do<ModerateEventCommand>(command => sentCommand = command), Arg.Any<CancellationToken>())
            .Returns(Success(report.EventId));

        var result = await CreateHandler().Handle(new ExecuteReportDecisionCommand
        {
            EventId = report.EventId,
            ReportId = report.Id,
            CaseId = caseItem.Id,
            DecisionId = decision.Id,
            ExpectedCaseConcurrencyStamp = caseItem.ConcurrencyStamp,
            CorrelationId = "coop-exec-1"
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(sentCommand).IsNotNull();
        await Assert.That(sentCommand!.SourceReportId).IsEqualTo(report.Id);
        await Assert.That(sentCommand.SourceReportDecisionId).IsEqualTo(decision.Id);
        await Assert.That(sentCommand.CorrelationId).IsEqualTo("coop-exec-1");
        await Assert.That(report.Status).IsEqualTo(EventReportStatus.Actioned);
        await Assert.That(caseItem.Status).IsEqualTo(EventReportCaseStatus.Closed);
        await _tenantUserRepository.DidNotReceive().IsActiveTenantUserAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenHeavyRedactionFails_DoesNotCloseCase()
    {
        var tenantId = Guid.CreateVersion7();
        var moderatorUserId = Guid.CreateVersion7();
        var report = CreateReportWithDecision(tenantId, moderatorUserId, EventReportDecisionKind.HeavyRedact);
        var caseItem = report.Cases.Single();
        var decision = report.Decisions.Single();

        ConfigureActiveModerator(tenantId, moderatorUserId);
        _eventReportRepository.GetByIdAsync(tenantId, report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _eventReportRepository.GetByIdForUpdateAsync(tenantId, report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _mediator.Send(Arg.Any<HeavyRedactEventCommand>(), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid>
            {
                Success = false,
                Id = report.EventId,
                Message = "pending storage deletion",
                Errors = ["Storage deletion pending."],
                FailureCode = "event_heavy_redaction_storage_deletion_pending"
            });

        var result = await CreateHandler().Handle(new ExecuteReportDecisionCommand
        {
            EventId = report.EventId,
            ReportId = report.Id,
            CaseId = caseItem.Id,
            DecisionId = decision.Id,
            ExpectedCaseConcurrencyStamp = caseItem.ConcurrencyStamp
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_heavy_redaction_storage_deletion_pending");
        await Assert.That(caseItem.Status).IsEqualTo(EventReportCaseStatus.DecisionReady);
        await _eventReportRepository.DidNotReceive().Update(Arg.Any<EventReport>());
    }

    [Test]
    public async Task Handle_WhenCaseAlreadyClosed_ReturnsIdempotentSuccessWithoutMediator()
    {
        var tenantId = Guid.CreateVersion7();
        var moderatorUserId = Guid.CreateVersion7();
        var report = CreateReportWithDecision(tenantId, moderatorUserId, EventReportDecisionKind.LightModerate);
        var caseItem = report.Cases.Single();
        var decision = report.Decisions.Single();
        caseItem.Close(DateTime.UtcNow);

        ConfigureActiveModerator(tenantId, moderatorUserId);
        _eventReportRepository.GetByIdAsync(tenantId, report.Id, Arg.Any<CancellationToken>()).Returns(report);

        var result = await CreateHandler().Handle(new ExecuteReportDecisionCommand
        {
            EventId = report.EventId,
            ReportId = report.Id,
            CaseId = caseItem.Id,
            DecisionId = decision.Id,
            ExpectedCaseConcurrencyStamp = Guid.CreateVersion7()
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _mediator.DidNotReceive().Send(Arg.Any<ModerateEventCommand>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive()
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithStaleCaseConcurrency_ReturnsConflictBeforeMediator()
    {
        var tenantId = Guid.CreateVersion7();
        var moderatorUserId = Guid.CreateVersion7();
        var report = CreateReportWithDecision(tenantId, moderatorUserId, EventReportDecisionKind.LightModerate);
        var caseItem = report.Cases.Single();
        var decision = report.Decisions.Single();

        ConfigureActiveModerator(tenantId, moderatorUserId);
        _eventReportRepository.GetByIdAsync(tenantId, report.Id, Arg.Any<CancellationToken>()).Returns(report);

        var result = await CreateHandler().Handle(new ExecuteReportDecisionCommand
        {
            EventId = report.EventId,
            ReportId = report.Id,
            CaseId = caseItem.Id,
            DecisionId = decision.Id,
            ExpectedCaseConcurrencyStamp = Guid.CreateVersion7()
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.CaseConcurrencyConflict);
        await Assert.That(caseItem.Status).IsEqualTo(EventReportCaseStatus.DecisionReady);
        await _mediator.DidNotReceive().Send(Arg.Any<ModerateEventCommand>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive()
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>());
    }

    private ExecuteReportDecisionCommandHandler CreateHandler() => new(
        _eventReportRepository,
        _tenantUserRepository,
        _unitOfWork,
        _tenantContext,
        _currentUserService,
        _mediator);

    private void ConfigureActiveModerator(Guid tenantId, Guid moderatorUserId)
    {
        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(moderatorUserId);
        _tenantUserRepository.IsActiveTenantUserAsync(tenantId, moderatorUserId, Arg.Any<CancellationToken>()).Returns(true);
    }

    private static EventReport CreateReportWithDecision(
        Guid tenantId,
        Guid moderatorUserId,
        EventReportDecisionKind decisionKind)
    {
        var report = EventReport.Create(
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

        report.UpdateStatus(EventReportStatus.UnderReview, DateTime.UtcNow);

        var caseItem = EventReportCase.Create(
            tenantId,
            report.Id,
            "default",
            EventReportPriority.Normal,
            DateTime.UtcNow.AddDays(1));
        caseItem.Assign(moderatorUserId, DateTime.UtcNow);
        caseItem.MarkDecisionReady(DateTime.UtcNow);

        var decision = EventReportDecision.Create(
            tenantId,
            caseItem.Id,
            report.Id,
            EventReportDecisionSource.LocalModerator,
            decisionKind,
            "policy_violation",
            safeNote: "safe note",
            moderatorUserId,
            externalDecisionId: null);

        report.Cases.Add(caseItem);
        report.Decisions.Add(decision);
        return report;
    }

    private static EventReport CreateReportWithDecision(
        Guid tenantId,
        Guid? moderatorUserId,
        EventReportDecisionSource decisionSource,
        EventReportDecisionKind decisionKind)
    {
        var report = EventReport.Create(
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

        report.UpdateStatus(EventReportStatus.UnderReview, DateTime.UtcNow);

        var caseItem = EventReportCase.Create(
            tenantId,
            report.Id,
            "default",
            EventReportPriority.Normal,
            DateTime.UtcNow.AddDays(1));
        if (moderatorUserId.HasValue)
        {
            caseItem.Assign(moderatorUserId.Value, DateTime.UtcNow);
        }

        caseItem.MarkDecisionReady(DateTime.UtcNow);

        var decision = EventReportDecision.Create(
            tenantId,
            caseItem.Id,
            report.Id,
            decisionSource,
            decisionKind,
            "policy_violation",
            safeNote: "safe note",
            moderatorUserId,
            externalDecisionId: decisionSource == EventReportDecisionSource.LocalModerator ? null : "coop-decision-1");

        report.Cases.Add(caseItem);
        report.Decisions.Add(decision);
        return report;
    }

    private static BaseCommandResponse<Guid> Success(Guid id) => new()
    {
        Success = true,
        Id = id,
        Message = "Succeeded"
    };
}
