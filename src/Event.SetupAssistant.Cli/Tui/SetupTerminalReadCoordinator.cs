// ABOUTME: Coordinates one background blocking key read against asynchronous terminal signal completion.
// ABOUTME: Returns the first event and permits a losing reader to complete safely after session restoration.

namespace ISLAMU.Event.SetupAssistant.Cli.Tui;

internal sealed class SetupTerminalReadCoordinator : IDisposable
{
    private readonly object _sync = new();
    private readonly ManualResetEventSlim _completed = new(false);
    private SetupTerminalEvent _result;
    private bool _hasResult;
    private bool _started;

    internal void Start(Func<SetupTerminalEvent> blockingRead)
    {
        ArgumentNullException.ThrowIfNull(blockingRead);
        lock (_sync)
        {
            if (_started) throw new InvalidOperationException("terminal-read-already-started");
            _started = true;
        }
        var reader = new Thread(() =>
        {
            SetupTerminalEvent value;
            try { value = blockingRead(); }
            catch (Exception exception) when (exception is IOException or InvalidOperationException)
            { value = SetupTerminalEvent.DriverError(); }
            TryComplete(value);
        })
        {
            IsBackground = true,
            Name = "event-setup-terminal-key-read",
        };
        reader.Start();
    }

    internal bool TryComplete(SetupTerminalEvent value)
    {
        lock (_sync)
        {
            if (_hasResult) return false;
            _result = value;
            _hasResult = true;
            _completed.Set();
            return true;
        }
    }

    internal SetupTerminalEvent Wait()
    {
        lock (_sync)
            if (!_started) throw new InvalidOperationException("terminal-read-not-started");
        _completed.Wait();
        lock (_sync) return _result;
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (!_hasResult) throw new InvalidOperationException("terminal-read-still-active");
            _completed.Dispose();
        }
    }
}
