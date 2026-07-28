// ABOUTME: Source-only EF metadata, runtime seed, and tenant-isolation tests for ticketing monetization persistence.
// ABOUTME: Uses design-time Npgsql metadata and InMemory repositories without Docker or Testcontainers.

using Explore.Application.Contracts.Infrastructure;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.QueryFilters;
using Explore.Persistence.Repositories;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using DomainEvent = Explore.Domain.Event;

namespace Event.Persistence.IntegrationTests.Repositories;

[Category("TicketingMonetizationPersistence")]
public sealed class TicketingMonetizationPersistenceTests
{
    [Test]
    public async Task EfModel_MapsTicketingIsolationHistoryAndMinorUnitConstraints()
    {
        await using var context = CreateModelContext();
        IModel model = context.GetService<IDesignTimeModel>().Model;

        foreach (Type type in new[] { typeof(EventTicketCatalogVersion), typeof(EventTicketType), typeof(EventCapacityPool) })
        {
            IEntityType entity = model.FindEntityType(type)!;
            await Assert.That(entity.FindDeclaredQueryFilter(QueryFilterNames.Tenant)).IsNotNull();
            await Assert.That(entity.FindDeclaredQueryFilter(QueryFilterNames.SoftDelete)).IsNotNull();
            await Assert.That(entity.FindProperty(nameof(EventTicketCatalogVersion.ConcurrencyStamp))!.IsConcurrencyToken).IsTrue();
        }

        await Assert.That(model.FindEntityType(typeof(TicketTypeEntitlement))!.FindDeclaredQueryFilter(QueryFilterNames.Tenant)).IsNotNull();
        IEntityType ticketType = model.FindEntityType(typeof(EventTicketType))!;
        await Assert.That(ticketType.FindProperty(nameof(EventTicketType.FixedPriceMinor))!.GetColumnType()).IsEqualTo("bigint");
        await Assert.That(ticketType.FindProperty(nameof(EventTicketType.MinimumPriceMinor))!.GetColumnType()).IsEqualTo("bigint");
        await Assert.That(ticketType.FindProperty(nameof(EventTicketType.SuggestedPriceMinor))!.GetColumnType()).IsEqualTo("bigint");

        IEntityType feePolicy = model.FindEntityType(typeof(PlatformFeePolicy))!;
        IEntityType eventEntity = model.FindEntityType(typeof(DomainEvent))!;
        await Assert.That(eventEntity.FindNavigation(nameof(DomainEvent.TicketCatalogVersions))!.GetFieldName()).IsEqualTo("_ticketCatalogVersions");
        await Assert.That(eventEntity.FindNavigation(nameof(DomainEvent.CapacityPools))!.GetFieldName()).IsEqualTo("_capacityPools");
        await Assert.That(model.FindEntityType(typeof(EventTicketCatalogVersion))!.GetTableName()).IsEqualTo("event_ticket_catalog_versions");
        await Assert.That(model.FindEntityType(typeof(EventTicketType))!.GetTableName()).IsEqualTo("event_ticket_types");
        await Assert.That(model.FindEntityType(typeof(TicketTypeEntitlement))!.GetTableName()).IsEqualTo("ticket_type_entitlements");
        await Assert.That(model.FindEntityType(typeof(EventCapacityPool))!.GetTableName()).IsEqualTo("event_capacity_pools");
        await Assert.That(feePolicy.FindProperty(nameof(PlatformFeePolicy.FeeBasisPoints))!.GetColumnType()).IsEqualTo("integer");
        await Assert.That(model.FindEntityType(typeof(PlatformFeeFixedCharge))!.FindProperty(nameof(PlatformFeeFixedCharge.AmountMinor))!.GetColumnType()).IsEqualTo("bigint");
        await Assert.That(model.FindEntityType(typeof(PlatformContributionOption))!.FindProperty(nameof(PlatformContributionOption.ContributionBasisPoints))!.GetColumnType()).IsEqualTo("integer");
        await Assert.That(feePolicy.GetIndexes().Any(index => index.IsUnique && index.GetFilter() == "is_active = true")).IsTrue();
        await Assert.That(model.FindEntityType(typeof(PlatformContributionSetting))!.GetIndexes().Any(index => index.IsUnique && index.GetFilter() == "is_active = true")).IsTrue();
        await Assert.That(model.FindEntityType(typeof(EventTicketCatalogVersion))!.GetIndexes().Any(index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual(["TenantId", "EventId", "VersionNumber"])
            && index.GetFilter() == "is_deleted = false")).IsTrue();
        await Assert.That(model.FindEntityType(typeof(EventTicketCatalogVersion))!.GetIndexes().Any(index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual(["TenantId", "EventId"])
            && index.GetFilter() == "ticket_catalog_status_id = 2 AND is_deleted = false")).IsTrue();
    }

