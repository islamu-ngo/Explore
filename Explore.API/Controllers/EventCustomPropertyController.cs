// ABOUTME: REST API controller for event-level custom property definition and value operations.
// ABOUTME: Manages event-local property definitions (ad-hoc or template-instantiated) and their values.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.DTOs.EventCustomProperty;
using Explore.Application.Features.EventCustomProperties.Requests.Commands;
using Explore.Application.Features.EventCustomProperties.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

/// <summary>
/// Event custom property definition and value management API endpoints.
/// All responses include HATEOAS links by default.
/// Send "Prefer: return=minimal" header to strip links.
/// </summary>
[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class EventCustomPropertyController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IResourceAssembler<EventCustomPropertyDefinitionDto, EventCustomPropertyDefinitionListDto> _resourceAssembler;

    public EventCustomPropertyController(
        IMediator mediator,
        IResourceAssembler<EventCustomPropertyDefinitionDto, EventCustomPropertyDefinitionListDto> resourceAssembler)
    {
        _mediator = mediator;
        _resourceAssembler = resourceAssembler;
    }

    /// <summary>
    /// Get event-local custom property definitions with pagination.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet(Name = RouteNames.GetEventCustomPropertyDefinitions)]
    [EndpointSummary("Get all EventCustomPropertyDefinitions")]
    [EndpointDescription("Get a paginated list of custom property definitions for a specific event. " +
        "Includes both template-instantiated and ad-hoc definitions. " +
        "Default page size is 20, max is 100. " +
        "Response includes HATEOAS navigation links (first, prev, next, last). " +
        "Send 'Prefer: return=minimal' header to strip links.")]
    [ProducesResponseType(typeof(HalCollectionResource<EventCustomPropertyDefinitionListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<EventCustomPropertyDefinitionListDto>>> GetAll(
        [FromQuery] Guid eventId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetEventCustomPropertyDefinitionListRequest
        {
            EventId = eventId,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            result,
            RouteNames.GetEventCustomPropertyDefinitions,
            additionalRouteValues: new { eventId },
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get event-local custom property definition details by ID.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet("{id:guid}", Name = RouteNames.GetEventCustomPropertyDefinitionById)]
    [EndpointSummary("Get EventCustomPropertyDefinition Details")]
    [EndpointDescription("Get full details of an event-local custom property definition including its options and provenance information. " +
        "Response includes links to related resources.")]
    [ProducesResponseType(typeof(HalResource<EventCustomPropertyDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<EventCustomPropertyDefinitionDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var definition = await _mediator.Send(new GetEventCustomPropertyDefinitionDetailsRequest { Id = id }, cancellationToken);
        if (definition == null)
            return NotFound();

        var halResource = await _resourceAssembler.ToResource(definition, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Create a new ad-hoc event-local custom property definition.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost(Name = RouteNames.CreateEventCustomPropertyDefinition)]
    [EndpointSummary("Create EventCustomPropertyDefinition")]
    [EndpointDescription("Create a new ad-hoc custom property definition for a specific event. " +
        "For template-based definitions, use the event creation endpoint with a templateId instead.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventCustomPropertyDefinitionDto definition, CancellationToken cancellationToken = default)
    {
        var command = new CreateEventCustomPropertyDefinitionCommand
        {
            DefinitionDto = definition
        };

        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtRoute(
            RouteNames.GetEventCustomPropertyDefinitionById,
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Update an event-local custom property definition.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("{id:guid}", Name = RouteNames.UpdateEventCustomPropertyDefinition)]
    [EndpointSummary("Update EventCustomPropertyDefinition")]
    [EndpointDescription("Update an existing event-local custom property definition and replace its option set. " +
        "Provenance fields (source template information) are read-only and preserved.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(
        Guid id,
        [FromBody] UpdateEventCustomPropertyDefinitionDto updateDto, CancellationToken cancellationToken = default)
    {
        if (id != updateDto.Id)
        {
            return BadRequest(new { error = "EventCustomPropertyDefinition ID mismatch" });
        }

        var command = new UpdateEventCustomPropertyDefinitionCommand
        {
            DefinitionDto = updateDto
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete an event-local custom property definition.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteEventCustomPropertyDefinition)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteEventCustomPropertyDefinitionCommand { Id = id };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Get all custom property values for an event.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet("values", Name = RouteNames.GetEventCustomPropertyValues)]
    [EndpointSummary("Get EventCustomPropertyValues")]
    [EndpointDescription("Get all custom property values for a specific event across all definitions.")]
    [ProducesResponseType(typeof(List<EventCustomPropertyValueDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<EventCustomPropertyValueDto>>> GetValues(
        [FromQuery] Guid eventId, CancellationToken cancellationToken = default)
    {
        var values = await _mediator.Send(new GetEventCustomPropertyValuesRequest
        {
            EventId = eventId
        }, cancellationToken);

        return Ok(values);
    }

    /// <summary>
    /// Set a single custom property value for an event.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("value", Name = RouteNames.SetEventCustomPropertyValue)]
    [EndpointSummary("Set EventCustomPropertyValue")]
    [EndpointDescription("Set or update a single custom property value for an event. " +
        "Uses upsert semantics based on definition ID, event ID, and ordinal.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> SetValue(
        [FromBody] SetEventCustomPropertyValueDto valueDto, CancellationToken cancellationToken = default)
    {
        var command = new SetEventCustomPropertyValueCommand
        {
            ValueDto = valueDto
        };

        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Set multiple values for a multi-value custom property definition.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("values", Name = RouteNames.SetEventCustomPropertyMultiValues)]
    [EndpointSummary("Set EventCustomPropertyMultiValues")]
    [EndpointDescription("Atomically replace all values for a multi-value custom property definition. " +
        "All existing values for the definition+event combination are removed and replaced.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> SetMultiValues(
        [FromBody] SetEventCustomPropertyMultiValuesDto multiValuesDto, CancellationToken cancellationToken = default)
    {
        var command = new SetEventCustomPropertyMultiValuesCommand
        {
            DefinitionId = multiValuesDto.DefinitionId,
            EventId = multiValuesDto.EventId,
            Values = multiValuesDto.Values
        };

        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}
