// ABOUTME: Integration tests for runtime database seeding against PostgreSQL.
// ABOUTME: Verifies development catalog reseeding remains idempotent across API startups.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Seed;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public class DatabaseSeederTests(PostgreSqlContainerFixture fixture)
{
    private static readonly IHostEnvironment DevelopmentEnvironment = new TestHostEnvironment();

    [Test]
    public async Task SeedAsync_InDevelopment_CanRefreshCatalogAcrossStartups()
    {
        await fixture.ResetAsync();

        await using (var context = fixture.CreateDbContext())
        {
            await DatabaseSeeder.SeedAsync(context, DevelopmentEnvironment);
        }

        await using (var context = fixture.CreateDbContext())
        {
            await DatabaseSeeder.SeedAsync(context, DevelopmentEnvironment);
        }

        await using var verifyContext = fixture.CreateDbContext();
        var visibleCatalogCount = await verifyContext.Events
            .Where(e => SeedIds.IslamicEventCatalogIds.Contains(e.Id))
            .CountAsync();
        var unfilteredCatalogCount = await verifyContext.Events
            .IgnoreQueryFilters()
            .Where(e => SeedIds.IslamicEventCatalogIds.Contains(e.Id))
            .CountAsync();
        var softDeletedCatalogCount = await verifyContext.Events
            .IgnoreQueryFilters()
            .Where(e => SeedIds.IslamicEventCatalogIds.Contains(e.Id) && e.IsDeleted)
            .CountAsync();

        await Assert.That(visibleCatalogCount).IsEqualTo(SeedIds.IslamicEventCatalogIds.Length);
        await Assert.That(unfilteredCatalogCount).IsEqualTo(SeedIds.IslamicEventCatalogIds.Length);
        await Assert.That(softDeletedCatalogCount).IsEqualTo(0);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Event.Persistence.IntegrationTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
