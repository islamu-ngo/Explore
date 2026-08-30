// ABOUTME: Unit tests for Infisical configuration key conversion rules.
// ABOUTME: Verifies AI provider secrets bind to the configuration section used by bootstrap workers.

using System.Reflection;
using Explore.Secrets.Configuration;
using Explore.Secrets.Extensions;
using Microsoft.Extensions.Configuration;
using TUnit.Core;

namespace Explore.Secrets.UnitTests.Configuration;

public sealed class InfisicalConfigurationProviderTests
{
    [Test]
    public async Task AddInfisical_WhenCredentialsAreConfigured_AddsConfiguredSource()
    {
        string clientSecret = SecretsTestValues.CreateSecret();
        var bootstrapConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SecretProvider:Infisical:Url"] = "https://secrets.example.test",
                ["SecretProvider:Infisical:ProjectId"] = "project-id",
                ["SecretProvider:Infisical:ClientId"] = "client-id",
                ["SecretProvider:Infisical:ClientSecret"] = clientSecret,
                ["SecretProvider:Infisical:Environment"] = "staging",
                ["SecretProvider:Infisical:Paths:0"] = "/api",
                ["SecretProvider:Infisical:Paths:1"] = "/keycloak",
            })
            .Build();
        var builder = new ConfigurationBuilder();

        builder.AddInfisical(bootstrapConfiguration);

        await Assert.That(builder.Sources).Count().IsEqualTo(1);
        var source = builder.Sources.Single();
        await Assert.That(source).IsTypeOf<InfisicalConfigurationSource>();
        var infisicalSource = (InfisicalConfigurationSource)source;
        await Assert.That(infisicalSource.Url).IsEqualTo("https://secrets.example.test");
        await Assert.That(infisicalSource.ProjectId).IsEqualTo("project-id");
        await Assert.That(infisicalSource.ClientId).IsEqualTo("client-id");
        await Assert.That(infisicalSource.ClientSecret).IsEqualTo(clientSecret);
        await Assert.That(infisicalSource.Environment).IsEqualTo("staging");
        await Assert.That(infisicalSource.Paths.SequenceEqual(["/api", "/keycloak"])).IsTrue();
    }

    [Test]
    public async Task AddInfisical_WhenCredentialsAreIncomplete_DoesNotAddSource()
    {
        var bootstrapConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SecretProvider:Infisical:ProjectId"] = "project-id",
            })
            .Build();
        var builder = new ConfigurationBuilder();

        Action act = () => builder.AddInfisical(bootstrapConfiguration);

        await Assert.That(act).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ConvertToConfigurationKey_WhenAiToolProposalsSecretProvided_MapsToAiProviderSetting()
    {
        var key = await ConvertToConfigurationKey("AI_TOOL_PROPOSALS_ENABLED", "/");

        await Assert.That(key).IsEqualTo("AiProvider:ToolProposalsEnabled");
    }

    private static async Task<string> ConvertToConfigurationKey(string secretKey, string path)
    {
        var method = typeof(InfisicalConfigurationProvider).GetMethod(
            "ConvertToConfigurationKey",
            BindingFlags.NonPublic | BindingFlags.Static);

        await Assert.That(method).IsNotNull();
        return ((string?)method!.Invoke(null, [secretKey, path]))!;
    }
}
