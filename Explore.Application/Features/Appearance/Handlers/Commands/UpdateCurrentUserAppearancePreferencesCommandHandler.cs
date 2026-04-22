// ABOUTME: Persists authenticated user appearance preferences as sparse overrides in the hierarchical settings model.
// ABOUTME: Removes the user override when it matches the inherited parent value to preserve sparse preference storage.

namespace Explore.Application.Features.Appearance.Handlers.Commands;

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Appearance.Validators;
using Explore.Application.Features.Appearance.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain;
using Explore.Domain.Constants;
using MediatR;

public class UpdateCurrentUserAppearancePreferencesCommandHandler : IRequestHandler<UpdateCurrentUserAppearancePreferencesCommand, BaseCommandResponse<Guid>>
{
    private readonly IUserPreferenceRepository _userPreferenceRepository;
    private readonly IHierarchicalSettingsResolver _hierarchicalSettingsResolver;
    private readonly IUiThemeRepository _uiThemeRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCurrentUserAppearancePreferencesCommandHandler(
        IUserPreferenceRepository userPreferenceRepository,
        IHierarchicalSettingsResolver hierarchicalSettingsResolver,
        IUiThemeRepository uiThemeRepository,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService)
    {
        _userPreferenceRepository = userPreferenceRepository;
        _hierarchicalSettingsResolver = hierarchicalSettingsResolver;
        _uiThemeRepository = uiThemeRepository;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateCurrentUserAppearancePreferencesCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            response.Success = false;
            response.Message = "User not authenticated.";
            return response;
        }

        var validator = new UpdateUserAppearancePreferencesDtoValidator();
        var validationResult = await validator.ValidateAsync(request.Preferences, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Appearance preference update failed.";
            response.Errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            return response;
        }

        var tenantId = _tenantContext.TenantId;

        if (request.Preferences.DefaultThemeId is { } requestedThemeId)
        {
            var themeVisible = await IsThemeVisibleToTenantAsync(requestedThemeId, tenantId);
            if (!themeVisible)
            {
                response.Success = false;
                response.Message = "Appearance preference update failed.";
                response.Errors = new List<string>
                {
                    "DefaultThemeId references a theme that is not visible to the current tenant."
                };
                return response;
            }
        }

        var parentAppearance = await _hierarchicalSettingsResolver.ResolveGroupAsync<AppearanceSettingGroup>(
            new SettingContext(TenantId: tenantId),
            cancellationToken);

        var normalizedThemeMode = request.Preferences.ThemeMode.Trim().ToLowerInvariant();
        await UpsertOrRemoveOverrideAsync(
            tenantId,
            userId.Value,
            GovernanceSettingKeys.Appearance.ThemeMode,
            normalizedThemeMode,
            parentAppearance.ThemeMode);

        var normalizedLanguage = request.Preferences.Language.Trim().ToLowerInvariant();
        await UpsertOrRemoveOverrideAsync(
            tenantId,
            userId.Value,
            GovernanceSettingKeys.Appearance.Language,
            normalizedLanguage,
            parentAppearance.Language);

        var normalizedDirection = request.Preferences.Direction.Trim().ToLowerInvariant();
        await UpsertOrRemoveOverrideAsync(
            tenantId,
            userId.Value,
            GovernanceSettingKeys.Appearance.Direction,
            normalizedDirection,
            parentAppearance.Direction);

        var requestedThemeIdValue = request.Preferences.DefaultThemeId?.ToString() ?? string.Empty;
        var parentThemeIdValue = parentAppearance.DefaultThemeId?.ToString() ?? string.Empty;
        await UpsertOrRemoveOverrideAsync(
            tenantId,
            userId.Value,
            GovernanceSettingKeys.Appearance.DefaultThemeId,
            requestedThemeIdValue,
            parentThemeIdValue);

        _hierarchicalSettingsResolver.InvalidateUserCache(tenantId, userId.Value);

        response.Success = true;
        response.Id = userId.Value;
        response.Message = "Appearance preferences updated successfully.";
        return response;
    }

    private async Task<bool> IsThemeVisibleToTenantAsync(Guid themeId, Guid tenantId)
    {
        var theme = await _uiThemeRepository.GetById(themeId);
        if (theme is null || !theme.IsActive)
        {
            return false;
        }

        return theme.TenantId is null || theme.TenantId == tenantId;
    }

    private async Task UpsertOrRemoveOverrideAsync(
        Guid tenantId,
        Guid userId,
        string settingKey,
        string normalizedValue,
        string parentValue)
    {
        if (string.Equals(parentValue, normalizedValue, StringComparison.Ordinal))
        {
            await _userPreferenceRepository.RemoveOverride(tenantId, userId, settingKey);
            return;
        }

        var serializedValue = SettingValueSerializer.Serialize(normalizedValue);
        var existing = await _userPreferenceRepository.GetByUserAndKey(tenantId, userId, settingKey);

        if (existing is not null)
        {
            existing.Value = serializedValue;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = userId;
            await _userPreferenceRepository.Update(existing);
            return;
        }

        await _userPreferenceRepository.Create(new UserPreference
        {
            TenantId = tenantId,
            Tenant = null!,
            UserId = userId,
            SettingKey = settingKey,
            Value = serializedValue,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        });
    }
}
