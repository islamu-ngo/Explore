// ABOUTME: Verifies atomic location-governance correction, outbox creation, and cache invalidation.
// ABOUTME: Covers controlled server time, tenant widening rejection, and PII-free correction payloads.

using System.Reflection;
using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Settings;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

public sealed class LocationPrivacyGovernanceMutationServiceTests
{
    [Test]
    public async Task InstanceTightening_InsideAmbientTransaction_DoesNotEvictBeforeCommit()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid actorId = Guid.CreateVersion7();
        DateTimeOffset now = new(2026, 7, 19, 14, 30, 0, TimeSpan.Zero);
        EventLocation placement = CreateExactPlacement(tenantId, eventId, actorId, now.UtcDateTime.AddDays(-2));
        var systemSettings = Substitute.For<ISystemSettingRepository>();
        systemSettings.GetByKey(
                GovernanceSettingKeys.LocationPrivacy.AllowPublicExactAddress,
                Arg.Any<CancellationToken>())
            .Returns(System(GovernanceSettingKeys.LocationPrivacy.AllowPublicExactAddress, "true"));
        var tenantSettings = Substitute.For<ITenantSettingRepository>();
        tenantSettings.GetByKeyAcrossTenants(
                GovernanceSettingKeys.LocationPrivacy.AllowPublicExactAddress,
                Arg.Any<CancellationToken>())
            .Returns([]);
        var eventLocations = Substitute.For<IEventLocationRepository>();
        eventLocations.GetActiveForGovernanceUpdateAsync(null, Arg.Any<CancellationToken>())
            .Returns([placement]);
        IReadOnlyCollection<EventLocationDisclosureAudit> savedAudits = [];
        IReadOnlyCollection<OutboxMessage> savedMessages = [];
        eventLocations.SaveGovernanceChangesAsync(
                Arg.Any<IReadOnlyCollection<EventLocationDisclosureAudit>>(),
                Arg.Any<IReadOnlyCollection<OutboxMessage>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                savedAudits = callInfo.ArgAt<IReadOnlyCollection<EventLocationDisclosureAudit>>(0);
                savedMessages = callInfo.ArgAt<IReadOnlyCollection<OutboxMessage>>(1);
                return Task.CompletedTask;
            });
        var mutationLock = new RecordingMutationLock();
        HybridCache cache = Substitute.For<HybridCache>();
        cache.RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                if (!mutationLock.Completed)
                {
                    throw new InvalidOperationException("Cache invalidation ran before the transaction completed.");
                }

                return ValueTask.CompletedTask;
            });
        var service = new LocationPrivacyGovernanceMutationService(
            systemSettings,
            tenantSettings,
            eventLocations,
            mutationLock,
            cache,
            new FixedTimeProvider(now));

        var result = await service.ExecuteAsync(
            GovernanceSettingKeys.LocationPrivacy.AllowPublicExactAddress,
            "false",
            SettingScope.Instance,
            tenantId: null,
            actorId,
            _ => Task.FromResult<string?>("true"));

        await Assert.That(result.Accepted).IsTrue();
        await Assert.That(placement.PolicyVersion).IsEqualTo(3);
        await Assert.That(placement.NeedsPrivacyReview).IsTrue();
        await Assert.That(placement.LastPolicyChangedAtUtc).IsEqualTo(now.UtcDateTime);
        await Assert.That(savedAudits).HasSingleItem();
        await Assert.That(savedAudits.Single().Reason)
            .IsEqualTo(EventLocationDisclosureAuditReasonEnum.GovernanceTightening);
        await Assert.That(savedAudits.Single().OccurredAtUtc).IsEqualTo(now.UtcDateTime);
        await Assert.That(savedMessages).HasSingleItem();
        await Assert.That(savedMessages.Single().EventType).IsEqualTo("location.privacy.corrected");
        await Assert.That(savedMessages.Single().CreatedAt).IsEqualTo(now.UtcDateTime);
        await Assert.That(savedMessages.Single().Payload).DoesNotContain("PRIVATE CANARY");
        await Assert.That(savedMessages.Single().Payload).DoesNotContain("Reason");
        await Assert.That(savedMessages.Single().Payload).DoesNotContain("governance_tightening");
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        await service.InvalidateMutationAsync(
            SettingScope.Instance,
            tenantId: null,
            result.CorrectedProjections,
            CancellationToken.None);

        await cache.Received(1).RemoveByTagAsync(CacheTags.EventLocations, Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(CacheTags.Events, Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventLists, Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventDetails, Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventLocationsByTenant(tenantId), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(CacheTags.Event(eventId), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventLocationsByEvent(eventId), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventLocation(placement.Id), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TenantWidening_IsRejectedWithoutPersistenceOrCacheInvalidation()
    {
        var systemSettings = Substitute.For<ISystemSettingRepository>();
        systemSettings.GetByKey(
                GovernanceSettingKeys.LocationPrivacy.MinimumHomeAudience,
                Arg.Any<CancellationToken>())
            .Returns(System(
                GovernanceSettingKeys.LocationPrivacy.MinimumHomeAudience,
                "\"CONFIRMED_PARTICIPANT\""));
        var tenantSettings = Substitute.For<ITenantSettingRepository>();
        tenantSettings.GetByTenantAndKeys(
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        HybridCache cache = Substitute.For<HybridCache>();
        var service = new LocationPrivacyGovernanceMutationService(
            systemSettings,
            tenantSettings,
            Substitute.For<IEventLocationRepository>(),
            new RecordingMutationLock(),
            cache,
            TimeProvider.System);
        bool persisted = false;

        var result = await service.ExecuteAsync(
            GovernanceSettingKeys.LocationPrivacy.MinimumHomeAudience,
            "\"ANY_CURRENT_REGISTRANT\"",
            SettingScope.Tenant,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            _ =>
            {
                persisted = true;
                return Task.FromResult<string?>(null);
            });

        await Assert.That(result.Accepted).IsFalse();
        await Assert.That(result.Error).Contains("would widen");
        await Assert.That(persisted).IsFalse();
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static EventLocation CreateExactPlacement(
        Guid tenantId,
        Guid eventId,
        Guid actorId,
        DateTime createdAtUtc)
    {
        var location = new Location
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Tenant = null!,
            FullName = "PRIVATE CANARY",
            Country = "BE",
            City = "Brussels"
        };
        location.ClassifyAs(LocationKindEnum.CommunityVenue);
        EventLocation placement = EventLocation.CreatePhysical(
            tenantId,
            eventId,
            location.Id,
            actorId,
            createdAtUtc);
        placement.ChangeDisclosurePolicy(
            EventLocationDisclosureFields.StreetAddress | EventLocationDisclosureFields.Postcode,
            LocationDisclosureAudienceEnum.AnyCurrentRegistrant,
            null,
            placement.PolicyVersion,
            actorId,
            EventLocationDisclosureAuditReasonEnum.OrganizerPolicyChange,
            createdAtUtc.AddMinutes(1));
        typeof(EventLocation).GetProperty(
                nameof(EventLocation.Location),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(placement, location);
        return placement;
    }

    private static SystemSetting System(string key, string value) => new()
    {
        Id = Guid.CreateVersion7(),
        SettingKey = key,
        Value = value
    };

    private sealed class RecordingMutationLock : ISettingMutationLock
    {
        public bool Completed { get; private set; }

        public async Task<T> ExecuteAsync<T>(
            string canonicalSettingKey,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            T result = await operation(cancellationToken);
            Completed = true;
            return result;
        }

        public Task<T> ExecuteManyAsync<T>(
            IEnumerable<string> canonicalSettingKeys,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) =>
            ExecuteAsync(string.Join('|', canonicalSettingKeys), operation, cancellationToken);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
