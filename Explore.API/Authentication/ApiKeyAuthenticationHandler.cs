// ABOUTME: Authenticates direct machine callers using the Phase 0 API-key spike configuration.
// ABOUTME: Produces a claims principal carrying tenant and owner context for post-auth tenant validation.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Explore.Application.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Explore.API.Authentication;

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var headerValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var rawApiKey = headerValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(rawApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API key header is empty."));
        }

        var client = Options.Clients.FirstOrDefault(candidate =>
            candidate.IsActive &&
            ApiKeyHashing.MatchesHash(rawApiKey, candidate.SecretHash));

        if (client is null)
        {
            Logger.LogWarning("[ApiKey] Authentication failed for path {Path}: no matching active client.", Request.Path);
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        if (client.ExpiresAtUtc is DateTimeOffset expiresAtUtc && expiresAtUtc <= DateTimeOffset.UtcNow)
        {
            Logger.LogWarning("[ApiKey] Authentication failed for key {KeyId}: key expired at {ExpiresAtUtc}.", client.KeyId, expiresAtUtc);
            return Task.FromResult(AuthenticateResult.Fail("API key expired."));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, $"api-key:{client.KeyId}"),
            new(ApiAuthenticationClaimTypes.AuthMethod, "api_key"),
            new(ApiAuthenticationClaimTypes.ApiKeyId, client.KeyId),
            new(ApiAuthenticationClaimTypes.TenantId, client.TenantId.ToString()),
            new(ApiAuthenticationClaimTypes.OwnerType, client.OwnerType),
            new(ApiAuthenticationClaimTypes.OwnerId, client.OwnerId)
        };

        claims.AddRange(client.Scopes
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => new Claim(ApiAuthenticationClaimTypes.Scope, scope)));

        var identity = new ClaimsIdentity(claims, ApiAuthenticationSchemeNames.ApiKey, ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiAuthenticationSchemeNames.ApiKey);

        Logger.LogInformation("[ApiKey] Authenticated key {KeyId} for tenant {TenantId} on {Path}.", client.KeyId, client.TenantId, Request.Path);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = $"{ApiAuthenticationSchemeNames.ApiKey} realm=\"api\"";
        return Task.CompletedTask;
    }
}
