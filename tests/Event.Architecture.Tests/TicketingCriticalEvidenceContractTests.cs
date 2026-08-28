// ABOUTME: Validates machine-readable critical evidence for each completed ticketing lifecycle phase.
// ABOUTME: Enforces mutation scores, zero-PII scans, anonymized MAD closure, and referenced artifact existence.

using System.Buffers;
using System.Globalization;
using System.Text.Json;

namespace Event.Architecture.Tests;

public sealed class TicketingCriticalEvidenceContractTests
{
    private const string EvidenceDirectory =
        "dev/active/event-ticketing-lifecycle/evidence";

    [Test]
    public async Task CompletedPhaseEvidenceMustSatisfyTheCriticalContract()
    {
        string evidenceRoot = ContextSystemHelpers.RepoPath(
            EvidenceDirectory.Split('/'));
        string[] manifests = Directory.GetFiles(
            evidenceRoot,
            "phase-*-evidence.yaml",
            SearchOption.TopDirectoryOnly);
        var failures = new List<string>();

        if (manifests.Length == 0)
        {
            failures.Add("No ticketing phase evidence manifests were found.");
        }

        foreach (string manifestPath in manifests.Order(StringComparer.Ordinal))
        {
            ValidateManifest(manifestPath, failures);
        }

        await Assert.That(failures).IsEmpty();
    }

    private static void ValidateManifest(
        string manifestPath,
        ICollection<string> failures)
    {
        string[] lines = File.ReadAllLines(manifestPath);
        string label = Path.GetFileName(manifestPath);

        RequireScalar(lines, "status", "pass", label, failures);
        RequireScalar(lines, "sentinel_pii_scan", "pass", label, failures);

        foreach (string section in new[]
                 {
                     "documentation",
                     "source_evidence",
                     "comments",
                     "evidence_artifacts"
                 })
        {
            string[] paths = ReadSequence(lines, section);
            if (paths.Length == 0)
            {
                failures.Add($"{label}: '{section}' must enumerate at least one path.");
            }

            ValidateReferencedPaths(paths, label, failures);
        }

        ValidateReferencedPaths(
            ReadSequence(lines, "generated_artifacts"),
            label,
            failures);
        ValidatePiiScan(
            ReadNestedScalar(lines, "pii_scan", "path"),
            label,
            failures);
        ValidateMadReview(
            ReadNestedScalar(lines, "mad_review", "path"),
            label,
            failures);
        ValidateMutationReports(lines, label, failures);
    }

    private static void ValidateMutationReports(
        string[] lines,
        string label,
        ICollection<string> failures)
    {
        IReadOnlyList<IReadOnlyDictionary<string, string>> reports =
            ReadObjectSequence(lines, "mutation_reports");
        if (reports.Count == 0)
        {
            failures.Add($"{label}: 'mutation_reports' must not be empty.");
            return;
        }

        foreach (IReadOnlyDictionary<string, string> report in reports)
        {
            if (!report.TryGetValue("project", out string? project)
                || !report.TryGetValue("path", out string? relativePath)
                || !report.TryGetValue(
                    "minimum_score_exclusive",
                    out string? minimumText)
                || !double.TryParse(
                    minimumText,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out double minimum))
            {
                failures.Add($"{label}: a mutation report entry is incomplete.");
                continue;
            }

            string path = ContextSystemHelpers.RepoPath(
                relativePath.Split('/'));
            if (!File.Exists(path))
            {
                failures.Add($"{label}: mutation report for {project} does not exist at {relativePath}.");
                continue;
            }

            double? score = ReadMutationScore(path);
            if (score is null)
            {
                failures.Add($"{label}: mutation report for {project} has no scoreable mutants.");
            }
            else if (score <= minimum)
            {
                failures.Add(
                    $"{label}: mutation score for {project} is {score:F2}; expected greater than {minimum:F2}.");
            }
        }
    }

    private static double? ReadMutationScore(string path)
    {
        int detected = 0;
        int undetected = 0;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024);

