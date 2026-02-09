using System;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantSettings;
using FluentValidation;

namespace Explore.Application.DTOs.TenantSettings.Validators;

public class UpdateTenantSettingsDtoValidator : AbstractValidator<UpdateTenantSettingsDto>
{
    private readonly ITenantRepository _tenantRepository;

    public UpdateTenantSettingsDtoValidator(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Tenant Settings ID is required");

        // TenantId is set by the handler from context, not by the client
        // No validation needed here
    }
}
