// ABOUTME: Stores the BFF auth cookie per circuit and exposes it during inbound activity execution.
// ABOUTME: Enables pooled self-call handlers to read the current circuit cookie via AsyncLocal.

namespace Explore.Blazor.Services;

public interface IBffAuthCookieStore
{
    string? CookieHeader { get; }
    void SetCookieHeader(string? cookieHeader);
    IDisposable BeginActivityScope();
}

public sealed class BffAuthCookieStore : IBffAuthCookieStore
{
    private static readonly AsyncLocal<CookieHolder?> _currentCookie = new();
    private string? _cookieHeader;

    public string? CookieHeader => _currentCookie.Value?.Value ?? _cookieHeader;

    public void SetCookieHeader(string? cookieHeader)
    {
        _cookieHeader = string.IsNullOrWhiteSpace(cookieHeader) ? null : cookieHeader;
    }

    public IDisposable BeginActivityScope()
    {
        var previous = _currentCookie.Value;
        _currentCookie.Value = new CookieHolder { Value = _cookieHeader };
        return new Scope(() => _currentCookie.Value = previous);
    }

    private sealed class CookieHolder
    {
        public string? Value { get; set; }
    }

    private sealed class Scope : IDisposable
    {
        private readonly Action _onDispose;
        private bool _disposed;

        public Scope(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _onDispose();
        }
    }
}