    [Test]
    public async Task RuntimeSeeder_RepairsTicketingLookupsAndCreatesDisabledMonetizationDefaults()
    {
        await using var context = CreateInMemoryContext("ticketing-seeder");

        await LookupTableSeeder.SeedTicketingLookupsAsync(context, CancellationToken.None);
        await LookupTableSeeder.SeedPlatformMonetizationDefaultsAsync(context, CancellationToken.None);
        context.TicketPricingModes.Remove(await context.TicketPricingModes.SingleAsync(mode => mode.Id == (int)TicketPricingModeEnum.SlidingScale));
        await context.SaveChangesAsync();

        await LookupTableSeeder.SeedTicketingLookupsAsync(context, CancellationToken.None);
        await LookupTableSeeder.SeedPlatformMonetizationDefaultsAsync(context, CancellationToken.None);

        await Assert.That((await context.TicketCatalogStatuses.OrderBy(row => row.Id).Select(row => row.MasterCode).ToArrayAsync()).SequenceEqual(["DRAFT", "PUBLISHED", "RETIRED"])).IsTrue();
        await Assert.That((await context.TicketPricingModes.OrderBy(row => row.Id).Select(row => row.MasterCode).ToArrayAsync()).SequenceEqual(["FIXED", "FREE", "DONATION", "PAY_WHAT_YOU_CAN", "SLIDING_SCALE"])).IsTrue();
        await Assert.That((await context.ParticipantDataCollectionModes.OrderBy(row => row.Id).Select(row => row.MasterCode).ToArrayAsync()).SequenceEqual(["NONE", "LEAD_BOOKER_ONLY", "PER_TICKET_OPTIONAL", "PER_TICKET_REQUIRED", "DEFERRED_ASSIGNMENT"])).IsTrue();
        await Assert.That((await context.EntitlementScopeTypes.OrderBy(row => row.Id).Select(row => row.MasterCode).ToArrayAsync()).SequenceEqual(["EVENT", "EVENT_DAY", "EVENT_SESSION"])).IsTrue();
        await Assert.That((await context.EntitlementSelectionRules.OrderBy(row => row.Id).Select(row => row.MasterCode).ToArrayAsync()).SequenceEqual(["ALL_INCLUDED", "FIXED_SELECTION", "CHOOSE_ONE", "CHOOSE_UP_TO_N"])).IsTrue();
        await Assert.That((await context.CapacityOversellPolicies.OrderBy(row => row.Id).Select(row => row.MasterCode).ToArrayAsync()).SequenceEqual(["DISALLOW", "ALLOW"])).IsTrue();
        PlatformFeePolicy policy = await context.PlatformFeePolicies.SingleAsync();
        PlatformContributionSetting setting = await context.PlatformContributionSettings.Include(row => row.Options).SingleAsync();
        await Assert.That(policy.IsEnabled).IsFalse();
        await Assert.That(policy.FeeBasisPoints).IsEqualTo(0);
        await Assert.That(setting.IsEnabled).IsFalse();
        await Assert.That(setting.Options.OrderBy(option => option.SortOrder).Select(option => option.ContributionBasisPoints).SequenceEqual([0, 500, 1_000, 1_500, 2_000])).IsTrue();
        await Assert.That(setting.Options.Single(option => option.IsDefault).ContributionBasisPoints).IsEqualTo(0);
    }

