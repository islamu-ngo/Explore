// ABOUTME: Enumeration of supported secret provider types.
// Determines which secret manager backend is used for secret retrieval.

namespace Explore.Secrets.Abstractions;

/// <summary>
/// Supported secret manager provider types.
/// </summary>
public enum SecretProviderType
{
    /// <summary>
    /// No external secret manager - uses environment variables only.
    /// Suitable for self-hosters and local development.
    /// </summary>
    None = 0,

    /// <summary>
    /// Infisical secret manager with Universal Auth.
    /// </summary>
    Infisical = 1,

    /// <summary>
    /// HashiCorp Vault with AppRole authentication.
    /// </summary>
    Vault = 2,

    /// <summary>
    /// Azure Key Vault with DefaultAzureCredential (Managed Identity).
    /// </summary>
    AzureKeyVault = 3,

    /// <summary>
    /// AWS Secrets Manager with IRSA credential chain.
    /// </summary>
    AwsSecretsManager = 4
}
