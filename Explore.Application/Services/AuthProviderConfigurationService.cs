// ABOUTME: Service implementation for managing instance-level authentication provider configuration.
// ABOUTME: Handles reading and writing auth provider settings (Keycloak, ATProto, Google SSO) via SystemSetting records.

using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;

namespace Explore.Application.Services;

public class AuthProviderConfigurationService : IAuthProviderConfigurationService
{
    private readonly ISystemSettingRepository _systemSettingRepository;

    public AuthProviderConfigurationService(ISystemSettingRepository systemSettingRepository)
    {
        _systemSettingRepository = systemSettingRepository;
    }

    public async Task<AuthProviderConfigurationDto> ReadConfigurationAsync()
    {
        var keycloakEnabled = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Authentication.KeycloakEnabled);
        var keycloakAuthority = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Authentication.KeycloakAuthority);
        var keycloakClientId = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Authentication.KeycloakClientId);
        var atprotoLoginEnabled = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Authentication.AtprotoLoginEnabled);
        var atprotoPublicUrl = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Authentication.AtprotoPublicUrl);
        var googleSsoEnabled = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Authentication.GoogleSsoEnabled);
        var googleClientId = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Authentication.GoogleClientId);

        return new AuthProviderConfigurationDto
        {
            KeycloakEnabled = DeserializeBoolean(keycloakEnabled?.Value, false),
            KeycloakAuthority = DeserializeString(keycloakAuthority?.Value, string.Empty),
            KeycloakClientId = DeserializeString(keycloakClientId?.Value, string.Empty),
            KeycloakClientSecret = string.Empty, // Never return secrets on read
            AtprotoLoginEnabled = DeserializeBoolean(atprotoLoginEnabled?.Value, false),
            AtprotoPublicUrl = DeserializeString(atprotoPublicUrl?.Value, string.Empty),
            GoogleSsoEnabled = DeserializeBoolean(googleSsoEnabled?.Value, false),
            GoogleClientId = DeserializeString(googleClientId?.Value, string.Empty),
            GoogleClientSecret = string.Empty, // Never return secrets on read
            LockKeycloakEnabled = keycloakEnabled?.IsLocked == true,
            LockAtprotoLoginEnabled = atprotoLoginEnabled?.IsLocked == true,
            LockGoogleSsoEnabled = googleSsoEnabled?.IsLocked == true,
        };
    }

    public async Task ApplyConfigurationAsync(AuthProviderConfigurationDto configuration)
    {
        await UpsertSettingAsync(
            GovernanceSettingKeys.Authentication.KeycloakEnabled,
            JsonSerializer.Serialize(configuration.KeycloakEnabled),
            SettingValueType.Boolean,
            configuration.LockKeycloakEnabled,
            "Authentication",
            1,
            "Whether Keycloak OIDC authentication is enabled");

        await UpsertSettingAsync(
            GovernanceSettingKeys.Authentication.KeycloakAuthority,
            JsonSerializer.Serialize(configuration.KeycloakAuthority),
            SettingValueType.String,
            true,
            "Authentication",
            2,
            "Keycloak realm authority URL");

        await UpsertSettingAsync(
            GovernanceSettingKeys.Authentication.KeycloakClientId,
            JsonSerializer.Serialize(configuration.KeycloakClientId),
            SettingValueType.String,
            true,
            "Authentication",
            3,
            "Keycloak OIDC client ID");

        if (!string.IsNullOrEmpty(configuration.KeycloakClientSecret))
        {
            await UpsertSettingAsync(
                InfrastructureSecretSettingKeys.Authentication.KeycloakClientSecret,
                JsonSerializer.Serialize(configuration.KeycloakClientSecret),
                SettingValueType.String,
                true,
                "Authentication",
                4,
                "Keycloak OIDC client secret");
        }

        await UpsertSettingAsync(
            GovernanceSettingKeys.Authentication.AtprotoLoginEnabled,
            JsonSerializer.Serialize(configuration.AtprotoLoginEnabled),
            SettingValueType.Boolean,
            configuration.LockAtprotoLoginEnabled,
            "Authentication",
            5,
            "Whether ATProto DID-based authentication is enabled");

        await UpsertSettingAsync(
            GovernanceSettingKeys.Authentication.AtprotoPublicUrl,
            JsonSerializer.Serialize(configuration.AtprotoPublicUrl),
            SettingValueType.String,
            true,
            "Authentication",
            6,
            "Publicly accessible URL for ATProto OAuth client metadata");

        await UpsertSettingAsync(
            GovernanceSettingKeys.Authentication.GoogleSsoEnabled,
            JsonSerializer.Serialize(configuration.GoogleSsoEnabled),
            SettingValueType.Boolean,
            configuration.LockGoogleSsoEnabled,
            "Authentication",
            7,
            "Whether Google SSO authentication is enabled");

        await UpsertSettingAsync(
            GovernanceSettingKeys.Authentication.GoogleClientId,
            JsonSerializer.Serialize(configuration.GoogleClientId),
            SettingValueType.String,
            true,
            "Authentication",
            8,
            "Google OAuth client ID");

        if (!string.IsNullOrEmpty(configuration.GoogleClientSecret))
        {
            await UpsertSettingAsync(
                InfrastructureSecretSettingKeys.Authentication.GoogleClientSecret,
                JsonSerializer.Serialize(configuration.GoogleClientSecret),
                SettingValueType.String,
                true,
                "Authentication",
                9,
                "Google OAuth client secret");
        }
    }

    public async Task<bool> IsConfiguredAsync()
    {
        var keycloakEnabled = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Authentication.KeycloakEnabled);
        var atprotoEnabled = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Authentication.AtprotoLoginEnabled);
        var googleEnabled = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Authentication.GoogleSsoEnabled);

        return DeserializeBoolean(keycloakEnabled?.Value, false)
               || DeserializeBoolean(atprotoEnabled?.Value, false)
               || DeserializeBoolean(googleEnabled?.Value, false);
    }

    public async Task<AuthProviderConfigurationDto> ReadConfigurationWithSecretsAsync()
    {
        var dto = await ReadConfigurationAsync();

        var keycloakSecret = await _systemSettingRepository.GetByKey(InfrastructureSecretSettingKeys.Authentication.KeycloakClientSecret);
        var googleSecret = await _systemSettingRepository.GetByKey(InfrastructureSecretSettingKeys.Authentication.GoogleClientSecret);

        dto.KeycloakClientSecret = DeserializeString(keycloakSecret?.Value, string.Empty);
        dto.GoogleClientSecret = DeserializeString(googleSecret?.Value, string.Empty);

        return dto;
    }

    private async Task UpsertSettingAsync(
        string settingKey,
        string value,
        SettingValueType valueType,
        bool isLocked,
        string category,
        int displayOrder,
        string description)
    {
        var existing = await _systemSettingRepository.GetByKey(settingKey);

        if (existing == null)
        {
            await _systemSettingRepository.Create(new SystemSetting
            {
                SettingKey = settingKey,
                Value = value,
                ValueType = valueType,
                IsLocked = isLocked,
                Description = description,
                Category = category,
                DisplayOrder = displayOrder,
                CreatedAt = DateTime.UtcNow
            });

            return;
        }

        existing.Value = value;
        existing.ValueType = valueType;
        existing.IsLocked = isLocked;
        existing.Description = description;
        existing.Category = category;
        existing.DisplayOrder = displayOrder;
        existing.UpdatedAt = DateTime.UtcNow;

        await _systemSettingRepository.Update(existing);
    }

    private static bool DeserializeBoolean(string? rawValue, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return defaultValue;

        try
        {
            return JsonSerializer.Deserialize<bool>(rawValue);
        }
        catch
        {
            return bool.TryParse(rawValue, out var parsed) ? parsed : defaultValue;
        }
    }

    private static string DeserializeString(string? rawValue, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return defaultValue;

        try
        {
            var deserialized = JsonSerializer.Deserialize<string>(rawValue);
            return string.IsNullOrWhiteSpace(deserialized) ? defaultValue : deserialized;
        }
        catch
        {
            return rawValue.Trim('"');
        }
    }
}
