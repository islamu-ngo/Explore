// ABOUTME: Protects first-party ATProto cookie-session results behind origin-bound one-time handoff codes.
// ABOUTME: Keeps platform JWTs and PDS credentials out of cross-host URLs and browser-visible state.

using System.Security.Cryptography;
using System.Text.Json;
using Explore.Blazor.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Explore.Blazor.Services.Auth;

public sealed record AtprotoTenantHandoff(
    AtprotoOAuthFlowSeed Seed,
    AtprotoBffSessionResult Session,
    DateTimeOffset ExpiresAt);

public sealed class AtprotoTenantSessionHandoffStore
{
    private const string Purpose = "tenant-handoff-v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AtprotoAtomicCache _cache;
    private readonly IDataProtector _protector;
    private readonly AtprotoTenantOriginResolver _originResolver;
    private readonly IOptions<AtprotoAuthenticationOptions> _configuredOptions;
    private readonly TimeProvider _timeProvider;

    public AtprotoTenantSessionHandoffStore(
        AtprotoAtomicCache cache,
        IDataProtectionProvider dataProtectionProvider,
        AtprotoTenantOriginResolver originResolver,
        IOptions<AtprotoAuthenticationOptions> configuredOptions,
        TimeProvider timeProvider)
    {
        _cache = cache;
        _protector = dataProtectionProvider.CreateProtector(typeof(AtprotoTenantSessionHandoffStore).FullName!, Purpose);
        _originResolver = originResolver;
        _configuredOptions = configuredOptions;
        _timeProvider = timeProvider;
    }

    public async Task<string> CreateAsync(
        AtprotoOAuthFlowSeed seed,
        AtprotoBffSessionResult session,
        CancellationToken cancellationToken)
    {
        var lifetime = TimeSpan.FromSeconds(Math.Clamp(
            _configuredOptions.Value.HandoffLifetimeSeconds,
            30,
            300));
        var handoff = new AtprotoTenantHandoff(seed, session, _timeProvider.GetUtcNow() + lifetime);
        var payload = _protector.Protect(JsonSerializer.SerializeToUtf8Bytes(handoff, JsonOptions));
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var code = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            if (await _cache.StoreAsync(Purpose, code, payload, lifetime, cancellationToken).ConfigureAwait(false))
            {
                return code;
            }
        }

        throw new InvalidOperationException("ATProto tenant handoff could not be created.");
    }

    public async Task<AtprotoTenantHandoff?> ConsumeAsync(
        string code,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var payload = await _cache.ConsumeAsync(Purpose, code, cancellationToken).ConfigureAwait(false);
        if (payload is null)
        {
            return null;
        }

        try
        {
            var handoff = JsonSerializer.Deserialize<AtprotoTenantHandoff>(_protector.Unprotect(payload), JsonOptions);
            var current = _originResolver.Resolve(request);
            if (handoff is null
                || handoff.ExpiresAt <= _timeProvider.GetUtcNow()
                || handoff.Seed.TenantId != current.TenantId
                || !string.Equals(handoff.Seed.TenantSlug, current.TenantSlug, StringComparison.Ordinal)
                || !AtprotoTenantOriginResolver.OriginsEqual(handoff.Seed.Origin, current.Origin))
            {
                return null;
            }

            return handoff;
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException or ArgumentException or InvalidOperationException)
        {
            return null;
        }
    }
}
