// ABOUTME: Adds Infisical compatibility loading for the separate control-plane Blazor BFF host.
// ABOUTME: Maps dedicated control-plane Keycloak and API secrets into canonical runtime configuration keys.

using Explore.Secrets.Extensions;
using Microsoft.Extensions.Logging;

namespace Event.ControlPlane.Blazor.Extensions;

public static class ConfigurationExtensions
{
    private static readonly ILoggerFactory BootstrapLoggerFactory =
        LoggerFactory.Create(builder => builder.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.IncludeScopes = false;
        }));

    private static readonly ILogger BootstrapLogger =
        BootstrapLoggerFactory.CreateLogger("Event.ControlPlane.Blazor.Bootstrap.Infisical");

    public static void AddInfisicalControlPlaneCompatibility(this IConfigurationBuilder configBuilder)
    {
        var bootstrapConfig = configBuilder.Build();

        BootstrapLogger.LogInformation(
            "Control-plane Infisical bootstrap state: ProjectId={ProjectIdState}, ClientId={ClientIdState}, HasClientSecret={HasClientSecret}",
            string.IsNullOrEmpty(bootstrapConfig["Infisical:ProjectId"]) ? "(not set)" : "(set)",
            string.IsNullOrEmpty(bootstrapConfig["Infisical:ClientId"]) ? "(not set)" : "(set)",
            !string.IsNullOrEmpty(bootstrapConfig["Infisical:ClientSecret"]));

        configBuilder.AddInfisical(bootstrapConfig, source =>
        {
            source.Paths.Clear();
            source.Paths.AddRange(["/keycloak", "/control-plane", "/blazor"]);
            source.ThrowOnFirstLoadFailure = false;
        });

        var configWithSecrets = configBuilder.Build();
        ApplyControlPlaneMapping(configBuilder, configWithSecrets);
    }

    private static void ApplyControlPlaneMapping(
        IConfigurationBuilder configBuilder,
        IConfiguration config)
    {
        var rawRealm = config["KEYCLOAK_REALM"] ?? config["Keycloak:Realm"];
        var rawClientId = config["KEYCLOAK_CONTROL_PLANE_CLIENT_ID"]
            ?? config["ControlPlane:Keycloak:ClientId"]
            ?? "islamu-event-control-plane";
        var rawClientSecret = config["KEYCLOAK_CONTROL_PLANE_CLIENT_SECRET"]
            ?? config["ControlPlane:Keycloak:ClientSecret"];
        var rawApiUrl = config["CONTROL_PLANE_API_ENDPOINT"]
            ?? config["API_ENDPOINT"]
            ?? config["ExploreApi:BaseUrl"];
        var hasAspireApiReference =
            !string.IsNullOrWhiteSpace(GetAspireApiReference(config, "https"))
            || !string.IsNullOrWhiteSpace(GetAspireApiReference(config, "http"));
        var explicitAuthority = config["KEYCLOAK_CONTROL_PLANE_AUTHORITY"]
            ?? config["ControlPlane:Keycloak:Authority"]
            ?? config["Bff:Authentication:Authority"];
        var baseUrl = config["KEYCLOAK_ENDPOINT"];

        string? keycloakAuthority = null;
        if (!string.IsNullOrWhiteSpace(explicitAuthority))
        {
            keycloakAuthority = explicitAuthority.TrimEnd('/');
        }
        else if (!string.IsNullOrWhiteSpace(baseUrl) && !string.IsNullOrWhiteSpace(rawRealm))
        {
            keycloakAuthority = $"{baseUrl.TrimEnd('/')}/realms/{rawRealm}";
        }

        var metadataAddress = string.IsNullOrWhiteSpace(keycloakAuthority)
            ? null
            : $"{keycloakAuthority}/.well-known/openid-configuration";

        BootstrapLogger.LogInformation(
            "Control-plane Keycloak mapping: Authority={Authority}, ClientId={ClientId}, HasClientSecret={HasClientSecret}, ApiBaseUrl={ApiBaseUrl}",
            keycloakAuthority ?? "(not mapped)",
            rawClientId,
            !string.IsNullOrWhiteSpace(rawClientSecret),
            hasAspireApiReference
                ? "(not mapped, Aspire service discovery configured)"
                : rawApiUrl ?? "(not set, will use default)");

        var mappedConfig = new Dictionary<string, string?>();

        TrySet(mappedConfig, config, "Bff:Authentication:Authority", keycloakAuthority);
        TrySet(mappedConfig, config, "Bff:Authentication:MetadataAddress", metadataAddress);
        TrySet(mappedConfig, config, "Bff:Authentication:ClientId", rawClientId);
        TrySet(mappedConfig, config, "Bff:Authentication:RequireHttpsMetadata", "true");

        if (!string.IsNullOrWhiteSpace(rawClientSecret))
        {
            mappedConfig["Bff:Authentication:ClientSecret"] = rawClientSecret;
        }

        if (!hasAspireApiReference)
        {
            TrySet(mappedConfig, config, "ExploreApi:BaseUrl", rawApiUrl);
        }

        configBuilder.AddInMemoryCollection(
            mappedConfig
                .Where(pair => !string.IsNullOrEmpty(pair.Value))
                .ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    private static void TrySet(
        IDictionary<string, string?> mappedConfig,
        IConfiguration root,
        string key,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !string.IsNullOrEmpty(root[key]))
        {
            return;
        }

        mappedConfig[key] = value;
    }

    private static string? GetAspireApiReference(IConfiguration config, string scheme) =>
        config[$"services:explore-api:{scheme}:0"]
        ?? config[$"services__explore-api__{scheme}__0"];
}
