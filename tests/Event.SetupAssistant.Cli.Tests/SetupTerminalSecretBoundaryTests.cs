// ABOUTME: Proves the SA-430 terminal secret boundary through deterministic driver events and state outcomes.
// ABOUTME: Covers TTY admission, filename-first gating, cleanup, Core parity, protected output, and value-free results.

using System.Security.Cryptography;
using ISLAMU.Event.Setup.Core;
using ISLAMU.Event.Setup.Core.Environment;
using ISLAMU.Event.SetupAssistant.Cli;
using ISLAMU.Event.SetupAssistant.Cli.Tui;

namespace ISLAMU.SetupAssistant.Cli.Tests;

public sealed partial class SetupTerminalSecretBoundaryTests
{
    private const string Sentinel = "S3ntinel-safe_42";
    private const string SafeFileName = "setup.env";

    [Test]
    public async Task SecretEntryRequiresExactlyAllSixSafeTerminalFacts()
    {
        for (int bits = 0; bits < 64; bits++)
        {
            var capabilities = new SetupCliTerminalCapabilities(
                (bits & 1) != 0, (bits & 2) != 0, (bits & 4) != 0,
                (bits & 8) != 0, (bits & 16) != 0, (bits & 32) != 0, false);
            using var driver = new SetupTerminalFakeDriver(capabilities, WorkflowEvents('m', Sentinel));
            using var session = new SetupTerminalSession(driver, new SetupTerminalFakeProtectedWriter());
            SetupTerminalResult result = session.Run(65_536, 4 * 1024 * 1024);
            bool allowed = (bits & 7) == 7 && (bits & 56) == 0;

            await Assert.That(result.Outcome == SetupTerminalOutcome.Completed).IsEqualTo(allowed).Because($"facts={bits}");
            await Assert.That(driver.KeysRead > 0).IsEqualTo(allowed).Because($"facts={bits}");
            await Assert.That(driver.IsExactlyRestored).IsTrue();
        }
    }

    [Test]
    public async Task MachineAndUnavailableProtectedOutputBlockBeforeAnyKeyRead()
    {
        using var machineDriver = SafeDriver(WorkflowEvents('m', Sentinel));
        var app = new SetupCliApplication(new SetupTerminalWorkflow(machineDriver));
        SetupCliExitCode machineExit = app.Run(Invocation(["tui", "--machine"], SetupCliMode.Machine));
        using var unavailableDriver = SafeDriver(WorkflowEvents('m', Sentinel));
        using var unavailableSession = new SetupTerminalSession(
            unavailableDriver, new SetupTerminalFakeProtectedWriter(isAvailable: false));
        SetupTerminalResult unavailable = unavailableSession.Run(256, 1024);

        await Assert.That(machineExit).IsEqualTo(SetupCliExitCode.Blocked);
        await Assert.That(machineDriver.KeysRead).IsEqualTo(0);
        await Assert.That(unavailable.Outcome).IsEqualTo(SetupTerminalOutcome.Blocked);
        await Assert.That(unavailable.DiagnosticCode).IsEqualTo("protected-output-unavailable");
        await Assert.That(unavailableDriver.KeysRead).IsEqualTo(0);
    }

    [Test]
    public async Task InvalidFilenameBlocksBeforeSecretReadOrGeneration()
    {
        SetupTerminalEvent[] events =
        [SetupTerminalEvent.Character('/'), SetupTerminalEvent.Character('g'), SetupTerminalEvent.Character('x')];
        var writer = new SetupTerminalFakeProtectedWriter();
        using var driver = SafeDriver(events);
        using var session = new SetupTerminalSession(driver, writer);
        SetupTerminalResult result = session.Run(256, 1024);

        await Assert.That(result.Outcome).IsEqualTo(SetupTerminalOutcome.Blocked);
        await Assert.That(result.DiagnosticCode).IsEqualTo("terminal-output-name-invalid");
        await Assert.That(driver.KeysRead).IsEqualTo(1);
        await Assert.That(writer.WriteCompleted).IsFalse();
        await Assert.That(session.State.PublicFileNameCharacterCount).IsEqualTo(0);
    }

