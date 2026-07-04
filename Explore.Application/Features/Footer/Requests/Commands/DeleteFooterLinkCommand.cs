// ABOUTME: Command to delete a single footer link from a group.
// ABOUTME: Validates the link's parent group belongs to the current tenant.

using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class DeleteFooterLinkCommand : IRequest<bool>, ISecureRequest
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid LinkId { get; set; }
    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString("D");
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => TenantId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["tenantId"] = TenantId.ToString("D"),
            ["linkId"] = LinkId.ToString("D")
        };

}
