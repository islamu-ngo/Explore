// ABOUTME: Guards the generated whole-instance configuration manifest client contract.
// ABOUTME: Pins the canonical typed view, binary response, and absence of tenant export aliases.

namespace Explore.Blazor.Client.Tests.Clients;

using System.Reflection;

public sealed class ConfigurationManifestGeneratedClientTests
{
    [Test]
    public async Task CanonicalExportPreservesTypedBinaryContract()
    {
        MethodInfo method = typeof(IEventApiClient).GetMethod(
            nameof(IEventApiClient.ExportConfigurationManifestAsync),
            [
                typeof(ConfigurationManifestExportView?),
                typeof(string),
                typeof(string),
                typeof(CancellationToken)
            ])
            ?? throw new InvalidOperationException(
                "Generated canonical manifest export method is missing.");

        await Assert.That(method.ReturnType.GetGenericArguments().Single())
            .IsEqualTo(typeof(FileResponse));
        ParameterInfo[] parameters = method.GetParameters();
        await Assert.That(parameters[0].ParameterType)
            .IsEqualTo(typeof(ConfigurationManifestExportView?));
        await Assert.That(parameters[0].IsOptional).IsTrue();
        await Assert.That(parameters[0].DefaultValue).IsNull();
        await Assert.That(parameters[^1].ParameterType)
            .IsEqualTo(typeof(CancellationToken));
        await Assert.That(parameters[^1].IsOptional).IsTrue();
    }

    [Test]
    public async Task CanonicalViewPreservesGovernedWireValues()
    {
        await Assert.That(Enum.GetNames<ConfigurationManifestExportView>())
            .IsEquivalentTo(
            [
                nameof(ConfigurationManifestExportView.Overrides),
                nameof(ConfigurationManifestExportView.Portable)
            ]);
    }

    [Test]
    public async Task TenantExportAliasesAreAbsent()
    {
        string[] methodNames = typeof(IEventApiClient).GetMethods()
            .Select(method => method.Name)
            .ToArray();

        await Assert.That(methodNames)
            .DoesNotContain("ExportTenant" + "ConfigurationManifestAsync");
        await Assert.That(methodNames)
            .DoesNotContain("ExportControlPlaneTenant" + "ConfigurationManifestAsync");
        await Assert.That(typeof(IEventApiClient).Assembly.GetType(
            "Explore.Blazor.Client.Clients.Tenant" + "ConfigurationManifestExportView"))
            .IsNull();
    }
}
