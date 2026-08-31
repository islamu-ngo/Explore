// ABOUTME: Provides a behavior-preserving terminal driver with explicit events and inspectable restored state.
// ABOUTME: Retains only public status/event metadata and no secret history, autosave, or clipboard values.

using ISLAMU.Event.SetupAssistant.Cli;
using ISLAMU.Event.SetupAssistant.Cli.Tui;

namespace ISLAMU.SetupAssistant.Cli.Tests;

internal sealed class SetupTerminalFakeDriver : ISetupTerminalDriver
{
    private readonly Queue<SetupTerminalEvent> _events;
    private readonly SetupTerminalDriverSnapshot _initial = new(false, false, false, 120, 40);
    private SetupTerminalDriverSnapshot _state;
    private bool _disposed;

    internal SetupTerminalFakeDriver(SetupCliTerminalCapabilities capabilities, IEnumerable<SetupTerminalEvent> events)
    {
        Capabilities = capabilities;
        _events = new Queue<SetupTerminalEvent>(events);
        _state = _initial;
    }

    public SetupCliTerminalCapabilities Capabilities { get; }
    public bool InterceptionActive => _state.InterceptionActive;
    public bool EchoSuppressedByIntercept => _state.EchoSuppressedByIntercept;
    public List<string> Writes { get; } = [];
    public List<string> EventLog { get; } = [];
    public List<string> History { get; } = [];
    public List<string> Autosave { get; } = [];
    public List<string> Clipboard { get; } = [];
    public int KeysRead { get; private set; }
    public bool IsExactlyRestored => _state == _initial;

    public SetupTerminalDriverSnapshot Snapshot() => _state;

    public void BeginInterception(SetupTerminalDriverSnapshot snapshot)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _state = snapshot with { InterceptionActive = true, EchoSuppressedByIntercept = true };
    }

    public SetupTerminalEvent ReadEvent()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        KeysRead++;
        if (_events.Count == 0) return SetupTerminalEvent.DriverError();
        SetupTerminalEvent value = _events.Dequeue();
        EventLog.Add(value.Kind.ToString());
        return value;
    }

    public void WriteBounded(string value, int maximumCharacters, int maximumBytes)
    {
        SetupTerminalText.Validate(value, maximumCharacters, maximumBytes);
        Writes.Add(value);
    }

    public void Restore(SetupTerminalDriverSnapshot snapshot) => _state = snapshot;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _state = _initial;
    }
}

internal sealed class SetupTerminalFakeProtectedWriter(
    bool isAvailable = true, bool throwOnWrite = false) : ISetupTerminalProtectedWriter
{
    private byte[] _observed = [];
    public bool IsAvailable { get; } = isAvailable;
    public bool WriteCompleted { get; private set; }
    public bool FileNameAccepted { get; private set; }
    public bool BufferIsCleared => _observed.All(value => value == 0);

    public SetupTerminalProtectedWriteResult WriteCreateNew(
        string validatedFileName, ReadOnlyMemory<byte> bytes, int maximumBytes)
    {
        FileNameAccepted = SetupPublicFileNameBuffer.IsSafe(validatedFileName);
        if (throwOnWrite) throw new IOException("synthetic-protected-write-failure");
        if (!FileNameAccepted || bytes.Length > maximumBytes) return SetupTerminalProtectedWriteResult.Blocked;
        _observed = bytes.ToArray();
        WriteCompleted = true;
        Array.Clear(_observed);
        return SetupTerminalProtectedWriteResult.Written;
    }

    public override string ToString() => $"{nameof(SetupTerminalFakeProtectedWriter)}:Completed={WriteCompleted}";
}
