// ABOUTME: Exhaustive architecture guard comparing update-contract inventory rows with current repository reality.
// ABOUTME: Fails on unlisted DTOs, handlers, PUT/PATCH operations, stale removals, or noncanonical migrated verbs.

using System.Text.Json;
using System.Text.RegularExpressions;

namespace Event.Architecture.Tests;

public sealed partial class UpdateContractInventoryArchitectureTests
{
    [GeneratedRegex(@"^\| (?<id>D-\d{3}) \| `(?<path>[^`]+)` \| [^|]+ \| (?<disposition>[CMSARN]) \|", RegexOptions.Multiline)]
    private static partial Regex DtoRowPattern();

    [GeneratedRegex(@"^\| (?<id>H-\d{3}) \| `(?<path>[^`]+)` \| [^|]+ \| (?<disposition>[CMSARN]) \|", RegexOptions.Multiline)]
    private static partial Regex HandlerRowPattern();

    [GeneratedRegex(@"^\| (?<id>A-\d{3}) \| `(?<operation>[^`]+)` \| `[^`]+` \| (?<disposition>[CMSARN]):", RegexOptions.Multiline)]
    private static partial Regex ApiRowPattern();

    [Test]
    public async Task InventoryMustExactlyCoverCurrentUpdateContracts()
    {
        string root = ResolveRepositoryRoot();
        string inventory = await File.ReadAllTextAsync(Path.Combine(
            root,
            "dev/active/full-property-update-sub-dto/full-property-update-sub-dto-inventory.md"));
        var failures = new List<string>();

        InventoryRow[] dtoRows = ParseRows(DtoRowPattern(), inventory, "path");
        InventoryRow[] handlerRows = ParseRows(HandlerRowPattern(), inventory, "path");
        ApiInventoryRow[] apiRows = ParseApiRows(inventory);

        ValidateIds(dtoRows, "D", 59, failures);
        ValidateIds(handlerRows, "H", 81, failures);
        ValidateIds(apiRows, "A", 113, failures);
        ValidateFiles(
            Path.Combine(root, "src/Explore.Application/DTOs"),
            "Update*Dto.cs",
            dtoRows,
            failures);
        ValidateFiles(
            Path.Combine(root, "src/Explore.Application/Features"),
            "Update*CommandHandler.cs",
            handlerRows,
            failures);
        await ValidateApiRowsAsync(root, apiRows, failures);

        await Assert.That(failures).IsEmpty().Because(string.Join(Environment.NewLine, failures));
    }

    private static InventoryRow[] ParseRows(Regex pattern, string inventory, string pathGroup) => pattern
        .Matches(inventory)
        .Select(match => new InventoryRow(
            match.Groups["id"].Value,
            match.Groups[pathGroup].Value,
            match.Groups["disposition"].Value[0]))
        .ToArray();

    private static ApiInventoryRow[] ParseApiRows(string inventory) => ApiRowPattern()
        .Matches(inventory)
        .Select(match =>
        {
            string qualifiedOperation = match.Groups["operation"].Value;
            return new ApiInventoryRow(
                match.Groups["id"].Value,
                qualifiedOperation[(qualifiedOperation.LastIndexOf('.') + 1)..],
                match.Groups["disposition"].Value[0]);
        })
        .ToArray();

    private static void ValidateIds<T>(
        IReadOnlyList<T> rows,
        string prefix,
        int expectedCount,
        ICollection<string> failures)
        where T : IInventoryRow
    {
        string[] expectedIds = Enumerable.Range(1, expectedCount)
            .Select(index => $"{prefix}-{index:000}")
            .ToArray();
        string[] actualIds = rows.Select(row => row.Id).ToArray();

        if (!actualIds.SequenceEqual(expectedIds, StringComparer.Ordinal))
        {
            failures.Add($"{prefix} register IDs must be exactly {prefix}-001 through {prefix}-{expectedCount:000} in order.");
        }

        foreach (IGrouping<string, T> duplicate in rows.GroupBy(row => row.Id).Where(group => group.Count() > 1))
        {
            failures.Add($"{duplicate.Key} appears {duplicate.Count()} times.");
        }
    }

