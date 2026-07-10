// ABOUTME: Source guard for WASM generated API client antiforgery handler registration.
// ABOUTME: Keeps WebPush mutations protected when browser code uses the NSwag IEventApiClient.

namespace Explore.Blazor.Client.Tests;

public sealed class ProgramApiClientAntiforgeryRegistrationTests
{
    [Test]
    public async Task Program_RegistersGeneratedEventApiClientWithAntiforgeryHandler()
    {
        var source = await File.ReadAllTextAsync(FindClientProgramPath());
        var eventApiClientRegistration = source[
            source.IndexOf("AddHttpClient<IEventApiClient, EventApiClient>", StringComparison.Ordinal)..];
        eventApiClientRegistration = eventApiClientRegistration[..eventApiClientRegistration.IndexOf("// Register TenantConfiguration", StringComparison.Ordinal)];

        await Assert.That(eventApiClientRegistration).Contains("AddHttpMessageHandler<BffAntiforgeryMessageHandler>()");
    }

    private static string FindClientProgramPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Explore.Blazor.Client", "Program.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate Explore.Blazor.Client/Program.cs");
    }
}
