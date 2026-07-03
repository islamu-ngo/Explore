// ABOUTME: Unit tests for event-scoped moderation report queue query handling.
// ABOUTME: Verifies filter composition, paging normalization, and safe list-row projections.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventReporting.Handlers.Queries;
using Explore.Application.Features.EventReporting.Requests.Queries;
using Explore.Application.Specifications.EventReports;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventReporting.Queries;

public sealed class GetModerationReportQueueRequestHandlerTests
{
    private readonly IEventReportRepository _eventReportRepository = Substitute.For<IEventReportRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    [Test]
    public async Task Handle_WithFilters_QueriesRepositoryAndMapsQueueRows()
    {
        var tenantId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var moderatorUserId = Guid.CreateVersion7();
        var report = CreateReport(tenantId, eventId, EventReportPriority.High);
        report.UpdateStatus(EventReportStatus.UnderReview, DateTime.UtcNow);
        var caseItem = EventReportCase.Create(
            tenantId,
            report.Id,
            "safety",
            EventReportPriority.High,
            DateTime.UtcNow.AddHours(4));
        caseItem.Assign(moderatorUserId, DateTime.UtcNow);
        report.Cases.Add(caseItem);
        report.Decisions.Add(EventReportDecision.Create(
            tenantId,
            caseItem.Id,
            report.Id,
            EventReportDecisionSource.LocalModerator,
            EventReportDecisionKind.LightModerate,
            "spam",
            safeNote: "safe note",
            moderatorUserId,
            externalDecisionId: null));
        report.Signals.Add(EventReportSignal.Create(
            tenantId,
            report.Id,
            eventId,
            EventReportSignalProvider.Local,
            "keyword",
            "spam-policy",
            score: 0.82m,
            EventReportSignalVerdict.NeedsReview,
            EventReportRecommendedAction.LightModerate,
            "safe summary",
            externalSignalId: null,
            "corr-1"));

        EventReportQuerySpecification? capturedSpecification = null;
        _tenantContext.TenantId.Returns(tenantId);
        _eventReportRepository.GetReportQueueAsync(
                tenantId,
                2,
                5,
                Arg.Do<EventReportQuerySpecification>(specification => capturedSpecification = specification),
                Arg.Any<CancellationToken>())
            .Returns((new List<EventReport> { report }, 1));

        var result = await CreateHandler().Handle(new GetModerationReportQueueRequest
        {
            EventId = eventId,
            PageNumber = 2,
            PageSize = 5,
            Statuses = [EventReportStatus.UnderReview],
            CaseStatuses = [EventReportCaseStatus.Assigned],
            Priority = EventReportPriority.High,
            QueueCode = "safety",
            AssignedModeratorUserId = moderatorUserId,
            ReasonCode = "spam",
            SortBy = "created_at",
            SortDescending = true,
            OpenOnly = false
        }, CancellationToken.None);

        await Assert.That(result.TotalCount).IsEqualTo(1);
        await Assert.That(result.PageNumber).IsEqualTo(2);
        await Assert.That(result.PageSize).IsEqualTo(5);
        var row = result.Items.Single();
        await Assert.That(row.Id).IsEqualTo(report.Id);
        await Assert.That(row.EventId).IsEqualTo(eventId);
        await Assert.That(row.StatusCode).IsEqualTo("under_review");
        await Assert.That(row.PriorityCode).IsEqualTo("high");
        await Assert.That(row.ReasonCode).IsEqualTo("spam");
        await Assert.That(row.ReasonName).IsEqualTo("Spam");
        await Assert.That(row.CurrentCase).IsNotNull();
        await Assert.That(row.CurrentCase!.QueueCode).IsEqualTo("safety");
        await Assert.That(row.CurrentCase.StatusCode).IsEqualTo("assigned");
        await Assert.That(row.CurrentCase.AssignedModeratorUserId).IsEqualTo(moderatorUserId);
        await Assert.That(row.DecisionCount).IsEqualTo(1);
        await Assert.That(row.SignalCount).IsEqualTo(1);

        await Assert.That(capturedSpecification).IsNotNull();
        var nonMatchingReport = CreateReport(tenantId, Guid.CreateVersion7(), EventReportPriority.High);
        var filtered = capturedSpecification!.Apply(new[] { report, nonMatchingReport }.AsQueryable()).ToList();
        await Assert.That(filtered).Count().IsEqualTo(1);
        await Assert.That(filtered[0].Id).IsEqualTo(report.Id);
    }

    [Test]
    public async Task Handle_WithMissingTenantOrEvent_ReturnsEmptyWithoutRepositoryLookup()
    {
        _tenantContext.TenantId.Returns(Guid.Empty);

        var result = await CreateHandler().Handle(new GetModerationReportQueueRequest
        {
            EventId = Guid.Empty,
            PageNumber = -5,
            PageSize = 500
        }, CancellationToken.None);

        await Assert.That(result.Items).IsEmpty();
        await Assert.That(result.TotalCount).IsEqualTo(0);
        await Assert.That(result.PageNumber).IsEqualTo(1);
        await Assert.That(result.PageSize).IsEqualTo(100);
        await _eventReportRepository.DidNotReceive().GetReportQueueAsync(
            Arg.Any<Guid>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<EventReportQuerySpecification>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithInvalidReasonCode_ReturnsEmptyWithoutRepositoryLookup()
    {
        _tenantContext.TenantId.Returns(Guid.CreateVersion7());

        var result = await CreateHandler().Handle(new GetModerationReportQueueRequest
        {
            EventId = Guid.CreateVersion7(),
            ReasonCode = "not a valid reason"
        }, CancellationToken.None);

        await Assert.That(result.Items).IsEmpty();
        await _eventReportRepository.DidNotReceive().GetReportQueueAsync(
            Arg.Any<Guid>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<EventReportQuerySpecification>(),
            Arg.Any<CancellationToken>());
    }

    private GetModerationReportQueueRequestHandler CreateHandler() => new(
        _eventReportRepository,
        _tenantContext);

    private static EventReport CreateReport(Guid tenantId, Guid eventId, EventReportPriority priority)
    {
        return EventReport.Create(
            tenantId,
            eventId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            EventReporterKind.AuthenticatedUser,
            EventReportSourceKind.UserReport,
            "spam",
            "organizer",
            priority,
            EventReportSeverityHint.High,
            reporterContactConsent: true,
            reporterLocale: "en",
            reporterIpHash: "ip-hash",
            reporterUserAgentHash: "ua-hash");
    }
}
