// ABOUTME: Generates the canonical API contract inventory markdown from schemas/openapi.json.
// ABOUTME: Test-based generator reads schemas/openapi.json and writes the committed docs artifact.

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Emits the source-of-truth API contract inventory.
///
/// This is NOT an assertion test. It is a governed code-generator that runs under
/// the integration-test harness to guarantee (a) the OpenAPI schema exists and is
/// well-formed, and (b) downstream governance artifacts stay in sync with the
/// checked-in build-time contract. CI drift detection can be layered on top via
/// <c>git diff --exit-code docs/API_CONTRACT_INVENTORY.md</c>.
///
/// Output: <c>docs/API_CONTRACT_INVENTORY.md</c>.
///
/// Columns: Path | HTTP Method | OperationId | Summary | Tags | RouteName | Classification | Has Auth? | TenantMode | RateLimitPolicy | CachePolicy
/// RouteName mirrors operationId because operationId is propagated from <c>RouteNames.Xxx</c>.
/// Classification is populated from the <c>x-endpoint-class</c> OpenAPI extension.
/// </summary>
public sealed class ApiContractInventoryGeneratorTests
{
    // Fixed verb order for stable output (OpenAPI 3.0 allowed verbs).
    private static readonly string[] VerbOrder =
    [
        "get", "post", "put", "patch", "delete", "options", "head", "trace"
    ];

    // Placeholder-operationId detectors (mirror Phase 0 rules). Used for the
    // summary counts at the bottom of the inventory so readers can see the
    // defect surface at a glance.
    private static readonly Regex DigitSuffixBeforeAsync = new(@"\d+Async$", RegexOptions.Compiled);
    private static readonly Regex EndsWithDigit = new(@"\d$", RegexOptions.Compiled);

    [Test]
    public async Task ApiContractInventory_Generate_WritesMarkdownToDocs()
    {
        var repoRoot = FindRepoRoot()
            ?? throw new InvalidOperationException(
                "Could not locate repository root from AppContext.BaseDirectory. " +
                "Expected to find a parent directory containing AGENTS.md and Explore.API/.");

        var schemaPath = Path.Combine(repoRoot, "schemas", "openapi.json");
        await Assert.That(File.Exists(schemaPath)).IsTrue()
            .Because("schemas/openapi.json is the committed build-time contract used by generated clients.");

        await using var stream = File.OpenRead(schemaPath);
        using var doc = await JsonDocument.ParseAsync(stream);

        var operations = EnumerateOperations(doc).ToList();
        await Assert.That(operations).IsNotEmpty()
            .Because("A functioning API must expose at least one operation in its OpenAPI document.");

        var markdown = RenderMarkdown(operations);

        var outputPath = Path.Combine(
            repoRoot,
            "docs",
            "API_CONTRACT_INVENTORY.md");

        var outputDir = Path.GetDirectoryName(outputPath)!;
        Directory.CreateDirectory(outputDir);
        await File.WriteAllTextAsync(outputPath, markdown, Encoding.UTF8);

        await Assert.That(File.Exists(outputPath)).IsTrue()
            .Because("Inventory markdown should be written to the expected path.");
    }

    private static IEnumerable<OperationRow> EnumerateOperations(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("paths", out var paths) ||
            paths.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        foreach (var pathProperty in paths.EnumerateObject())
        {
            var path = pathProperty.Name;
            if (pathProperty.Value.ValueKind != JsonValueKind.Object) continue;

            foreach (var verb in VerbOrder)
            {
                if (!pathProperty.Value.TryGetProperty(verb, out var op) ||
                    op.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var operationId = op.TryGetProperty("operationId", out var oid) && oid.ValueKind == JsonValueKind.String
                    ? oid.GetString()
                    : null;
                var summary = op.TryGetProperty("summary", out var s) && s.ValueKind == JsonValueKind.String
                    ? s.GetString()
                    : null;
                var tags = op.TryGetProperty("tags", out var t) && t.ValueKind == JsonValueKind.Array
                    ? string.Join(", ", t.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString()))
                    : string.Empty;
                var hasAuth = op.TryGetProperty("security", out var sec) &&
                              sec.ValueKind == JsonValueKind.Array &&
                              sec.GetArrayLength() > 0;
                var classification = op.TryGetProperty("x-endpoint-class", out var cls) && cls.ValueKind == JsonValueKind.String
                    ? cls.GetString()
                    : null;
                var tenantMode = op.TryGetProperty("x-tenant-mode", out var tenant) && tenant.ValueKind == JsonValueKind.String
                    ? tenant.GetString()
                    : null;
                var rateLimitPolicy = op.TryGetProperty("x-rate-limit-policy", out var rateLimit) && rateLimit.ValueKind == JsonValueKind.String
                    ? rateLimit.GetString()
                    : null;
                var cachePolicy = op.TryGetProperty("x-output-cache-policy", out var cache) && cache.ValueKind == JsonValueKind.String
                    ? cache.GetString()
                    : null;

                yield return new OperationRow(
                    Path: path,
                    Method: verb.ToUpperInvariant(),
                    OperationId: operationId,
                    Summary: summary,
                    Tags: tags,
                    Classification: classification,
                    HasAuth: hasAuth,
                    TenantMode: tenantMode,
                    RateLimitPolicy: rateLimitPolicy,
                    CachePolicy: cachePolicy);
            }
        }
    }

