// ABOUTME: Architecture guardrails for tenant query-filter and bypass conventions.
// ABOUTME: Prevents permissive null-tenant filters and unreviewed full query-filter bypasses from returning.

namespace Event.Architecture.Tests;

public class PersistenceTenantFilterArchitectureTests
{
    [Test]
    [DisplayName("Tenant query filters must fail closed when tenant context is missing")]
    public async Task TenantQueryFilters_ShouldFailClosed_WhenTenantContextIsMissing()
    {
        var queryFiltersPath = ContextSystemHelpers.RepoPath(
            "Explore.Persistence",
            "ExploreDbContext.QueryFilters.cs");
        var source = await File.ReadAllTextAsync(queryFiltersPath);

        await Assert.That(source.Contains("TenantContext == null ||", StringComparison.Ordinal)).IsFalse()
            .Because("missing TenantContext must not broaden tenant-scoped queries; use an explicit bypass reason for system/admin flows.");
    }

    [Test]
    [DisplayName("Runtime persistence code must not disable all query filters directly")]
    public async Task RuntimePersistenceCode_ShouldNotDisableAllQueryFiltersDirectly()
    {
        var persistenceRoot = ContextSystemHelpers.RepoPath("Explore.Persistence");
        var allowedRelativePaths = new HashSet<string>(StringComparer.Ordinal)
        {
            Path.Combine("Explore.Persistence", "QueryFilters", "QueryFilterExtensions.cs"),
        };

        var violations = new List<string>();
        foreach (var sourceFile in Directory.GetFiles(persistenceRoot, "*.cs", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(ContextSystemHelpers.RepoRoot, sourceFile);
            if (IsGeneratedOrNonRuntime(relativePath) || allowedRelativePaths.Contains(relativePath))
            {
                continue;
            }

            var source = await File.ReadAllTextAsync(sourceFile);
            if (source.Contains(".IgnoreQueryFilters()", StringComparison.Ordinal))
            {
                violations.Add(relativePath);
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("runtime code must use named filters or QueryFilterExtensions with an explicit bypass reason; full EF filter bypasses are reserved for seeding/tests.");
    }

    private static bool IsGeneratedOrNonRuntime(string relativePath)
    {
        return relativePath.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || relativePath.Contains($"{Path.DirectorySeparatorChar}Seed{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || relativePath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || relativePath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }
}
