// ABOUTME: Domain schema invariants for the webhook delivery redesign aggregate boundaries.
// ABOUTME: Guards identifier types, normalized state ownership, UUIDv7 factories, and invalid transitions.

using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain;
using TUnit.Core;

namespace Event.Domain.UnitTests.Entities;

public sealed class WebhookAggregateSchemaInvariantTests
{
    [Test]
    public async Task AggregateAndLookupIdentifiers_UseCanonicalTypes()
    {
        Type[] aggregateTypes =
        [
            typeof(WebhookMessage),
            typeof(WebhookDeliveryPlanSnapshot),
            typeof(WebhookLocalTargetSnapshot),
            typeof(WebhookProviderPublication),
            typeof(WebhookProviderPublicationAttempt),
            typeof(WebhookConsumerProviderBinding),
            typeof(IncomingWebhookMessage),
            typeof(IncomingWebhookEffectReceipt),
            typeof(IncomingWebhookProcessingAttempt),
            typeof(IncomingWebhookRedriveRecord)
        ];

        foreach (var aggregateType in aggregateTypes)
        {
            await Assert.That(aggregateType.GetProperty("Id")?.PropertyType)
                .IsEqualTo(typeof(Guid));
        }

        var lookupTypes = typeof(WebhookConsumer).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(WebhookConsumer).Namespace)
            .Where(type => type.Name.StartsWith("Webhook", StringComparison.Ordinal)
                || type.Name.StartsWith("IncomingWebhook", StringComparison.Ordinal))
            .Where(type => type.Name.EndsWith("Lookup", StringComparison.Ordinal))
            .ToArray();

        await Assert.That(lookupTypes.Length).IsGreaterThanOrEqualTo(10);
        foreach (var lookupType in lookupTypes)
        {
            await Assert.That(lookupType.GetProperty("Id")?.PropertyType)
                .IsEqualTo(typeof(int));
        }
    }

    [Test]
    public async Task QueryGovernedStates_AreStoredAsRequiredIntegerForeignKeys()
    {
        var expectedStateOwners = new (Type Entity, string IdProperty, string WrapperProperty)[]
        {
            (typeof(WebhookConsumer), "ConsumerKindId", "ConsumerKind"),
            (typeof(WebhookConsumer), "StatusId", "Status"),
            (typeof(WebhookConsumer), "ProviderModeId", "ProviderMode"),
            (typeof(WebhookConsumerProviderBinding), "ProviderKindId", "ProviderKind"),
            (typeof(WebhookEndpoint), "StatusId", "Status"),
            (typeof(WebhookLocalTargetSnapshot), "DeliveryStatusId", "DeliveryStatus"),
            (typeof(WebhookDeliveryAttempt), "OutcomeId", "Outcome"),
            (typeof(IncomingWebhookMessage), "StatusId", "Status"),
            (typeof(WebhookProviderPublication), "StatusId", "Status")
        };

        foreach (var expected in expectedStateOwners)
        {
            await Assert.That(expected.Entity.GetProperty(expected.IdProperty)?.PropertyType)
                .IsEqualTo(typeof(int));
            await Assert.That(expected.Entity.GetProperty(expected.WrapperProperty)?
                    .GetCustomAttributes(typeof(NotMappedAttribute), inherit: true).Length)
                .IsEqualTo(1);
        }
    }

    [Test]
    public async Task Publication_OwnsProviderStateAndReferencesImmutablePlan()
    {
        await Assert.That(typeof(WebhookProviderPublication)
                .GetProperty("WebhookDeliveryPlanSnapshotId")?.PropertyType)
            .IsEqualTo(typeof(Guid));
        await Assert.That(typeof(WebhookProviderPublication)
                .GetProperty("PublicationFence")?.PropertyType)
            .IsEqualTo(typeof(long));
        await Assert.That(typeof(WebhookProviderPublication)
                .GetProperty("ConcurrencyVersion")?.PropertyType)
            .IsEqualTo(typeof(long));

        await Assert.That(typeof(WebhookMessage).GetProperty("Status")).IsNull();
        await Assert.That(typeof(WebhookMessage).GetProperty("ProviderMessageId")).IsNull();
    }

    [Test]
    public async Task Factories_CreateVersionSevenIdentifiers_AndRejectInvalidOwnership()
    {
        var now = DateTimeOffset.UtcNow;
        var plan = WebhookDeliveryPlanSnapshot.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            WebhookProviderMode.Local,
            "configuration-v1",
            "contract-v1",
            "default",
            "retention-v1",
            now.AddDays(1),
            now);

        await Assert.That(plan.Id.Version).IsEqualTo(7);
        await Assert.ThrowsAsync<ArgumentException>(() => Task.FromResult(
            WebhookDeliveryPlanSnapshot.Create(
                Guid.Empty,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                WebhookProviderMode.Local,
                "configuration-v1",
                "contract-v1",
                "default",
                "retention-v1",
                now.AddDays(1),
                now)));
    }
}
