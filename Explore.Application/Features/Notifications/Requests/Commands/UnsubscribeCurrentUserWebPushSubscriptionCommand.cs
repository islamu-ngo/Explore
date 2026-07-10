// ABOUTME: Command request for removing one authenticated-user Web Push subscription.
// ABOUTME: Requires the subscription id to be owned by the current tenant/user before deactivation.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Commands;

public sealed class UnsubscribeCurrentUserWebPushSubscriptionCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid SubscriptionId { get; init; }
}
