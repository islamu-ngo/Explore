// ABOUTME: Query request for paginated current-user actor subscriptions.
// ABOUTME: Returns only subscription rows owned by the authenticated tenant user.

using Explore.Application.Authorization;
using Explore.Application.DTOs.ActorSubscription;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ActorSubscriptions.Requests.Queries;

[AuthorizeResource(ResourceKinds.ActorSubscription, AuthorizationActions.ActorSubscriptions.View)]
public class GetActorSubscriptionsRequest : IRequest<PaginatedResult<ActorSubscriptionListDto>>, ISecureRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public string? ResourceId => "current-user-subscriptions";
}
