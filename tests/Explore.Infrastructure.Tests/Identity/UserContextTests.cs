// ABOUTME: Unit coverage for infrastructure user claim extraction.
// ABOUTME: Guards the documented user-id fallback chain used by authenticated API requests.

using System.Security.Claims;
using Explore.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Explore.Infrastructure.Tests.Identity;

public sealed class UserContextTests
{
    [Test]
    public async Task UserId_UsesDocumentedClaimFallbackBeforeInternalClaim()
    {
        var subUserId = Guid.NewGuid();
        var nameIdentifierUserId = Guid.NewGuid();
        var sidUserId = Guid.NewGuid();
        var internalUserId = Guid.NewGuid();
        var context = CreateUserContext(
            new Claim("sub", subUserId.ToString("D")),
            new Claim(ClaimTypes.NameIdentifier, nameIdentifierUserId.ToString("D")),
            new Claim("sid", sidUserId.ToString("D")),
            new Claim("internal_user_id", internalUserId.ToString("D")));

        await Assert.That(context.UserId).IsEqualTo(subUserId);
    }

    [Test]
    public async Task UserId_SkipsNonGuidProviderClaimsAndUsesInternalClaim()
    {
        var internalUserId = Guid.NewGuid();
        var context = CreateUserContext(
            new Claim("sub", "external-provider-subject"),
            new Claim("sid", "external-provider-session"),
            new Claim("internal_user_id", internalUserId.ToString("D")));

        await Assert.That(context.UserId).IsEqualTo(internalUserId);
    }

    [Test]
    public async Task GetRequiredUserId_ThrowsWhenNoUsableUserIdClaimExists()
    {
        var context = CreateUserContext(new Claim("sub", "external-provider-subject"));

        await Assert.That(() => context.GetRequiredUserId())
            .Throws<UnauthorizedAccessException>();
    }

    private static UserContext CreateUserContext(params Claim[] claims)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
        });

        return new UserContext(accessor);
    }
}
