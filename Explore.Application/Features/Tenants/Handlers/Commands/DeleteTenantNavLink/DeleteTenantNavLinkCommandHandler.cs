// ABOUTME: Handler for deleting a tenant navigation link.
// ABOUTME: Fetches nav link by ID and delegates deletion.
using System;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Tenants.Requests.Commands.DeleteTenantNavLink;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Tenants.Handlers.Commands.DeleteTenantNavLink;

/// <summary>
/// Handler for DeleteTenantNavLinkCommand.
/// Deletes a navigation link for the current tenant.
/// Verifies the link belongs to the tenant before deleting.
/// </summary>
public class DeleteTenantNavLinkCommandHandler : IRequestHandler<DeleteTenantNavLinkCommand, BaseCommandResponse<bool>>
{
    private readonly ITenantNavigationLinkRepository _navigationLinkRepository;
    private readonly ITenantContext _tenantContext;

    public DeleteTenantNavLinkCommandHandler(
        ITenantNavigationLinkRepository navigationLinkRepository,
        ITenantContext tenantContext)
    {
        _navigationLinkRepository = navigationLinkRepository;
        _tenantContext = tenantContext;
    }

    public async Task<BaseCommandResponse<bool>> Handle(DeleteTenantNavLinkCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<bool>();

        // Verify the navigation link exists and belongs to the current tenant
        var existingLink = await _navigationLinkRepository.GetByIdAndTenantAsync(
            request.Id,
            _tenantContext.TenantId,
            cancellationToken);

        if (existingLink == null)
        {
            response.Success = false;
            response.Message = "Navigation link not found or does not belong to your tenant.";
            response.Errors = new() { "Navigation link not found." };
            return response;
        }

        // Delete the navigation link
        await _navigationLinkRepository.Delete(existingLink);

        response.Success = true;
        response.Message = "Navigation link deleted successfully.";

        return response;
    }
}
