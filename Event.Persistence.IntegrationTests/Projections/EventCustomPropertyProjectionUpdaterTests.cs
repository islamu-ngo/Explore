// ABOUTME: Testcontainers integration tests for EventCustomPropertyProjectionUpdater.
// ABOUTME: Covers insert/upsert/flag-refresh/remove/refresh/rebuild against a real PostgreSQL schema.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Services;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.Projections;
using Explore.Persistence.Repositories;
using Explore.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Projections;

[ClassDataSource<ProjectionTestContainerFixture>(Shared = SharedType.PerAssembly)]
public class EventCustomPropertyProjectionUpdaterTests
{
    private readonly ProjectionTestContainerFixture _fixture;

    public EventCustomPropertyProjectionUpdaterTests(ProjectionTestContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task UpdateForValueAsync_InsertsProjectionRow_WhenRuntimeValueCreated()
    {
        using var context = _fixture.CreateDbContext();
        var updater = CreateUpdater(context);

        var scope = await SeedEventWithDefinitionAsync(context, PropertyType.Text);
        var value = new EventCustomPropertyValue
        {
            EventCustomPropertyDefinitionId = scope.DefinitionId,
            EventId = scope.EventId,
            TenantId = scope.TenantId,
            TextValue = "Hello World",
            Ordinal = 0,
        };
        context.EventCustomPropertyValues.Add(value);
        await context.SaveChangesAsync();

        await updater.UpdateForValueAsync(value.Id, CancellationToken.None);
        await context.SaveChangesAsync();

        using var verify = _fixture.CreateDbContext();
        var projection = await verify.EventCustomPropertyProjections
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.EventCustomPropertyValueId == value.Id);

        await Assert.That(projection).IsNotNull();
        await Assert.That(projection!.TextValue).IsEqualTo("Hello World");
        await Assert.That(projection.NormalizedValue).IsEqualTo("hello world");
        await Assert.That(projection.Namespace).IsEqualTo("tenant.community");
        await Assert.That(projection.IsSearchable).IsTrue();
    }

    [Test]
    public async Task UpdateForValueAsync_UpsertsSingleRow_WhenValueUpdatedTwice()
    {
        using var context = _fixture.CreateDbContext();
        var updater = CreateUpdater(context);

        var scope = await SeedEventWithDefinitionAsync(context, PropertyType.Number);
        var value = new EventCustomPropertyValue
        {
            EventCustomPropertyDefinitionId = scope.DefinitionId,
            EventId = scope.EventId,
            TenantId = scope.TenantId,
            NumberValue = 42m,
            Ordinal = 0,
        };
        context.EventCustomPropertyValues.Add(value);
        await context.SaveChangesAsync();

        await updater.UpdateForValueAsync(value.Id, CancellationToken.None);
        await context.SaveChangesAsync();

        value.NumberValue = 99m;
        await context.SaveChangesAsync();
        await updater.UpdateForValueAsync(value.Id, CancellationToken.None);
        await context.SaveChangesAsync();

        using var verify = _fixture.CreateDbContext();
        var rows = await verify.EventCustomPropertyProjections
            .AsNoTracking()
            .Where(p => p.EventCustomPropertyValueId == value.Id)
            .ToListAsync();

        await Assert.That(rows.Count).IsEqualTo(1);
        await Assert.That(rows[0].NumberValue).IsEqualTo(99m);
        await Assert.That(rows[0].NormalizedValue).IsEqualTo("99");
    }

