// ABOUTME: CQRS command for creating a tenant member (user-role assignment).
// ABOUTME: Requires tenant_member Create permission via AuthorizeResource.

using Explore.Application.Authorization;
using Explore.Application.DTOs.TenantMember;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TenantMembers.Requests.Commands;

[AuthorizeResource("tenant_member", AuthorizationActions.Create)]
public class CreateTenantMemberCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateTenantMemberDto TenantMemberDto { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        TenantMemberDto.TenantId != Guid.Empty
            ? new Dictionary<string, object> { ["tenantId"] = TenantMemberDto.TenantId.ToString() }
            : null;
}
