// ABOUTME: Writes the pretty-printed OpenAPI document to Explore.API/swagger.json for NSwag client generation.
// ABOUTME: Phase 4.1 of api-contract-stabilization. Test-based exporter reuses ContractApiFixture.

using System.Text;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Refreshes the checked-in <c>Explore.API/swagger.json</c> from the live OpenAPI document.
///
/// This is NOT an assertion test. It is a governed file-generator that runs under the
/// integration-test harness to guarantee the swagger.json used by NSwag client generation
/// stays aligned with the actual API surface. The runtime <c>OpenApiExportService</c> also
/// writes this file at startup in Development, but that path requires the full Aspire
/// AppHost to be running. This test lets a headless test run refresh it deterministically.
///
/// Output: <c>Explore.API/swagger.json</c>.
/// </summary>
[ClassDataSource<ContractApiFixture>(Shared = SharedType.PerAssembly)]
public sealed class SwaggerJsonExportTests
{
    private readonly ContractApiFixture _fixture;

    public SwaggerJsonExportTests(ContractApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task SwaggerJson_Export_WritesPrettyPrintedDocToExploreApi()
    {
        using var response = await _fixture.Client.GetAsync("/openapi/event-api.json");
        await Assert.That(response.IsSuccessStatusCode).IsTrue()
            .Because("The OpenAPI document must be reachable before swagger.json can be refreshed.");

        var rawJson = await response.Content.ReadAsStringAsync();

        // Pretty-print so diffs stay readable.
        using var jsonDoc = JsonDocument.Parse(rawJson);
        var prettyJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var repoRoot = FindRepoRoot()
            ?? throw new InvalidOperationException(
                "Could not locate repository root from AppContext.BaseDirectory. " +
                "Expected to find a parent directory containing CLAUDE.md and Explore.API/.");

        var outputPath = Path.Combine(repoRoot, "Explore.API", "swagger.json");

        await File.WriteAllTextAsync(outputPath, prettyJson, Encoding.UTF8);

        await Assert.That(File.Exists(outputPath)).IsTrue()
            .Because("swagger.json should be written to Explore.API/.");
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var marker = Path.Combine(dir.FullName, "CLAUDE.md");
            var exploreApi = Path.Combine(dir.FullName, "Explore.API");
            if (File.Exists(marker) && Directory.Exists(exploreApi))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