    private static void ValidateFiles(
        string registerRoot,
        string pattern,
        IReadOnlyList<InventoryRow> rows,
        ICollection<string> failures)
    {
        HashSet<string> currentFiles = Directory.GetFiles(registerRoot, pattern, SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(registerRoot, path).Replace(Path.DirectorySeparatorChar, '/'))
            .ToHashSet(StringComparer.Ordinal);
        Dictionary<string, InventoryRow> rowsByPath = rows.ToDictionary(row => row.Path, StringComparer.Ordinal);

        foreach (string currentFile in currentFiles.Where(path => !rowsByPath.ContainsKey(path)))
        {
            failures.Add($"Current update file is not registered: {Path.GetRelativePath(ResolveRepositoryRoot(), Path.Combine(registerRoot, currentFile))}.");
        }

        foreach (InventoryRow row in rows)
        {
            bool exists = File.Exists(Path.Combine(registerRoot, row.Path));
            if (row.Disposition == 'R' && exists)
            {
                failures.Add($"{row.Id} is classified R but still exists: {row.Path}.");
            }
            else if (row.Disposition != 'R' && !exists)
            {
                failures.Add($"{row.Id} is classified {row.Disposition} but is absent: {row.Path}.");
            }
        }
    }

    private static async Task ValidateApiRowsAsync(
        string root,
        IReadOnlyList<ApiInventoryRow> rows,
        ICollection<string> failures)
    {
        await using FileStream stream = File.OpenRead(Path.Combine(root, "schemas", "openapi_islamu-event.json"));
        using JsonDocument document = await JsonDocument.ParseAsync(stream);
        var currentOperations = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (JsonProperty path in document.RootElement.GetProperty("paths").EnumerateObject())
        {
            foreach (string method in new[] { "put", "patch" })
            {
                if (!path.Value.TryGetProperty(method, out JsonElement operation)
                    || !operation.TryGetProperty("operationId", out JsonElement operationIdElement))
                {
                    continue;
                }

                string operationId = operationIdElement.GetString() ?? string.Empty;
                if (!currentOperations.TryAdd(operationId, method))
                {
                    failures.Add($"OpenAPI update operation ID is duplicated: {operationId}.");
                }
            }
        }

        Dictionary<string, ApiInventoryRow> rowsByOperation = rows.ToDictionary(row => row.OperationId, StringComparer.Ordinal);
        foreach (string operationId in currentOperations.Keys.Where(operation => !rowsByOperation.ContainsKey(operation)))
        {
            failures.Add($"Current PUT/PATCH operation is not registered: {operationId}.");
        }

        string generatedClient = await File.ReadAllTextAsync(Path.Combine(
            root,
            "src/Explore.Blazor.Client/Clients/EventApiClient.g.cs"));
        string semanticRegistry = await File.ReadAllTextAsync(Path.Combine(
            root,
            "tests/Event.Architecture.Tests/SemanticUpdateExceptionArchitectureTests.cs"));

        foreach (ApiInventoryRow row in rows)
        {
            bool exists = currentOperations.TryGetValue(row.OperationId, out string? method);
            if (row.Disposition == 'R')
            {
                if (exists || generatedClient.Contains($"{row.OperationId}Async(", StringComparison.Ordinal))
                {
                    failures.Add($"{row.Id} is classified R but {row.OperationId} remains exposed.");
                }
                continue;
            }

            if (!exists)
            {
                failures.Add($"{row.Id} is classified {row.Disposition} but {row.OperationId} is absent from OpenAPI.");
                continue;
            }

            if (row.Disposition is 'M' or 'C' && method != "patch")
            {
                failures.Add($"{row.Id} must be PATCH after migration but is {method?.ToUpperInvariant()}.");
            }

            if (row.Disposition is 'A' or 'S'
                && !semanticRegistry.Contains($"new(\"{row.OperationId}\"", StringComparison.Ordinal))
            {
                failures.Add($"{row.Id} lacks an exact semantic-exception rationale: {row.OperationId}.");
            }

            if (!generatedClient.Contains($"{row.OperationId}Async(", StringComparison.Ordinal))
            {
                failures.Add($"Generated client is missing current operation {row.OperationId}.");
            }
        }
    }

    private static string ResolveRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Explore.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate repository root containing Explore.slnx.");
    }

    private interface IInventoryRow
    {
        string Id { get; }
    }

    private sealed record InventoryRow(string Id, string Path, char Disposition) : IInventoryRow;

    private sealed record ApiInventoryRow(string Id, string OperationId, char Disposition) : IInventoryRow;
}
