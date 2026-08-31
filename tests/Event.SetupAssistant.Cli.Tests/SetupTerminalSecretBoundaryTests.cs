// ABOUTME: Proves the SA-430 terminal secret boundary through deterministic driver events and state outcomes.
// ABOUTME: Covers the complete TTY predicate, keyboard behavior, cleanup, bounded output, and value-free results.

using System.Text;
using ISLAMU.Event.SetupAssistant.Cli;
using ISLAMU.Event.SetupAssistant.Cli.Tui;

namespace ISLAMU.SetupAssistant.Cli.Tests;

public sealed class SetupTerminalSecretBoundaryTests
{
    private const string Sentinel = "S3ntinel-safe_42";

    [Test]
    public async Task SecretEntryRequiresExactlyAllSixSafeTerminalFacts()
    {
        for (int bits = 0; bits < 64; bits++)
        {
            var capabilities = new SetupCliTerminalCapabilities(
                (bits & 1) != 0, (bits & 2) != 0, (bits & 4) != 0,
                (bits & 8) != 0, (bits & 16) != 0, (bits & 32) != 0, false);
            using var driver = new SetupTerminalFakeDriver(capabilities, Events('m', Sentinel));
            using var session = new SetupTerminalSession(driver);
            SetupTerminalResult result = session.Run(65_536, 4 * 1024 * 1024);
            bool allowed = (bits & 7) == 7 && (bits & 56) == 0;

            await Assert.That(result.Outcome == SetupTerminalOutcome.Completed).IsEqualTo(allowed).Because($"facts={bits}");
            await Assert.That(driver.KeysRead > 0).IsEqualTo(allowed).Because($"facts={bits}");
            await Assert.That(driver.IsExactlyRestored).IsTrue();
            await Assert.That(driver.InterceptionActive).IsFalse();
        }
    }

    [Test]
    public async Task MachineModeIsBlockedBeforeWorkflowAndNeverReadsKeys()
    {
        using var driver = SafeDriver(Events('m', Sentinel));
        var workflow = new SetupTerminalWorkflow(driver);
        var app = new SetupCliApplication(workflow);
        SetupCliExitCode exit = app.Run(Invocation(["tui", "--machine"], SetupCliMode.Machine));

        await Assert.That(exit).IsEqualTo(SetupCliExitCode.Blocked);
        await Assert.That(driver.KeysRead).IsEqualTo(0);
        await Assert.That(driver.IsExactlyRestored).IsTrue();
    }

    [Test]
    public async Task EveryTerminalOutcomeClearsOwnedStateAndRestoresSession()
    {
        var vectors = new Dictionary<string, SetupTerminalEvent[]>(StringComparer.Ordinal)
        {
            ["success"] = Events('m', Sentinel),
            ["escape"] = [SetupTerminalEvent.Character('m'), SetupTerminalEvent.Character('x'), SetupTerminalEvent.Escape()],
            ["validation"] = [SetupTerminalEvent.Character('m'), SetupTerminalEvent.Enter()],
            ["driver-error"] = [SetupTerminalEvent.Character('m'), SetupTerminalEvent.Character('x'), SetupTerminalEvent.DriverError()],
            ["cancel-signal"] = [SetupTerminalEvent.Character('m'), SetupTerminalEvent.Character('x'), SetupTerminalEvent.CancelSignal()],
            ["termination"] = [SetupTerminalEvent.Character('m'), SetupTerminalEvent.Character('x'), SetupTerminalEvent.TerminationSignal()],
            ["suspend"] = [SetupTerminalEvent.Character('m'), SetupTerminalEvent.Character('x'), SetupTerminalEvent.Suspend()],
            ["resize-failure"] = [SetupTerminalEvent.Character('m'), SetupTerminalEvent.Character('x'), SetupTerminalEvent.ResizeFailure()],
            ["resize-change"] = [SetupTerminalEvent.Character('m'), SetupTerminalEvent.Character('x'), SetupTerminalEvent.ResizeChanged()],
            ["navigation"] = [SetupTerminalEvent.Character('m'), SetupTerminalEvent.Character('x'), SetupTerminalEvent.NavigationAway(), SetupTerminalEvent.NavigationBack(), .. Events('m', "fresh")]
        };

        foreach ((string name, SetupTerminalEvent[] events) in vectors)
        {
            using var driver = SafeDriver(events);
            using var session = new SetupTerminalSession(driver);
            SetupTerminalResult result = session.Run(65_536, 4 * 1024 * 1024);
            session.Dispose();
            session.Dispose();

            await Assert.That(session.State.SecretCharacterCount).IsEqualTo(0).Because(name);
            await Assert.That(driver.IsExactlyRestored).IsTrue().Because(name);
            await Assert.That(driver.InterceptionActive).IsFalse().Because(name);
            await Assert.That(SafeProjection(driver, result)).DoesNotContain(Sentinel, StringComparison.Ordinal);
        }
    }

