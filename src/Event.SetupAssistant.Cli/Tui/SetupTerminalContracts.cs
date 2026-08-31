// ABOUTME: Defines immutable value-safe events, outcomes, accessibility facts, and protected-write contracts.
// ABOUTME: Keeps terminal automation closed to explicit events and excludes secret values from public projections.

namespace ISLAMU.Event.SetupAssistant.Cli.Tui;

[Flags]
public enum SetupTerminalAccessibilityFeature
{
    None = 0,
    KeyboardBasicUnicode = 1 << 0,
    NonColorStatus = 1 << 1,
    MaskedInput = 1 << 2,
    ScreenReaderSemantics = 1 << 3,
    Braille = 1 << 4,
    ImeComposition = 1 << 5,
    UnicodeGraphemeEditing = 1 << 6,
    TerminalScrollbackErasure = 1 << 7,
    RightToLeft = 1 << 8,
    ScalableLayout = 1 << 9,
    OsWideAccessibility = 1 << 10,
}

public sealed record SetupTerminalAccessibility(
    SetupTerminalAccessibilityFeature Supported,
    SetupTerminalAccessibilityFeature Unverified)
{
    public static SetupTerminalAccessibility Current { get; } = new(
        SetupTerminalAccessibilityFeature.KeyboardBasicUnicode |
        SetupTerminalAccessibilityFeature.NonColorStatus |
        SetupTerminalAccessibilityFeature.MaskedInput,
        SetupTerminalAccessibilityFeature.ScreenReaderSemantics |
        SetupTerminalAccessibilityFeature.Braille |
        SetupTerminalAccessibilityFeature.ImeComposition |
        SetupTerminalAccessibilityFeature.UnicodeGraphemeEditing |
        SetupTerminalAccessibilityFeature.TerminalScrollbackErasure |
        SetupTerminalAccessibilityFeature.RightToLeft |
        SetupTerminalAccessibilityFeature.ScalableLayout |
        SetupTerminalAccessibilityFeature.OsWideAccessibility);
}

public enum SetupTerminalEventKind
{
    Character, Backspace, Enter, Escape, UnsupportedKey, CancelSignal,
    TerminationSignal, Suspend, ResizeChanged, ResizeFailure, NavigationAway,
    NavigationBack, DriverError,
}

public readonly record struct SetupTerminalEvent
{
    private SetupTerminalEvent(SetupTerminalEventKind kind, char character = default)
    {
        Kind = kind;
        CharacterValue = character;
    }

    public SetupTerminalEventKind Kind { get; }
    public char CharacterValue { get; }
    public static SetupTerminalEvent Character(char value) => new(SetupTerminalEventKind.Character, value);
    public static SetupTerminalEvent Backspace() => new(SetupTerminalEventKind.Backspace);
    public static SetupTerminalEvent Enter() => new(SetupTerminalEventKind.Enter);
    public static SetupTerminalEvent Escape() => new(SetupTerminalEventKind.Escape);
    public static SetupTerminalEvent UnsupportedKey() => new(SetupTerminalEventKind.UnsupportedKey);
    public static SetupTerminalEvent CancelSignal() => new(SetupTerminalEventKind.CancelSignal);
    public static SetupTerminalEvent TerminationSignal() => new(SetupTerminalEventKind.TerminationSignal);
    public static SetupTerminalEvent Suspend() => new(SetupTerminalEventKind.Suspend);
    public static SetupTerminalEvent ResizeChanged() => new(SetupTerminalEventKind.ResizeChanged);
    public static SetupTerminalEvent ResizeFailure() => new(SetupTerminalEventKind.ResizeFailure);
    public static SetupTerminalEvent NavigationAway() => new(SetupTerminalEventKind.NavigationAway);
    public static SetupTerminalEvent NavigationBack() => new(SetupTerminalEventKind.NavigationBack);
    public static SetupTerminalEvent DriverError() => new(SetupTerminalEventKind.DriverError);
    public override string ToString() => $"{nameof(SetupTerminalEvent)}:{Kind}";
}

public enum SetupTerminalOutcome { Completed, Cancelled, Blocked, Failed, Suspended }
public enum SetupTerminalReadiness { None, Ready, Incomplete, Blocked }
public enum SetupTerminalProtectedWriteResult { Written, Blocked }

public sealed record SetupTerminalResult(
    SetupTerminalOutcome Outcome,
    string DiagnosticCode,
    string? Digest,
    SetupTerminalReadiness Readiness,
    int MissingCount,
    int BlockedCount,
    SetupTerminalProtectedWriteResult ProtectedWrite,
    SetupTerminalAccessibility Accessibility)
{
    public override string ToString() =>
        $"{nameof(SetupTerminalResult)}:{Outcome}:{DiagnosticCode}:Readiness={Readiness}:Missing={MissingCount}:Blocked={BlockedCount}:Write={ProtectedWrite}";
}

public readonly record struct SetupTerminalDriverSnapshot(
    bool InterceptionActive, bool EchoSuppressedByIntercept, bool TreatControlCAsInput,
    int WindowWidth, int WindowHeight);

public sealed record SetupTerminalState(bool Active, int SecretCharacterCount, SetupTerminalOutcome? Outcome)
{
    public override string ToString() => $"{nameof(SetupTerminalState)}:Active={Active}:Count={SecretCharacterCount}:Outcome={Outcome}";
}

public interface ISetupTerminalProtectedWriter
{
    bool IsAvailable { get; }
    SetupTerminalProtectedWriteResult WriteCreateNew(ReadOnlyMemory<byte> bytes, int maximumBytes);
}
