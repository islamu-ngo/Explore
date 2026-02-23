using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Tenants.Requests.Commands;

/// <summary>
/// Command to update an existing tenant.
/// Returns the ID of the updated tenant.
/// </summary>
[AuthorizeResource("tenant", PermissionAction.Update)]
public class UpdateTenantCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    /// <summary>
    /// DTO containing the tenant data to update.
    /// </summary>
    public UpdateTenantDto TenantDto { get; set; } = null!;

    string? ISecureRequest.ResourceId => TenantDto.Id.ToString();
}
