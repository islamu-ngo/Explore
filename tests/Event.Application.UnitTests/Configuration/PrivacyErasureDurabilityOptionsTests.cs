// ABOUTME: Verifies the explicit privacy-erasure authority topology configuration contract.
// ABOUTME: Defaults to CoLocated, rejects legacy mode keys, and requires secrets only for ExternalDatabase.

using Explore.Application.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TUnit.Core;

namespace Event.Application.UnitTests.Configuration;

public sealed class PrivacyErasureDurabilityOptionsTests
{
    [Test]
    public async Task AbsentConfiguration_DefaultsToCoLocated()
    {
        PrivacyErasureDurabilityOptions options = Resolve(
            new Dictionary<string, string?>());

        await Assert.That(options.Topology)
            .IsEqualTo(PrivacyErasureAuthorityTopology.CoLocated);
    }

    [Test]
    [Arguments("CoLocated", PrivacyErasureAuthorityTopology.CoLocated)]
    [Arguments("colocated", PrivacyErasureAuthorityTopology.CoLocated)]
    [Arguments("ExternalDatabase", PrivacyErasureAuthorityTopology.ExternalDatabase)]
    [Arguments("externaldatabase", PrivacyErasureAuthorityTopology.ExternalDatabase)]
    public async Task SupportedTopologyName_IsAccepted(
        string configured,
        PrivacyErasureAuthorityTopology expected)
    {
        var values = new Dictionary<string, string?>
        {
            ["PrivacyErasure:Authority:Topology"] = configured
        };
        if (expected == PrivacyErasureAuthorityTopology.ExternalDatabase)
        {
            values["ConnectionStrings:PrivacyErasureAuthority"] =
                "Host=unused;Database=unused;Username=unused";
        }

        await Assert.That(Resolve(values).Topology).IsEqualTo(expected);
    }

    [Test]
    [Arguments("")]
    [Arguments(" ")]
    [Arguments("Automatic")]
    [Arguments("0")]
    [Arguments("1")]
    [Arguments("ApplicationDatabase")]
    [Arguments("RetainedAuthority")]
    public async Task UnsupportedTopologyName_FailsValidation(string configured)
    {
        await Assert.ThrowsAsync<OptionsValidationException>(() =>
            Task.FromResult(Resolve(new Dictionary<string, string?>
            {
                ["PrivacyErasure:Authority:Topology"] = configured
            })));
    }

    [Test]
    [Arguments("")]
    [Arguments(" ")]
    [Arguments("ApplicationDatabase")]
    [Arguments("RetainedAuthority")]
    public async Task PresentLegacyMode_FailsWithResetOnlyReplacementGuidance(string legacyMode)
    {
        OptionsValidationException? exception = await Assert.ThrowsAsync<OptionsValidationException>(() =>
            Task.FromResult(Resolve(new Dictionary<string, string?>
            {
                ["PrivacyErasure:Durability:Mode"] = legacyMode,
                ["PrivacyErasure:Authority:Topology"] = "CoLocated"
            })));

        await Assert.That(exception!.Failures.Single())
            .Contains("PrivacyErasure:Durability:Mode", StringComparison.Ordinal);
        await Assert.That(exception.Failures.Single())
            .Contains("PrivacyErasure:Authority:Topology", StringComparison.Ordinal);
        await Assert.That(exception.Failures.Single())
            .Contains("reset", StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(legacyMode))
        {
            await Assert.That(exception.Failures.Single()).DoesNotContain(legacyMode);
        }
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments(" ")]
    [Arguments("Host=only")]
    [Arguments("not-a-connection-string")]
    public async Task ExternalDatabase_InvalidConnection_FailsValidation(string? connectionString)
    {
        await Assert.ThrowsAsync<OptionsValidationException>(() =>
            Task.FromResult(Resolve(new Dictionary<string, string?>
            {
                ["PrivacyErasure:Authority:Topology"] = "ExternalDatabase",
                ["ConnectionStrings:PrivacyErasureAuthority"] = connectionString
            })));
    }

    [Test]
    public async Task CoLocated_StrayAuthorityConnectionDoesNotChangeTopology()
    {
        PrivacyErasureDurabilityOptions options = Resolve(new Dictionary<string, string?>
        {
            ["PrivacyErasure:Authority:Topology"] = "CoLocated",
            ["ConnectionStrings:PrivacyErasureAuthority"] =
                "Host=unused;Database=unused;Username=unused"
        });

        await Assert.That(options.Topology)
            .IsEqualTo(PrivacyErasureAuthorityTopology.CoLocated);
    }

    private static PrivacyErasureDurabilityOptions Resolve(
        IDictionary<string, string?> values) =>
        PrivacyErasureDurabilityOptions.FromConfiguration(
            new ConfigurationBuilder().AddInMemoryCollection(values).Build());
}
