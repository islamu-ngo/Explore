// ABOUTME: Generates a durable release evidence manifest from retained CI/CD artifacts.
// ABOUTME: Keeps manual GitHub Release evidence bundling in repository-owned C# tooling.
#:property RestorePackagesWithLockFile=false
#pragma warning disable CA1050 // File-based CI scripts intentionally keep helper records in the script file.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Globalization;

var artifactRoot = args.Length > 0 ? args[0] : "artifacts";
var outputDirectory = args.Length > 1 ? args[1] : "release-evidence";

if (!Directory.Exists(artifactRoot))
{
    Console.Error.WriteLine($"Artifact root does not exist: {artifactRoot}");
    return 1;
}

var artifactRootFullPath = Path.GetFullPath(artifactRoot);
var outputDirectoryFullPath = Path.GetFullPath(outputDirectory);

var sourceFiles = Directory
    .EnumerateFiles(artifactRootFullPath, "*", SearchOption.AllDirectories)
    .Select(Path.GetFullPath)
    .Where(path => !IsInsideDirectory(path, outputDirectoryFullPath))
    .Where(path => !Path.GetFileName(path).Equals(".DS_Store", StringComparison.OrdinalIgnoreCase))
    .OrderBy(path => ToManifestPath(artifactRootFullPath, path), StringComparer.Ordinal)
    .ToList();

if (sourceFiles.Count == 0)
{
    Console.Error.WriteLine($"No evidence files found under: {artifactRoot}");
    return 1;
}

Directory.CreateDirectory(outputDirectoryFullPath);

var artifacts = sourceFiles
    .Select(path => CreateEvidence(artifactRootFullPath, path))
    .ToList();

var bundle = new ReleaseEvidenceBundle(
    GeneratedAtUtc: DateTimeOffset.UtcNow,
    ArtifactRoot: artifactRoot,
    Repository: GetEnv("GITHUB_REPOSITORY"),
    Ref: GetEnv("GITHUB_REF"),
    CommitSha: GetEnv("GITHUB_SHA"),
    RunId: GetEnv("GITHUB_RUN_ID"),
    RunAttempt: GetEnv("GITHUB_RUN_ATTEMPT"),
    ReleaseVersion: GetEnv("RELEASE_VERSION"),
    ClaStatus: GetEnv("CLA_STATUS"),
    Notes: "Generated from retained CI/CD artifacts. Copy or attach this bundle to long-lived release evidence before GitHub Actions artifacts expire.",
    Artifacts: artifacts);

var jsonPath = Path.Combine(outputDirectoryFullPath, "release-evidence.json");
var markdownPath = Path.Combine(outputDirectoryFullPath, "release-evidence.md");
var releaseNotesPath = Path.Combine(outputDirectoryFullPath, "release-evidence-release-notes.md");
var checksumPath = Path.Combine(outputDirectoryFullPath, "release-evidence-checksums.sha256");

WriteJson(jsonPath, bundle);
File.WriteAllText(markdownPath, BuildMarkdown(bundle), Encoding.UTF8);
File.WriteAllText(releaseNotesPath, BuildReleaseNotesEvidence(bundle), Encoding.UTF8);
File.WriteAllLines(checksumPath, artifacts.Select(artifact => $"{artifact.Sha256}  {artifact.RelativePath}"));

Console.WriteLine($"Release evidence bundle generated from {artifacts.Count} artifact file(s).");
Console.WriteLine($"JSON: {jsonPath}");
Console.WriteLine($"Markdown: {markdownPath}");
Console.WriteLine($"Release notes evidence: {releaseNotesPath}");
Console.WriteLine($"Checksums: {checksumPath}");

return 0;

static ArtifactEvidence CreateEvidence(string artifactRootFullPath, string path)
{
    using var stream = File.OpenRead(path);
    var hash = SHA256.HashData(stream);
    var relativePath = ToManifestPath(artifactRootFullPath, path);

    return new ArtifactEvidence(
        RelativePath: relativePath,
        Category: ClassifyArtifact(relativePath),
        SizeBytes: new FileInfo(path).Length,
        Sha256: Convert.ToHexString(hash).ToLowerInvariant());
}

