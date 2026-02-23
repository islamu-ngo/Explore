using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Tenants.Requests.Commands;

/// <summary>
/// Command to create a new tenant.
/// Returns the ID of the created tenant.
/// </summary>
[AuthorizeResource("tenant", PermissionAction.Create)]
public class CreateTenantCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    /// <summary>
    /// DTO containing the tenant data to create.
    /// </summary>
    public CreateTenantDto TenantDto { get; set; } = null!;

    string? ISecureRequest.ResourceId => null;
}
