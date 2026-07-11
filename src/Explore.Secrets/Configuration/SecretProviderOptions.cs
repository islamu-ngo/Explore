// ABOUTME: Configuration options for secret providers.
// Supports Infisical, Vault, Azure Key Vault, and AWS Secrets Manager.

using Explore.Secrets.Abstractions;

namespace Explore.Secrets.Configuration;

/// <summary>
/// Configuration options for the secret provider system.
/// Bound from "SecretProvider" configuration section.
/// </summary>
public sealed class SecretProviderOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "SecretProvider";

    /// <summary>
    /// The type of secret provider to use.
    /// Defaults to None (environment variables only).
    /// </summary>
    public SecretProviderType Provider { get; set; } = SecretProviderType.None;

    /// <summary>
    /// Whether to fail fast on initialization errors in production.
    /// When true, application won't start if secrets are unavailable.
    /// </summary>
    public bool FailFast { get; set; } = true;

    /// <summary>
    /// Infisical-specific settings.
    /// </summary>
    public InfisicalOptions Infisical { get; set; } = new();

    /// <summary>
    /// HashiCorp Vault-specific settings.
    /// </summary>
    public VaultOptions Vault { get; set; } = new();

    /// <summary>
    /// Azure Key Vault-specific settings.
    /// </summary>
    public AzureKeyVaultOptions AzureKeyVault { get; set; } = new();

    /// <summary>
    /// AWS Secrets Manager-specific settings.
    /// </summary>
    public AwsSecretsManagerOptions AwsSecretsManager { get; set; } = new();
}

/// <summary>
/// Infisical secret manager configuration.
/// </summary>
public sealed class InfisicalOptions
{
    /// <summary>
    /// Infisical server URL (e.g., "https://infisical.example.com").
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Infisical project ID.
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// Client ID for Universal Auth.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Client secret for Universal Auth.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Environment slug (e.g., "dev", "staging", "prod").
    /// </summary>
    public string Environment { get; set; } = "dev";

    /// <summary>
    /// Secret paths to load (e.g., "/api", "/keycloak").
    /// If empty, loads from root path "/".
    /// </summary>
    public List<string> Paths { get; set; } = new() { "/" };
}

/// <summary>
/// HashiCorp Vault configuration.
/// </summary>
public sealed class VaultOptions
{
    /// <summary>
    /// Vault server URL (e.g., "https://vault.example.com:8200").
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// AppRole Role ID for authentication.
    /// </summary>
    public string? RoleId { get; set; }

    /// <summary>
    /// AppRole Secret ID for authentication.
    /// Can be a wrapped token for enhanced security.
    /// </summary>
    public string? SecretId { get; set; }

    /// <summary>
    /// Whether SecretId is a response-wrapped token.
    /// </summary>
    public bool SecretIdIsWrapped { get; set; }

    /// <summary>
    /// KV secrets engine mount path (default: "secret").
    /// </summary>
    public string MountPath { get; set; } = "secret";

    /// <summary>
    /// Secret paths within the mount (e.g., "explore/api").
    /// </summary>
    public List<string> Paths { get; set; } = new();

    /// <summary>
    /// Namespace for Vault Enterprise (optional).
    /// </summary>
    public string? Namespace { get; set; }
}

/// <summary>
/// Azure Key Vault configuration.
/// </summary>
public sealed class AzureKeyVaultOptions
{
    /// <summary>
    /// Key Vault URL (e.g., "https://mykeyvault.vault.azure.net/").
    /// </summary>
    public string? VaultUrl { get; set; }

    /// <summary>
    /// Azure AD Tenant ID (optional, for service principal auth).
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Azure AD Client ID (optional, for service principal auth).
    /// Leave empty to use Managed Identity.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Azure AD Client Secret (optional, for service principal auth).
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Secret name prefix to filter (optional).
    /// </summary>
    public string? SecretPrefix { get; set; }
}

/// <summary>
/// AWS Secrets Manager configuration.
/// </summary>
public sealed class AwsSecretsManagerOptions
{
    /// <summary>
    /// AWS region (e.g., "us-east-1", "eu-west-1").
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Access Key ID (optional, for explicit credentials).
    /// Leave empty to use IRSA or instance profile.
    /// </summary>
    public string? AccessKeyId { get; set; }

    /// <summary>
    /// Secret Access Key (optional, for explicit credentials).
    /// </summary>
    public string? SecretAccessKey { get; set; }

    /// <summary>
    /// Secret names or ARNs to load.
    /// </summary>
    public List<string> SecretNames { get; set; } = new();

    /// <summary>
    /// Whether secret values are JSON that should be flattened.
    /// </summary>
    public bool FlattenJsonSecrets { get; set; } = true;
}
