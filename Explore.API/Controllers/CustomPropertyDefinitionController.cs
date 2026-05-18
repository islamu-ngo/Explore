// ABOUTME: REST API controller for custom property definition CRUD operations.
// ABOUTME: Allows organizations to define custom fields for events and registrations with type validation.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.Features.CustomPropertyDefinitions.Requests.Commands;
using Explore.Application.Features.CustomPropertyDefinitions.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

/// <summary>
/// CustomPropertyDefinition management API endpoints.
/// All responses include HATEOAS links by default.
/// Send "Prefer: return=minimal" header to strip links.
/// </summary>
[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class CustomPropertyDefinitionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IResourceAssembler<CustomPropertyDefinitionDto, CustomPropertyDefinitionListDto> _resourceAssembler;

    public CustomPropertyDefinitionController(
        IMediator mediator,
        IResourceAssembler<CustomPropertyDefinitionDto, CustomPropertyDefinitionListDto> resourceAssembler)
    {
        _mediator = mediator;
        _resourceAssembler = resourceAssembler;
    }

    /// <summary>
    /// Get shared custom-property definitions with pagination.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet(Name = RouteNames.GetCustomPropertyDefinitions)]
    [EndpointSummary("Get all CustomPropertyDefinitions")]
    [EndpointDescription("Get a paginated list of shared custom-property definitions for one entity scope. Default page size is 20, max is 100. " +
        "Response includes HATEOAS navigation links (first, prev, next, last). " +
        "Send 'Prefer: return=minimal' header to strip links.")]
    [ProducesResponseType(typeof(HalCollectionResource<CustomPropertyDefinitionListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<CustomPropertyDefinitionListDto>>> GetAll(
        [FromQuery] EntityTypeName entityTypeName,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCustomPropertyDefinitionListRequest
        {
            EntityTypeName = entityTypeName,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            result,
            RouteNames.GetCustomPropertyDefinitions,
            additionalRouteValues: new { entityTypeName },
            HttpContext);

        return Ok(halResource);
    }


    /// <summary>
    /// Get shared custom-property definition details by ID.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet("{id:guid}", Name = RouteNames.GetCustomPropertyDefinitionById)]
    [EndpointSummary("Get CustomPropertyDefinition Details")]
    [EndpointDescription("Get full details of a custom property definition including its options. " +
        "Response includes links to related resources (events, members).")]
    [ProducesResponseType(typeof(HalResource<CustomPropertyDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<CustomPropertyDefinitionDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var definition = await _mediator.Send(new GetCustomPropertyDefinitionDetailsRequest { Id = id }, cancellationToken);
        if (definition == null)
            return NotFound();

        var halResource = await _resourceAssembler.ToResource(definition, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Create a new custom property definition.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost(Name = RouteNames.CreateCustomPropertyDefinition)]
    [EndpointSummary("Create CustomPropertyDefinition")]
    [EndpointDescription("Create a new shared custom-property definition for Organization or Group scope.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateCustomPropertyDefinitionDto customPropertyDefinition, CancellationToken cancellationToken = default)
    {
        var command = new CreateCustomPropertyDefinitionCommand
        {
            DefinitionDto = customPropertyDefinition
        };

        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToQuotaProblemOrBadRequest(response);
        }

        return CreatedAtRoute(
            RouteNames.GetCustomPropertyDefinitionById,
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Update a shared custom-property definition.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("{id:guid}", Name = RouteNames.UpdateCustomPropertyDefinition)]
    [EndpointSummary("Update CustomPropertyDefinition")]
    [EndpointDescription("Update an existing shared custom-property definition and replace its option set.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(
        Guid id,
        [FromBody] UpdateCustomPropertyDefinitionDto updateDto, CancellationToken cancellationToken = default)
    {
        if (id != updateDto.Id)
        {
            return BadRequest(new { error = "CustomPropertyDefinition ID mismatch" });
        }

        var command = new UpdateCustomPropertyDefinitionCommand
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
    /// Delete a shared custom-property definition.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteCustomPropertyDefinition)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteCustomPropertyDefinitionCommand { Id = id };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Permanently purge a dependency-free shared custom-property definition.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [EndpointClassification(EndpointClass.Admin)]
    [HttpDelete("{id:guid}/purge", Name = RouteNames.PurgeCustomPropertyDefinition)]
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
        var result = await _mediator.Send(new PurgeCustomPropertyDefinitionCommand
        {
            Id = id,
            Reason = purgeDto.Reason
        }, cancellationToken);

        if (result.Success)
        {
            return Ok(result);
        }

        return string.Equals(result.Message, "Custom-property definition not found.", StringComparison.Ordinal)
            ? NotFound(result)
            : BadRequest(result);
    }
}