static string BuildMarkdown(ReleaseEvidenceBundle bundle)
{
    var builder = new StringBuilder();
    builder.AppendLine("# Release Evidence Bundle");
    builder.AppendLine();
    AppendInvariantLine(builder, $"- Generated at UTC: `{bundle.GeneratedAtUtc:O}`");
    AppendInvariantLine(builder, $"- Repository: `{ValueOrUnknown(bundle.Repository)}`");
    AppendInvariantLine(builder, $"- Ref: `{ValueOrUnknown(bundle.Ref)}`");
    AppendInvariantLine(builder, $"- Commit SHA: `{ValueOrUnknown(bundle.CommitSha)}`");
    AppendInvariantLine(builder, $"- Workflow run: `{ValueOrUnknown(bundle.RunId)}` attempt `{ValueOrUnknown(bundle.RunAttempt)}`");
    AppendInvariantLine(builder, $"- Release version: `{ValueOrUnknown(bundle.ReleaseVersion)}`");
    AppendInvariantLine(builder, $"- CLA status: `{ValueOrUnknown(bundle.ClaStatus)}`");
    builder.AppendLine();
    builder.AppendLine("## Evidence Categories");
    builder.AppendLine();
    builder.AppendLine("| Category | Files |");
    builder.AppendLine("|---|---:|");

    foreach (var group in bundle.Artifacts.GroupBy(artifact => artifact.Category).OrderBy(group => group.Key, StringComparer.Ordinal))
    {
        AppendInvariantLine(builder, $"| {group.Key} | {group.Count()} |");
    }

    builder.AppendLine();
    builder.AppendLine("## Release Checklist Mapping");
    builder.AppendLine();
    builder.AppendLine("- `container`: image digests, immutable promotion evidence, SBOM/provenance, Trivy, attestation, and checksum evidence.");
    builder.AppendLine("- `deployment`: environment, expected image tag/digest, webhook, smoke, freeze, override, and rollback evidence.");
    builder.AppendLine("- `openapi`: OpenAPI drift and advisory breaking-change evidence.");
    builder.AppendLine("- `test-results`: TRX test results and build/analyzer logs.");
    builder.AppendLine("- `dependency`: NuGet vulnerability summaries and dependency review evidence.");
    builder.AppendLine("- `workflow-security`, `secret-scanning`, and `scorecard`: supply-chain and workflow posture evidence.");
    builder.AppendLine();
    builder.AppendLine("## Artifact Manifest");
    builder.AppendLine();
    builder.AppendLine("| Category | Path | SHA-256 | Bytes |");
    builder.AppendLine("|---|---|---|---:|");

    foreach (var artifact in bundle.Artifacts.OrderBy(artifact => artifact.Category, StringComparer.Ordinal).ThenBy(artifact => artifact.RelativePath, StringComparer.Ordinal))
    {
        AppendInvariantLine(builder, $"| {artifact.Category} | `{artifact.RelativePath}` | `{artifact.Sha256}` | {artifact.SizeBytes} |");
    }

    return builder.ToString();
}

static void WriteJson(string jsonPath, ReleaseEvidenceBundle bundle)
{
    using var stream = File.Create(jsonPath);
    using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

    writer.WriteStartObject();
    writer.WriteString("generatedAtUtc", bundle.GeneratedAtUtc);
    writer.WriteString("artifactRoot", bundle.ArtifactRoot);
    writer.WriteString("repository", bundle.Repository);
    writer.WriteString("ref", bundle.Ref);
    writer.WriteString("commitSha", bundle.CommitSha);
    writer.WriteString("runId", bundle.RunId);
    writer.WriteString("runAttempt", bundle.RunAttempt);
    writer.WriteString("releaseVersion", bundle.ReleaseVersion);
    writer.WriteString("claStatus", bundle.ClaStatus);
    writer.WriteString("notes", bundle.Notes);
    writer.WritePropertyName("artifacts");
    writer.WriteStartArray();

    foreach (var artifact in bundle.Artifacts)
    {
        writer.WriteStartObject();
        writer.WriteString("relativePath", artifact.RelativePath);
        writer.WriteString("category", artifact.Category);
        writer.WriteNumber("sizeBytes", artifact.SizeBytes);
        writer.WriteString("sha256", artifact.Sha256);
        writer.WriteEndObject();
    }

    writer.WriteEndArray();
    writer.WriteEndObject();
}

static string BuildReleaseNotesEvidence(ReleaseEvidenceBundle bundle)
{
    var builder = new StringBuilder();
    builder.AppendLine("### CI/CD Release Evidence");
    builder.AppendLine();
    AppendInvariantLine(builder, $"- Release version: `{ValueOrUnknown(bundle.ReleaseVersion)}`");
    AppendInvariantLine(builder, $"- Commit SHA: `{ValueOrUnknown(bundle.CommitSha)}`");
    AppendInvariantLine(builder, $"- Source ref: `{ValueOrUnknown(bundle.Ref)}`");
    AppendInvariantLine(builder, $"- Workflow run: `{ValueOrUnknown(bundle.RunId)}` attempt `{ValueOrUnknown(bundle.RunAttempt)}`");
    AppendInvariantLine(builder, $"- CLA status: `{ValueOrUnknown(bundle.ClaStatus)}`");
    builder.AppendLine("- Attached evidence bundle files:");
    builder.AppendLine("  - `release-evidence.json`");
    builder.AppendLine("  - `release-evidence.md`");
    builder.AppendLine("  - `release-evidence-checksums.sha256`");
    builder.AppendLine("  - `release-evidence-release-notes.md`");
    builder.AppendLine();
    builder.AppendLine("Evidence categories included in the bundle:");
    builder.AppendLine();
    builder.AppendLine("| Category | Files | Required release review | ");
    builder.AppendLine("|---|---:|---|");

    foreach (var group in bundle.Artifacts.GroupBy(artifact => artifact.Category).OrderBy(group => group.Key, StringComparer.Ordinal))
    {
        AppendInvariantLine(builder, $"| {group.Key} | {group.Count()} | {ReleaseReviewHint(group.Key)} |");
    }

    builder.AppendLine();
    builder.AppendLine("The attached checksum manifest is authoritative for copied evidence files. Do not rely on expiring GitHub Actions artifact links as the only release evidence source.");

    return builder.ToString();
}

