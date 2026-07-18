// ABOUTME: PostgreSQL-backed contract tests for the bounded ATProto event publication graph query.
// ABOUTME: Proves exact tenant selection, entity-first results, no tracking, and a fixed SQL command budget.

using System.Data.Common;
using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
public sealed class AtprotoEventPublicationRepositoryTests(PostgreSqlContainerFixture fixture)
{
    private const int MaximumPublicationQueryCount = 24;

    [Test]
    [NotInParallel("RealRuntimeDb")]
    public async Task GetAtprotoPublicationGraphAsync_ReturnsUntrackedTenantEntityGraphWithinBudget()
    {
        (Guid tenantId, Guid eventId) = await SeedEventAsync();
        var counter = new CommandCountingInterceptor();
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(counter)
            .Options;
        await using var context = new ExploreDbContext(options);

        AtprotoEventPublicationEntityGraph? graph = await new EventRepository(context)
            .GetAtprotoPublicationGraphAsync(tenantId, eventId, CancellationToken.None);

        await Assert.That(graph).IsNotNull();
        await Assert.That(graph!.Event.Id).IsEqualTo(eventId);
        await Assert.That(graph.Event.TenantId).IsEqualTo(tenantId);
        await Assert.That(context.ChangeTracker.Entries()).IsEmpty();
        await Assert.That(counter.ReaderCommandCount).IsLessThanOrEqualTo(MaximumPublicationQueryCount);
    }

    private async Task<(Guid TenantId, Guid EventId)> SeedEventAsync()
    {
        await using var context = fixture.CreateDbContext();
        var tenant = new Tenant
        {
            FullName = "ATProto projection tenant",
            Slug = $"atproto-projection-{Guid.NewGuid():N}",
            TenantStatusId = 2,
            TenantStatus = null!
        };
        var user = new User
        {
            Pii = new UserPii
            {
                Email = $"atproto-projection-{Guid.NewGuid():N}@example.test",
                FirstName = "Projection",
                LastName = "Owner"
            }
        };
        context.AddRange(tenant, user);
        await context.SaveChangesAsync();

        var actor = new Actor
        {
            Pii = new ActorPii { DisplayName = "Projection owner" },
            ActorTypeId = 1,
            ActorType = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            UserId = user.Id
        };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        var eventEntity = new Explore.Domain.Event
        {
            Id = Guid.CreateVersion7(),
            Title = "Bounded publication graph",
            ActorId = actor.Id,
            Actor = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityType = null!,
            EventStatusId = (int)EventStatusEnum.Published,
            EventStatus = null!,
            EventFormatId = (int)EventFormatEnum.Digital,
            EventFormat = null!,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();
        return (tenant.Id, eventEntity.Id);
    }

    private sealed class CommandCountingInterceptor : DbCommandInterceptor
    {
        public int ReaderCommandCount { get; private set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ReaderCommandCount++;
            return ValueTask.FromResult(result);
        }
    }
}
