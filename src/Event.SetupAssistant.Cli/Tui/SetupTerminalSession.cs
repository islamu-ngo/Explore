// ABOUTME: Runs the bounded filename-first linear secret workflow over explicit terminal events.
// ABOUTME: Clears public and secret buffers and restores interception on every terminal outcome.

using ISLAMU.Event.Setup.Core.Environment;

namespace ISLAMU.Event.SetupAssistant.Cli.Tui;

public sealed class SetupTerminalSession : IDisposable
{
    private readonly ISetupTerminalDriver _driver;
    private readonly ISetupTerminalProtectedWriter? _protectedWriter;
    private readonly SetupPublicFileNameBuffer _fileName = new();
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
        if (_protectedWriter?.IsAvailable != true)
            return Finish(SetupTerminalOutcome.Blocked, "protected-output-unavailable");
        _remainingCharacters = maximumCharacters;
        _remainingBytes = maximumBytes;
        SetupTerminalDriverSnapshot snapshot;
        try { snapshot = _driver.Snapshot(); }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        { return Finish(SetupTerminalOutcome.Failed, "terminal-snapshot-failed"); }

        SetupTerminalResult result;
        try
        {
            _driver.BeginInterception(snapshot);
            State = new SetupTerminalState(true, 0, null);
            Write("setup terminal\n");
            Write("keyboard non-color\n");
            Write("output filename\n");
            result = ReadFileName(maximumBytes);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        { result = Finish(SetupTerminalOutcome.Failed, "terminal-driver-failed"); }
        finally
        {
            ClearBuffers();
            State = State with { Active = false, SecretCharacterCount = 0, PublicFileNameCharacterCount = 0 };
        }
        try { _driver.Restore(snapshot); }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        { return Finish(SetupTerminalOutcome.Failed, "terminal-restore-failed"); }
        return result;
    }

    private SetupTerminalResult ReadFileName(int maximumBytes)
    {
        while (true)
        {
            SetupTerminalEvent input = _driver.ReadEvent();
            switch (input.Kind)
            {
                case SetupTerminalEventKind.Character:
                    if (!_fileName.TryAppend(input.CharacterValue))
                        return Finish(SetupTerminalOutcome.Blocked, "terminal-output-name-invalid");
                    State = State with { PublicFileNameCharacterCount = _fileName.Count };
                    break;
                case SetupTerminalEventKind.Backspace:
                    _fileName.Backspace();
                    State = State with { PublicFileNameCharacterCount = _fileName.Count };
                    break;
                case SetupTerminalEventKind.Enter:
                    if (!_fileName.IsValid)
                        return Finish(SetupTerminalOutcome.Blocked, "terminal-output-name-invalid");
                    return SelectMode(_fileName.CopyValidatedFileName(), maximumBytes);
                case SetupTerminalEventKind.NavigationAway:
                    ClearBuffers();
                    SetupTerminalEvent back = _driver.ReadEvent();
                    if (back.Kind == SetupTerminalEventKind.NavigationBack) continue;
                    return TerminalEventOutcome(back);
                default:
                    return TerminalEventOutcome(input);
            }
        }
    }

    private SetupTerminalResult SelectMode(string validatedFileName, int maximumBytes)
    {
        Write("m manual g gen\n");
        SetupTerminalEvent input = _driver.ReadEvent();
        if (input.Kind == SetupTerminalEventKind.Character && input.CharacterValue is 'm' or 'M')
        {
            Write("secret masked\n");
            return ReadManual(validatedFileName, maximumBytes);
        }
        if (input.Kind == SetupTerminalEventKind.Character && input.CharacterValue is 'g' or 'G')
            return Generate(validatedFileName, maximumBytes);
        if (input.Kind == SetupTerminalEventKind.NavigationAway)
            return NavigateBackToFileName(maximumBytes);
        return TerminalEventOutcome(input);
    }

    private SetupTerminalResult ReadManual(string validatedFileName, int maximumBytes)
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
                    return Compose(validatedFileName, _secret.CopyTransientValue(),
                        DotenvEntryKind.LocalHumanValue, DotenvProvenance.UserInput, maximumBytes);
                case SetupTerminalEventKind.NavigationAway:
                    return NavigateBackToFileName(maximumBytes);
                default:
                    return TerminalEventOutcome(input);
            }
        }
    }

    private SetupTerminalResult Generate(string validatedFileName, int maximumBytes)
    {
        using LocalSecretGenerator generator = LocalSecretGenerator.Create();
        using LocalSecretGenerationResult generated = generator.Generate(
            "SETUP_SECRET", LocalSecretGenerationProfile.OpaqueUrlSafe256);
        if (!generated.Succeeded) return Finish(SetupTerminalOutcome.Failed, "terminal-generation-failed");
        return Compose(validatedFileName, generated.Output!.CopyValue(),
            DotenvEntryKind.GeneratedValueReference, DotenvProvenance.Generated, maximumBytes);
    }

    private SetupTerminalResult Compose(string validatedFileName, string transient,
        DotenvEntryKind kind, DotenvProvenance provenance, int maximumBytes)
    {
        SetupTerminalArtifactResult artifact = SetupTerminalArtifactComposer.ComposeAndWrite(
            _protectedWriter!, validatedFileName, transient, kind, provenance, maximumBytes);
        return Finish(artifact.Outcome, artifact.Code, artifact.Digest, artifact.Readiness,
            artifact.MissingCount, artifact.BlockedCount, artifact.Write);
    }

    private SetupTerminalResult NavigateBackToFileName(int maximumBytes)
    {
        ClearBuffers();
        State = State with { SecretCharacterCount = 0, PublicFileNameCharacterCount = 0 };
        SetupTerminalEvent back = _driver.ReadEvent();
        return back.Kind == SetupTerminalEventKind.NavigationBack
            ? ReadFileName(maximumBytes) : TerminalEventOutcome(back);
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

    private SetupTerminalResult Finish(SetupTerminalOutcome outcome, string code, string? digest = null,
        SetupTerminalReadiness readiness = SetupTerminalReadiness.None, int missing = 0, int blocked = 0,
        SetupTerminalProtectedWriteResult write = SetupTerminalProtectedWriteResult.Blocked)
    {
        ClearBuffers();
        State = new SetupTerminalState(false, 0, outcome);
        return new SetupTerminalResult(outcome, code, digest, readiness, missing, blocked,
            write, SetupTerminalAccessibility.Current);
    }

    private void ClearBuffers() { _fileName.Clear(); _secret.Clear(); }

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
        _fileName.Dispose();
        _secret.Dispose();
        _driver.Dispose();
        State = State with { Active = false, SecretCharacterCount = 0, PublicFileNameCharacterCount = 0 };
    }
}
