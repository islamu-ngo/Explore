// ABOUTME: MediatR command for updating a tenant navigation link.
// ABOUTME: Carries the UpdateTenantNavLinkDto payload.
using Explore.Application.DTOs.Tenant;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Tenants.Requests.Commands.UpdateTenantNavLink;

/// <summary>
/// Command to update an existing tenant navigation link.
/// Returns a boolean indicating success or failure.
/// </summary>
public class UpdateTenantNavLinkCommand : IRequest<BaseCommandResponse<bool>>
{
    /// <summary>
    /// DTO containing the updated navigation link data.
    /// </summary>
    public UpdateTenantNavigationLinkDto NavigationLinkDto { get; set; } = null!;
}
