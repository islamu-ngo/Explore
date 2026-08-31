// ABOUTME: Runs the bounded linear secret workflow over explicit events and Setup Core composition.
// ABOUTME: Clears interception, secret characters, and rendered byte copies on every terminal outcome.

using System.Security.Cryptography;
using ISLAMU.Event.Setup.Core;
using ISLAMU.Event.Setup.Core.Environment;

namespace ISLAMU.Event.SetupAssistant.Cli.Tui;

public sealed class SetupTerminalSession : IDisposable
{
    private readonly ISetupTerminalDriver _driver;
    private readonly ISetupTerminalProtectedWriter? _protectedWriter;
    private readonly SetupSecretCharBuffer _secret;
    private bool _disposed;
    private int _remainingCharacters;
    private int _remainingBytes;

    public SetupTerminalSession(ISetupTerminalDriver driver, ISetupTerminalProtectedWriter? protectedWriter = null,
        int maximumSecretCharacters = DotenvCodec.MaximumValueUtf8Bytes)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _protectedWriter = protectedWriter;
        _secret = new SetupSecretCharBuffer(maximumSecretCharacters);
        State = new SetupTerminalState(false, 0, null);
    }

    public SetupTerminalState State { get; private set; }

    public SetupTerminalResult Run(int maximumCharacters, int maximumBytes)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!SetupTerminalCapabilityPolicy.AllowsSecretEntry(_driver.Capabilities))
            return Finish(SetupTerminalOutcome.Blocked, "interactive-terminal-required");
        _remainingCharacters = maximumCharacters;
        _remainingBytes = maximumBytes;
        SetupTerminalDriverSnapshot snapshot;
        try { snapshot = _driver.Snapshot(); }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        { return Finish(SetupTerminalOutcome.Failed, "terminal-snapshot-failed"); }

        try
        {
            _driver.BeginInterception(snapshot);
            State = new SetupTerminalState(true, 0, null);
            Write("setup terminal\n");
            Write("access keyboard non-color masked\n");
            Write("m manual g generate escape cancel\n");
            return SelectMode(maximumBytes);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            return Finish(SetupTerminalOutcome.Failed, "terminal-driver-failed");
        }
        finally
        {
            _secret.Clear();
            State = State with { Active = false, SecretCharacterCount = 0 };
            try { _driver.Restore(snapshot); }
            catch (Exception exception) when (exception is IOException or InvalidOperationException)
            { State = State with { Outcome = SetupTerminalOutcome.Failed }; }
        }
    }

    private SetupTerminalResult SelectMode(int maximumBytes)
    {
        while (true)
        {
            SetupTerminalEvent input = _driver.ReadEvent();
            if (input.Kind == SetupTerminalEventKind.Character && input.CharacterValue is 'm' or 'M')
            {
                Write("secret masked\n");
                return ReadManual(maximumBytes);
            }
            if (input.Kind == SetupTerminalEventKind.Character && input.CharacterValue is 'g' or 'G')
                return Generate(maximumBytes);
            if (input.Kind == SetupTerminalEventKind.NavigationAway)
            {
                _secret.Clear();
                SetupTerminalEvent back = _driver.ReadEvent();
                if (back.Kind == SetupTerminalEventKind.NavigationBack) continue;
                return TerminalEventOutcome(back);
            }
            if (input.Kind == SetupTerminalEventKind.Escape)
                return Finish(SetupTerminalOutcome.Cancelled, "terminal-cancelled");
            return TerminalEventOutcome(input);
        }
    }

    private SetupTerminalResult ReadManual(int maximumBytes)
    {
        while (true)
        {
            SetupTerminalEvent input = _driver.ReadEvent();
            switch (input.Kind)
            {
                case SetupTerminalEventKind.Character:
                    if (!_secret.TryAppend(input.CharacterValue))
                        return Finish(SetupTerminalOutcome.Failed, "terminal-secret-bound-exceeded");
                    State = State with { SecretCharacterCount = _secret.Count };
                    Write("mask *\n");
                    break;
                case SetupTerminalEventKind.Backspace:
                    _secret.Backspace();
                    State = State with { SecretCharacterCount = _secret.Count };
                    Write("mask reduced\n");
                    break;
                case SetupTerminalEventKind.Enter:
                    if (_secret.Count == 0) return Finish(SetupTerminalOutcome.Failed, "terminal-secret-invalid");
                    string transient = _secret.CopyTransientValue();
                    return Compose(transient, DotenvEntryKind.LocalHumanValue, DotenvProvenance.UserInput, maximumBytes);
                case SetupTerminalEventKind.NavigationAway:
                    _secret.Clear();
                    State = State with { SecretCharacterCount = 0 };
                    SetupTerminalEvent back = _driver.ReadEvent();
                    return back.Kind == SetupTerminalEventKind.NavigationBack ? SelectMode(maximumBytes) : TerminalEventOutcome(back);
                default:
                    return TerminalEventOutcome(input);
            }
        }
    }

    private SetupTerminalResult Generate(int maximumBytes)
    {
        using LocalSecretGenerator generator = LocalSecretGenerator.Create();
        using LocalSecretGenerationResult generated = generator.Generate("SETUP_SECRET", LocalSecretGenerationProfile.OpaqueUrlSafe256);
        if (!generated.Succeeded) return Finish(SetupTerminalOutcome.Failed, "terminal-generation-failed");
        string transient = generated.Output!.CopyValue();
        return Compose(transient, DotenvEntryKind.GeneratedValueReference, DotenvProvenance.Generated, maximumBytes);
    }

    private SetupTerminalResult Compose(string transient, DotenvEntryKind kind, DotenvProvenance provenance, int maximumBytes)
    {
        byte[] renderedBytes = [];
        try
        {
            var context = new EnvironmentActivationContext("standalone", ["platform"], ["environment", "local", "sqlite"]);
            DotenvCompositionResult composition = DotenvComposer.ComposeWithSecrets(CanonicalEnvironmentCatalogue.Catalogue,
                context, [new DotenvEntry("SETUP_SECRET", transient, kind, true, provenance)]);
            DotenvRenderResult rendered = DotenvCodec.Render(composition.Document, true);
            if (!rendered.Succeeded) return Finish(SetupTerminalOutcome.Failed, "terminal-compose-failed", composition);
            renderedBytes = rendered.Bytes.ToArray();
            string digest = ArtifactDigest.Compute(renderedBytes).Value;
            SetupTerminalProtectedWriteResult write = SetupTerminalProtectedWriteResult.Blocked;
            if (_protectedWriter?.IsAvailable == true)
                write = _protectedWriter.WriteCreateNew(renderedBytes, maximumBytes);
            if (_protectedWriter is not null && write != SetupTerminalProtectedWriteResult.Written)
                return Finish(SetupTerminalOutcome.Blocked, "protected-output-unavailable", composition, digest, write);
            return Finish(SetupTerminalOutcome.Completed, "terminal-complete", composition, digest, write);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException)
        { return Finish(SetupTerminalOutcome.Failed, "terminal-compose-failed"); }
        finally
        {
            if (renderedBytes.Length > 0) CryptographicOperations.ZeroMemory(renderedBytes);
        }
    }

    private SetupTerminalResult TerminalEventOutcome(SetupTerminalEvent input) => input.Kind switch
    {
        SetupTerminalEventKind.Escape => Finish(SetupTerminalOutcome.Cancelled, "terminal-cancelled"),
        SetupTerminalEventKind.CancelSignal => Finish(SetupTerminalOutcome.Cancelled, "terminal-cancel-signal"),
        SetupTerminalEventKind.TerminationSignal => Finish(SetupTerminalOutcome.Cancelled, "terminal-termination-signal"),
        SetupTerminalEventKind.Suspend => Finish(SetupTerminalOutcome.Suspended, "terminal-suspended"),
        SetupTerminalEventKind.ResizeChanged or SetupTerminalEventKind.ResizeFailure => Finish(SetupTerminalOutcome.Failed, "terminal-resize-failed"),
        SetupTerminalEventKind.UnsupportedKey => Finish(SetupTerminalOutcome.Failed, "terminal-key-unsupported"),
        _ => Finish(SetupTerminalOutcome.Failed, "terminal-driver-failed"),
    };

    private SetupTerminalResult Finish(SetupTerminalOutcome outcome, string code,
        DotenvCompositionResult? composition = null, string? digest = null,
        SetupTerminalProtectedWriteResult write = SetupTerminalProtectedWriteResult.Blocked)
    {
        _secret.Clear();
        State = new SetupTerminalState(false, 0, outcome);
        SetupTerminalReadiness readiness = composition?.Readiness.State switch
        {
            DotenvReadinessState.Ready => SetupTerminalReadiness.Ready,
            DotenvReadinessState.Incomplete => SetupTerminalReadiness.Incomplete,
            DotenvReadinessState.Blocked => SetupTerminalReadiness.Blocked,
            _ => SetupTerminalReadiness.None,
        };
        return new SetupTerminalResult(outcome, code, digest, readiness,
            composition?.Readiness.Missing.Count ?? 0, composition?.Readiness.Blocked.Count ?? 0,
            write, SetupTerminalAccessibility.Current);
    }

    private void Write(string value)
    {
        SetupTerminalText.Validate(value, _remainingCharacters, _remainingBytes);
        _driver.WriteBounded(value, _remainingCharacters, _remainingBytes);
        _remainingCharacters -= value.Length;
        _remainingBytes -= System.Text.Encoding.UTF8.GetByteCount(value);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _secret.Dispose();
        _driver.Dispose();
        State = State with { Active = false, SecretCharacterCount = 0 };
    }
}
