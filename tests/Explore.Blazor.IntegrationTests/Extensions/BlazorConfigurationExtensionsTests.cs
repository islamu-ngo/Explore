// ABOUTME: Regression tests for Blazor bootstrap configuration mapping.
// ABOUTME: Protects Aspire service discovery from being overridden by Infisical compatibility keys.

using Explore.Blazor.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Explore.Blazor.IntegrationTests.Extensions;

public sealed class BlazorConfigurationExtensionsTests
{
    [Test]
    public void AddInfisicalBlazorCompatibility_WhenAspireApiReferenceExists_DoesNotMapInfisicalApiEndpoint()
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
        configuration["ExploreApi:BaseUrl"].Should().BeNull();
        configuration["services:explore-api:https:0"].Should().Be("https://localhost:7211");
    }

    [Test]
    public void AddInfisicalBlazorCompatibility_WhenAspireApiReferenceIsMissing_MapsInfisicalApiEndpoint()
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
        configuration["ExploreApi:BaseUrl"].Should().Be("https://localhost:7039");
    }
}
