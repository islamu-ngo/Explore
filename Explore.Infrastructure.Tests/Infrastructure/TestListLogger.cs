// ABOUTME: Small test logger for asserting formatted structured log output.
// ABOUTME: Captures log level, rendered message, and exception payloads without external providers.

using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Tests.Infrastructure;

internal sealed class TestListLogger<T> : ILogger<T>
{
    public List<TestLogEntry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => NoopScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new TestLogEntry(logLevel, formatter(state, exception), exception));
    }

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new();

        public void Dispose()
        {
        }
    }
}

internal sealed record TestLogEntry(LogLevel Level, string Message, Exception? Exception);
