// ABOUTME: Verifies webhook business metrics use bounded, safe OpenTelemetry tags.
// ABOUTME: Guards against exposing endpoint URLs, payloads, secrets, message ids, or response bodies in dimensions.

using System.Diagnostics.Metrics;
using Explore.Application.Telemetry;
using NSubstitute;

namespace ApplicationUnitTests.Telemetry;

[NotInParallel("BusinessMetricsMeter")]
public sealed class BusinessMetricsWebhookTests
{
    [Test]
    public async Task RecordWebhookDeliveryFailureRecordsExpectedSafeTags()
    {
        using var metricsCapture = new MetricsCapture();
        var metrics = CreateMetrics();

        metrics.RecordWebhookDeliveryFailure(
            "event.published",
            "retry_scheduled",
            "http_non_success");

        var measurement = await metricsCapture.SingleAsync("explore.webhooks.delivery_failure");

        await Assert.That(measurement.Value).IsEqualTo(1);
        await Assert.That(measurement.Tags.Keys).DoesNotContain("tenant_id");
        await Assert.That(measurement.Tags["event_type"]?.ToString()).IsEqualTo("event.published");
        await Assert.That(measurement.Tags["outcome"]?.ToString()).IsEqualTo("retry_scheduled");
        await Assert.That(measurement.Tags["failure_category"]?.ToString()).IsEqualTo("http_non_success");
    }

    [Test]
    public async Task RecordWebhookDeliveryFailureDoesNotEmitSensitiveOrHighCardinalityTags()
    {
        using var metricsCapture = new MetricsCapture();
        var metrics = CreateMetrics();

        metrics.RecordWebhookDeliveryFailure(
            "event.published",
            "abandoned",
            "private_network_blocked");

        var measurement = await metricsCapture.SingleAsync("explore.webhooks.delivery_failure");

        await Assert.That(measurement.Tags.Keys).DoesNotContain("tenant_id");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("endpoint_id");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("endpoint_url");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("url");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("secret");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("secret_ref");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("message_id");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("payload");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("payload_json");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("response_body");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("response_body_preview");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("error");
    }

    [Test]
    public async Task RecordWebhookManualRetryRecordsBoundedUnknownsForUnsafeInput()
    {
        using var metricsCapture = new MetricsCapture();
        var metrics = CreateMetrics();

        metrics.RecordWebhookManualRetry(
            "https://example.test/private",
            "operator typed this",
            "raw exception with endpoint https://example.test");

        var measurement = await metricsCapture.SingleAsync("explore.webhooks.manual_retries");

        await Assert.That(measurement.Tags.Keys).DoesNotContain("tenant_id");
        await Assert.That(measurement.Tags["event_type"]?.ToString()).IsEqualTo("unknown");
        await Assert.That(measurement.Tags["outcome"]?.ToString()).IsEqualTo("unknown");
        await Assert.That(measurement.Tags["failure_category"]?.ToString()).IsEqualTo("unknown");
    }

    [Test]
    public async Task RecordWebhookProviderPublishFailureRecordsExpectedSafeTags()
    {
        using var metricsCapture = new MetricsCapture();
        var metrics = CreateMetrics();

        metrics.RecordWebhookProviderPublishFailure(
            "event.published",
            "Svix",
            "svix_provider_unavailable");

        var measurement = await metricsCapture.SingleAsync("explore.webhooks.provider_publish_failure");

        await Assert.That(measurement.Value).IsEqualTo(1);
        await Assert.That(measurement.Tags.Keys).DoesNotContain("tenant_id");
        await Assert.That(measurement.Tags["event_type"]?.ToString()).IsEqualTo("event.published");
        await Assert.That(measurement.Tags["provider"]?.ToString()).IsEqualTo("svix");
        await Assert.That(measurement.Tags["failure_category"]?.ToString()).IsEqualTo("svix_provider_unavailable");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("message_id");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("endpoint_url");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("payload");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("error");
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

        public async Task<Measurement> SingleAsync(string instrumentName)
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
