// ABOUTME: Verifies onboarding and instance probe operations expose concrete success response schemas.
// ABOUTME: Prevents generated API methods from regressing to untyped object return values.

using System.Text.Json;
using Explore.Application.DTOs.Instance;

namespace Event.Architecture.Tests;

public sealed class InstanceOnboardingOpenApiContractTests
{
    private static readonly (string Path, string Method, string Schema)[] Operations =
    [
        ("/api/instanceonboarding/validate-secret", "post", "SetupSecretValidationResultDto"),
        ("/api/instance/settings/smtp/test", "post", "SmtpConnectionTestResultDto"),
        ("/api/instance/settings/auth-provider/status", "get", "ProviderConfigurationStatusDto"),
        ("/api/instance/settings/authz-provider/status", "get", "ProviderConfigurationStatusDto")
    ];

    [Test]
    public async Task InstanceProbeOperations_MustDeclareConcreteSuccessSchemas()
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var schemaPath = Path.Combine(repositoryRoot, "schemas", "openapi_islamu-event.json");
        await using var schemaStream = File.OpenRead(schemaPath);
        using var document = await JsonDocument.ParseAsync(schemaStream);

        foreach (var (path, method, expectedSchema) in Operations)
        {
            var response = document.RootElement
                .GetProperty("paths")
                .GetProperty(path)
                .GetProperty(method)
                .GetProperty("responses")
                .GetProperty("200");
            var schemaReference = response
                .GetProperty("content")
                .EnumerateObject()
                .First(content => content.Name.StartsWith("application/json", StringComparison.Ordinal))
                .Value
                .GetProperty("schema")
                .GetProperty("$ref")
                .GetString();

            await Assert.That(schemaReference).IsEqualTo($"#/components/schemas/{expectedSchema}");
        }
    }

    [Test]
    public async Task TenantDelegationContract_MustNotExposeLegacyDecentralizationControls()
    {
        var serializedDto = JsonSerializer.Serialize(
            new TenantDelegationSettingsDto(),
            JsonSerializerOptions.Web);
        using var serializedDocument = JsonDocument.Parse(serializedDto);

        await Assert.That(serializedDocument.RootElement.TryGetProperty("decentralizationEnabled", out _)).IsFalse();
        await Assert.That(serializedDocument.RootElement.TryGetProperty("lockDecentralizationEnabled", out _)).IsFalse();

        var repositoryRoot = ResolveRepositoryRoot();
        var schemaPath = Path.Combine(repositoryRoot, "schemas", "openapi_islamu-event.json");
        await using var schemaStream = File.OpenRead(schemaPath);
        using var schemaDocument = await JsonDocument.ParseAsync(schemaStream);
        var properties = schemaDocument.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty(nameof(TenantDelegationSettingsDto))
            .GetProperty("properties");

        await Assert.That(properties.TryGetProperty("decentralizationEnabled", out _)).IsFalse();
        await Assert.That(properties.TryGetProperty("lockDecentralizationEnabled", out _)).IsFalse();

        var generatedClientPath = Path.Combine(
            repositoryRoot,
            "src",
            "Explore.Blazor.Client",
            "Clients",
            "EventApiClient.g.cs");
        var generatedClient = await File.ReadAllTextAsync(generatedClientPath);

        await Assert.That(generatedClient).DoesNotContain("DecentralizationEnabled", StringComparison.Ordinal);
        await Assert.That(generatedClient).DoesNotContain("decentralizationEnabled", StringComparison.Ordinal);
    }

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Explore.slnx")))
        {
            current = current.Parent;
        }

        return current?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the repository root from the architecture test output directory.");
    }
}
