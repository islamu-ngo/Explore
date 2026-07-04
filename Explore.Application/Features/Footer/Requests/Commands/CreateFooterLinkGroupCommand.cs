// ABOUTME: Command to create a new footer link group for the current tenant.
// ABOUTME: Carries tenant context for resource authorization before auto-assigning order.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class CreateFooterLinkGroupCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public required string Title { get; set; }
    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString("D");
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => TenantId == Guid.Empty
        ? null
        : new Dictionary<string, object> { ["tenantId"] = TenantId.ToString("D") };

}