    private static string RenderMarkdown(IReadOnlyList<OperationRow> operations)
    {
        var sorted = operations
            .OrderBy(o => o.Path, StringComparer.Ordinal)
            .ThenBy(o => Array.IndexOf(VerbOrder, o.Method.ToLowerInvariant()))
            .ToList();

        var distinctPaths = sorted.Select(o => o.Path).Distinct().Count();
        var missingOperationId = sorted.Count(o => string.IsNullOrWhiteSpace(o.OperationId));
        var placeholderFallback = sorted.Count(o =>
            !string.IsNullOrWhiteSpace(o.OperationId) &&
            (DigitSuffixBeforeAsync.IsMatch(o.OperationId!) ||
             EndsWithDigit.IsMatch(o.OperationId!)));
        var urlSegmentVersioned = sorted.Count(o =>
            Regex.IsMatch(o.Path, @"^/api/v\d"));
        var missingClassification = sorted.Count(o => string.IsNullOrWhiteSpace(o.Classification));
        var classificationCounts = sorted
            .Where(o => !string.IsNullOrWhiteSpace(o.Classification))
            .GroupBy(o => o.Classification!, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => $"`{g.Key}`={g.Count()}")
            .ToList();
        var rateLimitPolicyCounts = sorted
            .Where(o => !string.IsNullOrWhiteSpace(o.RateLimitPolicy))
            .GroupBy(o => o.RateLimitPolicy!, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => $"`{g.Key}`={g.Count()}")
            .ToList();
        var cachePolicyCounts = sorted
            .Where(o => !string.IsNullOrWhiteSpace(o.CachePolicy))
            .GroupBy(o => o.CachePolicy!, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => $"`{g.Key}`={g.Count()}")
            .ToList();
        var tenantModeCounts = sorted
            .Where(o => !string.IsNullOrWhiteSpace(o.TenantMode))
            .GroupBy(o => o.TenantMode!, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => $"`{g.Key}`={g.Count()}")
            .ToList();

        var sb = new StringBuilder();

        sb.AppendLine("<!-- ABOUTME: Canonical inventory of every OpenAPI operation exposed by Explore.API. -->");
        sb.AppendLine("<!-- ABOUTME: Regenerate by running ApiContractInventoryGeneratorTests in Event.API.IntegrationTests. -->");
        sb.AppendLine("<!-- AUTO-GENERATED by Event.API.IntegrationTests/Features/ApiContractInventoryGeneratorTests.cs. DO NOT EDIT BY HAND. -->");
        sb.AppendLine("<!-- Source: schemas/openapi.json -->");
        sb.AppendLine();
        sb.AppendLine("# API Contract Inventory");
        sb.AppendLine();
        sb.AppendLine($"**Source:** `schemas/openapi.json`");
        sb.AppendLine($"**Governed by:** [GOVERNANCE.md#api-contract-rules](GOVERNANCE.md#api-contract-rules)");
        sb.AppendLine("**Regenerate:** `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter \"/*/*/*/ApiContractInventory_Generate_WritesMarkdownToDocs\"`");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"- Total paths: **{distinctPaths}**");
        sb.AppendLine($"- Total operations: **{sorted.Count}**");
        sb.AppendLine($"- Operations missing `operationId`: **{missingOperationId}**");
        sb.AppendLine($"- Operation IDs with placeholder fallback pattern (ends in digit or `\\dAsync`): **{placeholderFallback}**");
        sb.AppendLine($"- URL-segment-versioned paths (`^/api/v\\d`, banned by governance): **{urlSegmentVersioned}**");
        sb.AppendLine($"- Operations missing `x-endpoint-class` extension: **{missingClassification}**");
        if (classificationCounts.Count > 0)
        {
            sb.AppendLine($"- Classification breakdown: {string.Join(", ", classificationCounts)}");
        }
        if (tenantModeCounts.Count > 0)
        {
            sb.AppendLine($"- Tenant-mode extension breakdown: {string.Join(", ", tenantModeCounts)}");
        }
        if (rateLimitPolicyCounts.Count > 0)
        {
            sb.AppendLine($"- Rate-limit policy extension breakdown: {string.Join(", ", rateLimitPolicyCounts)}");
        }
        if (cachePolicyCounts.Count > 0)
        {
            sb.AppendLine($"- Output-cache policy extension breakdown: {string.Join(", ", cachePolicyCounts)}");
        }
        sb.AppendLine();
        sb.AppendLine("## Operations");
        sb.AppendLine();
        sb.AppendLine("| # | Path | Method | OperationId | Summary | Tags | RouteName | Classification | Has Auth? | TenantMode | RateLimitPolicy | CachePolicy |");
        sb.AppendLine("|---:|---|---|---|---|---|---|---|---|---|---|---|");

