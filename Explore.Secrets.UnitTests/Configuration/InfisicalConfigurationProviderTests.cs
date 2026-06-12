// ABOUTME: Unit tests for Infisical configuration key conversion rules.
// ABOUTME: Verifies AI provider secrets bind to the configuration section used by bootstrap workers.

using System.Reflection;
using Explore.Secrets.Configuration;
using FluentAssertions;
using TUnit.Core;

namespace Explore.Secrets.UnitTests.Configuration;

public sealed class InfisicalConfigurationProviderTests
{
    [Test]
    public void ConvertToConfigurationKey_WhenAiToolProposalsSecretProvided_MapsToAiProviderSetting()
    {
        var key = ConvertToConfigurationKey("AI_TOOL_PROPOSALS_ENABLED", "/");

        key.Should().Be("AiProvider:ToolProposalsEnabled");
    }

    private static string ConvertToConfigurationKey(string secretKey, string path)
    {
        var method = typeof(InfisicalConfigurationProvider).GetMethod(
            "ConvertToConfigurationKey",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        return ((string?)method!.Invoke(null, [secretKey, path]))!;
    }
}
