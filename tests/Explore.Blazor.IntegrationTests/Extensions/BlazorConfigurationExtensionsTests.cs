// ABOUTME: Regression tests for Blazor bootstrap configuration mapping.
// ABOUTME: Protects Aspire service discovery from being overridden by Infisical compatibility keys.

using Explore.Blazor.Extensions;
using Microsoft.Extensions.Configuration;

namespace Explore.Blazor.IntegrationTests.Extensions;

public sealed class BlazorConfigurationExtensionsTests
{
    [Test]
    public async Task AddInfisicalBlazorCompatibility_WhenAspireApiReferenceExists_DoesNotMapInfisicalApiEndpoint()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["API_ENDPOINT"] = "https://localhost:7039",
                ["Infisical:ProjectId"] = "",
                ["Infisical:ClientId"] = "",
                ["Infisical:ClientSecret"] = "",
                ["services:explore-api:https:0"] = "https://localhost:7211"
            });

        configurationBuilder.AddInfisicalBlazorCompatibility();

        var configuration = configurationBuilder.Build();
        await Assert.That(configuration["ExploreApi:BaseUrl"]).IsNull();
        await Assert.That(configuration["services:explore-api:https:0"]).IsEqualTo("https://localhost:7211");
    }

    [Test]
    public async Task AddInfisicalBlazorCompatibility_WhenAspireApiReferenceIsMissing_MapsInfisicalApiEndpoint()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["API_ENDPOINT"] = "https://localhost:7039",
                ["Infisical:ProjectId"] = "",
                ["Infisical:ClientId"] = "",
                ["Infisical:ClientSecret"] = ""
            });

        configurationBuilder.AddInfisicalBlazorCompatibility();

        var configuration = configurationBuilder.Build();
        await Assert.That(configuration["ExploreApi:BaseUrl"]).IsEqualTo("https://localhost:7039");
    }
}
