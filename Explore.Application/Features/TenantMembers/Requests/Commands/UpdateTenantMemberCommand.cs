// ABOUTME: CQRS command for updating a tenant member's role assignment.
// ABOUTME: Requires tenant_member Update permission via AuthorizeResource.

using Explore.Application.Authorization;
using Explore.Application.DTOs.TenantMember;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TenantMembers.Requests.Commands;

[AuthorizeResource("tenant_member", AuthorizationActions.Update)]
public class UpdateTenantMemberCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateTenantMemberDto TenantMemberDto { get; set; }

    string? ISecureRequest.ResourceId => TenantMemberDto.Id.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        TenantMemberDto.TenantId != Guid.Empty
            ? new Dictionary<string, object> { ["tenantId"] = TenantMemberDto.TenantId.ToString() }
            : null;
}
