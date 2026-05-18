// ABOUTME: Assesses BFF-held access tokens without exposing token material to the browser.
// ABOUTME: Provides safe token summaries and user-id extraction for auth endpoint refresh flows.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Explore.Blazor.Services.Auth;

public interface IBffAccessTokenAssessmentService
{
    BffAccessTokenAssessment Assess(string? accessToken);

    string Describe(string? accessToken);

    string? ResolveUserId(ClaimsPrincipal? principal);

    string? ResolveUserId(IEnumerable<Claim>? claims);
}

public readonly record struct BffAccessTokenAssessment(bool IsUsable, string Reason);

public sealed class BffAccessTokenAssessmentService : IBffAccessTokenAssessmentService
{
    private static readonly TimeSpan ExpirySafetyWindow = TimeSpan.FromSeconds(30);

    public BffAccessTokenAssessment Assess(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return new BffAccessTokenAssessment(false, "missing_access_token");
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(accessToken))
            {
                return new BffAccessTokenAssessment(false, "unreadable_access_token");
            }

            var token = handler.ReadJwtToken(accessToken);
            var validToUtc = token.ValidTo;
            if (validToUtc <= DateTime.UtcNow.Add(ExpirySafetyWindow))
            {
                return new BffAccessTokenAssessment(false, $"expired_access_token:{validToUtc:o}");
            }

            return new BffAccessTokenAssessment(true, $"valid_until:{validToUtc:o}");
        }
        catch (Exception)
        {
            return new BffAccessTokenAssessment(false, "access_token_parse_failed");
        }
    }

    public string Describe(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return "missing";
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(accessToken))
            {
                return "unreadable_jwt";
            }

            var token = handler.ReadJwtToken(accessToken);
            var userId = ResolveUserId(token.Claims) ?? "unknown";
            var issuer = string.IsNullOrWhiteSpace(token.Issuer) ? "unknown" : token.Issuer;
            var audience = token.Audiences.FirstOrDefault()
                ?? token.Claims.FirstOrDefault(c => c.Type == "azp")?.Value
                ?? "unknown";
            return $"user={userId};validTo={token.ValidTo:o};iss={issuer};aud={audience}";
        }
        catch (Exception)
        {
            return "jwt_parse_failed";
        }
    }

    public string? ResolveUserId(ClaimsPrincipal? principal) => ResolveUserId(principal?.Claims);

    public string? ResolveUserId(IEnumerable<Claim>? claims)
    {
        if (claims is null)
        {
            return null;
        }

        return claims.FirstOrDefault(c => c.Type == "sub")?.Value
            ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
            ?? claims.FirstOrDefault(c => c.Type == "sid")?.Value;
    }
}
