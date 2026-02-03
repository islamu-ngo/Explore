// ABOUTME: Validates required secrets are configured based on provider type.
// Used with ValidateOnStart for fail-fast behavior in production.

using Explore.Secrets.Abstractions;
using Explore.Secrets.Configuration;
using Microsoft.Extensions.Options;

namespace Explore.Secrets.Validation;

/// <summary>
/// Validates <see cref="SecretProviderOptions"/> configuration.
/// Ensures required settings are present based on the selected provider type.
/// </summary>
public sealed class SecretProviderOptionsValidator : IValidateOptions<SecretProviderOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, SecretProviderOptions options)
    {
        var errors = new List<string>();

        switch (options.Provider)
        {
            case SecretProviderType.None:
                // No validation needed for environment-only mode
                break;

            case SecretProviderType.Infisical:
                ValidateInfisicalOptions(options.Infisical, errors);
                break;

            case SecretProviderType.Vault:
                ValidateVaultOptions(options.Vault, errors);
                break;

            case SecretProviderType.AzureKeyVault:
                ValidateAzureKeyVaultOptions(options.AzureKeyVault, errors);
                break;

            case SecretProviderType.AwsSecretsManager:
                ValidateAwsSecretsManagerOptions(options.AwsSecretsManager, errors);
                break;

            default:
                errors.Add($"Unknown secret provider type: {options.Provider}");
                break;
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateInfisicalOptions(InfisicalOptions options, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(options.Url))
            errors.Add("Infisical URL is required (SecretProvider:Infisical:Url)");

        if (string.IsNullOrWhiteSpace(options.ProjectId))
            errors.Add("Infisical Project ID is required (SecretProvider:Infisical:ProjectId)");

        if (string.IsNullOrWhiteSpace(options.ClientId))
            errors.Add("Infisical Client ID is required (SecretProvider:Infisical:ClientId)");

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
            errors.Add("Infisical Client Secret is required (SecretProvider:Infisical:ClientSecret)");

        if (string.IsNullOrWhiteSpace(options.Environment))
            errors.Add("Infisical Environment is required (SecretProvider:Infisical:Environment)");

        if (!Uri.TryCreate(options.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            errors.Add("Infisical URL must be a valid HTTP/HTTPS URL");
        }
    }

    private static void ValidateVaultOptions(VaultOptions options, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(options.Url))
            errors.Add("Vault URL is required (SecretProvider:Vault:Url)");

        if (string.IsNullOrWhiteSpace(options.RoleId))
            errors.Add("Vault Role ID is required (SecretProvider:Vault:RoleId)");

        if (string.IsNullOrWhiteSpace(options.SecretId))
            errors.Add("Vault Secret ID is required (SecretProvider:Vault:SecretId)");

        if (!Uri.TryCreate(options.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            errors.Add("Vault URL must be a valid HTTP/HTTPS URL");
        }

        if (options.Paths.Count == 0)
            errors.Add("At least one Vault secret path is required (SecretProvider:Vault:Paths)");
    }

    private static void ValidateAzureKeyVaultOptions(AzureKeyVaultOptions options, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(options.VaultUrl))
            errors.Add("Azure Key Vault URL is required (SecretProvider:AzureKeyVault:VaultUrl)");

        if (!Uri.TryCreate(options.VaultUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != "https" ||
            !uri.Host.EndsWith(".vault.azure.net", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Azure Key Vault URL must be a valid HTTPS URL ending in .vault.azure.net");
        }

        // Service Principal auth requires all three: TenantId, ClientId, ClientSecret
        var hasServicePrincipal = !string.IsNullOrWhiteSpace(options.ClientId);
        if (hasServicePrincipal)
        {
            if (string.IsNullOrWhiteSpace(options.TenantId))
                errors.Add("Azure AD Tenant ID is required when using Service Principal auth");
            if (string.IsNullOrWhiteSpace(options.ClientSecret))
                errors.Add("Azure AD Client Secret is required when using Service Principal auth");
        }
    }

    private static void ValidateAwsSecretsManagerOptions(AwsSecretsManagerOptions options, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(options.Region))
            errors.Add("AWS Region is required (SecretProvider:AwsSecretsManager:Region)");

        if (options.SecretNames.Count == 0)
            errors.Add("At least one AWS secret name is required (SecretProvider:AwsSecretsManager:SecretNames)");

        // Explicit credentials require both access key and secret
        var hasExplicitCredentials = !string.IsNullOrWhiteSpace(options.AccessKeyId);
        if (hasExplicitCredentials && string.IsNullOrWhiteSpace(options.SecretAccessKey))
        {
            errors.Add("AWS Secret Access Key is required when Access Key ID is provided");
        }
    }
}
