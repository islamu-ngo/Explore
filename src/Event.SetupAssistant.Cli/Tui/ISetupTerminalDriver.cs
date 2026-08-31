// ABOUTME: Defines the narrow interception driver and bounded ordinary-text output boundary.
// ABOUTME: Treats six terminal facts as a fail-closed predicate before any key is read.

using System.Text;

namespace ISLAMU.Event.SetupAssistant.Cli.Tui;

public interface ISetupTerminalDriver : IDisposable
{
    SetupCliTerminalCapabilities Capabilities { get; }
    bool InterceptionActive { get; }
    bool EchoSuppressedByIntercept { get; }
    SetupTerminalDriverSnapshot Snapshot();
    void BeginInterception(SetupTerminalDriverSnapshot snapshot);
    SetupTerminalEvent ReadEvent();
    void WriteBounded(string value, int maximumCharacters, int maximumBytes);
    void Restore(SetupTerminalDriverSnapshot snapshot);
}

public static class SetupTerminalCapabilityPolicy
{
    public static bool AllowsSecretEntry(SetupCliTerminalCapabilities capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        return capabilities.StdinIsTty && capabilities.StdoutIsTty && capabilities.StderrIsTty
            && !capabilities.InputRedirected && !capabilities.OutputRedirected && !capabilities.ErrorRedirected;
    }
}

public static class SetupTerminalText
{
    public static void Validate(string value, int maximumCharacters, int maximumBytes)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (maximumCharacters < 1 || maximumBytes < 1 || value.Length > maximumCharacters
            || Encoding.UTF8.GetByteCount(value) > maximumBytes)
            throw new IOException("terminal-output-bound");
        foreach (char character in value)
        {
            if (character == '\n') continue;
            if (character < ' ' || character == '\u007f' || character == '\u001b')
                throw new IOException("terminal-output-control");
        }
    }
}

public interface ISetupTerminalWorkflow
{
    SetupTerminalResult Run(SetupCliInvocation invocation);
}
