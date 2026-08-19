// ABOUTME: CQRS command for subscribing the current tenant user to an actor.
// ABOUTME: Idempotently reactivates an existing durable subscription row.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ActorSubscription;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ActorSubscriptions.Requests.Commands;

[AuthorizeResource(ResourceKinds.ActorSubscription, AuthorizationActions.ActorSubscriptions.Create)]
public class SubscribeToActorCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required SubscribeToActorDto Subscription { get; set; }

    public string? ResourceId => Subscription.TargetActorId == Guid.Empty ? null : Subscription.TargetActorId.ToString();

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        Subscription.TargetActorId == Guid.Empty
        ? null
        : new PersonalResourceAuthorizationFacts(Guid.Empty, null);
}
