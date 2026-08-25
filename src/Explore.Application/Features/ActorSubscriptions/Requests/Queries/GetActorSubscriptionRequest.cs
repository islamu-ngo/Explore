// ABOUTME: Query request for the current user's subscription to one actor.
// ABOUTME: Handler resolves the caller through TenantUser before reading subscription state.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ActorSubscription;
using MediatR;

namespace Explore.Application.Features.ActorSubscriptions.Requests.Queries;

[AuthorizeResource(ResourceKinds.ActorSubscription, AuthorizationActions.ActorSubscriptions.View)]
public sealed record GetActorSubscriptionRequest : IRequest<ActorSubscriptionDto?>, ISecureRequest
{
    public Guid TargetActorId { get; init; }

    public string? ResourceId => TargetActorId == Guid.Empty ? null : TargetActorId.ToString();

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        TargetActorId == Guid.Empty
        ? null
        : new PersonalResourceAuthorizationFacts(Guid.Empty, null);
}
