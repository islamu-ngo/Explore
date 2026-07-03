// ABOUTME: Unit tests for runtime webhook provider routing and local fanout enqueue behavior.
// ABOUTME: Ensures disabled, dry-run, and local modes preserve retry-safe provider results.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Domain;
using Explore.Infrastructure.Configuration;
using Explore.Infrastructure.Webhooks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure.Webhooks;

public sealed class WebhookProviderResolverTests
{
    [Test]
    public async Task RuntimeProvider_WhenDisabled_ReturnsNonRetryableDisabledFailure()
    {
        var provider = CreateRuntimeProvider(new WebhookOptions { Enabled = false });

        var result = await provider.PublishAsync(CreateMessage(), CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.IsRetryable).IsFalse();
        await Assert.That(result.FailureCategory).IsEqualTo("webhooks_disabled");
    }

    [Test]
    public async Task RuntimeProvider_WhenDryRun_ReturnsSuccessWithoutLocalRepositoryCalls()
    {
        var endpointRepository = Substitute.For<IWebhookEndpointRepository>();
        var attemptRepository = Substitute.For<IWebhookDeliveryAttemptRepository>();
        var provider = CreateRuntimeProvider(
            new WebhookOptions { Provider = WebhookOptions.ProviderDryRun },
            endpointRepository,
            attemptRepository);

        var result = await provider.PublishAsync(CreateMessage(), CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await endpointRepository.DidNotReceiveWithAnyArgs()
            .GetActiveSubscribedEndpointsAsync(default, default!, default, default);
        await attemptRepository.DidNotReceiveWithAnyArgs()
            .CreateManyAsync(default!, default);
    }

    [Test]
    public async Task LocalProvider_WhenEndpointsSubscribed_CreatesScheduledAttempts()
    {
        var endpointRepository = Substitute.For<IWebhookEndpointRepository>();
        var attemptRepository = Substitute.For<IWebhookDeliveryAttemptRepository>();
        var message = CreateMessage();
        var endpoint = new WebhookEndpoint
        {
            Id = Guid.CreateVersion7(),
            TenantId = message.TenantId,
            ConsumerId = message.ConsumerId!.Value,
            Url = "https://example.com/webhook",
            SecretRef = "secret/webhooks/endpoint",
            SecretVersion = 1,
            Status = WebhookEndpointStatus.Active,
            MaxAttempts = 8,
            TimeoutSeconds = 15
        };

        attemptRepository.GetByMessageAsync(message.TenantId, message.MessageId, Arg.Any<CancellationToken>())
            .Returns([]);
        endpointRepository.GetActiveSubscribedEndpointsAsync(
                message.TenantId,
                message.EventType,
                WebhookProviderMode.Local,
                Arg.Any<CancellationToken>())
            .Returns([endpoint]);
        attemptRepository.CreateManyAsync(Arg.Any<IReadOnlyCollection<WebhookDeliveryAttempt>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<IReadOnlyCollection<WebhookDeliveryAttempt>>().ToList());

        var provider = new LocalWebhookDeliveryProvider(
            endpointRepository,
            attemptRepository,
            new WebhookRetryScheduler());

        var result = await provider.PublishAsync(message, CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await attemptRepository.Received(1).CreateManyAsync(
            Arg.Is<IReadOnlyCollection<WebhookDeliveryAttempt>>(attempts =>
                attempts.Count == 1
                && attempts.Single().TenantId == message.TenantId
                && attempts.Single().MessageId == message.MessageId
                && attempts.Single().EndpointId == endpoint.Id
                && attempts.Single().AttemptNumber == 1
                && attempts.Single().Status == WebhookDeliveryAttemptStatus.Scheduled),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RuntimeProvider_WhenSvixConfigured_DelegatesToSvixProvider()
    {
        var provider = CreateRuntimeProvider(new WebhookOptions { Provider = WebhookOptions.ProviderSvix });

        var result = await provider.PublishAsync(CreateMessage(), CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.ProviderMessageId).IsEqualTo("msg_svix");
    }

    private static RuntimeWebhookDeliveryProvider CreateRuntimeProvider(
        WebhookOptions options,
        IWebhookEndpointRepository? endpointRepository = null,
        IWebhookDeliveryAttemptRepository? attemptRepository = null)
    {
        endpointRepository ??= Substitute.For<IWebhookEndpointRepository>();
        attemptRepository ??= Substitute.For<IWebhookDeliveryAttemptRepository>();
        attemptRepository.GetByMessageAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);
        endpointRepository.GetActiveSubscribedEndpointsAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<WebhookProviderMode>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var local = new LocalWebhookDeliveryProvider(
            endpointRepository,
            attemptRepository,
            new WebhookRetryScheduler());
        var svixClient = Substitute.For<ISvixWebhookClient>();
        var consumerRepository = Substitute.For<IWebhookConsumerRepository>();
        var providerLinkRepository = Substitute.For<IWebhookProviderLinkRepository>();
        providerLinkRepository.GetByTenantMessageAndProviderAsync(
                Arg.Any<Guid>(),
                WebhookExternalProvider.Svix,
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns((WebhookProviderLink?)null);
        providerLinkRepository.CreateAsync(Arg.Any<WebhookProviderLink>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<WebhookProviderLink>());
        consumerRepository.GetByTenantAndIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WebhookConsumer?)null);
        svixClient.GetOrCreateApplicationAsync(Arg.Any<SvixApplicationSyncRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => new SvixApplicationSyncResult("app_svix", call.Arg<SvixApplicationSyncRequest>().AppUid));
        svixClient.CreateMessageAsync(Arg.Any<SvixMessageCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SvixMessageCreateResult("msg_svix"));
        var svix = new SvixWebhookDeliveryProvider(
            svixClient,
            consumerRepository,
            providerLinkRepository);

        return new RuntimeWebhookDeliveryProvider(
            new DisabledWebhookDeliveryProvider(),
            new DryRunWebhookDeliveryProvider(),
            local,
            svix,
            new StaticOptionsMonitor<WebhookOptions>(options),
            NullLogger<RuntimeWebhookDeliveryProvider>.Instance);
    }

    private static WebhookProviderMessage CreateMessage()
    {
        var messageId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        return new WebhookProviderMessage(
            messageId,
            tenantId,
            Guid.CreateVersion7(),
            "event.published",
            "domain-event-1",
            "Event",
            Guid.CreateVersion7(),
            "{\"id\":\"msg_1\"}",
            "hash",
            DateTimeOffset.UtcNow.AddDays(14));
    }

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public StaticOptionsMonitor(T currentValue)
        {
            CurrentValue = currentValue;
        }

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
