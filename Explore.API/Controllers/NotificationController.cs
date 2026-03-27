// ABOUTME: API controller for notification management (list, detail, read status, delete).
// ABOUTME: All endpoints require authentication — notifications are personal user data.

using Asp.Versioning;
using Explore.Application.DTOs.Notification;
using Explore.Application.Features.Notifications.Requests.Commands;
using Explore.Application.Features.Notifications.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/notification
    [HttpGet(Name = Hateoas.RouteNames.GetNotifications)]
    [EndpointSummary("Get User Notifications")]
    [EndpointDescription("Retrieve a paginated list of notifications for the authenticated user. Supports filtering by read status and notification type.")]
    [ProducesResponseType(typeof(PaginatedResult<NotificationListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResult<NotificationListDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isRead = null,
        [FromQuery] int? notificationTypeId = null,
        [FromQuery] int? notificationScopeId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetUserNotificationsRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            IsRead = isRead,
            NotificationTypeId = notificationTypeId,
            NotificationScopeId = notificationScopeId
        }, cancellationToken);

        return Ok(result);
    }

    // GET: api/notification/{id}
    [HttpGet("{id}", Name = Hateoas.RouteNames.GetNotificationById)]
    [EndpointSummary("Get Notification by ID")]
    [EndpointDescription("Retrieve a specific notification. Returns 404 if not found or doesn't belong to the authenticated user.")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var notification = await _mediator.Send(new GetNotificationByIdRequest { Id = id }, cancellationToken);

        return Ok(notification);
    }

    // GET: api/notification/unread-count
    [HttpGet("unread-count", Name = Hateoas.RouteNames.GetUnreadNotificationCount)]
    [EndpointSummary("Get Unread Notification Count")]
    [EndpointDescription("Returns the number of unread notifications for the authenticated user. Supports optional scope filter (ActorType: User=1, Organization=2, Group=4, System=5). Optimized with partial index.")]
    [ProducesResponseType(typeof(UnreadCountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UnreadCountDto>> GetUnreadCount(
        [FromQuery] int? notificationScopeId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetUnreadCountRequest { NotificationScopeId = notificationScopeId }, cancellationToken);
        return Ok(result);
    }

    // PATCH: api/notification/{id}/read
    [HttpPatch("{id}/read", Name = Hateoas.RouteNames.MarkNotificationAsRead)]
    [EndpointSummary("Mark Notification as Read")]
    [EndpointDescription("Marks a single notification as read. Idempotent — succeeds silently if already read.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> MarkAsRead(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new MarkNotificationAsReadCommand { Id = id }, cancellationToken);

        return Ok(response);
    }

    // POST: api/notification/read-all
    [HttpPost("read-all", Name = Hateoas.RouteNames.MarkAllNotificationsAsRead)]
    [EndpointSummary("Mark All Notifications as Read")]
    [EndpointDescription("Bulk marks all unread notifications as read (YouTube-style). Uses timestamp cutoff to prevent race conditions.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> MarkAllAsRead(CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new MarkAllNotificationsAsReadCommand(), cancellationToken);
        return Ok(response);
    }

    // DELETE: api/notification/{id}
    [HttpDelete("{id}", Name = Hateoas.RouteNames.DeleteNotification)]
    [EndpointSummary("Delete Notification")]
    [EndpointDescription("Soft-deletes a notification for the authenticated user.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeleteNotificationCommand { Id = id }, cancellationToken);

        return NoContent();
    }
}
