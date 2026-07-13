// ABOUTME: Unit tests for canonical webhook message publication and provider dispatch.
// ABOUTME: Covers idempotent creation, disabled mode, provider failures, and payload validation.

using System.Diagnostics.Metrics;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
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

    [Test]
    public async Task PublishAsync_WhenProviderDisabled_SkipsWithoutCreatingMessage()
    {
        var fixture = new Fixture("Disabled");
        var context = CreateContext();

        var result = await fixture.Publisher.PublishAsync(context, CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.Skipped).IsTrue();
        await Assert.That(result.FailureCategory).IsEqualTo("webhooks_disabled");
        await fixture.MessageRepository.DidNotReceiveWithAnyArgs().GetByTenantAndIdAsync(default, default, default);
        await fixture.MessageRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
        await fixture.DeliveryProvider.DidNotReceiveWithAnyArgs().PublishAsync(default!, default);
    }

    [Test]
    public async Task PublishAsync_WhenMessageIsNew_CreatesCanonicalMessageAndDispatchesExactBytes()
    {
        var fixture = new Fixture("Local");
        var context = CreateContext();
        WebhookMessage? createdMessage = null;
        WebhookProviderMessage? providerMessage = null;
        fixture.MessageRepository.GetByTenantAndIdAsync(TenantId, MessageId, Arg.Any<CancellationToken>())
            .Returns((WebhookMessage?)null);
        fixture.MessageRepository.CreateAsync(
                Arg.Do<WebhookMessage>(message => createdMessage = message),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<WebhookMessage>());
        fixture.DeliveryProvider.PublishAsync(
                Arg.Do<WebhookProviderMessage>(message => providerMessage = message),
                Arg.Any<CancellationToken>())
            .Returns(WebhookProviderPublishResult.Success("local-provider-message"));

        var result = await fixture.Publisher.PublishAsync(context, CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.Skipped).IsFalse();
        await Assert.That(result.MessageId).IsEqualTo(MessageId);
        await Assert.That(result.ProviderMessageId).IsEqualTo("local-provider-message");
        await Assert.That(createdMessage).IsNotNull();
        await Assert.That(createdMessage!.Id).IsEqualTo(MessageId);
        await Assert.That(createdMessage.TenantId).IsEqualTo(TenantId);
        await Assert.That(createdMessage.ConsumerId).IsEqualTo(ConsumerId);
        await Assert.That(createdMessage.EventType).IsEqualTo(WebhookEventNames.EventPublished);
        await Assert.That(Encoding.UTF8.GetString(createdMessage.GetPayloadBytes()!)).Contains("\"event.published\"");
        await Assert.That(providerMessage).IsNotNull();
        await Assert.That(providerMessage!.MessageId).IsEqualTo(MessageId);
        await Assert.That(providerMessage.ConsumerId).IsEqualTo(ConsumerId);
        await Assert.That(providerMessage.PayloadBytes).IsEquivalentTo(createdMessage.GetPayloadBytes()!);
        await Assert.That(providerMessage.PayloadHash).IsEqualTo(createdMessage.PayloadHash);
    }

    [Test]
    public async Task PublishAsync_WhenExistingMessageHasSamePayload_DoesNotRepublish()
    {
        var fixture = new Fixture("Local");
        var existing = CreateMessage();
        fixture.MessageRepository.GetByTenantAndIdAsync(TenantId, MessageId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await fixture.Publisher.PublishAsync(CreateContext(), CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.MessageId).IsEqualTo(MessageId);
        await Assert.That(result.ProviderMessageId).IsNull();
        await fixture.MessageRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
        await fixture.DeliveryProvider.DidNotReceiveWithAnyArgs().PublishAsync(default!, default);
    }

    [Test]
    public async Task PublishAsync_WhenProviderFails_ReturnsRetryableFailureWithoutMutatingMessage()
    {
        var fixture = new Fixture("Local");
        fixture.MessageRepository.GetByTenantAndIdAsync(TenantId, MessageId, Arg.Any<CancellationToken>())
            .Returns((WebhookMessage?)null);
        fixture.MessageRepository.CreateAsync(Arg.Any<WebhookMessage>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<WebhookMessage>());
        fixture.DeliveryProvider.PublishAsync(Arg.Any<WebhookProviderMessage>(), Arg.Any<CancellationToken>())
            .Returns(WebhookProviderPublishResult.Failure(
                "webhook_provider_failed",
                isRetryable: true,
                "HttpRequestException"));

        var result = await fixture.Publisher.PublishAsync(CreateContext(), CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.MessageId).IsEqualTo(MessageId);
        await Assert.That(result.IsRetryable).IsTrue();
        await Assert.That(result.FailureCategory).IsEqualTo("webhook_provider_failed");
    }

    [Test]
    public async Task PublishAsync_WhenPayloadBuildFails_DoesNotPersistOrDispatch()
    {
        var fixture = new Fixture("Local");
        var context = CreateContext("unknown.event");
        fixture.MessageRepository.GetByTenantAndIdAsync(TenantId, MessageId, Arg.Any<CancellationToken>())
            .Returns((WebhookMessage?)null);

        var result = await fixture.Publisher.PublishAsync(context, CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.IsRetryable).IsFalse();
        await Assert.That(result.FailureCategory).IsEqualTo("unknown_event_type");
        await fixture.MessageRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
        await fixture.DeliveryProvider.DidNotReceiveWithAnyArgs().PublishAsync(default!, default);
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

    private static WebhookMessage CreateMessage()
    {
        var payload = new DefaultWebhookPayloadBuilder(new WebhookEventTypeRegistry())
            .BuildAsync(CreateContext(), CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        return WebhookMessage.Create(
            MessageId,
            TenantId,
            WebhookEventNames.EventPublished,
            "domain-event-1",
            "Event",
            AggregateId,
            ConsumerId,
            payload.PayloadBytes!,
            OccurredAt.AddDays(14).UtcDateTime,
            OccurredAt.UtcDateTime);
    }

    private sealed class Fixture
    {
        public Fixture(string providerName)
        {
            MessageRepository = Substitute.For<IWebhookMessageRepository>();
            DeliveryProvider = Substitute.For<IWebhookDeliveryProvider>();
            DeliveryProvider.ProviderName.Returns(providerName);

            var meterFactory = Substitute.For<IMeterFactory>();
            meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));

            Publisher = new DefaultWebhookEventPublisher(
                new DefaultWebhookPayloadBuilder(new WebhookEventTypeRegistry()),
                MessageRepository,
                DeliveryProvider,
                new BusinessMetrics(meterFactory));
        }

        public IWebhookMessageRepository MessageRepository { get; }

        public IWebhookDeliveryProvider DeliveryProvider { get; }

        public DefaultWebhookEventPublisher Publisher { get; }
    }
}
