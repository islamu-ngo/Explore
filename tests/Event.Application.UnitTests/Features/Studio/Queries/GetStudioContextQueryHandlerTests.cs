// ABOUTME: Unit tests for the private actor-scoped Studio context query.
// ABOUTME: Proves unauthorized actor hints fail closed and context data remains link-only.

using System.Text.Json;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Ai;
using Explore.Application.DTOs.Studio;
using Explore.Application.Exceptions;
using Explore.Application.Features.AiAssistant.Actors;
using Explore.Application.Features.Studio.Handlers.Queries;
using Explore.Application.Features.Studio.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using EventEntity = Explore.Domain.Event;

namespace Event.Application.UnitTests.Features.Studio.Queries;

public sealed class GetStudioContextQueryHandlerTests
{
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IAiAssistantActorContextService _actorContexts = Substitute.For<IAiAssistantActorContextService>();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IAuthorizationProvider _authorization = Substitute.For<IAuthorizationProvider>();

    public GetStudioContextQueryHandlerTests()
    {
        _userContext.GetRequiredUserId().Returns(_userId);
        _tenantContext.TenantId.Returns(_tenantId);
    }

    [Test]
    public async Task Handle_UnauthorizedActorHint_FailsClosedBeforeReadingEvents()
    {
        Guid requestedActorId = Guid.CreateVersion7();
        _actorContexts.ResolveAuthorizedActorAsync(_tenantId, _userId, requestedActorId, Arg.Any<CancellationToken>())
            .Returns(AiAssistantActorContextResolution.Failure(
                "actor_context_not_authorized",
                "Actor is not available to the current user.",
                []));

        await Assert.That(async () => await CreateHandler().Handle(
            new GetStudioContextQuery(requestedActorId),
            CancellationToken.None)).Throws<AuthorizationException>();
        _ = _events.DidNotReceive().GetEventsByActorWithDetails(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_NoAuthorizedActor_ReturnsNoRegistrationOrdersRelationCandidate()
    {
        _actorContexts.ResolveAuthorizedActorAsync(_tenantId, _userId, null, Arg.Any<CancellationToken>())
            .Returns(AiAssistantActorContextResolution.Success(null, []));

        StudioContextDto result = await CreateHandler().Handle(new GetStudioContextQuery(), CancellationToken.None);

        await Assert.That(result.SelectedActorId).IsNull();
        await Assert.That(result.AllowedLinkRelations).IsEmpty();
        _ = _events.DidNotReceive().GetEventsByActorWithDetails(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StudioContextDto_SerializesNoActorAuthorityOrTenantInventory()
    {
        var dto = new StudioContextDto { SelectedActorId = Guid.CreateVersion7() };

        string json = JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        await Assert.That(json).DoesNotContain("actor");
        await Assert.That(json).DoesNotContain("tenant");
        await Assert.That(json).DoesNotContain("registration");
    }

    [Test]
    public async Task Handle_AuthorizedActorWithPlatformManagedEvent_ExposesOrderCapability()
    {
        Guid actorId = Guid.CreateVersion7();
        EventEntity managedEvent = CreateEvent(actorId, ParticipationHandlingModeEnum.PlatformManaged);
        _actorContexts.ResolveAuthorizedActorAsync(_tenantId, _userId, actorId, Arg.Any<CancellationToken>())
            .Returns(AiAssistantActorContextResolution.Success(actorId, []));
        _events.GetEventsByActorWithDetails(actorId, Arg.Any<CancellationToken>()).Returns([managedEvent]);
        _authorization.IsAllowedBatchAsync(Arg.Any<IReadOnlyList<AuthorizationCheck>>(), Arg.Any<CancellationToken>())
            .Returns([true]);

        StudioContextDto result = await CreateHandler().Handle(new GetStudioContextQuery(actorId), CancellationToken.None);

        await Assert.That(result.AllowedLinkRelations).Contains(LinkRelations.ViewRegistrationOrders);
        await _authorization.Received(1).IsAllowedBatchAsync(
            Arg.Is<IReadOnlyList<AuthorizationCheck>>(checks =>
                checks.Count == 1 &&
                checks[0].ResourceKind == ResourceKinds.Event &&
                checks[0].Action == AuthorizationActions.Events.ManageRegistrations &&
                checks[0].ResourceId == managedEvent.Id.ToString("D")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ExternalManagedEvent_ExposesNoOrderCapability()
    {
        Guid actorId = Guid.CreateVersion7();
        _actorContexts.ResolveAuthorizedActorAsync(_tenantId, _userId, actorId, Arg.Any<CancellationToken>())
            .Returns(AiAssistantActorContextResolution.Success(actorId, []));
        _events.GetEventsByActorWithDetails(actorId, Arg.Any<CancellationToken>())
            .Returns([CreateEvent(actorId, ParticipationHandlingModeEnum.ExternalManaged)]);

        StudioContextDto result = await CreateHandler().Handle(new GetStudioContextQuery(actorId), CancellationToken.None);

        await Assert.That(result.AllowedLinkRelations).IsEmpty();
        _ = await _authorization.DidNotReceive().IsAllowedBatchAsync(
            Arg.Any<IReadOnlyList<AuthorizationCheck>>(),
            Arg.Any<CancellationToken>());
    }

    private GetStudioContextQueryHandler CreateHandler() => new(
        _userContext,
        _tenantContext,
        _actorContexts,
        _events,
        _authorization);

    private EventEntity CreateEvent(Guid actorId, ParticipationHandlingModeEnum mode) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = _tenantId,
        ActorId = actorId,
        Title = "Registration event",
        Actor = null!,
        Tenant = null!,
        EventStatus = null!,
        VisibilityType = null!,
        EventFormat = null!,
        ParticipationConfiguration = EventParticipationConfiguration.Create(
            Guid.CreateVersion7(),
            _tenantId,
            (int)mode,
            (int)AdvanceRegistrationObligationEnum.Required,
            mode == ParticipationHandlingModeEnum.PlatformManaged
                ? (int)IdentityAccessModeEnum.AccountRequired
                : null,
            null,
            DateTime.UtcNow)
    };
}
