// ABOUTME: Command for saving organization-scoped notification preference cells.
// ABOUTME: Carries route authority into resource authorization and transactional preference writes.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Notification;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Commands;

[AuthorizeResource(ResourceKinds.Organization, AuthorizationActions.Update)]
public sealed class UpdateOrganizationNotificationPreferenceMatrixCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid OrganizationId { get; set; }
    public IReadOnlyList<UpdateNotificationPreferenceCellDto>? Cells { get; set; }

    string? ISecureRequest.ResourceId => OrganizationId.ToString();
}
