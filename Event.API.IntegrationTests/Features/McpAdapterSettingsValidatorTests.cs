// ABOUTME: Unit-style tests for validating optional MCP adapter static settings.
// ABOUTME: Locks the initial stateless Streamable HTTP posture and legacy-SSE disable path.

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
        var result = _validator.Validate(null, new McpAdapterSettings());

        result.Succeeded.Should().BeTrue();
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
    public void Validate_WhenLegacySseIsRequested_Fails()
    {
        var result = _validator.Validate(null, new McpAdapterSettings { EnableLegacySse = true });

        result.Failed.Should().BeTrue();
    }
}
