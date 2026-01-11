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
    public class CreateTenantSettingsCommandHandler : IRequestHandler<CreateTenantSettingsCommand, BaseCommandResponse<Guid>>
    {
        private readonly ITenantSettingsRepository _tenantSettingsRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateTenantSettingsDto> _validator;

        public CreateTenantSettingsCommandHandler(
            ITenantSettingsRepository tenantSettingsRepository,
            IMapper mapper,
            IValidator<CreateTenantSettingsDto> validator)
        {
            _tenantSettingsRepository = tenantSettingsRepository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateTenantSettingsCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validationResult = await _validator.ValidateAsync(request.TenantSettingsDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Tenant Settings creation failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var tenantSettings = _mapper.Map<Domain.TenantSettings>(request.TenantSettingsDto);
            tenantSettings = await _tenantSettingsRepository.Create(tenantSettings);

            response.Success = true;
            response.Id = tenantSettings.Id;
            response.Message = "Tenant Settings created successfully.";

            return response;
        }
    }
}
