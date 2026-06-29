// ABOUTME: API route contract tests for EventSeriesController endpoints.
// ABOUTME: Verifies grouped PATCH update contract, route-ID authority, and conflict response metadata.

using System.Reflection;
using Explore.API.Attributes;
using Explore.API.Controllers;
using Explore.API.Hateoas;
using Explore.Application.DTOs.EventSeries;
using Explore.Application.Features.EventSeries.Requests.Commands;
using Explore.Application.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Event.Api.IntegrationTests.Features;

public class EventSeriesControllerTests
{
    [Test]
    public async Task UpdateRoute_UsesPatchRouteIdAuthoritativeContract()
    {
        var action = typeof(EventSeriesController).GetMethod(nameof(EventSeriesController.Update))!;
        var route = action.GetCustomAttribute<HttpPatchAttribute>()!;
        var classification = action.GetCustomAttribute<EndpointClassificationAttribute>()!;

        await Assert.That(route.Template).IsEqualTo("{id:guid}");
        await Assert.That(route.Name).IsEqualTo(RouteNames.UpdateEventSeries);
        await Assert.That(classification.Class).IsEqualTo(EndpointClass.Authenticated);
        await Assert.That(action.GetCustomAttribute<AuthorizeAttribute>()).IsNotNull();
        await Assert.That(action.GetCustomAttribute<AllowAnonymousAttribute>()).IsNull();
        await Assert.That(action.GetCustomAttribute<OutputCacheAttribute>()).IsNull();
        await Assert.That(typeof(UpdateEventSeriesDto).GetProperty("Id")).IsNull();

        var id = Guid.CreateVersion7();
        var command = new UpdateEventSeriesCommand
        {
            EventSeriesId = id,
            ExpectedConcurrencyStamp = Guid.CreateVersion7(),
            EventSeriesDto = new UpdateEventSeriesDto
            {
                Description = new UpdateEventSeriesDescriptionDto { Value = OptionalUpdate<string?>.Set("Updated") }
            }
        };

        await Assert.That(command.EventSeriesId).IsEqualTo(id);
        await Assert.That(command.EventSeriesDto.Description).IsNotNull();

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
