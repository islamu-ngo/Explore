// ABOUTME: bUnit coverage for the webhook management panel's HAL-gated UI actions.
// ABOUTME: Verifies Phase 7 webhook controls stay accessible and service-backed without client role checks.

using Explore.Blazor.Client.Components.Webhooks;
using Explore.Blazor.Client.Contracts.Services.Webhooks;

namespace Explore.Blazor.Client.Tests.Components.Webhooks;

public sealed class WebhookManagementPanelTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IWebhookManagementService _webhookService;

    public WebhookManagementPanelTests()
    {
        _ctx = new BlazorTestContext();
        _webhookService = Substitute.For<IWebhookManagementService>();
        _ctx.Services.AddSingleton(_webhookService);
    }

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task WebhookManagementPanel_WhenHalLinksExist_RendersAccessibleActionAffordances()
    {
        var snapshot = CreateSnapshot(includeActionLinks: true);
        _webhookService.GetSnapshotAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(snapshot));

        var cut = RenderPanel();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Webhook status summary", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Webhook status summary was not rendered.");
            }
        });

        await Assert.That(cut.Markup).Contains("aria-label=\"Refresh webhooks\"", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("aria-label=\"Open provider portal\"", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("aria-label=\"Update endpoint\"", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("aria-label=\"Rotate signing secret\"", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("aria-label=\"Send test webhook\"", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("aria-label=\"Archive endpoint\"", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("aria-label=\"View delivery attempts\"", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("aria-label=\"Retry delivery\"", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task WebhookManagementPanel_WhenHalLinksAreAbsent_HidesMutationAffordances()
    {
        var snapshot = CreateSnapshot(includeActionLinks: false);
        _webhookService.GetSnapshotAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(snapshot));

        var cut = RenderPanel();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Webhook status summary", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Webhook status summary was not rendered.");
            }
        });

        await Assert.That(cut.Markup).DoesNotContain("aria-label=\"Open provider portal\"", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).DoesNotContain("aria-label=\"Update endpoint\"", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).DoesNotContain("aria-label=\"Rotate signing secret\"", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).DoesNotContain("aria-label=\"Send test webhook\"", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).DoesNotContain("aria-label=\"Archive endpoint\"", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).DoesNotContain("aria-label=\"Retry delivery\"", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task WebhookManagementPanel_TestRetryPortalAndAttemptActions_InvokeServiceLayer()
    {
        var snapshot = CreateSnapshot(includeActionLinks: true);
        var endpointId = snapshot.Endpoints.Single().Id!.Value;
        var consumerId = snapshot.Consumers.Single().Id!.Value;
        var messageId = snapshot.Messages.Single().Id!.Value;
        var attemptId = snapshot.DeliveryAttempts.Single().Id!.Value;

        _webhookService.GetSnapshotAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(snapshot));
        _webhookService.TestEndpointAsync(endpointId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(WebhookActionResult.Succeeded("Test webhook queued.")));
        _webhookService.RetryDeliveryAttemptAsync(attemptId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(WebhookActionResult.Succeeded("Retry scheduled.")));
        _webhookService.GetDeliveryAttemptsAsync(messageId, null, 100, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(snapshot.DeliveryAttempts));
        _webhookService.OpenProviderPortalAsync(consumerId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new WebhookPortalResult(true, "Portal ready.", "https://svix.example.test/app-portal")));

        var cut = RenderPanel();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("aria-label=\"Send test webhook\"", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Endpoint test action was not rendered.");
            }
        });

        cut.Find("button[aria-label='Send test webhook']").Click();
        cut.Find("button[aria-label='View delivery attempts']").Click();
        cut.Find("button[aria-label='Retry delivery']").Click();
        cut.Find("button[aria-label='Open provider portal']").Click();

        cut.WaitForAssertion(() =>
            _webhookService.Received(1).TestEndpointAsync(endpointId, Arg.Any<CancellationToken>()));
        await _webhookService.Received(1).GetDeliveryAttemptsAsync(messageId, null, 100, Arg.Any<CancellationToken>());
        await _webhookService.Received(1).RetryDeliveryAttemptAsync(attemptId, Arg.Any<CancellationToken>());
        await _webhookService.Received(1).OpenProviderPortalAsync(consumerId, Arg.Any<CancellationToken>());
    }

    private IRenderedComponent<WebhookManagementPanel> RenderPanel() =>
        _ctx.RenderMudComponent<WebhookManagementPanel>();

    private static WebhookManagementSnapshot CreateSnapshot(bool includeActionLinks)
    {
        var tenantId = Guid.CreateVersion7();
        var consumerId = Guid.CreateVersion7();
        var endpointId = Guid.CreateVersion7();
        var eventTypeId = Guid.CreateVersion7();
        var messageId = Guid.CreateVersion7();
        var attemptId = Guid.CreateVersion7();

        var consumer = new HalResourceOfWebhookConsumerDto
        {
            Id = consumerId,
            TenantId = tenantId,
            Name = "Operations bridge",
            ConsumerKindId = 5,
            ConsumerKindName = "System integration",
            ProviderModeId = 3,
            ProviderModeName = "Svix",
            StatusId = 1,
            StatusName = "Active"
        };

        var endpoint = new HalResourceOfWebhookEndpointDto
        {
            Id = endpointId,
            TenantId = tenantId,
            ConsumerId = consumerId,
            ConsumerName = consumer.Name,
            ProviderModeId = 3,
            ProviderModeName = "Svix",
            DestinationHost = "integrator.example.test",
            Description = "Integration endpoint",
            StatusId = 1,
            StatusName = "Active",
            SecretVersion = 2,
            MaxAttempts = 8,
            TimeoutSeconds = 15,
            RateLimitPerMinute = 60,
            Subscriptions =
            [
                new Subscriptions
                {
                    Id = Guid.CreateVersion7(),
                    EventTypeId = eventTypeId,
                    EventTypeName = "event.published",
                    EventTypeGroupName = "event",
                    IsEnabled = true,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        var message = new HalResourceOfWebhookMessageDto
        {
            Id = messageId,
            TenantId = tenantId,
            EventType = "event.published",
            EventId = "evt_018f",
            AggregateKind = "Event",
            AggregateId = Guid.CreateVersion7(),
            ConsumerId = consumerId,
            ConsumerName = consumer.Name,
            PayloadHash = "sha256:1234567890abcdef",
            PayloadRetentionUntil = DateTimeOffset.UtcNow.AddDays(14),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var attempt = new HalResourceOfWebhookDeliveryAttemptDto
        {
            Id = attemptId,
            TenantId = tenantId,
            MessageId = messageId,
            MessageEventType = message.EventType,
            EndpointId = endpointId,
            AttemptNumber = 2,
            StatusId = 5,
            StatusName = "Failed",
            ScheduledAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            HttpStatusCode = 500,
            FailureCategory = "http_non_success",
            DurationMs = 1200,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        if (includeActionLinks)
        {
            GeneratedHalLinkTestHelper.SetLinks(
                consumer,
                ("open-provider-portal", $"/api/webhooks/consumers/{consumerId}/svix/app-portal", "POST"));
            GeneratedHalLinkTestHelper.SetLinks(
                endpoint,
                ("update", $"/api/webhooks/endpoints/{endpointId}", "PUT"),
                ("rotate-secret", $"/api/webhooks/endpoints/{endpointId}/rotate-secret", "POST"),
                ("test", $"/api/webhooks/endpoints/{endpointId}/test", "POST"),
                ("delete", $"/api/webhooks/endpoints/{endpointId}", "DELETE"));
            GeneratedHalLinkTestHelper.SetLinks(
                attempt,
                ("retry", $"/api/webhooks/delivery-attempts/{attemptId}/retry", "POST"));
        }

        return new WebhookManagementSnapshot
        {
            EventTypes =
            [
                new WebhookEventTypeDto
                {
                    Id = eventTypeId,
                    Name = "event.published",
                    GroupName = "event",
                    Description = "Event becomes publicly visible.",
                    SchemaVersion = 1,
                    IsPublic = true,
                    IsEnabled = true,
                    PayloadRetentionDays = 14,
                    SchemaJson = "{}",
                    ExamplePayloadJson = "{}"
                }
            ],
            Consumers = [consumer],
            Endpoints = [endpoint],
            Messages = [message],
            DeliveryAttempts = [attempt],
            CanCreateConsumer = true,
            CanCreateEndpoint = true
        };
    }
}
