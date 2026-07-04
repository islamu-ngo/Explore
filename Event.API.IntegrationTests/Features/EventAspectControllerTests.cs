// ABOUTME: API route contract tests for EventAspectController endpoints.
// ABOUTME: Verifies the aspect-controller split preserves existing event aspect route, auth, cache, and response contracts.

using System.Reflection;
using Explore.API.Attributes;
using Explore.API.Controllers;
using Explore.API.Hateoas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Event.Api.IntegrationTests.Features;

public class EventAspectControllerTests
{
    [Test]
    public async Task ControllerRoute_PreservesExistingEventAspectBaseRoute()
    {
        var route = typeof(EventAspectController).GetCustomAttribute<RouteAttribute>()!;
        var tags = typeof(EventAspectController).GetCustomAttribute<TagsAttribute>()!;

        await Assert.That(route.Template).IsEqualTo("api/event");
        await Assert.That(tags.Tags).Contains("Event");
    }

    [Test]
    public async Task ReadRoutes_PreservePublicCachedAspectContracts()
    {
        await AssertReadRoute(
            nameof(EventAspectController.GetIslamicAspect),
            "{id:guid}/aspects/islamic",
            RouteNames.GetEventIslamicAspect);

        await AssertReadRoute(
            nameof(EventAspectController.GetTechAspect),
            "{id:guid}/aspects/tech",
            RouteNames.GetEventTechAspect);
    }

    [Test]
    public async Task UpsertRoutes_PreserveAuthenticatedAspectContracts()
    {
        await AssertUpsertRoute(
            nameof(EventAspectController.UpsertIslamicAspect),
            "{id:guid}/aspects/islamic",
            RouteNames.UpsertEventIslamicAspect);

        await AssertUpsertRoute(
            nameof(EventAspectController.UpsertTechAspect),
            "{id:guid}/aspects/tech",
            RouteNames.UpsertEventTechAspect);
    }

    [Test]
    public async Task DeleteRoutes_PreserveAuthenticatedAspectContracts()
    {
        await AssertDeleteRoute(
            nameof(EventAspectController.DeleteIslamicAspect),
            "{id:guid}/aspects/islamic",
            RouteNames.DeleteEventIslamicAspect);

        await AssertDeleteRoute(
            nameof(EventAspectController.DeleteTechAspect),
            "{id:guid}/aspects/tech",
            RouteNames.DeleteEventTechAspect);
    }

    [Test]
    public async Task EventController_NoLongerOwnsAspectActions()
    {
        await Assert.That(typeof(EventController).GetMethod("GetIslamicAspect")).IsNull();
        await Assert.That(typeof(EventController).GetMethod("UpsertIslamicAspect")).IsNull();
        await Assert.That(typeof(EventController).GetMethod("DeleteIslamicAspect")).IsNull();
        await Assert.That(typeof(EventController).GetMethod("GetTechAspect")).IsNull();
        await Assert.That(typeof(EventController).GetMethod("UpsertTechAspect")).IsNull();
        await Assert.That(typeof(EventController).GetMethod("DeleteTechAspect")).IsNull();
    }

    private static async Task AssertReadRoute(string actionName, string template, string routeName)
    {
        var action = typeof(EventAspectController).GetMethod(actionName)!;
        var route = action.GetCustomAttribute<HttpGetAttribute>()!;
        var classification = action.GetCustomAttribute<EndpointClassificationAttribute>()!;

        await Assert.That(route.Template).IsEqualTo(template);
        await Assert.That(route.Name).IsEqualTo(routeName);
        await Assert.That(classification.Class).IsEqualTo(EndpointClass.Public);
        await Assert.That(action.GetCustomAttribute<AllowAnonymousAttribute>()).IsNotNull();
        await Assert.That(action.GetCustomAttribute<AuthorizeAttribute>()).IsNull();
        await Assert.That(action.GetCustomAttribute<OutputCacheAttribute>()?.PolicyName).IsEqualTo("DetailData");
        await AssertProducesProblem(action, StatusCodes.Status404NotFound);
    }

    private static async Task AssertUpsertRoute(string actionName, string template, string routeName)
    {
        var action = typeof(EventAspectController).GetMethod(actionName)!;
        var route = action.GetCustomAttribute<HttpPutAttribute>()!;
        var classification = action.GetCustomAttribute<EndpointClassificationAttribute>()!;

        await Assert.That(route.Template).IsEqualTo(template);
        await Assert.That(route.Name).IsEqualTo(routeName);
        await Assert.That(classification.Class).IsEqualTo(EndpointClass.Authenticated);
        await Assert.That(action.GetCustomAttribute<AuthorizeAttribute>()).IsNotNull();
        await Assert.That(action.GetCustomAttribute<AllowAnonymousAttribute>()).IsNull();
        await Assert.That(action.GetCustomAttribute<OutputCacheAttribute>()).IsNull();
        await Assert.That(action.GetCustomAttribute<ConsumesAttribute>()?.ContentTypes.Contains("application/json")).IsTrue();
        await AssertProducesProblem(action, StatusCodes.Status400BadRequest, typeof(ValidationProblemDetails));
        await AssertProducesProblem(action, StatusCodes.Status401Unauthorized);
        await AssertProducesProblem(action, StatusCodes.Status404NotFound);
    }

    private static async Task AssertDeleteRoute(string actionName, string template, string routeName)
    {
        var action = typeof(EventAspectController).GetMethod(actionName)!;
        var route = action.GetCustomAttribute<HttpDeleteAttribute>()!;
        var classification = action.GetCustomAttribute<EndpointClassificationAttribute>()!;

        await Assert.That(route.Template).IsEqualTo(template);
        await Assert.That(route.Name).IsEqualTo(routeName);
        await Assert.That(classification.Class).IsEqualTo(EndpointClass.Authenticated);
        await Assert.That(action.GetCustomAttribute<AuthorizeAttribute>()).IsNotNull();
        await Assert.That(action.GetCustomAttribute<AllowAnonymousAttribute>()).IsNull();
        await Assert.That(action.GetCustomAttribute<OutputCacheAttribute>()).IsNull();
        await Assert.That(action.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Any(attribute => attribute.StatusCode == StatusCodes.Status204NoContent)).IsTrue();
        await AssertProducesProblem(action, StatusCodes.Status401Unauthorized);
        await AssertProducesProblem(action, StatusCodes.Status404NotFound);
    }

    private static async Task AssertProducesProblem(
        MethodInfo action,
        int statusCode,
        Type? problemType = null)
    {
        problemType ??= typeof(ProblemDetails);

        await Assert.That(action.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Any(attribute => attribute.StatusCode == statusCode && attribute.Type == problemType)).IsTrue();
    }
}
