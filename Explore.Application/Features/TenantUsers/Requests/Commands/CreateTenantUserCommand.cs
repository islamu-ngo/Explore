using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.TenantUser;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TenantUsers.Requests.Commands;

[AuthorizeResource("tenant_user", PermissionAction.Create)]
public class CreateTenantUserCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateTenantUserDto TenantUserDto { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        TenantUserDto.TenantId != Guid.Empty
            ? new Dictionary<string, object> { ["tenantId"] = TenantUserDto.TenantId.ToString() }
            : null;
}
