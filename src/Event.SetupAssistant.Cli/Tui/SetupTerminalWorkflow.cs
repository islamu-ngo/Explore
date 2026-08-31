// ABOUTME: Adapts one injected terminal session to the CLI invocation bounds without changing machine commands.
// ABOUTME: Rejects capability mismatches before interception and returns only value-safe metadata.

namespace ISLAMU.Event.SetupAssistant.Cli.Tui;

public sealed class SetupTerminalWorkflow : ISetupTerminalWorkflow
{
    private readonly ISetupTerminalDriver _driver;
    private readonly ISetupTerminalProtectedWriter? _protectedWriter;

    public SetupTerminalWorkflow(ISetupTerminalDriver driver, ISetupTerminalProtectedWriter? protectedWriter = null)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _protectedWriter = protectedWriter;
    }

    public SetupTerminalResult Run(SetupCliInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        if (invocation.Mode == SetupCliMode.Machine
            || !SetupTerminalCapabilityPolicy.AllowsSecretEntry(invocation.Terminal)
            || !SetupTerminalCapabilityPolicy.AllowsSecretEntry(_driver.Capabilities))
            return new SetupTerminalResult(SetupTerminalOutcome.Blocked, "interactive-terminal-required", null,
                SetupTerminalReadiness.None, 0, 0, SetupTerminalProtectedWriteResult.Blocked,
                SetupTerminalAccessibility.Current);
        using var session = new SetupTerminalSession(_driver, _protectedWriter);
        return session.Run(invocation.Io.MaximumCharacters, invocation.Io.MaximumBytes);
    }
}

internal sealed class BlockedSetupTerminalWorkflow : ISetupTerminalWorkflow
{
    internal static BlockedSetupTerminalWorkflow Instance { get; } = new();
    public SetupTerminalResult Run(SetupCliInvocation invocation) => new(
        SetupTerminalOutcome.Blocked, "interactive-terminal-unavailable", null, SetupTerminalReadiness.None,
        0, 0, SetupTerminalProtectedWriteResult.Blocked, SetupTerminalAccessibility.Current);
}
