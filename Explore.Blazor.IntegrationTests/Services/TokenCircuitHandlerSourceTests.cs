// ABOUTME: Source-level regression tests for first-run setup cookie capture in Blazor circuits.
// ABOUTME: Ensures unauthenticated setup circuits can still forward BFF setup cookies.

namespace Explore.Blazor.IntegrationTests.Services;

public sealed class TokenCircuitHandlerSourceTests
{
    [Test]
    public async Task TokenCircuitHandler_ShouldCaptureCookies_BeforeUnauthenticatedCircuitExit()
    {
        var source = await ReadTokenCircuitHandlerSourceAsync();

        var captureIndex = source.IndexOf("CaptureCookieHeader(httpContext, circuit.Id);", StringComparison.Ordinal);
        var unauthenticatedExitIndex = source.IndexOf("httpContext?.User?.Identity?.IsAuthenticated != true", StringComparison.Ordinal);

        await Assert.That(captureIndex).IsGreaterThanOrEqualTo(0);
        await Assert.That(unauthenticatedExitIndex).IsGreaterThanOrEqualTo(0);
        await Assert.That(captureIndex).IsLessThan(unauthenticatedExitIndex);
        await Assert.That(source).Contains("First-run setup is unauthenticated");
    }

    private static async Task<string> ReadTokenCircuitHandlerSourceAsync()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "Explore.Blazor",
                "Services",
                "TokenCircuitHandler.cs");

            if (File.Exists(candidate))
            {
                return await File.ReadAllTextAsync(candidate);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("TokenCircuitHandler.cs was not found.");
    }
}
