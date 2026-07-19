// ABOUTME: Persists protected CarpaNet OAuth state with bounded lifetime and atomic single-use consumption.
// ABOUTME: Rebinds issuer, DID, PDS, tenant, origin, return path, and client key before token exchange.

using System.Security.Cryptography;
using System.Text.Json;
using CarpaNet.OAuth.Storage;
using Explore.Blazor.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Explore.Blazor.Services.Auth;

public sealed class CacheBackedOAuthStateStore : IOAuthStateStore
{
    private const string Purpose = "oauth-state-v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AtprotoAtomicCache _cache;
    private readonly IDataProtector _protector;
    private readonly AtprotoOAuthFlowContext _flowContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AtprotoTenantOriginResolver _originResolver;
    private readonly IOptions<AtprotoAuthenticationOptions> _configuredOptions;
    private readonly TimeProvider _timeProvider;

    public CacheBackedOAuthStateStore(
        AtprotoAtomicCache cache,
        IDataProtectionProvider dataProtectionProvider,
        AtprotoOAuthFlowContext flowContext,
        IHttpContextAccessor httpContextAccessor,
        AtprotoTenantOriginResolver originResolver,
        IOptions<AtprotoAuthenticationOptions> configuredOptions,
        TimeProvider timeProvider)
    {
        _cache = cache;
        _protector = dataProtectionProvider.CreateProtector(typeof(CacheBackedOAuthStateStore).FullName!, Purpose);
        _flowContext = flowContext;
        _httpContextAccessor = httpContextAccessor;
        _originResolver = originResolver;
        _configuredOptions = configuredOptions;
        _timeProvider = timeProvider;
    }

    public static string EncodeAppState(AtprotoOAuthFlowSeed seed) =>
        JsonSerializer.Serialize(seed, JsonOptions);

    public async Task<string?> GetPinnedKeyIdAsync(
        string state,
        CancellationToken cancellationToken = default)
    {
        var payload = await _cache.GetAsync(Purpose, state, cancellationToken).ConfigureAwait(false);
        if (payload is null)
        {
            return null;
        }

        try
        {
            var data = JsonSerializer.Deserialize<OAuthStateData>(_protector.Unprotect(payload), JsonOptions);
            return data is null || data.ExpiresAt <= _timeProvider.GetUtcNow()
                ? null
                : ParseAndValidate(data).OAuthClientKeyId;
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException or ArgumentException or InvalidOperationException)
        {
            return null;
        }
    }

    public async Task StoreAsync(
        string state,
        OAuthStateData data,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        var seed = ParseAndValidate(data);
        var now = _timeProvider.GetUtcNow();
        var configuredLifetime = TimeSpan.FromSeconds(Math.Clamp(
            _configuredOptions.Value.StateLifetimeSeconds,
            30,
            600));
        var lifetime = data.ExpiresAt - now;
        if (lifetime <= TimeSpan.Zero || lifetime > configuredLifetime + TimeSpan.FromSeconds(1))
        {
            throw new InvalidOperationException("ATProto OAuth state lifetime is invalid.");
        }

        _ = seed;
        var payload = _protector.Protect(JsonSerializer.SerializeToUtf8Bytes(data, JsonOptions));
        if (!await _cache.StoreAsync(Purpose, state, payload, lifetime, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("ATProto OAuth state collision.");
        }
    }

    public async Task<OAuthStateData?> ConsumeAsync(
        string state,
        CancellationToken cancellationToken = default)
    {
        var payload = await _cache.ConsumeAsync(Purpose, state, cancellationToken).ConfigureAwait(false);
        if (payload is null)
        {
            return null;
        }

        try
        {
            var data = JsonSerializer.Deserialize<OAuthStateData>(_protector.Unprotect(payload), JsonOptions);
            if (data is null || data.ExpiresAt <= _timeProvider.GetUtcNow())
            {
                return null;
            }

            var seed = ParseAndValidate(data);
            var issuer = ParseHttpsOriginOrUri(data.Issuer, allowPath: false);
            var context = _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("ATProto callback HTTP context is unavailable.");
            var callbackIssuer = context.Request.Query["iss"];
            if (callbackIssuer.Count != 1
                || !Uri.TryCreate(callbackIssuer[0], UriKind.Absolute, out var suppliedIssuer)
                || !UrisEqual(issuer, suppliedIssuer))
            {
                return null;
            }

            var callbackOrigin = new Uri($"{context.Request.Scheme}://{context.Request.Host.Value}/", UriKind.Absolute);
            if (!AtprotoTenantOriginResolver.OriginsEqual(callbackOrigin, _originResolver.ParseCanonicalOrigin()))
            {
                return null;
            }

            _flowContext.BindConsumedState(new(seed, issuer));
            return data;
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException or ArgumentException or InvalidOperationException)
        {
            return null;
        }
    }

    private static AtprotoOAuthFlowSeed ParseAndValidate(OAuthStateData data)
    {
        if (string.IsNullOrWhiteSpace(data.AppState) || data.AppState.Length > 4096)
        {
            throw new InvalidOperationException("ATProto application state is invalid.");
        }

        var seed = JsonSerializer.Deserialize<AtprotoOAuthFlowSeed>(data.AppState, JsonOptions)
            ?? throw new InvalidOperationException("ATProto application state is invalid.");
        var issuer = ParseHttpsOriginOrUri(data.Issuer, allowPath: false);
        var storedPds = ParseHttpsOriginOrUri(data.PdsUrl, allowPath: false);
        if (seed.TenantId == Guid.Empty
            || string.IsNullOrWhiteSpace(seed.ExpectedDid)
            || seed.ExpectedDid.Length > 2048
            || !seed.ExpectedDid.StartsWith("did:", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(seed.TenantSlug)
            || string.IsNullOrWhiteSpace(seed.OAuthClientKeyId)
            || seed.OAuthClientKeyId.Length > 128
            || !IsSafeReturnPath(seed.ReturnPath)
            || !UrisEqual(storedPds, seed.ExpectedPdsUri)
            || seed.Origin.AbsolutePath != "/"
            || seed.Origin.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("ATProto OAuth state binding is invalid.");
        }

        _ = issuer;
        return seed;
    }

    private static Uri ParseHttpsOriginOrUri(string? value, bool allowPath)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment)
            || (!allowPath && uri.AbsolutePath != "/"))
        {
            throw new ArgumentException("ATProto URI binding is invalid.");
        }

        return uri;
    }

    private static bool UrisEqual(Uri left, Uri right) =>
        string.Equals(
            left.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped).TrimEnd('/'),
            right.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped).TrimEnd('/'),
            StringComparison.Ordinal);

    private static bool IsSafeReturnPath(string value) =>
        value.Length is > 0 and <= 2048
        && value[0] == '/'
        && !value.StartsWith("//", StringComparison.Ordinal)
        && !value.StartsWith("/\\", StringComparison.Ordinal)
        && !value.Contains('\r')
        && !value.Contains('\n');
}
