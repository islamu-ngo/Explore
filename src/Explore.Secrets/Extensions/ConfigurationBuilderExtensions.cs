// ABOUTME: Adds Infisical and database-backed secret sources to IConfigurationBuilder.
// ABOUTME: Owns the API-side startup configuration implementation for Explore.Secrets consumers.

namespace Explore.Secrets.Extensions;

using Explore.Secrets.Configuration;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Extension methods for configuring secret sources in IConfigurationBuilder.
/// </summary>
public static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds Infisical as a configuration source.
    /// Secrets are loaded from Infisical and made available through IConfiguration.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="configuration">Configuration containing Infisical bootstrap credentials.</param>
    /// <param name="configure">Optional action to configure additional options.</param>
    /// <returns>The configuration builder for chaining.</returns>
    /// <remarks>
    /// Expected deployment keys use <c>SecretProvider:Infisical:*</c> (or the
    /// documented <c>INFISICAL_*</c> environment bootstrap inputs). Appsettings and
    /// .NET User Secrets are not supported secret origins.
    /// </remarks>
    public static IConfigurationBuilder AddInfisical(
        this IConfigurationBuilder builder,
        IConfiguration configuration,
        Action<InfisicalConfigurationSource>? configure = null)
    {
        var projectId = configuration["SecretProvider:Infisical:ProjectId"]
            ?? configuration["INFISICAL_PROJECT_ID"];
        var clientId = configuration["SecretProvider:Infisical:ClientId"]
            ?? configuration["INFISICAL_CLIENT_ID"];
        var clientSecret = configuration["SecretProvider:Infisical:ClientSecret"]
            ?? configuration["INFISICAL_CLIENT_SECRET"];

        if (string.IsNullOrEmpty(projectId)
            || string.IsNullOrEmpty(clientId)
            || string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException(
                "Infisical authority requires its project and universal-auth credentials.");
        }

        var source = new InfisicalConfigurationSource
        {
            Url = configuration["SecretProvider:Infisical:Url"]
                ?? configuration["INFISICAL_URL"]
                ?? "https://app.infisical.com",
            ProjectId = projectId,
            ClientId = clientId,
            ClientSecret = clientSecret,
            Environment = configuration["SecretProvider:Infisical:Environment"]
                ?? configuration["INFISICAL_ENV"]
                ?? "dev",
        };

        var paths = configuration.GetSection("SecretProvider:Infisical:Paths").Get<List<string>>();
        if (paths is { Count: > 0 })
        {
            source.Paths.Clear();
            source.Paths.AddRange(paths);
        }

        configure?.Invoke(source);

        return builder.Add(source);
    }

    /// <summary>
    /// Adds Infisical as a configuration source with explicit credentials.
    /// Use this when you want to pass credentials directly rather than from configuration.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="projectId">Infisical project ID.</param>
    /// <param name="clientId">Universal Auth client ID.</param>
    /// <param name="clientSecret">Universal Auth client secret.</param>
    /// <param name="configure">Optional action to configure additional options.</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddInfisical(
        this IConfigurationBuilder builder,
        string projectId,
        string clientId,
        string clientSecret,
        Action<InfisicalConfigurationSource>? configure = null)
    {
        var source = new InfisicalConfigurationSource
        {
            ProjectId = projectId,
            ClientId = clientId,
            ClientSecret = clientSecret,
        };

        configure?.Invoke(source);

        return builder.Add(source);
    }

    /// <summary>
    /// Adds database configuration provider for encrypted AppSettings.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="connectionString">Database connection string.</param>
    /// <param name="encryptionOptions">Encryption options for decrypting values.</param>
    /// <param name="configure">Optional action to configure additional options.</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddDatabaseConfiguration(
        this IConfigurationBuilder builder,
        string connectionString,
        EncryptionOptions encryptionOptions,
        Action<DbConfigurationSource>? configure = null)
    {
        var source = new DbConfigurationSource
        {
            ConnectionString = connectionString,
            EncryptionOptions = encryptionOptions
        };

        configure?.Invoke(source);

        return builder.Add(source);
    }
}
