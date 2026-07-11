// ABOUTME: MediatR command for deleting a tenant by ID.
// ABOUTME: Carries the target tenant ID.
using System;
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.Tenants.Requests.Commands;

/// <summary>
/// Command to delete a tenant.
/// Returns true if the tenant was successfully deleted, false if not found.
/// </summary>
[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Delete)]
public class DeleteTenantCommand : IRequest<bool>, ISecureRequest
{
    /// <summary>
    /// The ID of the tenant to delete.
    /// </summary>
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
