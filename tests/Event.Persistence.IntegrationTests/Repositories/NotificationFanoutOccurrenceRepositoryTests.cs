// ABOUTME: Verifies tenant-safe immutable fanout occurrence persistence and PII-free outbox pointers.
// ABOUTME: Covers wrong-tenant relationships, recipient deduplication, and transaction rollback.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models.InternalEvents;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class NotificationFanoutOccurrenceRepositoryTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task CreateAndLoad_PersistsOccurrenceAndTenantScopedPointer()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        Scenario scenario = await CreateScenarioAsync(context, "occurrence-load");
        var occurrence = CreateOccurrence(scenario.TenantId, scenario.EventId);
        OutboxMessage pointer = NotificationFanoutOccurrenceOutboxMessageFactory.Create(occurrence);
        var repository = new NotificationFanoutOccurrenceRepository(context);
        var outboxRepository = new OutboxRepository(context);
        var unitOfWork = new EfCoreUnitOfWork(context);

        await unitOfWork.ExecuteInTransactionAsync(async _ =>
        {
            await repository.Create(occurrence);
            await outboxRepository.Create(pointer);
        });

        var contract = NotificationFanoutOccurrenceOutboxMessageFactory.DeserializePointer(pointer.Payload!);
        var loaded = await repository.GetByPointerAsync(contract);
        var wrongTenant = await repository.GetByPointerAsync(contract with { TenantId = Guid.CreateVersion7() });

        await Assert.That(loaded).IsNotNull();
        await Assert.That(loaded!.SafeAfterSnapshotJson).Contains("09:00:00Z");
        await Assert.That(wrongTenant).IsNull();
        await Assert.That(pointer.Payload!).DoesNotContain("title", StringComparison.OrdinalIgnoreCase);
        await Assert.That(pointer.Payload!).DoesNotContain("location", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task Create_WithEventFromAnotherTenant_FailsCompositeForeignKey()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        Scenario tenantA = await CreateScenarioAsync(context, "occurrence-fk-a");
        Scenario tenantB = await CreateScenarioAsync(context, "occurrence-fk-b");
        var occurrence = CreateOccurrence(tenantA.TenantId, tenantB.EventId);

        context.NotificationFanoutOccurrences.Add(occurrence);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Test]
    public async Task NotificationIntent_AllowsOneRecipientPerOccurrence()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        Scenario scenario = await CreateScenarioAsync(context, "occurrence-dedup", includeRecipient: true);
        var occurrence = CreateOccurrence(scenario.TenantId, scenario.EventId);
        context.NotificationFanoutOccurrences.Add(occurrence);
        await context.SaveChangesAsync();

        context.NotificationIntents.Add(CreateIntent(scenario, occurrence.Id, "fanout:first"));
        await context.SaveChangesAsync();
        context.NotificationIntents.Add(CreateIntent(scenario, occurrence.Id, "fanout:second"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Test]
    public async Task UnitOfWork_WhenMutationFails_RollsBackOccurrenceAndPointer()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        Scenario scenario = await CreateScenarioAsync(context, "occurrence-rollback");
        var occurrence = CreateOccurrence(scenario.TenantId, scenario.EventId);
        OutboxMessage pointer = NotificationFanoutOccurrenceOutboxMessageFactory.Create(occurrence);
        var repository = new NotificationFanoutOccurrenceRepository(context);
        var outboxRepository = new OutboxRepository(context);
        var unitOfWork = new EfCoreUnitOfWork(context);

        await Assert.ThrowsAsync<InjectedMutationFailureException>(() =>
            unitOfWork.ExecuteInTransactionAsync(async _ =>
            {
                await repository.Create(occurrence);
                await outboxRepository.Create(pointer);
                throw new InjectedMutationFailureException();
            }));

        await using var verificationContext = fixture.CreateDbContext();
        bool occurrenceExists = await verificationContext.NotificationFanoutOccurrences
            .AnyAsync(row => row.Id == occurrence.Id);
        bool pointerExists = await verificationContext.OutboxMessages
            .AnyAsync(row => row.Id == pointer.Id);

        await Assert.That(occurrenceExists).IsFalse();
        await Assert.That(pointerExists).IsFalse();
    }

    private static NotificationFanoutOccurrence CreateOccurrence(Guid tenantId, Guid eventId)
    {
        DateTime occurredAt = DateTime.UtcNow;
        return NotificationFanoutOccurrence.Create(
            Guid.CreateVersion7(), tenantId, eventId, null,
            occurredAt, occurredAt, Guid.CreateVersion7(),
            "{\"fields\":[\"startTime\"]}",
            "{\"startTime\":\"2026-07-18T08:00:00Z\"}",
            "{\"startTime\":\"2026-07-18T09:00:00Z\"}",
            "event.session.updated", 1,
            (int)NotificationDeliveryPolicyEnum.CriticalEventUpdateOptional, 1,
            30, occurredAt.AddMinutes(5), "event", eventId,
            $"event:{eventId:N}:schedule", occurredAt.AddMinutes(5));
    }

    private static NotificationIntent CreateIntent(Scenario scenario, Guid occurrenceId, string deduplicationKey)
    {
        return new NotificationIntent
        {
            Id = Guid.CreateVersion7(),
            TenantId = scenario.TenantId,
            CategoryId = (int)NotificationCategoryEnum.EventLifecycle,
            OwnershipTypeId = (int)NotificationOwnershipTypeEnum.IslamuEvent,
            RecipientKindId = (int)NotificationRecipientKindEnum.User,
            StatusId = (int)NotificationIntentStatusEnum.Pending,
            TemplateKey = "event.session.updated",
            DeduplicationKey = deduplicationKey,
            RecipientUserId = scenario.RecipientUserId!.Value,
            FanoutOccurrenceId = occurrenceId,
            EventId = scenario.EventId,
            CreatedAt = DateTime.UtcNow,
        };
    }

    private static async Task<Scenario> CreateScenarioAsync(
        ExploreDbContext context,
        string slugPrefix,
        bool includeRecipient = false)
    {
        var tenant = new Tenant
        {
            Id = Guid.CreateVersion7(),
            FullName = $"Fanout {slugPrefix}",
            Slug = $"{slugPrefix}-{Guid.NewGuid():N}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!,
        };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        var actor = new Actor
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = null!,
            ActorTypeId = (int)ActorTypeEnum.Bot,
            ActorType = null!,
            Pii = new ActorPii { DisplayName = "Fanout source" },
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        var @event = new Explore.Domain.Event
        {
            Id = Guid.CreateVersion7(),
            Title = "Fanout event",
            ActorId = actor.Id,
            Actor = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityType = null!,
            EventStatusId = (int)EventStatusEnum.Published,
            EventStatus = null!,
            EventFormatId = (int)EventFormatEnum.Local,
            EventFormat = null!,
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
        context.Events.Add(@event);

        Guid? recipientUserId = null;
        if (includeRecipient)
        {
            var user = new User
            {
                Id = Guid.CreateVersion7(),
                Pii = new UserPii
                {
                    Email = $"{slugPrefix}@example.test",
                    FirstName = "Fanout",
                    LastName = "Recipient",
                },
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
            };
            context.TenantUsers.Add(new TenantUser
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenant.Id,
                Tenant = null!,
                UserId = user.Id,
                User = user,
                StatusId = (int)TenantUserStatusEnum.Active,
                JoinedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            });
            recipientUserId = user.Id;
        }

        await context.SaveChangesAsync();
        return new Scenario(tenant.Id, @event.Id, recipientUserId);
    }

    private sealed record Scenario(Guid TenantId, Guid EventId, Guid? RecipientUserId);
    private sealed class InjectedMutationFailureException : Exception;
}
