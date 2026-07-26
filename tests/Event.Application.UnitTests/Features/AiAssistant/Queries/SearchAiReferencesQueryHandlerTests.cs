// ABOUTME: Unit tests for AI reference search query normalization and DTO mapping.
// ABOUTME: Verifies bounded repository calls and that full event content never leaves the handler.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.AiAssistant.Handlers.Queries;
using Explore.Application.Features.AiAssistant.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using DomainEvent = Explore.Domain.Event;

namespace Event.Application.UnitTests.Features.AiAssistant.Queries;

public sealed class SearchAiReferencesQueryHandlerTests
{
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly IActorRepository _actorRepository = Substitute.For<IActorRepository>();
    private readonly SearchAiReferencesQueryHandler _handler;

    public SearchAiReferencesQueryHandlerTests()
    {
        _actorRepository.SearchAiReferenceActorsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _handler = new SearchAiReferencesQueryHandler(_eventRepository, _actorRepository);
    }

    [Test]
    public async Task Handle_WhenSearchTermIsTooShort_ReturnsEmptyWithoutRepositoryCall()
    {
        var result = await _handler.Handle(new SearchAiReferencesQuery { SearchTerm = " a ", Limit = 10 }, CancellationToken.None);

        await Assert.That(result).IsEmpty();
        await _eventRepository.DidNotReceiveWithAnyArgs().SearchAiReferenceEventsAsync(default!, default, default);
        await _actorRepository.DidNotReceiveWithAnyArgs().SearchAiReferenceActorsAsync(default!, default, default);
    }

    [Test]
    public async Task Handle_ClampsLimitAndMapsLightweightEventFields()
    {
        var eventId = Guid.NewGuid();
        var events = new List<DomainEvent>
        {
            new()
            {
                Id = eventId,
                Title = "Community Iftar",
                Subtitle = "A short card summary",
                Description = "Repository description that should not replace subtitle.",
                Content = "Full internal event content must not be returned.",
                FirstSessionDate = new DateOnly(2026, 3, 1),
                LastSessionDate = new DateOnly(2026, 3, 2),
                EventStatusId = 2,
                EventStatus = new EventStatus { Id = 2, MasterCode = "published", FullName = "Published" },
                VisibilityTypeId = 1,
                VisibilityType = new VisibilityType { Id = 1, MasterCode = "public", FullName = "Public" },
                EventFormatId = 1,
                EventFormat = new EventFormat { Id = 1, MasterCode = "in_person", FullName = "In person" },
                ActorId = Guid.NewGuid(),
                Actor = null!,
                TenantId = Guid.NewGuid(),
                Tenant = null!,
            }
        };
        _eventRepository.SearchAiReferenceEventsAsync("iftar", SearchAiReferencesQueryHandler.MaxLimit, Arg.Any<CancellationToken>())
            .Returns(events);

        var result = await _handler.Handle(new SearchAiReferencesQuery { SearchTerm = "  iftar  ", Limit = 999 }, CancellationToken.None);

        await _eventRepository.Received(1).SearchAiReferenceEventsAsync(
            "iftar",
            SearchAiReferencesQueryHandler.MaxLimit,
            Arg.Any<CancellationToken>());
        await Assert.That(result.Count).IsEqualTo(1);
        var reference = result.Single();
        await Assert.That(reference.Kind).IsEqualTo("Event");
        await Assert.That(reference.ReferenceId).IsEqualTo(eventId);
        await Assert.That(reference.DisplayName).IsEqualTo("Community Iftar");
        await Assert.That(reference.Summary).IsEqualTo("A short card summary");
        await Assert.That(reference.FirstSessionDate).IsEqualTo(new DateOnly(2026, 3, 1));
        await Assert.That(reference.LastSessionDate).IsEqualTo(new DateOnly(2026, 3, 2));
        await Assert.That(reference.EventStatus).IsEqualTo("Published");
        await Assert.That(reference.Visibility).IsEqualTo("Public");
        await Assert.That(reference.Format).IsEqualTo("In person");
        await Assert.That(reference.Summary).DoesNotContain("Full internal event content");
    }

    [Test]
    public async Task Handle_MapsActorAndOrganizationReferencesWithLightweightFields()
    {
        var actorId = Guid.NewGuid();
        var organizationActorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var organizationActor = new Actor
        {
            Id = organizationActorId,
            ActorTypeId = (int)ActorTypeEnum.Organization,
            ActorType = new ActorType { Id = (int)ActorTypeEnum.Organization, MasterCode = "ORGANIZATION", FullName = "Organization" },
            OrganizationId = organizationId,
            Pii = new ActorPii { DisplayName = "Community Center" },
            Description = "A short organization summary."
        };
        var userActor = new Actor
        {
            Id = actorId,
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = new ActorType { Id = (int)ActorTypeEnum.User, MasterCode = "USER", FullName = "User" },
            UserId = Guid.NewGuid(),
            Pii = new ActorPii { DisplayName = "Amina Speaker" }
        };
        userActor.AtprotoIdentities.Add(new AtprotoIdentity
        {
            Id = Guid.CreateVersion7(),
            Did = "did:plc:amina",
            ActorId = actorId,
            Actor = userActor,
            Handle = "amina.example",
            PdsHost = "https://pds.example.test",
            IsActive = true,
            LastResolvedAt = DateTime.UtcNow
        });
        var actors = new List<Actor> { organizationActor, userActor };
        _eventRepository.SearchAiReferenceEventsAsync("amina", 10, Arg.Any<CancellationToken>())
            .Returns([]);
        _actorRepository.SearchAiReferenceActorsAsync("amina", 10, Arg.Any<CancellationToken>())
            .Returns(actors);

        var result = await _handler.Handle(new SearchAiReferencesQuery { SearchTerm = "amina", Limit = 10 }, CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(2);
        var organization = result.Single(reference => reference.Kind == "Organization");
        await Assert.That(organization.ReferenceId).IsEqualTo(organizationId);
        await Assert.That(organization.DisplayName).IsEqualTo("Community Center");
        await Assert.That(organization.Summary).IsEqualTo("A short organization summary.");
        var actor = result.Single(reference => reference.Kind == "Actor");
        await Assert.That(actor.ReferenceId).IsEqualTo(actorId);
        await Assert.That(actor.DisplayName).IsEqualTo("Amina Speaker");
        await Assert.That(actor.Summary).IsEqualTo("@amina.example");
    }

    [Test]
    public async Task Handle_WhenLimitIsNotPositive_UsesDefaultLimit()
    {
        _eventRepository.SearchAiReferenceEventsAsync("lecture", SearchAiReferencesQueryHandler.DefaultLimit, Arg.Any<CancellationToken>())
            .Returns([]);

        await _handler.Handle(new SearchAiReferencesQuery { SearchTerm = "lecture", Limit = 0 }, CancellationToken.None);

        await _eventRepository.Received(1).SearchAiReferenceEventsAsync(
            "lecture",
            SearchAiReferencesQueryHandler.DefaultLimit,
            Arg.Any<CancellationToken>());
    }
}
