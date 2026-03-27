// ABOUTME: REST API controller for actor (speaker/performer) CRUD operations with HATEOAS support.
// ABOUTME: Manages speaker profiles, credentials, and associations with events and organizations.

using Asp.Versioning;
using Explore.API.Hateoas;
using Explore.Application.DTOs.Actor;
using Explore.Application.Features.Actors.Requests.Commands;
using Explore.Application.Features.Actors.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

/// <summary>
/// Actor management API endpoints.
/// All responses include HATEOAS links by default.
/// Send "Prefer: return=minimal" header to strip links.
/// </summary>
[ApiVersion("0.1")]
[Route("api/actor")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class ActorController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IResourceAssembler<ActorDto, ActorListDto> _resourceAssembler;

    public ActorController(
        IMediator mediator,
        IResourceAssembler<ActorDto, ActorListDto> resourceAssembler)
    {
        _mediator = mediator;
        _resourceAssembler = resourceAssembler;
    }

    /// <summary>
    /// Get all actors with pagination.
    /// </summary>
    [HttpGet(Name = RouteNames.GetActors)]
    [EndpointSummary("Get all Actors")]
    [EndpointDescription("Retrieve a paginated list of all actors. " +
        "Default page size is 20, max is 100. " +
        "Response includes HATEOAS navigation links. " +
        "Send 'Prefer: return=minimal' header to strip links.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HalCollectionResource<ActorListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<ActorListDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetActorListRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            result,
            RouteNames.GetActors,
            additionalRouteValues: null,
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get actor details by ID.
    /// </summary>
    [HttpGet("{id:guid}", Name = RouteNames.GetActorById)]
    [EndpointSummary("Get Actor Details")]
    [EndpointDescription("Get detailed information about a specific actor. " +
        "Response includes links to related resources (events, organization).")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HalResource<ActorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<ActorDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var actor = await _mediator.Send(new GetActorDetailsRequest { Id = id }, cancellationToken);
        if (actor == null)
            return NotFound();

        var halResource = await _resourceAssembler.ToResource(actor, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Get actor by DID (Decentralized Identifier).
    /// </summary>
    [HttpGet("by-did/{did}", Name = RouteNames.GetActorByDid)]
    [EndpointSummary("Get Actor by DID")]
    [EndpointDescription("Get actor details using their decentralized identifier (DID).")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HalResource<ActorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<ActorDto>>> GetByDid(string did, CancellationToken cancellationToken = default)
    {
        var actor = await _mediator.Send(new GetActorByDidRequest { Did = did }, cancellationToken);
        if (actor == null)
            return NotFound();

        var halResource = await _resourceAssembler.ToResource(actor, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Get actors by tenant.
    /// </summary>
    [HttpGet("by-tenant/{tenantId:guid}", Name = RouteNames.GetActorsByTenant)]
    [EndpointSummary("Get Actors by Tenant")]
    [EndpointDescription("Get all actors belonging to a specific tenant.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HalCollectionResource<ActorListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<ActorListDto>>> GetByTenant(
        Guid tenantId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var actors = await _mediator.Send(new GetActorsByTenantRequest
        {
            TenantId = tenantId,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            actors,
            RouteNames.GetActorsByTenant,
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Create a new actor.
    /// </summary>
    [HttpPost(Name = RouteNames.CreateActor)]
    [EndpointSummary("Create Actor")]
    [EndpointDescription("Create a new actor (user or organization profile).")]
    [Authorize]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateActorDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateActorCommand { ActorDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtRoute(
            RouteNames.GetActorById,
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Update an existing actor. Supports full update (ActorDto) or targeted appearance update (AppearanceDto).
    /// Supply only the DTO(s) to update; null DTOs are ignored.
    /// </summary>
    [HttpPut("{id:guid}", Name = RouteNames.UpdateActor)]
    [EndpointSummary("Update Actor")]
    [EndpointDescription("Update an existing actor. Supports full update via ActorDto or targeted appearance updates via AppearanceDto. Null DTOs are ignored.")]
    [Authorize]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateActorRequestDto dto, CancellationToken cancellationToken = default)
    {
        var command = new UpdateActorCommand
        {
            Id = id,
            ActorDto = dto.ActorDto,
            AppearanceDto = dto.AppearanceDto
        };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Delete an actor.
    /// </summary>
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteActor)]
    [EndpointSummary("Delete Actor")]
    [EndpointDescription("Delete an actor. Admin only.")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteActorCommand { Id = id };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    public sealed record UpdateActorRequestDto(UpdateActorDto? ActorDto, UpdateActorAppearanceDto? AppearanceDto);
}