static string ReleaseReviewHint(string category)
{
    return category switch
    {
        "container" => "Image digest, SBOM/provenance, scan, attestation, promotion, and checksum evidence agree.",
        "deployment" => "Environment, expected tag/digest, webhook result, smoke result, freeze state, and rollback note are present.",
        "openapi" => "OpenAPI drift and advisory breaking-change evidence were reviewed.",
        "test-results" => "TRX and build/analyzer logs were reviewed.",
        "dependency" => "NuGet vulnerability and dependency policy evidence were reviewed.",
        "workflow-security" => "Action pin, actionlint, zizmor, cache, and deploy contract evidence were reviewed.",
        "secret-scanning" => "Secret scan SARIF/text evidence was reviewed.",
        "scorecard" => "OpenSSF Scorecard evidence was reviewed.",
        "security-tests" => "Security/Cerbos test evidence was reviewed.",
        "e2e-runtime" => "E2E runtime evidence and reliability inventory were reviewed.",
        _ => "Review and classify before publishing the release."
    };
}

static string ClassifyArtifact(string relativePath)
{
    var normalized = relativePath.ToLowerInvariant();
    var fileName = Path.GetFileName(normalized);

    if (normalized.Contains("deployment", StringComparison.Ordinal) || normalized.Contains("artifacts/deploy", StringComparison.Ordinal))
    {
        return "deployment";
    }

    if (fileName.Contains("digest", StringComparison.Ordinal)
        || fileName.Contains("promotion", StringComparison.Ordinal)
        || fileName.Contains("trivy", StringComparison.Ordinal)
        || fileName.Contains("attestation", StringComparison.Ordinal)
        || fileName.Contains("oci-", StringComparison.Ordinal)
        || fileName.Contains("checksums", StringComparison.Ordinal))
    {
        return "container";
    }

    if (normalized.Contains("openapi", StringComparison.Ordinal) || normalized.Contains("oasdiff", StringComparison.Ordinal))
    {
        return "openapi";
    }

    if (normalized.Contains("workflow-security", StringComparison.Ordinal)
        || normalized.Contains("actionlint", StringComparison.Ordinal)
        || normalized.Contains("zizmor", StringComparison.Ordinal))
    {
        return "workflow-security";
    }

    if (normalized.Contains("secret-scanning", StringComparison.Ordinal) || normalized.Contains("gitleaks", StringComparison.Ordinal))
    {
        return "secret-scanning";
    }

    if (normalized.Contains("scorecard", StringComparison.Ordinal))
    {
        return "scorecard";
    }

    if (normalized.Contains("nuget-vulnerabilit", StringComparison.Ordinal) || normalized.Contains("dependency", StringComparison.Ordinal))
    {
        return "dependency";
    }

    if (normalized.Contains("security-test", StringComparison.Ordinal) || normalized.Contains("cerbos", StringComparison.Ordinal))
    {
        return "security-tests";
    }

    if (normalized.Contains("e2e", StringComparison.Ordinal)
        || normalized.Contains("playwright", StringComparison.Ordinal)
        || normalized.Contains("screenshot", StringComparison.Ordinal)
        || normalized.Contains("video", StringComparison.Ordinal))
    {
        return "e2e-runtime";
    }

    if (fileName.EndsWith(".trx", StringComparison.Ordinal)
        || fileName.Contains("build", StringComparison.Ordinal)
        || normalized.Contains("test-results", StringComparison.Ordinal))
    {
        return "test-results";
    }

    return "uncategorized";
}

static bool IsInsideDirectory(string path, string directory)
{
    var relativePath = Path.GetRelativePath(directory, path);
    return !relativePath.StartsWith("..", StringComparison.Ordinal)
        && !Path.IsPathRooted(relativePath);
}

static string ToManifestPath(string rootFullPath, string fileFullPath)
{
    return Path.GetRelativePath(rootFullPath, fileFullPath)
        .Replace(Path.DirectorySeparatorChar, '/')
        .Replace(Path.AltDirectorySeparatorChar, '/');
}

static string GetEnv(string name)
{
    return Environment.GetEnvironmentVariable(name) ?? string.Empty;
}

static string ValueOrUnknown(string value)
{
    return string.IsNullOrWhiteSpace(value) ? "not provided" : value;
}

static void AppendInvariantLine(StringBuilder builder, FormattableString value)
{
    builder.Append(value.ToString(CultureInfo.InvariantCulture));
    builder.AppendLine();
}

public sealed record ReleaseEvidenceBundle(
    DateTimeOffset GeneratedAtUtc,
    string ArtifactRoot,
    string Repository,
    string Ref,
    string CommitSha,
    string RunId,
    string RunAttempt,
    string ReleaseVersion,
    string ClaStatus,
    string Notes,
    IReadOnlyList<ArtifactEvidence> Artifacts);

public sealed record ArtifactEvidence(
    string RelativePath,
    string Category,
    long SizeBytes,
    string Sha256);
