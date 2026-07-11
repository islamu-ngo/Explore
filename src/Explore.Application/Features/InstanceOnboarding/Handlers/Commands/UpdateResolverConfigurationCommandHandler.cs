// ABOUTME: Handles resolver-configuration updates for instance administrators.
// ABOUTME: Persists system-only resolver settings and invalidates the dedicated resolver config cache.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Services;
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
            return response;
        }

        var validator = new ResolverConfigurationDtoValidator();
        var validationResult = await validator.ValidateAsync(request.Configuration, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Invalid resolver configuration.";
            response.Errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            return response;
        }

        await _resolverConfigService.ApplyConfigurationAsync(request.Configuration, request.UserId, cancellationToken);

        response.Success = true;
        response.Id = Guid.Empty;
        response.Message = "Tenant resolver configuration updated successfully.";
        return response;
    }
}
