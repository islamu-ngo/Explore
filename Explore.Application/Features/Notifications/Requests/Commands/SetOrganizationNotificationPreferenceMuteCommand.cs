// ABOUTME: Command for setting organization-scoped notification preference global mute.
// ABOUTME: Preserves saved channel choices while writing the scoped profile row transactionally.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Commands;

[AuthorizeResource(ResourceKinds.Organization, AuthorizationActions.Update)]
public sealed class SetOrganizationNotificationPreferenceMuteCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid OrganizationId { get; set; }
    public bool IsMuted { get; set; }

    string? ISecureRequest.ResourceId => OrganizationId.ToString();
}
