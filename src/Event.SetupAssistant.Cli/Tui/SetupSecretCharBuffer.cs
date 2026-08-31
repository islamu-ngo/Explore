// ABOUTME: Owns one bounded mutable secret character array and clears it on every release path.
// ABOUTME: Exposes only count and an explicitly transient managed-string copy required by Setup Core.

namespace ISLAMU.Event.SetupAssistant.Cli.Tui;

public sealed class SetupSecretCharBuffer : IDisposable
{
    private char[]? _characters;
    private int _count;

    public SetupSecretCharBuffer(int capacity)
    {
        if (capacity is < 1 or > 8_192) throw new ArgumentOutOfRangeException(nameof(capacity));
        _characters = new char[capacity];
    }

    public int Count => _count;
    public bool TryAppend(char value)
    {
        char[] characters = _characters ?? throw new ObjectDisposedException(nameof(SetupSecretCharBuffer));
        if (char.IsControl(value) || char.IsSurrogate(value) || _count == characters.Length) return false;
        characters[_count++] = value;
        return true;
    }

    public bool Backspace()
    {
        char[] characters = _characters ?? throw new ObjectDisposedException(nameof(SetupSecretCharBuffer));
        if (_count == 0) return false;
        characters[--_count] = '\0';
        return true;
    }

    public string CopyTransientValue()
    {
        char[] characters = _characters ?? throw new ObjectDisposedException(nameof(SetupSecretCharBuffer));
        return new string(characters, 0, _count);
    }

    public void Clear()
    {
        char[]? characters = _characters;
        if (characters is not null) characters.AsSpan().Clear();
        _count = 0;
    }

    public void Dispose()
    {
        char[]? characters = Interlocked.Exchange(ref _characters, null);
        if (characters is not null) characters.AsSpan().Clear();
        _count = 0;
    }

    public override string ToString() => $"{nameof(SetupSecretCharBuffer)}:Redacted:Count={_count}";
}
