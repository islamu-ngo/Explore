// ABOUTME: Unit tests for current reporter-owned paged event-report status list reads.
// ABOUTME: Verifies ownership scoping, pagination normalization, and limited DTO projection.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventReporting.Handlers.Queries;
using Explore.Application.Features.EventReporting.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventReporting.Queries;

public sealed class GetMyReportsRequestHandlerTests
{
    private readonly IEventReportRepository _eventReportRepository = Substitute.For<IEventReportRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    [Test]
    public async Task Handle_WhenReporterHasReports_ReturnsPagedLimitedStatusProjection()
    {
        var tenantId = Guid.CreateVersion7();
        var reporterUserId = Guid.CreateVersion7();
        var first = CreateReport(tenantId, Guid.CreateVersion7(), reporterUserId, "spam", DateTime.UtcNow.AddHours(-2));
        var second = CreateReport(tenantId, Guid.CreateVersion7(), reporterUserId, "safety_concern", DateTime.UtcNow.AddHours(-1));
        second.UpdateStatus(EventReportStatus.Actioned, DateTime.UtcNow);
        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(reporterUserId);
        _eventReportRepository.GetByReporterAsync(
                tenantId,
                reporterUserId,
                2,
                5,
                Arg.Any<CancellationToken>())
            .Returns(([first, second], 7));

        var result = await CreateHandler().Handle(
            new GetMyReportsRequest { PageNumber = 2, PageSize = 5 },
            CancellationToken.None);

        await Assert.That(result.PageNumber).IsEqualTo(2);
        await Assert.That(result.PageSize).IsEqualTo(5);
        await Assert.That(result.TotalCount).IsEqualTo(7);
        await Assert.That(result.Items.Count).IsEqualTo(2);
        await Assert.That(result.Items[0].Id).IsEqualTo(first.Id);
        await Assert.That(result.Items[0].ReasonCode).IsEqualTo("spam");
        await Assert.That(result.Items[0].StatusCode).IsEqualTo("submitted");
        await Assert.That(result.Items[1].ReasonCode).IsEqualTo("safety_concern");
        await Assert.That(result.Items[1].StatusCode).IsEqualTo("actioned");
    }

    [Test]
    public async Task Handle_WhenCurrentUserIsMissing_ReturnsEmptyPageWithoutRepositoryLookup()
    {
        _tenantContext.TenantId.Returns(Guid.CreateVersion7());
        _currentUserService.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new GetMyReportsRequest { PageNumber = 0, PageSize = 500 },
            CancellationToken.None);

        await Assert.That(result.PageNumber).IsEqualTo(1);
        await Assert.That(result.PageSize).IsEqualTo(100);
        await Assert.That(result.TotalCount).IsEqualTo(0);
        await Assert.That(result.Items).IsEmpty();
        await _eventReportRepository.DidNotReceive().GetByReporterAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    private GetMyReportsRequestHandler CreateHandler()
    {
        return new GetMyReportsRequestHandler(
            _eventReportRepository,
            _tenantContext,
            _currentUserService);
    }

    private static EventReport CreateReport(
        Guid tenantId,
        Guid eventId,
        Guid reporterUserId,
        string reasonCode,
        DateTime createdAt)
    {
        return EventReport.Create(
            tenantId,
            eventId,
            reporterUserId,
            Guid.CreateVersion7(),
            EventReporterKind.AuthenticatedUser,
            EventReportSourceKind.UserReport,
            reasonCode,
            null,
            EventReportPriority.Normal,
            null,
            true,
            "en",
            "ip-hash",
            "ua-hash",
            createdAt);
    }
}
