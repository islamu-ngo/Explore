// ABOUTME: Contract tests for setup-secret rate-limit endpoint metadata.
// ABOUTME: Guards the bootstrap validation endpoint's anonymous access and fixed setup-secret policy.

using System.Reflection;
using Explore.API.Controllers;
using Explore.API.Extensions;
using Explore.API.Hateoas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Event.Api.IntegrationTests.Features;

public sealed class SetupSecretRateLimitMetadataTests
{
    [Test]
    public async Task ValidateSecretUsesAnonymousSetupSecretRateLimitPolicy()
    {
        var method = typeof(InstanceOnboardingController).GetMethod(nameof(InstanceOnboardingController.ValidateSecret));

        await Assert.That(method).IsNotNull();
        ArgumentNullException.ThrowIfNull(method);

        await Assert.That(method.GetCustomAttribute<AllowAnonymousAttribute>()).IsNotNull();

        var post = method.GetCustomAttribute<HttpPostAttribute>();
        await Assert.That(post).IsNotNull();
        await Assert.That(post!.Template).IsEqualTo("validate-secret");
        await Assert.That(post.Name).IsEqualTo(RouteNames.ValidateInstanceSetupSecret);

        var rateLimit = method.GetCustomAttribute<EnableRateLimitingAttribute>();
        await Assert.That(rateLimit).IsNotNull();
        await Assert.That(rateLimit!.PolicyName).IsEqualTo(RateLimitingExtensions.SetupSecretPolicy);
    }
}
