// ABOUTME: Regression tests for API startup configuration compatibility mapping.
// ABOUTME: Ensures Infisical deployment keys bind to canonical .NET configuration sections.

using Explore.API.Extensions;
using Explore.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace Event.Api.IntegrationTests.Features;

public sealed class ConfigurationExtensionsTests
{
    [Test]
    public async Task AddInfisicalCompatibility_MapsCerbosUsePolicyScopeFromInfisicalKey()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["CERBOS_USE_POLICY_SCOPE"] = "false"
        });

        await Assert.That(configuration["Cerbos:UsePolicyScope"]).IsEqualTo("false");
    }

    [Test]
    public async Task AddInfisicalCompatibility_MapsKeycloakClientIdsForApiProviderManagement()
    {
        var canonical = BuildConfiguration(new Dictionary<string, string?>
        {
            ["KEYCLOAK_CLIENT_ID"] = "canonical-blazor"
        });
        var composeAlias = BuildConfiguration(new Dictionary<string, string?>
        {
            ["KEYCLOAK_BLAZOR_CLIENT_ID"] = "event-blazor",
            ["KEYCLOAK_BLAZOR_CLIENT_SECRET"] = "server-only-secret"
        });

        await Assert.That(canonical["Keycloak:ClientId"]).IsEqualTo("canonical-blazor");
        await Assert.That(composeAlias["Keycloak:ClientId"]).IsEqualTo("event-blazor");
        await Assert.That(composeAlias["Keycloak:ClientSecret"]).IsEqualTo("server-only-secret");
    }

    [Test]
    public async Task AddInfisicalCompatibility_DoesNotOverrideCanonicalCerbosUsePolicyScope()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["CERBOS_USE_POLICY_SCOPE"] = "false",
            ["Cerbos:UsePolicyScope"] = "true"
        });

        await Assert.That(configuration["Cerbos:UsePolicyScope"]).IsEqualTo("true");
    }

    [Test]
    public async Task AddInfisicalCompatibility_MapsCerbosHttpEndpointToAdminApiEndpoint()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["CERBOS_HTTP_ENDPOINT"] = "cerbosapi.openislamu.org:443"
        });

        await Assert.That(configuration["Cerbos:HttpEndpoint"]).IsEqualTo("cerbosapi.openislamu.org:443");
        await Assert.That(configuration["Cerbos:AdminApi:Endpoints:0"]).IsEqualTo("cerbosapi.openislamu.org:443");
    }

    [Test]
    public async Task AddInfisicalCompatibility_MapsCerbosTlsAndAdminApiCredentials()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["CERBOS_USE_TLS"] = "true",
            ["CERBOS_PLAINTEXT_MODE"] = "false",
            ["CERBOS_ADMIN_USERNAME"] = "cerbos-admin",
            ["CERBOS_ADMIN_PASSWORD"] = "server-side-password"
        });

        await Assert.That(configuration["Cerbos:UseTls"]).IsEqualTo("true");
        await Assert.That(configuration["Cerbos:PlaintextMode"]).IsEqualTo("false");
        await Assert.That(configuration["Cerbos:AdminApi:AdminUsername"]).IsEqualTo("cerbos-admin");
        await Assert.That(configuration["Cerbos:AdminApi:AdminPassword"]).IsEqualTo("server-side-password");
    }

    [Test]
    public async Task AddInfisicalCompatibility_MapsAuthorizationProviderIntentWithoutOverridingCanonicalValue()
    {
        var mapped = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AUTHORIZATION_PROVIDER"] = "cerbos"
        });
        var canonical = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AUTHORIZATION_PROVIDER"] = "cerbos",
            ["Authorization:Provider"] = "local"
        });

        await Assert.That(mapped["Authorization:Provider"]).IsEqualTo("cerbos");
        await Assert.That(canonical["Authorization:Provider"]).IsEqualTo("local");
    }

    [Test]
    [Arguments(null, true)]
    [Arguments("", true)]
    [Arguments("local", true)]
    [Arguments("CERBOS", true)]
    [Arguments("fallback", false)]
    public async Task AuthorizationProviderDeploymentOptions_ValidatesOnlySupportedIntent(
        string? provider,
        bool expected)
    {
        var result = AuthorizationProviderDeploymentOptions.IsValid(new()
        {
            Provider = provider
        });

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task AddInfisicalCompatibility_MapsMailSmtpKeys()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["MAIL_SMTP_HOST"] = "mailpit",
            ["MAIL_SMTP_PORT"] = "1025",
            ["MAIL_SMTP_USERNAME"] = "smtp-user",
            ["MAIL_SMTP_PASSWORD"] = "smtp-secret",
            ["MAIL_SMTP_ENCRYPTION"] = "None",
            ["MAIL_SMTP_FROM_ADDRESS"] = "noreply@localhost",
            ["MAIL_SMTP_FROM_NAME"] = "ISLAMU Event Dev"
        });

        await Assert.That(configuration["Smtp:Host"]).IsEqualTo("mailpit");
        await Assert.That(configuration["Smtp:Port"]).IsEqualTo("1025");
        await Assert.That(configuration["Smtp:Username"]).IsEqualTo("smtp-user");
        await Assert.That(configuration["Smtp:Password"]).IsEqualTo("smtp-secret");
        await Assert.That(configuration["Smtp:Encryption"]).IsEqualTo("None");
        await Assert.That(configuration["Smtp:FromAddress"]).IsEqualTo("noreply@localhost");
        await Assert.That(configuration["Smtp:FromName"]).IsEqualTo("ISLAMU Event Dev");
    }

    [Test]
    public async Task AddInfisicalCompatibility_MapsApiFolderVapidKeys()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["VAPID_PUBLIC_KEY"] = "public-key",
            ["VAPID_PRIVATE_KEY"] = "private-key",
            ["VAPID_SUBJECT"] = "mailto:admin@example.com"
        });

        await Assert.That(configuration["WebPush:Enabled"]).IsEqualTo("true");
        await Assert.That(configuration["WebPush:VapidPublicKey"]).IsEqualTo("public-key");
        await Assert.That(configuration["WebPush:VapidPrivateKey"]).IsEqualTo("private-key");
        await Assert.That(configuration["WebPush:VapidSubject"]).IsEqualTo("mailto:admin@example.com");
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(values);

        builder.AddInfisicalCompatibility();
        return builder.Build();
    }
}
