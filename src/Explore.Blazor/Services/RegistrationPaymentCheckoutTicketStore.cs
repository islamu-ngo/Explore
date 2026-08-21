// ABOUTME: Protects checkout-cookie payloads and atomically bounds their one-time nonces.
// ABOUTME: Uses bounded standalone memory or mandatory Redis according to the registered host profile.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Collections.Immutable;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using StackExchange.Redis;

namespace Explore.Blazor.Services;

public sealed record RegistrationPaymentCheckoutTicketStoreOptions(bool RequiresRedis);

public sealed record RegistrationPaymentCheckoutTicketValidation(
    string ActiveKey,
    string Nonce);

public sealed record RegistrationPaymentCheckoutTicketIssue(
    string ProtectedCookie,
    RegistrationPaymentCheckoutTicketValidation Ticket,
    Uri Target,
    DateTimeOffset ExpiresAt);

public sealed class RegistrationPaymentCheckoutTicketStore
{
    private const string Purpose = "registration-payment-checkout-cookie-v1";
    private const string NonceKeyPrefix = "Explore.Blazor:registration-payment-checkout-cookie-v1:nonce:";
    private const int MaximumStandaloneEntries = 1024;
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string IssueScript = """
        local previous = redis.call('GET', KEYS[1])
        if previous then redis.call('DEL', ARGV[1] .. previous) end
        redis.call('SET', KEYS[1], ARGV[2], 'PX', ARGV[3])
        redis.call('SET', ARGV[1] .. ARGV[2], ARGV[4], 'PX', ARGV[3])
        return 1
        """;
    private const string ConsumeScript = """
        if redis.call('GET', KEYS[1]) ~= ARGV[1] then return 0 end
        local target = redis.call('GETDEL', KEYS[2])
        if not target then return false end
        redis.call('DEL', KEYS[1])
        return target
        """;
    private const string RevokeScript = """
        if redis.call('GET', KEYS[1]) ~= ARGV[1] then return 0 end
        redis.call('DEL', KEYS[2])
        redis.call('DEL', KEYS[1])
        return 1
        """;

    private readonly IConnectionMultiplexer? _redis;
    private readonly IDataProtector _protector;
    private readonly TimeProvider _timeProvider;
    private readonly RegistrationPaymentCheckoutTicketStoreOptions _options;
    private readonly Lock _memoryLock = new();
    private ImmutableDictionary<string, MemoryNonce> _memoryByNonce =
        ImmutableDictionary.Create<string, MemoryNonce>(StringComparer.Ordinal);
    private ImmutableDictionary<string, string> _memoryByActiveKey =
        ImmutableDictionary.Create<string, string>(StringComparer.Ordinal);

    public RegistrationPaymentCheckoutTicketStore(
        IEnumerable<IConnectionMultiplexer> redisConnections,
        IDataProtectionProvider dataProtectionProvider,
        TimeProvider timeProvider,
        RegistrationPaymentCheckoutTicketStoreOptions options)
    {
        _redis = redisConnections.SingleOrDefault();
        _protector = dataProtectionProvider.CreateProtector(typeof(RegistrationPaymentCheckoutTicketStore).FullName!, Purpose);
        _timeProvider = timeProvider;
        _options = options;
    }

    public RegistrationPaymentCheckoutTicketIssue? PrepareIssue(
        Uri target,
        Guid eventId,
        Guid orderId,
        HttpRequest request,
        string tenantSlug,
        string checkoutSession)
    {
        if (target.AbsoluteUri.Length > 2048 || string.IsNullOrWhiteSpace(checkoutSession))
        {
            return null;
        }

        string sessionDigest = Digest(checkoutSession);
        DateTimeOffset expiresAt = _timeProvider.GetUtcNow() + Lifetime;
        string nonce = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        string audience = BuildAudience(request, tenantSlug, eventId, orderId);
        string activeKey = CreateKey("active", audience + "|" + sessionDigest);
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(
            new TicketPayload(audience, sessionDigest, nonce, eventId, orderId, expiresAt),
            JsonOptions);
        string protectedCookie = WebEncoders.Base64UrlEncode(_protector.Protect(payload));
        return protectedCookie.Length <= 3072
            ? new(protectedCookie, new(activeKey, nonce), target, expiresAt)
            : null;
    }

    public Task CommitIssueAsync(
        RegistrationPaymentCheckoutTicketIssue issue,
        CancellationToken cancellationToken) =>
        RegisterNonceAsync(
            issue.Ticket.ActiveKey,
            issue.Ticket.Nonce,
            issue.Target,
            issue.ExpiresAt,
            cancellationToken);

