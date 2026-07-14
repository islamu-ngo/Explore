// ABOUTME: bUnit coverage for the webhook management panel's HAL-gated UI actions.
// ABOUTME: Verifies Phase 7 webhook controls stay accessible and service-backed without client role checks.

using Explore.Blazor.Client.Components.Webhooks;
using Explore.Blazor.Client.Components.Common;
using Explore.Blazor.Client.Contracts.Services.Webhooks;
using MudBlazor;

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
        await Assert.That(cut.Markup).Contains("4 of 12 capabilities available", StringComparison.OrdinalIgnoreCase);
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
    public async Task WebhookManagementPanel_WhenCapabilityAuthorityIsUnavailable_ShowsSafeExplanation()
    {
        var snapshot = CreateSnapshot(includeActionLinks: false, capabilityAuthorityAvailable: false);
        _webhookService.GetSnapshotAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(snapshot));

        var cut = RenderPanel();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Capability authority unavailable", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Capability authority status was not rendered.");
            }
        });

        await Assert.That(cut.Markup).Contains("Provider binding verification is required.", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).DoesNotContain("webhook_provider_binding_unverified", StringComparison.Ordinal);
        await Assert.That(cut.Markup).DoesNotContain("aria-label=\"Open provider portal\"", StringComparison.OrdinalIgnoreCase);
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

    [Test]
    public async Task WebhookManagementPanel_UpdateEndpoint_RequiresAndForwardsExplicitPendingWorkDecision()
    {
        var snapshot = CreateSnapshot(includeActionLinks: true);
        var endpoint = snapshot.Endpoints.Single();
        var endpointId = endpoint.Id!.Value;

        _webhookService.GetSnapshotAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(snapshot));
        _webhookService.UpdateEndpointAsync(
                endpointId,
                Arg.Any<UpdateWebhookEndpointRequestDto>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(WebhookActionResult.Succeeded("Endpoint updated.", endpointId)));

        var dialogProvider = _ctx.Render<MudDialogProvider>();
        var cut = RenderPanel();
        cut.WaitForAssertion(() => cut.Find("button[aria-label='Update endpoint']"));

        cut.Find("button[aria-label='Update endpoint']").Click();

        dialogProvider.WaitForAssertion(() =>
        {
            if (!dialogProvider.Markup.Contains("Pending delivery work", StringComparison.OrdinalIgnoreCase)
                || !dialogProvider.Markup.Contains("Preserve existing snapshots", StringComparison.OrdinalIgnoreCase)
                || !dialogProvider.Markup.Contains("Migrate eligible pending deliveries", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The explicit pending-work decision controls were not rendered.");
            }

            var save = dialogProvider.FindAll("button").Single(button => button.TextContent.Trim() == "Save");
            if (!save.HasAttribute("disabled"))
            {
                throw new InvalidOperationException("Update must remain disabled before an explicit decision and reason.");
            }
        });

        var fields = dialogProvider.FindComponents<AppTextField<string>>();
        var urlField = fields.Single(field => HasAdditionalAttribute(field, "Label", "Endpoint URL"));
        var reasonField = fields.Single(field => HasAdditionalAttribute(field, "data-testid", "webhook-update-pending-reason"));
        var decisionGroup = dialogProvider.FindComponent<MudRadioGroup<int>>();
        var acknowledgement = dialogProvider.FindComponents<MudCheckBox<bool>>()
            .Single(checkBox => checkBox.Markup.Contains("provider publications", StringComparison.OrdinalIgnoreCase));

        await cut.InvokeAsync(() => urlField.Instance.ValueChanged.InvokeAsync("https://hooks.example.test/events"));
        await cut.InvokeAsync(() => decisionGroup.Instance.ValueChanged.InvokeAsync(WebhookPendingWorkDecisionIds.MigrateEligible));
        await cut.InvokeAsync(() => reasonField.Instance.ValueChanged.InvokeAsync("Adopt the rotated integration route"));
        await cut.InvokeAsync(() => acknowledgement.Instance.ValueChanged.InvokeAsync(true));

        dialogProvider.FindAll("button").Single(button => button.TextContent.Trim() == "Save").Click();

        await _webhookService.Received(1).UpdateEndpointAsync(
            endpointId,
            Arg.Is<UpdateWebhookEndpointRequestDto>(request =>
                request.ExpectedConfigurationVersion == endpoint.ConfigurationVersion
                && request.PendingWorkDecisionId == WebhookPendingWorkDecisionIds.MigrateEligible
                && request.PendingWorkReason == "Adopt the rotated integration route"
                && request.AcknowledgeUncertainProviderPublications == true
                && request.Url == "https://hooks.example.test/events"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task WebhookManagementPanel_RotateSecret_RequiresAndForwardsExplicitPendingWorkDecision()
    {
        var snapshot = CreateSnapshot(includeActionLinks: true);
        var endpoint = snapshot.Endpoints.Single();
        var endpointId = endpoint.Id!.Value;

        _webhookService.GetSnapshotAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(snapshot));
        _webhookService.RotateEndpointSecretAsync(
                endpointId,
                Arg.Any<RotateWebhookEndpointSecretRequestDto>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(WebhookActionResult.Succeeded("Signing secret rotated.", endpointId)));

        var dialogProvider = _ctx.Render<MudDialogProvider>();
        var cut = RenderPanel();
        cut.WaitForAssertion(() => cut.Find("button[aria-label='Rotate signing secret']"));

        cut.Find("button[aria-label='Rotate signing secret']").Click();

        dialogProvider.WaitForAssertion(() =>
        {
            var rotate = dialogProvider.FindAll("button").Single(button => button.TextContent.Trim() == "Rotate");
            if (!rotate.HasAttribute("disabled"))
            {
                throw new InvalidOperationException("Rotation must remain disabled before an explicit decision and reason.");
            }
        });

        var fields = dialogProvider.FindComponents<AppTextField<string>>();
        var secretField = fields.Single(field => HasAdditionalAttribute(field, "Label", "New secret reference"));
        var reasonField = fields.Single(field => HasAdditionalAttribute(field, "data-testid", "webhook-rotate-pending-reason"));
        var decisionGroup = dialogProvider.FindComponent<MudRadioGroup<int>>();
        var acknowledgement = dialogProvider.FindComponents<MudCheckBox<bool>>()
            .Single(checkBox => checkBox.Markup.Contains("provider publications", StringComparison.OrdinalIgnoreCase));

        await cut.InvokeAsync(() => secretField.Instance.ValueChanged.InvokeAsync("webhook:endpoint:v3"));
        await cut.InvokeAsync(() => decisionGroup.Instance.ValueChanged.InvokeAsync(WebhookPendingWorkDecisionIds.PreserveExisting));
        await cut.InvokeAsync(() => reasonField.Instance.ValueChanged.InvokeAsync("Keep queued deliveries on their original credential"));
        await cut.InvokeAsync(() => acknowledgement.Instance.ValueChanged.InvokeAsync(true));

        dialogProvider.FindAll("button").Single(button => button.TextContent.Trim() == "Rotate").Click();

        await _webhookService.Received(1).RotateEndpointSecretAsync(
            endpointId,
            Arg.Is<RotateWebhookEndpointSecretRequestDto>(request =>
                request.ExpectedConfigurationVersion == endpoint.ConfigurationVersion
                && request.PendingWorkDecisionId == WebhookPendingWorkDecisionIds.PreserveExisting
                && request.PendingWorkReason == "Keep queued deliveries on their original credential"
                && request.AcknowledgeUncertainProviderPublications == true
                && request.NewSecretRef == "webhook:endpoint:v3"
                && request.PreviousSecretValidForSeconds == 86400),
            Arg.Any<CancellationToken>());
    }

    private IRenderedComponent<WebhookManagementPanel> RenderPanel() =>
        _ctx.RenderMudComponent<WebhookManagementPanel>();

    private static WebhookManagementSnapshot CreateSnapshot(
        bool includeActionLinks,
        bool capabilityAuthorityAvailable = true)
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
            ProviderCapabilityAuthorityAvailable = capabilityAuthorityAvailable,
            CapabilityResolutionVersion = "selfhost-v1.96.1-v1",
            CapabilityUnavailableReasonCode = capabilityAuthorityAvailable
                ? null
                : "webhook_provider_binding_unverified",
            ProviderCapabilities = CreateProviderCapabilities(capabilityAuthorityAvailable),
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
            ConfigurationVersion = 7,
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
            EndpointStatusCode = "ACTIVE",
            EndpointStatusName = "Active",
            AttemptNumber = 2,
            OutcomeId = 4,
            OutcomeCode = "FAILED",
            OutcomeName = "Failed",
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

    private static List<ProviderCapabilities> CreateProviderCapabilities(bool authorityAvailable)
    {
        var capabilities = new (int Id, string Code, string Name, bool IsProven)[]
        {
            (1, "ENDPOINT_MANAGEMENT", "Endpoint management", true),
            (2, "ATTEMPT_INSPECTION", "Attempt inspection", false),
            (4, "MESSAGE_REPLAY", "Message replay", false),
            (8, "PAYLOAD_INSPECTION", "Payload inspection", true),
            (16, "APP_PORTAL", "App portal", true),
            (32, "EVENT_CATALOG", "Event catalog", true),
            (64, "RETENTION_CONTROL", "Retention control", false),
            (128, "APPLICATION_THROTTLING", "Application throttling", false),
            (256, "ENDPOINT_THROTTLING", "Endpoint throttling", false),
            (512, "TRANSFORMATIONS", "Transformations", false),
            (1024, "ORDERING", "Ordering", false),
            (2048, "CALLBACKS", "Callbacks", false)
        };

        return capabilities
            .Select(capability => new ProviderCapabilities
            {
                CapabilityId = capability.Id,
                CapabilityCode = capability.Code,
                CapabilityName = capability.Name,
                IsAvailable = authorityAvailable && capability.IsProven,
                AvailableFromProviderCodes = authorityAvailable && capability.IsProven ? ["SVIX"] : [],
                UnavailableReasonCode = authorityAvailable && capability.IsProven
                    ? null
                    : authorityAvailable
                        ? "webhook_provider_capability_unproven"
                        : "webhook_provider_binding_unverified"
            })
            .ToList();
    }

    private static bool HasAdditionalAttribute<T>(
        IRenderedComponent<T> component,
        string name,
        string expectedValue)
        where T : IComponent =>
        component.Instance is AppTextField<string> field
        && field.AdditionalAttributes?.TryGetValue(name, out var value) == true
        && string.Equals(value?.ToString(), expectedValue, StringComparison.Ordinal);
}
