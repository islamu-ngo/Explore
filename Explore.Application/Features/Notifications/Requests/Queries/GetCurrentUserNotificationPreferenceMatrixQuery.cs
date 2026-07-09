// ABOUTME: Query request for the authenticated user's notification preference matrix.
// ABOUTME: Returns render-ready category, channel, cell, and mute state from the resolver.

using Explore.Application.DTOs.Notification;
using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Queries;

public sealed class GetCurrentUserNotificationPreferenceMatrixQuery : IRequest<NotificationPreferenceMatrixDto>
{
}
