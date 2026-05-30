// ABOUTME: Verifies notification fanout metrics use bounded, safe OpenTelemetry tags.
// ABOUTME: Guards against exposing event, actor, user, or deduplication identifiers in metric dimensions.

using System.Diagnostics.Metrics;
using Explore.Application.Services;
using Explore.Application.Telemetry;
using NSubstitute;

namespace ApplicationUnitTests.Telemetry;

public sealed class BusinessMetricsNotificationFanoutTests
{
    [Test]
    public async Task RecordNotificationFanoutRunRecordsExpectedSafeTags()
    {
        using var metricsCapture = new MetricsCapture();
        var metrics = CreateMetrics();
        var tenantId = Guid.NewGuid().ToString();

        metrics.RecordNotificationFanoutRun(tenantId, EventPublishedNotificationFanoutService.FanoutKind, EventPublishedNotificationFanoutService.OutcomeCompleted);

        var measurement = await metricsCapture.SingleByTenantAsync("explore.notifications.fanout_runs", tenantId);

        await Assert.That(measurement.Value).IsEqualTo(1);
        await Assert.That(measurement.Tags["tenant_id"]?.ToString()).IsEqualTo(tenantId);
        await Assert.That(measurement.Tags["fanout_kind"]?.ToString()).IsEqualTo(EventPublishedNotificationFanoutService.FanoutKind);
        await Assert.That(measurement.Tags["outcome"]?.ToString()).IsEqualTo(EventPublishedNotificationFanoutService.OutcomeCompleted);
    }

    [Test]
    public async Task RecordNotificationFanoutSubscribersRecordsAggregatedCountWithSafeTags()
    {
        using var metricsCapture = new MetricsCapture();
        var metrics = CreateMetrics();
        var tenantId = Guid.NewGuid().ToString();

        metrics.RecordNotificationFanoutSubscribers(7, tenantId, EventPublishedNotificationFanoutService.FanoutKind, EventPublishedNotificationFanoutService.OutcomeNotificationCreated);

        var measurement = await metricsCapture.SingleByTenantAsync("explore.notifications.fanout_subscribers", tenantId);

        await Assert.That(measurement.Value).IsEqualTo(7);
        await Assert.That(measurement.Tags["tenant_id"]?.ToString()).IsEqualTo(tenantId);
        await Assert.That(measurement.Tags["fanout_kind"]?.ToString()).IsEqualTo(EventPublishedNotificationFanoutService.FanoutKind);
        await Assert.That(measurement.Tags["outcome"]?.ToString()).IsEqualTo(EventPublishedNotificationFanoutService.OutcomeNotificationCreated);
    }

    [Test]
    public async Task NotificationFanoutMetricsDoNotEmitSensitiveOrHighCardinalityTags()
    {
        using var metricsCapture = new MetricsCapture();
        var metrics = CreateMetrics();
        var tenantId = Guid.NewGuid().ToString();

        metrics.RecordNotificationFanoutRun(tenantId, EventPublishedNotificationFanoutService.FanoutKind, EventPublishedNotificationFanoutService.OutcomeFailed);

        var measurement = await metricsCapture.SingleByTenantAsync("explore.notifications.fanout_runs", tenantId);

        await Assert.That(measurement.Tags.Keys).DoesNotContain("event_id");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("actor_id");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("source_actor_id");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("subscriber_user_id");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("subscriber_tenant_user_id");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("notification_id");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("deduplication_key");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("event_title");
    }

    private static BusinessMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
        return new BusinessMetrics(meterFactory);
    }

    private sealed class MetricsCapture : IDisposable
    {
        private readonly MeterListener _listener;
        private readonly Lock _measurementsLock = new();
        private readonly List<Measurement> _measurements = [];

        public MetricsCapture()
        {
            _listener = new MeterListener();
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == BusinessMetrics.MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };

            _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
            {
                lock (_measurementsLock)
                {
                    _measurements.Add(new Measurement(
                        instrument.Name,
                        measurement,
                        tags.ToArray().ToDictionary(tag => tag.Key, tag => tag.Value)));
                }
            });

            _listener.Start();
        }

        public async Task<Measurement> SingleByTenantAsync(string instrumentName, string tenantId)
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                Measurement[] snapshot;
                lock (_measurementsLock)
                {
                    snapshot = [.. _measurements];
                }

                var matches = snapshot
                    .Where(measurement => measurement.InstrumentName == instrumentName)
                    .Where(measurement => measurement.Tags.TryGetValue("tenant_id", out var value)
                        && string.Equals(value?.ToString(), tenantId, StringComparison.Ordinal))
                    .ToList();

                if (matches.Count > 0)
                {
                    return matches.Single();
                }

                await Task.Delay(10);
            }

            lock (_measurementsLock)
            {
                return _measurements
                    .Where(measurement => measurement.InstrumentName == instrumentName)
                    .Where(measurement => measurement.Tags.TryGetValue("tenant_id", out var value)
                        && string.Equals(value?.ToString(), tenantId, StringComparison.Ordinal))
                    .Single();
            }
        }

        public void Dispose()
        {
            _listener.Dispose();
        }
    }

    private sealed record Measurement(string InstrumentName, long Value, IReadOnlyDictionary<string, object?> Tags);
}
