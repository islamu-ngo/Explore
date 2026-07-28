// ABOUTME: REST API controller for actor (speaker/performer) CRUD operations with HATEOAS support.
// ABOUTME: Manages speaker profiles, credentials, and associations with events and organizations.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
using Explore.API.Models;
using Explore.Application.DTOs.Actor;
using Explore.Application.Features.Actors.Requests.Commands;
using Explore.Application.Features.Actors.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using Explore.Domain.Enums;
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
[EndpointClassification(EndpointClass.Public)]
public class ActorController : ControllerBase
{
    private static readonly ApiValidationProblemDescriptor CreateValidationProblem = new(
        "actor",
        "Actor validation failed",
        "Actor creation failed.");

    private static readonly ApiValidationProblemDescriptor UpdateValidationProblem = new(
        "actor",
        "Actor validation failed",
        "Actor update failed.");

    private static readonly ApiNotFoundProblemDescriptor ActorNotFoundProblem = new(
        "Actor not found",
        "Actor not found.");

    private static readonly ApiValidationProblemDescriptor GlobalModerationValidationProblem = new(
        "globalActorModeration",
        "Global actor moderation validation failed",
        "Global actor moderation failed.");

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
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<ActorListDto>>> GetAll(
        [FromQuery] PaginationQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetActorListRequest
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<ActorDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var actor = await _mediator.Send(new GetActorDetailsRequest { Id = id }, cancellationToken);
        if (actor == null)
        {
            return this.ToNotFoundProblem(ActorNotFoundProblem);
        }

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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<ActorDto>>> GetByDid(string did, CancellationToken cancellationToken = default)
    {
        var actor = await _mediator.Send(new GetActorByDidRequest { Did = did }, cancellationToken);
        if (actor == null)
        {
            return this.ToNotFoundProblem(ActorNotFoundProblem);
        }

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
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<ActorListDto>>> GetByTenant(
        Guid tenantId,
        [FromQuery] PaginationQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var actors = await _mediator.Send(new GetActorsByTenantRequest
        {
            TenantId = tenantId,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
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
    [EndpointClassification(EndpointClass.Authenticated)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateActorDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateActorCommand { ActorDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, CreateValidationProblem);
        }

        return CreatedAtRoute(
            RouteNames.GetActorById,
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Partially update an existing actor using nullable logical groups.
    /// </summary>
    [HttpPatch("{id:guid}", Name = RouteNames.UpdateActor)]
    [EndpointSummary("Update Actor")]
    [EndpointDescription("Partially update an existing actor. Route ID is authoritative and If-Match must contain the current actor concurrency stamp.")]
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(
        Guid id,
        [FromBody] UpdateActorDto dto,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseConcurrencyStamp(ifMatch, out var expectedConcurrencyStamp))
        {
            return this.ToValidationProblem(
                UpdateValidationProblem,
                "If-Match header is required and must contain the current actor concurrency stamp.");
        }

        var command = new UpdateActorCommand
        {
            ActorId = id,
            ExpectedConcurrencyStamp = expectedConcurrencyStamp,
            UpdateActorDto = dto
        };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return string.Equals(response.Message, "Actor not found.", StringComparison.Ordinal)
                ? this.ToNotFoundProblem(ActorNotFoundProblem)
                : this.ToCommandValidationProblem(response, UpdateValidationProblem);
        }

        return Ok(response);
    }

    /// <summary>
    /// Suspend an actor globally.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("{actorId:guid}/moderation/suspend", Name = RouteNames.SuspendActor)]
    [EndpointSummary("Suspend Actor Globally")]
    [EndpointDescription("Suspends an actor globally using the server-selected moderation action.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> SuspendActor(
        Guid actorId,
        [FromBody] GlobalModerationRequestDto request,
        CancellationToken cancellationToken = default) =>
        ModerateActor(actorId, GlobalModerationAction.Suspend, request, cancellationToken);

    /// <summary>
    /// Reinstate an actor globally.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("{actorId:guid}/moderation/reinstate", Name = RouteNames.ReinstateActor)]
    [EndpointSummary("Reinstate Actor Globally")]
    [EndpointDescription("Reinstates an actor globally using the server-selected moderation action.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> ReinstateActor(
        Guid actorId,
        [FromBody] GlobalModerationRequestDto request,
        CancellationToken cancellationToken = default) =>
        ModerateActor(actorId, GlobalModerationAction.Reinstate, request, cancellationToken);

    /// <summary>
    /// Suspend an AT Protocol identity globally.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("atproto-identities/{identityId:guid}/moderation/suspend", Name = RouteNames.SuspendAtprotoIdentity)]
    [EndpointSummary("Suspend AT Protocol Identity Globally")]
    [EndpointDescription("Suspends an AT Protocol identity globally using the server-selected moderation action.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> SuspendAtprotoIdentity(
        Guid identityId,
        [FromBody] GlobalModerationRequestDto request,
        CancellationToken cancellationToken = default) =>
        ModerateAtprotoIdentity(identityId, GlobalModerationAction.Suspend, request, cancellationToken);

    /// <summary>
    /// Reinstate an AT Protocol identity globally.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("atproto-identities/{identityId:guid}/moderation/reinstate", Name = RouteNames.ReinstateAtprotoIdentity)]
    [EndpointSummary("Reinstate AT Protocol Identity Globally")]
    [EndpointDescription("Reinstates an AT Protocol identity globally using the server-selected moderation action.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> ReinstateAtprotoIdentity(
        Guid identityId,
        [FromBody] GlobalModerationRequestDto request,
        CancellationToken cancellationToken = default) =>
        ModerateAtprotoIdentity(identityId, GlobalModerationAction.Reinstate, request, cancellationToken);

    /// <summary>
    /// Delete an actor.
    /// </summary>
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteActor)]
    [EndpointSummary("Delete Actor")]
    [EndpointDescription("Delete an actor. Admin only.")]
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteActorCommand { Id = id };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    private async Task<ActionResult<BaseCommandResponse<Guid>>> ModerateActor(
        Guid actorId,
        GlobalModerationAction action,
        GlobalModerationRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ModerateActorCommand
        {
            ActorId = actorId,
            Moderation = new GlobalModerationRequest
            {
                Action = action,
                ReasonCode = request.ReasonCode
            }
        }, cancellationToken);

        return MapModerationResponse(response);
    }

    private async Task<ActionResult<BaseCommandResponse<Guid>>> ModerateAtprotoIdentity(
        Guid identityId,
        GlobalModerationAction action,
        GlobalModerationRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ModerateAtprotoIdentityCommand
        {
            AtprotoIdentityId = identityId,
            Moderation = new GlobalModerationRequest
            {
                Action = action,
                ReasonCode = request.ReasonCode
            }
        }, cancellationToken);

        return MapModerationResponse(response);
    }

    private ActionResult<BaseCommandResponse<Guid>> MapModerationResponse(BaseCommandResponse<Guid> response)
    {
        if (response.Success)
        {
            return Ok(response);
        }

        if (string.Equals(
                response.Message,
                "Authenticated instance administrator context is required.",
                StringComparison.Ordinal))
        {
            return this.ToAuthenticationRequiredProblem(detail: response.Message!);
        }

        return response.Message?.Contains("administrators", StringComparison.OrdinalIgnoreCase) == true
            ? this.ToForbiddenProblem(detail: response.Message)
            : this.ToCommandValidationProblem(response, GlobalModerationValidationProblem);
    }

    private static bool TryParseConcurrencyStamp(string? ifMatch, out Guid concurrencyStamp)
    {
        concurrencyStamp = default;
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return false;
        }

        var value = ifMatch.Trim();
        if (value.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        value = value.Trim('"');
        return Guid.TryParse(value, out concurrencyStamp) && concurrencyStamp != Guid.Empty;
    }
}
