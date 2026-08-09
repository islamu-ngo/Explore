// ABOUTME: Handles resolver-configuration updates for instance administrators.
// ABOUTME: Persists system-only resolver settings and invalidates the dedicated resolver config cache.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Instance;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.DTOs.Onboarding.Validators;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public class UpdateResolverConfigurationCommandHandler : IRequestHandler<UpdateResolverConfigurationCommand, BaseCommandResponse<Guid>>
{
    private readonly IAdminContext _adminContext;
    private readonly IResolverConfigService _resolverConfigService;

    public UpdateResolverConfigurationCommandHandler(
        IAdminContext adminContext,
        IResolverConfigService resolverConfigService)
    {
        _adminContext = adminContext;
        _resolverConfigService = resolverConfigService;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateResolverConfigurationCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var isInstanceAdmin = await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken);
        if (!isInstanceAdmin)
        {
            response.Success = false;
            response.Message = "Only instance administrators can update tenant resolver configuration.";
            response.FailureCode = FailureCodes.AdminRequired;
            return response;
        }

        if (!request.Patch.HasChanges())
        {
            response.Success = false;
            response.Message = "Resolver configuration patch must include at least one setting.";
            return response;
        }

        var configuration = await _resolverConfigService.GetConfigurationAsync(cancellationToken);
        configuration.HeaderEnabled = request.Patch.HeaderEnabled.HasValue ? request.Patch.HeaderEnabled.Value : configuration.HeaderEnabled;
        configuration.SubdomainEnabled = request.Patch.SubdomainEnabled.HasValue ? request.Patch.SubdomainEnabled.Value : configuration.SubdomainEnabled;
        configuration.CustomDomainEnabled = request.Patch.CustomDomainEnabled.HasValue ? request.Patch.CustomDomainEnabled.Value : configuration.CustomDomainEnabled;
        configuration.PathEnabled = request.Patch.PathEnabled.HasValue ? request.Patch.PathEnabled.Value : configuration.PathEnabled;
        configuration.PathPrefix = request.Patch.PathPrefix.HasValue
            ? request.Patch.PathPrefix.Value ?? string.Empty
            : configuration.PathPrefix;
        configuration.InstanceBaseDomain = request.Patch.InstanceBaseDomain.HasValue
            ? request.Patch.InstanceBaseDomain.Value ?? string.Empty
            : configuration.InstanceBaseDomain;
        configuration.AllowTenantCustomDomains = request.Patch.AllowTenantCustomDomains.HasValue ? request.Patch.AllowTenantCustomDomains.Value : configuration.AllowTenantCustomDomains;

        var validator = new ResolverConfigurationDtoValidator();
        var validationResult = await validator.ValidateAsync(configuration, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Invalid resolver configuration.";
            response.Errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            return response;
        }

        await _resolverConfigService.ApplyConfigurationAsync(request.Patch, configuration, request.UserId, cancellationToken);

        response.Success = true;
        response.Id = Guid.Empty;
        response.Message = "Tenant resolver configuration updated successfully.";
        return response;
    }
}
