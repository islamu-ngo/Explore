// ABOUTME: Command to reorder footer link groups for the current tenant.
// ABOUTME: Accepts an ordered list of group IDs and updates their Order properties.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class ReorderFooterLinkGroupsCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    /// <summary>Group IDs in the desired display order (first = 0).</summary>
    public required List<Guid> OrderedGroupIds { get; set; }
    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString("D");
    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        TenantId == Guid.Empty
        ? null
        : new TenantScopedAuthorizationFacts(TenantId);

}
