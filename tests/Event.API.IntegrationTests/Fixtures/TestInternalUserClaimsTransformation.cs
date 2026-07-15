// ABOUTME: Supplies a deterministic application-user identity to real-JWT authorization fixtures.
// ABOUTME: Keeps authentication tests independent from external-login synchronization and domain seeding.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Event.Api.IntegrationTests.Fixtures;

public sealed class TestInternalUserClaimsTransformation(Guid userId) : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true
            || principal.HasClaim(claim => claim.Type == "internal_user_id"))
        {
            return Task.FromResult(principal);
        }

        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("internal_user_id", userId.ToString("D")));
        principal.AddIdentity(identity);
        return Task.FromResult(principal);
    }
}
