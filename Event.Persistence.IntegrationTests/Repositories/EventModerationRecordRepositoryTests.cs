// ABOUTME: PostgreSQL persistence tests for tenant-scoped event moderation history records.
// ABOUTME: Verifies repository ordering, tenant filters, and database uniqueness constraints.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class EventModerationRecordRepositoryTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task GetByEventAsync_ReturnsNewestRecordsFirst_ForTenantEvent()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var (_, @event, user) = await SetupEventAsync(context);
        var repository = new EventModerationRecordRepository(context);
        var first = EventModerationRecord.CreateLightModeration(
            @event.TenantId,
            @event.Id,
            user.Id,
            "policy_review",
            (int)EventStatusEnum.Published,
            "first",
            DateTimeOffset.UtcNow.AddMinutes(-5));
        var second = EventModerationRecord.CreateUnmoderation(
            first,
            user.Id,
            "review_complete",
            "second",
            DateTimeOffset.UtcNow);

        await repository.Create(first);
        await repository.Create(second);

        var results = await repository.GetByEventAsync(@event.TenantId, @event.Id, CancellationToken.None);

        await Assert.That(results.Count).IsEqualTo(2);
        await Assert.That(results[0].CorrelationId).IsEqualTo("second");
        await Assert.That(results[1].CorrelationId).IsEqualTo("first");
    }

    [Test]
    public async Task TenantFilter_HidesModerationRecordsFromOtherTenants()
    {
        await fixture.ResetAsync();
        await using var setupContext = fixture.CreateDbContext();
        var (tenant, @event, user) = await SetupEventAsync(setupContext);
        setupContext.EventModerationRecords.Add(EventModerationRecord.CreateLightModeration(
            tenant.Id,
            @event.Id,
            user.Id,
            "policy_review",
            (int)EventStatusEnum.Published,
            null,
            DateTimeOffset.UtcNow));
        await setupContext.SaveChangesAsync();

        await using var filteredContext = fixture.CreateTenantFilteredDbContext(new StaticTenantContext(Guid.NewGuid()));

        await Assert.That(await filteredContext.EventModerationRecords.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task GetByIdAsync_UsesExplicitTenantPredicate_ForOutboxWorkerLookup()
    {
        await fixture.ResetAsync();
        await using var setupContext = fixture.CreateDbContext();
        var (tenant, @event, user) = await SetupEventAsync(setupContext);
        var record = EventModerationRecord.CreateHeavyRedaction(
            tenant.Id,
            @event.Id,
            user.Id,
            "illegal_content",
            (int)EventStatusEnum.Published,
            null,
            DateTimeOffset.UtcNow);
        setupContext.EventModerationRecords.Add(record);
        await setupContext.SaveChangesAsync();

        await using var filteredContext = fixture.CreateTenantFilteredDbContext(new StaticTenantContext(Guid.NewGuid()));
        var repository = new EventModerationRecordRepository(filteredContext);

        var result = await repository.GetByIdAsync(tenant.Id, record.Id, CancellationToken.None);
        var wrongTenantResult = await repository.GetByIdAsync(Guid.NewGuid(), record.Id, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(record.Id);
        await Assert.That(result.EventId).IsEqualTo(@event.Id);
        await Assert.That(wrongTenantResult).IsNull();
    }

    [Test]
    public async Task SaveChanges_ShouldRejectDuplicateTenantCorrelationId()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var (tenant, @event, user) = await SetupEventAsync(context);
        const string correlationId = "same-correlation";
        context.EventModerationRecords.Add(EventModerationRecord.CreateLightModeration(
            tenant.Id,
            @event.Id,
            user.Id,
            "policy_review",
            (int)EventStatusEnum.Published,
            correlationId,
            DateTimeOffset.UtcNow));
        context.EventModerationRecords.Add(EventModerationRecord.CreateHeavyRedaction(
            tenant.Id,
            @event.Id,
            user.Id,
            "illegal_content",
            (int)EventStatusEnum.Published,
            correlationId,
            DateTimeOffset.UtcNow.AddMinutes(1)));

        await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    private static async Task<(Tenant tenant, Explore.Domain.Event @event, User user)> SetupEventAsync(ExploreDbContext context)
    {
        var tenant = new Tenant
        {
            FullName = "Moderation Test Tenant",
            Slug = "moderation-" + Guid.NewGuid().ToString("N")[..8],
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!
        };
        context.Tenants.Add(tenant);

        var user = new User
        {
            Pii = new UserPii
            {
                Email = $"moderator-{Guid.NewGuid():N}@example.com",
                FirstName = "Mod",
                LastName = "User"
            }
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var actor = new Actor
        {
            Pii = new ActorPii { DisplayName = "Moderation Actor" },
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            UserId = user.Id
        };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        var @event = new Explore.Domain.Event
        {
            Id = Guid.NewGuid(),
            Title = "Moderation History Event",
            ActorId = actor.Id,
            Actor = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityType = null!,
            EventStatusId = (int)EventStatusEnum.Published,
            EventStatus = null!,
            EventFormatId = (int)EventFormatEnum.Digital,
            EventFormat = null!
        };
        context.Events.Add(@event);
        await context.SaveChangesAsync();

        return (tenant, @event, user);
    }

    private sealed class StaticTenantContext(Guid tenantId) : Explore.Application.Contracts.Infrastructure.ITenantContext
    {
        public Guid TenantId { get; } = tenantId;
    }
}
