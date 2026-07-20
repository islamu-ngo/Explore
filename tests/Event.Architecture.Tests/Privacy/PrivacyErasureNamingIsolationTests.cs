// ABOUTME: Guards canonical platform-wide privacy-erasure names and configuration keys.
// ABOUTME: Allows location-specific PII lifecycle language while rejecting legacy authority/workflow surfaces.

namespace Event.Architecture.Tests.Privacy;

public sealed class PrivacyErasureNamingIsolationTests
{
    private static readonly string[] LegacyAuthorityTokens =
    [
        "LocationPrivacyAuthority",
        "LocationPrivacyErasureAuthority",
        "LocationPrivacyErasureDurability",
        "ILocationPrivacyErasureLedger",
        "ILocationPrivacyErasureReplay",
        "IGlobalLocationPrivacyErasure",
        "ApplicationDatabaseLocationPrivacyErasure",
        "GlobalLocationPrivacyErasureService",
        "LocationErasureReplayService",
        "ILocationErasureReplayService",
        "LocationPrivacyStartupGate"
    ];

    [Test]
    public async Task ProductionSource_UsesOnlyCanonicalAuthorityWorkflowAndConfigurationNames()
    {
        string repositoryRoot = FindRepositoryRoot();
        string[] sourceFiles = Directory.EnumerateFiles(
                Path.Combine(repositoryRoot, "src"),
                "*",
                SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal)
                || path.EndsWith(".json", StringComparison.Ordinal))
            .Where(IsOwnedSource)
            .Where(path => !path.Contains(
                $"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .ToArray();
        var matches = new List<string>();

        foreach (string sourceFile in sourceFiles)
        {
            string source = await File.ReadAllTextAsync(sourceFile);
            foreach (string token in LegacyAuthorityTokens.Where(source.Contains))
            {
                matches.Add($"{Path.GetRelativePath(repositoryRoot, sourceFile)}:{token}");
            }
        }

        await Assert.That(matches).IsEmpty();
    }

    [Test]
    public async Task CanonicalConfigurationKeys_ArePresentAndLegacyKeysAreAbsent()
    {
        string repositoryRoot = FindRepositoryRoot();
        string source = string.Join(
            '\n',
            Directory.EnumerateFiles(
                    Path.Combine(repositoryRoot, "src"),
                    "*",
                    SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".cs", StringComparison.Ordinal)
                    || path.EndsWith(".json", StringComparison.Ordinal))
                .Where(IsOwnedSource)
                .Where(path => !path.Contains(
                    $"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal))
                .Select(File.ReadAllText));

        await Assert.That(source.Contains("PrivacyErasure:Durability", StringComparison.Ordinal))
            .IsTrue();
        await Assert.That(source.Contains("PrivacyErasureAuthority", StringComparison.Ordinal))
            .IsTrue();
        await Assert.That(source.Contains("LocationPrivacy:ErasureDurability", StringComparison.Ordinal))
            .IsFalse();
        await Assert.That(source.Contains("ConnectionStrings:LocationPrivacyAuthority", StringComparison.Ordinal))
            .IsFalse();
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Repository root was not found.");
    }

    private static bool IsOwnedSource(string path) =>
        !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
}
