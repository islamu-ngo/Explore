// ABOUTME: Testcontainers integration tests for EventSessionCustomPropertyProjectionUpdater.
// ABOUTME: Mirrors event updater tests against a real PostgreSQL schema.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Services;
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
public class EventSessionCustomPropertyProjectionUpdaterTests
{
    private readonly ProjectionTestContainerFixture _fixture;

    public EventSessionCustomPropertyProjectionUpdaterTests(ProjectionTestContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task UpdateForValueAsync_InsertsProjectionRow_WhenRuntimeValueCreated()
    {
        using var context = _fixture.CreateDbContext();
        var updater = CreateUpdater(context);

        var scope = await SeedSessionWithDefinitionAsync(context, PropertyType.Text);
        var value = new EventSessionCustomPropertyValue
        {
            EventSessionCustomPropertyDefinitionId = scope.DefinitionId,
            EventSessionId = scope.EventSessionId,
            TenantId = scope.TenantId,
            TextValue = "Session Value",
            Ordinal = 0,
        };
        context.EventSessionCustomPropertyValues.Add(value);
        await context.SaveChangesAsync();

        await updater.UpdateForValueAsync(value.Id, CancellationToken.None);
        await context.SaveChangesAsync();

        using var verify = _fixture.CreateDbContext();
        var projection = await verify.EventSessionCustomPropertyProjections
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.EventSessionCustomPropertyValueId == value.Id);

        await Assert.That(projection).IsNotNull();
        await Assert.That(projection!.NormalizedValue).IsEqualTo("session value");
    }

    [Test]
    public async Task RefreshForEventSessionAsync_RebuildsAllRowsForSession()
    {
        using var context = _fixture.CreateDbContext();
        var updater = CreateUpdater(context);

        var scope = await SeedSessionWithDefinitionAsync(context, PropertyType.Text);
        for (var i = 0; i < 2; i++)
        {
            var value = new EventSessionCustomPropertyValue
            {
                EventSessionCustomPropertyDefinitionId = scope.DefinitionId,
                EventSessionId = scope.EventSessionId,
                TenantId = scope.TenantId,
                TextValue = $"session-{i}",
                Ordinal = i,
            };
            context.EventSessionCustomPropertyValues.Add(value);
        }
        await context.SaveChangesAsync();

        await updater.RefreshForEventSessionAsync(scope.EventSessionId, CancellationToken.None);

        using var verify = _fixture.CreateDbContext();
        var rows = await verify.EventSessionCustomPropertyProjections
            .AsNoTracking()
            .Where(p => p.EventSessionId == scope.EventSessionId)
            .OrderBy(p => p.Ordinal)
            .ToListAsync();

        await Assert.That(rows.Count).IsEqualTo(2);
        await Assert.That(rows[0].NormalizedValue).IsEqualTo("session-0");
        await Assert.That(rows[1].NormalizedValue).IsEqualTo("session-1");
    }

    [Test]
    public async Task RemoveForDefinitionAsync_DeletesAllProjectionRows()
    {
        using var context = _fixture.CreateDbContext();
        var updater = CreateUpdater(context);

        var scope = await SeedSessionWithDefinitionAsync(context, PropertyType.Text);
        var value = new EventSessionCustomPropertyValue
        {
            EventSessionCustomPropertyDefinitionId = scope.DefinitionId,
            EventSessionId = scope.EventSessionId,
            TenantId = scope.TenantId,
            TextValue = "remove me",
            Ordinal = 0,
        };
        context.EventSessionCustomPropertyValues.Add(value);
        await context.SaveChangesAsync();
        await updater.UpdateForValueAsync(value.Id, CancellationToken.None);
        await context.SaveChangesAsync();

        await updater.RemoveForDefinitionAsync(scope.DefinitionId, CancellationToken.None);

        using var verify = _fixture.CreateDbContext();
        var remaining = await verify.EventSessionCustomPropertyProjections
            .AsNoTracking()
            .CountAsync(p => p.EventSessionCustomPropertyDefinitionId == scope.DefinitionId);
        await Assert.That(remaining).IsEqualTo(0);
    }