    public Task RevokeIssueAsync(
        RegistrationPaymentCheckoutTicketValidation ticket,
        CancellationToken cancellationToken)
    {
        if (_options.RequiresRedis)
        {
            return RevokeRedisIssueAsync(ticket, cancellationToken);
        }

        RevokeMemoryIssue(ticket);
        return Task.CompletedTask;
    }

    public RegistrationPaymentCheckoutTicketValidation? Validate(
        string protectedCookie,
        HttpRequest request,
        string tenantSlug,
        string checkoutSession)
    {
        if (string.IsNullOrWhiteSpace(checkoutSession))
        {
            return null;
        }

        string currentSessionDigest = Digest(checkoutSession);

        try
        {
            TicketPayload? payload = JsonSerializer.Deserialize<TicketPayload>(
                _protector.Unprotect(WebEncoders.Base64UrlDecode(protectedCookie)),
                JsonOptions);
            if (payload is null
                || payload.ExpiresAt <= _timeProvider.GetUtcNow()
                || !FixedEquals(payload.SessionDigest, currentSessionDigest)
                || !string.Equals(
                    payload.Audience,
                    BuildAudience(request, tenantSlug, payload.EventId, payload.OrderId),
                    StringComparison.Ordinal))
            {
                return null;
            }

            return new(CreateKey("active", payload.Audience + "|" + payload.SessionDigest), payload.Nonce);
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException or FormatException or ArgumentException)
        {
            return null;
        }
    }

    public Task<Uri?> PeekTargetAsync(
        RegistrationPaymentCheckoutTicketValidation ticket,
        CancellationToken cancellationToken) =>
        _options.RequiresRedis
            ? PeekRedisTargetAsync(ticket, cancellationToken)
            : Task.FromResult(PeekMemoryTarget(ticket));

    public Task<Uri?> ConsumeTargetAsync(
        RegistrationPaymentCheckoutTicketValidation ticket,
        CancellationToken cancellationToken) =>
        _options.RequiresRedis
            ? ConsumeRedisTargetAsync(ticket, cancellationToken)
            : Task.FromResult(ConsumeMemoryTarget(ticket));

    private async Task RegisterNonceAsync(
        string activeKey,
        string nonce,
        Uri target,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        if (!_options.RequiresRedis)
        {
            RegisterMemoryNonce(activeKey, nonce, target, expiresAt);
            return;
        }

        IDatabase database = GetRequiredRedisDatabase();
        try
        {
            await database.ScriptEvaluateAsync(
                    IssueScript,
                    [activeKey],
                    [NonceKeyPrefix, nonce, (long)Lifetime.TotalMilliseconds, target.AbsoluteUri])
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new RegistrationPaymentCheckoutStoreUnavailableException(exception);
        }
    }

    private async Task<Uri?> PeekRedisTargetAsync(
        RegistrationPaymentCheckoutTicketValidation ticket,
        CancellationToken cancellationToken)
    {
        IDatabase database = GetRequiredRedisDatabase();
        try
        {
            RedisValue activeNonce = await database.StringGetAsync(ticket.ActiveKey).WaitAsync(cancellationToken).ConfigureAwait(false);
            if (!activeNonce.HasValue || !FixedEquals(activeNonce.ToString(), ticket.Nonce))
            {
                return null;
            }

            RedisValue target = await database.StringGetAsync(NonceKeyPrefix + ticket.Nonce).WaitAsync(cancellationToken).ConfigureAwait(false);
            return target.HasValue && Uri.TryCreate(target.ToString(), UriKind.Absolute, out Uri? uri) ? uri : null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new RegistrationPaymentCheckoutStoreUnavailableException(exception);
        }
    }

    private async Task<Uri?> ConsumeRedisTargetAsync(
        RegistrationPaymentCheckoutTicketValidation ticket,
        CancellationToken cancellationToken)
    {
        IDatabase database = GetRequiredRedisDatabase();
        try
        {
            RedisResult result = await database.ScriptEvaluateAsync(
                    ConsumeScript,
                    [ticket.ActiveKey, NonceKeyPrefix + ticket.Nonce],
                    [ticket.Nonce])
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            return !result.IsNull && Uri.TryCreate(result.ToString(), UriKind.Absolute, out Uri? target) ? target : null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new RegistrationPaymentCheckoutStoreUnavailableException(exception);
        }
    }

    private async Task RevokeRedisIssueAsync(
        RegistrationPaymentCheckoutTicketValidation ticket,
        CancellationToken cancellationToken)
    {
        IDatabase database = GetRequiredRedisDatabase();
        try
        {
            await database.ScriptEvaluateAsync(
                    RevokeScript,
                    [ticket.ActiveKey, NonceKeyPrefix + ticket.Nonce],
                    [ticket.Nonce])
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new RegistrationPaymentCheckoutStoreUnavailableException(exception);
        }
    }

