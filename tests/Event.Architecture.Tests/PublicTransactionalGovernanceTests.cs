// ABOUTME: Architecture tests for the anonymous, idempotent PublicTransactional endpoint contract.
// ABOUTME: Establishes the Phase 3 classification and required-key governance baseline before runtime enforcement.

namespace Event.Architecture.Tests;

using System.Reflection;
using Explore.API.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;

public class PublicTransactionalGovernanceTests
{
    [Test]
    public async Task EndpointClassification_ExposesPublicTransactionalAndRequiredKeyMarker()
    {
        var classification = Enum.GetNames<EndpointClass>()
            .SingleOrDefault(name => name == "PublicTransactional");
        var requiredKeyAttribute = typeof(EndpointClass).Assembly.GetType(
            "Explore.API.Attributes.RequireIdempotencyKeyAttribute");

        await Assert.That(classification).IsEqualTo("PublicTransactional");
        await Assert.That(requiredKeyAttribute).IsNotNull();
    }

    [Test]
    public async Task PublicTransactionalRules_AcceptCompliantSyntheticPost()
    {
        var violations = PublicTransactionalEndpointGovernance.FindViolations(
            new[] { typeof(CompliantPublicTransactionalController) });

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task PublicTransactionalRules_RejectEveryMissingRequiredInvariant()
    {
        var violations = PublicTransactionalEndpointGovernance.FindViolations(
            new[] { typeof(NonCompliantPublicTransactionalController) });

        await Assert.That(violations).Contains("NonCompliantPublicTransactionalController.Get: must use only unsafe HTTP verbs.");
        await Assert.That(violations).Contains("NonCompliantPublicTransactionalController.PostWithoutKey: POST actions must declare [RequireIdempotencyKey].");
        await Assert.That(violations).Contains("NonCompliantPublicTransactionalController.PostWithWrongPolicy: must use [EnableRateLimiting(\"public_transactional\")].");
        await Assert.That(violations).Contains("NonCompliantPublicTransactionalController.PostWithoutAnonymous: must be effectively [AllowAnonymous].");
        await Assert.That(violations).Contains("NonCompliantPublicTransactionalController.PostWithAntiforgery: must not declare API antiforgery metadata.");
    }

    [EndpointClassification(EndpointClass.PublicTransactional)]
    private sealed class CompliantPublicTransactionalController : ControllerBase
    {
        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting("public_transactional")]
        [RequireIdempotencyKey]
        public OkResult Post() => Ok();
    }

    [EndpointClassification(EndpointClass.PublicTransactional)]
    private sealed class NonCompliantPublicTransactionalController : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        [EnableRateLimiting("public_transactional")]
        public OkResult Get() => Ok();

        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting("public_transactional")]
        public OkResult PostWithoutKey() => Ok();

        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting("global")]
        [RequireIdempotencyKey]
        public OkResult PostWithWrongPolicy() => Ok();

        [HttpPost]
        [EnableRateLimiting("public_transactional")]
        [RequireIdempotencyKey]
        public OkResult PostWithoutAnonymous() => Ok();

        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting("public_transactional")]
        [RequireIdempotencyKey]
        [ValidateAntiForgeryToken]
        public OkResult PostWithAntiforgery() => Ok();
    }
}

internal static class PublicTransactionalEndpointGovernance
{
    private const string PublicTransactionalRateLimitPolicy = "public_transactional";

    private static readonly HashSet<string> UnsafeHttpMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete
    };

    private static readonly HashSet<string> AntiforgeryMetadataNames = new(StringComparer.Ordinal)
    {
        "Microsoft.AspNetCore.Antiforgery.IAntiforgeryMetadata",
        "Microsoft.AspNetCore.Mvc.ViewFeatures.IAntiforgeryPolicy",
        "Microsoft.AspNetCore.Antiforgery.DisableAntiforgeryAttribute",
        "Microsoft.AspNetCore.Mvc.ValidateAntiForgeryTokenAttribute",
        "Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute",
        "Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryTokenAttribute"
    };

    public static IReadOnlyList<string> FindViolations(IEnumerable<Type> controllerTypes)
    {
        var violations = new List<string>();

        foreach (var controller in controllerTypes)
        {
            var actions = controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(IsHttpAction);

            foreach (var action in actions)
            {
                if (ResolveAttribute<EndpointClassificationAttribute>(controller, action)?.Class
                    != EndpointClass.PublicTransactional)
                {
                    continue;
                }

                var actionId = $"{controller.Name}.{action.Name}";

                if (!HasEffectiveAttribute<AllowAnonymousAttribute>(controller, action))
                {
                    violations.Add($"{actionId}: must be effectively [AllowAnonymous].");
                }

                if (!UsesOnlyUnsafeHttpVerbs(action))
                {
                    violations.Add($"{actionId}: must use only unsafe HTTP verbs.");
                }

                if (ResolveAttribute<EnableRateLimitingAttribute>(controller, action)?.PolicyName
                    != PublicTransactionalRateLimitPolicy)
                {
                    violations.Add($"{actionId}: must use [EnableRateLimiting(\"public_transactional\")].");
                }

                if (HasEffectiveAntiforgeryMetadata(controller, action))
                {
                    violations.Add($"{actionId}: must not declare API antiforgery metadata.");
                }

                if (HasAttribute<HttpPostAttribute>(action)
                    && !HasEffectiveAttribute<RequireIdempotencyKeyAttribute>(controller, action))
                {
                    violations.Add($"{actionId}: POST actions must declare [RequireIdempotencyKey].");
                }
            }
        }

        return violations;
    }

    private static bool IsHttpAction(MethodInfo method) =>
        !method.IsSpecialName && method.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any();

    private static bool UsesOnlyUnsafeHttpVerbs(MethodInfo method)
    {
        var httpMethods = method.GetCustomAttributes<HttpMethodAttribute>(inherit: true)
            .SelectMany(attribute => attribute.HttpMethods)
            .ToArray();

        return httpMethods.Length > 0 && httpMethods.All(UnsafeHttpMethods.Contains);
    }

    private static bool HasEffectiveAntiforgeryMetadata(Type controller, MethodInfo action) =>
        HasAntiforgeryMetadata(controller) || HasAntiforgeryMetadata(action);

    private static bool HasAntiforgeryMetadata(MemberInfo member) =>
        member.GetCustomAttributes(inherit: true).Any(attribute =>
            AntiforgeryMetadataNames.Contains(attribute.GetType().FullName ?? string.Empty)
            || attribute.GetType().GetInterfaces().Any(interfaceType =>
                AntiforgeryMetadataNames.Contains(interfaceType.FullName ?? string.Empty)));

    private static bool HasEffectiveAttribute<TAttribute>(Type controller, MethodInfo action)
        where TAttribute : Attribute =>
        HasAttribute<TAttribute>(controller) || HasAttribute<TAttribute>(action);

    private static TAttribute? ResolveAttribute<TAttribute>(Type controller, MethodInfo action)
        where TAttribute : Attribute =>
        action.GetCustomAttributes<TAttribute>(inherit: true).FirstOrDefault()
        ?? controller.GetCustomAttributes<TAttribute>(inherit: true).FirstOrDefault();

    private static bool HasAttribute<TAttribute>(MemberInfo member)
        where TAttribute : Attribute =>
        member.GetCustomAttributes<TAttribute>(inherit: true).Any();

}
