using TUnit.Core;
using TUnit.Core.Interfaces;

namespace Event.Api.IntegrationTests.Fixtures;

public class ApiTestFixture : IAsyncInitializer, IAsyncDisposable
{
    public CustomWebApplicationFactory Factory { get; private set; }
    public HttpClient Client { get; private set; }

    public async Task InitializeAsync()
    {
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
