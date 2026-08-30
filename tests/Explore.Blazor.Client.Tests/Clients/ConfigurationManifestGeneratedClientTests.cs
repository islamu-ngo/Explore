// ABOUTME: Guards generated configuration manifest export and import-session client contracts.
// ABOUTME: Pins binary upload streams, required header capabilities, and canonical operation names.

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

    [Test]
    public async Task ImportUploadsUseCanonicalBinaryStreams()
    {
        MethodInfo instance = RequireMethod(
            nameof(IEventApiClient.CreateInstanceConfigurationImportSessionAsync));
        MethodInfo tenant = RequireMethod(
            nameof(IEventApiClient.CreateTenantConfigurationImportSessionAsync));

        await Assert.That(instance.GetParameters()[0].ParameterType)
            .IsEqualTo(typeof(Stream));
        await Assert.That(tenant.GetParameters()[0].ParameterType)
            .IsEqualTo(typeof(Guid));
        await Assert.That(tenant.GetParameters()[1].ParameterType)
            .IsEqualTo(typeof(Stream));
        await Assert.That(instance.ReturnType.GetGenericArguments().Single())
            .IsEqualTo(
                typeof(HalResourceOfConfigurationImportSessionCreatedResult));
    }

    [Test]
    public async Task ImportPreviewAndCancellationRequireHeaderCapability()
    {
        string[] operations =
        [
            nameof(IEventApiClient.PreviewInstanceConfigurationImportSessionAsync),
            nameof(IEventApiClient.RefreshInstanceConfigurationImportSessionAsync),
            nameof(IEventApiClient.CancelInstanceConfigurationImportSessionAsync),
            nameof(IEventApiClient.PreviewTenantConfigurationImportSessionAsync),
            nameof(IEventApiClient.RefreshTenantConfigurationImportSessionAsync),
            nameof(IEventApiClient.CancelTenantConfigurationImportSessionAsync)
        ];

        foreach (string operation in operations)
        {
            MethodInfo method = RequireMethod(operation);
            ParameterInfo capability = method.GetParameters().Single(
                parameter =>
                    parameter.Name == "x_Configuration_Import_Token");
            await Assert.That(capability.ParameterType)
                .IsEqualTo(typeof(string));
            await Assert.That(capability.IsOptional).IsFalse();
            await Assert.That(capability.HasDefaultValue).IsFalse();
        }
    }

    [Test]
    public async Task ImportRequestCannotCarryTargetOrFreshnessFacts()
    {
        string[] properties = typeof(ConfigurationImportPreviewRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        await Assert.That(properties).IsEquivalentTo(
        [
            "ApplyMode",
            "GrantedApprovalCodes",
            "Mappings",
            "SelectedSectionKeys"
        ]);
    }

    [Test]
    public async Task CreatedSessionDiagnosticsNeverPrintCapabilityToken()
    {
        const string sentinel = "generated-capability-sentinel";
        var result = new ConfigurationImportSessionCreatedResult
        {
            SessionId = Guid.NewGuid(),
            AccessToken = sentinel,
            TargetScope = ConfigurationImportScope.Instance,
            State = ConfigurationImportSessionState.Uploaded,
            ExpiresAt = DateTimeOffset.UtcNow,
            ArtifactByteLength = 128
        };

        await Assert.That(result.ToString()).DoesNotContain(sentinel);
    }

    private static MethodInfo RequireMethod(string name)
    {
        MethodInfo[] methods = typeof(IEventApiClient).GetMethods()
            .Where(method => method.Name == name)
            .ToArray();
        return methods.Length == 1
            ? methods[0]
            : throw new InvalidOperationException(
                $"Expected one generated method named {name}.");
    }
}
