// ABOUTME: PostgreSQL-backed tests for sitemap event filtering in EventRepository.
// ABOUTME: Verifies sitemap URLs only include published public events from the active tenant.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Infrastructure;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.Repositories;
using TUnit.Core;
using DomainEvent = Explore.Domain.Event;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class EventSitemapRepositoryTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task GetPublishedPublicEventsForSitemap_ReturnsPublishedPublicEventsForCurrentTenantOnly()
    {
        await fixture.ResetAsync();
        var tenantA = await SeedTenantAsync("sitemap-a");
        var tenantB = await SeedTenantAsync("sitemap-b");

        var includedEvent = CreateEvent(
            tenantA,
            "Published public sitemap event",
            EventStatusEnum.Published,
            VisibilityTypeEnum.Public,
            DateTimeOffset.UtcNow.AddDays(1));

        await using (var seedContext = fixture.CreateDbContext())
        {
            seedContext.Events.Add(includedEvent);
            seedContext.Events.Add(CreateEvent(
                tenantA,
                "Draft public sitemap event",
                EventStatusEnum.Draft,
                VisibilityTypeEnum.Public,
                DateTimeOffset.UtcNow.AddDays(2)));
            seedContext.Events.Add(CreateEvent(
                tenantA,
                "Published private sitemap event",
                EventStatusEnum.Published,
                VisibilityTypeEnum.Private,
                DateTimeOffset.UtcNow.AddDays(3)));
            seedContext.Events.Add(CreateEvent(
                tenantB,
                "Other tenant published public sitemap event",
                EventStatusEnum.Published,
                VisibilityTypeEnum.Public,
                DateTimeOffset.UtcNow.AddDays(4)));
            await seedContext.SaveChangesAsync();
        }

        await using var tenantAContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantA.TenantId));
        var repository = new EventRepository(tenantAContext);

        IReadOnlyList<DomainEvent> results = await repository.GetPublishedPublicEventsForSitemap(
            maxCount: 10,
            CancellationToken.None);

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results.Single().Id).IsEqualTo(includedEvent.Id);
        await Assert.That(results.Single().TenantId).IsEqualTo(tenantA.TenantId);
        await Assert.That(results.Single().EventStatusId).IsEqualTo((int)EventStatusEnum.Published);
        await Assert.That(results.Single().VisibilityTypeId).IsEqualTo((int)VisibilityTypeEnum.Public);
    }

    private async Task<EventSitemapScope> SeedTenantAsync(string slugPrefix)
    {
        await using var context = fixture.CreateDbContext();
        var tenant = new Tenant
        {
            Id = Guid.CreateVersion7(),
            FullName = $"Sitemap {slugPrefix}",
            Slug = $"sitemap-{slugPrefix}-{Guid.NewGuid().ToString("N")[..8]}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!,
        };
        context.Tenants.Add(tenant);

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Pii = new UserPii
            {
                Email = $"sitemap-{slugPrefix}-{Guid.NewGuid():N}@example.com",
                FirstName = "Sitemap",
                LastName = "Tester",
            },
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var actor = new Actor
        {
            Id = Guid.CreateVersion7(),
            Pii = new ActorPii { DisplayName = $"Sitemap Actor {slugPrefix}" },
            ActorTypeId = 1,
            ActorType = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            UserId = user.Id,
        };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        return new EventSitemapScope(tenant.Id, actor.Id);
    }

    private static DomainEvent CreateEvent(
        EventSitemapScope scope,
        string title,
        EventStatusEnum status,
        VisibilityTypeEnum visibility,
        DateTimeOffset startsAt)
    {
        return new DomainEvent
        {
            Id = Guid.CreateVersion7(),
            Title = title,
            Subtitle = "Sitemap summary",
            Description = "Sitemap visibility fixture",
            Content = "Sitemap test content",
            FirstSessionDate = DateOnly.FromDateTime(startsAt.UtcDateTime),
            LastSessionDate = DateOnly.FromDateTime(startsAt.UtcDateTime),
            FirstSessionStartUtc = startsAt,
            LastSessionStartUtc = startsAt.AddHours(1),
            ActorId = scope.ActorId,
            Actor = null!,
            TenantId = scope.TenantId,
            Tenant = null!,
            VisibilityTypeId = (int)visibility,
            VisibilityType = null!,
            EventStatusId = (int)status,
            EventStatus = null!,
            EventFormatId = 1,
            EventFormat = null!,
            TotalViews = 0,
            IsRegistrationRequired = false,
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
    }

    private sealed record TestTenantContext(Guid TenantId) : ITenantContext;

    private sealed record EventSitemapScope(Guid TenantId, Guid ActorId);
}
