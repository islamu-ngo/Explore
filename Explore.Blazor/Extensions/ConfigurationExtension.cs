using Microsoft.Extensions.Configuration;

namespace Explore.Blazor.Extensions;

public static class ConfigurationExtensions
{
    public static void AddInfisicalBlazorCompatibility(this IConfigurationBuilder configBuilder)
    {
        var tempConfig = configBuilder.Build();

        // 1. Read Raw Values
        var rawRealm = tempConfig["KEYCLOAK_REALM"] ?? "islamu-dev";
        var rawClientSecret = tempConfig["EXPLORE_BLAZOR_SERVER_CLIENT_SECRET_COOLIFY"];
        var rawApiUrl = tempConfig["EXPLORE_API_BASE_URL"]; // You should add this to Coolify env vars

        // 2. Construct Derived Values
        var keycloakAuthority = $"https://keycloak.openislamu.org/realms/{rawRealm}";

        var mappedConfig = new Dictionary<string, string?>();

        // --- Keycloak Mapping ---
        mappedConfig["Keycloak:Authority"] = keycloakAuthority;
        mappedConfig["Keycloak:ClientId"] = "explore-blazor-server"; // Hardcoded ID matching your setup
        mappedConfig["Keycloak:ClientSecret"] = rawClientSecret;
        mappedConfig["Keycloak:RequireHttpsMetadata"] = "true";

        // --- API Mapping ---
        // If you set EXPLORE_API_BASE_URL in Coolify, it maps here.
        // Otherwise defaults to localhost (dev) or whatever is in appsettings.
        if (!string.IsNullOrEmpty(rawApiUrl))
        {
            mappedConfig["ExploreApi:BaseUrl"] = rawApiUrl;
        }

        // --- Google Maps ---
        // Since your secret in Infisical is already named "GoogleMaps__ApiKey",
        // .NET automatically reads that as "GoogleMaps:ApiKey".
        // We don't need to manually map it unless you rename it to something like "GOOGLE_MAPS_KEY".

        // 3. Inject
        configBuilder.AddInMemoryCollection(
            mappedConfig.Where(kv => !string.IsNullOrEmpty(kv.Value))
                        .ToDictionary(kv => kv.Key, kv => kv.Value)!
        );
    }
}