    [Test]
    public async Task EveryOutcomeClearsBothBuffersAndRestoresSession()
    {
        var vectors = new Dictionary<string, SetupTerminalEvent[]>(StringComparer.Ordinal)
        {
            ["success"] = WorkflowEvents('m', Sentinel),
            ["escape"] = [.. FileNameEvents(), SetupTerminalEvent.Character('m'), SetupTerminalEvent.Character('x'), SetupTerminalEvent.Escape()],
            ["validation"] = [.. FileNameEvents(), SetupTerminalEvent.Character('m'), SetupTerminalEvent.Enter()],
            ["driver-error"] = [.. FileNameEvents(), SetupTerminalEvent.Character('m'), SetupTerminalEvent.Character('x'), SetupTerminalEvent.DriverError()],
            ["cancel-signal"] = [.. FileNameEvents(), SetupTerminalEvent.Character('m'), SetupTerminalEvent.Character('x'), SetupTerminalEvent.CancelSignal()],
            ["termination"] = [.. FileNameEvents(), SetupTerminalEvent.Character('m'), SetupTerminalEvent.Character('x'), SetupTerminalEvent.TerminationSignal()],
            ["suspend"] = [.. FileNameEvents(), SetupTerminalEvent.Character('m'), SetupTerminalEvent.Character('x'), SetupTerminalEvent.Suspend()],
            ["resize"] = [.. FileNameEvents(), SetupTerminalEvent.Character('m'), SetupTerminalEvent.Character('x'), SetupTerminalEvent.ResizeChanged()],
            ["resize-failure"] = [.. FileNameEvents(), SetupTerminalEvent.Character('m'), SetupTerminalEvent.Character('x'), SetupTerminalEvent.ResizeFailure()],
            ["navigation"] = [.. FileNameEvents(), SetupTerminalEvent.Character('m'), SetupTerminalEvent.Character('x'), SetupTerminalEvent.NavigationAway(), SetupTerminalEvent.NavigationBack(), .. WorkflowEvents('m', "fresh")],
            ["filename-cancel"] = [SetupTerminalEvent.Character('n'), SetupTerminalEvent.Escape()],
            ["filename-signal"] = [SetupTerminalEvent.Character('n'), SetupTerminalEvent.TerminationSignal()],
        };

        foreach ((string name, SetupTerminalEvent[] events) in vectors)
        {
            using var driver = SafeDriver(events);
            using var session = new SetupTerminalSession(driver, new SetupTerminalFakeProtectedWriter());
            SetupTerminalResult result = session.Run(65_536, 4 * 1024 * 1024);
            session.Dispose();
            session.Dispose();

            await Assert.That(session.State.SecretCharacterCount).IsEqualTo(0).Because(name);
            await Assert.That(session.State.PublicFileNameCharacterCount).IsEqualTo(0).Because(name);
            await Assert.That(driver.IsExactlyRestored).IsTrue().Because(name);
            await Assert.That(SafeProjection(driver, result)).DoesNotContain(Sentinel, StringComparison.Ordinal);
            await Assert.That(SafeProjection(driver, result)).DoesNotContain(SafeFileName, StringComparison.Ordinal);
        }
    }

    [Test]
    public async Task KeyboardEditingIsMaskedBoundedAndReadinessIsTruthfullyIncomplete()
    {
        using var edited = SafeDriver([.. FileNameEvents(), SetupTerminalEvent.Character('m'),
            SetupTerminalEvent.Character('a'), SetupTerminalEvent.Character('é'), SetupTerminalEvent.Backspace(),
            SetupTerminalEvent.Character('z'), SetupTerminalEvent.Enter()]);
        using var exact = SafeDriver(WorkflowEvents('m', "ab"));
        using var plusOne = SafeDriver(WorkflowEvents('m', "abc"));
        using var editedSession = new SetupTerminalSession(edited, new SetupTerminalFakeProtectedWriter(), 3);
        using var exactSession = new SetupTerminalSession(exact, new SetupTerminalFakeProtectedWriter(), 2);
        using var plusSession = new SetupTerminalSession(plusOne, new SetupTerminalFakeProtectedWriter(), 2);
        SetupTerminalResult completed = editedSession.Run(256, 1024);

        await Assert.That(completed.Outcome).IsEqualTo(SetupTerminalOutcome.Completed);
        await Assert.That(completed.Readiness).IsEqualTo(SetupTerminalReadiness.Incomplete);
        await Assert.That(completed.MissingCount).IsEqualTo(1);
        await Assert.That(exactSession.Run(256, 1024).Outcome).IsEqualTo(SetupTerminalOutcome.Completed);
        await Assert.That(plusSession.Run(256, 1024).DiagnosticCode).IsEqualTo("terminal-secret-bound-exceeded");
        await Assert.That(string.Concat(edited.Writes)).DoesNotContain("az", StringComparison.Ordinal);
    }

