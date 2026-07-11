// ABOUTME: Tests public authentication-token DTOs do not expose credential material.
// ABOUTME: Guards generated API contracts from serializing access, refresh, ID, or DPoP secrets.

using Explore.Application.DTOs.UserAuthenticationToken;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.UserAuthenticationTokens.Queries;

public class UserAuthenticationTokenDtoPrivacyTests
{
    [Test]
    public async Task DetailDto_DoesNotExposeCredentialMaterial()
    {
        var propertyNames = typeof(UserAuthenticationTokenDto)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        await Assert.That(propertyNames).DoesNotContain(nameof(CreateUserAuthenticationTokenDto.AccessToken));
        await Assert.That(propertyNames).DoesNotContain(nameof(CreateUserAuthenticationTokenDto.RefreshToken));
        await Assert.That(propertyNames).DoesNotContain(nameof(CreateUserAuthenticationTokenDto.DpopKey));
        await Assert.That(propertyNames).DoesNotContain(nameof(CreateUserAuthenticationTokenDto.IdToken));
        await Assert.That(propertyNames).DoesNotContain("UserEmail");
        await Assert.That(propertyNames).DoesNotContain("UserFullName");
        await Assert.That(propertyNames).DoesNotContain("TenantFullName");
    }

    [Test]
    public async Task ListDto_DoesNotExposeCredentialOrIdentityMaterial()
    {
        var propertyNames = typeof(UserAuthenticationTokenListDto)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        await Assert.That(propertyNames).DoesNotContain(nameof(CreateUserAuthenticationTokenDto.AccessToken));
        await Assert.That(propertyNames).DoesNotContain(nameof(CreateUserAuthenticationTokenDto.RefreshToken));
        await Assert.That(propertyNames).DoesNotContain(nameof(CreateUserAuthenticationTokenDto.DpopKey));
        await Assert.That(propertyNames).DoesNotContain(nameof(CreateUserAuthenticationTokenDto.IdToken));
        await Assert.That(propertyNames).DoesNotContain("UserEmail");
        await Assert.That(propertyNames).DoesNotContain("UserFullName");
        await Assert.That(propertyNames).DoesNotContain("TenantFullName");
    }
}
