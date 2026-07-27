// ABOUTME: Command for saving group-scoped notification preference cells.
// ABOUTME: Carries route authority into resource authorization and transactional preference writes.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Notification;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Commands;

[AuthorizeResource(ResourceKinds.Group, AuthorizationActions.Update)]
public sealed class UpdateGroupNotificationPreferenceMatrixCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid GroupId { get; set; }
    public IReadOnlyList<UpdateNotificationPreferenceCellDto>? Cells { get; set; }

    string? ISecureRequest.ResourceId => GroupId.ToString();
}
