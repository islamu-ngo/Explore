// ABOUTME: Composite fixture combining Keycloak and Cerbos containers for security integration tests.
// ABOUTME: Implements IAsyncLifetime to start both containers once per test assembly and tear down after.

using TUnit.Core.Interfaces;

namespace Event.Api.IntegrationTests.Fixtures;

/// <summary>
/// Composite fixture orchestrating Keycloak + Cerbos container lifecycle.
/// Start both containers in parallel, expose their endpoints for downstream
/// <see cref="SecurityWebApplicationFactory"/> and <see cref="CerbosPolicyContractTests"/>.
/// </summary>
public sealed class SecurityInfrastructureFixture : IAsyncInitializer, IAsyncDisposable
{
    public KeycloakContainerFixture Keycloak { get; } = new();
    public CerbosContainerFixture Cerbos { get; } = new();

    /// <summary>
    /// Token client delegating to the Keycloak container.
    /// </summary>
    public KeycloakTokenClient TokenClient => Keycloak.TokenClient;

    /// <summary>
    /// The OIDC authority URL from the Keycloak container.
    /// </summary>
    public string KeycloakAuthority => Keycloak.Authority;

    public string KeycloakBaseUrl => Keycloak.BaseUrl;

    /// <summary>
    /// The OIDC metadata address from the Keycloak container.
    /// </summary>
    public string KeycloakMetadataAddress => Keycloak.MetadataAddress;

    public KeycloakTokenClient CreateTokenClient(string clientSecret)
        => new(KeycloakBaseUrl, KeycloakContainerFixture.RealmName, KeycloakContainerFixture.TestClientId, clientSecret);

    /// <summary>
    /// The Cerbos gRPC endpoint for SDK clients.
    /// </summary>
    public string CerbosGrpcEndpoint => Cerbos.GrpcEndpoint;

    /// <summary>
    /// The Cerbos HTTP endpoint for REST API and health checks.
    /// </summary>
    public string CerbosHttpEndpoint => Cerbos.HttpEndpoint;

    public async Task InitializeAsync()
    {
        // Start both containers in parallel for faster fixture setup.
        var keycloakTask = Keycloak.InitializeAsync();
        var cerbosTask = Cerbos.InitializeAsync();

        await Task.WhenAll(keycloakTask, cerbosTask);
    }

    public async ValueTask DisposeAsync()
    {
        await Keycloak.DisposeAsync();
        await Cerbos.DisposeAsync();
    }
}