        try
        {
            using FileStream stream = File.OpenRead(path);
            var state = new JsonReaderState();
            int buffered = 0;
            bool awaitingStatusValue = false;
            bool isFinalBlock = false;

            while (!isFinalBlock)
            {
                if (buffered == buffer.Length)
                {
                    byte[] expanded = ArrayPool<byte>.Shared.Rent(
                        checked(buffer.Length * 2));
                    buffer.AsSpan(0, buffered).CopyTo(expanded);
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = expanded;
                }

                int read = stream.Read(
                    buffer,
                    buffered,
                    buffer.Length - buffered);
                isFinalBlock = read == 0;
                int available = buffered + read;
                var reader = new Utf8JsonReader(
                    buffer.AsSpan(0, available),
                    isFinalBlock,
                    state);

                while (reader.Read())
                {
                    if (awaitingStatusValue)
                    {
                        if (reader.TokenType != JsonTokenType.String)
                        {
                            throw new JsonException(
                                "A Stryker status value must be a string.");
                        }

                        string? status = reader.GetString();
                        if (status is "Killed" or "Timeout")
                        {
                            detected++;
                        }
                        else if (status is "Survived" or "NoCoverage")
                        {
                            undetected++;
                        }

                        awaitingStatusValue = false;
                    }
                    else if (reader.TokenType == JsonTokenType.PropertyName
                             && reader.ValueTextEquals("status"))
                    {
                        awaitingStatusValue = true;
                    }
                }

                int consumed = checked((int)reader.BytesConsumed);
                buffered = available - consumed;
                if (buffered > 0)
                {
                    buffer.AsSpan(consumed, buffered).CopyTo(buffer);
                }

                state = reader.CurrentState;
            }

            if (buffered != 0 || awaitingStatusValue)
            {
                throw new JsonException(
                    "The Stryker report ended with incomplete JSON.");
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        int scoreable = detected + undetected;
        return scoreable == 0
            ? null
            : detected * 100d / scoreable;
    }

    private static void ValidatePiiScan(
        string relativePath,
        string label,
        ICollection<string> failures)
    {
        string path = ContextSystemHelpers.RepoPath(relativePath.Split('/'));
        if (!File.Exists(path))
        {
            failures.Add($"{label}: PII scan does not exist at {relativePath}.");
            return;
        }

        using JsonDocument document = JsonDocument.Parse(
            File.ReadAllText(path));
        JsonElement root = document.RootElement;
        if (root.GetProperty("status").GetString() != "pass")
        {
            failures.Add($"{label}: PII scan status must be 'pass'.");
        }

        foreach (JsonProperty sentinel in root
            .GetProperty("sentinels")
            .EnumerateObject())
        {
            if (sentinel.Value.GetInt32() != 0)
            {
                failures.Add(
                    $"{label}: PII sentinel '{sentinel.Name}' has a non-zero count.");
            }
        }

        string[] scannedPaths = root.GetProperty("scannedPaths")
            .EnumerateArray()
            .Select(item => item.GetString())
            .Where(item => item is not null)
            .Cast<string>()
            .ToArray();
        if (scannedPaths.Length == 0)
        {
            failures.Add($"{label}: PII scan must enumerate scanned paths.");
        }

        ValidateReferencedPaths(scannedPaths, label, failures);
    }

    private static void ValidateMadReview(
        string relativePath,
        string label,
        ICollection<string> failures)
    {
        string path = ContextSystemHelpers.RepoPath(relativePath.Split('/'));
        if (!File.Exists(path))
        {
            failures.Add($"{label}: MAD review does not exist at {relativePath}.");
            return;
        }

        string[] lines = File.ReadAllLines(path);
        RequireScalar(lines, "anonymized", "true", label, failures);
        RequireScalar(lines, "decision", "pass", label, failures);
        RequireScalar(
            lines,
            "identity_attribution_count",
            "0",
            label,
            failures);
        RequireScalar(
            lines,
            "unresolved_critical_count",
            "0",
            label,
            failures);

        double weight = ReadObjectSequence(lines, "proposals")
            .Select(item => item.TryGetValue("weight", out string? value)
                && double.TryParse(
                    value,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out double parsed)
                    ? parsed
                    : 0d)
            .Sum();
        if (Math.Abs(weight - 1d) > 0.0001d)
        {
            failures.Add($"{label}: anonymized MAD proposal weights must total 1.0.");
        }
    }

    private static void ValidateReferencedPaths(
        IEnumerable<string> relativePaths,
        string label,
        ICollection<string> failures)
    {
        foreach (string relativePath in relativePaths)
        {
            if (!File.Exists(ContextSystemHelpers.RepoPath(
                    relativePath.Split('/'))))
            {
                failures.Add($"{label}: referenced file does not exist: {relativePath}.");
            }
        }
    }

    private static void RequireScalar(
        string[] lines,
        string key,
        string expected,
        string label,
        ICollection<string> failures)
    {
        string actual = ReadScalar(lines, key);
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            failures.Add(
                $"{label}: '{key}' must be '{expected}', but was '{actual}'.");
        }
    }

