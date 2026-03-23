// ABOUTME: Handler for creating tenant settings with validation.
// ABOUTME: Validates input, maps DTO, persists via repository.
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantSettings;
using Explore.Application.DTOs.TenantSettings.Validators;
using Explore.Application.Features.TenantSettings.Requests.Commands;
using Explore.Application.Responses;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.TenantSettings.Handlers.Commands;

public class CreateTenantSettingsCommandHandler : IRequestHandler<CreateTenantSettingsCommand, BaseCommandResponse<Guid>>
{
    private readonly ITenantSettingsRepository _tenantSettingsRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;

    public CreateTenantSettingsCommandHandler(
        ITenantSettingsRepository tenantSettingsRepository,
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        IMapper mapper)
    {
        _tenantSettingsRepository = tenantSettingsRepository;
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateTenantSettingsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new CreateTenantSettingsDtoValidator(_tenantRepository);
        var validationResult = await validator.ValidateAsync(request.TenantSettingsDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Tenant Settings creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var tenantSettings = _mapper.Map<Domain.TenantSettings>(request.TenantSettingsDto);

        // Set TenantId from the request context
        tenantSettings.TenantId = _tenantContext.TenantId;

        tenantSettings = await _tenantSettingsRepository.Create(tenantSettings);

        response.Success = true;
        response.Id = tenantSettings.Id;
        response.Message = "Tenant Settings created successfully.";

        return response;
    }
}
