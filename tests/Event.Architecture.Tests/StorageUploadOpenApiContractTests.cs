// ABOUTME: Verifies the canonical storage upload operation exposes a generated-client-compatible binary body.
// ABOUTME: Prevents regressions to bodyless clients that force Blazor to construct backend HTTP requests manually.

using System.Text.Json;

namespace Event.Architecture.Tests;

public sealed class StorageUploadOpenApiContractTests
{
    [Test]
    public async Task StorageUploadSessionContent_MustDeclareRequiredBinaryRequestBody()
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var schemaPath = Path.Combine(repositoryRoot, "schemas", "openapi_islamu-event.json");
        await using var schemaStream = File.OpenRead(schemaPath);
        using var document = await JsonDocument.ParseAsync(schemaStream);

        var operation = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/storageobject/upload-sessions/{uploadSessionId}/content")
            .GetProperty("put");
        var requestBody = operation.GetProperty("requestBody");
        var schema = requestBody
            .GetProperty("content")
            .GetProperty("application/octet-stream")
            .GetProperty("schema");

        await Assert.That(requestBody.GetProperty("required").GetBoolean()).IsTrue();
        await Assert.That(schema.GetProperty("type").GetString()).IsEqualTo("string");
        await Assert.That(schema.GetProperty("format").GetString()).IsEqualTo("binary");
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
