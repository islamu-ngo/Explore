// ABOUTME: Unit tests for Svix outgoing webhook provider publication behavior.
// ABOUTME: Covers app UID mapping, idempotency inputs, provider links, and safe failure classification.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Domain;
using Explore.Infrastructure.Webhooks;
using NSubstitute;
using Svix;

namespace Explore.Infrastructure.Tests.Infrastructure.Webhooks;

public sealed class SvixWebhookDeliveryProviderTests
{
    private static readonly Guid MessageId = Guid.Parse("018f0000-0000-7000-8000-000000000111");
    private static readonly Guid TenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
    private static readonly Guid ConsumerId = Guid.Parse("018f0000-0000-7000-8000-000000000050");
    private static readonly Guid AggregateId = Guid.Parse("018f0000-0000-7000-8000-000000000222");

    [Test]
    public async Task PublishAsync_WhenMessageAlreadyLinked_ReturnsExistingProviderMessageWithoutCallingSvix()
    {
        var fixture = new Fixture();
        fixture.ProviderLinkRepository.GetByTenantMessageAndProviderAsync(
                TenantId,
                WebhookExternalProvider.Svix,
                MessageId,
                Arg.Any<CancellationToken>())
            .Returns(new WebhookProviderLink
            {
                Id = Guid.CreateVersion7(),
                TenantId = TenantId,
                MessageId = MessageId,
                Provider = WebhookExternalProvider.Svix,
                ExternalMessageId = "msg_existing",
                SyncState = WebhookProviderLinkSyncState.Synced,
                CreatedAt = DateTime.UtcNow
            });

        var result = await fixture.Provider.PublishAsync(CreateMessage(), CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.ProviderMessageId).IsEqualTo("msg_existing");
        await fixture.SvixClient.DidNotReceiveWithAnyArgs()
            .GetOrCreateApplicationAsync(default!, default);
        await fixture.SvixClient.DidNotReceiveWithAnyArgs()
            .CreateMessageAsync(default!, default);
    }

