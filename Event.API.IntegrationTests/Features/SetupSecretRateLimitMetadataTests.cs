// ABOUTME: Contract tests for setup-secret rate-limit endpoint metadata.
// ABOUTME: Guards the bootstrap validation endpoint's anonymous access and fixed setup-secret policy.

using System.Reflection;
using Explore.API.Controllers;
using Explore.API.Extensions;
using Explore.API.Filters;
using Explore.API.Hateoas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

        AssertProducesProblem(method, StatusCodes.Status410Gone);
        AssertProducesProblem(method, StatusCodes.Status429TooManyRequests);
    }

    [Test]
    public async Task SetupSecretRequiredActionsUseSetupSecretRateLimitPolicy()
    {
        var methods = SetupSecretRequiredActions();

        await Assert.That(methods).IsNotEmpty();

        foreach (var method in methods)
        {
            var rateLimit = method.GetCustomAttribute<EnableRateLimitingAttribute>();

            await Assert.That(rateLimit)
                .IsNotNull()
                .Because($"{method.Name} is setup-secret gated and must use the setup-secret brute-force limiter.");
            await Assert.That(rateLimit!.PolicyName).IsEqualTo(RateLimitingExtensions.SetupSecretPolicy);
        }
    }

    [Test]
    public async Task SetupSecretRequiredActionsAdvertiseProblemDetailsForFilterFailures()
    {
        var methods = SetupSecretRequiredActions();

        await Assert.That(methods).IsNotEmpty();

        foreach (var method in methods)
        {
            AssertProducesProblem(method, StatusCodes.Status403Forbidden);
            AssertProducesProblem(method, StatusCodes.Status410Gone);
            AssertProducesProblem(method, StatusCodes.Status429TooManyRequests);
        }
    }

    private static IReadOnlyList<MethodInfo> SetupSecretRequiredActions()
    {
        return typeof(InstanceOnboardingController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<SetupSecretRequiredAttribute>() is not null)
            .OrderBy(method => method.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static void AssertProducesProblem(MethodInfo method, int statusCode)
    {
        var hasProblemMetadata = method.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Any(attribute => attribute.StatusCode == statusCode && attribute.Type == typeof(ProblemDetails));

        if (!hasProblemMetadata)
        {
            throw new InvalidOperationException(
                $"{method.Name} must advertise ProblemDetails for HTTP {statusCode}.");
        }
    }
}
