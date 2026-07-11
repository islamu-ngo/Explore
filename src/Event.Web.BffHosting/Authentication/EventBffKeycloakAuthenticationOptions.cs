// ABOUTME: Binds Keycloak OIDC and cookie settings for reusable browser-BFF host profiles.
// ABOUTME: Requires private host identity values explicitly while keeping all secrets server-side.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Event.Web.BffHosting.Authentication;

public sealed class EventBffKeycloakAuthenticationOptions
{
    public const string SectionName = "Bff:Authentication";

    public string? Authority { get; init; }

    public string? MetadataAddress { get; init; }

    public string ClientId { get; init; } = string.Empty;

    public string? ClientSecret { get; init; }

    public bool? RequireHttpsMetadata { get; init; }

    public string CallbackPath { get; init; } = "/signin-oidc";

    public string SignedOutCallbackPath { get; init; } = "/signout-callback-oidc";

    public string LoginPath { get; init; } = "/login";

    public string AccessDeniedPath { get; init; } = "/forbidden";

    public string CookieName { get; init; } = string.Empty;

    public TimeSpan CookieLifetime { get; init; } = TimeSpan.FromHours(12);

    public bool RequireInstanceAdminClaim { get; init; } = true;

    public string InstanceAdminClaimType { get; init; } = "explore:admin:instance";

    public string InstanceAdminClaimValue { get; init; } = "true";

    public static EventBffKeycloakAuthenticationOptions FromConfiguration(
        IConfiguration configuration,
        EventBffHostProfile profile,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var isPrivateHost = profile == EventBffHostProfile.ControlPlane;
        var authority = isPrivateHost
            ? First(configuration, $"{SectionName}:Authority")
            : First(configuration, $"{SectionName}:Authority", "Keycloak:Authority");
        var metadataAddress = isPrivateHost
            ? First(configuration, $"{SectionName}:MetadataAddress")
            : First(configuration, $"{SectionName}:MetadataAddress", "Keycloak:MetadataAddress");
        var clientId = (isPrivateHost
                ? First(configuration, $"{SectionName}:ClientId")
                : First(configuration, $"{SectionName}:ClientId", "Keycloak:ClientId"))
            ?? DefaultClientId(profile);

        var cookieName = First(configuration,
            $"{SectionName}:CookieName",
            "Bff:Cookie:Name")
            ?? DefaultCookieName(profile, environment);

        return new EventBffKeycloakAuthenticationOptions
        {
            Authority = authority,
            MetadataAddress = metadataAddress,
            ClientId = clientId,
            ClientSecret = isPrivateHost
                ? First(configuration, $"{SectionName}:ClientSecret")
                : First(configuration, $"{SectionName}:ClientSecret", "Keycloak:ClientSecret"),
            RequireHttpsMetadata = isPrivateHost
                ? Bool(configuration, $"{SectionName}:RequireHttpsMetadata")
                : Bool(configuration, $"{SectionName}:RequireHttpsMetadata", "Keycloak:RequireHttpsMetadata"),
            CallbackPath = First(configuration, $"{SectionName}:CallbackPath")
                ?? "/signin-oidc",
            SignedOutCallbackPath = First(configuration, $"{SectionName}:SignedOutCallbackPath")
                ?? "/signout-callback-oidc",
            LoginPath = First(configuration,
                $"{SectionName}:LoginPath",
                "Bff:Cookie:LoginPath")
                ?? "/auth/login",
            AccessDeniedPath = First(configuration,
                $"{SectionName}:AccessDeniedPath",
                "Bff:Cookie:AccessDeniedPath")
                ?? "/forbidden",
            CookieName = cookieName,
            CookieLifetime = Minutes(configuration,
                $"{SectionName}:CookieLifetimeMinutes",
                "Bff:Cookie:LifetimeMinutes")
                ?? TimeSpan.FromHours(profile == EventBffHostProfile.ControlPlane ? 8 : 12),
            RequireInstanceAdminClaim = Bool(configuration,
                $"{SectionName}:RequireInstanceAdminClaim",
                "Bff:Security:RequireInstanceAdminClaim")
                ?? profile == EventBffHostProfile.ControlPlane,
            InstanceAdminClaimType = First(configuration,
                $"{SectionName}:InstanceAdminClaimType",
                "Bff:Security:InstanceAdminClaimType")
                ?? "explore:admin:instance",
            InstanceAdminClaimValue = First(configuration,
                $"{SectionName}:InstanceAdminClaimValue",
                "Bff:Security:InstanceAdminClaimValue")
                ?? "true"
        };
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Authority) && string.IsNullOrWhiteSpace(MetadataAddress))
        {
            throw new InvalidOperationException(
                "Keycloak OIDC configuration is missing. Configure Bff:Authentication:Authority or Bff:Authentication:MetadataAddress.");
        }

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            throw new InvalidOperationException(
                "Keycloak OIDC configuration is missing Bff:Authentication:ClientId.");
        }

        if (string.IsNullOrWhiteSpace(ClientSecret))
        {
            throw new InvalidOperationException(
                "Keycloak OIDC configuration is missing Bff:Authentication:ClientSecret for the confidential BFF client.");
        }
    }

    private static string DefaultClientId(EventBffHostProfile profile) =>
        profile == EventBffHostProfile.ControlPlane
            ? string.Empty
            : "islamu-event-blazor";

    private static string DefaultCookieName(EventBffHostProfile profile, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            return profile == EventBffHostProfile.ControlPlane
                ? "event-control-plane-bff-dev"
                : "islamu-event-public-dev";
        }

        return profile == EventBffHostProfile.ControlPlane
            ? "__Host-event-control-plane-bff"
            : "__Host-islamu-event-web";
    }

    private static string? First(IConfiguration configuration, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static bool? Bool(IConfiguration configuration, params string[] keys)
    {
        var value = First(configuration, keys);
        return bool.TryParse(value, out var parsed) ? parsed : null;
    }

    private static TimeSpan? Minutes(IConfiguration configuration, params string[] keys)
    {
        var value = First(configuration, keys);
        return int.TryParse(value, out var parsed) && parsed > 0
            ? TimeSpan.FromMinutes(parsed)
            : null;
    }
}
