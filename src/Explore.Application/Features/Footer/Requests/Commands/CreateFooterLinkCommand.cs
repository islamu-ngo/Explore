// ABOUTME: Command to create a new link inside a footer link group.
// ABOUTME: Order is auto-assigned as max+1 within the group.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class CreateFooterLinkCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid GroupId { get; set; }
    public required string Label { get; set; }
    public required string Url { get; set; }
    public bool OpenInNewTab { get; set; }
    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString("D");
    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        TenantId == Guid.Empty
        ? null
        : new TenantScopedAuthorizationFacts(TenantId);

}
