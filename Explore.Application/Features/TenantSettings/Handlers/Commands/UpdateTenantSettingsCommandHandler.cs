// ABOUTME: Handler for updating tenant settings with validation.
// ABOUTME: Validates input, fetches entity, applies updates.
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantSettings;
using Explore.Application.DTOs.TenantSettings.Validators;
using Explore.Application.Features.TenantSettings.Requests.Commands;
using Explore.Application.Responses;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.TenantSettings.Handlers.Commands;

public class UpdateTenantSettingsCommandHandler : IRequestHandler<UpdateTenantSettingsCommand, BaseCommandResponse<Guid>>
{
    private readonly ITenantSettingsRepository _tenantSettingsRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IMapper _mapper;

    public UpdateTenantSettingsCommandHandler(
        ITenantSettingsRepository tenantSettingsRepository,
        ITenantRepository tenantRepository,
        IMapper mapper)
    {
        _tenantSettingsRepository = tenantSettingsRepository;
        _tenantRepository = tenantRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateTenantSettingsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateTenantSettingsDtoValidator(_tenantRepository);
        var validationResult = await validator.ValidateAsync(request.TenantSettingsDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Tenant Settings update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var existingTenantSettings = await _tenantSettingsRepository.GetById(request.TenantSettingsDto.Id);
        if (existingTenantSettings == null)
        {
            response.Success = false;
            response.Message = "Tenant Settings not found.";
            return response;
        }

        _mapper.Map(request.TenantSettingsDto, existingTenantSettings);
        await _tenantSettingsRepository.Update(existingTenantSettings);

        response.Success = true;
        response.Id = existingTenantSettings.Id;
        response.Message = "Tenant Settings updated successfully.";

        return response;
    }
}
