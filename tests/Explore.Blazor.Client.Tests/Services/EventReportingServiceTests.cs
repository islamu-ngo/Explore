// ABOUTME: Unit tests for the reporter-facing EventReportingService wrapper.
// ABOUTME: Verifies HAL pagination mapping and resilient fallbacks around generated API calls.

using Explore.Blazor.Client.Contracts.Services.EventReporting;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class EventReportingServiceTests
{
    private readonly IEventApiClient _apiClient = Substitute.For<IEventApiClient>();
    private readonly ILogger<EventReportingService> _logger = Substitute.For<ILogger<EventReportingService>>();

    [Test]
    public async Task GetMyReportsAsync_WhenApiSucceeds_ReturnsPagedHalReportResources()
    {
        var report = CreateReportResource("submitted", "Submitted", "spam", "Spam");
        _apiClient.GetMyEventReportsAsync(
                Arg.Any<int?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new HalCollectionResourceOfMyEventReportDto
            {
                PageNumber = 2,
                PageSize = 10,
                TotalCount = 21,
                TotalPages = 3,
                HasPrevious = true,
                HasNext = true,
                _embedded = new HalCollectionEmbeddedOfMyEventReportDto
                {
                    Items = [report]
                }
            });

        var result = await CreateService().GetMyReportsAsync(2, 10, CancellationToken.None);

        await Assert.That(result.PageNumber).IsEqualTo(2);
        await Assert.That(result.PageSize).IsEqualTo(10);
        await Assert.That(result.TotalCount).IsEqualTo(21);
        await Assert.That(result.TotalPages).IsEqualTo(3);
        await Assert.That(result.HasPrevious).IsTrue();
        await Assert.That(result.HasNext).IsTrue();
        await Assert.That(result.Reports.Count).IsEqualTo(1);
        await Assert.That(result.Reports[0].Id).IsEqualTo(report.Id);
        await Assert.That(result.Reports[0].ReasonCode).IsEqualTo("spam");

        await _apiClient.Received(1).GetMyEventReportsAsync(
            2,
            10,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetMyReportsAsync_WhenApiThrows_ReturnsNormalizedEmptyPage()
    {
        _apiClient.GetMyEventReportsAsync(
                Arg.Any<int?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Server Error", 500));

        var result = await CreateService().GetMyReportsAsync(3, 15, CancellationToken.None);

        await Assert.That(result.Reports).IsEmpty();
        await Assert.That(result.PageNumber).IsEqualTo(3);
        await Assert.That(result.PageSize).IsEqualTo(15);
        await Assert.That(result.TotalCount).IsEqualTo(0);
        await Assert.That(result.HasPrevious).IsFalse();
        await Assert.That(result.HasNext).IsFalse();
    }

    [Test]
    public async Task GetMyReportAsync_WhenReportNotFound_ReturnsNull()
    {
        var reportId = Guid.NewGuid();
        _apiClient.GetMyEventReportAsync(
                reportId,
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not Found", 404));

        var result = await CreateService().GetMyReportAsync(reportId, CancellationToken.None);

        await Assert.That(result).IsNull();
    }

    private EventReportingService CreateService() => new(_apiClient, _logger);

    private static HalResourceOfMyEventReportDto CreateReportResource(
        string statusCode,
        string statusName,
        string reasonCode,
        string reasonName)
    {
        return HalLinkTestFactory.WithLinks(new HalResourceOfMyEventReportDto
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            StatusId = 1,
            StatusCode = statusCode,
            StatusName = statusName,
            ReasonId = 1,
            ReasonCode = reasonCode,
            ReasonName = reasonName,
            SubmittedAtUtc = DateTimeOffset.UtcNow
        }, new HalLinkTestLink("self", "/api/event-reports/my/report", "GET"));
    }

    private static ApiException CreateApiException(string message, int statusCode)
    {
        return new ApiException(
            message,
            statusCode,
            string.Empty,
            new Dictionary<string, IEnumerable<string>>(),
            new InvalidOperationException(message));
    }
}