    [Test]
    public async Task UpdateForDefinitionAsync_RefreshesExposureFlags_AcrossAllValues()
    {
        using var context = _fixture.CreateDbContext();
        var updater = CreateUpdater(context);

        var scope = await SeedEventWithDefinitionAsync(context, PropertyType.Text);
        for (var i = 0; i < 3; i++)
        {
            var value = new EventCustomPropertyValue
            {
                EventCustomPropertyDefinitionId = scope.DefinitionId,
                EventId = scope.EventId,
                TenantId = scope.TenantId,
                TextValue = $"row-{i}",
                Ordinal = i,
            };
            context.EventCustomPropertyValues.Add(value);
        }
        await context.SaveChangesAsync();

        await updater.UpdateForDefinitionAsync(scope.DefinitionId, CancellationToken.None);
        await context.SaveChangesAsync();

        var definition = await context.EventCustomPropertyDefinitions.FirstAsync(d => d.Id == scope.DefinitionId);
        definition.IsSearchable = false;
        definition.IsFilterable = false;
        definition.ExposureLevel = ExposureLevel.Internal;
        await context.SaveChangesAsync();

        await updater.UpdateForDefinitionAsync(scope.DefinitionId, CancellationToken.None);
        await context.SaveChangesAsync();

        using var verify = _fixture.CreateDbContext();
        var rows = await verify.EventCustomPropertyProjections
            .AsNoTracking()
            .Where(p => p.EventCustomPropertyDefinitionId == scope.DefinitionId)
            .ToListAsync();

        await Assert.That(rows.Count).IsEqualTo(3);
        foreach (var row in rows)
        {
            await Assert.That(row.IsSearchable).IsFalse();
            await Assert.That(row.IsFilterable).IsFalse();
            await Assert.That(row.ExposureLevel).IsEqualTo(ExposureLevel.Internal);
        }
    }

    [Test]
    public async Task RemoveForDefinitionAsync_DeletesAllProjectionRows()
    {
        using var context = _fixture.CreateDbContext();
        var updater = CreateUpdater(context);

        var scope = await SeedEventWithDefinitionAsync(context, PropertyType.Text);
        var value = new EventCustomPropertyValue
        {
            EventCustomPropertyDefinitionId = scope.DefinitionId,
            EventId = scope.EventId,
            TenantId = scope.TenantId,
            TextValue = "to be deleted",
            Ordinal = 0,
        };
        context.EventCustomPropertyValues.Add(value);
        await context.SaveChangesAsync();
        await updater.UpdateForValueAsync(value.Id, CancellationToken.None);
        await context.SaveChangesAsync();

        await updater.RemoveForDefinitionAsync(scope.DefinitionId, CancellationToken.None);

        using var verify = _fixture.CreateDbContext();
        var remaining = await verify.EventCustomPropertyProjections
            .AsNoTracking()
            .CountAsync(p => p.EventCustomPropertyDefinitionId == scope.DefinitionId);

        await Assert.That(remaining).IsEqualTo(0);
    }

    [Test]
    public async Task RefreshForEventAsync_RebuildsAllRowsForEvent()
    {
        using var context = _fixture.CreateDbContext();
        var updater = CreateUpdater(context);

        var scope = await SeedEventWithDefinitionAsync(context, PropertyType.Text);
        for (var i = 0; i < 2; i++)
        {
            var value = new EventCustomPropertyValue
            {
                EventCustomPropertyDefinitionId = scope.DefinitionId,
                EventId = scope.EventId,
                TenantId = scope.TenantId,
                TextValue = $"value-{i}",
                Ordinal = i,
            };
            context.EventCustomPropertyValues.Add(value);
        }
        await context.SaveChangesAsync();

        await updater.RefreshForEventAsync(scope.EventId, CancellationToken.None);

        using var verify = _fixture.CreateDbContext();
        var rows = await verify.EventCustomPropertyProjections
            .AsNoTracking()
            .Where(p => p.EventId == scope.EventId)
            .OrderBy(p => p.Ordinal)
            .ToListAsync();

        await Assert.That(rows.Count).IsEqualTo(2);
        await Assert.That(rows[0].NormalizedValue).IsEqualTo("value-0");
        await Assert.That(rows[1].NormalizedValue).IsEqualTo("value-1");
    }