    [Test]
    public async Task PublishAsync_WhenConsumerMessageIsNew_CreatesSvixMessageAndProviderLink()
    {
        var fixture = new Fixture();
        var message = CreateMessage();
        SvixApplicationSyncRequest? appRequest = null;
        SvixMessageCreateRequest? messageRequest = null;
        WebhookProviderLink? createdLink = null;
        var expectedAppUid = $"islamu-consumer-{ConsumerId:N}";

        fixture.ProviderLinkRepository.GetByTenantMessageAndProviderAsync(
                TenantId,
                WebhookExternalProvider.Svix,
                MessageId,
                Arg.Any<CancellationToken>())
            .Returns((WebhookProviderLink?)null);
        fixture.ConsumerRepository.GetByTenantAndIdAsync(TenantId, ConsumerId, Arg.Any<CancellationToken>())
            .Returns(new WebhookConsumer
            {
                Id = ConsumerId,
                TenantId = TenantId,
                ConsumerKind = WebhookConsumerKind.Organization,
                Name = "Community site",
                Status = WebhookConsumerStatus.Active,
                ProviderMode = WebhookProviderMode.Svix,
                CreatedAt = DateTime.UtcNow
            });
        fixture.SvixClient.GetOrCreateApplicationAsync(
                Arg.Do<SvixApplicationSyncRequest>(request => appRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new SvixApplicationSyncResult("app_123", expectedAppUid));
        fixture.SvixClient.CreateMessageAsync(
                Arg.Do<SvixMessageCreateRequest>(request => messageRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new SvixMessageCreateResult("msg_123"));
        fixture.ProviderLinkRepository.CreateAsync(
                Arg.Do<WebhookProviderLink>(link => createdLink = link),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<WebhookProviderLink>());

        var result = await fixture.Provider.PublishAsync(message, CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.ProviderMessageId).IsEqualTo("msg_123");
        await Assert.That(appRequest).IsNotNull();
        await Assert.That(appRequest!.AppUid).IsEqualTo(expectedAppUid);
        await Assert.That(appRequest.Name).IsEqualTo("Community site");
        await Assert.That(appRequest.IdempotencyKey).IsEqualTo($"svix-app:{expectedAppUid}");
        await Assert.That(appRequest.Metadata["islamu.tenant_id"]).IsEqualTo(TenantId.ToString("D"));
        await Assert.That(appRequest.Metadata["islamu.consumer_id"]).IsEqualTo(ConsumerId.ToString("D"));
        await Assert.That(appRequest.Metadata["islamu.consumer_kind"]).IsEqualTo(nameof(WebhookConsumerKind.Organization));
        await Assert.That(messageRequest).IsNotNull();
        await Assert.That(messageRequest!.AppUid).IsEqualTo(expectedAppUid);
        await Assert.That(messageRequest.EventType).IsEqualTo("event.published");
        await Assert.That(messageRequest.EventId).IsEqualTo(MessageId.ToString("D"));
        await Assert.That(messageRequest.IdempotencyKey).IsEqualTo(MessageId.ToString("D"));
        await Assert.That(System.Text.Encoding.UTF8.GetString(messageRequest.PayloadBytes)).IsEqualTo("{\"type\":\"event.published\"}");
        await Assert.That(messageRequest.PayloadRetentionDays).IsEqualTo(14);
        await Assert.That(createdLink).IsNotNull();
        await Assert.That(createdLink!.TenantId).IsEqualTo(TenantId);
        await Assert.That(createdLink.ConsumerId).IsEqualTo(ConsumerId);
        await Assert.That(createdLink.MessageId).IsEqualTo(MessageId);
        await Assert.That(createdLink.Provider).IsEqualTo(WebhookExternalProvider.Svix);
        await Assert.That(createdLink.ExternalAppId).IsNull();
        await Assert.That(createdLink.ExternalMessageId).IsEqualTo("msg_123");
        await Assert.That(createdLink.SyncState).IsEqualTo(WebhookProviderLinkSyncState.Synced);
    }

    [Test]
    public async Task PublishAsync_WhenSvixRateLimits_ReturnsRetryableProviderUnavailableFailure()
    {
        var fixture = new Fixture();
        fixture.ProviderLinkRepository.GetByTenantMessageAndProviderAsync(
                TenantId,
                WebhookExternalProvider.Svix,
                MessageId,
                Arg.Any<CancellationToken>())
            .Returns((WebhookProviderLink?)null);
        fixture.ConsumerRepository.GetByTenantAndIdAsync(TenantId, ConsumerId, Arg.Any<CancellationToken>())
            .Returns((WebhookConsumer?)null);
        fixture.SvixClient.GetOrCreateApplicationAsync(Arg.Any<SvixApplicationSyncRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => new SvixApplicationSyncResult("app_123", call.Arg<SvixApplicationSyncRequest>().AppUid));
        fixture.SvixClient.CreateMessageAsync(Arg.Any<SvixMessageCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<SvixMessageCreateResult>>(_ => throw new ApiException(429, "rate limited"));

        var result = await fixture.Provider.PublishAsync(CreateMessage(), CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.IsRetryable).IsTrue();
        await Assert.That(result.FailureCategory).IsEqualTo("svix_provider_unavailable");
        await Assert.That(result.SafeDetail).IsEqualTo("SvixApi:429");
        await fixture.ProviderLinkRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Test]
    public async Task PublishAsync_WhenSvixAuthFails_ReturnsNonRetryableAuthFailure()
    {
        var fixture = new Fixture();
        fixture.ProviderLinkRepository.GetByTenantMessageAndProviderAsync(
                TenantId,
                WebhookExternalProvider.Svix,
                MessageId,
                Arg.Any<CancellationToken>())
            .Returns((WebhookProviderLink?)null);
        fixture.ConsumerRepository.GetByTenantAndIdAsync(TenantId, ConsumerId, Arg.Any<CancellationToken>())
            .Returns((WebhookConsumer?)null);
        fixture.SvixClient.GetOrCreateApplicationAsync(Arg.Any<SvixApplicationSyncRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<SvixApplicationSyncResult>>(_ => throw new ApiException(401, "unauthorized"));

        var result = await fixture.Provider.PublishAsync(CreateMessage(), CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.IsRetryable).IsFalse();
        await Assert.That(result.FailureCategory).IsEqualTo("svix_auth_failed");
        await Assert.That(result.SafeDetail).IsEqualTo("SvixApi:401");
    }

    private static WebhookProviderMessage CreateMessage() =>
        new(
            MessageId,
            TenantId,
            ConsumerId,
            "event.published",
            "domain-event-1",
            "Event",
            AggregateId,
            "{\"type\":\"event.published\"}"u8.ToArray(),
            "hash",
            DateTimeOffset.UtcNow.AddDays(14));

    private sealed class Fixture
    {
        public Fixture()
        {
            SvixClient = Substitute.For<ISvixWebhookClient>();
            ConsumerRepository = Substitute.For<IWebhookConsumerRepository>();
            ProviderLinkRepository = Substitute.For<IWebhookProviderLinkRepository>();
            Provider = new SvixWebhookDeliveryProvider(
                SvixClient,
                ConsumerRepository,
                ProviderLinkRepository);
        }

        public ISvixWebhookClient SvixClient { get; }

        public IWebhookConsumerRepository ConsumerRepository { get; }

        public IWebhookProviderLinkRepository ProviderLinkRepository { get; }

        public SvixWebhookDeliveryProvider Provider { get; }
    }
}
