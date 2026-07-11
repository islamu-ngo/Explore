// ABOUTME: API contract tests for control-plane saturation policy metadata.
// ABOUTME: Verifies admin control-plane routes use the dedicated rate-limit and timeout policies.

using System.Reflection;
using Explore.API.Controllers;
using Explore.API.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
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

    [Test]
    public async Task TenantLifecycleActions_DeclareConflictProblemDetails()
    {
        string[] actionNames =
        [
            nameof(ControlPlaneController.ActivateTenant),
            nameof(ControlPlaneController.SuspendTenant),
            nameof(ControlPlaneController.ArchiveTenant),
            nameof(ControlPlaneController.ReactivateTenant),
            nameof(ControlPlaneController.ScheduleTenantPurge)
        ];

        foreach (var actionName in actionNames)
        {
            var action = typeof(ControlPlaneController).GetMethod(actionName);
            var conflict = action?.GetCustomAttributes<ProducesResponseTypeAttribute>()
                .SingleOrDefault(attribute => attribute.StatusCode == StatusCodes.Status409Conflict);

            await Assert.That(conflict).IsNotNull();
            await Assert.That(conflict!.Type).IsEqualTo(typeof(ProblemDetails));
        }
    }

    private static IEnumerable<MethodInfo> PublicActionMethods()
    {
        return typeof(ControlPlaneController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName);
    }
}
