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
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCurrentUserAppearancePreferencesCommandHandler(
        IUserPreferenceRepository userPreferenceRepository,
        IHierarchicalSettingsResolver hierarchicalSettingsResolver,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService)
    {
        _userPreferenceRepository = userPreferenceRepository;
        _hierarchicalSettingsResolver = hierarchicalSettingsResolver;
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
        var normalizedThemeMode = request.Preferences.ThemeMode.Trim().ToLowerInvariant();
        var parentAppearance = await _hierarchicalSettingsResolver.ResolveGroupAsync<AppearanceSettingGroup>(
            new SettingContext(TenantId: tenantId),
            cancellationToken);

        if (string.Equals(parentAppearance.ThemeMode, normalizedThemeMode, StringComparison.Ordinal))
        {
            await _userPreferenceRepository.RemoveOverride(tenantId, userId.Value, GovernanceSettingKeys.Appearance.ThemeMode);
        }
        else
        {
            var serializedValue = SettingValueSerializer.Serialize(normalizedThemeMode);
            var existingPreference = await _userPreferenceRepository.GetByUserAndKey(
                tenantId,
                userId.Value,
                GovernanceSettingKeys.Appearance.ThemeMode);

            if (existingPreference is not null)
            {
                existingPreference.Value = serializedValue;
                existingPreference.UpdatedAt = DateTime.UtcNow;
                existingPreference.UpdatedBy = userId.Value;
                await _userPreferenceRepository.Update(existingPreference);
            }
            else
            {
                await _userPreferenceRepository.Create(new UserPreference
                {
                    TenantId = tenantId,
                    Tenant = null!,
                    UserId = userId.Value,
                    SettingKey = GovernanceSettingKeys.Appearance.ThemeMode,
                    Value = serializedValue,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId.Value
                });
            }
        }

        var normalizedLanguage = request.Preferences.Language.Trim().ToLowerInvariant();
        if (string.Equals(parentAppearance.Language, normalizedLanguage, StringComparison.Ordinal))
        {
            await _userPreferenceRepository.RemoveOverride(tenantId, userId.Value, GovernanceSettingKeys.Appearance.Language);
        }
        else
        {
            var serializedLanguage = SettingValueSerializer.Serialize(normalizedLanguage);
            var existingLanguage = await _userPreferenceRepository.GetByUserAndKey(
                tenantId,
                userId.Value,
                GovernanceSettingKeys.Appearance.Language);

            if (existingLanguage is not null)
            {
                existingLanguage.Value = serializedLanguage;
                existingLanguage.UpdatedAt = DateTime.UtcNow;
                existingLanguage.UpdatedBy = userId.Value;
                await _userPreferenceRepository.Update(existingLanguage);
            }
            else
            {
                await _userPreferenceRepository.Create(new UserPreference
                {
                    TenantId = tenantId,
                    Tenant = null!,
                    UserId = userId.Value,
                    SettingKey = GovernanceSettingKeys.Appearance.Language,
                    Value = serializedLanguage,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId.Value
                });
            }
        }

        var normalizedDirection = request.Preferences.Direction.Trim().ToLowerInvariant();
        if (string.Equals(parentAppearance.Direction, normalizedDirection, StringComparison.Ordinal))
        {
            await _userPreferenceRepository.RemoveOverride(tenantId, userId.Value, GovernanceSettingKeys.Appearance.Direction);
        }
        else
        {
            var serializedDirection = SettingValueSerializer.Serialize(normalizedDirection);
            var existingDirection = await _userPreferenceRepository.GetByUserAndKey(
                tenantId,
                userId.Value,
                GovernanceSettingKeys.Appearance.Direction);

            if (existingDirection is not null)
            {
                existingDirection.Value = serializedDirection;
                existingDirection.UpdatedAt = DateTime.UtcNow;
                existingDirection.UpdatedBy = userId.Value;
                await _userPreferenceRepository.Update(existingDirection);
            }
            else
            {
                await _userPreferenceRepository.Create(new UserPreference
                {
                    TenantId = tenantId,
                    Tenant = null!,
                    UserId = userId.Value,
                    SettingKey = GovernanceSettingKeys.Appearance.Direction,
                    Value = serializedDirection,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId.Value
                });
            }
        }

        _hierarchicalSettingsResolver.InvalidateUserCache(tenantId, userId.Value);

        response.Success = true;
        response.Id = userId.Value;
        response.Message = "Appearance preferences updated successfully.";
        return response;
    }
}
