using FluentValidation;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantSettings;
using System;

namespace Explore.Application.DTOs.TenantSettings.Validators
{
    public class UpdateTenantSettingsDtoValidator : AbstractValidator<UpdateTenantSettingsDto>
    {
        private readonly ITenantRepository _tenantRepository;
        public UpdateTenantSettingsDtoValidator(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;

            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Tenant Settings ID is required");

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("Tenant ID is required")
                .MustAsync(TenantExists)
                .WithMessage("Tenant does not exist");
        }

        private async Task<bool> TenantExists(Guid tenantId, CancellationToken cancellationToken)
        {
            return await _tenantRepository.Exists(tenantId);
        }
    }
}
