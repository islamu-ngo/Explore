// ABOUTME: Repository-wide ratchet proving obsolete pre-cutover identities are fully removed.
// ABOUTME: Covers runtime source, tests, generated artifacts, deployment files, documentation, and packaging.

using System.Text;

namespace Event.Architecture.Tests;

public sealed class ConfigurationManifestCutoverTests
{
    private static readonly string[] IncludedRoots =
    [
        ".agents",
        ".ci",
        "deploy",
        "docs",
        "eng",
        "islamic-value-sensitive-design",
        "schemas",
        "src",
        "tests"
    ];

    private static readonly HashSet<string> TextExtensions =
    [
        ".cs",
        ".csproj",
        ".css",
        ".env",
        ".example",
        ".html",
        ".json",
        ".md",
        ".props",
        ".razor",
        ".resx",
        ".sh",
        ".targets",
        ".toml",
        ".xml",
        ".yaml",
        ".yml"
    ];

    [Test]
    public async Task TrackedRuntimeAndDocumentationSurfacesContainNoObsoleteIdentity()
    {
        string repositoryRoot = FindRepositoryRoot();
        string[] forbidden =
        [
            "Tenant" + "ConfigurationManifest",
            "tenant-" + "configuration-manifest",
            "TENANT_" + "CONFIGURATION_MANIFEST",
            "tenant_" + "configuration_manifest",
            "tenant " + "configuration manifest",
            "tenant " + "manifest",
            "tenant-" + "manifest",
            "TENANT_" + "MANIFEST"
        ];
        var violations = new List<string>();

        foreach (string rootName in IncludedRoots)
        {
            string root = Path.Combine(repositoryRoot, rootName);
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (string path in Directory.EnumerateFiles(
                         root,
                         "*",
                         SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(repositoryRoot, path);
                if (IsBuildArtifact(relativePath))
                {
                    continue;
                }

                foreach (string token in forbidden)
                {
                    if (relativePath.Contains(token, StringComparison.OrdinalIgnoreCase))
                    {
                        violations.Add($"path:{relativePath}");
                    }
                }

                if (!TextExtensions.Contains(Path.GetExtension(path)))
                {
                    continue;
                }

                string content = await File.ReadAllTextAsync(path, Encoding.UTF8);
                foreach (string token in forbidden)
                {
                    if (content.Contains(token, StringComparison.OrdinalIgnoreCase))
                    {
                        violations.Add($"content:{relativePath}:{token}");
                    }
                }
            }
        }

        await Assert.That(violations.Distinct(StringComparer.Ordinal).Order()).IsEmpty();
    }

    private static bool IsBuildArtifact(string relativePath) =>
        relativePath.Split(Path.DirectorySeparatorChar).Any(segment =>
            segment is "bin" or "obj" or "TestResults");

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