    [Test]
    public async Task ProtectedWriteAndSessionMetadataHaveExactCoreParityWithoutDecodedArtifactString()
    {
        var writer = new SetupTerminalFakeProtectedWriter();
        using var driver = SafeDriver(WorkflowEvents('m', Sentinel));
        using var session = new SetupTerminalSession(driver, writer);
        SetupTerminalResult actual = session.Run(65_536, 4 * 1024 * 1024);
        var context = new EnvironmentActivationContext("standalone", ["platform"], ["environment", "local", "sqlite"]);
        DotenvCompositionResult composition = DotenvComposer.ComposeWithSecrets(CanonicalEnvironmentCatalogue.Catalogue,
            context, [new DotenvEntry("SETUP_SECRET", Sentinel, DotenvEntryKind.LocalHumanValue, true, DotenvProvenance.UserInput)]);
        DotenvRenderResult rendered = DotenvCodec.Render(composition.Document, true);
        byte[] expectedBytes = rendered.Bytes.ToArray();
        try
        {
            await Assert.That(actual.Digest).IsEqualTo(ArtifactDigest.Compute(expectedBytes).Value);
            await Assert.That(actual.Readiness.ToString()).IsEqualTo(composition.Readiness.State.ToString());
            await Assert.That(expectedBytes.Count(value => value == (byte)'\n')).IsEqualTo(2);
        }
        finally { CryptographicOperations.ZeroMemory(expectedBytes); }

        await Assert.That(writer.WriteCompleted).IsTrue();
        await Assert.That(writer.BufferIsCleared).IsTrue();
        await Assert.That(SafeProjection(driver, actual) + writer).DoesNotContain(Sentinel, StringComparison.Ordinal);
        await Assert.That(SafeProjection(driver, actual) + writer).DoesNotContain(SafeFileName, StringComparison.Ordinal);
        await Assert.That(driver.History.Concat(driver.Autosave).Concat(driver.Clipboard)).IsEmpty();
    }

    [Test]
    public async Task ProtectedWriterExceptionFailsClosedAndClearsFilenameAndSecretState()
    {
        using var driver = SafeDriver(WorkflowEvents('m', Sentinel));
        using var session = new SetupTerminalSession(driver,
            new SetupTerminalFakeProtectedWriter(throwOnWrite: true));
        SetupTerminalResult result = session.Run(256, 1024 * 1024);

        await Assert.That(result.Outcome).IsEqualTo(SetupTerminalOutcome.Blocked);
        await Assert.That(result.DiagnosticCode).IsEqualTo("protected-output-unavailable");
        await Assert.That(session.State.SecretCharacterCount).IsEqualTo(0);
        await Assert.That(session.State.PublicFileNameCharacterCount).IsEqualTo(0);
        await Assert.That(driver.IsExactlyRestored).IsTrue();
        await Assert.That(SafeProjection(driver, result)).DoesNotContain(Sentinel, StringComparison.Ordinal);
        await Assert.That(SafeProjection(driver, result)).DoesNotContain(SafeFileName, StringComparison.Ordinal);
    }

    [Test]
    public async Task GenerationRemainsProtectedAndNonColorOutputIsBounded()
    {
        var writer = new SetupTerminalFakeProtectedWriter();
        using var driver = SafeDriver([.. FileNameEvents(), SetupTerminalEvent.Character('g')]);
        using var session = new SetupTerminalSession(driver, writer);
        SetupTerminalResult result = session.Run(128, 1024 * 1024);

        await Assert.That(result.Outcome).IsEqualTo(SetupTerminalOutcome.Completed);
        await Assert.That(result.Readiness).IsEqualTo(SetupTerminalReadiness.Incomplete);
        await Assert.That(writer.WriteCompleted).IsTrue();
        await Assert.That(driver.Writes.Sum(value => value.Length)).IsLessThanOrEqualTo(128);
        await Assert.That(driver.Writes.SelectMany(value => value).Any(value => value is '\u001b' or '\r' or '\u007f')).IsFalse();
    }

    [Test]
    public async Task TextTuiReturnsIncompleteWhenProtectedArtifactStillHasMissingPublicRequirements()
    {
        var writer = new SetupTerminalFakeProtectedWriter();
        using var driver = SafeDriver(WorkflowEvents('m', Sentinel));
        var application = new SetupCliApplication(new SetupTerminalWorkflow(driver, writer));

        SetupCliExitCode exit = application.Run(Invocation(["tui"], SetupCliMode.Text));

        await Assert.That(exit).IsEqualTo(SetupCliExitCode.Incomplete);
        await Assert.That(writer.WriteCompleted).IsTrue();
        await Assert.That(driver.IsExactlyRestored).IsTrue();
        await Assert.That(string.Concat(driver.Writes)).DoesNotContain(Sentinel, StringComparison.Ordinal);
        await Assert.That(string.Concat(driver.Writes)).DoesNotContain(SafeFileName, StringComparison.Ordinal);
    }

    private static SetupTerminalEvent[] WorkflowEvents(char mode, string value) =>
        [.. FileNameEvents(), SetupTerminalEvent.Character(mode), .. value.Select(SetupTerminalEvent.Character), SetupTerminalEvent.Enter()];

    private static SetupTerminalEvent[] FileNameEvents() =>
        [.. SafeFileName.Select(SetupTerminalEvent.Character), SetupTerminalEvent.Enter()];

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