    [Test]
    public async Task RebuildForTenantAsync_PopulatesStatusAndProjectsSessions()
    {
        using var context = _fixture.CreateDbContext();
        var updater = CreateUpdater(context);

        var scope = await SeedSessionWithDefinitionAsync(context, PropertyType.Number);
        var value = new EventSessionCustomPropertyValue
        {
            EventSessionCustomPropertyDefinitionId = scope.DefinitionId,
            EventSessionId = scope.EventSessionId,
            TenantId = scope.TenantId,
            NumberValue = 7m,
            Ordinal = 0,
        };
        context.EventSessionCustomPropertyValues.Add(value);
        await context.SaveChangesAsync();

        var result = await updater.RebuildForTenantAsync(scope.TenantId, CancellationToken.None);

        await Assert.That(result.LockAcquired).IsTrue();
        await Assert.That(result.RowsProcessed).IsGreaterThanOrEqualTo(1);

        using var verify = _fixture.CreateDbContext();
        var status = await verify.CustomPropertyProjectionStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.TenantId == scope.TenantId
                && s.ProjectionName == IEventSessionCustomPropertyProjectionUpdater.ProjectionName);
        await Assert.That(status).IsNotNull();
        await Assert.That(status!.State).IsEqualTo(CustomPropertyProjectionState.Idle);
    }

    #region Helpers

    private static EventSessionCustomPropertyProjectionUpdater CreateUpdater(ExploreDbContext context)
    {
        var statusRepo = new CustomPropertyProjectionStatusRepository(context);
        var dirtyScopeRepo = new CustomPropertyProjectionDirtyScopeRepository(context);
        var tenantSettingRepo = new TenantSettingRepository(context);
        var systemSettingRepo = new SystemSettingRepository(context);
        var quotaResolver = new CustomPropertyQuotaResolver(tenantSettingRepo, systemSettingRepo);

        return new EventSessionCustomPropertyProjectionUpdater(
            context,
            dirtyScopeRepo,
            statusRepo,
            quotaResolver);
    }

    private static async Task<SessionProjectionTestScope> SeedSessionWithDefinitionAsync(
        ExploreDbContext context,
        PropertyType propertyType)
    {
        var tenant = new Tenant
        {
            FullName = "Session Proj Tenant",
            Slug = "sess-proj-" + Guid.NewGuid().ToString("N")[..8],
            TenantStatusId = 2,
            TenantStatus = null!,
        };
        context.Tenants.Add(tenant);

        var user = new User
        {
            Pii = new UserPii
            {
                Email = $"sess-{Guid.NewGuid().ToString("N")[..8]}@example.com",
                FirstName = "Sess",
                LastName = "Tester",
            },
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var actor = new Actor
        {
            Pii = new ActorPii { DisplayName = "Sess Actor" },
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
            Title = "Parent Event",
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
        await context.SaveChangesAsync();

        var session = new EventSession
        {
            Id = Guid.NewGuid(),
            EventId = @event.Id,
            Event = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            Title = "Test Session",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddHours(1),
            ConcurrencyStamp = Guid.NewGuid(),
        };
        context.EventSessions.Add(session);

        var definition = new EventSessionCustomPropertyDefinition
        {
            EventSessionId = session.Id,
            TenantId = tenant.Id,
            Namespace = "tenant.community",
            Key = $"sess-field-{Guid.NewGuid().ToString("N")[..6]}",
            DisplayName = "Sess Field",
            PropertyType = propertyType,
            IsActive = true,
            ExposureLevel = ExposureLevel.Public,
            IsSearchable = true,
            IsFilterable = true,
            InstantiatedAt = DateTimeOffset.UtcNow,
        };
        context.EventSessionCustomPropertyDefinitions.Add(definition);
        await context.SaveChangesAsync();

        return new SessionProjectionTestScope(tenant.Id, session.Id, definition.Id);
    }

    private sealed record SessionProjectionTestScope(Guid TenantId, Guid EventSessionId, Guid DefinitionId);

    #endregion
}
