// ABOUTME: Unit tests for Infisical configuration key conversion rules.
// ABOUTME: Verifies AI provider secrets bind to the configuration section used by bootstrap workers.

using System.Reflection;
using Explore.Secrets.Configuration;
using Explore.Secrets.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TUnit.Core;

namespace Explore.Secrets.UnitTests.Configuration;

public sealed class InfisicalConfigurationProviderTests
{
    [Test]
    public void AddInfisical_WhenCredentialsAreConfigured_AddsConfiguredSource()
    {
        var bootstrapConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Infisical:Url"] = "https://secrets.example.test",
                ["Infisical:ProjectId"] = "project-id",
                ["Infisical:ClientId"] = "client-id",
                ["Infisical:ClientSecret"] = "client-secret",
                ["Infisical:Environment"] = "staging",
                ["Infisical:Paths:0"] = "/api",
                ["Infisical:Paths:1"] = "/keycloak",
            })
            .Build();
        var builder = new ConfigurationBuilder();

        builder.AddInfisical(bootstrapConfiguration);

        var source = builder.Sources.Should().ContainSingle()
            .Which.Should().BeOfType<InfisicalConfigurationSource>().Subject;
        source.Url.Should().Be("https://secrets.example.test");
        source.ProjectId.Should().Be("project-id");
        source.ClientId.Should().Be("client-id");
        source.ClientSecret.Should().Be("client-secret");
        source.Environment.Should().Be("staging");
        source.Paths.Should().Equal("/api", "/keycloak");
    }

    [Test]
    public void AddInfisical_WhenCredentialsAreIncomplete_DoesNotAddSource()
    {
        var bootstrapConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Infisical:ProjectId"] = "project-id",
            })
            .Build();
        var builder = new ConfigurationBuilder();

        builder.AddInfisical(bootstrapConfiguration);

        builder.Sources.Should().BeEmpty();
    }

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
