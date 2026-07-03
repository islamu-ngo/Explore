// ABOUTME: Unit tests for LocalProvider webhook retry backoff scheduling.
// ABOUTME: Locks the documented attempt schedule used by delivery workers and manual retries.

using Explore.Infrastructure.Webhooks;

namespace Explore.Infrastructure.Tests.Infrastructure.Webhooks;

public sealed class WebhookRetrySchedulerTests
{
    [Test]
    public async Task GetDelay_ReturnsDocumentedBackoffSchedule()
    {
        var scheduler = new WebhookRetryScheduler();

        await Assert.That(scheduler.GetDelay(1)).IsEqualTo(TimeSpan.Zero);
        await Assert.That(scheduler.GetDelay(2)).IsEqualTo(TimeSpan.FromSeconds(30));
        await Assert.That(scheduler.GetDelay(3)).IsEqualTo(TimeSpan.FromMinutes(5));
        await Assert.That(scheduler.GetDelay(4)).IsEqualTo(TimeSpan.FromMinutes(30));
        await Assert.That(scheduler.GetDelay(5)).IsEqualTo(TimeSpan.FromHours(2));
        await Assert.That(scheduler.GetDelay(6)).IsEqualTo(TimeSpan.FromHours(6));
        await Assert.That(scheduler.GetDelay(7)).IsEqualTo(TimeSpan.FromHours(12));
        await Assert.That(scheduler.GetDelay(8)).IsEqualTo(TimeSpan.FromHours(24));
    }

    [Test]
    public async Task CanScheduleAttempt_RespectsEndpointAndSchedulerMaximums()
    {
        var scheduler = new WebhookRetryScheduler();

        await Assert.That(scheduler.CanScheduleAttempt(8, endpointMaxAttempts: 8)).IsTrue();
        await Assert.That(scheduler.CanScheduleAttempt(9, endpointMaxAttempts: 20)).IsFalse();
        await Assert.That(scheduler.CanScheduleAttempt(4, endpointMaxAttempts: 3)).IsFalse();
    }
}
