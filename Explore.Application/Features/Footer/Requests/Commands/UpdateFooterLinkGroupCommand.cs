// ABOUTME: Command to update the title and active state of a footer link group.
// ABOUTME: Validates the group belongs to the current tenant before updating.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class UpdateFooterLinkGroupCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid GroupId { get; set; }
    public required string Title { get; set; }
    public bool IsActive { get; set; }
    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString("D");
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => TenantId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["tenantId"] = TenantId.ToString("D"),
            ["groupId"] = GroupId.ToString("D")
        };

}
