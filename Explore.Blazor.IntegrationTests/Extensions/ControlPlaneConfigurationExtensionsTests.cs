// ABOUTME: Regression tests for control-plane Blazor bootstrap configuration mapping.
// ABOUTME: Protects Aspire service discovery from generic Infisical API endpoint overrides.

extern alias ControlPlaneBlazor;

using ControlPlaneBlazor::Event.ControlPlane.Blazor.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Explore.Blazor.IntegrationTests.Extensions;

public sealed class ControlPlaneConfigurationExtensionsTests
{
    [Test]
    public void AddInfisicalControlPlaneCompatibility_WhenAspireApiReferenceExists_DoesNotMapGenericApiEndpoint()
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

        configurationBuilder.AddInfisicalControlPlaneCompatibility();

        var configuration = configurationBuilder.Build();
        configuration["ExploreApi:BaseUrl"].Should().BeNull();
        configuration["services:explore-api:https:0"].Should().Be("https://localhost:7211");
    }
}