    [Test]
    public async Task RebuildForTenantAsync_PopulatesStatusRowAndProcessesEvents()
    {
        using var context = _fixture.CreateDbContext();
        var updater = CreateUpdater(context);

        var scope = await SeedEventWithDefinitionAsync(context, PropertyType.Text);
        var value = new EventCustomPropertyValue
        {
            EventCustomPropertyDefinitionId = scope.DefinitionId,
            EventId = scope.EventId,
            TenantId = scope.TenantId,
            TextValue = "seeded",
            Ordinal = 0,
        };
        context.EventCustomPropertyValues.Add(value);
        await context.SaveChangesAsync();

        var result = await updater.RebuildForTenantAsync(scope.TenantId, CancellationToken.None);

        await Assert.That(result.LockAcquired).IsTrue();
        await Assert.That(result.RowsProcessed).IsGreaterThanOrEqualTo(1);
        await Assert.That(result.RowsFailed).IsEqualTo(0);

        using var verify = _fixture.CreateDbContext();
        var status = await verify.CustomPropertyProjectionStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == scope.TenantId);
        await Assert.That(status).IsNotNull();
        await Assert.That(status!.State).IsEqualTo(CustomPropertyProjectionState.Idle);

        var projection = await verify.EventCustomPropertyProjections
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.EventCustomPropertyValueId == value.Id);
        await Assert.That(projection).IsNotNull();
        await Assert.That(projection!.NormalizedValue).IsEqualTo("seeded");
    }

    #region Helpers

    private static EventCustomPropertyProjectionUpdater CreateUpdater(ExploreDbContext context)
    {
        var statusRepo = new CustomPropertyProjectionStatusRepository(context);
        var dirtyScopeRepo = new CustomPropertyProjectionDirtyScopeRepository(context);
        var tenantSettingRepo = new TenantSettingRepository(context);
        var systemSettingRepo = new SystemSettingRepository(context);
        var quotaResolver = new CustomPropertyQuotaResolver(tenantSettingRepo, systemSettingRepo);

        return new EventCustomPropertyProjectionUpdater(
            context,
            dirtyScopeRepo,
            statusRepo,
            quotaResolver,
            new ProjectionMetrics(new TestMeterFactory()));
    }

    private static async Task<ProjectionTestScope> SeedEventWithDefinitionAsync(
        ExploreDbContext context,
        PropertyType propertyType)
    {
        var tenant = new Tenant
        {
            FullName = "Proj Test Tenant",
            Slug = "proj-" + Guid.NewGuid().ToString("N")[..8],
            TenantStatusId = 2,
            TenantStatus = null!,
        };
        context.Tenants.Add(tenant);

        var user = new User
        {
            Pii = new UserPii
            {
                Email = $"proj-{Guid.NewGuid().ToString("N")[..8]}@example.com",
                FirstName = "Proj",
                LastName = "Tester",
            },
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var actor = new Actor
        {
            Pii = new ActorPii { DisplayName = "Proj Actor" },
            ActorTypeId = 1,
            ActorType = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            UserId = user.Id,
        };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        var @event = new Explore.Domain.Event
        {
            Id = Guid.NewGuid(),
            Title = "Proj Event",
            ActorId = actor.Id,
            Actor = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            EventStatusId = 1,
            EventStatus = null!,
            EventFormatId = 1,
            EventFormat = null!,
            VisibilityTypeId = 1,
            VisibilityType = null!,
            TotalViews = 0,
            IsRegistrationRequired = false,
            ConcurrencyStamp = Guid.NewGuid(),
        };
        context.Events.Add(@event);

        var definition = new EventCustomPropertyDefinition
        {
            EventId = @event.Id,
            TenantId = tenant.Id,
            Namespace = "tenant.community",
            Key = $"field-{Guid.NewGuid().ToString("N")[..6]}",
            DisplayName = "Field",
            PropertyType = propertyType,
            IsActive = true,
            ExposureLevel = ExposureLevel.Public,
            IsSearchable = true,
            IsFilterable = true,
            InstantiatedAt = DateTimeOffset.UtcNow,
        };
        context.EventCustomPropertyDefinitions.Add(definition);
        await context.SaveChangesAsync();

        return new ProjectionTestScope(tenant.Id, @event.Id, definition.Id);
    }

    private sealed record ProjectionTestScope(Guid TenantId, Guid EventId, Guid DefinitionId);

    #endregion
}
