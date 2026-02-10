using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Tenants.Requests.Commands.UpdateTenantNavLink;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Tenants.Handlers.Commands.UpdateTenantNavLink;

/// <summary>
/// Handler for UpdateTenantNavLinkCommand.
/// Updates an existing navigation link for the current tenant.
/// Verifies the link belongs to the tenant before updating.
/// </summary>
public class UpdateTenantNavLinkCommandHandler : IRequestHandler<UpdateTenantNavLinkCommand, BaseCommandResponse<bool>>
{
    private readonly ITenantNavigationLinkRepository _navigationLinkRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;

    public UpdateTenantNavLinkCommandHandler(
        ITenantNavigationLinkRepository navigationLinkRepository,
        ITenantContext tenantContext,
        IMapper mapper)
    {
        _navigationLinkRepository = navigationLinkRepository;
        _tenantContext = tenantContext;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<bool>> Handle(UpdateTenantNavLinkCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<bool>();

        // Verify the navigation link exists and belongs to the current tenant
        var existingLink = await _navigationLinkRepository.GetByIdAndTenantAsync(
            request.NavigationLinkDto.Id,
            _tenantContext.TenantId,
            cancellationToken);

        if (existingLink == null)
        {
            response.Success = false;
            response.Message = "Navigation link not found or does not belong to your tenant.";
            response.Errors = new() { "Navigation link not found." };
            return response;
        }

        // Update the navigation link properties
        existingLink.Label = request.NavigationLinkDto.Label;
        existingLink.Url = request.NavigationLinkDto.Url;
        existingLink.Icon = request.NavigationLinkDto.Icon;
        existingLink.OpenInNewTab = request.NavigationLinkDto.OpenInNewTab;

        // Update the entity
        await _navigationLinkRepository.Update(existingLink);

        response.Success = true;
        response.Message = "Navigation link updated successfully.";

        return response;
    }
}
