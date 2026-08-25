// ABOUTME: Command request for registering the authenticated user's browser Web Push subscription.
// ABOUTME: Tenant and user ownership are taken from server context, never from client-supplied fields.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Commands;

public sealed record SubscribeCurrentUserWebPushSubscriptionCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required string DeviceIdentifier { get; init; }
    public required string Endpoint { get; init; }
    public required string P256Dh { get; init; }
    public required string Auth { get; init; }
    public DateTime? ExpirationTime { get; init; }
}
