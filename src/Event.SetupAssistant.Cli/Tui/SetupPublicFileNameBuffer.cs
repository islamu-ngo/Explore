// ABOUTME: Owns one bounded mutable public filename without path separators, traversal, or control characters.
// ABOUTME: Clears filename characters on every exit and exposes no filename through string projections.

namespace ISLAMU.Event.SetupAssistant.Cli.Tui;

public sealed class SetupPublicFileNameBuffer : IDisposable
{
    public const int MaximumCharacters = 64;
    private char[]? _characters = new char[MaximumCharacters];
    private int _count;

    public int Count => _count;

    public bool TryAppend(char value)
    {
        char[] characters = _characters ?? throw new ObjectDisposedException(nameof(SetupPublicFileNameBuffer));
        if (_count == characters.Length || !IsAllowed(value)) return false;
        characters[_count++] = value;
        return true;
    }

    public bool Backspace()
    {
        char[] characters = _characters ?? throw new ObjectDisposedException(nameof(SetupPublicFileNameBuffer));
        if (_count == 0) return false;
        characters[--_count] = '\0';
        return true;
    }

    public bool IsValid => _count > 0 && !IsReserved(_characters!.AsSpan(0, _count));

    public string CopyValidatedFileName()
    {
        char[] characters = _characters ?? throw new ObjectDisposedException(nameof(SetupPublicFileNameBuffer));
        if (!IsValid) throw new InvalidOperationException("terminal-output-name-invalid");
        return new string(characters, 0, _count);
    }

    public void Clear()
    {
        if (_characters is { } characters) characters.AsSpan().Clear();
        _count = 0;
    }

    public void Dispose()
    {
        char[]? characters = Interlocked.Exchange(ref _characters, null);
        if (characters is not null) characters.AsSpan().Clear();
        _count = 0;
    }

    public override string ToString() => $"{nameof(SetupPublicFileNameBuffer)}:Redacted:Count={_count}";

    public static bool IsSafe(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > MaximumCharacters) return false;
        return value.All(IsAllowed) && !IsReserved(value.AsSpan());
    }

    private static bool IsAllowed(char value) =>
        value is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '.' or '_' or '-';

    private static bool IsReserved(ReadOnlySpan<char> value) =>
        value.SequenceEqual("-".AsSpan()) || value.SequenceEqual(".".AsSpan()) || value.SequenceEqual("..".AsSpan());
}