    [Test]
    public async Task KeyboardEditingIsDeterministicMaskedAndBounded()
    {
        using var edited = SafeDriver([
            SetupTerminalEvent.Character('m'), SetupTerminalEvent.Character('a'), SetupTerminalEvent.Character('é'),
            SetupTerminalEvent.Backspace(), SetupTerminalEvent.Character('z'), SetupTerminalEvent.Enter()]);
        using var exact = SafeDriver([SetupTerminalEvent.Character('m'), SetupTerminalEvent.Character('a'), SetupTerminalEvent.Character('b'), SetupTerminalEvent.Enter()]);
        using var plusOne = SafeDriver([SetupTerminalEvent.Character('m'), SetupTerminalEvent.Character('a'), SetupTerminalEvent.Character('b'), SetupTerminalEvent.Character('c')]);
        using var unsupported = SafeDriver([SetupTerminalEvent.Character('m'), SetupTerminalEvent.Character('a'), SetupTerminalEvent.UnsupportedKey()]);
        using var editedSession = new SetupTerminalSession(edited, maximumSecretCharacters: 3);
        using var exactSession = new SetupTerminalSession(exact, maximumSecretCharacters: 2);
        using var plusOneSession = new SetupTerminalSession(plusOne, maximumSecretCharacters: 2);
        using var unsupportedSession = new SetupTerminalSession(unsupported);
        SetupTerminalResult completed = editedSession.Run(256, 1024);

        await Assert.That(completed.Outcome).IsEqualTo(SetupTerminalOutcome.Completed);
        await Assert.That(exactSession.Run(256, 1024).Outcome).IsEqualTo(SetupTerminalOutcome.Completed);
        await Assert.That(plusOneSession.Run(256, 1024).DiagnosticCode).IsEqualTo("terminal-secret-bound-exceeded");
        await Assert.That(unsupportedSession.Run(256, 1024).DiagnosticCode).IsEqualTo("terminal-key-unsupported");
        await Assert.That(string.Concat(edited.Writes).Any(character => character is '\u001b' or '\r' or '\u007f')).IsFalse();
        await Assert.That(string.Concat(edited.Writes)).DoesNotContain("az", StringComparison.Ordinal);
    }

    [Test]
    public async Task ProtectedWriteUsesCoreBytesOnceAndLeavesNoValueInPublicSurfaces()
    {
        var writer = new SetupTerminalFakeProtectedWriter();
        using var driver = SafeDriver(Events('m', Sentinel));
        using var session = new SetupTerminalSession(driver, writer);
        SetupTerminalResult result = session.Run(65_536, 4 * 1024 * 1024);
        string projection = SafeProjection(driver, result) + writer;

        await Assert.That(result.Outcome).IsEqualTo(SetupTerminalOutcome.Completed);
        await Assert.That(result.Readiness).IsEqualTo(SetupTerminalReadiness.Ready);
        await Assert.That(writer.WriteCompleted).IsTrue();
        await Assert.That(writer.BufferIsCleared).IsTrue();
        await Assert.That(projection).DoesNotContain(Sentinel, StringComparison.Ordinal);
        await Assert.That(driver.History).IsEmpty();
        await Assert.That(driver.Autosave).IsEmpty();
        await Assert.That(driver.Clipboard).IsEmpty();
    }

    [Test]
    public async Task AccessibilityContractAndNonColorOutputAreTruthfulAndUsable()
    {
        SetupTerminalAccessibility accessibility = SetupTerminalAccessibility.Current;
        using var driver = SafeDriver(Events('m', "valid"));
        using var session = new SetupTerminalSession(driver);
        SetupTerminalResult result = session.Run(128, 1024);

        await Assert.That(accessibility.Supported).IsEqualTo(
            SetupTerminalAccessibilityFeature.KeyboardBasicUnicode | SetupTerminalAccessibilityFeature.NonColorStatus |
            SetupTerminalAccessibilityFeature.MaskedInput);
        await Assert.That(accessibility.Unverified).HasFlag(SetupTerminalAccessibilityFeature.ScreenReaderSemantics);
        await Assert.That(accessibility.Unverified).HasFlag(SetupTerminalAccessibilityFeature.Braille);
        await Assert.That(accessibility.Unverified).HasFlag(SetupTerminalAccessibilityFeature.ImeComposition);
        await Assert.That(accessibility.Unverified).HasFlag(SetupTerminalAccessibilityFeature.UnicodeGraphemeEditing);
        await Assert.That(accessibility.Unverified).HasFlag(SetupTerminalAccessibilityFeature.TerminalScrollbackErasure);
        await Assert.That(accessibility.Unverified).HasFlag(SetupTerminalAccessibilityFeature.RightToLeft);
        await Assert.That(accessibility.Unverified).HasFlag(SetupTerminalAccessibilityFeature.ScalableLayout);
        await Assert.That(accessibility.Unverified).HasFlag(SetupTerminalAccessibilityFeature.OsWideAccessibility);
        await Assert.That(result.Outcome).IsEqualTo(SetupTerminalOutcome.Completed);
        await Assert.That(driver.Writes.Sum(value => value.Length)).IsLessThanOrEqualTo(128);
    }

    private static SetupTerminalEvent[] Events(char mode, string value) =>
        [SetupTerminalEvent.Character(mode), .. value.Select(SetupTerminalEvent.Character), SetupTerminalEvent.Enter()];

    private static SetupTerminalFakeDriver SafeDriver(IEnumerable<SetupTerminalEvent> events) => new(
        new SetupCliTerminalCapabilities(true, true, true, false, false, false, false), events);

    private static SetupCliInvocation Invocation(string[] args, SetupCliMode mode) => new(args, mode,
        new SetupCliIo(new EmptyInput(), new NullWriter(), new NullWriter(), 65_536, 4 * 1024 * 1024),
        new SetupCliTerminalCapabilities(true, true, true, false, false, false, false), new SetupCliEnvironmentPresence([]));

    private static string SafeProjection(SetupTerminalFakeDriver driver, SetupTerminalResult result) =>
        string.Concat(driver.Writes) + string.Concat(driver.EventLog) + result + result.DiagnosticCode + result.Digest;

    private sealed class EmptyInput : ISetupCliInput { public ReadOnlyMemory<byte> Read(string path, int maximumBytes) => ReadOnlyMemory<byte>.Empty; }
    private sealed class NullWriter : ISetupCliWriter { public void Write(string path, ReadOnlyMemory<byte> bytes, int maximumBytes) { } }
}
