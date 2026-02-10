using System;
using MediatR;

namespace Explore.Application.Features.Tenants.Requests.Commands;

/// <summary>
/// Command to delete a tenant.
/// Returns true if the tenant was successfully deleted, false if not found.
/// </summary>
public class DeleteTenantCommand : IRequest<bool>
{
    /// <summary>
    /// The ID of the tenant to delete.
    /// </summary>
    public Guid Id { get; set; }
}
