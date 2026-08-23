// ABOUTME: Verifies the EventLocation privacy review-queue monitor snapshot and gauge publication.
// ABOUTME: Proves the operator threshold is applied and the backlog reaches the meter on every probe.

using System.Diagnostics.Metrics;
using Explore.Application.Configuration;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using Explore.Application.Telemetry;
using Explore.Tests.Shared.Telemetry;
using Microsoft.Extensions.Options;
using NSubstitute;
using TUnit.Core;

namespace ApplicationUnitTests.Services;

[Category("EventLocationPrivacy")]
// The gauge listener subscribes by meter name, so a sibling test publishing a different depth on its
// own meter instance would be observed here. Serializing the meter keeps the assertion deterministic.
[NotInParallel("EventLocationPrivacyMeter")]
public sealed class EventLocationReviewQueueMonitorTests
{
    [Test]
    [Arguments(0, 50, false)]
    [Arguments(50, 50, false)]
    [Arguments(51, 50, true)]
    [Arguments(4, 1, true)]
    public async Task GetSnapshotAsync_AppliesTheConfiguredDegradedThreshold(
        int depth,
        int threshold,
        bool expectedExceeds)
    {
        EventLocationReviewQueueMonitor monitor = CreateMonitor(depth, threshold, out _);

        EventLocationReviewQueueSnapshot snapshot = await monitor.GetSnapshotAsync(CancellationToken.None);

        await Assert.That(snapshot.Depth).IsEqualTo(depth);
        await Assert.That(snapshot.DegradedThreshold).IsEqualTo(threshold);
        await Assert.That(snapshot.ExceedsThreshold).IsEqualTo(expectedExceeds);
    }

    [Test]
    public async Task GetSnapshotAsync_PublishesTheBacklogToThePrivacyGauge()
    {
        using var capture = new GaugeCapture();
        EventLocationReviewQueueMonitor monitor = CreateMonitor(13, 50, out _);

        await monitor.GetSnapshotAsync(CancellationToken.None);
        capture.RecordObservableInstruments();

        await Assert.That(capture.LatestReviewQueueDepth).IsEqualTo(13);
    }

    [Test]
    public async Task GetSnapshotAsync_CountsTheBacklogExactlyOncePerProbe()
    {
        EventLocationReviewQueueMonitor monitor = CreateMonitor(3, 50, out IEventLocationRepository repository);

        await monitor.GetSnapshotAsync(CancellationToken.None);

        await repository.Received(1).CountNeedingPrivacyReviewAsync(Arg.Any<CancellationToken>());
    }

    private static EventLocationReviewQueueMonitor CreateMonitor(
        int depth,
        int threshold,
        out IEventLocationRepository repository)
    {
        repository = Substitute.For<IEventLocationRepository>();
        repository.CountNeedingPrivacyReviewAsync(Arg.Any<CancellationToken>()).Returns(depth);
        return new EventLocationReviewQueueMonitor(
            repository,
            EventLocationPrivacyMetricsFactory.Create(),
            Options.Create(new EventLocationPrivacyObservabilityOptions
            {
                ReviewQueueDegradedThreshold = threshold
            }));
    }

    private sealed class GaugeCapture : IDisposable
    {
        private readonly MeterListener _listener;
        private long? _latest;

        public GaugeCapture()
        {
            _listener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == EventLocationPrivacyMetrics.MeterName
                        && instrument.Name == EventLocationPrivacyMetrics.ReviewQueueDepthInstrument)
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }
                }
            };
            _listener.SetMeasurementEventCallback<long>((_, measurement, _, _) => _latest = measurement);
            _listener.Start();
        }

        public long? LatestReviewQueueDepth => _latest;

        public void RecordObservableInstruments() => _listener.RecordObservableInstruments();

        public void Dispose() => _listener.Dispose();
    }
}
