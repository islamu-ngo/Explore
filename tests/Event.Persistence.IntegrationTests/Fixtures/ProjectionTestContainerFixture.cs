// ABOUTME: Minimal PostgreSQL container fixture for projection updater tests.
// ABOUTME: Uses EnsureCreatedAsync so tests run against the current EF model without depending on migration-file drift.

using Explore.Application.Contracts.Infrastructure;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

namespace Event.Persistence.IntegrationTests.Fixtures;

/// <summary>
/// Lightweight container fixture scoped to projection integration tests. Does not run the
/// full LookupTableSeeder, does not rely on migration files, and constructs schema directly
/// from the current model via <see cref="DatabaseFacade.EnsureCreatedAsync"/>.
/// Projection tests seed their own minimal tenant/event graph and do not need platform lookups.
/// </summary>
public class ProjectionTestContainerFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly PostgreSqlContainer _container;

    public ProjectionTestContainerFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:18-alpine")
            .WithDatabase("explore_db_projection")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateDbContextInternal();
        await context.Database.EnsureCreatedAsync();
        await SeedMinimalLookupsAsync(context);
    }

    private static async Task SeedMinimalLookupsAsync(ExploreDbContext context)
    {
        // Only the FK targets required by projection tests to build a minimal tenant/event graph.
        context.Set<ActorType>().Add(new ActorType { Id = 1, MasterCode = "USER", FullName = "User" });
        context.Set<TenantStatus>().AddRange(
            new TenantStatus { Id = 1, MasterCode = "PENDING", FullName = "Pending" },
            new TenantStatus { Id = 2, MasterCode = "ACTIVE", FullName = "Active" });
        context.Set<EventStatus>().Add(new EventStatus { Id = 1, MasterCode = "DRAFT", FullName = "Draft" });
        context.Set<EventProvenanceType>().Add(new EventProvenanceType
        {
            Id = (int)EventProvenanceTypeEnum.OrganizerCreated,
            MasterCode = "ORGANIZER_CREATED",
            FullName = "Organizer created"
        });
        context.Set<EventSessionStatus>().Add(new EventSessionStatus { Id = 1, MasterCode = "DRAFT", FullName = "Draft" });
        context.Set<EventFormat>().Add(new EventFormat { Id = 1, MasterCode = "LOCAL", FullName = "Local" });
        context.Set<VisibilityType>().Add(new VisibilityType { Id = 1, MasterCode = "PUBLIC", FullName = "Public" });
        await context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }

    public ExploreDbContext CreateDbContext()
    {
        var context = CreateDbContextInternal();
        context.EnableTenantFilterBypass("Projection integration test system context.");
        return context;
    }

    public ExploreDbContext CreateDbContext(ITenantContext tenantContext)
    {
        var context = CreateDbContextInternal();
        context.TenantContext = tenantContext;
        return context;
    }

    private ExploreDbContext CreateDbContextInternal()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new ExploreDbContext(options);
    }
}
