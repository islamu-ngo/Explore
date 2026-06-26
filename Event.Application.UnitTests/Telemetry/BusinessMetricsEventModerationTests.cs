// ABOUTME: Verifies event moderation metrics use bounded, safe OpenTelemetry tags.
// ABOUTME: Guards against exposing event IDs, moderation case text, image paths, object keys, or exception details.

using System.Diagnostics.Metrics;
using Explore.Application.Telemetry;
using NSubstitute;

namespace ApplicationUnitTests.Telemetry;

[NotInParallel("BusinessMetricsMeter")]
public sealed class BusinessMetricsEventModerationTests
{
    [Test]
    public async Task RecordEventModerationActionRecordsExpectedSafeTags()
    {
        using var metricsCapture = new MetricsCapture();
        using var metrics = CreateMetrics();

        metrics.RecordEventModerationAction(
            "tenant-a",
            "heavy_redacted",
            "pending_storage_deletion",
            "storage_deletion_pending",
            irreversible: true);

        var measurement = await metricsCapture.SingleAsync("explore.events.moderation_actions");

        await Assert.That(measurement.Value).IsEqualTo(1);
        await Assert.That(measurement.Tags["tenant_id"]?.ToString()).IsEqualTo("tenant-a");
        await Assert.That(measurement.Tags["action_kind"]?.ToString()).IsEqualTo("heavy_redacted");
        await Assert.That(measurement.Tags["outcome"]?.ToString()).IsEqualTo("pending_storage_deletion");
        await Assert.That(measurement.Tags["failure_category"]?.ToString()).IsEqualTo("storage_deletion_pending");
        await Assert.That(measurement.Tags["irreversible"]?.ToString()).IsEqualTo("true");
    }

    [Test]
    public async Task EventModerationMetricsDoNotEmitUnsafeOrHighCardinalityTags()
    {
        using var metricsCapture = new MetricsCapture();
        using var metrics = CreateMetrics();
        var rawIdentifier = Guid.NewGuid().ToString("N");

        metrics.RecordEventModerationAction(
            "tenant-a",
            $"event-title-{rawIdentifier}",
            $"tenants/{rawIdentifier}/illegal.png",
            $"case-{rawIdentifier}",
            irreversible: null);

        var measurement = await metricsCapture.SingleAsync("explore.events.moderation_actions");
        var tagKeys = measurement.Tags.Keys.ToArray();
        var tagValues = string.Join(" ", measurement.Tags.Values.Select(value => value?.ToString()));

        await Assert.That(tagKeys).DoesNotContain("event_id");
        await Assert.That(tagKeys).DoesNotContain("event_title");
        await Assert.That(tagKeys).DoesNotContain("title");
        await Assert.That(tagKeys).DoesNotContain("slug");
        await Assert.That(tagKeys).DoesNotContain("description");
        await Assert.That(tagKeys).DoesNotContain("content");
        await Assert.That(tagKeys).DoesNotContain("reason_code");
        await Assert.That(tagKeys).DoesNotContain("correlation_id");
        await Assert.That(tagKeys).DoesNotContain("storage_object_id");
        await Assert.That(tagKeys).DoesNotContain("object_key");
        await Assert.That(tagKeys).DoesNotContain("path");
        await Assert.That(tagKeys).DoesNotContain("filename");
        await Assert.That(tagKeys).DoesNotContain("exception");
        await Assert.That(tagKeys).DoesNotContain("error");

        await Assert.That(tagValues).DoesNotContain(rawIdentifier);
        await Assert.That(tagValues).DoesNotContain("illegal.png");
        await Assert.That(tagValues).DoesNotContain("event-title");
        await Assert.That(tagValues).DoesNotContain("case-");
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
                var matches = Snapshot()
                    .Where(measurement => measurement.InstrumentName == instrumentName)
                    .ToList();

                if (matches.Count > 0)
                {
                    return matches.Single();
                }

                await Task.Delay(10);
            }

            return Snapshot()
                .Where(measurement => measurement.InstrumentName == instrumentName)
                .Single();
        }

        public void Dispose()
        {
            _listener.Dispose();
        }

        private Measurement[] Snapshot()
        {
            lock (_measurementsLock)
            {
                return [.. _measurements];
            }
        }
    }

    private sealed record Measurement(string InstrumentName, long Value, IReadOnlyDictionary<string, object?> Tags);
}