        for (var i = 0; i < sorted.Count; i++)
        {
            var row = sorted[i];
            sb.Append('|').Append(' ').Append(i + 1).Append(' ');
            sb.Append("| `").Append(Escape(row.Path)).Append("` ");
            sb.Append("| `").Append(row.Method).Append("` ");
            sb.Append("| ").Append(row.OperationId is null ? "_(missing)_" : $"`{Escape(row.OperationId)}`").Append(' ');
            sb.Append("| ").Append(string.IsNullOrWhiteSpace(row.Summary) ? "_(none)_" : Escape(row.Summary!)).Append(' ');
            sb.Append("| ").Append(string.IsNullOrWhiteSpace(row.Tags) ? "_(none)_" : Escape(row.Tags)).Append(' ');
            sb.Append("| ").Append(row.OperationId is null ? "_(missing)_" : $"`{Escape(row.OperationId)}`").Append(' ');
            sb.Append("| ").Append(string.IsNullOrWhiteSpace(row.Classification) ? "_(missing)_" : $"`{Escape(row.Classification!)}`").Append(' ');
            sb.Append("| ").Append(row.HasAuth ? "yes" : "no").Append(' ');
            sb.Append("| ").Append(string.IsNullOrWhiteSpace(row.TenantMode) ? "_(none)_" : $"`{Escape(row.TenantMode!)}`").Append(' ');
            sb.Append("| ").Append(string.IsNullOrWhiteSpace(row.RateLimitPolicy) ? "_(none)_" : $"`{Escape(row.RateLimitPolicy!)}`").Append(' ');
            sb.Append("| ").Append(string.IsNullOrWhiteSpace(row.CachePolicy) ? "_(none)_" : $"`{Escape(row.CachePolicy!)}`").Append(' ');
            sb.AppendLine("|");
        }

        sb.AppendLine();
        sb.AppendLine("## Columns");
        sb.AppendLine();
        sb.AppendLine("- **Path** — raw OpenAPI path template.");
        sb.AppendLine("- **Method** — HTTP verb from the OpenAPI path item.");
        sb.AppendLine("- **OperationId** — NSwag codegen hook. Must be present, unique, and non-placeholder (see governance rules).");
        sb.AppendLine("- **Summary** — from `[EndpointSummary]` on the action, if any.");
        sb.AppendLine("- **Tags** — OpenAPI tags; typically the controller short name.");
        sb.AppendLine("- **RouteName** — stable endpoint name propagated to OpenAPI `operationId` from `[Http...(Name = RouteNames.Xxx)]`.");
        sb.AppendLine("- **Classification** — `Public` / `Authenticated` / `Admin`, sourced from the `x-endpoint-class` OpenAPI extension (injected by `EndpointClassificationTransformer` from `[EndpointClassification(...)]` attributes).");
        sb.AppendLine("- **Has Auth?** — `yes` if the OpenAPI operation has at least one non-empty `security` requirement.");
        sb.AppendLine("- **TenantMode** — sourced from the `x-tenant-mode` OpenAPI extension when the endpoint declares explicit tenant posture.");
        sb.AppendLine("- **RateLimitPolicy** — sourced from the `x-rate-limit-policy` OpenAPI extension when the endpoint declares a named rate-limit policy.");
        sb.AppendLine("- **CachePolicy** — sourced from the `x-output-cache-policy` OpenAPI extension when the endpoint declares a named output-cache policy.");

        return sb.ToString();
    }

    private static string Escape(string value)
    {
        // Markdown-table cell escaping: pipes become `\|`, newlines collapse to spaces.
        return value.Replace("|", @"\|").Replace("\r", " ").Replace("\n", " ");
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var marker = Path.Combine(dir.FullName, "AGENTS.md");
            var exploreApi = Path.Combine(dir.FullName, "Explore.API");
            if (File.Exists(marker) && Directory.Exists(exploreApi))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    private readonly record struct OperationRow(
        string Path,
        string Method,
        string? OperationId,
        string? Summary,
        string Tags,
        string? Classification,
        bool HasAuth,
        string? TenantMode,
        string? RateLimitPolicy,
        string? CachePolicy);
}
