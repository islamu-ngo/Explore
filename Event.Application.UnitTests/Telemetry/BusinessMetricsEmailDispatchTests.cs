// ABOUTME: Verifies Basic Dispatch Mode email metrics use bounded, safe OpenTelemetry tags.
// ABOUTME: Guards against exposing email body, recipient, subject, or secret-like data in metric dimensions.

using System.Diagnostics.Metrics;
using Explore.Application.Telemetry;
using NSubstitute;

namespace ApplicationUnitTests.Telemetry;

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

    private static BusinessMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
        return new BusinessMetrics(meterFactory);
    }

    private sealed class MetricsCapture : IDisposable
    {
        private readonly MeterListener _listener;
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
                _measurements.Add(new Measurement(
                    instrument.Name,
                    measurement,
                    tags.ToArray().ToDictionary(tag => tag.Key, tag => tag.Value)));
            });

            _listener.Start();
        }

        public Task<Measurement> SingleByTenantAsync(string instrumentName, string tenantId)
        {
            var matches = _measurements
                .Where(measurement => measurement.InstrumentName == instrumentName)
                .Where(measurement => measurement.Tags.TryGetValue("tenant_id", out var value)
                    && string.Equals(value?.ToString(), tenantId, StringComparison.Ordinal))
                .ToList();
            return Task.FromResult(matches.Single());
        }

        public void Dispose()
        {
            _listener.Dispose();
        }
    }

    private sealed record Measurement(string InstrumentName, long Value, IReadOnlyDictionary<string, object?> Tags);
}
