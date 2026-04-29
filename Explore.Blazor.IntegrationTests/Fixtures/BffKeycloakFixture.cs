// ABOUTME: Keycloak container fixture for BFF security integration tests.
// ABOUTME: Starts a containerized Keycloak with the ISLAMU test realm for OIDC challenge tests.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using TUnit.Core.Interfaces;

namespace Explore.Blazor.IntegrationTests.Fixtures;

/// <summary>
/// Keycloak container fixture for Blazor BFF security tests.
/// Provides the OIDC authority URL for the BFF's DynamicAuthSchemeManager.
/// </summary>
public sealed class BffKeycloakFixture : IAsyncInitializer, IAsyncDisposable
{
    private const string KeycloakImage = "quay.io/keycloak/keycloak:26.1.2";
    public const string RealmName = "ISLAMU";
    public const string TestClientId = "islamu-event-blazor";
    public const string TestClientSecret = "test-blazor-secret";

    private IContainer _container = null!;

    public string Authority { get; private set; } = string.Empty;
    public string MetadataAddress { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var testRealmPath = Path.Combine(
            AppContext.BaseDirectory, "TestAssets", "ISLAMU-realm.test.json");

        if (!File.Exists(testRealmPath))
        {
            throw new FileNotFoundException(
                $"Test realm export not found at '{testRealmPath}'. " +
                "Ensure ISLAMU-realm.test.json is included as Content.");
        }

        _container = new ContainerBuilder()
            .WithImage(KeycloakImage)
            .WithPortBinding(8080, true)
            .WithResourceMapping(testRealmPath, "/opt/keycloak/data/import/ISLAMU-realm.test.json")
            .WithCommand("start-dev", "--import-realm", "--http-port=8080")
            .WithEnvironment("KC_HEALTH_ENABLED", "true")
            .WithEnvironment("KC_HTTP_ENABLED", "true")
            .WithEnvironment("KEYCLOAK_ADMIN", "admin")
            .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", "admin")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request =>
                    request
                        .ForPath($"/realms/{RealmName}/.well-known/openid-configuration")
                        .ForPort(8080)
                        .ForStatusCode(System.Net.HttpStatusCode.OK)))
            .Build();

        await _container.StartAsync();

        await Task.Delay(TimeSpan.FromSeconds(3));

        var host = _container.Hostname;
        var port = _container.GetMappedPublicPort(8080);

        Authority = $"http://{host}:{port}/realms/{RealmName}";
        MetadataAddress = $"{Authority}/.well-known/openid-configuration";
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

/// <summary>
/// Test category constants for BFF security tests.
/// </summary>
public static class BffTestCategories
{
    public const string Security = "Security";
}
