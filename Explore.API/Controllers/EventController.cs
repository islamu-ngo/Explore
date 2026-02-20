using Asp.Versioning;
using Explore.API.Hateoas;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventAspects;
using Explore.Application.Features.EventAspects.Requests.Commands;
using Explore.Application.Features.EventAspects.Requests.Queries;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
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
public class EventController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<EventController> _logger;
    private readonly IResourceAssembler<EventDto, EventListDto> _resourceAssembler;

    public EventController(
        IMediator mediator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<EventController> logger,
        IResourceAssembler<EventDto, EventListDto> resourceAssembler)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _resourceAssembler = resourceAssembler;
    }

    /// <summary>
    /// Get all events with pagination and optional filtering.
    /// </summary>
    [HttpGet(Name = RouteNames.GetEvents)]
    [EndpointSummary("Get all Events")]
    [EndpointDescription("Get a paginated, filterable list of all Events (Conference, Webinar, Workshop...). " +
        "Default page size is 20, max is 100. " +
        "Supports filtering by category, tag, format, madhab, location, language, date range, and free-text search. " +
        "Supports module-conditional aspect filters: Islamic (genderMode, quranRecitation, referencePrayer, islamicLanguage) " +
        "and Tech (skillLevel, codingCompetition, hackathon, requiresLaptop, techStack). " +
        "Aspect filters are silently ignored when the corresponding module is not enabled for the tenant. " +
        "Supports JSONB metadata filtering via metadataJsonContains and metadataJsonKeyExists. " +
        "Supports sorting by date, title, views, or createdAt. " +
        "Response includes HATEOAS navigation links (first, prev, next, last). " +
        "Send 'Prefer: return=minimal' header to strip links.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HalCollectionResource<EventListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<EventListDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        // Core event filters
        [FromQuery] string? searchTerm = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? tagId = null,
        [FromQuery] int? formatId = null,
        [FromQuery] int? madhabId = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] int? registrationModeId = null,
        [FromQuery] int? languageId = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        [FromQuery] int? eventTypeId = null,
        [FromQuery] int? audienceGenderId = null,
        [FromQuery] int? audienceAgeId = null,
        [FromQuery] int? eventStatusId = null,
        // Islamic aspect filters (module-conditional — silently ignored when module is disabled)
        [FromQuery] int? genderModeId = null,
        [FromQuery] bool? includesQuranRecitation = null,
        [FromQuery] int? referencePrayerId = null,
        [FromQuery] int? islamicPrimaryLanguageId = null,
        [FromQuery] bool? hasIslamicAspect = null,
        // Tech aspect filters (module-conditional — silently ignored when module is disabled)
        [FromQuery] int? skillLevelId = null,
        [FromQuery] bool? isCodingCompetition = null,
        [FromQuery] bool? isHackathon = null,
        [FromQuery] bool? requiresLaptop = null,
        [FromQuery] string? techStackTag = null,
        [FromQuery] bool? hasTechAspect = null,
        // JSONB metadata filters
        [FromQuery] string? metadataJsonContains = null,
        [FromQuery] string? metadataJsonKeyExists = null,
        // Sorting
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetEventListRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            // Core event filters
            SearchTerm = searchTerm,
            CategoryId = categoryId,
            TagId = tagId,
            FormatId = formatId,
            MadhabId = madhabId,
            LocationId = locationId,
            RegistrationModeId = registrationModeId,
            LanguageId = languageId,
            DateFrom = dateFrom,
            DateTo = dateTo,
            EventTypeId = eventTypeId,
            AudienceGenderId = audienceGenderId,
            AudienceAgeId = audienceAgeId,
            EventStatusId = eventStatusId,
            // Islamic aspect filters
            GenderModeId = genderModeId,
            IncludesQuranRecitation = includesQuranRecitation,
            ReferencePrayerId = referencePrayerId,
            IslamicPrimaryLanguageId = islamicPrimaryLanguageId,
            HasIslamicAspect = hasIslamicAspect,
            // Tech aspect filters
            SkillLevelId = skillLevelId,
            IsCodingCompetition = isCodingCompetition,
            IsHackathon = isHackathon,
            RequiresLaptop = requiresLaptop,
            TechStackTag = techStackTag,
            HasTechAspect = hasTechAspect,
            // JSONB metadata filters
            MetadataJsonContains = metadataJsonContains,
            MetadataJsonKeyExists = metadataJsonKeyExists,
            // Sorting
            SortBy = sortBy,
            SortDescending = sortDescending
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
    [HttpGet("my", Name = RouteNames.GetMyEvents)]
    [EndpointSummary("Get My Events")]
    [EndpointDescription("Get a paginated list of events created by the current user's organizations. " +
        "Default page size is 20, max is 100.")]
    [Authorize]
    [ProducesResponseType(typeof(HalCollectionResource<EventListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<HalCollectionResource<EventListDto>>> GetMyEvents(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("=== GetMyEvents API Request ===");
            _logger.LogInformation("User authenticated: {IsAuthenticated}", User?.Identity?.IsAuthenticated);
            _logger.LogInformation("User name: {Name}", User?.Identity?.Name);

            var userId = GetCurrentUserId();
            _logger.LogInformation("Extracted userId: {UserId}", userId);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in token");
                return Unauthorized(new { error = "User ID not found in token" });
            }

            _logger.LogInformation("Sending GetMyEventsRequest for userId: {UserId}", userId);
            var result = await _mediator.Send(new GetMyEventsRequest
            {
                UserId = userId,
                PageNumber = pageNumber,
                PageSize = pageSize
            }, cancellationToken);

            _logger.LogInformation("Retrieved {Count} events", result?.Items?.Count ?? 0);

            var halResource = await _resourceAssembler.ToCollectionResource(
                result!,
                RouteNames.GetMyEvents,
                additionalRouteValues: null,
                HttpContext);

            return Ok(halResource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetMyEvents");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Get event details by ID.
    /// </summary>
    [HttpGet("{id:guid}", Name = RouteNames.GetEventById)]
    [EndpointSummary("Get Event Details")]
    [EndpointDescription("Get full details of an event including actor information, sessions, and related resources. " +
        "Response includes links to related resources (sessions, categories, tags).")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HalResource<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<EventDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await _mediator.Send(new GetEventDetailsRequest { Id = id }, cancellationToken);

        if (@event is null)
        {
            return NotFound();
        }

        var halResource = await _resourceAssembler.ToResource(@event, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Create a new event.
    /// </summary>
    [HttpPost(Name = RouteNames.CreateEvent)]
    [EndpointSummary("Create Event")]
    [EndpointDescription("Creates a new event. If OrganizationId is provided, the event is created under that organization. " +
        "If GroupId is provided, the event is created under that group. " +
        "If neither is provided, the event is created under the user's personal actor when tenant policy allows user-reported publishing.")]
    [Authorize]
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
    [HttpPost("with-sessions")]
    [EndpointSummary("Create Event with Sessions")]
    [EndpointDescription("Creates a new event along with its sessions in a single transaction. " +
        "At least one session is required. FirstSessionDate and LastSessionDate are computed automatically from the sessions. " +
        "OrganizationId and GroupId are optional and mutually exclusive publisher contexts.")]
    [Authorize]
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
    /// Update an existing event.
    /// </summary>
    [HttpPut("{id:guid}", Name = RouteNames.UpdateEvent)]
    [EndpointSummary("Update Event")]
    [EndpointDescription("Update an existing event. User must be a member of the organization that owns the event.")]
    [Authorize]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateEventDto @event, CancellationToken cancellationToken = default)
    {
        if (id != @event.Id)
        {
            return BadRequest(new { error = "Event ID mismatch" });
        }

        var command = new UpdateEventCommand { EventDto = @event };
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
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteEvent)]
    [EndpointSummary("Delete Event")]
    [EndpointDescription("Delete an event. User must be a member of the organization that owns the event.")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User ID not found in token" });
            }

            var command = new DeleteEventCommand { Id = id, UserId = userId };
            var result = await _mediator.Send(command, cancellationToken);

            if (!result)
            {
                return NotFound(new { error = "Event not found or you don't have permission to delete it" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event {EventId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Extracts the current user ID from claims using the standard fallback pattern.
    /// </summary>
    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst(
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;
    }

    #region Event Aspects

    /// <summary>
    /// Get the Islamic aspect for an event.
    /// </summary>
    [HttpGet("{id:guid}/aspects/islamic", Name = RouteNames.GetEventIslamicAspect)]
    [EndpointSummary("Get Event Islamic Aspect")]
    [EndpointDescription("Get the Islamic-specific characteristics of an event (Madhab, prayer timing, gender mode). " +
        "Returns 404 if the event doesn't have an Islamic aspect configured.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventIslamicAspectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<EventIslamicAspectDto>> GetIslamicAspect(Guid id, CancellationToken cancellationToken = default)
    {
        var aspect = await _mediator.Send(new GetEventIslamicAspectRequest { EventId = id }, cancellationToken);

        if (aspect == null)
        {
            return NotFound(new { error = "Islamic aspect not found for this event." });
        }

        return Ok(aspect);
    }

    /// <summary>
    /// Create or update the Islamic aspect for an event.
    /// </summary>
    [HttpPut("{id:guid}/aspects/islamic", Name = RouteNames.UpsertEventIslamicAspect)]
    [EndpointSummary("Create/Update Event Islamic Aspect")]
    [EndpointDescription("Creates or updates the Islamic-specific characteristics of an event. " +
        "Includes Madhab, prayer-based scheduling, gender segregation mode, and language settings.")]
    [Authorize]
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
    [HttpDelete("{id:guid}/aspects/islamic", Name = RouteNames.DeleteEventIslamicAspect)]
    [EndpointSummary("Delete Event Islamic Aspect")]
    [EndpointDescription("Removes the Islamic-specific characteristics from an event. " +
        "The event itself is not deleted.")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteIslamicAspect(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new DeleteEventIslamicAspectCommand { EventId = id }, cancellationToken);

        if (!result)
        {
            return NotFound(new { error = "Islamic aspect not found for this event." });
        }

        return NoContent();
    }

    /// <summary>
    /// Get the Tech aspect for an event.
    /// </summary>
    [HttpGet("{id:guid}/aspects/tech", Name = RouteNames.GetEventTechAspect)]
    [EndpointSummary("Get Event Tech Aspect")]
    [EndpointDescription("Get the tech/developer-specific characteristics of an event (skill level, hackathon details, tech stack). " +
        "Returns 404 if the event doesn't have a Tech aspect configured.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventTechAspectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<EventTechAspectDto>> GetTechAspect(Guid id, CancellationToken cancellationToken = default)
    {
        var aspect = await _mediator.Send(new GetEventTechAspectRequest { EventId = id }, cancellationToken);

        if (aspect == null)
        {
            return NotFound(new { error = "Tech aspect not found for this event." });
        }

        return Ok(aspect);
    }

    /// <summary>
    /// Create or update the Tech aspect for an event.
    /// </summary>
    [HttpPut("{id:guid}/aspects/tech", Name = RouteNames.UpsertEventTechAspect)]
    [EndpointSummary("Create/Update Event Tech Aspect")]
    [EndpointDescription("Creates or updates the tech/developer-specific characteristics of an event. " +
        "Includes skill level requirements, hackathon track, tech stack tags, and competition details.")]
    [Authorize]
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
    [HttpDelete("{id:guid}/aspects/tech", Name = RouteNames.DeleteEventTechAspect)]
    [EndpointSummary("Delete Event Tech Aspect")]
    [EndpointDescription("Removes the tech/developer-specific characteristics from an event. " +
        "The event itself is not deleted.")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteTechAspect(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new DeleteEventTechAspectCommand { EventId = id }, cancellationToken);

        if (!result)
        {
            return NotFound(new { error = "Tech aspect not found for this event." });
        }

        return NoContent();
    }

    #endregion
}
