// ABOUTME: API route contract tests for EventAgendaItemController endpoints.
// ABOUTME: Verifies grouped PATCH update contract, route-ID authority, authorization metadata, and conflict response metadata.

using System.Reflection;
using Explore.API.Attributes;
using Explore.API.Controllers;
using Explore.API.Hateoas;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventAgendaItem;
using Explore.Application.Features.EventAgendaItems.Requests.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Event.Api.IntegrationTests.Features;

public class EventAgendaItemControllerTests
{
    [Test]
    public async Task UpdateRoute_UsesPatchRouteIdAuthoritativeContract()
    {
        var action = typeof(EventAgendaItemController).GetMethod(nameof(EventAgendaItemController.Update))!;
        var route = action.GetCustomAttribute<HttpPatchAttribute>()!;
        var classification = action.GetCustomAttribute<EndpointClassificationAttribute>()!;

        await Assert.That(route.Template).IsEqualTo("{id:guid}");
        await Assert.That(route.Name).IsEqualTo(RouteNames.UpdateEventAgendaItem);
        await Assert.That(classification.Class).IsEqualTo(EndpointClass.Authenticated);
        await Assert.That(action.GetCustomAttribute<AuthorizeAttribute>()).IsNotNull();
        await Assert.That(action.GetCustomAttribute<AllowAnonymousAttribute>()).IsNull();
        await Assert.That(action.GetCustomAttribute<OutputCacheAttribute>()).IsNull();

        var commandAuthorization = typeof(UpdateEventAgendaItemCommand).GetCustomAttribute<AuthorizeResourceAttribute>()!;
        await Assert.That(commandAuthorization.Resource).IsEqualTo(ResourceKinds.EventAgendaItem);
        await Assert.That(commandAuthorization.Action).IsEqualTo(AuthorizationActions.Update);

        var id = Guid.CreateVersion7();
        var secureRequest = (ISecureRequest)new UpdateEventAgendaItemCommand
        {
            EventAgendaItemId = id,
            ExpectedConcurrencyStamp = Guid.CreateVersion7(),
            EventAgendaItemDto = new UpdateEventAgendaItemDto
            {
                Title = new UpdateEventAgendaItemTitleDto { Value = "Updated agenda item" }
            }
        };
        await Assert.That(secureRequest.ResourceId).IsEqualTo(id.ToString());

        await AssertProducesProblem(action, StatusCodes.Status403Forbidden);
        await AssertProducesProblem(action, StatusCodes.Status404NotFound);
        await AssertProducesProblem(action, StatusCodes.Status409Conflict);
    }

    private static async Task AssertProducesProblem(MethodInfo action, int statusCode)
    {
        await Assert.That(action.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Any(attribute => attribute.StatusCode == statusCode && attribute.Type == typeof(ProblemDetails))).IsTrue();
    }
}
