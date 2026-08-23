// ABOUTME: Guards the ELP-230C contraction that mediates every physical venue reference by an EventLocation.
// ABOUTME: Asserts the constraint exists in each carrier configuration and in all five provider migrations.

namespace Event.Architecture.Tests;

public sealed class EventLocationSchemaContractionTests
{
    private const string ContractionSql = "location_id IS NULL OR event_location_id IS NOT NULL";
    private const string MigrationName = "ContractEventLocationPhysicalReferences";

    /// <summary>Carrier configuration file and the constraint name it must declare.</summary>
    private static readonly (string ConfigurationFile, string ConstraintName)[] Carriers =
    [
        ("EventSessionConfiguration.cs", "CK_EventSession_PhysicalLocationRequiresEventLocation"),
        ("EventSessionGroupConfiguration.cs", "CK_EventSessionGroup_PhysicalLocationRequiresEventLocation"),
        ("EventAgendaItemConfiguration.cs", "CK_EventAgendaItem_PhysicalLocationRequiresEventLocation"),
        ("EventSessionAgendaItemConfiguration.cs", "CK_EventSessionAgendaItem_PhysicalLocationRequiresEventLocation")
    ];

    /// <summary>Every provider must ship the contraction; a missing lane silently loses the guarantee.</summary>
    private static readonly (string Project, string MigrationsFolder)[] MigrationProjects =
    [
        ("Explore.Persistence", "Migrations"),
        ("Explore.Persistence.Migrations.Sqlite", "Migrations"),
        ("Explore.Persistence.Migrations.SqlServer", "Migrations"),
        ("Explore.Persistence.Migrations.MariaDb", "Migrations"),
        ("Explore.Persistence.Migrations.MySql", "Migrations")
    ];

    [Test]
    [MethodDataSource(nameof(CarrierConfigurations))]
    public async Task EveryCarrierDeclaresThePhysicalReferenceContraction(
        string configurationFile,
        string constraintName)
    {
        string path = Path.Combine(
            ContextSystemHelpers.RepoPath("Explore.Persistence"),
            "Configurations",
            "Entities",
            configurationFile);
        string source = await File.ReadAllTextAsync(path);

        await Assert.That(source).Contains(constraintName);
        await Assert.That(source).Contains(ContractionSql);
    }

    [Test]
    [MethodDataSource(nameof(MigrationProjectNames))]
    public async Task EveryProviderShipsTheContractionMigration(string project)
    {
        string migrationsRoot = Path.Combine(ContextSystemHelpers.RepoPath(project), "Migrations");

        string[] migrations = Directory.GetFiles(migrationsRoot, $"*_{MigrationName}.cs");

        await Assert.That(migrations).HasCount(1);

        string source = await File.ReadAllTextAsync(migrations[0]);
        foreach ((_, string constraintName) in Carriers)
        {
            await Assert.That(source).Contains(constraintName);
        }

        // A contraction with no reverse is not reversible in development; the Down must drop all four.
        await Assert.That(CountOccurrences(source, "AddCheckConstraint")).IsEqualTo(4);
        await Assert.That(CountOccurrences(source, "DropCheckConstraint")).IsEqualTo(4);
    }

    [Test]
    public async Task ContractionMigrationsAreNotHandEditedAwayFromTheGeneratedShape()
    {
        foreach ((string project, string folder) in MigrationProjects)
        {
            string migrationsRoot = Path.Combine(ContextSystemHelpers.RepoPath(project), folder);
            string[] designers = Directory.GetFiles(migrationsRoot, $"*_{MigrationName}.Designer.cs");

            // The designer snapshot is what proves the migration was produced by dotnet ef, not by hand.
            await Assert.That(designers).HasCount(1);
        }
    }

    public static IEnumerable<Func<(string, string)>> CarrierConfigurations() =>
        Carriers.Select<(string ConfigurationFile, string ConstraintName), Func<(string, string)>>(
            carrier => () => (carrier.ConfigurationFile, carrier.ConstraintName));

    public static IEnumerable<Func<string>> MigrationProjectNames() =>
        MigrationProjects.Select<(string Project, string MigrationsFolder), Func<string>>(
            entry => () => entry.Project);

    private static int CountOccurrences(string source, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }
}
