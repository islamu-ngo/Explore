// ABOUTME: Configuration extensions for the Blazor Server project.
// ABOUTME: Adds Infisical as configuration source and maps Infisical secret names to .NET config keys.

using Explore.Secrets.Extensions;

namespace Explore.Blazor.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds Infisical secrets and maps them to canonical .NET configuration keys for Blazor Server.
    /// </summary>
    public static void AddInfisicalBlazorCompatibility(this IConfigurationBuilder configBuilder)
    {
        var bootstrapConfig = configBuilder.Build();

        Console.WriteLine("[Blazor Infisical] Checking bootstrap credentials...");
        Console.WriteLine($"[Blazor Infisical] ProjectId: {(string.IsNullOrEmpty(bootstrapConfig["Infisical:ProjectId"]) ? "(not set)" : "(set)")}");
        Console.WriteLine($"[Blazor Infisical] ClientId: {(string.IsNullOrEmpty(bootstrapConfig["Infisical:ClientId"]) ? "(not set)" : "(set)")}");
        Console.WriteLine($"[Blazor Infisical] HasClientSecret: {!string.IsNullOrEmpty(bootstrapConfig["Infisical:ClientSecret"])}");

        configBuilder.AddInfisical(bootstrapConfig, source =>
        {
            source.Paths.Clear();
            source.Paths.AddRange(["/keycloak", "/blazor", "/api", "/postgresql"]);
            source.ThrowOnFirstLoadFailure = false;
        });

        var configWithSecrets = configBuilder.Build();
        ApplyBlazorMapping(configBuilder, configWithSecrets);
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

        Console.WriteLine("[Blazor Infisical] Keycloak configuration mapped:");
        Console.WriteLine($"  HasKeycloakInput: {hasKeycloakInput}");
        Console.WriteLine($"  Authority: {keycloakAuthority ?? "(not mapped)"}");
        Console.WriteLine($"  ClientId: {keycloakClientId ?? "(not mapped)"}");
        Console.WriteLine($"  HasClientSecret: {!string.IsNullOrEmpty(rawClientSecret)}");
        Console.WriteLine($"  HasGoogleClientId: {!string.IsNullOrEmpty(rawGoogleClientId)}");
        Console.WriteLine($"  HasGoogleClientSecret: {!string.IsNullOrEmpty(rawGoogleClientSecret)}");
        Console.WriteLine($"  API BaseUrl: {rawApiUrl ?? "(not set, will use default)"}");

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
        if (!string.IsNullOrEmpty(rawApiUrl))
        {
            TrySet(mappedConfig, config, "ExploreApi:BaseUrl", rawApiUrl);
        }

        configBuilder.AddInMemoryCollection(
            mappedConfig.Where(kv => !string.IsNullOrEmpty(kv.Value))
                        .ToDictionary(kv => kv.Key, kv => kv.Value)!
        );
    }
}
