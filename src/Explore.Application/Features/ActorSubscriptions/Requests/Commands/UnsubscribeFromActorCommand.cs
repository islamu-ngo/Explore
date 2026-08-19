// ABOUTME: CQRS command for unsubscribing the current tenant user from an actor.
// ABOUTME: Preserves the subscription row as status history instead of soft deleting it.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ActorSubscription;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ActorSubscriptions.Requests.Commands;

[AuthorizeResource(ResourceKinds.ActorSubscription, AuthorizationActions.ActorSubscriptions.Delete)]
public class UnsubscribeFromActorCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UnsubscribeFromActorDto Subscription { get; set; }

    public string? ResourceId => Subscription.TargetActorId == Guid.Empty ? null : Subscription.TargetActorId.ToString();

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        Subscription.TargetActorId == Guid.Empty
        ? null
        : new PersonalResourceAuthorizationFacts(Guid.Empty, null);
}
