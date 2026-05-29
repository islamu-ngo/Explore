// ABOUTME: Verifies AI provider metrics use bounded, safe OpenTelemetry tags.
// ABOUTME: Guards against exposing prompts, model IDs, endpoints, provider request IDs, or secrets.

using System.Diagnostics.Metrics;
using Explore.Application.Telemetry;
using NSubstitute;

namespace ApplicationUnitTests.Telemetry;

public sealed class BusinessMetricsAiProviderTests
{
    [Test]
    public async Task RecordAiProviderHealthCheckRecordsExpectedSafeTags()
    {
        using var metricsCapture = new MetricsCapture();
        var metrics = CreateMetrics();

        metrics.RecordAiProviderHealthCheck("openai-compatible", "healthy", "configured_no_probe");

        var measurement = await metricsCapture.SingleAsync("explore.ai.provider.health_checks");

        await Assert.That(measurement.Value).IsEqualTo(1);
        await Assert.That(measurement.Tags["provider"]?.ToString()).IsEqualTo("openai-compatible");
        await Assert.That(measurement.Tags["status"]?.ToString()).IsEqualTo("healthy");
        await Assert.That(measurement.Tags["reason"]?.ToString()).IsEqualTo("configured_no_probe");
    }

    [Test]
    public async Task RecordAiProviderRequestDoesNotEmitSensitiveOrHighCardinalityTags()
    {
        using var metricsCapture = new MetricsCapture();
        var metrics = CreateMetrics();

        metrics.RecordAiProviderRequest("fake", "succeeded");

        var measurement = await metricsCapture.SingleAsync("explore.ai.provider.requests");

        await Assert.That(measurement.Tags.Keys).DoesNotContain("prompt");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("response");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("content");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("endpoint");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("endpoint_url");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("api_key");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("model_id");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("provider_request_id");
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
