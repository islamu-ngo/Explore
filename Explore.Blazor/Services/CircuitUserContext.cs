// ABOUTME: Bridges circuit user identity into the current inbound activity for cross-scope handler access.
// ABOUTME: Avoids relying on OnCircuitOpenedAsync execution context flowing into pooled HttpClient handlers.

namespace Explore.Blazor.Services;

/// <summary>
/// Provides access to the current circuit's user identity across async context boundaries.
/// Uses AsyncLocal so the value flows through Blazor circuit-dispatched events even when
/// IHttpContextAccessor.HttpContext is null.
/// </summary>
public interface ICircuitUserContext
{
    string? UserId { get; }
    void SetUserId(string? userId);
    IDisposable BeginActivityScope();
}

public sealed class CircuitUserContext : ICircuitUserContext
{
    private static readonly AsyncLocal<UserIdHolder?> _currentUserId = new();
    private string? _userId;

    public string? UserId => _currentUserId.Value?.Value ?? _userId;

    public void SetUserId(string? userId)
    {
        _userId = string.IsNullOrWhiteSpace(userId) ? null : userId;
    }

    public IDisposable BeginActivityScope()
    {
        var previous = _currentUserId.Value;
        _currentUserId.Value = new UserIdHolder { Value = _userId };
        return new Scope(() => _currentUserId.Value = previous);
    }

    private sealed class UserIdHolder
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
