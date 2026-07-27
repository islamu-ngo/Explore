// ABOUTME: CQRS command for updating the current user's actor subscription notification level.
// ABOUTME: Requires the observed concurrency stamp to fail closed on stale writes.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ActorSubscription;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ActorSubscriptions.Requests.Commands;

[AuthorizeResource(ResourceKinds.ActorSubscription, AuthorizationActions.ActorSubscriptions.Update)]
public class UpdateActorSubscriptionNotificationLevelCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TargetActorId { get; set; }
    public required UpdateActorSubscriptionNotificationLevelDto Patch { get; set; }

    public string? ResourceId => TargetActorId == Guid.Empty ? null : TargetActorId.ToString();

    public IDictionary<string, object>? ResourceAttributes => TargetActorId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["targetActorId"] = TargetActorId.ToString()
        };
}
