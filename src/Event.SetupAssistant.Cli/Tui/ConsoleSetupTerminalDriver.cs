// ABOUTME: Owns Console key interception, advisory dimensions, cancellation, and POSIX signal subscriptions.
// ABOUTME: Races each background key read against signals and restores Console state without waiting for late readers.

using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace ISLAMU.Event.SetupAssistant.Cli.Tui;

public sealed class ConsoleSetupTerminalDriver : ISetupTerminalDriver
{
    private readonly ConcurrentQueue<SetupTerminalEvent> _pending = new();
    private readonly List<PosixSignalRegistration> _signals = [];
    private readonly object _coordination = new();
    private SetupTerminalReadCoordinator? _activeRead;
    private SetupTerminalDriverSnapshot? _snapshot;
    private bool _disposed;

    public ConsoleSetupTerminalDriver(SetupCliTerminalCapabilities capabilities) =>
        Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));

    public SetupCliTerminalCapabilities Capabilities { get; }
    public bool InterceptionActive { get; private set; }
    public bool EchoSuppressedByIntercept => InterceptionActive;

    public SetupTerminalDriverSnapshot Snapshot()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new SetupTerminalDriverSnapshot(InterceptionActive, EchoSuppressedByIntercept,
            Console.TreatControlCAsInput, Console.WindowWidth, Console.WindowHeight);
    }

    public void BeginInterception(SetupTerminalDriverSnapshot snapshot)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!SetupTerminalCapabilityPolicy.AllowsSecretEntry(Capabilities))
            throw new InvalidOperationException("interactive-terminal-required");
        _snapshot = snapshot;
        Console.TreatControlCAsInput = false;
        Console.CancelKeyPress += OnCancelKeyPress;
        RegisterSignals();
        InterceptionActive = true;
    }

    public SetupTerminalEvent ReadEvent()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_pending.TryDequeue(out SetupTerminalEvent queued)) return queued;
        if (!TryDimensions(out int width, out int height)) return SetupTerminalEvent.ResizeFailure();

        SetupTerminalReadCoordinator coordinator;
        lock (_coordination)
        {
            if (_pending.TryDequeue(out queued)) return queued;
            coordinator = new SetupTerminalReadCoordinator();
            _activeRead = coordinator;
        }
        coordinator.Start(() => ReadKeyEvent(width, height));
        SetupTerminalEvent result = coordinator.Wait();
        lock (_coordination)
        {
            if (ReferenceEquals(_activeRead, coordinator)) _activeRead = null;
        }
        coordinator.Dispose();
        return result;
    }

    public void WriteBounded(string value, int maximumCharacters, int maximumBytes)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SetupTerminalText.Validate(value, maximumCharacters, maximumBytes);
        Console.Out.Write(value);
        Console.Out.Flush();
    }

    public void Restore(SetupTerminalDriverSnapshot snapshot)
    {
        if (_disposed) return;
        Console.CancelKeyPress -= OnCancelKeyPress;
        foreach (PosixSignalRegistration registration in _signals) registration.Dispose();
        _signals.Clear();
        lock (_coordination)
        {
            _activeRead?.TryComplete(SetupTerminalEvent.DriverError());
            _activeRead = null;
        }
        InterceptionActive = snapshot.InterceptionActive;
        _snapshot = null;
        while (_pending.TryDequeue(out _)) { }
        Console.TreatControlCAsInput = snapshot.TreatControlCAsInput;
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_snapshot is { } snapshot) Restore(snapshot);
        _disposed = true;
    }

    private SetupTerminalEvent ReadKeyEvent(int initialWidth, int initialHeight)
    {
        ConsoleKeyInfo key;
        try { key = Console.ReadKey(intercept: true); }
        catch (IOException) { return SetupTerminalEvent.DriverError(); }
        catch (InvalidOperationException) { return SetupTerminalEvent.DriverError(); }
        if (!TryDimensions(out int width, out int height)) return SetupTerminalEvent.ResizeFailure();
        return width != initialWidth || height != initialHeight
            ? SetupTerminalEvent.ResizeChanged() : MapKey(key);
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs args)
    {
        args.Cancel = true;
        Publish(SetupTerminalEvent.CancelSignal());
    }

    private void RegisterSignals()
    {
        if (OperatingSystem.IsWindows()) return;
        TryRegister(PosixSignal.SIGTERM, SetupTerminalEvent.TerminationSignal());
        TryRegister(PosixSignal.SIGHUP, SetupTerminalEvent.TerminationSignal());
        TryRegister(PosixSignal.SIGQUIT, SetupTerminalEvent.TerminationSignal());
        TryRegister(PosixSignal.SIGTSTP, SetupTerminalEvent.Suspend());
        TryRegister(PosixSignal.SIGWINCH, SetupTerminalEvent.ResizeChanged());
    }

    private void TryRegister(PosixSignal signal, SetupTerminalEvent terminalEvent)
    {
        try
        {
            _signals.Add(PosixSignalRegistration.Create(signal, context =>
            {
                context.Cancel = true;
                Publish(terminalEvent);
            }));
        }
        catch (PlatformNotSupportedException) { }
        catch (ArgumentException) { }
    }

    private void Publish(SetupTerminalEvent value)
    {
        lock (_coordination)
        {
            if (_activeRead is not null && _activeRead.TryComplete(value)) return;
            _pending.Enqueue(value);
        }
    }

    private static bool TryDimensions(out int width, out int height)
    {
        try
        {
            width = Console.WindowWidth;
            height = Console.WindowHeight;
            return true;
        }
        catch (IOException) { }
        catch (InvalidOperationException) { }
        width = 0;
        height = 0;
        return false;
    }

    private static SetupTerminalEvent MapKey(ConsoleKeyInfo key)
    {
        if ((key.Modifiers & ConsoleModifiers.Control) != 0) return SetupTerminalEvent.UnsupportedKey();
        return key.Key switch
        {
            ConsoleKey.Backspace => SetupTerminalEvent.Backspace(),
            ConsoleKey.Enter => SetupTerminalEvent.Enter(),
            ConsoleKey.Escape => SetupTerminalEvent.Escape(),
            ConsoleKey.Delete or ConsoleKey.Home or ConsoleKey.End or ConsoleKey.LeftArrow or ConsoleKey.RightArrow
                or ConsoleKey.UpArrow or ConsoleKey.DownArrow or ConsoleKey.PageUp or ConsoleKey.PageDown
                or ConsoleKey.Insert or ConsoleKey.Tab => SetupTerminalEvent.UnsupportedKey(),
            _ when !char.IsControl(key.KeyChar) && !char.IsSurrogate(key.KeyChar) => SetupTerminalEvent.Character(key.KeyChar),
            _ => SetupTerminalEvent.UnsupportedKey(),
        };
    }
}
