// ABOUTME: Unit tests for webhook delivery-plan resolution and materialization behavior.
// ABOUTME: Ensures dry-run plans persist canonical messages without provider publications or local targets.

using System.Diagnostics.Metrics;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Telemetry;
using Explore.Application.Webhooks;
using Explore.Domain;
using Explore.Infrastructure.Configuration;
using Explore.Infrastructure.Webhooks;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure.Webhooks;

public sealed class WebhookProviderResolverTests
{
    [Test]
    public async Task CapabilityResolver_WhenSelfHostedSvixProfileIsPinned_ReturnsOnlyProvenCapabilities()
    {
        var resolver = new WebhookProviderCapabilityResolver(
            new StaticOptionsMonitor<WebhookOptions>(SupportedSelfHostedOptions()));

        var resolution = resolver.Resolve(WebhookProviderMode.Svix);

        await Assert.That(resolution.IsProviderModeAvailable).IsTrue();
        await Assert.That(resolution.LocalCapabilities).IsEqualTo(WebhookProviderCapability.None);
        await Assert.That(resolution.ProviderCapabilities).IsEqualTo(
            WebhookProviderCapability.EndpointManagement |
            WebhookProviderCapability.PayloadInspection |
            WebhookProviderCapability.AppPortal |
            WebhookProviderCapability.EventCatalog);
        await Assert.That(resolution.ProviderCapabilities.HasFlag(WebhookProviderCapability.Replay)).IsFalse();
        await Assert.That(resolution.ProviderEnvironment)
            .IsEqualTo(SvixConformanceProfileRegistry.SelfHostedEnvironment);
        await Assert.That(resolution.ProviderVersion)
            .IsEqualTo(SvixConformanceProfileRegistry.SelfHostedProviderVersion);
    }

    [Test]
    public async Task CapabilityResolver_WhenLocalModeSelected_DoesNotClaimProviderNativeParity()
    {
        var resolver = new WebhookProviderCapabilityResolver(
            new StaticOptionsMonitor<WebhookOptions>(new WebhookOptions()));

        var resolution = resolver.Resolve(WebhookProviderMode.Local);

        await Assert.That(resolution.IsProviderModeAvailable).IsTrue();
        await Assert.That(resolution.LocalCapabilities).IsEqualTo(
            WebhookProviderCapability.EndpointManagement |
            WebhookProviderCapability.EventCatalog);
        await Assert.That(resolution.ProviderCapabilities).IsEqualTo(WebhookProviderCapability.None);
        await Assert.That(resolution.SupportsLocalConfiguration(WebhookProviderCapability.EndpointManagement)).IsTrue();
        await Assert.That(resolution.SupportsLocalConfiguration(WebhookProviderCapability.ProviderAttemptVisibility)).IsFalse();
    }

    [Test]
    public async Task CapabilityResolver_WhenTenantOverrideIsLocked_RejectsDifferentMode()
    {
        var options = new WebhookOptions
        {
            Provider = WebhookOptions.ProviderLocal,
            AllowTenantOverride = false
        };
        var resolver = new WebhookProviderCapabilityResolver(new StaticOptionsMonitor<WebhookOptions>(options));

        var resolution = resolver.Resolve(WebhookProviderMode.Svix);

        await Assert.That(resolution.IsProviderModeAvailable).IsFalse();
        await Assert.That(resolution.UnavailableReasonCode)
            .IsEqualTo("webhook_provider_tenant_override_disabled");
    }

    [Test]
    public async Task Publisher_WhenDryRunPlanIsResolved_MaterializesCanonicalMessageWithoutProviderIdentifier()
    {
        var planResolver = Substitute.For<IWebhookDeliveryPlanResolver>();
        var materializer = Substitute.For<IWebhookDeliveryPlanMaterializer>();
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
        var context = CreateContext();
        var materializedAt = context.OccurredAt.AddMinutes(1);
        WebhookDeliveryMaterialization? captured = null;
        planResolver.ResolveAsync(context, Arg.Any<CancellationToken>())
            .Returns(WebhookDeliveryPlanResolution.Success(
                context.ConsumerId!.Value,
                WebhookProviderMode.DryRun,
                "dry-run-v1",
                1,
                "standard",
                "retention-v1",
                context.OccurredAt.AddDays(14).UtcDateTime));
        materializer.MaterializeAsync(
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
        var publisher = new DefaultWebhookEventPublisher(
            new DefaultWebhookPayloadBuilder(new WebhookEventTypeRegistry()),
            planResolver,
            materializer,
            new BusinessMetrics(meterFactory),
            new FixedTimeProvider(materializedAt));

        var result = await publisher.PublishAsync(context, CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.MessageId).IsEqualTo(context.MessageId);
        await Assert.That(result.ProviderMessageId).IsNull();
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.DeliveryPlan.ProviderMode).IsEqualTo(WebhookProviderMode.DryRun);
        await Assert.That(captured.ProviderPublications).IsEmpty();
        await Assert.That(captured.LocalTargets).IsEmpty();
        await Assert.That(Encoding.UTF8.GetString(captured.Message.GetPayloadBytes()!))
            .Contains("\"event.published\"");
    }

    private static WebhookEventBuildContext CreateContext()
    {
        var messageId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var aggregateId = Guid.CreateVersion7();

        return new WebhookEventBuildContext(
            messageId,
            tenantId,
            WebhookEventNames.EventPublished,
            "domain-event-1",
            "Event",
            aggregateId,
            DateTimeOffset.UtcNow,
            new Dictionary<string, object?>
            {
                ["eventId"] = aggregateId.ToString("D"),
                ["status"] = "Published",
                ["publicUrl"] = "https://example.org/events/community-iftar"
            },
            Guid.CreateVersion7());
    }

    private static WebhookOptions SupportedSelfHostedOptions() =>
        new()
        {
            Provider = WebhookOptions.ProviderSvix,
            Svix = new WebhookSvixOptions
            {
                BaseUrl = "http://svix:8071",
                Environment = SvixConformanceProfileRegistry.SelfHostedEnvironment,
                ProviderVersion = SvixConformanceProfileRegistry.SelfHostedProviderVersion,
                CapabilityPolicyVersion = SvixConformanceProfileRegistry.SelfHostedCapabilityPolicyVersion
            }
        };

    private sealed class StaticOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = currentValue;
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
