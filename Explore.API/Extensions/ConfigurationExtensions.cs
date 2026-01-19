// Explore.API/Extensions/ConfigurationExtensions.cs
namespace Explore.API.Extensions;

public static class ConfigurationExtensions
{
    public static void AddInfisicalCompatibility(this IConfigurationBuilder configBuilder)
    {
        // Build the current config to read the "Raw" environment variables
        // We build a temporary version just to read the current values
        var tempConfig = configBuilder.Build();

        // 1. Read Raw Values (The names Infisical/Docker uses)
        var rawDbUrl = tempConfig["POSTGRESQL_PUBLIC_URL"];
        var rawRealm = tempConfig["KEYCLOAK_REALM"] ?? "islamu-dev";

        // 2. Compute Derived Values (The logic needed for Keycloak)
        // This makes your app robust: if variables change, you only update this one file.
        var keycloakAuthority = $"https://keycloak.openislamu.org/realms/{rawRealm}";

        // 3. Create the Mapping Dictionary
        // This translates "External World" -> "Internal .NET World"
        var mappedConfig = new Dictionary<string, string?>();

        // Map Database if it exists
        if (!string.IsNullOrEmpty(rawDbUrl))
        {
            mappedConfig["ConnectionStrings:DefaultConnection"] = rawDbUrl;
        }

        // Map Keycloak
        mappedConfig["Keycloak:Realm"] = rawRealm;
        mappedConfig["Keycloak:Authority"] = keycloakAuthority;
        mappedConfig["Keycloak:MetadataAddress"] = $"{keycloakAuthority}/.well-known/openid-configuration";
        mappedConfig["Keycloak:Audience"] = "explore-api";
        mappedConfig["Keycloak:RequireHttpsMetadata"] = "true";

        // Map S3 (Centralizing your S3 logic here too for consistency)
        mappedConfig["S3Settings:Region"] = tempConfig["ISLAMU_EVENT_REGION"];
        mappedConfig["S3Settings:BucketName"] = tempConfig["ISLAMU_EVENT_PRIVATE_BUCKET_NAME"];
        mappedConfig["S3Settings:AccessKeyId"] = tempConfig["ISLAMU_EVENT_PRIVATE_ACCESS_KEY_ID"];
        mappedConfig["S3Settings:SecretAccessKey"] = tempConfig["ISLAMU_EVENT_PRIVATE_SECRET_ACCESS_KEY_ID"];
        mappedConfig["S3Settings:Endpoint"] = tempConfig["ISLAMU_EVENT_S3_ENDPOINT"];

        // 4. Inject the Mapped Configuration back into the pipeline
        // The .NET app will now see these keys as if they were in appsettings.json
        configBuilder.AddInMemoryCollection(
            mappedConfig.Where(kv => !string.IsNullOrEmpty(kv.Value))
                        .ToDictionary(kv => kv.Key, kv => kv.Value)!
        );
    }
}
