// ABOUTME: Query request for a single notification by ID.
// ABOUTME: Handler verifies the notification belongs to the authenticated user.

using Explore.Application.DTOs.Notification;
using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Queries;

public sealed record GetNotificationByIdRequest(Guid Id = default) : IRequest<NotificationDto?>;
