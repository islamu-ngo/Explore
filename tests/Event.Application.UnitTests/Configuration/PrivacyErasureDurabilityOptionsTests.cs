// ABOUTME: Verifies strict startup-only selection of application or retained erasure durability.
// ABOUTME: Proves secrets never activate retained mode and the legacy connection key is ignored.

using Explore.Application;
using Explore.Application.Configuration;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TUnit.Core;

namespace Event.Application.UnitTests.Configuration;

public sealed class PrivacyErasureDurabilityOptionsTests
{
    [Test]
    public async Task AbsentConfiguration_DefaultsToApplicationDatabase()
    {
        PrivacyErasureDurabilityOptions options = Resolve(
            new Dictionary<string, string?>());
        await Assert.That(options.Mode).IsEqualTo(PrivacyErasureDurabilityMode.ApplicationDatabase);
    }

    [Test]
    public async Task StrayAuthorityConnection_DoesNotActivateRetainedMode()
    {
        PrivacyErasureDurabilityOptions options = Resolve(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PrivacyErasureAuthority"] =
                "Host=unused;Database=unused;Username=unused"
        });
        await Assert.That(options.Mode).IsEqualTo(PrivacyErasureDurabilityMode.ApplicationDatabase);
    }

    [Test]
    public async Task InvalidMode_FailsValidation()
    {
        await Assert.ThrowsAsync<OptionsValidationException>(() => Task.FromResult(Resolve(new Dictionary<string, string?>
        {
            ["PrivacyErasure:Durability:Mode"] = "Automatic"
        })));
    }

    [Test]
    public async Task NumericZeroMode_FailsValidation()
    {
        await Assert.ThrowsAsync<OptionsValidationException>(() => Task.FromResult(Resolve(new Dictionary<string, string?>
        {
            ["PrivacyErasure:Durability:Mode"] = "0"
        })));
    }

    [Test]
    public async Task NumericOneMode_FailsValidation()
    {
        await Assert.ThrowsAsync<OptionsValidationException>(() => Task.FromResult(Resolve(new Dictionary<string, string?>
        {
            ["PrivacyErasure:Durability:Mode"] = "1",
            ["ConnectionStrings:PrivacyErasureAuthority"] =
                "Host=unused;Database=unused;Username=unused"
        })));
    }

    [Test]
    public async Task RetainedModeWithoutCanonicalConnection_FailsValidation()
    {
        await Assert.ThrowsAsync<OptionsValidationException>(() => Task.FromResult(Resolve(new Dictionary<string, string?>
        {
            ["PrivacyErasure:Durability:Mode"] = "RetainedAuthority",
            ["LocationPrivacy:ErasureAuthority:ConnectionString"] = "Host=legacy"
        })));
    }

    [Test]
    public async Task DefaultComposition_SelectsApplicationDatabaseWorkflow()
    {
        var services = new ServiceCollection();
        services.ConfigureApplicationServices(new ConfigurationBuilder().Build());

        ServiceDescriptor workflow = services.Last(descriptor =>
            descriptor.ServiceType == typeof(IPrivacyErasureService));
        await Assert.That(workflow.ImplementationType)
            .IsEqualTo(typeof(ApplicationDatabasePrivacyErasureWorkflow));
    }

    [Test]
    public async Task ExplicitRetainedComposition_SelectsRetainedWorkflow()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["PrivacyErasure:Durability:Mode"] = "retainedauthority",
                ["ConnectionStrings:PrivacyErasureAuthority"] =
                    "Host=unused;Database=unused;Username=unused"
            }).Build();
        var services = new ServiceCollection();
        services.ConfigureApplicationServices(configuration);

        ServiceDescriptor workflow = services.Last(descriptor =>
            descriptor.ServiceType == typeof(IPrivacyErasureService));
        await Assert.That(workflow.ImplementationType)
            .IsEqualTo(typeof(RetainedAuthorityPrivacyErasureWorkflow));
    }

    private static PrivacyErasureDurabilityOptions Resolve(
        IDictionary<string, string?> values) =>
        PrivacyErasureDurabilityOptions.FromConfiguration(
            new ConfigurationBuilder().AddInMemoryCollection(values).Build());
}
