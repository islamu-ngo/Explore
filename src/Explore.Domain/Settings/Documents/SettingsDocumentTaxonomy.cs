// ABOUTME: Registry gate for typed settings documents and their non-secret storage boundary.
// ABOUTME: Prevents infrastructure credentials from being introduced into JSONB governance documents.

namespace Explore.Domain.Settings.Documents;

using Explore.Domain.Constants;

public static class SettingsDocumentTaxonomy
{
    private static readonly IReadOnlySet<string> KnownSecretKeys = new HashSet<string>(StringComparer.Ordinal)
    {
        InfrastructureSecretSettingKeys.Email.SmtpUsername,
        InfrastructureSecretSettingKeys.Email.SmtpPassword,
        InfrastructureSecretSettingKeys.Storage.AccessKeyId,
        InfrastructureSecretSettingKeys.Storage.SecretAccessKey,
        InfrastructureSecretSettingKeys.Cerbos.CustomAdminUsername,
        InfrastructureSecretSettingKeys.Cerbos.CustomAdminPassword,
        Explore.Domain.Secrets.SecretDefinitionRegistry.Keys.Analytics.PersonalApiKey,
        InfrastructureSecretSettingKeys.Authentication.KeycloakClientSecret,
        InfrastructureSecretSettingKeys.Authentication.GoogleClientSecret,
    };

    public static IReadOnlySet<string> TenantDocumentKeys => SettingsDocumentKeys.Tenant.All;

    public static bool IsAllowedTenantDocument(string documentKey) =>
        SettingsDocumentKeys.Tenant.All.Contains(documentKey);

    public static bool IsKnownSecretKey(string key) => KnownSecretKeys.Contains(key);

    public static bool IsNonSecretTenantDocument(string documentKey) =>
        IsAllowedTenantDocument(documentKey) && !IsKnownSecretKey(documentKey);
}
