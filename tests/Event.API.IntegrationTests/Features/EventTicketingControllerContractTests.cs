// ABOUTME: Contract tests for authenticated event ticketing API routes and ProblemDetails metadata.
// ABOUTME: Ensures all ticketing actions retain stable route names and documented failure responses.

using System.Reflection;
using Event.Api.IntegrationTests.Fixtures;
using Explore.API.Controllers;
using Explore.API.Hateoas;
using Explore.API.Filters;
using Explore.Application.Hateoas;
using Explore.Application.DTOs.EventTicketing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

[Category(TestCategories.Phase43Ticketing)]
public sealed class EventTicketingControllerContractTests
{
    [Test]
    public async Task TicketingActions_UseNamedRoutesAndProblemDetailsFailures()
    {
        var expected = new Dictionary<string, string>
        {
            [nameof(EventTicketingController.Get)] = RouteNames.GetEventTicketCatalogManagement,
            [nameof(EventTicketingController.CreateDraft)] = RouteNames.CreateEventTicketCatalogDraft,
            [nameof(EventTicketingController.CloneDraft)] = RouteNames.CloneEventTicketCatalogDraft,
            [nameof(EventTicketingController.CreateType)] = RouteNames.CreateEventTicketType,
            [nameof(EventTicketingController.UpdateType)] = RouteNames.UpdateEventTicketType,
            [nameof(EventTicketingController.DeleteType)] = RouteNames.DeleteEventTicketType,
            [nameof(EventTicketingController.CreatePool)] = RouteNames.CreateEventCapacityPool,
            [nameof(EventTicketingController.UpdatePool)] = RouteNames.UpdateEventCapacityPool,
            [nameof(EventTicketingController.DeletePool)] = RouteNames.DeleteEventCapacityPool,
            [nameof(EventTicketingController.Publish)] = RouteNames.PublishEventTicketCatalog
        };
        var conflictCapableActions = new HashSet<string>
        {
            nameof(EventTicketingController.CreateDraft),
            nameof(EventTicketingController.CloneDraft),
            nameof(EventTicketingController.UpdateType),
            nameof(EventTicketingController.CreatePool),
            nameof(EventTicketingController.UpdatePool),
            nameof(EventTicketingController.Publish)
        };

        foreach ((string actionName, string routeName) in expected)
        {
            MethodInfo action = typeof(EventTicketingController).GetMethod(actionName)
                ?? throw new InvalidOperationException($"Action {actionName} was not found.");
            HttpMethodAttribute route = action.GetCustomAttributes<HttpMethodAttribute>().Single();

            await Assert.That(route.Name).IsEqualTo(routeName);

            foreach (int statusCode in new[]
                     {
                         StatusCodes.Status400BadRequest,
                         StatusCodes.Status401Unauthorized,
                         StatusCodes.Status403Forbidden,
                         StatusCodes.Status404NotFound
                     })
            {
                ProducesResponseTypeAttribute response = action.GetCustomAttributes<ProducesResponseTypeAttribute>()
                    .Single(attribute => attribute.StatusCode == statusCode);
                await Assert.That(response.Type).IsEqualTo(typeof(ProblemDetails));
            }

            ProducesResponseTypeAttribute[] responses = action.GetCustomAttributes<ProducesResponseTypeAttribute>().ToArray();
            bool hasConflictResponse = responses.Any(response => response.StatusCode == StatusCodes.Status409Conflict);
            await Assert.That(hasConflictResponse).IsEqualTo(conflictCapableActions.Contains(actionName));
            if (hasConflictResponse)
            {
                await Assert.That(responses.Single(response => response.StatusCode == StatusCodes.Status409Conflict).Type)
                    .IsEqualTo(typeof(ProblemDetails));
            }
        }
    }

    [Test]
    public async Task ManagementRead_UsesHalResponseAndPrivateNoStore()
    {
        MethodInfo action = typeof(EventTicketingController).GetMethod(nameof(EventTicketingController.Get))
            ?? throw new InvalidOperationException("The ticketing management read action was not found.");

        ProducesResponseTypeAttribute response = action.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Single(attribute => attribute.StatusCode == StatusCodes.Status200OK);

        await Assert.That(response.Type).IsEqualTo(typeof(HalResource<EventTicketCatalogManagementDto>));
        await Assert.That(action.IsDefined(typeof(PrivateNoStoreAttribute), inherit: true)).IsTrue();
    }
}
