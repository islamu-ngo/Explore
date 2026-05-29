// ABOUTME: Runtime settings for Basic Dispatch Mode email processing over PostgreSQL plus SMTP.
// ABOUTME: Keeps RabbitMQ optional by controlling the local polling dispatcher separately from broker settings.

namespace Explore.Infrastructure;

public class EmailDispatchProcessorSettings
{
    public const string SectionName = "EmailDispatchProcessor";

    public bool Enabled { get; set; } = true;
    public EmailDispatchProcessorMode Mode { get; set; } = EmailDispatchProcessorMode.TickerQ;
    public int PollingIntervalSeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 50;
    public int MaxAttemptCount { get; set; } = 5;
    public int InitialRetryDelaySeconds { get; set; } = 5;
    public int MaxRetryDelaySeconds { get; set; } = 3600;
    public int ProcessingLeaseTimeoutSeconds { get; set; } = 900;
    public string ConsumerId { get; set; } = Environment.MachineName;
    public bool VerboseLogging { get; set; }

    public int CalculateRetryDelay(int failedAttemptCount)
    {
        var exponent = Math.Max(0, failedAttemptCount - 1);
        var delay = InitialRetryDelaySeconds * (int)Math.Pow(2, exponent);
        return Math.Min(delay, MaxRetryDelaySeconds);
    }
}

public enum EmailDispatchProcessorMode
{
    Disabled = 0,
    TickerQ = 1,
    HostedService = 2
}
