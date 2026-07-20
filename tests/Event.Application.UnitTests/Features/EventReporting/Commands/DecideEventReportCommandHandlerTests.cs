// ABOUTME: Unit tests for local event-report decision command handling.
// ABOUTME: Verifies assigned-moderator ownership, decision persistence, duplicate grouping, and concurrency protection.

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

public sealed class DecideEventReportCommandHandlerTests
{
    private readonly IEventReportRepository _eventReportRepository = Substitute.For<IEventReportRepository>();
    private readonly ITenantUserRepository _tenantUserRepository = Substitute.For<ITenantUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public DecideEventReportCommandHandlerTests()
    {
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>();
                return operation(CancellationToken.None);
            });

        _eventReportRepository.PersistDecisionCaptureAsync(
                Arg.Any<EventReport>(),
                Arg.Any<EventReportDecision>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    [Test]
    public async Task Handle_WithAssignedCase_RecordsDecisionAndMarksCaseDecisionReady()
    {
        var tenantId = Guid.CreateVersion7();
        var moderatorUserId = Guid.CreateVersion7();
        var report = CreateUnderReviewReport(tenantId);
        var caseItem = CreateAssignedCase(tenantId, report.Id, moderatorUserId);
        report.Cases.Add(caseItem);

        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(moderatorUserId);
        _tenantUserRepository.IsActiveTenantUserAsync(tenantId, moderatorUserId, Arg.Any<CancellationToken>()).Returns(true);
        _eventReportRepository.GetByIdForUpdateAsync(tenantId, report.Id, Arg.Any<CancellationToken>()).Returns(report);
        var result = await CreateHandler().Handle(new DecideEventReportCommand
        {
            EventId = report.EventId,
            ReportId = report.Id,
            CaseId = caseItem.Id,
            ExpectedCaseConcurrencyStamp = caseItem.ConcurrencyStamp,
            DecisionKind = EventReportDecisionKind.LightModerate,
            ReasonCode = "policy_violation",
            SafeNote = "Visible title violates listing rules."
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(report.Status).IsEqualTo(EventReportStatus.UnderReview);
        await Assert.That(caseItem.Status).IsEqualTo(EventReportCaseStatus.DecisionReady);
        var decision = report.Decisions.Single();
        await Assert.That(decision.Id).IsEqualTo(result.Id);
        await Assert.That(decision.TenantId).IsEqualTo(tenantId);
        await Assert.That(decision.ReportId).IsEqualTo(report.Id);
        await Assert.That(decision.CaseId).IsEqualTo(caseItem.Id);
        await Assert.That(decision.DecisionSource).IsEqualTo(EventReportDecisionSource.LocalModerator);
        await Assert.That(decision.DecisionKind).IsEqualTo(EventReportDecisionKind.LightModerate);
        await Assert.That(decision.ReasonCode).IsEqualTo("policy_violation");
        await Assert.That(decision.SafeNote).IsEqualTo("Visible title violates listing rules.");
        await Assert.That(decision.ModeratorUserId).IsEqualTo(moderatorUserId);
        await _eventReportRepository.Received(1).PersistDecisionCaptureAsync(
            report,
            decision,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithDuplicateDecision_CapturesIdentityWithoutMutatingReportOutcome()
    {
        var tenantId = Guid.CreateVersion7();
        var moderatorUserId = Guid.CreateVersion7();
        var duplicateGroupId = Guid.CreateVersion7();
        var report = CreateUnderReviewReport(tenantId);
        var caseItem = CreateAssignedCase(tenantId, report.Id, moderatorUserId);
        report.Cases.Add(caseItem);

        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(moderatorUserId);
        _tenantUserRepository.IsActiveTenantUserAsync(tenantId, moderatorUserId, Arg.Any<CancellationToken>()).Returns(true);
        _eventReportRepository.GetByIdForUpdateAsync(tenantId, report.Id, Arg.Any<CancellationToken>()).Returns(report);

        var result = await CreateHandler().Handle(new DecideEventReportCommand
        {
            EventId = report.EventId,
            ReportId = report.Id,
            CaseId = caseItem.Id,
            ExpectedCaseConcurrencyStamp = caseItem.ConcurrencyStamp,
            DecisionKind = EventReportDecisionKind.Duplicate,
            DuplicateGroupId = duplicateGroupId,
            ReasonCode = "duplicate_report"
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(report.Status).IsEqualTo(EventReportStatus.UnderReview);
        await Assert.That(report.DuplicateGroupId).IsNull();
        await Assert.That(caseItem.Status).IsEqualTo(EventReportCaseStatus.DecisionReady);
        await Assert.That(report.Decisions.Single().DuplicateGroupId).IsEqualTo(duplicateGroupId);
    }

    [Test]
    public async Task Handle_WhenDuplicateDecisionMissingGroup_ReturnsValidationFailureBeforeTransaction()
    {
        var result = await CreateHandler().Handle(new DecideEventReportCommand
        {
            EventId = Guid.CreateVersion7(),
            ReportId = Guid.CreateVersion7(),
            CaseId = Guid.CreateVersion7(),
            ExpectedCaseConcurrencyStamp = Guid.CreateVersion7(),
            DecisionKind = EventReportDecisionKind.Duplicate,
            ReasonCode = "duplicate_report"
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.ValidationFailed);
        await _unitOfWork.DidNotReceive()
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenCaseIsAssignedToAnotherModerator_ReturnsAssignmentMismatch()
    {
        var tenantId = Guid.CreateVersion7();
        var moderatorUserId = Guid.CreateVersion7();
        var otherModeratorUserId = Guid.CreateVersion7();
        var report = CreateUnderReviewReport(tenantId);
        var caseItem = CreateAssignedCase(tenantId, report.Id, otherModeratorUserId);
        report.Cases.Add(caseItem);

        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(moderatorUserId);
        _tenantUserRepository.IsActiveTenantUserAsync(tenantId, moderatorUserId, Arg.Any<CancellationToken>()).Returns(true);
        _eventReportRepository.GetByIdForUpdateAsync(tenantId, report.Id, Arg.Any<CancellationToken>()).Returns(report);

        var result = await CreateHandler().Handle(new DecideEventReportCommand
        {
            EventId = report.EventId,
            ReportId = report.Id,
            CaseId = caseItem.Id,
            ExpectedCaseConcurrencyStamp = caseItem.ConcurrencyStamp,
            DecisionKind = EventReportDecisionKind.NoViolation,
            ReasonCode = "no_violation"
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.AssignmentMismatch);
        await Assert.That(caseItem.Status).IsEqualTo(EventReportCaseStatus.Assigned);
        await _eventReportRepository.DidNotReceive().PersistDecisionCaptureAsync(
            Arg.Any<EventReport>(),
            Arg.Any<EventReportDecision>(),
            Arg.Any<CancellationToken>());
    }

    private DecideEventReportCommandHandler CreateHandler() => new(
        _eventReportRepository,
        _tenantUserRepository,
        _unitOfWork,
        _tenantContext,
        _currentUserService);

    private static EventReport CreateUnderReviewReport(Guid tenantId)
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
            reportCaseUpdatesConsent: false,
            reportFollowUpContactConsent: false,
            reporterLocale: null,
            reporterIpHash: null,
            reporterUserAgentHash: null);

        report.UpdateStatus(EventReportStatus.UnderReview, DateTime.UtcNow);
        return report;
    }

    private static EventReportCase CreateAssignedCase(Guid tenantId, Guid reportId, Guid moderatorUserId)
    {
        var caseItem = EventReportCase.Create(
            tenantId,
            reportId,
            "default",
            EventReportPriority.Normal,
            DateTime.UtcNow.AddDays(1));

        caseItem.Assign(moderatorUserId, DateTime.UtcNow);
        return caseItem;
    }
}
