// ABOUTME: Verifies obsolete decentralization persistence is retired by a forward migration.
// ABOUTME: Guards the current EF snapshot, setting-row cleanup, and schema narrative from regression.

namespace Event.Architecture.Tests;

public sealed class LegacyDecentralizationRetirementContractTests
{
    private const string LegacyKey = "federation.decentralization_enabled";
    private const string LegacyLocalValueColumn = "tenant_delegation_decentralization_enabled_local_value";
    private const string LegacyOverrideModeColumn = "tenant_delegation_decentralization_enabled_override_mode";

    private static readonly string[] SettingTables =
    [
        "system_settings",
        "tenant_setting_overrides",
        "organization_setting_overrides",
        "group_setting_overrides",
        "user_preferences"
    ];

    [Test]
    public async Task CurrentSchema_MustRetireLegacyDecentralizationColumnsAndSettingRows()
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var migrationsDirectory = Path.Combine(repositoryRoot, "src", "Explore.Persistence", "Migrations");
        var migrationPath = Directory
            .EnumerateFiles(migrationsDirectory, "*_RetireLegacyDecentralizationSetting.cs")
            .Single();
        var migration = await File.ReadAllTextAsync(migrationPath);
        var snapshot = await File.ReadAllTextAsync(
            Path.Combine(migrationsDirectory, "ExploreDbContextModelSnapshot.cs"));
        var schema = await File.ReadAllTextAsync(
            Path.Combine(repositoryRoot, "schemas", "islamu-event.md"));

        await Assert.That(migration).Contains(LegacyLocalValueColumn);
        await Assert.That(migration).Contains(LegacyOverrideModeColumn);
        await Assert.That(migration).Contains("DropColumn");
        await Assert.That(migration).Contains("AddColumn<bool>");
        await Assert.That(migration).Contains("AddColumn<int>");
        await Assert.That(migration).Contains("defaultValue: false");
        await Assert.That(migration).Contains("defaultValue: 0");
        await Assert.That(migration).Contains(LegacyKey);

        foreach (var table in SettingTables)
        {
            await Assert.That(migration).Contains($"DELETE FROM {table}");
        }

        await Assert.That(snapshot).DoesNotContain("DecentralizationEnabled", StringComparison.Ordinal);
        await Assert.That(snapshot).DoesNotContain(LegacyLocalValueColumn, StringComparison.Ordinal);
        await Assert.That(snapshot).DoesNotContain(LegacyOverrideModeColumn, StringComparison.Ordinal);
        await Assert.That(schema).DoesNotContain(LegacyLocalValueColumn, StringComparison.Ordinal);
        await Assert.That(schema).DoesNotContain(LegacyOverrideModeColumn, StringComparison.Ordinal);
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
