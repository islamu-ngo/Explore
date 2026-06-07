// ABOUTME: Authenticates direct machine callers using the Phase 0 API-key spike configuration.
// ABOUTME: Produces a claims principal carrying tenant and owner context for post-auth tenant validation.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Explore.Application.Constants;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Services;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Explore.API.Authentication;

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private static readonly TimeSpan UsageUpdateInterval = TimeSpan.FromMinutes(5);
    private readonly IExternalApiKeyRepository _externalApiKeyRepository;
    private readonly BusinessMetrics _metrics;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IExternalApiKeyRepository externalApiKeyRepository,
        BusinessMetrics metrics)
        : base(options, logger, encoder)
    {
        _externalApiKeyRepository = externalApiKeyRepository;
        _metrics = metrics;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var headerValues))
        {
            return AuthenticateResult.NoResult();
        }

        var rawApiKey = headerValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(rawApiKey))
        {
            _metrics.RecordExternalApiKeyAuthentication("empty_header", tenantId: "unknown", ownerType: "unknown");
            return AuthenticateResult.Fail("API key header is empty.");
        }

        var persistedResult = await TryAuthenticatePersistedClientAsync(rawApiKey);
        if (persistedResult is not null)
        {
            return persistedResult;
        }

        var configuredClient = Options.Clients.FirstOrDefault(candidate =>
            candidate.IsActive &&
            ApiKeyHashing.MatchesHash(rawApiKey, candidate.SecretHash));

        if (configuredClient is null)
        {
            _metrics.RecordExternalApiKeyAuthentication("invalid", tenantId: "unknown", ownerType: "unknown");
            Logger.LogWarning("[ApiKey] Authentication failed for path {Path}: no matching active client.", Request.Path);
            return AuthenticateResult.Fail("Invalid API key.");
        }

        if (configuredClient.ExpiresAtUtc is DateTimeOffset expiresAtUtc && expiresAtUtc <= DateTimeOffset.UtcNow)
        {
            _metrics.RecordExternalApiKeyAuthentication(
                "expired",
                configuredClient.TenantId.ToString(),
                configuredClient.OwnerType);
            Logger.LogWarning("[ApiKey] Authentication failed for key {KeyId}: key expired at {ExpiresAtUtc}.", configuredClient.KeyId, expiresAtUtc);
            return AuthenticateResult.Fail("API key expired.");
        }

        return BuildSuccessResult(
            configuredClient.KeyId,
            configuredClient.TenantId,
            configuredClient.OwnerType,
            configuredClient.OwnerId,
            configuredClient.Scopes,
            "configured fallback");
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = $"{ApiAuthenticationSchemeNames.ApiKey} realm=\"api\"";
        return Task.CompletedTask;
    }

    private async Task<AuthenticateResult?> TryAuthenticatePersistedClientAsync(string rawApiKey)
    {
        if (!ApiKeyHashing.TryParsePersistedApiKey(rawApiKey, out var keyId, out var secret))
        {
            return null;
        }

        var persistedClient = await _externalApiKeyRepository.GetByKeyIdForAuthentication(keyId);
        if (persistedClient is null)
        {
            return null;
        }

        if (!IsUsableStatus(persistedClient.ExternalApiKeyStatusId))
        {
            _metrics.RecordExternalApiKeyAuthentication(
                "inactive",
                persistedClient.TenantId?.ToString() ?? "platform",
                persistedClient.OwnerType.ToString());
            Logger.LogWarning("[ApiKey] Authentication failed for persisted key {KeyId}: status {StatusId} is not usable.", persistedClient.KeyId, persistedClient.ExternalApiKeyStatusId);
            return AuthenticateResult.Fail("API key is not active.");
        }

        if (!ApiKeyHashing.MatchesHash(secret, persistedClient.SecretHash))
        {
            _metrics.RecordExternalApiKeyAuthentication(
                "invalid",
                persistedClient.TenantId?.ToString() ?? "platform",
                persistedClient.OwnerType.ToString());
            Logger.LogWarning("[ApiKey] Authentication failed for persisted key {KeyId}: secret hash mismatch.", persistedClient.KeyId);
            return AuthenticateResult.Fail("Invalid API key.");
        }

        if (persistedClient.ExpiresAt is DateTime expiresAtUtc && expiresAtUtc <= DateTime.UtcNow)
        {
            _metrics.RecordExternalApiKeyAuthentication(
                "expired",
                persistedClient.TenantId?.ToString() ?? "platform",
                persistedClient.OwnerType.ToString());
            Logger.LogWarning("[ApiKey] Authentication failed for persisted key {KeyId}: key expired at {ExpiresAtUtc}.", persistedClient.KeyId, expiresAtUtc);
            return AuthenticateResult.Fail("API key expired.");
        }

        await _externalApiKeyRepository.TouchUsageMetadata(
            persistedClient.Id,
            DateTime.UtcNow,
            Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
            UsageUpdateInterval,
            Context.RequestAborted);

        return BuildSuccessResult(
            persistedClient.KeyId,
            persistedClient.TenantId,
            persistedClient.OwnerType.ToString(),
            persistedClient.OwnerId.ToString(),
            SplitScopes(persistedClient.Scopes),
            "persisted credential");
    }

    private AuthenticateResult BuildSuccessResult(
        string keyId,
        Guid? tenantId,
        string ownerType,
        string ownerId,
        IEnumerable<string> scopes,
        string source)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, $"api-key:{keyId}"),
            new(ApiAuthenticationClaimTypes.AuthMethod, "api_key"),
            new(ApiAuthenticationClaimTypes.ApiKeyId, keyId),
            new(ApiAuthenticationClaimTypes.OwnerType, ownerType),
            new(ApiAuthenticationClaimTypes.OwnerId, ownerId)
        };

        if (tenantId.HasValue)
        {
            claims.Add(new Claim(ApiAuthenticationClaimTypes.TenantId, tenantId.Value.ToString()));
        }

        if (Enum.TryParse<ExternalApiKeyOwnerType>(ownerType, ignoreCase: true, out var parsedOwnerType) &&
            parsedOwnerType == ExternalApiKeyOwnerType.User &&
            Guid.TryParse(ownerId, out var ownerUserId))
        {
            claims.Add(new Claim("internal_user_id", ownerUserId.ToString()));
        }

        claims.AddRange(scopes
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => new Claim(ApiAuthenticationClaimTypes.Scope, scope)));

        var identity = new ClaimsIdentity(claims, ApiAuthenticationSchemeNames.ApiKey, ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiAuthenticationSchemeNames.ApiKey);

        _metrics.RecordExternalApiKeyAuthentication("success", tenantId?.ToString() ?? "platform", ownerType);
        Logger.LogInformation("[ApiKey] Authenticated key {KeyId} for tenant {TenantId} on {Path} via {Source}.", keyId, tenantId?.ToString() ?? "platform", Request.Path, source);
        return AuthenticateResult.Success(ticket);
    }

    private static bool IsUsableStatus(int statusId)
    {
        return statusId == (int)ExternalApiKeyStatusEnum.Active
            || statusId == (int)ExternalApiKeyStatusEnum.PendingRotation;
    }

    private static IReadOnlyList<string> SplitScopes(string scopes)
    {
        return scopes
            .Split([' ', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
