// ABOUTME: LocalProvider retry schedule for webhook delivery attempts.
// ABOUTME: Centralizes Svix-inspired backoff timing so workers and manual retry flows stay consistent.

namespace Explore.Infrastructure.Webhooks;

public sealed class WebhookRetryScheduler
{
    private static readonly TimeSpan[] Delays =
    [
        TimeSpan.Zero,
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(2),
        TimeSpan.FromHours(6),
        TimeSpan.FromHours(12),
        TimeSpan.FromHours(24)
    ];

    public int MaxScheduledAttempts => Delays.Length;

    public TimeSpan GetDelay(int attemptNumber)
    {
        if (attemptNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptNumber), "Attempt number must be greater than zero.");
        }

        return attemptNumber <= Delays.Length
            ? Delays[attemptNumber - 1]
            : Delays[^1];
    }

    public DateTime GetScheduledAtUtc(int attemptNumber, DateTime nowUtc) =>
        nowUtc.Add(GetDelay(attemptNumber));

    public bool CanScheduleAttempt(int attemptNumber, int endpointMaxAttempts) =>
        attemptNumber >= 1
        && attemptNumber <= endpointMaxAttempts
        && attemptNumber <= MaxScheduledAttempts;
}
