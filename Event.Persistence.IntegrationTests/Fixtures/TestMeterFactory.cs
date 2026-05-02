// ABOUTME: Minimal IMeterFactory implementation for integration tests that construct metric instruments directly.
// ABOUTME: Keeps projection tests independent from the API service-provider metrics registration.

using System.Diagnostics.Metrics;

namespace Event.Persistence.IntegrationTests.Fixtures;

internal sealed class TestMeterFactory : IMeterFactory
{
    private readonly List<Meter> _meters = [];

    public Meter Create(MeterOptions options)
    {
        var meter = new Meter(options);
        _meters.Add(meter);
        return meter;
    }

    public void Dispose()
    {
        foreach (var meter in _meters)
        {
            meter.Dispose();
        }
    }
}
