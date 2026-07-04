// ABOUTME: Metadata contract tests for user authentication token endpoints.
// ABOUTME: Ensures sensitive token session routes remain authenticated and non-cacheable.

using System.Reflection;
using Explore.API.Attributes;
using Explore.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Event.Api.IntegrationTests.Features;

public sealed class UserAuthenticationTokenControllerMetadataTests
{
    [Test]
    public async Task ControllerIsAuthenticatedEndpointClass()
    {
        var controllerType = typeof(UserAuthenticationTokenController);

        await Assert.That(controllerType.GetCustomAttribute<EndpointClassificationAttribute>()?.Class)
            .IsEqualTo(EndpointClass.Authenticated);
    }

    [Test]
    public async Task ActionsRequireAuthenticationAndAdvertiseAuthFailures()
    {
        foreach (var action in SensitiveActions())
        {
            await Assert.That(action.GetCustomAttribute<AuthorizeAttribute>())
                .IsNotNull()
                .Because($"{action.Name} exposes per-user token session data or mutations.");

            AssertProducesProblem(action, StatusCodes.Status401Unauthorized);
            AssertProducesProblem(action, StatusCodes.Status403Forbidden);
        }
    }

    [Test]
    public async Task ReadActionsDoNotUseSharedOutputCache()
    {
        foreach (var action in ReadActions())
        {
            await Assert.That(action.GetCustomAttribute<OutputCacheAttribute>())
                .IsNull()
                .Because($"{action.Name} returns user-scoped token session metadata.");

            var responseCache = action.GetCustomAttribute<ResponseCacheAttribute>();

            await Assert.That(responseCache).IsNotNull();
            await Assert.That(responseCache!.NoStore).IsTrue();
            await Assert.That(responseCache.Location).IsEqualTo(ResponseCacheLocation.None);
        }
    }

    private static IReadOnlyList<MethodInfo> SensitiveActions()
    {
        return
        [
            Action(nameof(UserAuthenticationTokenController.GetAll)),
            Action(nameof(UserAuthenticationTokenController.GetById)),
            Action(nameof(UserAuthenticationTokenController.Create)),
            Action(nameof(UserAuthenticationTokenController.Update)),
            Action(nameof(UserAuthenticationTokenController.Delete))
        ];
    }

    private static IReadOnlyList<MethodInfo> ReadActions()
    {
        return
        [
            Action(nameof(UserAuthenticationTokenController.GetAll)),
            Action(nameof(UserAuthenticationTokenController.GetById))
        ];
    }

    private static MethodInfo Action(string name)
    {
        var action = typeof(UserAuthenticationTokenController).GetMethod(name);
        ArgumentNullException.ThrowIfNull(action);
        return action;
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
