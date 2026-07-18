// ABOUTME: Verifies Basic Dispatch Mode email metrics use bounded, safe OpenTelemetry tags.
// ABOUTME: Guards against exposing email body, recipient, subject, or secret-like data in metric dimensions.

using System.Diagnostics.Metrics;
using Explore.Application.Telemetry;
using NSubstitute;

namespace ApplicationUnitTests.Telemetry;

[NotInParallel("BusinessMetricsMeter")]
public sealed class BusinessMetricsEmailDispatchTests
{
    [Test]
    public async Task RecordEmailDispatchAttemptRecordsExpectedSafeTags()
    {
        using var metricsCapture = new MetricsCapture();
        var metrics = CreateMetrics();
        var tenantId = Guid.NewGuid().ToString();

        metrics.RecordEmailDispatchAttempt(tenantId, "retry_scheduled", "smtp_send_failed");

        var measurement = await metricsCapture.SingleByTenantAsync("explore.email_dispatch.attempts", tenantId);

        await Assert.That(measurement.Value).IsEqualTo(1);
        await Assert.That(measurement.Tags["tenant_id"]?.ToString()).IsEqualTo(tenantId);
        await Assert.That(measurement.Tags["outcome"]?.ToString()).IsEqualTo("retry_scheduled");
        await Assert.That(measurement.Tags["failure_category"]?.ToString()).IsEqualTo("smtp_send_failed");
    }

    [Test]
    public async Task RecordEmailDispatchAttemptDoesNotEmitSensitiveOrHighCardinalityTags()
    {
        using var metricsCapture = new MetricsCapture();
        var metrics = CreateMetrics();

        var tenantId = Guid.NewGuid().ToString();

        metrics.RecordEmailDispatchAttempt(tenantId, "sent");

        var measurement = await metricsCapture.SingleByTenantAsync("explore.email_dispatch.attempts", tenantId);

        await Assert.That(measurement.Tags.Keys).DoesNotContain("recipient");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("recipient_email");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("subject");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("body");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("html_body");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("plain_text_body");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("secret");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("provider_message_id");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("error");
    }

    [Test]
    public async Task RecordEmailDispatchRabbitMqConsumeRecordsExpectedSafeTags()
    {
        using var metricsCapture = new MetricsCapture();
        var metrics = CreateMetrics();
        var tenantId = Guid.NewGuid().ToString();

        metrics.RecordEmailDispatchRabbitMqConsume(tenantId, "acked", "none");

        var measurement = await metricsCapture.SingleByTenantAsync("explore.email_dispatch.rabbitmq.consumes", tenantId);

        await Assert.That(measurement.Value).IsEqualTo(1);
        await Assert.That(measurement.Tags["tenant_id"]?.ToString()).IsEqualTo(tenantId);
        await Assert.That(measurement.Tags["outcome"]?.ToString()).IsEqualTo("acked");
        await Assert.That(measurement.Tags["failure_category"]?.ToString()).IsEqualTo("none");
    }

    [Test]
    public async Task RecordEmailDispatchRabbitMqConsumeDoesNotEmitSensitiveOrHighCardinalityTags()
    {
        using var metricsCapture = new MetricsCapture();
        var metrics = CreateMetrics();

        var tenantId = Guid.NewGuid().ToString();

        metrics.RecordEmailDispatchRabbitMqConsume(tenantId, "rejected", "missing_row");

        var measurement = await metricsCapture.SingleByTenantAsync("explore.email_dispatch.rabbitmq.consumes", tenantId);

        await Assert.That(measurement.Tags.Keys).DoesNotContain("recipient");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("recipient_email");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("subject");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("body");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("html_body");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("plain_text_body");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("secret");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("provider_message_id");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("publish_event_id");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("delivery_tag");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("error");
    }

    [Test]
    public async Task RecordEmailDispatchOperationalSignalsUsesOnlyBoundedSafeTags()
    {
        using var metricsCapture = new MetricsCapture();
        var metrics = CreateMetrics();
        var tenantId = Guid.CreateVersion7().ToString();

        metrics.RecordEmailDispatchTenantBacklog(tenantId, 17);
        metrics.RecordEmailDispatchOldestPendingAge(125.5);

        var backlog = await metricsCapture.SingleByTenantAsync("explore.email_dispatch.tenant_backlog", tenantId);
        var oldest = await metricsCapture.SingleAsync("explore.email_dispatch.oldest_pending_age");

        await Assert.That(backlog.Value).IsEqualTo(17);
        await Assert.That(backlog.Tags.Keys).IsEquivalentTo(["tenant_id"]);
        await Assert.That(oldest.DoubleValue).IsEqualTo(125.5);
        await Assert.That(oldest.Tags).IsEmpty();
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
            _listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
            {
                lock (_measurementsLock)
                {
                    _measurements.Add(new Measurement(
                        instrument.Name,
                        0,
                        tags.ToArray().ToDictionary(tag => tag.Key, tag => tag.Value),
                        measurement));
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

        public async Task<Measurement> SingleAsync(string instrumentName)
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                lock (_measurementsLock)
                {
                    var match = _measurements.SingleOrDefault(value => value.InstrumentName == instrumentName);
                    if (match is not null)
                    {
                        return match;
                    }
                }

                await Task.Delay(10);
            }

            lock (_measurementsLock)
            {
                return _measurements.Single(value => value.InstrumentName == instrumentName);
            }
        }

        public void Dispose()
        {
            _listener.Dispose();
        }
    }

    private sealed record Measurement(
        string InstrumentName,
        long Value,
        IReadOnlyDictionary<string, object?> Tags,
        double? DoubleValue = null);
}
