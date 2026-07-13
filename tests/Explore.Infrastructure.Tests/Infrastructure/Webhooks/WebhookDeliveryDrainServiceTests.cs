// ABOUTME: Unit tests for LocalProvider webhook delivery attempt drainage.
// ABOUTME: Verifies signed HTTP delivery, retries, SSRF blocking, and stale lease recovery transitions.

using System.Diagnostics.Metrics;
using System.Net;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Infrastructure.Configuration;
using Explore.Infrastructure.Webhooks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure.Webhooks;

public sealed class WebhookDeliveryDrainServiceTests
{
    [Test]
    public async Task ProcessBatchAsync_WhenEndpointReturnsSuccess_MarksAttemptSucceededAndSendsSignedPayload()
    {
        var handler = new RecordingMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var fixture = new Fixture(handler);
        var attempt = CreateAttempt();
        fixture.ConfigureClaim(attempt);

        WebhookDeliveryDrainResult result = await fixture.Service.ProcessBatchAsync(CancellationToken.None);

        await Assert.That(result.SucceededCount).IsEqualTo(1);
        await Assert.That(handler.CallCount).IsEqualTo(1);
        await Assert.That(handler.Body).IsEqualTo(Encoding.UTF8.GetString(attempt.Message!.GetPayloadBytes()!));
        await Assert.That(handler.Headers.ContainsKey("svix-id")).IsTrue();
        await Assert.That(handler.Headers.ContainsKey("svix-timestamp")).IsTrue();
        await Assert.That(handler.Headers.ContainsKey("svix-signature")).IsTrue();
        await fixture.AttemptRepository.Received(1).MarkSucceededAsync(
            attempt.TenantId,
            attempt.Id,
            Arg.Any<Guid>(),
            Arg.Any<long>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            (int)HttpStatusCode.NoContent,
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
        await fixture.EndpointRepository.Received(1).MarkSuccessAsync(
            attempt.TenantId,
            attempt.EndpointId,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await fixture.AttemptRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Test]
    public async Task ProcessBatchAsync_WhenEndpointReturnsServerError_SchedulesRetry()
    {
        var handler = new RecordingMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("provider unavailable", Encoding.UTF8, "text/plain")
        });
        var fixture = new Fixture(handler);
        var attempt = CreateAttempt();
        fixture.ConfigureClaim(attempt);

        WebhookDeliveryDrainResult result = await fixture.Service.ProcessBatchAsync(CancellationToken.None);

        await Assert.That(result.RetryScheduledCount).IsEqualTo(1);
        await fixture.AttemptRepository.Received(1).MarkFailedAsync(
            attempt.TenantId,
            attempt.Id,
            Arg.Any<Guid>(),
            Arg.Any<long>(),
            WebhookDeliveryAttemptOutcome.Failed,
            Arg.Any<DateTime>(),
            "http_non_success",
            (int)HttpStatusCode.InternalServerError,
            Arg.Any<int>(),
            Arg.Is<DateTime?>(value => value != null),
            Arg.Any<CancellationToken>());
        await fixture.AttemptRepository.Received(1).CreateAsync(
            Arg.Is<WebhookDeliveryAttempt>(retry =>
                retry != null
                && retry.TenantId == attempt.TenantId
                && retry.MessageId == attempt.MessageId
                && retry.EndpointId == attempt.EndpointId
                && retry.AttemptNumber == 2
                && retry.Outcome == WebhookDeliveryAttemptOutcome.Scheduled),
            Arg.Any<CancellationToken>());
        await fixture.EndpointRepository.Received(1).RecordFailureAsync(
            attempt.TenantId,
            attempt.EndpointId,
            Arg.Any<DateTime>(),
            "http_non_success",
            attempt.Endpoint!.MaxAttempts,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessBatchAsync_WhenHttpClientTimesOut_SchedulesRetry()
    {
        var handler = new RecordingMessageHandler(_ => throw new TaskCanceledException("simulated timeout"));
        var fixture = new Fixture(handler);
        var attempt = CreateAttempt();
        fixture.ConfigureClaim(attempt);

        WebhookDeliveryDrainResult result = await fixture.Service.ProcessBatchAsync(CancellationToken.None);

        await Assert.That(result.RetryScheduledCount).IsEqualTo(1);
        await fixture.AttemptRepository.Received(1).MarkFailedAsync(
            attempt.TenantId,
            attempt.Id,
            Arg.Any<Guid>(),
            Arg.Any<long>(),
            WebhookDeliveryAttemptOutcome.Failed,
            Arg.Any<DateTime>(),
            "timeout",
            Arg.Is<int?>(value => value == null),
            Arg.Any<int>(),
            Arg.Is<DateTime?>(value => value != null),
            Arg.Any<CancellationToken>());
        await fixture.AttemptRepository.Received(1).CreateAsync(
            Arg.Is<WebhookDeliveryAttempt>(retry =>
                retry != null
                && retry.AttemptNumber == 2
                && retry.Outcome == WebhookDeliveryAttemptOutcome.Scheduled),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessBatchAsync_WhenEndpointRedirects_TreatsRedirectAsFailedDelivery()
    {
        var handler = new RecordingMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Redirect));
        var fixture = new Fixture(handler);
        var attempt = CreateAttempt();
        fixture.ConfigureClaim(attempt);

        await fixture.Service.ProcessBatchAsync(CancellationToken.None);

        await fixture.AttemptRepository.Received(1).MarkFailedAsync(
            attempt.TenantId,
            attempt.Id,
            Arg.Any<Guid>(),
            Arg.Any<long>(),
            WebhookDeliveryAttemptOutcome.Failed,
            Arg.Any<DateTime>(),
            "redirect_response",
            (int)HttpStatusCode.Redirect,
            Arg.Any<int>(),
            Arg.Is<DateTime?>(value => value != null),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessBatchAsync_WhenEndpointTargetsPrivateAddress_BlocksWithoutHttpSend()
    {
        var handler = new RecordingMessageHandler(_ => throw new InvalidOperationException("HTTP should not be called."));
        var fixture = new Fixture(handler);
        var attempt = CreateAttempt();
        attempt.Endpoint!.Url = "http://127.0.0.1/webhook";
        fixture.ConfigureClaim(attempt);

        WebhookDeliveryDrainResult result = await fixture.Service.ProcessBatchAsync(CancellationToken.None);

        await Assert.That(result.AbandonedCount).IsEqualTo(1);
        await Assert.That(handler.CallCount).IsEqualTo(0);
        await fixture.AttemptRepository.Received(1).MarkFailedAsync(
            attempt.TenantId,
            attempt.Id,
            Arg.Any<Guid>(),
            Arg.Any<long>(),
            WebhookDeliveryAttemptOutcome.Abandoned,
            Arg.Any<DateTime>(),
            "private_network_blocked",
            Arg.Is<int?>(value => value == null),
            0,
            Arg.Is<DateTime?>(value => value == null),
            Arg.Any<CancellationToken>());
        await fixture.EndpointRepository.Received(1).RecordFailureAsync(
            attempt.TenantId,
            attempt.EndpointId,
            Arg.Any<DateTime>(),
            "private_network_blocked",
            1,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RecoverStaleProcessingAsync_ResetsExpiredSendingAttempts()
    {
        var fixture = new Fixture(new RecordingMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent)),
            new WebhookDeliveryProcessorSettings { BatchSize = 25, ProcessingLeaseTimeoutSeconds = 120 });
        DateTime? cutoff = null;
        fixture.AttemptRepository.ResetStaleSendingAsync(
                Arg.Do<DateTime>(value => cutoff = value),
                Arg.Any<DateTime>(),
                "processing_lease_expired",
                25,
                Arg.Any<CancellationToken>())
            .Returns(3);

        WebhookDeliveryRecoveryResult result = await fixture.Service.RecoverStaleProcessingAsync(CancellationToken.None);

        await Assert.That(result.RecoveredCount).IsEqualTo(3);
        await Assert.That(cutoff).IsNotNull();
        await Assert.That(Math.Abs((result.ProcessingStartedBefore - cutoff!.Value).TotalMilliseconds)).IsLessThan(5);
    }

    [Test]
    public async Task ScheduleManualRetryAsync_WhenAttemptIsTerminal_CreatesImmediateScheduledAttempt()
    {
        var fixture = new Fixture(new RecordingMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent)));
        var attempt = CreateAttempt();
        attempt.Outcome = WebhookDeliveryAttemptOutcome.Abandoned;
        attempt.CompletedAt = DateTime.UtcNow.AddMinutes(-5);
        fixture.AttemptRepository.GetByTenantAndIdAsync(attempt.TenantId, attempt.Id, Arg.Any<CancellationToken>())
            .Returns(attempt);
        fixture.AttemptRepository.HasActiveAttemptForEndpointAsync(
                attempt.TenantId,
                attempt.MessageId,
                attempt.EndpointId,
                Arg.Any<CancellationToken>())
            .Returns(false);
        fixture.AttemptRepository.GetNextAttemptNumberAsync(
                attempt.TenantId,
                attempt.MessageId,
                attempt.EndpointId,
                Arg.Any<CancellationToken>())
            .Returns(9);
        WebhookDeliveryAttempt? createdAttempt = null;
        fixture.AttemptRepository.CreateAsync(
                Arg.Do<WebhookDeliveryAttempt>(value => createdAttempt = value),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<WebhookDeliveryAttempt>());

        WebhookDeliverySingleDrainResult result = await fixture.Service.ScheduleManualRetryAsync(
            attempt.TenantId,
            attempt.Id,
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(WebhookDeliveryDrainOutcome.RetryScheduled);
        await Assert.That(createdAttempt).IsNotNull();
        await Assert.That(result.AttemptId).IsEqualTo(createdAttempt!.Id);
        await Assert.That(createdAttempt.TenantId).IsEqualTo(attempt.TenantId);
        await Assert.That(createdAttempt.MessageId).IsEqualTo(attempt.MessageId);
        await Assert.That(createdAttempt.EndpointId).IsEqualTo(attempt.EndpointId);
        await Assert.That(createdAttempt.AttemptNumber).IsEqualTo(9);
        await Assert.That(createdAttempt.Outcome).IsEqualTo(WebhookDeliveryAttemptOutcome.Scheduled);
        await Assert.That(createdAttempt.ScheduledAt).IsBetween(
            DateTime.UtcNow.AddSeconds(-5),
            DateTime.UtcNow.AddSeconds(5));
    }

    [Test]
    public async Task ScheduleManualRetryAsync_WhenActiveAttemptAlreadyExists_ReturnsDeferredWithoutDuplicate()
    {
        var fixture = new Fixture(new RecordingMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent)));
        var attempt = CreateAttempt();
        attempt.Outcome = WebhookDeliveryAttemptOutcome.Failed;
        fixture.AttemptRepository.GetByTenantAndIdAsync(attempt.TenantId, attempt.Id, Arg.Any<CancellationToken>())
            .Returns(attempt);
        fixture.AttemptRepository.HasActiveAttemptForEndpointAsync(
                attempt.TenantId,
                attempt.MessageId,
                attempt.EndpointId,
                Arg.Any<CancellationToken>())
            .Returns(true);

        WebhookDeliverySingleDrainResult result = await fixture.Service.ScheduleManualRetryAsync(
            attempt.TenantId,
            attempt.Id,
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(WebhookDeliveryDrainOutcome.Deferred);
        await fixture.AttemptRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Test]
    public async Task ScheduleManualRetryAsync_WhenEndpointIsNotActive_SkipsWithoutScheduling()
    {
        var fixture = new Fixture(new RecordingMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent)));
        var attempt = CreateAttempt();
        attempt.Outcome = WebhookDeliveryAttemptOutcome.Abandoned;
        attempt.Endpoint!.Status = WebhookEndpointStatus.Disabled;
        fixture.AttemptRepository.GetByTenantAndIdAsync(attempt.TenantId, attempt.Id, Arg.Any<CancellationToken>())
            .Returns(attempt);

        WebhookDeliverySingleDrainResult result = await fixture.Service.ScheduleManualRetryAsync(
            attempt.TenantId,
            attempt.Id,
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(WebhookDeliveryDrainOutcome.Skipped);
        await fixture.AttemptRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    private static WebhookDeliveryAttempt CreateAttempt()
    {
        var tenantId = Guid.CreateVersion7();
        var messageId = Guid.CreateVersion7();
        var endpointId = Guid.CreateVersion7();
        var createdAt = DateTime.UtcNow;
        return new WebhookDeliveryAttempt
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            MessageId = messageId,
            Message = WebhookMessage.Create(
                messageId,
                tenantId,
                "event.published",
                "domain-event-1",
                "Event",
                Guid.CreateVersion7(),
                consumerId: null,
                "{\"type\":\"event.published\"}"u8,
                "application/json",
                "utf-8",
                createdAt,
                createdAt.AddDays(14),
                createdAt),
            EndpointId = endpointId,
            Endpoint = new WebhookEndpoint
            {
                Id = endpointId,
                TenantId = tenantId,
                ConsumerId = Guid.CreateVersion7(),
                Url = "https://93.184.216.34/webhook",
                Status = WebhookEndpointStatus.Active,
                SecretRef = "endpoint-one",
                SecretVersion = 1,
                MaxAttempts = 8,
                TimeoutSeconds = 15,
                CreatedAt = DateTime.UtcNow
            },
            AttemptNumber = 1,
            Outcome = WebhookDeliveryAttemptOutcome.Scheduled,
            ScheduledAt = createdAt.AddSeconds(-1),
            CreatedAt = createdAt
        };
    }

    private static string CreateSvixSecret() =>
        "whsec_" + Convert.ToBase64String(Encoding.UTF8.GetBytes("local-webhook-signing-secret"));

    private sealed class Fixture
    {
        public Fixture(
            RecordingMessageHandler handler,
            WebhookDeliveryProcessorSettings? settings = null,
            WebhookOptions? webhookOptions = null)
        {
            AttemptRepository = Substitute.For<IWebhookDeliveryAttemptRepository>();
            EndpointRepository = Substitute.For<IWebhookEndpointRepository>();
            MessageRepository = Substitute.For<IWebhookMessageRepository>();
            var services = new ServiceCollection();
            services.AddSingleton(AttemptRepository);
            services.AddSingleton(EndpointRepository);
            services.AddSingleton(MessageRepository);
            services.AddSingleton(Substitute.For<ITenantContextAccessor>());
            ServiceProvider = services.BuildServiceProvider();

            AttemptRepository.MarkSucceededAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<Guid>(),
                    Arg.Any<Guid>(),
                    Arg.Any<long>(),
                    Arg.Any<DateTime>(),
                    Arg.Any<DateTime>(),
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<CancellationToken>())
                .Returns(true);
            AttemptRepository.MarkFailedAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<Guid>(),
                    Arg.Any<Guid>(),
                    Arg.Any<long>(),
                    Arg.Any<WebhookDeliveryAttemptOutcome>(),
                    Arg.Any<DateTime>(),
                    Arg.Any<string>(),
                    Arg.Any<int?>(),
                    Arg.Any<int>(),
                    Arg.Any<DateTime?>(),
                    Arg.Any<CancellationToken>())
                .Returns(true);
            EndpointRepository.RecordFailureAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<Guid>(),
                    Arg.Any<DateTime>(),
                    Arg.Any<string>(),
                    Arg.Any<int>(),
                    Arg.Any<CancellationToken>())
                .Returns(new WebhookEndpointFailureState(1, false));

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{WebhookOptions.SectionName}:EndpointSecrets:endpoint-one"] = CreateSvixSecret()
                })
                .Build();
            var meterFactory = Substitute.For<IMeterFactory>();
            meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
            var options = webhookOptions ?? new WebhookOptions
            {
                Enabled = true,
                Provider = WebhookOptions.ProviderLocal,
                Local = new WebhookLocalOptions
                {
                    BlockPrivateNetworks = true,
                    MaxAttempts = 8,
                    MaxPayloadBytes = 1024 * 1024,
                    MaxResponsePreviewBytes = 4096,
                    TimeoutSeconds = 15,
                    ConnectTimeoutSeconds = 3
                }
            };

            Service = new WebhookDeliveryDrainService(
                ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
                new StaticHttpClientFactory(new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan }),
                new WebhookSignatureService(),
                new WebhookEndpointSafetyPolicy(new StaticOptionsMonitor<WebhookOptions>(options)),
                new WebhookRetryScheduler(),
                new WebhookEndpointSecretResolver(configuration),
                Options.Create(settings ?? new WebhookDeliveryProcessorSettings()),
                new StaticOptionsMonitor<WebhookOptions>(options),
                new BusinessMetrics(meterFactory),
                NullLogger<WebhookDeliveryDrainService>.Instance);
        }

        public IWebhookDeliveryAttemptRepository AttemptRepository { get; }

        public IWebhookEndpointRepository EndpointRepository { get; }

        public IWebhookMessageRepository MessageRepository { get; }

        public ServiceProvider ServiceProvider { get; }

        public WebhookDeliveryDrainService Service { get; }

        public void ConfigureClaim(WebhookDeliveryAttempt attempt)
        {
            var claimedAt = DateTime.UtcNow;
            var leaseToken = Guid.CreateVersion7();
            const long processingFence = 1;
            AttemptRepository.GetDueTenantIdsAsync(
                    Arg.Any<int>(),
                    Arg.Any<DateTime>(),
                    Arg.Any<CancellationToken>())
                .Returns([attempt.TenantId]);
            AttemptRepository.CountDueScheduledAsync(
                    Arg.Any<DateTime>(),
                    Arg.Any<CancellationToken>())
                .Returns(1);
            AttemptRepository.ClaimDueAsync(
                    Arg.Any<WebhookDeliveryClaimRequest>(),
                    Arg.Any<IReadOnlyDictionary<Guid, WebhookDeliveryClaimLimits>>(),
                    Arg.Any<CancellationToken>())
                .Returns([
                    new WebhookDeliveryClaim(
                        attempt,
                        leaseToken,
                        processingFence,
                        claimedAt,
                        claimedAt.AddMinutes(2))
                ]);
        }
    }

    private sealed class StaticHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class RecordingMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public string? Body { get; private set; }
        public Dictionary<string, string> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            foreach (var header in request.Headers)
            {
                Headers[header.Key] = string.Join(" ", header.Value);
            }

            return responseFactory(request);
        }
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
