// ABOUTME: Resolves private Studio navigation capabilities for an authorized acting actor.
// ABOUTME: Fails closed on invalid actor context or authorization errors and returns no role or event inventory data.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Studio;
using Explore.Application.Exceptions;
using Explore.Application.Features.AiAssistant.Actors;
using Explore.Application.Features.Studio.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.Studio.Handlers.Queries;

public sealed class GetStudioContextQueryHandler(
    IUserContext userContext,
    ITenantContext tenantContext,
    IAiAssistantActorContextService actorContexts,
    IEventRepository events,
    IAuthorizationProvider authorization)
    : IRequestHandler<GetStudioContextQuery, StudioContextDto>
{
    public async Task<StudioContextDto> Handle(GetStudioContextQuery request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.GetRequiredUserId();
        Guid tenantId = tenantContext.TenantId;
        AiAssistantActorContextResolution actor = await actorContexts.ResolveAuthorizedActorAsync(
            tenantId,
            userId,
            request.ActorId,
            cancellationToken);

        if (!actor.Succeeded)
        {
            throw new AuthorizationException(ResourceKinds.Actor, AuthorizationActions.View);
        }

        var context = new StudioContextDto { SelectedActorId = actor.ActorId };
        if (actor.ActorId is not { } actorId)
        {
            return context;
        }

        IReadOnlyList<Event> managedEvents = await events.GetEventsByActorWithDetails(actorId, cancellationToken);
        List<AuthorizationCheck> checks = managedEvents
            .Where(IsPlatformManaged)
            .Select(eventEntity => new AuthorizationCheck(
                ResourceKinds.Event,
                eventEntity.Id.ToString("D"),
                AuthorizationActions.Events.ManageRegistrations,
                ResourceDescriptors.EventAuthorizationTarget.GetResourceAttributes(eventEntity),
                ResourceDescriptors.EventAuthorizationTarget.GetScope(eventEntity)))
            .ToList();
        if (checks.Count == 0)
        {
            return context;
        }

        try
        {
            IReadOnlyList<bool> decisions = await authorization.IsAllowedBatchAsync(checks, cancellationToken);
            if (decisions.Any(allowed => allowed))
            {
                context.AllowedLinkRelations.Add(LinkRelations.ViewRegistrationOrders);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            context.AllowedLinkRelations.Clear();
        }

        return context;
    }

    private static bool IsPlatformManaged(Event eventEntity) =>
        eventEntity.ParticipationConfiguration?.ParticipationHandlingModeId ==
        (int)ParticipationHandlingModeEnum.PlatformManaged;
}
