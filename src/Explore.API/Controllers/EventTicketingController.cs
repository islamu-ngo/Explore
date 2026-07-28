// ABOUTME: Authenticated event-scoped ticket catalog, ticket type, and capacity-pool authoring endpoints.
// ABOUTME: Delegates all platform-managed and event-authority enforcement to MediatR requests.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Extensions;
using Explore.API.Hateoas;
using Explore.Application.Features.EventTicketing;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[ApiController]
[Route("api/events/{eventId:guid}/ticketing")]
[Authorize]
[EndpointClassification(EndpointClass.Authenticated)]
public sealed class EventTicketingController(IMediator mediator) : ControllerBase
{
    [HttpGet("", Name = RouteNames.GetEventTicketCatalogManagement)]
    [ProducesResponseType(typeof(EventTicketCatalogManagementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventTicketCatalogManagementDto>> Get(Guid eventId, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetEventTicketCatalogManagementQuery(eventId), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("draft", Name = RouteNames.CreateEventTicketCatalogDraft)] [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> CreateDraft(Guid eventId, [FromBody] CreateEventTicketCatalogDraftCommand command, CancellationToken ct) => SendCreated(new CreateEventTicketCatalogDraftCommand { EventId = eventId, CurrencyCode = command.CurrencyCode }, RouteNames.GetEventTicketCatalogManagement, new { eventId }, ct);

    [HttpPost("draft:clone", Name = RouteNames.CloneEventTicketCatalogDraft)] [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> CloneDraft(Guid eventId, CancellationToken ct) => SendCreated(new CloneEventTicketCatalogDraftCommand { EventId = eventId }, RouteNames.GetEventTicketCatalogManagement, new { eventId }, ct);

    [HttpPost("ticket-types", Name = RouteNames.CreateEventTicketType)] [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> CreateType(Guid eventId, [FromBody] EventTicketTypeDto ticketType, CancellationToken ct) => SendCreated(new CreateEventTicketTypeCommand { EventId = eventId, TicketType = ticketType }, RouteNames.GetEventTicketCatalogManagement, new { eventId }, ct);

    [HttpPut("ticket-types/{ticketTypeId:guid}", Name = RouteNames.UpdateEventTicketType)] [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> UpdateType(Guid eventId, Guid ticketTypeId, [FromBody] EventTicketTypeDto ticketType, CancellationToken ct) => SendOk(new UpdateEventTicketTypeCommand { EventId = eventId, TicketTypeId = ticketTypeId, TicketType = ticketType }, ct);

    [HttpDelete("ticket-types/{ticketTypeId:guid}", Name = RouteNames.DeleteEventTicketType)] [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> DeleteType(Guid eventId, Guid ticketTypeId, CancellationToken ct) => SendOk(new DeleteEventTicketTypeCommand { EventId = eventId, TicketTypeId = ticketTypeId }, ct);

    [HttpPost("capacity-pools", Name = RouteNames.CreateEventCapacityPool)] [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> CreatePool(Guid eventId, [FromBody] EventCapacityPoolDto capacityPool, CancellationToken ct) => SendCreated(new CreateEventCapacityPoolCommand { EventId = eventId, CapacityPool = capacityPool }, RouteNames.GetEventTicketCatalogManagement, new { eventId }, ct);

    [HttpPut("capacity-pools/{capacityPoolId:guid}", Name = RouteNames.UpdateEventCapacityPool)] [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> UpdatePool(Guid eventId, Guid capacityPoolId, [FromBody] EventCapacityPoolDto capacityPool, CancellationToken ct) => SendOk(new UpdateEventCapacityPoolCommand { EventId = eventId, CapacityPoolId = capacityPoolId, CapacityPool = capacityPool }, ct);

    [HttpDelete("capacity-pools/{capacityPoolId:guid}", Name = RouteNames.DeleteEventCapacityPool)] [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> DeletePool(Guid eventId, Guid capacityPoolId, CancellationToken ct) => SendOk(new DeleteEventCapacityPoolCommand { EventId = eventId, CapacityPoolId = capacityPoolId }, ct);

    [HttpPost("publish", Name = RouteNames.PublishEventTicketCatalog)] [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)] [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> Publish(Guid eventId, CancellationToken ct) => SendOk(new PublishEventTicketCatalogCommand { EventId = eventId }, ct);

    private async Task<ActionResult<BaseCommandResponse<Guid>>> SendOk<T>(T command, CancellationToken ct) where T : IRequest<BaseCommandResponse<Guid>> { var response = await mediator.Send(command, ct); return response.Success ? Ok(response) : response.FailureCode == "event_ticketing_not_found" ? NotFound() : BadRequest(response); }
    private async Task<ActionResult<BaseCommandResponse<Guid>>> SendCreated<T>(T command, string route, object values, CancellationToken ct) where T : IRequest<BaseCommandResponse<Guid>> { var response = await mediator.Send(command, ct); return response.Success ? CreatedAtRoute(route, values, response) : response.FailureCode == "event_ticketing_not_found" ? NotFound() : BadRequest(response); }
}
