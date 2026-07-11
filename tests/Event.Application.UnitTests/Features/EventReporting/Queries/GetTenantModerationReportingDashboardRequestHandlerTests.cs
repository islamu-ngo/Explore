// ABOUTME: Unit tests for tenant moderation-reporting dashboard query handling.
// ABOUTME: Verifies tenant-bounded aggregate counts stay payload-free and skip repository access without tenant context.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventReporting.Handlers.Queries;
using Explore.Application.Features.EventReporting.Requests.Queries;
using Explore.Domain.Enums;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventReporting.Queries;

public sealed class GetTenantModerationReportingDashboardRequestHandlerTests
{
    private readonly IEventReportRepository _eventReportRepository = Substitute.For<IEventReportRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Reporting:Health:StuckProviderSyncMinutes"] = "45"
        })
        .Build();

    [Test]
    public async Task Handle_WithTenant_ReturnsAggregateQueueAndProviderHealth()
    {
        var tenantId = Guid.CreateVersion7();
        _tenantContext.TenantId.Returns(tenantId);
        _eventReportRepository.CountByTenantAndStatusesAsync(
                tenantId,
                Arg.Is<IReadOnlyCollection<EventReportStatus>>(statuses => Matches(statuses, SubmittedStatuses)),
                Arg.Any<CancellationToken>())
            .Returns(3);
        _eventReportRepository.CountByTenantAndStatusesAsync(
                tenantId,
                Arg.Is<IReadOnlyCollection<EventReportStatus>>(statuses => Matches(statuses, InReviewStatuses)),
                Arg.Any<CancellationToken>())
            .Returns(5);
        _eventReportRepository.CountByTenantAndStatusesAsync(
                tenantId,
                Arg.Is<IReadOnlyCollection<EventReportStatus>>(statuses => Matches(statuses, ClosedStatuses)),
                Arg.Any<CancellationToken>())
            .Returns(8);
        _eventReportRepository.CountCasesByTenantAndStatusesAsync(
                tenantId,
                Arg.Is<IReadOnlyCollection<EventReportCaseStatus>>(statuses => Matches(statuses, OpenCaseStatuses)),
                Arg.Any<CancellationToken>())
            .Returns(2);
        _eventReportRepository.CountCasesByTenantAndStatusesAsync(
                tenantId,
                Arg.Is<IReadOnlyCollection<EventReportCaseStatus>>(statuses => Matches(statuses, AssignedCaseStatuses)),
                Arg.Any<CancellationToken>())
            .Returns(4);
        _eventReportRepository.CountCasesByTenantAndStatusesAsync(
                tenantId,
                Arg.Is<IReadOnlyCollection<EventReportCaseStatus>>(statuses => Matches(statuses, WaitingExternalCaseStatuses)),
                Arg.Any<CancellationToken>())
            .Returns(6);
        _eventReportRepository.CountCasesByTenantAndStatusesAsync(
                tenantId,
                Arg.Is<IReadOnlyCollection<EventReportCaseStatus>>(statuses => Matches(statuses, WaitingReporterCaseStatuses)),
                Arg.Any<CancellationToken>())
            .Returns(7);
        _eventReportRepository.CountCasesByTenantAndStatusesAsync(
                tenantId,
                Arg.Is<IReadOnlyCollection<EventReportCaseStatus>>(statuses => Matches(statuses, DecisionReadyCaseStatuses)),
                Arg.Any<CancellationToken>())
            .Returns(9);
        _eventReportRepository.CountExternalLinksByTenantAndSyncStateAsync(tenantId, EventReportSyncState.Pending, Arg.Any<CancellationToken>()).Returns(11);
        _eventReportRepository.CountExternalLinksByTenantAndSyncStateBeforeAsync(tenantId, EventReportSyncState.Pending, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(12);
        _eventReportRepository.CountExternalLinksByTenantAndSyncStateAsync(tenantId, EventReportSyncState.Failed, Arg.Any<CancellationToken>()).Returns(13);
        _eventReportRepository.CountExternalLinksByTenantAndSyncStateAsync(tenantId, EventReportSyncState.Disabled, Arg.Any<CancellationToken>()).Returns(14);
        _eventReportRepository.CountExternalLinksByTenantAndSyncStateAsync(tenantId, EventReportSyncState.Ignored, Arg.Any<CancellationToken>()).Returns(15);

        var result = await CreateHandler().Handle(new GetTenantModerationReportingDashboardRequest(tenantId), CancellationToken.None);

        await Assert.That(result.TenantId).IsEqualTo(tenantId);
        await Assert.That(result.QueueHealth.SubmittedReports).IsEqualTo(3);
        await Assert.That(result.QueueHealth.InReviewReports).IsEqualTo(5);
        await Assert.That(result.QueueHealth.ClosedReports).IsEqualTo(8);
        await Assert.That(result.QueueHealth.OpenCases).IsEqualTo(2);
        await Assert.That(result.QueueHealth.AssignedCases).IsEqualTo(4);
        await Assert.That(result.QueueHealth.WaitingExternalCases).IsEqualTo(6);
        await Assert.That(result.QueueHealth.WaitingReporterCases).IsEqualTo(7);
        await Assert.That(result.QueueHealth.DecisionReadyCases).IsEqualTo(9);
        await Assert.That(result.ProviderSyncHealth.PendingSyncs).IsEqualTo(11);
        await Assert.That(result.ProviderSyncHealth.StuckPendingSyncs).IsEqualTo(12);
        await Assert.That(result.ProviderSyncHealth.FailedSyncs).IsEqualTo(13);
        await Assert.That(result.ProviderSyncHealth.DisabledSyncs).IsEqualTo(14);
        await Assert.That(result.ProviderSyncHealth.IgnoredSyncs).IsEqualTo(15);
    }

    [Test]
    public async Task Handle_WithoutTenant_ReturnsEmptyDashboardWithoutRepositoryLookup()
    {
        _tenantContext.TenantId.Returns(Guid.Empty);

        var result = await CreateHandler().Handle(new GetTenantModerationReportingDashboardRequest(Guid.Empty), CancellationToken.None);

        await Assert.That(result.TenantId).IsEqualTo(Guid.Empty);
        await Assert.That(result.QueueHealth.SubmittedReports).IsEqualTo(0);
        await Assert.That(result.ProviderSyncHealth.PendingSyncs).IsEqualTo(0);
        await _eventReportRepository.DidNotReceive().CountByTenantAndStatusesAsync(
            Arg.Any<Guid>(),
            Arg.Any<IReadOnlyCollection<EventReportStatus>>(),
            Arg.Any<CancellationToken>());
        await _eventReportRepository.DidNotReceive().CountExternalLinksByTenantAndSyncStateAsync(
            Arg.Any<Guid>(),
            Arg.Any<EventReportSyncState>(),
            Arg.Any<CancellationToken>());
    }

    private GetTenantModerationReportingDashboardRequestHandler CreateHandler() => new(
        _eventReportRepository,
        _tenantContext,
        _configuration);

    private static readonly EventReportStatus[] SubmittedStatuses = [EventReportStatus.Submitted];
    private static readonly EventReportStatus[] InReviewStatuses = [EventReportStatus.Triaged, EventReportStatus.UnderReview, EventReportStatus.Escalated];
    private static readonly EventReportStatus[] ClosedStatuses = [EventReportStatus.Actioned, EventReportStatus.Dismissed, EventReportStatus.Duplicate, EventReportStatus.Closed];
    private static readonly EventReportCaseStatus[] OpenCaseStatuses = [EventReportCaseStatus.Open];
    private static readonly EventReportCaseStatus[] AssignedCaseStatuses = [EventReportCaseStatus.Assigned];
    private static readonly EventReportCaseStatus[] WaitingExternalCaseStatuses = [EventReportCaseStatus.WaitingExternal];
    private static readonly EventReportCaseStatus[] WaitingReporterCaseStatuses = [EventReportCaseStatus.WaitingReporter];
    private static readonly EventReportCaseStatus[] DecisionReadyCaseStatuses = [EventReportCaseStatus.DecisionReady];

    private static bool Matches<T>(IReadOnlyCollection<T>? actual, IReadOnlyCollection<T> expected)
        where T : struct, Enum
    {
        return actual is not null && actual.SequenceEqual(expected);
    }
}
