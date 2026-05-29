// ABOUTME: Query request for the current user's subscription to one actor.
// ABOUTME: Handler resolves the caller through TenantUser before reading subscription state.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ActorSubscription;
using MediatR;

namespace Explore.Application.Features.ActorSubscriptions.Requests.Queries;

[AuthorizeResource(ResourceKinds.ActorSubscription, AuthorizationActions.ActorSubscriptions.View)]
public class GetActorSubscriptionRequest : IRequest<ActorSubscriptionDto?>, ISecureRequest
{
    public Guid TargetActorId { get; set; }

    public string? ResourceId => TargetActorId == Guid.Empty ? null : TargetActorId.ToString();

    public IDictionary<string, object>? ResourceAttributes => TargetActorId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["targetActorId"] = TargetActorId.ToString()
        };
}
