// ABOUTME: Command for setting organization-scoped notification preference global mute.
// ABOUTME: Preserves saved channel choices while writing the scoped profile row transactionally.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Commands;

[AuthorizeResource(ResourceKinds.Organization, AuthorizationActions.Update)]
public sealed record SetOrganizationNotificationPreferenceMuteCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid OrganizationId { get; init; }
    public bool IsMuted { get; init; }

    string? ISecureRequest.ResourceId => OrganizationId.ToString();
}
