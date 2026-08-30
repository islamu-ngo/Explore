// ABOUTME: Builds configuration from exactly one deployment-selected secret authority.
// ABOUTME: Supports explicit Environment and Infisical modes without lower-source fallback.

using Explore.Secrets.Abstractions;
using Explore.Secrets.Extensions;
using Microsoft.Extensions.Configuration;

namespace Explore.Secrets.Configuration;

public static class SecretAuthorityConfiguration
{
    public static SecretProviderType GetRequiredProvider(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        string? configured = configuration[$"{SecretProviderOptions.SectionName}:Provider"]
            ?? configuration["SECRET_PROVIDER"];

        if (!Enum.TryParse(configured, ignoreCase: true, out SecretProviderType provider)
            || provider is not (SecretProviderType.Environment or SecretProviderType.Infisical))
        {
            throw new InvalidOperationException(
                "SecretProvider:Provider must explicitly select Environment or Infisical.");
        }

        return provider;
    }

    public static IConfiguration Build(
        IConfiguration bootstrapConfiguration,
        params string[] infisicalPaths)
    {
        SecretProviderType provider = GetRequiredProvider(bootstrapConfiguration);
        if (provider == SecretProviderType.Environment)
        {
            return new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();
        }

        var builder = new ConfigurationBuilder();
        builder.AddInfisical(bootstrapConfiguration, source =>
        {
            source.Paths.Clear();
            source.Paths.AddRange(infisicalPaths);
            source.ThrowOnFirstLoadFailure = true;
        });
        return builder.Build();
    }
}
