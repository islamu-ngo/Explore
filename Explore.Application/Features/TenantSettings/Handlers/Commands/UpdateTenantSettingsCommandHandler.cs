using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantSettings;
using Explore.Application.Features.TenantSettings.Requests.Commands;
using Explore.Application.DTOs.TenantSettings.Validators;
using Explore.Application.Responses;
using FluentValidation;

namespace Explore.Application.Features.TenantSettings.Handlers.Commands
{
    public class UpdateTenantSettingsCommandHandler : IRequestHandler<UpdateTenantSettingsCommand, BaseCommandResponse<Guid>>
    {
        private readonly ITenantSettingsRepository _tenantSettingsRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<UpdateTenantSettingsDto> _validator;

        public UpdateTenantSettingsCommandHandler(
            ITenantSettingsRepository tenantSettingsRepository,
            IMapper mapper,
            IValidator<UpdateTenantSettingsDto> validator)
        {
            _tenantSettingsRepository = tenantSettingsRepository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(UpdateTenantSettingsCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validationResult = await _validator.ValidateAsync(request.TenantSettingsDto);

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
}
