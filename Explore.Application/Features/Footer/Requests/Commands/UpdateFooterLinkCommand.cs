// ABOUTME: Command to update a footer link's label, URL, and display options.
// ABOUTME: Validates the link's parent group belongs to the current tenant.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class UpdateFooterLinkCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid LinkId { get; set; }
    public required string Label { get; set; }
    public required string Url { get; set; }
    public bool OpenInNewTab { get; set; }
    public bool IsActive { get; set; }
    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString("D");
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => TenantId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["tenantId"] = TenantId.ToString("D"),
            ["linkId"] = LinkId.ToString("D")
        };

}
