// ABOUTME: Provides atomic one-time ATProto state and handoff storage using Redis GETDEL or explicit dev memory.
// ABOUTME: Refuses distributed auth-flow operation when Redis is absent instead of degrading replay protection.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Explore.Blazor.Authentication;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Explore.Blazor.Services.Auth;

public sealed class AtprotoAtomicCache(
    IEnumerable<IConnectionMultiplexer> redisConnections,
    IHostEnvironment environment,
    IOptions<AtprotoAuthenticationOptions> configuredOptions,
    TimeProvider timeProvider)
{
    private readonly IConnectionMultiplexer? _redis = redisConnections.SingleOrDefault();
    private ImmutableDictionary<string, MemoryEntry> _memory =
        ImmutableDictionary.Create<string, MemoryEntry>(StringComparer.Ordinal);

    public bool IsReady => _redis is not null || IsMemoryAllowed();

    public async Task<bool> StoreAsync(
        string purpose,
        string token,
        byte[] payload,
        TimeSpan lifetime,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateInput(purpose, token, payload, lifetime);
        var key = CreateKey(purpose, token);
        if (_redis is not null)
        {
            return await _redis.GetDatabase()
                .StringSetAsync(key, payload, lifetime, When.NotExists)
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        EnsureMemoryAllowed();
        return ImmutableInterlocked.TryAdd(
            ref _memory,
            key,
            new(payload, timeProvider.GetUtcNow() + lifetime));
    }

    public async Task<byte[]?> ConsumeAsync(
        string purpose,
        string token,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateToken(purpose, token);
        var key = CreateKey(purpose, token);
        if (_redis is not null)
        {
            var value = await _redis.GetDatabase()
                .StringGetDeleteAsync(key)
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            return value.HasValue ? (byte[])value! : null;
        }

        EnsureMemoryAllowed();
        if (!ImmutableInterlocked.TryRemove(ref _memory, key, out var entry)
            || entry.ExpiresAt <= timeProvider.GetUtcNow())
        {
            return null;
        }

        return entry.Payload;
    }

    public async Task<byte[]?> GetAsync(
        string purpose,
        string token,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateToken(purpose, token);
        var key = CreateKey(purpose, token);
        if (_redis is not null)
        {
            var value = await _redis.GetDatabase()
                .StringGetAsync(key)
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            return value.HasValue ? (byte[])value! : null;
        }

        EnsureMemoryAllowed();
        if (!Volatile.Read(ref _memory).TryGetValue(key, out var entry)
            || entry.ExpiresAt <= timeProvider.GetUtcNow())
        {
            ImmutableInterlocked.TryRemove(ref _memory, key, out _);
            return null;
        }

        return entry.Payload;
    }

    private bool IsMemoryAllowed() =>
        configuredOptions.Value.UseSingleNodeMemoryStore
        && (environment.IsDevelopment() || environment.IsEnvironment("Testing"));

    private void EnsureMemoryAllowed()
    {
        if (!IsMemoryAllowed())
        {
            throw new InvalidOperationException("ATProto OAuth requires Redis; single-node memory is development-only and must be explicit.");
        }
    }

    private static void ValidateInput(string purpose, string token, byte[] payload, TimeSpan lifetime)
    {
        ValidateToken(purpose, token);
        if (payload.Length is 0 or > 64 * 1024)
        {
            throw new ArgumentException("ATProto cache payload size is invalid.", nameof(payload));
        }

        if (lifetime <= TimeSpan.Zero || lifetime > TimeSpan.FromMinutes(10))
        {
            throw new ArgumentOutOfRangeException(nameof(lifetime));
        }
    }

    private static void ValidateToken(string purpose, string token)
    {
        if (string.IsNullOrWhiteSpace(purpose) || purpose.Length > 32
            || string.IsNullOrWhiteSpace(token) || token.Length is < 16 or > 512)
        {
            throw new ArgumentException("ATProto cache key is invalid.");
        }
    }

    private static string CreateKey(string purpose, string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return $"Explore.Blazor:atproto:{purpose}:{Convert.ToHexString(hash)}";
    }

    private sealed record MemoryEntry(byte[] Payload, DateTimeOffset ExpiresAt);
}
