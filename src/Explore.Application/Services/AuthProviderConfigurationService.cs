// ABOUTME: Service implementation for managing instance-level authentication provider configuration.
// ABOUTME: Handles reading and writing auth provider settings (Keycloak, ATProto, Google SSO) via SystemSetting records.

using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Secrets;
using Microsoft.Extensions.Configuration;

namespace Explore.Application.Services;

public class AuthProviderConfigurationService : IAuthProviderConfigurationService
{
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly IConfiguration _configuration;

    public AuthProviderConfigurationService(
        ISystemSettingRepository systemSettingRepository,
        IConfiguration configuration)
    {
        _systemSettingRepository = systemSettingRepository;
        _configuration = configuration;
    }

    public async Task<AuthProviderConfigurationDto> ReadConfigurationAsync()
    {
        var keycloakEnabled = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Authentication.KeycloakEnabled);
        var keycloak = await ResolveKeycloakConfigurationAsync();
        var atprotoLoginEnabled = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Authentication.AtprotoLoginEnabled);
        var atprotoPublicUrl = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Authentication.AtprotoPublicUrl);
        var googleSsoEnabled = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Authentication.GoogleSsoEnabled);
        var googleClientId = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Authentication.GoogleClientId);
        var keycloakSecret = await _systemSettingRepository.GetByKey(InfrastructureSecretSettingKeys.Authentication.KeycloakClientSecret);

        var keycloakSecretDeploymentManaged = IsKeycloakClientSecretDeploymentManaged();
        var storedKeycloakSecretConfigured = !string.IsNullOrWhiteSpace(DeserializeString(keycloakSecret?.Value, string.Empty));
        var configuredKeycloakSecretConfigured = !string.IsNullOrWhiteSpace(ReadConfiguredKeycloakClientSecret());

        return new AuthProviderConfigurationDto
        {
            KeycloakEnabled = keycloak.Enabled,
            KeycloakAuthority = keycloak.Authority,
            KeycloakClientId = keycloak.ClientId,
            KeycloakClientSecret = string.Empty, // Never return secrets on read
            KeycloakDetectedFromEnvironment = keycloak.DetectedFromEnvironment,
            KeycloakClientSecretOwnership = CreateOwnershipMetadata(
                keycloakSecretDeploymentManaged,
                configured: keycloakSecretDeploymentManaged
                    ? configuredKeycloakSecretConfigured
                    : storedKeycloakSecretConfigured || configuredKeycloakSecretConfigured,
                bootstrapAvailable: !keycloakSecretDeploymentManaged
                    && !storedKeycloakSecretConfigured
                    && configuredKeycloakSecretConfigured,
                applicationManagedDescription: "Keycloak client secret can be rotated and stored by the platform. Deployment values only seed runtime configuration until an application-managed secret is saved.",
                deploymentManagedDescription: "Keycloak client secret is deployment-managed. Rotate it in the deployment secret provider and update the matching Keycloak client outside the Admin UI."),
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

        if (!IsKeycloakAuthorityDeploymentManaged())
        {
            await UpsertSettingAsync(
                GovernanceSettingKeys.Authentication.KeycloakAuthority,
                JsonSerializer.Serialize(configuration.KeycloakAuthority),
                SettingValueType.String,
                true,
                "Authentication",
                2,
                "Keycloak realm authority URL");
        }

        if (!IsKeycloakClientIdDeploymentManaged())
        {
            await UpsertSettingAsync(
                GovernanceSettingKeys.Authentication.KeycloakClientId,
                JsonSerializer.Serialize(configuration.KeycloakClientId),
                SettingValueType.String,
                true,
                "Authentication",
                3,
                "Keycloak OIDC client ID");
        }

        if (!string.IsNullOrEmpty(configuration.KeycloakClientSecret)
            && !IsKeycloakClientSecretDeploymentManaged())
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
        var keycloak = await ResolveKeycloakConfigurationAsync();
        var atprotoEnabled = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Authentication.AtprotoLoginEnabled);
        var googleEnabled = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Authentication.GoogleSsoEnabled);

        return keycloak.Enabled
               || DeserializeBoolean(atprotoEnabled?.Value, false)
               || DeserializeBoolean(googleEnabled?.Value, false);
    }

    public async Task<AuthProviderConfigurationDto> ReadConfigurationWithSecretsAsync()
    {
        var dto = await ReadConfigurationAsync();

        var keycloakSecret = await _systemSettingRepository.GetByKey(InfrastructureSecretSettingKeys.Authentication.KeycloakClientSecret);
        var googleSecret = await _systemSettingRepository.GetByKey(InfrastructureSecretSettingKeys.Authentication.GoogleClientSecret);

        var storedKeycloakSecret = DeserializeString(keycloakSecret?.Value, string.Empty);
        var configuredKeycloakSecret = ReadConfiguredKeycloakClientSecret();
        dto.KeycloakClientSecret = IsKeycloakClientSecretDeploymentManaged()
            ? configuredKeycloakSecret
            : string.IsNullOrWhiteSpace(storedKeycloakSecret)
                ? configuredKeycloakSecret
                : storedKeycloakSecret;
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
        await _systemSettingRepository.UpsertAsync(new SystemSetting
        {
            SettingKey = settingKey,
            Value = value,
            ValueType = valueType,
            IsLocked = isLocked,
            Description = description,
            Category = category,
            DisplayOrder = displayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    private async Task<(bool Enabled, string Authority, string ClientId, bool DetectedFromEnvironment)>
        ResolveKeycloakConfigurationAsync()
    {
        var enabledSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Authentication.KeycloakEnabled);
        var authoritySetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Authentication.KeycloakAuthority);
        var clientIdSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Authentication.KeycloakClientId);
        var storedEnabled = DeserializeBoolean(enabledSetting?.Value, false);
        var storedAuthority = DeserializeString(authoritySetting?.Value, string.Empty);
        var storedClientId = DeserializeString(clientIdSetting?.Value, string.Empty);
        var storedUsable = storedEnabled
                           && !string.IsNullOrWhiteSpace(storedAuthority)
                           && !string.IsNullOrWhiteSpace(storedClientId);
        var deploymentAuthority = ReadFirstConfigured("Keycloak:Authority");
        var deploymentClientId = ReadFirstConfigured("Keycloak:ClientId");
        var deploymentUsable = !string.IsNullOrWhiteSpace(deploymentAuthority)
                               && !string.IsNullOrWhiteSpace(deploymentClientId);
        var authorityDeploymentManaged = IsKeycloakAuthorityDeploymentManaged();
        var clientIdDeploymentManaged = IsKeycloakClientIdDeploymentManaged();

        if (authorityDeploymentManaged || clientIdDeploymentManaged)
        {
            var effectiveAuthority = authorityDeploymentManaged
                ? deploymentAuthority
                : string.IsNullOrWhiteSpace(storedAuthority)
                    ? deploymentAuthority
                    : storedAuthority;
            var effectiveClientId = clientIdDeploymentManaged
                ? deploymentClientId
                : string.IsNullOrWhiteSpace(storedClientId)
                    ? deploymentClientId
                    : storedClientId;
            var effectiveUsable = !string.IsNullOrWhiteSpace(effectiveAuthority)
                                  && !string.IsNullOrWhiteSpace(effectiveClientId);
            var enabled = effectiveUsable && (storedEnabled || deploymentUsable);
            var usesDeploymentMetadata = authorityDeploymentManaged
                                         || clientIdDeploymentManaged
                                         || string.IsNullOrWhiteSpace(storedAuthority)
                                         || string.IsNullOrWhiteSpace(storedClientId);

            return (enabled, effectiveAuthority, effectiveClientId, enabled && usesDeploymentMetadata);
        }

        if (storedUsable)
        {
            return (true, storedAuthority, storedClientId, false);
        }

        return deploymentUsable
            ? (true, deploymentAuthority, deploymentClientId, true)
            : (false, storedAuthority, storedClientId, false);
    }

    private bool IsKeycloakAuthorityDeploymentManaged()
    {
        return IsDeploymentManaged(GovernanceSettingKeys.Authentication.KeycloakAuthority)
               || IsDeploymentManaged(SecretDefinitionRegistry.Keys.Keycloak.Endpoint)
               || IsDeploymentManaged("Keycloak:Authority");
    }

    private bool IsKeycloakClientIdDeploymentManaged()
    {
        return IsDeploymentManaged(GovernanceSettingKeys.Authentication.KeycloakClientId)
               || IsDeploymentManaged(SecretDefinitionRegistry.Keys.Keycloak.ClientId)
               || IsDeploymentManaged("Keycloak:ClientId");
    }

    private bool IsKeycloakClientSecretDeploymentManaged()
    {
        return IsDeploymentManaged(InfrastructureSecretSettingKeys.Authentication.KeycloakClientSecret)
               || IsDeploymentManaged(SecretDefinitionRegistry.Keys.Keycloak.BlazorClientSecret)
               || IsDeploymentManaged("Keycloak:ClientSecret")
               || IsDeploymentManaged("Keycloak:BlazorClientSecret")
               || IsDeploymentManaged("Authentication:Keycloak:ClientSecret");
    }

    private string ReadConfiguredKeycloakClientSecret()
    {
        return ReadFirstConfigured(
            "Keycloak:ClientSecret",
            "Keycloak:BlazorClientSecret",
            "Authentication:Keycloak:ClientSecret",
            "KEYCLOAK_BLAZOR_CLIENT_SECRET");
    }

    private string ReadFirstConfigured(params string[] keys)
    {
        return keys
            .Select(key => _configuration[key])
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?.Trim() ?? string.Empty;
    }

    private bool IsDeploymentManaged(string key)
    {
        var configuredKeys = _configuration.GetSection("Secrets:Ownership:DeploymentManagedKeys")
            .GetChildren()
            .Select(section => section.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();

        return configuredKeys.Any(candidate =>
            candidate.Equals("*", StringComparison.OrdinalIgnoreCase)
            || candidate.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    private static DTOs.Secrets.SecretOwnershipDto CreateOwnershipMetadata(
        bool deploymentManaged,
        bool configured,
        bool bootstrapAvailable,
        string applicationManagedDescription,
        string deploymentManagedDescription)
    {
        if (deploymentManaged)
        {
            return new DTOs.Secrets.SecretOwnershipDto
            {
                Mode = "deployment-managed",
                Source = "deployment",
                Badge = "Managed by Deployment",
                Description = deploymentManagedDescription,
                Editable = false,
                Configured = configured,
                BootstrapAvailable = false
            };
        }

        return new DTOs.Secrets.SecretOwnershipDto
        {
            Mode = "application-managed",
            Source = bootstrapAvailable ? "deployment-bootstrap" : "application",
            Badge = bootstrapAvailable ? "Bootstrap from Deployment" : "Managed by Application",
            Description = bootstrapAvailable
                ? "This secret was detected from deployment configuration. If you rotate it from the Admin UI, saved application settings will be used from then on."
                : applicationManagedDescription,
            Editable = true,
            Configured = configured,
            BootstrapAvailable = bootstrapAvailable
        };
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
