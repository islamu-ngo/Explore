// ABOUTME: Unit tests for the event moderation history management query.
// ABOUTME: Verifies tenant-scoped audit mapping without exposing event content or storage identifiers.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Events.Handlers.Queries;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Events.Queries;

public sealed class GetEventModerationHistoryRequestHandlerTests
{
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly IEventModerationRecordRepository _moderationRecordRepository = Substitute.For<IEventModerationRecordRepository>();
    private readonly GetEventModerationHistoryRequestHandler _handler;

    public GetEventModerationHistoryRequestHandlerTests()
    {
        _handler = new GetEventModerationHistoryRequestHandler(
            _eventRepository,
            _moderationRecordRepository);
    }

    [Test]
    public async Task Handle_WhenEventDoesNotExist_ReturnsNullWithoutReadingHistory()
    {
        var eventId = Guid.NewGuid();
        _eventRepository.GetById(eventId).Returns((Explore.Domain.Event?)null);

        var result = await _handler.Handle(new GetEventModerationHistoryRequest { Id = eventId }, CancellationToken.None);

        await Assert.That(result).IsNull();
        await _moderationRecordRepository.DidNotReceiveWithAnyArgs()
            .GetByEventAsync(default, default, default);
    }

    [Test]
    public async Task Handle_WhenEventExists_MapsSafeModerationHistory()
    {
        var @event = CreateEvent();
        var moderatorUserId = Guid.NewGuid();
        var lightRecord = EventModerationRecord.CreateLightModeration(
            Guid.CreateVersion7(),
            @event.TenantId,
            @event.Id,
            moderatorUserId,
            "community_safety_review",
            (int)EventStatusEnum.Published,
            "case-light-1",
            DateTimeOffset.UtcNow.AddMinutes(-10));
        var heavyRecord = EventModerationRecord.CreateHeavyRedaction(
            Guid.CreateVersion7(),
            @event.TenantId,
            @event.Id,
            moderatorUserId,
            "illegal_image",
            (int)EventStatusEnum.Published,
            "case-heavy-1",
            DateTimeOffset.UtcNow.AddMinutes(-5));
        var unmoderationRecord = EventModerationRecord.CreateUnmoderation(
            Guid.CreateVersion7(),
            lightRecord,
            moderatorUserId,
            "appeal_approved",
            "case-unmoderate-1",
            DateTimeOffset.UtcNow);

        _eventRepository.GetById(@event.Id).Returns(@event);
        _moderationRecordRepository.GetByEventAsync(@event.TenantId, @event.Id, Arg.Any<CancellationToken>())
            .Returns([unmoderationRecord, heavyRecord, lightRecord]);

        var result = await _handler.Handle(new GetEventModerationHistoryRequest { Id = @event.Id }, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!).Count().IsEqualTo(3);
        await _moderationRecordRepository.Received(1).GetByEventAsync(
            @event.TenantId,
            @event.Id,
            Arg.Any<CancellationToken>());

        var first = result![0];
        await Assert.That(first.Id).IsEqualTo(unmoderationRecord.Id);
        await Assert.That(first.EventId).IsEqualTo(@event.Id);
        await Assert.That(first.ModeratorUserId).IsEqualTo(moderatorUserId);
        await Assert.That(first.ActionKindId).IsEqualTo((int)EventModerationActionKind.Unmoderated);
        await Assert.That(first.ActionKindName).IsEqualTo(nameof(EventModerationActionKind.Unmoderated));
        await Assert.That(first.ReasonCode).IsEqualTo("appeal_approved");
        await Assert.That(first.PreviousStatusId).IsEqualTo((int)EventStatusEnum.Moderated);
        await Assert.That(first.PreviousStatusName).IsEqualTo(nameof(EventStatusEnum.Moderated));
        await Assert.That(first.ResultingStatusId).IsEqualTo((int)EventStatusEnum.Published);
        await Assert.That(first.ResultingStatusName).IsEqualTo(nameof(EventStatusEnum.Published));
        await Assert.That(first.IsIrreversible).IsFalse();
        await Assert.That(first.AllowsUnmoderation).IsFalse();
        await Assert.That(first.SourceModerationRecordId).IsEqualTo(lightRecord.Id);
        await Assert.That(first.CorrelationId).IsEqualTo("case-unmoderate-1");

        var heavy = result![1];
        await Assert.That(heavy.ActionKindName).IsEqualTo(nameof(EventModerationActionKind.HeavyRedacted));
        await Assert.That(heavy.IsIrreversible).IsTrue();
        await Assert.That(heavy.AllowsUnmoderation).IsFalse();

        var projectedPropertyNames = typeof(Explore.Application.DTOs.Event.EventModerationHistoryDto)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        await Assert.That(projectedPropertyNames).DoesNotContain("Title");
        await Assert.That(projectedPropertyNames).DoesNotContain("Description");
        await Assert.That(projectedPropertyNames).DoesNotContain("Content");
        await Assert.That(projectedPropertyNames).DoesNotContain("Slug");
        await Assert.That(projectedPropertyNames).DoesNotContain("FeaturedImageId");
        await Assert.That(projectedPropertyNames).DoesNotContain("BackgroundImageId");
        await Assert.That(projectedPropertyNames).DoesNotContain("ObjectKey");
        await Assert.That(projectedPropertyNames).DoesNotContain("Uri");
    }

    private static Explore.Domain.Event CreateEvent() => new(EventStatusEnum.Moderated)
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        Tenant = null!,
        ActorId = Guid.NewGuid(),
        Actor = null!,
        Title = "Sensitive title must not be projected",
        Slug = "sensitive-title",
        Description = "Sensitive description must not be projected",
        EventStatus = null!,
        VisibilityTypeId = 1,
        VisibilityType = null!,
        EventFormatId = 1,
        EventFormat = null!
    };
}
