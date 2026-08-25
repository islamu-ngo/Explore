// ABOUTME: Command request for removing one authenticated-user Web Push subscription.
// ABOUTME: Requires the subscription id to be owned by the current tenant/user before deactivation.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Commands;

public sealed record UnsubscribeCurrentUserWebPushSubscriptionCommand(Guid SubscriptionId = default) : IRequest<BaseCommandResponse<Guid>>;
