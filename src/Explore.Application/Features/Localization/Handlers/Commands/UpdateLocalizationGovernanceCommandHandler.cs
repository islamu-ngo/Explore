// ABOUTME: Handler for UpdateLocalizationGovernanceCommand — validates, upserts 9 governance keys, invalidates resolver cache.
// ABOUTME: Validator is manually instantiated per repo convention (no DI for validators).

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Localization;
using Explore.Application.DTOs.Localization.Validators;
using Explore.Application.Features.Localization.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Settings;
using Explore.Domain.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Localization.Handlers.Commands;

public class UpdateLocalizationGovernanceCommandHandler
    : IRequestHandler<UpdateLocalizationGovernanceCommand, BaseCommandResponse<Guid>>
{
    private readonly SettingUpsertService _upsertService;
    private readonly ITranslationConfigResolver _configResolver;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<UpdateLocalizationGovernanceCommandHandler> _logger;

    public UpdateLocalizationGovernanceCommandHandler(
        SettingUpsertService upsertService,
        ITranslationConfigResolver configResolver,
        ICurrentUserService currentUserService,
        ITenantContext tenantContext,
        ILogger<UpdateLocalizationGovernanceCommandHandler> logger)
    {
        _upsertService = upsertService;
        _configResolver = configResolver;
        _currentUserService = currentUserService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateLocalizationGovernanceCommand request,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateLocalizationGovernanceDtoValidator();
        var validation = await validator.ValidateAsync(request.Dto, cancellationToken);
        if (!validation.IsValid)
        {
            response.Success = false;
            response.Message = "Localization governance update failed.";
            response.Errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var actor = _currentUserService.UserId;
        var dto = request.Dto;
        if (dto.Tms is { } tms)
        {
            await _upsertService.UpsertValueAsync(GovernanceSettingKeys.Localization.TmsProvider, SettingValueSerializer.Serialize(tms.Provider.Trim().ToLowerInvariant()), actor, cancellationToken);
            await _upsertService.UpsertValueAsync(GovernanceSettingKeys.Localization.TmsApiUrl, SettingValueSerializer.Serialize(tms.ApiUrl ?? string.Empty), actor, cancellationToken);
            await _upsertService.UpsertValueAsync(GovernanceSettingKeys.Localization.TmsProjectId, SettingValueSerializer.Serialize(tms.ProjectId ?? string.Empty), actor, cancellationToken);
            await _upsertService.UpsertValueAsync(GovernanceSettingKeys.Localization.TmsComponent, SettingValueSerializer.Serialize(tms.Component ?? string.Empty), actor, cancellationToken);
        }

        var enabledLanguagesCsv = string.Empty;
        if (dto.Languages is { } languages)
        {
            enabledLanguagesCsv = string.Join(",", languages.EnabledLanguages.Select(c => c.Trim().ToLowerInvariant()));
            await _upsertService.UpsertValueAsync(GovernanceSettingKeys.Localization.DefaultLanguage, SettingValueSerializer.Serialize(languages.DefaultLanguage.Trim().ToLowerInvariant()), actor, cancellationToken);
            await _upsertService.UpsertValueAsync(GovernanceSettingKeys.Localization.EnabledLanguages, SettingValueSerializer.Serialize(enabledLanguagesCsv), actor, cancellationToken);
            await _upsertService.UpsertValueAsync(GovernanceSettingKeys.Localization.FallbackLanguage, SettingValueSerializer.Serialize(languages.FallbackLanguage.Trim().ToLowerInvariant()), actor, cancellationToken);
        }

        if (dto.Runtime is { } runtime)
        {
            await _upsertService.UpsertValueAsync(GovernanceSettingKeys.Localization.ClientPickerEnabled, runtime.ClientPickerEnabled ? "true" : "false", actor, cancellationToken);
            await _upsertService.UpsertValueAsync(GovernanceSettingKeys.Localization.ForceOfflineMode, runtime.ForceOfflineMode ? "true" : "false", actor, cancellationToken);
        }

        _configResolver.InvalidateCache(_tenantContext.TenantId);

        _logger.LogInformation(
            "[LOCALIZATION] Governance updated by {Actor}: provider={Provider}, enabled=[{Enabled}], fallback={Fallback}, pickerEnabled={Picker}, forceOffline={ForceOffline}",
            actor, dto.Tms?.Provider, enabledLanguagesCsv, dto.Languages?.FallbackLanguage, dto.Runtime?.ClientPickerEnabled, dto.Runtime?.ForceOfflineMode);

        response.Success = true;
        response.Id = actor ?? Guid.Empty;
        response.Message = "Localization governance updated successfully.";
        return response;
    }
}
