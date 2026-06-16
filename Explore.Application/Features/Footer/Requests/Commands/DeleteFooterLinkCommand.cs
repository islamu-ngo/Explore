// ABOUTME: Command to delete a single footer link from a group.
// ABOUTME: Validates the link's parent group belongs to the current tenant.

using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class DeleteFooterLinkCommand : IRequest<bool>, ISecureRequest
{
    public Guid UserId { get; set; }
    public Guid LinkId { get; set; }
    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;

}
