using Explore.Application.Authorization;
using Explore.Application.DTOs.TenantUser;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TenantUsers.Requests.Commands;

[AuthorizeResource("tenant_user", PermissionAction.Update)]
public class UpdateTenantUserCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateTenantUserDto TenantUserDto { get; set; }

    string? ISecureRequest.ResourceId => TenantUserDto.Id.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        TenantUserDto.TenantId != Guid.Empty
            ? new Dictionary<string, object> { ["tenantId"] = TenantUserDto.TenantId.ToString() }
            : null;
}
