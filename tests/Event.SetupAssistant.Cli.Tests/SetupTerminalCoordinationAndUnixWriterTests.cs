// ABOUTME: Proves key-or-signal first-event coordination and late-reader safety without timing or real signals.
// ABOUTME: Verifies supported Unix protected output uses create-new owner-only files and rejects unsafe names.

using ISLAMU.Event.SetupAssistant.Cli.Tui;

namespace ISLAMU.SetupAssistant.Cli.Tests;

public sealed partial class SetupTerminalSecretBoundaryTests
{
    [Test]
    public async Task SignalCompletionWinsWithoutWaitingForBlockedReaderAndLateKeyIsHarmless()
    {
        using var started = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        using var readerReturned = new ManualResetEventSlim(false);
        using var coordinator = new SetupTerminalReadCoordinator();
        coordinator.Start(() =>
        {
            started.Set();
            release.Wait();
            readerReturned.Set();
            return SetupTerminalEvent.Character('x');
        });

        await Assert.That(started.Wait(TimeSpan.FromSeconds(5))).IsTrue();
        await Assert.That(coordinator.TryComplete(SetupTerminalEvent.TerminationSignal())).IsTrue();
        await Assert.That(coordinator.Wait().Kind).IsEqualTo(SetupTerminalEventKind.TerminationSignal);
        release.Set();
        await Assert.That(readerReturned.Wait(TimeSpan.FromSeconds(5))).IsTrue();
        await Assert.That(coordinator.TryComplete(SetupTerminalEvent.Character('y'))).IsFalse();
    }

    [Test]
    public async Task KeyCompletionWinsAndLaterSignalIsQueuedAsLosingCompletion()
    {
        using var release = new ManualResetEventSlim(false);
        using var coordinator = new SetupTerminalReadCoordinator();
        coordinator.Start(() =>
        {
            release.Wait();
            return SetupTerminalEvent.Enter();
        });
        release.Set();

        await Assert.That(coordinator.Wait().Kind).IsEqualTo(SetupTerminalEventKind.Enter);
        await Assert.That(coordinator.TryComplete(SetupTerminalEvent.Suspend())).IsFalse();
    }

    [Test]
    public async Task UnixProtectedWriterCreatesOwnerOnlyOnceAndPreservesExistingFile()
    {
        if (!(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())) return;
        string directory = Path.Combine(Path.GetTempPath(), $"event-setup-sa430-{Environment.ProcessId}");
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        Directory.CreateDirectory(directory);
        byte[] content = [0x41, 0x3D, 0x31, 0x0A];
        try
        {
            var writer = new UnixSetupTerminalProtectedWriter(directory);
            SetupTerminalProtectedWriteResult first = writer.WriteCreateNew("safe.env", content, 1024);
            SetupTerminalProtectedWriteResult second = writer.WriteCreateNew("safe.env", new byte[] { 0x42 }, 1024);
            string path = Path.Combine(directory, "safe.env");

            await Assert.That(first).IsEqualTo(SetupTerminalProtectedWriteResult.Written);
            await Assert.That(second).IsEqualTo(SetupTerminalProtectedWriteResult.Blocked);
            await Assert.That(File.GetUnixFileMode(path)).IsEqualTo(UnixFileMode.UserRead | UnixFileMode.UserWrite);
            await Assert.That(await File.ReadAllBytesAsync(path)).IsEquivalentTo(content);
            await Assert.That(writer.ToString()).DoesNotContain(directory, StringComparison.Ordinal);
        }
        finally
        {
            Array.Clear(content);
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task PublicFilenamePolicyRejectsTraversalSeparatorsControlsAndDash()
    {
        string[] rejected = ["-", ".", "..", "../x", "x/y", "x\\y", "x\ny", new string('a', 65)];
        foreach (string value in rejected)
            await Assert.That(SetupPublicFileNameBuffer.IsSafe(value)).IsFalse().Because("rejected filename vector");
        await Assert.That(SetupPublicFileNameBuffer.IsSafe(".env")).IsTrue();
        await Assert.That(SetupPublicFileNameBuffer.IsSafe("setup-local_1.env")).IsTrue();
    }
}
