// ABOUTME: Command for setting group-scoped notification preference global mute.
// ABOUTME: Preserves saved channel choices while writing the scoped profile row transactionally.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Commands;

[AuthorizeResource(ResourceKinds.Group, AuthorizationActions.Update)]
public sealed class SetGroupNotificationPreferenceMuteCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid GroupId { get; set; }
    public bool IsMuted { get; set; }

    string? ISecureRequest.ResourceId => GroupId.ToString();
}
