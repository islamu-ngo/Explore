// ABOUTME: REST API controller for event CRUD operations with advanced filtering, pagination, and HATEOAS support.
// ABOUTME: Supports specification-based queries, soft-delete recovery, and complex event discovery with multiple filter dimensions.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.API.Models;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventAspects;
using Explore.Application.Features.EventAspects.Requests.Commands;
using Explore.Application.Features.EventAspects.Requests.Queries;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using Explore.Application.Specifications.Events;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

/// <summary>
/// Event management API endpoints.
/// All responses include HATEOAS links by default.
/// Send "Prefer: return=minimal" header to strip links.
/// </summary>
[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class EventController : ExploreControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EventController> _logger;
    private readonly IResourceAssembler<EventDto, EventListDto> _resourceAssembler;

    public EventController(
        IMediator mediator,
        ILogger<EventController> logger,
        IResourceAssembler<EventDto, EventListDto> resourceAssembler)
    {
        _mediator = mediator;
        _logger = logger;
        _resourceAssembler = resourceAssembler;
    }

    /// <summary>
    /// Get all events with pagination and optional filtering.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet(Name = RouteNames.GetEvents)]
    [EndpointSummary("Get all Events")]
    [EndpointDescription("Get a paginated, filterable list of all Events (Conference, Webinar, Workshop...). " +
        "Default page size is 20, max is 100. " +
        "Supports filtering by category, tag, format, madhab, location, language, date range, and free-text search. " +
        "Supports module-conditional aspect filters: Islamic (genderMode, quranRecitation, referencePrayer, islamicLanguage) " +
        "and Tech (skillLevel, codingCompetition, hackathon, requiresLaptop, techStack). " +
        "Aspect filters are silently ignored when the corresponding module is not enabled for the tenant. " +
        "Supports sorting by date, title, views, or createdAt. " +
        "Response includes HATEOAS navigation links (first, prev, next, last). " +
        "Send 'Prefer: return=minimal' header to strip links.")]
    [ProducesResponseType(typeof(HalCollectionResource<EventListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<EventListDto>>> GetAll(
        [FromQuery] EventFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetEventListRequest
        {
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            SearchTerm = filter.SearchTerm,
            CategoryId = filter.CategoryId,
            IncludedCategoryIds = filter.IncludedCategoryIds,
            ExcludedCategoryIds = filter.ExcludedCategoryIds,
            CategoryInclusionMode = ParseTagFilterMode(filter.CategoryInclusionMode, TagFilterMode.And),
            CategoryExclusionMode = ParseTagFilterMode(filter.CategoryExclusionMode, TagFilterMode.Or),
            IncludedTagIds = filter.IncludedTagIds,
            ExcludedTagIds = filter.ExcludedTagIds,
            InclusionMode = ParseTagFilterMode(filter.InclusionMode, TagFilterMode.And),
            ExclusionMode = ParseTagFilterMode(filter.ExclusionMode, TagFilterMode.Or),
            FormatIds = filter.FormatIds,
            MadhabIds = filter.MadhabIds,
            LocationIds = filter.LocationIds,
            RegistrationModeIds = filter.RegistrationModeIds,
            LanguageIds = filter.LanguageIds,
            DateFrom = filter.DateFrom,
            DateTo = filter.DateTo,
            EventTypeIds = filter.EventTypeIds,
            AudienceGenderIds = filter.AudienceGenderIds,
            AudienceAgeIds = filter.AudienceAgeIds,
            EventStatusIds = filter.EventStatusIds,
            GenderModeIds = filter.GenderModeIds,
            IncludesQuranRecitation = filter.IncludesQuranRecitation,
            ReferencePrayerIds = filter.ReferencePrayerIds,
            IslamicPrimaryLanguageIds = filter.IslamicPrimaryLanguageIds,
            HasIslamicAspect = filter.HasIslamicAspect,
            SkillLevelId = filter.SkillLevelId,
            IsCodingCompetition = filter.IsCodingCompetition,
            IsHackathon = filter.IsHackathon,
            RequiresLaptop = filter.RequiresLaptop,
            TechStackTag = filter.TechStackTag,
            HasTechAspect = filter.HasTechAspect,
            SortBy = filter.SortBy,
            SortDescending = filter.SortDescending
        }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            result,
            RouteNames.GetEvents,
            additionalRouteValues: null,
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get events for the current user's organizations.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet("my", Name = RouteNames.GetMyEvents)]
    [EndpointSummary("Get My Events")]
    [EndpointDescription("Get a paginated list of events created by the current user's organizations. " +
        "Default page size is 20, max is 100.")]
    [ProducesResponseType(typeof(HalCollectionResource<EventListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<HalCollectionResource<EventListDto>>> GetMyEvents(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId?.ToString();

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _mediator.Send(new GetMyEventsRequest
        {
            UserId = userId,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            result!,
            RouteNames.GetMyEvents,
            additionalRouteValues: null,
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get event details by ID.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("{id:guid}", Name = RouteNames.GetEventById)]
    [EndpointSummary("Get Event Details")]
    [EndpointDescription("Get full details of an event including actor information, sessions, and related resources. " +
        "Response includes links to related resources (sessions, categories, tags).")]
    [ProducesResponseType(typeof(HalResource<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<EventDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await _mediator.Send(new GetEventDetailsRequest { Id = id }, cancellationToken);
        if (@event == null)
            return NotFound();

        var halResource = await _resourceAssembler.ToResource(@event, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Create a new event.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost(Name = RouteNames.CreateEvent)]
    [EndpointSummary("Create Event")]
    [EndpointDescription("Creates a new event. If OrganizationId is provided, the event is created under that organization. " +
        "If GroupId is provided, the event is created under that group. " +
        "If neither is provided, the event is created under the user's personal actor when tenant policy allows user-reported publishing.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventDto @event, CancellationToken cancellationToken = default)
    {
        var command = new CreateEventCommand { EventDto = @event };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtRoute(
            RouteNames.GetEventById,
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Create a new event with sessions in a single transaction.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("with-sessions", Name = RouteNames.CreateEventWithSessions)]
    [EndpointSummary("Create Event with Sessions")]
    [EndpointDescription("Creates a new event along with its sessions in a single transaction. " +
        "At least one session is required. FirstSessionDate and LastSessionDate are computed automatically from the sessions. " +
        "OrganizationId and GroupId are optional and mutually exclusive publisher contexts.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> CreateWithSessions([FromBody] CreateEventWithSessionsDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateEventWithSessionsCommand { EventWithSessionsDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtRoute(
            RouteNames.GetEventById,
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Update an existing event. Supports full update (EventDto) or targeted field updates (e.g., EventStatusDto).
    /// Supply only the DTO(s) to update; null properties are ignored.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("{id:guid}", Name = RouteNames.UpdateEvent)]
    [EndpointSummary("Update Event")]
    [EndpointDescription("Update an existing event. Supports full update via EventDto or targeted updates via specific DTOs (e.g., EventStatusDto). Null DTOs are ignored.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateEventRequestDto dto, CancellationToken cancellationToken = default)
    {
        var command = new UpdateEventCommand
        {
            Id = id,
            EventDto = dto.EventDto,
            EventStatusDto = dto.EventStatusDto
        };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Delete an event.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteEvent)]
    [EndpointSummary("Delete Event")]
    [EndpointDescription("Delete an event. User must be a member of the organization that owns the event.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId?.ToString();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "User ID not found in token" });
        }

        var command = new DeleteEventCommand { Id = id, UserId = userId };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    #region Event Aspects

    /// <summary>
    /// Get the Islamic aspect for an event.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("{id:guid}/aspects/islamic", Name = RouteNames.GetEventIslamicAspect)]
    [EndpointSummary("Get Event Islamic Aspect")]
    [EndpointDescription("Get the Islamic-specific characteristics of an event (Madhab, prayer timing, gender mode). " +
        "Returns 404 if the event doesn't have an Islamic aspect configured.")]
    [ProducesResponseType(typeof(EventIslamicAspectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<EventIslamicAspectDto>> GetIslamicAspect(Guid id, CancellationToken cancellationToken = default)
    {
        var aspect = await _mediator.Send(new GetEventIslamicAspectRequest { EventId = id }, cancellationToken);

        return Ok(aspect);
    }

    /// <summary>
    /// Create or update the Islamic aspect for an event.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("{id:guid}/aspects/islamic", Name = RouteNames.UpsertEventIslamicAspect)]
    [EndpointSummary("Create/Update Event Islamic Aspect")]
    [EndpointDescription("Creates or updates the Islamic-specific characteristics of an event. " +
        "Includes Madhab, prayer-based scheduling, gender segregation mode, and language settings.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpsertIslamicAspect(
        Guid id,
        [FromBody] CreateUpdateIslamicAspectDto aspectDto, CancellationToken cancellationToken = default)
    {
        var command = new UpsertEventIslamicAspectCommand
        {
            EventId = id,
            AspectDto = aspectDto
        };

        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            if (response.Message == "Event not found.")
            {
                return NotFound(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Delete the Islamic aspect from an event.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id:guid}/aspects/islamic", Name = RouteNames.DeleteEventIslamicAspect)]
    [EndpointSummary("Delete Event Islamic Aspect")]
    [EndpointDescription("Removes the Islamic-specific characteristics from an event. " +
        "The event itself is not deleted.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteIslamicAspect(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeleteEventIslamicAspectCommand { EventId = id }, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Get the Tech aspect for an event.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("{id:guid}/aspects/tech", Name = RouteNames.GetEventTechAspect)]
    [EndpointSummary("Get Event Tech Aspect")]
    [EndpointDescription("Get the tech/developer-specific characteristics of an event (skill level, hackathon details, tech stack). " +
        "Returns 404 if the event doesn't have a Tech aspect configured.")]
    [ProducesResponseType(typeof(EventTechAspectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<EventTechAspectDto>> GetTechAspect(Guid id, CancellationToken cancellationToken = default)
    {
        var aspect = await _mediator.Send(new GetEventTechAspectRequest { EventId = id }, cancellationToken);

        return Ok(aspect);
    }

    /// <summary>
    /// Create or update the Tech aspect for an event.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("{id:guid}/aspects/tech", Name = RouteNames.UpsertEventTechAspect)]
    [EndpointSummary("Create/Update Event Tech Aspect")]
    [EndpointDescription("Creates or updates the tech/developer-specific characteristics of an event. " +
        "Includes skill level requirements, hackathon track, tech stack tags, and competition details.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpsertTechAspect(
        Guid id,
        [FromBody] CreateUpdateTechAspectDto aspectDto, CancellationToken cancellationToken = default)
    {
        var command = new UpsertEventTechAspectCommand
        {
            EventId = id,
            AspectDto = aspectDto
        };

        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            if (response.Message == "Event not found.")
            {
                return NotFound(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Delete the Tech aspect from an event.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id:guid}/aspects/tech", Name = RouteNames.DeleteEventTechAspect)]
    [EndpointSummary("Delete Event Tech Aspect")]
    [EndpointDescription("Removes the tech/developer-specific characteristics from an event. " +
        "The event itself is not deleted.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteTechAspect(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeleteEventTechAspectCommand { EventId = id }, cancellationToken);

        return NoContent();
    }

    #endregion

    private static TagFilterMode ParseTagFilterMode(string? value, TagFilterMode defaultValue) =>
        value?.ToLowerInvariant() switch
        {
            "and" => TagFilterMode.And,
            "or" => TagFilterMode.Or,
            _ => defaultValue
        };

    public sealed record UpdateEventRequestDto(UpdateEventDto? EventDto, UpdateEventStatusDto? EventStatusDto);
}
