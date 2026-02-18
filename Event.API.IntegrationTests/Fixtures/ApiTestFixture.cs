using TUnit.Core;
using TUnit.Core.Interfaces;

namespace Event.Api.IntegrationTests.Fixtures;

public class ApiTestFixture : IAsyncInitializer, IAsyncDisposable
{
    public CustomWebApplicationFactory Factory { get; private set; }
    public HttpClient Client { get; private set; }

    public async Task InitializeAsync()
    {
        // Set environment variable for connection string as a fallback for main Program.cs
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Host=localhost;Database=explore_db_test;Username=postgres;Password=postgres");

        // Mock Keycloak Environment Variables (Critical for Program.cs startup before WAF configuration overrides)
        Environment.SetEnvironmentVariable("Keycloak__Authority", "https://auth.example.com");
        Environment.SetEnvironmentVariable("Keycloak__Realm", "explore");
        Environment.SetEnvironmentVariable("Keycloak__Audience", "explore-api");
        Environment.SetEnvironmentVariable("Keycloak__RequireHttpsMetadata", "false");
        Environment.SetEnvironmentVariable("Keycloak__MetadataAddress", "https://auth.example.com/.well-known/openid-configuration");

        Factory = new CustomWebApplicationFactory();
        Client = Factory.CreateClient();
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        Client?.Dispose();

        if (Factory is null)
        {
            return;
        }

        try
        {
            await Factory.DisposeAsync();
        }
        catch (NullReferenceException ex)
        {
            // Workaround for intermittent WebApplicationFactory teardown race in test host.
            Console.WriteLine($"Ignoring WebApplicationFactory teardown NullReferenceException: {ex.Message}");
        }
    }
}
