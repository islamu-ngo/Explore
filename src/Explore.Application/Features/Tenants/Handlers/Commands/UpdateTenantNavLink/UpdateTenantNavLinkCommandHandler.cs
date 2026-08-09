// ABOUTME: Handler for updating a tenant navigation link.
// ABOUTME: Validates input, normalizes values, fetches entity, applies updates.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Tenant.Validators;
using Explore.Application.Features.Tenants.Requests.Commands.UpdateTenantNavLink;
using Explore.Application.Responses;
using Explore.Application.Settings;
using Explore.Domain.Constants;
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
    private readonly IHierarchicalSettingsResolver _settingsResolver;

    public UpdateTenantNavLinkCommandHandler(
        ITenantNavigationLinkRepository navigationLinkRepository,
        ITenantContext tenantContext,
        IHierarchicalSettingsResolver settingsResolver)
    {
        _navigationLinkRepository = navigationLinkRepository;
        _tenantContext = tenantContext;
        _settingsResolver = settingsResolver;
    }

    public async Task<BaseCommandResponse<bool>> Handle(UpdateTenantNavLinkCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<bool>();

        // Validate the DTO
        bool requireHttps = await _settingsResolver.ResolveAsync<bool>(
            GovernanceSettingKeys.Security.RequireHttpsExternalUrls,
            new SettingContext(),
            cancellationToken);
        var validator = new UpdateTenantNavigationLinkDtoValidator(requireHttps);
        var validationResult = await validator.ValidateAsync(request.Update, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Validation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        if (request.TenantId != _tenantContext.TenantId)
        {
            response.Success = false;
            response.Message = "Navigation link not found or does not belong to your tenant.";
            response.Errors = ["Navigation link not found."];
            response.FailureCode = FailureCodes.NotFound;
            return response;
        }

        var existingLink = await _navigationLinkRepository.GetByIdAndTenantAsync(
            request.NavigationLinkId,
            _tenantContext.TenantId,
            cancellationToken);

        if (existingLink == null)
        {
            response.Success = false;
            response.Message = "Navigation link not found or does not belong to your tenant.";
            response.Errors = new() { "Navigation link not found." };
            response.FailureCode = FailureCodes.NotFound;
            return response;
        }

        if (request.Update.Label is not null)
            existingLink.Label = request.Update.Label.Value.Trim();

        if (request.Update.Url is not null)
            existingLink.Url = request.Update.Url.Value.Trim();

        if (request.Update.Icon is { Value.HasValue: true })
            existingLink.Icon = string.IsNullOrWhiteSpace(request.Update.Icon.Value.Value)
                ? null
                : request.Update.Icon.Value.Value.Trim();

        if (request.Update.OpenInNewTab?.Value is bool openInNewTab)
            existingLink.OpenInNewTab = openInNewTab;

        // Update the entity
        await _navigationLinkRepository.Update(existingLink);

        response.Success = true;
        response.Message = "Navigation link updated successfully.";

        return response;
    }
}
