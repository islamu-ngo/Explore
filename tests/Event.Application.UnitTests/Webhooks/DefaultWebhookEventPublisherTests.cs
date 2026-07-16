// ABOUTME: Unit tests for immutable outgoing webhook message and delivery-plan materialization.
// ABOUTME: Covers fail-closed resolution, exact bytes, idempotent replay, conflicts, and validation.

using System.Diagnostics.Metrics;
using System.Security.Cryptography;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Exceptions;
using Explore.Application.Telemetry;
using Explore.Application.Webhooks;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Webhooks;

public sealed class DefaultWebhookEventPublisherTests
{
    private static readonly Guid MessageId = Guid.Parse("018f0000-0000-7000-8000-000000000999");
    private static readonly Guid TenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
    private static readonly Guid AggregateId = Guid.Parse("018f0000-0000-7000-8000-000000000001");
    private static readonly Guid ConsumerId = Guid.Parse("018f0000-0000-7000-8000-000000000050");
    private static readonly DateTimeOffset OccurredAt = new(2026, 7, 3, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset MaterializedAt = OccurredAt.AddMinutes(1);
    private static readonly byte[] PayloadBytes = Encoding.UTF8.GetBytes("{\"type\":\"event.published\"}");

    [Test]
    public async Task PublishAsync_WhenResolutionUnavailable_SkipsBeforeBuildingOrMaterializing()
    {
        var fixture = new Fixture();
        fixture.PlanResolver.ResolveAsync(Arg.Any<WebhookEventBuildContext>(), Arg.Any<CancellationToken>())
            .Returns(WebhookDeliveryPlanResolution.Unavailable("webhooks_disabled", "No governed plan."));
        var context = CreateContext();

        var result = await fixture.Publisher.PublishAsync(context, CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.Skipped).IsTrue();
        await Assert.That(result.MessageId).IsNull();
        await Assert.That(result.FailureCategory).IsEqualTo("webhooks_disabled");
        await fixture.PayloadBuilder.DidNotReceiveWithAnyArgs().BuildAsync(default!, default);
        await fixture.Materializer.DidNotReceiveWithAnyArgs().MaterializeAsync(default!, default);
    }

    [Test]
    public async Task PublishAsync_WhenPlanIsResolved_MaterializesExactBytesAndFrozenPlanWithoutDispatch()
    {
        var fixture = new Fixture();
        var context = CreateContext();
        WebhookDeliveryMaterialization? captured = null;
        fixture.Materializer.MaterializeAsync(
                Arg.Do<WebhookDeliveryMaterialization>(value => captured = value),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var value = call.Arg<WebhookDeliveryMaterialization>();
                return new WebhookDeliveryMaterializationResult(
                    value.Message,
                    value.DeliveryPlan,
                    Created: true);
            });

        var result = await fixture.Publisher.PublishAsync(context, CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.Skipped).IsFalse();
        await Assert.That(result.MessageId).IsEqualTo(captured!.Message.Id);
        await Assert.That(result.ProviderMessageId).IsNull();
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Message.Id.Version).IsEqualTo(7);
        await Assert.That(captured.Message.Id).IsNotEqualTo(MessageId);
        await Assert.That(captured.Message.TenantId).IsEqualTo(TenantId);
        await Assert.That(captured.Message.ConsumerId).IsEqualTo(ConsumerId);
        await Assert.That(captured.Message.EventType).IsEqualTo(WebhookEventNames.EventPublished);
        await Assert.That(captured.Message.MaterializedAt).IsEqualTo(MaterializedAt.UtcDateTime);
        await Assert.That(captured.Message.GetPayloadBytes()!).IsEquivalentTo(PayloadBytes);
        await Assert.That(captured.Message.PayloadHash).IsEqualTo(ComputePayloadHash(PayloadBytes));
        await Assert.That(captured.DeliveryPlan.WebhookMessageId).IsEqualTo(captured.Message.Id);
        await Assert.That(captured.DeliveryPlan.WebhookConsumerId).IsEqualTo(ConsumerId);
        await Assert.That(captured.DeliveryPlan.ProviderMode).IsEqualTo(WebhookProviderMode.DryRun);
        await Assert.That(captured.DeliveryPlan.ConfigurationVersion).IsEqualTo("configuration-v7");
        await Assert.That(captured.DeliveryPlan.EventContractVersion).IsEqualTo("1");
        await Assert.That(captured.DeliveryPlan.RetentionPolicy).IsEqualTo("standard");
        await Assert.That(captured.DeliveryPlan.RetentionPolicyVersion).IsEqualTo("retention-v2");
        await Assert.That(captured.LocalTargets).IsEmpty();
        await Assert.That(captured.ProviderPublications).IsEmpty();
    }

    [Test]
    public async Task PublishAsync_ForInstanceProviderTarget_KeepsSourceTenantAndConsumerApplicationIdentitySeparate()
    {
        var fixture = new Fixture();
        var instanceId = Guid.CreateVersion7();
        var profile = WebhookProviderCapabilityProfile.Create(
            WebhookProviderKind.Svix,
            "1.96.1",
            WebhookProviderCapability.EndpointManagement,
            "selfhost-v1.96.1-v1",
            MaterializedAt.AddMinutes(-2));
        var binding = WebhookConsumerProviderBinding.CreatePending(
            null,
            ConsumerId,
            instanceId,
            "self-hosted",
            profile,
            WebhookProviderCapability.EndpointManagement);
        binding.VerifyOwnership(null, ConsumerId, "app_instance_consumer", MaterializedAt.AddMinutes(-1));
        fixture.PlanResolver.ResolveAsync(
                Arg.Any<WebhookEventBuildContext>(),
                Arg.Any<CancellationToken>())
            .Returns(WebhookDeliveryPlanResolution.Success(
                ConsumerId,
                WebhookProviderMode.Svix,
                "consumer-v3:selfhost-v1.96.1-v1",
                1,
                "standard",
                "retention-v2",
                OccurredAt.AddDays(14),
                OccurredAt.AddDays(30),
                OccurredAt.AddDays(90),
                OccurredAt.AddDays(90).UtcDateTime,
                OccurredAt.AddDays(30),
                providerTargets:
                [
                    new WebhookProviderTargetResolution(
                        binding,
                        "webhook:svix:auth-token",
                        "credential-v4",
                        MaterializedAt.AddHours(1).UtcDateTime)
                ]));
        WebhookDeliveryMaterialization? captured = null;
        fixture.Materializer.MaterializeAsync(
                Arg.Do<WebhookDeliveryMaterialization>(value => captured = value),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var value = call.Arg<WebhookDeliveryMaterialization>();
                return new WebhookDeliveryMaterializationResult(
                    value.Message,
                    value.DeliveryPlan,
                    Created: true);
            });

        var result = await fixture.Publisher.PublishAsync(CreateContext(), CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(captured).IsNotNull();
        var publication = captured!.ProviderPublications.Single();
        await Assert.That(captured.Message.TenantId).IsEqualTo(TenantId);
        await Assert.That(captured.DeliveryPlan.TenantId).IsEqualTo(TenantId);
        await Assert.That(publication.TenantId).IsEqualTo(TenantId);
        await Assert.That(binding.TenantId).IsNull();
        await Assert.That(publication.ApplicationUid).IsEqualTo(
            WebhookConsumerProviderBinding.CreateApplicationUid(instanceId, ConsumerId));
        await Assert.That(publication.ProviderApplicationId).IsEqualTo("app_instance_consumer");
    }

    [Test]
    public async Task PublishAsync_WhenMaterializerReturnsExistingSemanticMessage_ReturnsIdempotentSuccess()
    {
        var fixture = new Fixture(created: false);

        var result = await fixture.Publisher.PublishAsync(CreateContext(), CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.MessageId!.Value.Version).IsEqualTo(7);
        await Assert.That(result.ProviderMessageId).IsNull();
        await fixture.Materializer.Received(1).MaterializeAsync(
            Arg.Any<WebhookDeliveryMaterialization>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishAsync_WhenImmutableIdentityConflicts_ReturnsNonRetryableConflict()
    {
        var fixture = new Fixture();
        fixture.Materializer.MaterializeAsync(
                Arg.Any<WebhookDeliveryMaterialization>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<WebhookDeliveryMaterializationResult>>(_ =>
                throw new WebhookMaterializationConflictException("Changed immutable bytes."));

        var result = await fixture.Publisher.PublishAsync(CreateContext(), CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.MessageId).IsNull();
        await Assert.That(result.IsRetryable).IsFalse();
        await Assert.That(result.FailureCategory).IsEqualTo("webhook_payload_conflict");
    }

    [Test]
    public async Task PublishAsync_WhenPayloadBuildFails_DoesNotMaterialize()
    {
        var fixture = new Fixture();
        var context = CreateContext("unknown.event");
        fixture.PayloadBuilder.BuildAsync(context, Arg.Any<CancellationToken>())
            .Returns(WebhookPayloadBuildResult.Failure("unknown_event_type"));

        var result = await fixture.Publisher.PublishAsync(context, CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.MessageId).IsNull();
        await Assert.That(result.IsRetryable).IsFalse();
        await Assert.That(result.FailureCategory).IsEqualTo("unknown_event_type");
        await fixture.Materializer.DidNotReceiveWithAnyArgs().MaterializeAsync(default!, default);
    }

    [Test]
    public async Task PublishAsync_WhenSuccessfulResolutionIsIncomplete_FailsClosedWithoutMaterializing()
    {
        var fixture = new Fixture();
        fixture.PlanResolver.ResolveAsync(Arg.Any<WebhookEventBuildContext>(), Arg.Any<CancellationToken>())
            .Returns(new WebhookDeliveryPlanResolution(
                true,
                ConsumerId,
                WebhookProviderMode.DryRun,
                null,
                1,
                "standard",
                "retention-v2",
                OccurredAt.AddDays(14),
                null,
                null,
                OccurredAt.AddDays(14).UtcDateTime,
                null,
                [],
                [],
                null,
                null));

        var result = await fixture.Publisher.PublishAsync(CreateContext(), CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.MessageId).IsNull();
        await Assert.That(result.IsRetryable).IsFalse();
        await Assert.That(result.FailureCategory).IsEqualTo("invalid_webhook_delivery_plan");
        await fixture.Materializer.DidNotReceiveWithAnyArgs().MaterializeAsync(default!, default);
    }

    [Test]
    public async Task PublishAsync_WhenPayloadSchemaDiffersFromGovernedContract_FailsClosed()
    {
        var fixture = new Fixture();
        fixture.PayloadBuilder.BuildAsync(Arg.Any<WebhookEventBuildContext>(), Arg.Any<CancellationToken>())
            .Returns(Fixture.CreatePayload(schemaVersion: 2));

        var result = await fixture.Publisher.PublishAsync(CreateContext(), CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.MessageId).IsNull();
        await Assert.That(result.FailureCategory).IsEqualTo("invalid_webhook_delivery_plan");
        await fixture.Materializer.DidNotReceiveWithAnyArgs().MaterializeAsync(default!, default);
    }

    [Test]
    public async Task PublishAsync_WhenPayloadRetentionDiffersFromGovernedPolicy_FailsClosed()
    {
        var fixture = new Fixture();
        fixture.PayloadBuilder.BuildAsync(Arg.Any<WebhookEventBuildContext>(), Arg.Any<CancellationToken>())
            .Returns(Fixture.CreatePayload(payloadRetentionUntil: OccurredAt.AddDays(1)));

        var result = await fixture.Publisher.PublishAsync(CreateContext(), CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.MessageId).IsNull();
        await Assert.That(result.FailureCategory).IsEqualTo("invalid_webhook_delivery_plan");
        await fixture.Materializer.DidNotReceiveWithAnyArgs().MaterializeAsync(default!, default);
    }

    private static WebhookEventBuildContext CreateContext(
        string eventType = WebhookEventNames.EventPublished) =>
        new(
            MessageId,
            TenantId,
            eventType,
            "domain-event-1",
            "Event",
            AggregateId,
            OccurredAt,
            new Dictionary<string, object?>
            {
                ["eventId"] = AggregateId.ToString(),
                ["status"] = "Published",
                ["publicUrl"] = "https://example.org/events/community-iftar"
            },
            ConsumerId);

    private static string ComputePayloadHash(ReadOnlySpan<byte> payloadBytes) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(payloadBytes)).ToLowerInvariant()}";

    private sealed class Fixture
    {
        public Fixture(bool created = true)
        {
            PayloadBuilder = Substitute.For<IWebhookPayloadBuilder>();
            PlanResolver = Substitute.For<IWebhookDeliveryPlanResolver>();
            Materializer = Substitute.For<IWebhookDeliveryPlanMaterializer>();
            PayloadBuilder.BuildAsync(Arg.Any<WebhookEventBuildContext>(), Arg.Any<CancellationToken>())
                .Returns(CreatePayload());
            PlanResolver.ResolveAsync(Arg.Any<WebhookEventBuildContext>(), Arg.Any<CancellationToken>())
                .Returns(CreateResolution());
            Materializer.MaterializeAsync(
                    Arg.Any<WebhookDeliveryMaterialization>(),
                    Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var value = call.Arg<WebhookDeliveryMaterialization>();
                    return new WebhookDeliveryMaterializationResult(
                        value.Message,
                        value.DeliveryPlan,
                        created);
                });

            var meterFactory = Substitute.For<IMeterFactory>();
            meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));

            Publisher = new DefaultWebhookEventPublisher(
                PayloadBuilder,
                PlanResolver,
                Materializer,
                new BusinessMetrics(meterFactory),
                new FixedTimeProvider(MaterializedAt));
        }

        public IWebhookPayloadBuilder PayloadBuilder { get; }

        public IWebhookDeliveryPlanResolver PlanResolver { get; }

        public IWebhookDeliveryPlanMaterializer Materializer { get; }

        public DefaultWebhookEventPublisher Publisher { get; }

        public static WebhookPayloadBuildResult CreatePayload(
            int schemaVersion = 1,
            DateTimeOffset? payloadRetentionUntil = null) =>
            WebhookPayloadBuildResult.Success(
                new WebhookEventEnvelope(
                    MessageId,
                    WebhookEventNames.EventPublished,
                    schemaVersion,
                    OccurredAt,
                    TenantId,
                    new Dictionary<string, object?>()),
                PayloadBytes,
                ComputePayloadHash(PayloadBytes),
                payloadRetentionUntil ?? OccurredAt.AddDays(14));

        private static WebhookDeliveryPlanResolution CreateResolution() =>
            WebhookDeliveryPlanResolution.Success(
                ConsumerId,
                WebhookProviderMode.DryRun,
                "configuration-v7",
                1,
                "standard",
                "retention-v2",
                OccurredAt.AddDays(14),
                OccurredAt.AddDays(30),
                OccurredAt.AddDays(90),
                OccurredAt.AddDays(90).UtcDateTime,
                OccurredAt.AddDays(30));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
