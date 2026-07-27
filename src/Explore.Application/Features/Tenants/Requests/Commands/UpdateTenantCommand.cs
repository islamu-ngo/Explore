// ABOUTME: MediatR command for updating a tenant's profile.
// ABOUTME: Carries the UpdateTenantDto payload.
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
[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class UpdateTenantCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    /// <summary>
    /// DTO containing the tenant data to update.
    /// </summary>
    public Guid TenantId { get; set; }
    public UpdateTenantDto Update { get; set; } = null!;

    string? ISecureRequest.ResourceId => TenantId.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString("D")
    };
}
