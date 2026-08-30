// ABOUTME: Adds the isolated Infisical authority source to IConfigurationBuilder.
// ABOUTME: Contains no database-backed or lower-authority fallback source.

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
        var url = configuration["SecretProvider:Infisical:Url"]
            ?? configuration["INFISICAL_URL"];
        var environment = configuration["SecretProvider:Infisical:Environment"]
            ?? configuration["INFISICAL_ENV"];

        if (string.IsNullOrEmpty(projectId)
            || string.IsNullOrEmpty(clientId)
            || string.IsNullOrEmpty(clientSecret)
            || string.IsNullOrEmpty(url)
            || string.IsNullOrEmpty(environment))
        {
            throw new InvalidOperationException(
                "Infisical authority requires an explicit URL, environment, project, and universal-auth credentials.");
        }

        var source = new InfisicalConfigurationSource
        {
            Url = url,
            ProjectId = projectId,
            ClientId = clientId,
            ClientSecret = clientSecret,
            Environment = environment,
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

}
