// ABOUTME: REST API controller for event-session-level custom property definition and value operations.
// ABOUTME: Manages session-local property definitions (ad-hoc or template-instantiated) and their values.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.DTOs.EventSessionCustomProperty;
using Explore.Application.Features.EventSessionCustomProperties.Requests.Commands;
using Explore.Application.Features.EventSessionCustomProperties.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

/// <summary>
/// Event session custom property definition and value management API endpoints.
/// All responses include HATEOAS links by default.
/// Send "Prefer: return=minimal" header to strip links.
/// </summary>
[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class EventSessionCustomPropertyController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IResourceAssembler<EventSessionCustomPropertyDefinitionDto, EventSessionCustomPropertyDefinitionListDto> _resourceAssembler;

    public EventSessionCustomPropertyController(
        IMediator mediator,
        IResourceAssembler<EventSessionCustomPropertyDefinitionDto, EventSessionCustomPropertyDefinitionListDto> resourceAssembler)
    {
        _mediator = mediator;
        _resourceAssembler = resourceAssembler;
    }

    /// <summary>
    /// Get event-session-local custom property definitions with pagination.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet(Name = RouteNames.GetEventSessionCustomPropertyDefinitions)]
    [EndpointSummary("Get all EventSessionCustomPropertyDefinitions")]
    [EndpointDescription("Get a paginated list of custom property definitions for a specific event session. " +
        "Includes both template-instantiated and ad-hoc definitions. " +
        "Default page size is 20, max is 100. " +
        "Response includes HATEOAS navigation links (first, prev, next, last). " +
        "Send 'Prefer: return=minimal' header to strip links.")]
    [ProducesResponseType(typeof(HalCollectionResource<EventSessionCustomPropertyDefinitionListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<EventSessionCustomPropertyDefinitionListDto>>> GetAll(
        [FromQuery] Guid eventSessionId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetEventSessionCustomPropertyDefinitionListRequest
        {
            EventSessionId = eventSessionId,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            result,
            RouteNames.GetEventSessionCustomPropertyDefinitions,
            additionalRouteValues: new { eventSessionId },
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get event-session-local custom property definition details by ID.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet("{id:guid}", Name = RouteNames.GetEventSessionCustomPropertyDefinitionById)]
    [EndpointSummary("Get EventSessionCustomPropertyDefinition Details")]
    [EndpointDescription("Get full details of an event-session-local custom property definition including its options and provenance information. " +
        "Response includes links to related resources.")]
    [ProducesResponseType(typeof(HalResource<EventSessionCustomPropertyDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<EventSessionCustomPropertyDefinitionDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var definition = await _mediator.Send(new GetEventSessionCustomPropertyDefinitionDetailsRequest { Id = id }, cancellationToken);
        if (definition == null)
            return NotFound();

        var halResource = await _resourceAssembler.ToResource(definition, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Create a new ad-hoc event-session-local custom property definition.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost(Name = RouteNames.CreateEventSessionCustomPropertyDefinition)]
    [EndpointSummary("Create EventSessionCustomPropertyDefinition")]
    [EndpointDescription("Create a new ad-hoc custom property definition for a specific event session. " +
        "For template-based definitions, use the event session creation endpoint with a sessionTemplateId instead.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventSessionCustomPropertyDefinitionDto definition, CancellationToken cancellationToken = default)
    {
        var command = new CreateEventSessionCustomPropertyDefinitionCommand
        {
            DefinitionDto = definition
        };

        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToQuotaProblemOrBadRequest(response);
        }

        return CreatedAtRoute(
            RouteNames.GetEventSessionCustomPropertyDefinitionById,
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Update an event-session-local custom property definition.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("{id:guid}", Name = RouteNames.UpdateEventSessionCustomPropertyDefinition)]
    [EndpointSummary("Update EventSessionCustomPropertyDefinition")]
    [EndpointDescription("Update an existing event-session-local custom property definition and replace its option set. " +
        "Provenance fields (source template information) are read-only and preserved.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(
        Guid id,
        [FromBody] UpdateEventSessionCustomPropertyDefinitionDto updateDto, CancellationToken cancellationToken = default)
    {
        if (id != updateDto.Id)
        {
            return BadRequest(new { error = "EventSessionCustomPropertyDefinition ID mismatch" });
        }

        var command = new UpdateEventSessionCustomPropertyDefinitionCommand
        {
            DefinitionDto = updateDto
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
        {
            return this.ToQuotaProblemOrBadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete an event-session-local custom property definition.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteEventSessionCustomPropertyDefinition)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteEventSessionCustomPropertyDefinitionCommand { Id = id };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Permanently purge a dependency-free event-session-local custom property definition.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [EndpointClassification(EndpointClass.Admin)]
    [HttpDelete("{id:guid}/purge", Name = RouteNames.PurgeEventSessionCustomPropertyDefinition)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<CustomPropertyPurgeResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<CustomPropertyPurgeResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<CustomPropertyPurgeResultDto>>> Purge(
        Guid id,
        [FromBody] PurgeCustomPropertyDefinitionDto purgeDto,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new PurgeEventSessionCustomPropertyDefinitionCommand
        {
            Id = id,
            Reason = purgeDto.Reason
        }, cancellationToken);

        if (result.Success)
        {
            return Ok(result);
        }

        return string.Equals(result.Message, "Session custom-property definition not found.", StringComparison.Ordinal)
            ? NotFound(result)
            : BadRequest(result);
    }

    /// <summary>
    /// Get all custom property values for an event session.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet("values", Name = RouteNames.GetEventSessionCustomPropertyValues)]
    [EndpointSummary("Get EventSessionCustomPropertyValues")]
    [EndpointDescription("Get all custom property values for a specific event session across all definitions.")]
    [ProducesResponseType(typeof(List<EventSessionCustomPropertyValueDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<EventSessionCustomPropertyValueDto>>> GetValues(
        [FromQuery] Guid eventSessionId, CancellationToken cancellationToken = default)
    {
        var values = await _mediator.Send(new GetEventSessionCustomPropertyValuesRequest
        {
            EventSessionId = eventSessionId
        }, cancellationToken);

        return Ok(values);
    }

    /// <summary>
    /// Set a single custom property value for an event session.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("value", Name = RouteNames.SetEventSessionCustomPropertyValue)]
    [EndpointSummary("Set EventSessionCustomPropertyValue")]
    [EndpointDescription("Set or update a single custom property value for an event session. " +
        "Uses upsert semantics based on definition ID, event session ID, and ordinal.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> SetValue(
        [FromBody] SetEventSessionCustomPropertyValueDto valueDto, CancellationToken cancellationToken = default)
    {
        var command = new SetEventSessionCustomPropertyValueCommand
        {
            ValueDto = valueDto
        };

        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToQuotaProblemOrBadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Set multiple values for a multi-value custom property definition on an event session.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("values", Name = RouteNames.SetEventSessionCustomPropertyMultiValues)]
    [EndpointSummary("Set EventSessionCustomPropertyMultiValues")]
    [EndpointDescription("Atomically replace all values for a multi-value custom property definition. " +
        "All existing values for the definition+event session combination are removed and replaced.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> SetMultiValues(
        [FromBody] SetEventSessionCustomPropertyMultiValuesDto multiValuesDto, CancellationToken cancellationToken = default)
    {
        var command = new SetEventSessionCustomPropertyMultiValuesCommand
        {
            DefinitionId = multiValuesDto.DefinitionId,
            EventSessionId = multiValuesDto.EventSessionId,
            Values = multiValuesDto.Values
        };

        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToQuotaProblemOrBadRequest(response);
        }

        return Ok(response);
    }
}
