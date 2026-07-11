// ABOUTME: Unit tests for sitemap event query mapping and safety limits.
// ABOUTME: Verifies SEO sitemap generation only receives repository-filtered event entities.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Seo.Handlers.Queries;
using Explore.Application.Features.Seo.Requests.Queries;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Seo;

public sealed class GetSitemapEventsQueryHandlerTests
{
    private readonly IEventRepository _repository = Substitute.For<IEventRepository>();
    private readonly GetSitemapEventsQueryHandler _handler;

    public GetSitemapEventsQueryHandlerTests()
    {
        _handler = new GetSitemapEventsQueryHandler(_repository);
    }

    [Test]
    public async Task Handle_MapsRepositoryEvents_ToSitemapEntries()
    {
        var eventId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2026, 4, 2, 10, 0, 0, DateTimeKind.Utc);

        _repository.GetPublishedPublicEventsForSitemap(50_000, Arg.Any<CancellationToken>())
            .Returns([CreateEvent(eventId, createdAt, updatedAt)]);

        var result = await _handler.Handle(new GetSitemapEventsQuery(), CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].EventId).IsEqualTo(eventId);
        await Assert.That(result[0].LastModifiedAt).IsEqualTo(updatedAt);
    }

    [Test]
    public async Task Handle_UsesCreatedAt_WhenUpdatedAtIsMissing()
    {
        var createdAt = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);

        _repository.GetPublishedPublicEventsForSitemap(10, Arg.Any<CancellationToken>())
            .Returns([CreateEvent(Guid.NewGuid(), createdAt, updatedAt: null)]);

        var result = await _handler.Handle(new GetSitemapEventsQuery(10), CancellationToken.None);

        await Assert.That(result[0].LastModifiedAt).IsEqualTo(createdAt);
    }

    [Test]
    public async Task Handle_ClampsMaxCount_ToSitemapProtocolLimit()
    {
        _repository.GetPublishedPublicEventsForSitemap(50_000, Arg.Any<CancellationToken>())
            .Returns([]);

        await _handler.Handle(new GetSitemapEventsQuery(99_999), CancellationToken.None);

        await _repository.Received(1).GetPublishedPublicEventsForSitemap(50_000, Arg.Any<CancellationToken>());
    }

    private static Explore.Domain.Event CreateEvent(Guid id, DateTime createdAt, DateTime? updatedAt)
    {
        return new Explore.Domain.Event
        {
            Id = id,
            Title = "Sitemap Event",
            Actor = null!,
            Tenant = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }
}
