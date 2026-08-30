// ABOUTME: Configuration extensions for the Blazor Server project.
// ABOUTME: Adds Infisical as configuration source and maps Infisical secret names to .NET config keys.

using Explore.Blazor.Configuration;
using Explore.Blazor.Services.Auth;

namespace Explore.Blazor.Extensions;

public static class ConfigurationExtensions
{
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
    /// Adds Infisical secrets and maps them to canonical .NET configuration keys for Blazor Server.
    /// </summary>
    public static void AddSecretAuthorityConfiguration(this IConfigurationBuilder configBuilder)
    {
        var bootstrapConfig = configBuilder.Build();
        string? configuredProvider = bootstrapConfig["SecretProvider:Provider"]
            ?? bootstrapConfig["SECRET_PROVIDER"];
        if (string.Equals(configuredProvider, "Environment", StringComparison.OrdinalIgnoreCase))
        {
            ApplyBlazorMapping(
                configBuilder,
                new ConfigurationBuilder().AddEnvironmentVariables().Build());
            return;
        }

        if (!string.Equals(configuredProvider, "Infisical", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "SecretProvider:Provider must explicitly select Environment or Infisical.");
        }

        var authorityBuilder = new ConfigurationBuilder();
        authorityBuilder.AddInfisical(bootstrapConfig, source =>
        {
            source.Paths.Clear();
            source.Paths.AddRange(["/keycloak", "/blazor", "/atproto"]);
            source.ThrowOnFirstLoadFailure = true;
        });
        ApplyBlazorMapping(configBuilder, authorityBuilder.Build());
    }

    /// <summary>
    /// Maps Infisical secret names to .NET configuration keys for Blazor Server.
    /// </summary>
    /// <remarks>
    /// Canonical Infisical keys:
    ///   /keycloak: KEYCLOAK_ENDPOINT, KEYCLOAK_REALM, KEYCLOAK_CLIENT_ID, KEYCLOAK_BLAZOR_CLIENT_SECRET
    ///   /blazor:   API_ENDPOINT, GOOGLE_CLIENT_ID, GOOGLE_CLIENT_SECRET
    /// </remarks>
    private static void ApplyBlazorMapping(IConfigurationBuilder configBuilder, IConfiguration config)
    {
        var rawRealm = config["KEYCLOAK_REALM"] ?? config["Keycloak:Realm"];
        var rawKeycloakClientId = config["KEYCLOAK_CLIENT_ID"] ?? config["Keycloak:ClientId"];
        var rawClientSecret = config["KEYCLOAK_BLAZOR_CLIENT_SECRET"] ?? config["Keycloak:ClientSecret"];
        var rawGoogleClientId = config["GOOGLE_CLIENT_ID"] ?? config["Google:ClientId"];
        var rawGoogleClientSecret = config["GOOGLE_CLIENT_SECRET"] ?? config["Google:ClientSecret"];
        var rawApiUrl = config["API_ENDPOINT"] ?? config["ExploreApi:BaseUrl"];
        var rawAtprotoOAuthClientPrivateJwks = config["ATPROTO_OAUTH_CLIENT_PRIVATE_JWKS"]
            ?? config[AtprotoClientKeyProvider.ConfigurationKey];
        var hasAspireApiReference =
            !string.IsNullOrWhiteSpace(GetAspireApiReference(config, "http"))
            || !string.IsNullOrWhiteSpace(GetAspireApiReference(config, "https"));
        var baseUrl = config["KEYCLOAK_ENDPOINT"];
        var explicitAuthority = config["Keycloak:Authority"];

        var hasKeycloakInput =
            !string.IsNullOrWhiteSpace(explicitAuthority)
            || !string.IsNullOrWhiteSpace(baseUrl)
            || !string.IsNullOrWhiteSpace(rawRealm)
            || !string.IsNullOrWhiteSpace(rawKeycloakClientId)
            || !string.IsNullOrWhiteSpace(rawClientSecret);

        string? keycloakAuthority = null;
        if (!string.IsNullOrWhiteSpace(explicitAuthority))
        {
            keycloakAuthority = explicitAuthority.TrimEnd('/');
        }
        else if (!string.IsNullOrWhiteSpace(baseUrl) && !string.IsNullOrWhiteSpace(rawRealm))
        {
            keycloakAuthority = $"{baseUrl.TrimEnd('/')}/realms/{rawRealm}";
        }

        var keycloakClientId = rawKeycloakClientId;
        if (string.IsNullOrWhiteSpace(keycloakClientId) && !string.IsNullOrWhiteSpace(keycloakAuthority))
        {
            keycloakClientId = "islamu-event-blazor";
        }

        var metadataAddress = string.IsNullOrWhiteSpace(keycloakAuthority)
            ? null
            : $"{keycloakAuthority}/.well-known/openid-configuration";

        var mappedConfig = new Dictionary<string, string?>();

        static void TrySet(IDictionary<string, string?> dict, IConfiguration root, string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.IsNullOrEmpty(root[key]))
                return;
            dict[key] = value;
        }

        // Keycloak
        if (hasKeycloakInput)
        {
            TrySet(mappedConfig, config, "Keycloak:Realm", rawRealm);
            TrySet(mappedConfig, config, "Keycloak:Authority", keycloakAuthority);
            TrySet(mappedConfig, config, "Keycloak:MetadataAddress", metadataAddress);
            TrySet(mappedConfig, config, "Keycloak:ClientId", keycloakClientId);
            TrySet(mappedConfig, config, "Keycloak:RequireHttpsMetadata", "true");
        }

        if (!string.IsNullOrWhiteSpace(rawClientSecret))
        {
            mappedConfig["Keycloak:ClientSecret"] = rawClientSecret;
        }

        // Google
        TrySet(mappedConfig, config, "Google:ClientId", rawGoogleClientId);
        if (!string.IsNullOrWhiteSpace(rawGoogleClientSecret))
        {
            mappedConfig["Google:ClientSecret"] = rawGoogleClientSecret;
        }

        // API
        if (!hasAspireApiReference && !string.IsNullOrEmpty(rawApiUrl))
        {
            TrySet(mappedConfig, config, "ExploreApi:BaseUrl", rawApiUrl);
        }

        if (!string.IsNullOrWhiteSpace(rawAtprotoOAuthClientPrivateJwks))
        {
            mappedConfig[AtprotoClientKeyProvider.ConfigurationKey] = rawAtprotoOAuthClientPrivateJwks;
        }

        configBuilder.AddInMemoryCollection(
            mappedConfig.Where(kv => !string.IsNullOrEmpty(kv.Value))
                        .ToDictionary(kv => kv.Key, kv => kv.Value)!
        );
    }

    private static string? GetAspireApiReference(IConfiguration config, string scheme) =>
        config[$"services:explore-api:{scheme}:0"]
        ?? config[$"services__explore-api__{scheme}__0"];
}
