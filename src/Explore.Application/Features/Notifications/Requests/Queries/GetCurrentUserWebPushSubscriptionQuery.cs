// ABOUTME: Query request for the authenticated user's Web Push subscription on one browser device.
// ABOUTME: Returns a safe status DTO without endpoint or browser key material.

using Explore.Application.DTOs.Notification;
using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Queries;

public sealed record GetCurrentUserWebPushSubscriptionQuery : IRequest<WebPushSubscriptionDto?>
{
    public required string DeviceIdentifier { get; init; }
}
