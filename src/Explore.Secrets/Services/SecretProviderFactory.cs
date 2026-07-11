// ABOUTME: Factory for creating secret provider instances based on configuration.
// Instantiates the appropriate provider type from SecretProviderOptions.

using Explore.Secrets.Abstractions;
using Explore.Secrets.Configuration;
using Explore.Secrets.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Secrets.Services;

/// <summary>
/// Factory for creating <see cref="ISecretProvider"/> instances.
/// Determines the provider type from configuration and instantiates appropriately.
/// </summary>
public sealed class SecretProviderFactory
{
    private readonly IOptions<SecretProviderOptions> _options;
    private readonly ILoggerFactory _loggerFactory;

    public SecretProviderFactory(
        IOptions<SecretProviderOptions> options,
        ILoggerFactory loggerFactory)
    {
        _options = options;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Creates a secret provider based on the configured provider type.
    /// </summary>
    /// <returns>An initialized secret provider instance.</returns>
    /// <exception cref="SecretProviderException">When the provider type is not supported.</exception>
    public ISecretProvider Create()
    {
        var options = _options.Value;
        var logger = _loggerFactory.CreateLogger<SecretProviderFactory>();

        logger.LogInformation(
            "Creating secret provider of type: {ProviderType}",
            options.Provider);

        return options.Provider switch
        {
            SecretProviderType.None => CreateEnvironmentProvider(),
            SecretProviderType.Infisical => CreateInfisicalProvider(),
            SecretProviderType.Vault => CreateVaultProvider(),
            SecretProviderType.AzureKeyVault => CreateAzureKeyVaultProvider(),
            SecretProviderType.AwsSecretsManager => CreateAwsSecretsManagerProvider(),
            _ => throw new SecretProviderException(
                $"Unsupported secret provider type: {options.Provider}",
                options.Provider,
                "Create",
                isTransient: false)
        };
    }

    private ISecretProvider CreateEnvironmentProvider()
    {
        var logger = _loggerFactory.CreateLogger<EnvironmentSecretProvider>();
        return new EnvironmentSecretProvider(logger);
    }

    private ISecretProvider CreateInfisicalProvider()
    {
        var logger = _loggerFactory.CreateLogger<InfisicalSecretProvider>();
        return new InfisicalSecretProvider(logger, _options);
    }

    private ISecretProvider CreateVaultProvider()
    {
        // Will be implemented in Phase 5A
        throw new SecretProviderException(
            "Vault provider not yet implemented. Use 'None' for environment variables.",
            SecretProviderType.Vault,
            "Create",
            isTransient: false);
    }

    private ISecretProvider CreateAzureKeyVaultProvider()
    {
        // Will be implemented in Phase 5B
        throw new SecretProviderException(
            "Azure Key Vault provider not yet implemented. Use 'None' for environment variables.",
            SecretProviderType.AzureKeyVault,
            "Create",
            isTransient: false);
    }

    private ISecretProvider CreateAwsSecretsManagerProvider()
    {
        // Will be implemented in Phase 5C
        throw new SecretProviderException(
            "AWS Secrets Manager provider not yet implemented. Use 'None' for environment variables.",
            SecretProviderType.AwsSecretsManager,
            "Create",
            isTransient: false);
    }
}
