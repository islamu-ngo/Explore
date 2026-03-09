// ABOUTME: TUnit fixture for authenticated API integration tests that require single-tenant resolution.
// ABOUTME: Reuses the test-auth header flow while forcing the API host into single-tenant mode.

using Explore.Application.Contracts.Infrastructure;
using TUnit.Core.Interfaces;

namespace Event.Api.IntegrationTests.Fixtures;

public class SingleTenantAuthenticatedApiTestFixture : IAsyncInitializer, IAsyncDisposable
{
    public SingleTenantAuthenticatedWebApplicationFactory Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public IAuthorizationProvider? AuthorizationProvider { get; set; }

    public async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Host=localhost;Database=explore_db_test;Username=postgres;Password=postgres");
        Environment.SetEnvironmentVariable("Keycloak__Authority", "https://auth.example.com");
        Environment.SetEnvironmentVariable("Keycloak__Realm", "explore");
        Environment.SetEnvironmentVariable("Keycloak__Audience", "explore-api");
        Environment.SetEnvironmentVariable("Keycloak__RequireHttpsMetadata", "false");
        Environment.SetEnvironmentVariable("Keycloak__MetadataAddress", "https://auth.example.com/.well-known/openid-configuration");

        Factory = new SingleTenantAuthenticatedWebApplicationFactory
        {
            AuthorizationProviderOverride = AuthorizationProvider
        };
        Client = Factory.CreateClient();

        await Task.CompletedTask;
    }

    public HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url, Guid userId, string name = "Test User")
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add(TestAuthHandler.AuthHeaderName, TestAuthHandler.CreateAuthHeaderValue(userId, name));
        return request;
    }

    public async ValueTask DisposeAsync()
    {
        Client?.Dispose();
        await Factory.DisposeAsync();
    }
}
