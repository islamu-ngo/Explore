// ABOUTME: Regression tests for API startup configuration compatibility mapping.
// ABOUTME: Ensures Infisical deployment keys bind to canonical .NET configuration sections.

using Explore.API.Extensions;
using Explore.Infrastructure.Services;
using Explore.Secrets.Database;
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
    public async Task AddInfisicalCompatibility_DoesNotMapLegacyPostgresPublicUrlToDefaultConnection()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["POSTGRESQL_PUBLIC_URL"] = "postgres://user:secret@db:5432/event"
        });

        await Assert.That(configuration["ConnectionStrings:DefaultConnection"]).IsNull();
    }

    [Test]
    public async Task AddInfisicalCompatibility_ProjectsDiscretePostgresIntoRuntimeDatabaseContract()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["POSTGRESQL_HOST"] = "pg.example.test",
            ["POSTGRESQL_PORT"] = "6543",
            ["POSTGRESQL_DATABASE"] = "event_db",
            ["POSTGRESQL_USERNAME"] = "app_user",
            ["POSTGRESQL_PASSWORD"] = "app-secret",
        });

        var options = PrimaryDatabaseConfiguration.BindRuntime(configuration);

        await Assert.That(options.Provider).IsEqualTo(PrimaryDatabaseProvider.PostgreSql);
        await Assert.That(options.Host).IsEqualTo("pg.example.test");
        await Assert.That(options.Port).IsEqualTo(6543);
        await Assert.That(options.Database).IsEqualTo("event_db");
        await Assert.That(options.Username).IsEqualTo("app_user");
        await Assert.That(options.Password).IsEqualTo("app-secret");
    }

    [Test]
    public async Task AddInfisicalCompatibility_DoesNotOverrideExplicitStructuredDatabaseContract()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["POSTGRESQL_HOST"] = "projected-host",
            ["POSTGRESQL_DATABASE"] = "projected_db",
            ["POSTGRESQL_USERNAME"] = "projected_user",
            ["POSTGRESQL_PASSWORD"] = "projected-secret",
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "explicit-host",
            ["Database:Database"] = "explicit_db",
            ["Database:Runtime:Username"] = "explicit_user",
            ["Database:Runtime:Password"] = "explicit-secret",
            ["Database:Runtime:TlsMode"] = "Required",
        });

        var options = PrimaryDatabaseConfiguration.BindRuntime(configuration);

        await Assert.That(options.Host).IsEqualTo("explicit-host");
        await Assert.That(options.Database).IsEqualTo("explicit_db");
        await Assert.That(options.Username).IsEqualTo("explicit_user");
        await Assert.That(options.Password).IsEqualTo("explicit-secret");
        await Assert.That(options.TlsMode).IsEqualTo(PrimaryDatabaseTlsMode.Required);
    }

    [Test]
    public async Task AddInfisicalCompatibility_MapsPrivacyErasureAuthorityKeys()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["PRIVACY_ERASURE_AUTHORITY_TOPOLOGY"] = "ExternalDatabase",
            ["PRIVACY_ERASURE_AUTHORITY_HOST"] = "authority",
            ["PRIVACY_ERASURE_AUTHORITY_PORT"] = "6543",
            ["PRIVACY_ERASURE_AUTHORITY_DATABASE"] = "privacy",
            ["PRIVACY_ERASURE_AUTHORITY_RUNTIME_USERNAME"] = "runtime",
            ["PRIVACY_ERASURE_AUTHORITY_RUNTIME_PASSWORD"] = "runtime-secret",
            ["PRIVACY_ERASURE_AUTHORITY_MIGRATOR_USERNAME"] = "migrator",
            ["PRIVACY_ERASURE_AUTHORITY_MIGRATOR_PASSWORD"] = "migrator-secret",
            ["PRIVACY_ERASURE_AUTHORITY_TLS_MODE"] = "Required",
            ["PRIVACY_ERASURE_AUTHORITY_TRUST_SERVER_CERTIFICATE"] = "false"
        });

        await Assert.That(configuration["PrivacyErasure:Authority:Topology"])
            .IsEqualTo("ExternalDatabase");
        await Assert.That(configuration["PrivacyErasureAuthorityDatabase:Provider"])
            .IsEqualTo("PostgreSql");
        await Assert.That(configuration["PrivacyErasureAuthorityDatabase:Host"])
            .IsEqualTo("authority");
        await Assert.That(configuration["PrivacyErasureAuthorityDatabase:Runtime:Username"])
            .IsEqualTo("runtime");
        await Assert.That(configuration["PrivacyErasureAuthorityDatabase:Migrator:Username"])
            .IsEqualTo("migrator");
    }

    [Test]
    public async Task AddInfisicalCompatibility_DoesNotOverrideCanonicalPrivacyErasureAuthorityKeys()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["PRIVACY_ERASURE_AUTHORITY_TOPOLOGY"] = "ExternalDatabase",
            ["PRIVACY_ERASURE_AUTHORITY_HOST"] = "mapped-host",
            ["PRIVACY_ERASURE_AUTHORITY_RUNTIME_USERNAME"] = "mapped-user",
            ["PrivacyErasure:Authority:Topology"] = "EmbeddedSqlite",
            ["PrivacyErasureAuthorityDatabase:Host"] = "explicit-host",
            ["PrivacyErasureAuthorityDatabase:Runtime:Username"] = "explicit-user"
        });

        await Assert.That(configuration["PrivacyErasure:Authority:Topology"])
            .IsEqualTo("EmbeddedSqlite");
        await Assert.That(configuration["PrivacyErasureAuthorityDatabase:Host"])
            .IsEqualTo("explicit-host");
        await Assert.That(configuration["PrivacyErasureAuthorityDatabase:Runtime:Username"])
            .IsEqualTo("explicit-user");
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
