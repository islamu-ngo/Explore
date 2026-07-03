// ABOUTME: Unit tests for local event-report assignment command handling.
// ABOUTME: Verifies active tenant-user checks, report status movement, and concurrency failures.

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

public sealed class AssignEventReportCommandHandlerTests
{
    private readonly IEventReportRepository _eventReportRepository = Substitute.For<IEventReportRepository>();
    private readonly ITenantUserRepository _tenantUserRepository = Substitute.For<ITenantUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public AssignEventReportCommandHandlerTests()
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
    public async Task Handle_WithActiveAssignee_AssignsCaseAndMovesReportUnderReview()
    {
        var tenantId = Guid.CreateVersion7();
        var moderatorUserId = Guid.CreateVersion7();
        var assigneeUserId = Guid.CreateVersion7();
        var report = CreateReport(tenantId);
        report.UpdateStatus(EventReportStatus.Triaged, DateTime.UtcNow);
        var caseItem = CreateCase(tenantId, report.Id);
        report.Cases.Add(caseItem);

        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(moderatorUserId);
        _tenantUserRepository.IsActiveTenantUserAsync(tenantId, moderatorUserId, Arg.Any<CancellationToken>()).Returns(true);
        _tenantUserRepository.IsActiveTenantUserAsync(tenantId, assigneeUserId, Arg.Any<CancellationToken>()).Returns(true);
        _eventReportRepository.GetByIdForUpdateAsync(tenantId, report.Id, Arg.Any<CancellationToken>()).Returns(report);

        var result = await CreateHandler().Handle(new AssignEventReportCommand
        {
            EventId = report.EventId,
            ReportId = report.Id,
            CaseId = caseItem.Id,
            AssigneeUserId = assigneeUserId,
            ExpectedCaseConcurrencyStamp = caseItem.ConcurrencyStamp
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(report.Id);
        await Assert.That(report.Status).IsEqualTo(EventReportStatus.UnderReview);
        await Assert.That(caseItem.Status).IsEqualTo(EventReportCaseStatus.Assigned);
        await Assert.That(caseItem.AssignedModeratorUserId).IsEqualTo(assigneeUserId);
        await _eventReportRepository.Received(1).Update(report);
    }

    [Test]
    public async Task Handle_WhenAssigneeIsNotActive_ReturnsFailureBeforeLoadingReport()
    {
        var tenantId = Guid.CreateVersion7();
        var moderatorUserId = Guid.CreateVersion7();
        var assigneeUserId = Guid.CreateVersion7();

        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(moderatorUserId);
        _tenantUserRepository.IsActiveTenantUserAsync(tenantId, moderatorUserId, Arg.Any<CancellationToken>()).Returns(true);
        _tenantUserRepository.IsActiveTenantUserAsync(tenantId, assigneeUserId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateHandler().Handle(new AssignEventReportCommand
        {
            EventId = Guid.CreateVersion7(),
            ReportId = Guid.CreateVersion7(),
            CaseId = Guid.CreateVersion7(),
            AssigneeUserId = assigneeUserId,
            ExpectedCaseConcurrencyStamp = Guid.CreateVersion7()
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.AssigneeUnavailable);
        await _eventReportRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive()
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithStaleConcurrencyStamp_ReturnsConflictWithoutSaving()
    {
        var tenantId = Guid.CreateVersion7();
        var moderatorUserId = Guid.CreateVersion7();
        var assigneeUserId = Guid.CreateVersion7();
        var report = CreateReport(tenantId);
        var caseItem = CreateCase(tenantId, report.Id);
        report.Cases.Add(caseItem);

        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(moderatorUserId);
        _tenantUserRepository.IsActiveTenantUserAsync(tenantId, moderatorUserId, Arg.Any<CancellationToken>()).Returns(true);
        _tenantUserRepository.IsActiveTenantUserAsync(tenantId, assigneeUserId, Arg.Any<CancellationToken>()).Returns(true);
        _eventReportRepository.GetByIdForUpdateAsync(tenantId, report.Id, Arg.Any<CancellationToken>()).Returns(report);

        var result = await CreateHandler().Handle(new AssignEventReportCommand
        {
            EventId = report.EventId,
            ReportId = report.Id,
            CaseId = caseItem.Id,
            AssigneeUserId = assigneeUserId,
            ExpectedCaseConcurrencyStamp = Guid.CreateVersion7()
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.CaseConcurrencyConflict);
        await Assert.That(caseItem.Status).IsEqualTo(EventReportCaseStatus.Open);
        await _eventReportRepository.DidNotReceive().Update(Arg.Any<EventReport>());
    }

    private AssignEventReportCommandHandler CreateHandler() => new(
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
