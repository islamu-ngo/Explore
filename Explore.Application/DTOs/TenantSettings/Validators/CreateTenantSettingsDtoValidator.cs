using FluentValidation;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantSettings;
using System;

namespace Explore.Application.DTOs.TenantSettings.Validators
{
    public class CreateTenantSettingsDtoValidator : AbstractValidator<CreateTenantSettingsDto>
    {
        private readonly ITenantRepository _tenantRepository;

        public CreateTenantSettingsDtoValidator(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;

            // TenantId is set by the handler from context, not by the client
            // No validation needed here
        }
    }
}
