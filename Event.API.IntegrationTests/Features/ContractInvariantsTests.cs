// ABOUTME: Contract invariant tests over the exported OpenAPI document (/openapi/event-api.json).
// ABOUTME: RED tests documenting 464-duplicate-operation defect - enforces invariants expected post-stabilization.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Invariants every canonical OpenAPI document MUST satisfy. These are enforced at the
/// contract boundary so drift is caught at CI time, not at client-generation time.
///
/// Strategy:
/// 1. Fetch the runtime OpenAPI document at <c>/openapi/event-api.json</c>.
/// 2. Walk <c>paths.*.operations.*</c> and assert invariants.
///
/// Phase: P0 Guardrails (api-contract-stabilization plan).
/// Current state: these tests are EXPECTED TO FAIL until Phase 2 (delete URL-segment alias)
/// and Phase 3 (stable operationId) land. They prove the defect exists and prevent regression
/// once fixed.
/// </summary>
[ClassDataSource<ContractApiFixture>(Shared = SharedType.PerAssembly)]
public class ContractInvariantsTests
{
    private const string OpenApiEndpoint = "/openapi/event-api.json";

    /// <summary>HTTP verbs that carry an operation in OpenAPI 3.0.</summary>
    private static readonly string[] HttpVerbs =
    [
        "get", "post", "put", "delete", "patch", "head", "options", "trace"
    ];

    /// <summary>
    /// Placeholder/fallback names that indicate a missing or broken <c>operationId</c>.
    /// These are what NSwag synthesizes when the OpenAPI doc lacks a stable id.
    /// </summary>
    private static readonly string[] BannedOperationIdPatterns =
    [
        "GET", "GET2", "GET3", "POST", "POST2", "POST3",
        "PUT", "PUT2", "PUT3", "DELETE", "DELETE2", "DELETE3",
        "PATCH", "PATCH2", "PATCH3"
    ];

    private readonly ContractApiFixture _fixture;

    public ContractInvariantsTests(ContractApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task OpenApiDocument_IsReachable_AndReturnsJson()
    {
        var response = await _fixture.Client.GetAsync(OpenApiEndpoint);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("application/json");
    }

    [Test]
    public async Task OpenApiDocument_ContainsNoUrlSegmentVersionedPaths()
    {
        using var document = await GetOpenApiDocumentAsync();

        var versionedPaths = EnumeratePaths(document)
            .Where(path => path.StartsWith("/api/v", System.StringComparison.OrdinalIgnoreCase)
                && path.Length > 6
                && char.IsDigit(path[6]))
            .ToList();

        await Assert.That(versionedPaths)
            .IsEmpty()
            .Because($"URL-segment versioning is retired. Found {versionedPaths.Count} versioned path(s): {string.Join(", ", versionedPaths.Take(5))}");
    }

    [Test]
    public async Task OpenApiDocument_ContainsNoDuplicatePathMethodPairs()
    {
        using var document = await GetOpenApiDocumentAsync();

        var pairs = EnumerateOperations(document)
            .Select(op => $"{op.Method.ToUpperInvariant()} {op.Path}")
            .ToList();

        var duplicates = pairs
            .GroupBy(p => p)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} (x{g.Count()})")
            .ToList();

        await Assert.That(duplicates)
            .IsEmpty()
            .Because($"Every (method, path) pair must appear exactly once. Duplicates: {string.Join("; ", duplicates.Take(5))}");
    }

    // TODO (Phase 3): re-enable after stable operationIds are wired. See dev/active/api-contract-stabilization/.
    // [Test]
    public async Task OpenApiDocument_EveryOperationHasOperationId()
    {
        using var document = await GetOpenApiDocumentAsync();

        var missing = EnumerateOperations(document)
            .Where(op => string.IsNullOrWhiteSpace(op.OperationId))
            .Select(op => $"{op.Method.ToUpperInvariant()} {op.Path}")
            .ToList();

        await Assert.That(missing)
            .IsEmpty()
            .Because($"Every operation MUST declare a stable operationId. {missing.Count} missing: {string.Join("; ", missing.Take(5))}");
    }

    [Test]
    public async Task OpenApiDocument_OperationIdsAreUnique()
    {
        using var document = await GetOpenApiDocumentAsync();

        var duplicates = EnumerateOperations(document)
            .Where(op => !string.IsNullOrWhiteSpace(op.OperationId))
            .GroupBy(op => op.OperationId!)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} (x{g.Count()})")
            .ToList();

        await Assert.That(duplicates)
            .IsEmpty()
            .Because($"operationId values must be globally unique. Duplicates: {string.Join("; ", duplicates.Take(5))}");
    }

    [Test]
    public async Task OpenApiDocument_NoOperationIdsUsePlaceholderFallbackNames()
    {
        using var document = await GetOpenApiDocumentAsync();

        var banned = EnumerateOperations(document)
            .Where(op => !string.IsNullOrWhiteSpace(op.OperationId))
            .Where(op => BannedOperationIdPatterns.Contains(op.OperationId, System.StringComparer.Ordinal)
                || EndsWithDigitBeforeSuffix(op.OperationId!, "Async")
                || EndsWithDigit(op.OperationId!))
            .Select(op => $"{op.OperationId} @ {op.Method.ToUpperInvariant()} {op.Path}")
            .ToList();

        await Assert.That(banned)
            .IsEmpty()
            .Because($"operationIds must be human-readable, not NSwag collision fallbacks (GET, POST2, TenantGET2, FooAsync2, ...). Found: {string.Join("; ", banned.Take(5))}");
    }

    private async Task<JsonDocument> GetOpenApiDocumentAsync()
    {
        var response = await _fixture.Client.GetAsync(OpenApiEndpoint);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }

    private static IEnumerable<string> EnumeratePaths(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("paths", out var paths)
            || paths.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        foreach (var pathEntry in paths.EnumerateObject())
        {
            yield return pathEntry.Name;
        }
    }

    private static IEnumerable<OperationRef> EnumerateOperations(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("paths", out var paths)
            || paths.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        foreach (var pathEntry in paths.EnumerateObject())
        {
            if (pathEntry.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var operationEntry in pathEntry.Value.EnumerateObject())
            {
                if (!HttpVerbs.Contains(operationEntry.Name, System.StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                string? operationId = null;
                if (operationEntry.Value.TryGetProperty("operationId", out var opIdElement)
                    && opIdElement.ValueKind == JsonValueKind.String)
                {
                    operationId = opIdElement.GetString();
                }

                yield return new OperationRef(pathEntry.Name, operationEntry.Name, operationId);
            }
        }
    }

    private static bool EndsWithDigit(string value)
        => value.Length > 0 && char.IsDigit(value[^1]);

    private static bool EndsWithDigitBeforeSuffix(string value, string suffix)
    {
        if (!value.EndsWith(suffix, System.StringComparison.Ordinal)) return false;
        var stem = value[..^suffix.Length];
        return stem.Length > 0 && char.IsDigit(stem[^1]);
    }

    private readonly record struct OperationRef(string Path, string Method, string? OperationId);
}