    private IDatabase GetRequiredRedisDatabase()
    {
        if (_redis is null)
        {
            throw new RegistrationPaymentCheckoutStoreUnavailableException();
        }

        try
        {
            return _redis.GetDatabase();
        }
        catch (Exception exception)
        {
            throw new RegistrationPaymentCheckoutStoreUnavailableException(exception);
        }
    }

    private void RegisterMemoryNonce(string activeKey, string nonce, Uri target, DateTimeOffset expiresAt)
    {
        lock (_memoryLock)
        {
            RemoveExpiredMemoryEntries();
            if (_memoryByActiveKey.TryGetValue(activeKey, out string? previousNonce))
            {
                _memoryByActiveKey = _memoryByActiveKey.Remove(activeKey);
                _memoryByNonce = _memoryByNonce.Remove(previousNonce);
            }

            if (_memoryByNonce.Count >= MaximumStandaloneEntries)
            {
                throw new RegistrationPaymentCheckoutStoreUnavailableException();
            }

            _memoryByActiveKey = _memoryByActiveKey.Add(activeKey, nonce);
            _memoryByNonce = _memoryByNonce.Add(nonce, new(activeKey, target, expiresAt));
        }
    }

    private Uri? PeekMemoryTarget(RegistrationPaymentCheckoutTicketValidation ticket)
    {
        lock (_memoryLock)
        {
            RemoveExpiredMemoryEntries();
            return _memoryByActiveKey.TryGetValue(ticket.ActiveKey, out string? activeNonce)
                && FixedEquals(activeNonce, ticket.Nonce)
                && _memoryByNonce.TryGetValue(ticket.Nonce, out MemoryNonce? entry)
                    ? entry.Target
                    : null;
        }
    }

    private Uri? ConsumeMemoryTarget(RegistrationPaymentCheckoutTicketValidation ticket)
    {
        lock (_memoryLock)
        {
            RemoveExpiredMemoryEntries();
            if (!_memoryByActiveKey.TryGetValue(ticket.ActiveKey, out string? activeNonce)
                || !FixedEquals(activeNonce, ticket.Nonce)
                || !_memoryByNonce.TryGetValue(ticket.Nonce, out MemoryNonce? entry))
            {
                return null;
            }

            _memoryByNonce = _memoryByNonce.Remove(ticket.Nonce);
            _memoryByActiveKey = _memoryByActiveKey.Remove(ticket.ActiveKey);
            return entry!.Target;
        }
    }

    private void RevokeMemoryIssue(RegistrationPaymentCheckoutTicketValidation ticket)
    {
        lock (_memoryLock)
        {
            if (_memoryByActiveKey.TryGetValue(ticket.ActiveKey, out string? activeNonce)
                && FixedEquals(activeNonce, ticket.Nonce))
            {
                _memoryByActiveKey = _memoryByActiveKey.Remove(ticket.ActiveKey);
                _memoryByNonce = _memoryByNonce.Remove(ticket.Nonce);
            }
        }
    }

    private void RemoveExpiredMemoryEntries()
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        foreach ((string nonce, MemoryNonce entry) in _memoryByNonce.Where(pair => pair.Value.ExpiresAt <= now).ToArray())
        {
            _memoryByNonce = _memoryByNonce.Remove(nonce);
            if (_memoryByActiveKey.TryGetValue(entry.ActiveKey, out string? activeNonce)
                && string.Equals(activeNonce, nonce, StringComparison.Ordinal))
            {
                _memoryByActiveKey = _memoryByActiveKey.Remove(entry.ActiveKey);
            }
        }
    }

    private static string BuildAudience(HttpRequest request, string tenantSlug, Guid eventId, Guid orderId) =>
        $"{request.Scheme}://{request.Host}{request.PathBase}|{tenantSlug}|{eventId:D}|{orderId:D}";

    private static string CreateKey(string kind, string value)
    {
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        return $"Explore.Blazor:{Purpose}:{kind}:{hash}";
    }

    private static string Digest(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static bool FixedEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));

    private sealed record TicketPayload(
        string Audience,
        string SessionDigest,
        string Nonce,
        Guid EventId,
        Guid OrderId,
        DateTimeOffset ExpiresAt);

    private sealed record MemoryNonce(string ActiveKey, Uri Target, DateTimeOffset ExpiresAt);
}

public sealed class RegistrationPaymentCheckoutStoreUnavailableException : Exception
{
    public RegistrationPaymentCheckoutStoreUnavailableException()
        : base("Registration payment checkout ticket storage is unavailable.")
    {
    }

    public RegistrationPaymentCheckoutStoreUnavailableException(Exception innerException)
        : base("Registration payment checkout ticket storage is unavailable.", innerException)
    {
    }
}