    [Test]
    public async Task TicketRepository_RequiresMatchingTenantAndEventForTicketAndCapacityLookups()
    {
        await using var context = CreateInMemoryContext("ticketing-repository");
        Guid tenantA = Guid.CreateVersion7();
        Guid tenantB = Guid.CreateVersion7();
        Guid eventA = Guid.CreateVersion7();
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(tenantA, eventA, "USD", 1);
        EventCapacityPool pool = EventCapacityPool.Create(tenantA, eventA, "Hall", 100, 900, CapacityOversellPolicyEnum.Disallow, true);
        EventTicketType ticket = EventTicketType.Create(tenantA, catalog.Id, "General", "USD", TicketPricingModeEnum.Free, null, null, null, ParticipantDataCollectionModeEnum.None, pool.Id, null, null, false, false, null, null, null, null);
        catalog.AddTicketType(ticket, pool);

        context.EnableTenantFilterBypass("Seeds ticketing repository isolation test rows.");
        context.AddRange(catalog, pool, ticket);
        await context.SaveChangesAsync();
        context.ClearTenantFilterBypass();
        context.TenantContext = new TestTenantContext(tenantA);
        var repository = new EventTicketCatalogRepository(context);

        await Assert.That(await repository.GetTicketTypeByIdEventAndTenantAsync(ticket.Id, eventA, tenantA, CancellationToken.None)).IsNotNull();
        await Assert.That(await repository.GetTicketTypeByIdEventAndTenantAsync(ticket.Id, Guid.CreateVersion7(), tenantA, CancellationToken.None)).IsNull();
        await Assert.That(await repository.GetTicketTypeByIdEventAndTenantAsync(ticket.Id, eventA, tenantB, CancellationToken.None)).IsNull();
        await Assert.That(await repository.GetCapacityPoolByIdEventAndTenantAsync(pool.Id, eventA, tenantA, CancellationToken.None)).IsNotNull();
        await Assert.That(await repository.GetCapacityPoolByIdEventAndTenantAsync(pool.Id, eventA, tenantB, CancellationToken.None)).IsNull();
    }

    [Test]
    public async Task ManagementCatalog_TracksAndPersistsAddedTicketTypesAndEntitlements()
    {
        await using var context = CreateInMemoryContext("ticketing-management");
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(tenantId, eventId, "USD", 1);
        EventTicketType initialTicket = CreateFreeTicket(catalog, "Initial");
        catalog.AddTicketType(initialTicket, null);
        context.EnableTenantFilterBypass("Seeds tracked catalog management test rows.");
        context.Add(catalog);
        await context.SaveChangesAsync();
        context.ClearTenantFilterBypass();
        context.TenantContext = new TestTenantContext(tenantId);
        var repository = new EventTicketCatalogRepository(context);

        EventTicketCatalogVersion managed = (await repository.GetManagementCatalogAsync(eventId, tenantId, CancellationToken.None))!;
        EventTicketType addedTicket = CreateFreeTicket(managed, "Added");
        managed.AddTicketType(addedTicket, null);
        managed.AddEntitlement(addedTicket, TicketTypeEntitlement.CreateForEvent(addedTicket.Id, tenantId, eventId, 1));
        await repository.UpdateAsync(managed, CancellationToken.None);

        context.ChangeTracker.Clear();
        EventTicketCatalogVersion persisted = (await repository.GetManagementCatalogAsync(eventId, tenantId, CancellationToken.None))!;
        await Assert.That(persisted.TicketTypes.Count).IsEqualTo(2);
        await Assert.That(persisted.TicketTypes.Single(ticket => ticket.Name == "Added").Entitlements.Count).IsEqualTo(1);
    }

