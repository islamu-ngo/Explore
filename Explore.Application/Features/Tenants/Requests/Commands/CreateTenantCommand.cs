// ABOUTME: MediatR command for creating a new tenant.
// ABOUTME: Carries the CreateTenantDto payload and optional requesting-user context for automatic admin assignment.
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

    /// <summary>
    /// The authenticated user making the request. Set by the controller from the JWT claims.
    /// Required when <see cref="CreateTenantDto.AssignCurrentUserAsTenantAdmin"/> is true.
    /// </summary>
    public Guid? RequestingUserId { get; init; }

    string? ISecureRequest.ResourceId => null;
}
