// ABOUTME: Unit tests for local event-report triage command handling.
// ABOUTME: Verifies queue movement, status transitions, event matching, and stale concurrency protection.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventReporting;
using Explore.Application.Features.EventReporting.Handlers.Commands;
using Explore.Application.Features.EventReporting.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventReporting.Commands;

public sealed class TriageEventReportCommandHandlerTests
{
    private readonly IEventReportRepository _eventReportRepository = Substitute.For<IEventReportRepository>();
    private readonly ITenantUserRepository _tenantUserRepository = Substitute.For<ITenantUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public TriageEventReportCommandHandlerTests()
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
    public async Task Handle_WithOpenCase_UpdatesQueuePriorityAndReportStatus()
    {
        var tenantId = Guid.CreateVersion7();
        var moderatorUserId = Guid.CreateVersion7();
        var report = CreateReport(tenantId);
        var caseItem = CreateCase(tenantId, report.Id);
        report.Cases.Add(caseItem);

        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(moderatorUserId);
        _tenantUserRepository.IsActiveTenantUserAsync(tenantId, moderatorUserId, Arg.Any<CancellationToken>()).Returns(true);
        _eventReportRepository.GetByIdForUpdateAsync(tenantId, report.Id, Arg.Any<CancellationToken>()).Returns(report);

        var result = await CreateHandler().Handle(new TriageEventReportCommand
        {
            EventId = report.EventId,
            ReportId = report.Id,
            CaseId = caseItem.Id,
            ExpectedCaseConcurrencyStamp = caseItem.ConcurrencyStamp,
            QueueCode = "urgent-safety",
            Priority = EventReportPriority.Urgent
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsEqualTo(report.Id);
        await Assert.That(report.Status).IsEqualTo(EventReportStatus.Triaged);
        await Assert.That(report.Priority).IsEqualTo(EventReportPriority.Urgent);
        await Assert.That(caseItem.QueueCode).IsEqualTo("urgent-safety");
        await Assert.That(caseItem.Priority).IsEqualTo(EventReportPriority.Urgent);
        await _eventReportRepository.Received(1).Update(report);
    }

    [Test]
    public async Task Handle_WithStaleConcurrencyStamp_ReturnsConflictWithoutSaving()
    {
        var tenantId = Guid.CreateVersion7();
        var moderatorUserId = Guid.CreateVersion7();
        var report = CreateReport(tenantId);
        var caseItem = CreateCase(tenantId, report.Id);
        report.Cases.Add(caseItem);

        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(moderatorUserId);
        _tenantUserRepository.IsActiveTenantUserAsync(tenantId, moderatorUserId, Arg.Any<CancellationToken>()).Returns(true);
        _eventReportRepository.GetByIdForUpdateAsync(tenantId, report.Id, Arg.Any<CancellationToken>()).Returns(report);

        var result = await CreateHandler().Handle(new TriageEventReportCommand
        {
            EventId = report.EventId,
            ReportId = report.Id,
            CaseId = caseItem.Id,
            ExpectedCaseConcurrencyStamp = Guid.CreateVersion7(),
            QueueCode = "urgent-safety",
            Priority = EventReportPriority.Urgent
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.CaseConcurrencyConflict);
        await Assert.That(report.Status).IsEqualTo(EventReportStatus.Submitted);
        await _eventReportRepository.DidNotReceive().Update(Arg.Any<EventReport>());
    }

    [Test]
    public async Task Handle_WhenReportEventDoesNotMatchCommand_ReturnsMismatch()
    {
        var tenantId = Guid.CreateVersion7();
        var moderatorUserId = Guid.CreateVersion7();
        var report = CreateReport(tenantId);
        var caseItem = CreateCase(tenantId, report.Id);
        report.Cases.Add(caseItem);

        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(moderatorUserId);
        _tenantUserRepository.IsActiveTenantUserAsync(tenantId, moderatorUserId, Arg.Any<CancellationToken>()).Returns(true);
        _eventReportRepository.GetByIdForUpdateAsync(tenantId, report.Id, Arg.Any<CancellationToken>()).Returns(report);

        var result = await CreateHandler().Handle(new TriageEventReportCommand
        {
            EventId = Guid.CreateVersion7(),
            ReportId = report.Id,
            CaseId = caseItem.Id,
            ExpectedCaseConcurrencyStamp = caseItem.ConcurrencyStamp,
            QueueCode = "default",
            Priority = EventReportPriority.High
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.EventMismatch);
        await _eventReportRepository.DidNotReceive().Update(Arg.Any<EventReport>());
    }

    private TriageEventReportCommandHandler CreateHandler() => new(
        _eventReportRepository,
        _tenantUserRepository,
        _unitOfWork,
        _tenantContext,
        _currentUserService);

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
            reportCaseUpdatesConsent: false,
            reportFollowUpContactConsent: false,
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