    [Test]
    public async Task CatalogReads_SelectLatestNonRetiredManagementGraphAndTrackStatusSpecificUpdateGraphs()
    {
        await using var context = CreateInMemoryContext("ticketing-catalog-reads");
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        EventTicketCatalogVersion draft = EventTicketCatalogVersion.Create(tenantId, eventId, "USD", 1);
        EventTicketCatalogVersion published = CreatePublishedCatalog(tenantId, eventId, 2);
        EventTicketCatalogVersion retired = CreatePublishedCatalog(tenantId, eventId, 3);
        retired.Retire();
        context.EnableTenantFilterBypass("Seeds catalog read semantics test rows.");
        context.AddRange(draft, published, retired);
        await context.SaveChangesAsync();
        context.ClearTenantFilterBypass();
        context.TenantContext = new TestTenantContext(tenantId);
        var repository = new EventTicketCatalogRepository(context);

        EventTicketCatalogVersion management = (await repository.GetManagementCatalogAsync(eventId, tenantId, CancellationToken.None))!;
        await Assert.That(management.VersionNumber).IsEqualTo(2);
        await Assert.That(context.Entry(management).State).IsEqualTo(EntityState.Unchanged);

        context.ChangeTracker.Clear();
        EventTicketCatalogVersion draftForUpdate = (await repository.GetDraftForUpdateAsync(eventId, tenantId, CancellationToken.None))!;
        await Assert.That(draftForUpdate.VersionNumber).IsEqualTo(1);
        await Assert.That(context.Entry(draftForUpdate).State).IsEqualTo(EntityState.Unchanged);

        EventTicketCatalogVersion publishedForUpdate = (await repository.GetPublishedForUpdateAsync(eventId, tenantId, CancellationToken.None))!;
        await Assert.That(publishedForUpdate.VersionNumber).IsEqualTo(2);
        await Assert.That(context.Entry(publishedForUpdate).State).IsEqualTo(EntityState.Unchanged);

        context.ChangeTracker.Clear();
        EventTicketCatalogVersion publishedRead = (await repository.GetPublishedCatalogAsync(eventId, tenantId, CancellationToken.None))!;
        await Assert.That(publishedRead.VersionNumber).IsEqualTo(2);
        await Assert.That(context.Entry(publishedRead).State).IsEqualTo(EntityState.Detached);
        await Assert.That(publishedRead.TicketTypes.Single().Entitlements.Single().TicketTypeId).IsEqualTo(publishedRead.TicketTypes.Single().Id);
    }

    [Test]
    public async Task TicketRepository_DoesNotOwnTransactionCreation()
    {
        string repositoryPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/Explore.Persistence/Repositories/EventTicketCatalogRepository.cs"));
        string source = await File.ReadAllTextAsync(repositoryPath);

        await Assert.That(source.Contains("BeginTransaction", StringComparison.Ordinal)).IsFalse();
        await Assert.That(source.Contains("PublishDraftReplacingCurrentAsync", StringComparison.Ordinal)).IsFalse();
    }

    private static EventTicketCatalogVersion CreatePublishedCatalog(Guid tenantId, Guid eventId, int versionNumber)
    {
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(tenantId, eventId, "USD", versionNumber);
        EventTicketType ticket = CreateFreeTicket(catalog, $"Ticket {versionNumber}");
        catalog.AddTicketType(ticket, null);
        catalog.AddEntitlement(ticket, TicketTypeEntitlement.CreateForEvent(ticket.Id, tenantId, eventId, 1));
        catalog.Publish();
        return catalog;
    }

    private static EventTicketType CreateFreeTicket(EventTicketCatalogVersion catalog, string name) => EventTicketType.Create(
        catalog.TenantId, catalog.Id, name, "USD", TicketPricingModeEnum.Free, null, null, null,
        ParticipantDataCollectionModeEnum.None, null, null, null, false, false, null, null, null, null);

    private static TicketingTestDbContext CreateModelContext() => new(new DbContextOptionsBuilder<ExploreDbContext>()
        .UseNpgsql("Host=localhost;Database=ticketing_model;Username=unused;Password=unused")
        .UseSnakeCaseNamingConvention().Options);

    private static TicketingTestDbContext CreateInMemoryContext(string name) => new(new DbContextOptionsBuilder<ExploreDbContext>()
        .UseInMemoryDatabase($"{name}-{Guid.NewGuid():N}").Options);

    private sealed record TestTenantContext(Guid TenantId) : ITenantContext;

    private sealed class TicketingTestDbContext(DbContextOptions<ExploreDbContext> options) : ExploreDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Actor>().Ignore(actor => actor.MergesFrom).Ignore(actor => actor.MergesInto);
            modelBuilder.Ignore<ActorMerge>();
        }
    }
}
