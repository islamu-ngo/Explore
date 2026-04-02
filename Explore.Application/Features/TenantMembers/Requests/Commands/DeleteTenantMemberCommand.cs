// ABOUTME: CQRS command for deleting a tenant member.
// ABOUTME: Requires tenant_member Delete permission via AuthorizeResource.

using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.TenantMembers.Requests.Commands;

[AuthorizeResource("tenant_member", AuthorizationActions.Delete)]
public class DeleteTenantMemberCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
