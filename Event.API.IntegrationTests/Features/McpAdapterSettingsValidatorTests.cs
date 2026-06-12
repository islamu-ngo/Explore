// ABOUTME: Unit-style tests for validating optional MCP adapter startup settings.
// ABOUTME: Locks endpoint/stateless constraints while allowing legacy SSE only as a startup ceiling.

using Explore.API.Configuration;
using FluentAssertions;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

public sealed class McpAdapterSettingsValidatorTests
{
    private readonly McpAdapterSettingsValidator _validator = new();

    [Test]
    public void Validate_WhenDefaultsAreUsed_Succeeds()
    {
        var settings = new McpAdapterSettings();

        var result = _validator.Validate(null, settings);

        result.Succeeded.Should().BeTrue();
        settings.Enabled.Should().BeTrue();
        settings.EndpointPath.Should().Be("/mcp");
        settings.Stateless.Should().BeTrue();
        settings.EnableLegacySse.Should().BeTrue();
    }

    [Test]
    public void Validate_WhenEndpointPathIsRelative_Fails()
    {
        var result = _validator.Validate(null, new McpAdapterSettings { EndpointPath = "mcp" });

        result.Failed.Should().BeTrue();
    }

    [Test]
    public void Validate_WhenStatefulTransportIsRequested_Fails()
    {
        var result = _validator.Validate(null, new McpAdapterSettings { Stateless = false });

        result.Failed.Should().BeTrue();
    }

    [Test]
    public void Validate_WhenLegacySseCeilingIsRequested_Succeeds()
    {
        var result = _validator.Validate(null, new McpAdapterSettings { EnableLegacySse = true });

        result.Succeeded.Should().BeTrue();
    }
}
