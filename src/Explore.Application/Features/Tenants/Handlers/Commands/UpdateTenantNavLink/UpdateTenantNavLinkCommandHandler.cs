// ABOUTME: Handler for updating a tenant navigation link.
// ABOUTME: Validates input, normalizes values, fetches entity, applies updates.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Tenant.Validators;
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

        // Validate the DTO
        var validator = new UpdateTenantNavigationLinkDtoValidator();
        var validationResult = await validator.ValidateAsync(request.NavigationLinkDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Validation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

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

        // Update with normalized values: trim, blank icon → null
        existingLink.Label = request.NavigationLinkDto.Label.Trim();
        existingLink.Url = request.NavigationLinkDto.Url.Trim();
        existingLink.Icon = string.IsNullOrWhiteSpace(request.NavigationLinkDto.Icon) ? null : request.NavigationLinkDto.Icon.Trim();
        existingLink.OpenInNewTab = request.NavigationLinkDto.OpenInNewTab;

        // Update the entity
        await _navigationLinkRepository.Update(existingLink);

        response.Success = true;
        response.Message = "Navigation link updated successfully.";

        return response;
    }
}
