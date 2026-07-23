// ABOUTME: API integration and route contract tests for event controller endpoints.
// ABOUTME: Verifies public event reads and authenticated lifecycle/management contracts.

using System.Net;
using System.Reflection;
using Event.Api.IntegrationTests.Fixtures;
using Explore.API.Attributes;
using Explore.API.Controllers;
using Explore.API.Hateoas;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Features.Events.Requests.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

[NotInParallel("ApiTestFixture")]
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class EventsControllerTests
{
    private readonly ApiTestFixture _fixture;

    public EventsControllerTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task GetAll_ShouldReturnOk()
    {
        // Act
        // Route is "api/[controller]" -> "api/Event" (singular based on class name EventController)
        var response = await _fixture.Client.GetAsync("/api/event");

        // Debug: Print content if error
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[TEST DEBUG] Status: {response.StatusCode}, Content: {content}");
        }

        // Assert
        // Note: It might return 401 Unauthorized if AllowAnonymous is not set on the endpoint.
        // Assuming public read access based on conventions "GET = AllowAnonymous".
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetById_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var response = await _fixture.Client.GetAsync($"/api/event/{Guid.NewGuid()}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetOpenGraphImageRoute_UsesExplicitAnonymousPngContract()
    {
        var action = typeof(EventController).GetMethod(nameof(EventController.GetOpenGraphImage))!;
        var route = action.GetCustomAttribute<HttpGetAttribute>()!;
        var classification = action.GetCustomAttribute<EndpointClassificationAttribute>()!;
        var produces = action.GetCustomAttribute<ProducesAttribute>()!;
        var responseTypes = action.GetCustomAttributes<ProducesResponseTypeAttribute>().ToArray();

        await Assert.That(route.Template).IsEqualTo("public/{slugCode}/og-image");
        await Assert.That(route.Name).IsEqualTo(RouteNames.GetEventOpenGraphImage);
        await Assert.That(classification.Class).IsEqualTo(EndpointClass.Public);
        await Assert.That(action.GetCustomAttribute<AllowAnonymousAttribute>()).IsNotNull();
        await Assert.That(action.GetCustomAttribute<AuthorizeAttribute>()).IsNull();
        await Assert.That(action.GetCustomAttribute<OutputCacheAttribute>()).IsNull();
        await Assert.That(produces.ContentTypes.Contains("image/png")).IsTrue();
        await Assert.That(responseTypes.Any(attribute =>
            attribute.StatusCode == StatusCodes.Status200OK && attribute.Type == typeof(FileContentResult))).IsTrue();
        await Assert.That(responseTypes.Any(attribute =>
            attribute.StatusCode == StatusCodes.Status400BadRequest && attribute.Type == typeof(ProblemDetails))).IsTrue();
        await Assert.That(responseTypes.Any(attribute =>
            attribute.StatusCode == StatusCodes.Status404NotFound && attribute.Type == typeof(ProblemDetails))).IsTrue();
        await Assert.That(responseTypes.Any(attribute => attribute.StatusCode == StatusCodes.Status304NotModified)).IsTrue();
    }

    [Test]
    public async Task GetManagementDetailsRoute_UsesExplicitAuthenticatedContract()
    {
        var action = typeof(EventController).GetMethod(nameof(EventController.GetManagementDetails))!;
        var route = action.GetCustomAttribute<HttpGetAttribute>()!;
        var classification = action.GetCustomAttribute<EndpointClassificationAttribute>()!;

        await Assert.That(route.Template).IsEqualTo("{id:guid}/management-detail");
        await Assert.That(route.Name).IsEqualTo(RouteNames.GetEventManagementDetails);
        await Assert.That(classification.Class).IsEqualTo(EndpointClass.Authenticated);
        await Assert.That(action.GetCustomAttribute<AuthorizeAttribute>()).IsNotNull();
        await Assert.That(action.GetCustomAttribute<OutputCacheAttribute>()).IsNull();
        var requestAuthorization = typeof(GetEventManagementDetailsRequest).GetCustomAttribute<AuthorizeResourceAttribute>()!;
        await Assert.That(requestAuthorization.Resource).IsEqualTo(ResourceKinds.Event);
        await Assert.That(requestAuthorization.Action).IsEqualTo(AuthorizationActions.Events.ViewManagement);
        await Assert.That(action.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Any(attribute => attribute.StatusCode == StatusCodes.Status403Forbidden && attribute.Type == typeof(ProblemDetails))).IsTrue();
        await Assert.That(action.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Any(attribute => attribute.StatusCode == StatusCodes.Status404NotFound && attribute.Type == typeof(ProblemDetails))).IsTrue();
    }

    [Test]
    public async Task GetModerationHistoryRoute_UsesExplicitAuthenticatedManagementContract()
    {
        var id = Guid.NewGuid();
        var action = typeof(EventController).GetMethod(nameof(EventController.GetModerationHistory))!;
        var route = action.GetCustomAttribute<HttpGetAttribute>()!;
        var classification = action.GetCustomAttribute<EndpointClassificationAttribute>()!;

        await Assert.That(route.Template).IsEqualTo("{id:guid}/moderation/history");
        await Assert.That(route.Name).IsEqualTo(RouteNames.GetEventModerationHistory);
        await Assert.That(classification.Class).IsEqualTo(EndpointClass.Authenticated);
        await Assert.That(action.GetCustomAttribute<AuthorizeAttribute>()).IsNotNull();
        await Assert.That(action.GetCustomAttribute<AllowAnonymousAttribute>()).IsNull();
        await Assert.That(action.GetCustomAttribute<OutputCacheAttribute>()).IsNull();

        var requestAuthorization = typeof(GetEventModerationHistoryRequest).GetCustomAttribute<AuthorizeResourceAttribute>()!;
        await Assert.That(requestAuthorization.Resource).IsEqualTo(ResourceKinds.Event);
        await Assert.That(requestAuthorization.Action).IsEqualTo(AuthorizationActions.Events.ViewManagement);

        var secureRequest = (ISecureRequest)new GetEventModerationHistoryRequest { Id = id };
        await Assert.That(secureRequest.ResourceId).IsEqualTo(id.ToString());
        await AssertProducesProblem(action, StatusCodes.Status403Forbidden);
        await AssertProducesProblem(action, StatusCodes.Status404NotFound);
    }

    [Test]
    public async Task GetPublishReadinessRoute_UsesExplicitAuthenticatedLifecycleContract()
    {
        var id = Guid.NewGuid();
        var action = typeof(EventController).GetMethod(nameof(EventController.GetPublishReadiness))!;
        var route = action.GetCustomAttribute<HttpGetAttribute>()!;
        var classification = action.GetCustomAttribute<EndpointClassificationAttribute>()!;

        await Assert.That(route.Template).IsEqualTo("{id:guid}/publish-readiness");
        await Assert.That(route.Name).IsEqualTo(RouteNames.GetEventPublishReadiness);
        await Assert.That(classification.Class).IsEqualTo(EndpointClass.Authenticated);
        await Assert.That(action.GetCustomAttribute<AuthorizeAttribute>()).IsNotNull();
        await Assert.That(action.GetCustomAttribute<AllowAnonymousAttribute>()).IsNull();
        await Assert.That(action.GetCustomAttribute<OutputCacheAttribute>()).IsNull();

        var requestAuthorization = typeof(GetEventPublishReadinessRequest).GetCustomAttribute<AuthorizeResourceAttribute>()!;
        await Assert.That(requestAuthorization.Resource).IsEqualTo(ResourceKinds.Event);
        await Assert.That(requestAuthorization.Action).IsEqualTo(AuthorizationActions.Update);

        var secureRequest = (ISecureRequest)new GetEventPublishReadinessRequest { Id = id };
        await Assert.That(secureRequest.ResourceId).IsEqualTo(id.ToString());
        await AssertProducesProblem(action, StatusCodes.Status403Forbidden);
        await AssertProducesProblem(action, StatusCodes.Status404NotFound);
    }

    [Test]
    public async Task ImportRoute_UsesExplicitAuthenticatedLifecycleContract()
    {
        var tenantId = Guid.NewGuid();
        var action = typeof(EventController).GetMethod(nameof(EventController.Import))!;
        var route = action.GetCustomAttribute<HttpPostAttribute>()!;
        var classification = action.GetCustomAttribute<EndpointClassificationAttribute>()!;

        await Assert.That(route.Template).IsEqualTo("import");
        await Assert.That(route.Name).IsEqualTo(RouteNames.ImportEvent);
        await Assert.That(classification.Class).IsEqualTo(EndpointClass.Authenticated);
        await Assert.That(action.GetCustomAttribute<AuthorizeAttribute>()).IsNotNull();
        await Assert.That(action.GetCustomAttribute<AllowAnonymousAttribute>()).IsNull();
        await Assert.That(action.GetCustomAttribute<OutputCacheAttribute>()).IsNull();

        var commandAuthorization = typeof(ImportEventCommand).GetCustomAttribute<AuthorizeResourceAttribute>()!;
        await Assert.That(commandAuthorization.Resource).IsEqualTo(ResourceKinds.Event);
        await Assert.That(commandAuthorization.Action).IsEqualTo(AuthorizationActions.Create);

        var secureRequest = (ISecureRequest)new ImportEventCommand
        {
            Request = new ImportEventRequestDto
            {
                Title = "Imported event",
                TenantId = tenantId,
                OwnerActorId = Guid.NewGuid(),
                ProvenanceSource = "test",
                ProvenanceExternalId = "external-1"
            }
        };
        await Assert.That(secureRequest.ResourceId).IsEqualTo(tenantId.ToString());
        await AssertProducesProblem(action, StatusCodes.Status403Forbidden);
    }

    [Test]
    public async Task EventLifecyclePostRoutes_UseExplicitAuthenticatedContracts()
    {
        await AssertEventLifecyclePostRoute(
            nameof(EventController.Publish),
            "{id:guid}/publish",
            RouteNames.PublishEvent,
            typeof(PublishEventCommand),
            AuthorizationActions.Update);

        await AssertEventLifecyclePostRoute(
            nameof(EventController.Archive),
            "{id:guid}/archive",
            RouteNames.ArchiveEvent,
            typeof(ArchiveEventCommand),
            AuthorizationActions.Update);

        await AssertEventLifecyclePostRoute(
            nameof(EventController.Cancel),
            "{id:guid}/cancel",
            RouteNames.CancelEvent,
            typeof(CancelEventCommand),
            AuthorizationActions.Update);
    }

    [Test]
    public async Task ModerateLightRoute_UsesExplicitAuthenticatedContract()
    {
        var action = typeof(EventController).GetMethod(nameof(EventController.ModerateLight))!;
        var route = action.GetCustomAttribute<HttpPostAttribute>()!;
        var classification = action.GetCustomAttribute<EndpointClassificationAttribute>()!;

        await Assert.That(route.Template).IsEqualTo("{id:guid}/moderation/light");
        await Assert.That(route.Name).IsEqualTo(RouteNames.ModerateEventLight);
        await Assert.That(classification.Class).IsEqualTo(EndpointClass.Authenticated);
        await Assert.That(action.GetCustomAttribute<AuthorizeAttribute>()).IsNotNull();
        await AssertModerationRequestBody(action);
        await Assert.That(action.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Any(attribute => attribute.StatusCode == StatusCodes.Status403Forbidden && attribute.Type == typeof(ProblemDetails))).IsTrue();
        await Assert.That(action.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Any(attribute => attribute.StatusCode == StatusCodes.Status409Conflict && attribute.Type == typeof(ProblemDetails))).IsTrue();
    }

    [Test]
    public async Task ModerateHeavyRoute_UsesExplicitAuthenticatedContract()
    {
        var action = typeof(EventController).GetMethod(nameof(EventController.ModerateHeavy))!;
        var route = action.GetCustomAttribute<HttpPostAttribute>()!;
        var classification = action.GetCustomAttribute<EndpointClassificationAttribute>()!;

        await Assert.That(route.Template).IsEqualTo("{id:guid}/moderation/heavy");
        await Assert.That(route.Name).IsEqualTo(RouteNames.ModerateEventHeavy);
        await Assert.That(classification.Class).IsEqualTo(EndpointClass.Authenticated);
        await Assert.That(action.GetCustomAttribute<AuthorizeAttribute>()).IsNotNull();
        await AssertModerationRequestBody(action);
        await Assert.That(action.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Any(attribute => attribute.StatusCode == StatusCodes.Status403Forbidden && attribute.Type == typeof(ProblemDetails))).IsTrue();
        await Assert.That(action.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Any(attribute => attribute.StatusCode == StatusCodes.Status409Conflict && attribute.Type == typeof(ProblemDetails))).IsTrue();
        await Assert.That(action.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Any(attribute => attribute.StatusCode == StatusCodes.Status503ServiceUnavailable && attribute.Type == typeof(ProblemDetails))).IsTrue();
    }

    [Test]
    public async Task UnmoderateRoute_UsesExplicitAuthenticatedContract()
    {
        var action = typeof(EventController).GetMethod(nameof(EventController.Unmoderate))!;
        var route = action.GetCustomAttribute<HttpPostAttribute>()!;
        var classification = action.GetCustomAttribute<EndpointClassificationAttribute>()!;

        await Assert.That(route.Template).IsEqualTo("{id:guid}/moderation/unmoderate");
        await Assert.That(route.Name).IsEqualTo(RouteNames.UnmoderateEvent);
        await Assert.That(classification.Class).IsEqualTo(EndpointClass.Authenticated);
        await Assert.That(action.GetCustomAttribute<AuthorizeAttribute>()).IsNotNull();
        await AssertModerationRequestBody(action);
        await Assert.That(action.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Any(attribute => attribute.StatusCode == StatusCodes.Status403Forbidden && attribute.Type == typeof(ProblemDetails))).IsTrue();
        await Assert.That(action.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Any(attribute => attribute.StatusCode == StatusCodes.Status409Conflict && attribute.Type == typeof(ProblemDetails))).IsTrue();
    }

    private static async Task AssertEventLifecyclePostRoute(
        string actionName,
        string template,
        string routeName,
        Type commandType,
        string authorizationAction)
    {
        var action = typeof(EventController).GetMethod(actionName)!;
        var route = action.GetCustomAttribute<HttpPostAttribute>()!;
        var classification = action.GetCustomAttribute<EndpointClassificationAttribute>()!;

        await Assert.That(route.Template).IsEqualTo(template);
        await Assert.That(route.Name).IsEqualTo(routeName);
        await Assert.That(classification.Class).IsEqualTo(EndpointClass.Authenticated);
        await Assert.That(action.GetCustomAttribute<AuthorizeAttribute>()).IsNotNull();
        await Assert.That(action.GetCustomAttribute<AllowAnonymousAttribute>()).IsNull();
        await Assert.That(action.GetCustomAttribute<OutputCacheAttribute>()).IsNull();

        var commandAuthorization = commandType.GetCustomAttribute<AuthorizeResourceAttribute>()!;
        await Assert.That(commandAuthorization.Resource).IsEqualTo(ResourceKinds.Event);
        await Assert.That(commandAuthorization.Action).IsEqualTo(authorizationAction);
        await AssertProducesProblem(action, StatusCodes.Status403Forbidden);
        await AssertProducesProblem(action, StatusCodes.Status404NotFound);
        await AssertProducesProblem(action, StatusCodes.Status409Conflict);
    }

    private static async Task AssertProducesProblem(MethodInfo action, int statusCode)
    {
        await Assert.That(action.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Any(attribute => attribute.StatusCode == statusCode && attribute.Type == typeof(ProblemDetails))).IsTrue();
    }

    private static async Task AssertModerationRequestBody(MethodInfo action)
    {
        var requestParameter = action.GetParameters()
            .SingleOrDefault(parameter => parameter.ParameterType == typeof(EventModerationRequestDto));

        await Assert.That(requestParameter).IsNotNull();
        await Assert.That(requestParameter!.GetCustomAttribute<FromBodyAttribute>()).IsNotNull();
        await Assert.That(action.GetCustomAttribute<ConsumesAttribute>()?.ContentTypes.Contains("application/json")).IsTrue();
    }
}
