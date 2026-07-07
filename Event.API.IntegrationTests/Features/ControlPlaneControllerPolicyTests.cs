// ABOUTME: API contract tests for control-plane saturation policy metadata.
// ABOUTME: Verifies admin control-plane routes use the dedicated rate-limit and timeout policies.

using System.Reflection;
using Explore.API.Controllers;
using Explore.API.Extensions;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

public sealed class ControlPlaneControllerPolicyTests
{
    [Test]
    public async Task PublicActions_UseDedicatedControlPlaneRateLimitAndTimeoutPolicies()
    {
        foreach (var method in PublicActionMethods())
        {
            var rateLimit = method.GetCustomAttribute<EnableRateLimitingAttribute>();
            var timeout = method.GetCustomAttribute<RequestTimeoutAttribute>();

            await Assert.That(rateLimit).IsNotNull();
            await Assert.That(rateLimit!.PolicyName).IsEqualTo(RateLimitingExtensions.ControlPlanePolicy);
            await Assert.That(timeout).IsNotNull();
            await Assert.That(timeout!.PolicyName).IsEqualTo(RequestTimeoutExtensions.ControlPlanePolicy);
        }
    }

    private static IEnumerable<MethodInfo> PublicActionMethods()
    {
        return typeof(ControlPlaneController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName);
    }
}