    private static string ReadScalar(string[] lines, string key)
    {
        string prefix = key + ":";
        string? line = lines.FirstOrDefault(candidate =>
            candidate.StartsWith(prefix, StringComparison.Ordinal));
        return line is null
            ? string.Empty
            : Unquote(line[prefix.Length..].Trim());
    }

    private static string ReadNestedScalar(
        string[] lines,
        string section,
        string key)
    {
        int sectionIndex = Array.FindIndex(
            lines,
            line => line.Equals(section + ":", StringComparison.Ordinal));
        if (sectionIndex < 0)
        {
            return string.Empty;
        }

        string prefix = "  " + key + ":";
        string? line = lines
            .Skip(sectionIndex + 1)
            .TakeWhile(candidate =>
                candidate.Length == 0 || char.IsWhiteSpace(candidate[0]))
            .FirstOrDefault(candidate =>
                candidate.StartsWith(prefix, StringComparison.Ordinal));
        return line is null
            ? string.Empty
            : Unquote(line[prefix.Length..].Trim());
    }

    private static string[] ReadSequence(string[] lines, string section)
    {
        int sectionIndex = Array.FindIndex(
            lines,
            line => line.Equals(section + ":", StringComparison.Ordinal)
                || line.Equals(section + ": []", StringComparison.Ordinal));
        if (sectionIndex < 0
            || lines[sectionIndex].EndsWith("[]", StringComparison.Ordinal))
        {
            return [];
        }

        return lines
            .Skip(sectionIndex + 1)
            .TakeWhile(line =>
                line.Length == 0 || char.IsWhiteSpace(line[0]))
            .Where(line => line.StartsWith("  - ", StringComparison.Ordinal))
            .Select(line => Unquote(line[4..].Trim()))
            .ToArray();
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, string>>
        ReadObjectSequence(string[] lines, string section)
    {
        int sectionIndex = Array.FindIndex(
            lines,
            line => line.Equals(section + ":", StringComparison.Ordinal));
        if (sectionIndex < 0)
        {
            return [];
        }

        var result = new List<IReadOnlyDictionary<string, string>>();
        Dictionary<string, string>? current = null;
        foreach (string line in lines
            .Skip(sectionIndex + 1)
            .TakeWhile(candidate =>
                candidate.Length == 0 || char.IsWhiteSpace(candidate[0])))
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                if (current is not null)
                {
                    result.Add(current);
                }

                current = new Dictionary<string, string>(
                    StringComparer.Ordinal);
                AddPair(current, trimmed[2..]);
            }
            else if (current is not null && trimmed.Length != 0)
            {
                AddPair(current, trimmed);
            }
        }

        if (current is not null)
        {
            result.Add(current);
        }

        return result;
    }

    private static void AddPair(
        IDictionary<string, string> values,
        string pair)
    {
        int separator = pair.IndexOf(':');
        if (separator <= 0)
        {
            return;
        }

        values[pair[..separator].Trim()] =
            Unquote(pair[(separator + 1)..].Trim());
    }

    private static string Unquote(string value) =>
        value.Trim('"', '\'');
}
