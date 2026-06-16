// ABOUTME: REST API controller for event CRUD operations with advanced filtering, pagination, and HATEOAS support.
// ABOUTME: Supports specification-based queries, soft-delete recovery, and complex event discovery with multiple filter dimensions.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
using Explore.API.Models;
using Explore.API.Services.Calendar;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventAspects;
using Explore.Application.DTOs.EventProgram;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Features.EventAspects.Requests.Commands;
using Explore.Application.Features.EventAspects.Requests.Queries;
using Explore.Application.Features.EventPrograms.Requests.Queries;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Features.EventSessions.Requests.Queries;
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
    private static readonly ApiValidationProblemDescriptor CreateValidationProblem = new(
        "event",
        "Event validation failed",
        "Event creation failed.");

    private static readonly ApiValidationProblemDescriptor PublishValidationProblem = new(
        "event",
        "Event validation failed",
        "Event publishing failed.");

    private static readonly ApiValidationProblemDescriptor UpdateValidationProblem = new(
        "event",
        "Event validation failed",
        "Event update failed.");

    private static readonly ApiValidationProblemDescriptor StatusValidationProblem = new(
        "event",
        "Event validation failed",
        "Event status update failed.");

    private static readonly ApiValidationProblemDescriptor IslamicAspectValidationProblem = new(
        "eventIslamicAspect",
        "Event Islamic aspect validation failed",
        "Event Islamic aspect update failed.");

    private static readonly ApiValidationProblemDescriptor TechAspectValidationProblem = new(
        "eventTechAspect",
        "Event tech aspect validation failed",
        "Event tech aspect update failed.");

    private static readonly ApiNotFoundProblemDescriptor EventNotFoundProblem = new(
        "Event not found",
        "Event not found.");

    private readonly IMediator _mediator;
    private readonly ILogger<EventController> _logger;
    private readonly IResourceAssembler<EventDto, EventListDto> _resourceAssembler;
    private readonly IEventCalendarFileBuilder _calendarFileBuilder;
    private readonly Explore.Application.Contracts.Infrastructure.IPublicUrlBuilder _publicUrlBuilder;

    public EventController(
        IMediator mediator,
        ILogger<EventController> logger,
        IResourceAssembler<EventDto, EventListDto> resourceAssembler,
        IEventCalendarFileBuilder calendarFileBuilder,
        Explore.Application.Contracts.Infrastructure.IPublicUrlBuilder publicUrlBuilder)
    {
        _mediator = mediator;
        _logger = logger;
        _resourceAssembler = resourceAssembler;
        _calendarFileBuilder = calendarFileBuilder;
        _publicUrlBuilder = publicUrlBuilder;
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
        "Supports custom-property projection filters and text search gated behind the tenant feature flag " +
        "'custom_properties.projection_discovery_enabled' — silently ignored when disabled. " +
        "Supports sorting by date, title, views, or createdAt. " +
        "Response includes HATEOAS navigation links (first, prev, next, last). " +
        "Send 'Prefer: return=minimal' header to strip links.")]
    [ProducesResponseType(typeof(HalCollectionResource<EventListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
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
            ActorId = filter.ActorId,
            OrganizationId = filter.OrganizationId,
            GroupId = filter.GroupId,
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
            SortDescending = filter.SortDescending,
            CustomPropertyFilters = filter.CustomPropertyFilters,
            CustomPropertySearchTerm = filter.CustomPropertySearchTerm
        }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            result,
            RouteNames.GetEvents,
            additionalRouteValues: new
            {
                filter.ActorId,
                filter.OrganizationId,
                filter.GroupId
            },
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
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<HalCollectionResource<EventListDto>>> GetMyEvents(
        [FromQuery] PaginationQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId?.ToString();

        if (string.IsNullOrEmpty(userId))
        {
            return this.ToAuthenticationRequiredProblem();
        }

        var result = await _mediator.Send(new GetMyEventsRequest
        {
            UserId = userId,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            result!,
            RouteNames.GetMyEvents,
            additionalRouteValues: null,
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get the current user's event creation context.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet("creation-context", Name = RouteNames.GetEventCreationContext)]
    [EndpointSummary("Get Event Creation Context")]
    [EndpointDescription("Returns tenant event publishing policy and the personal, organization, and group publisher options available to the current user.")]
    [ProducesResponseType(typeof(EventCreationContextDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EventCreationContextDto>> GetCreationContext(CancellationToken cancellationToken = default)
    {
        var context = await _mediator.Send(new GetEventCreationContextRequest(), cancellationToken);
        return Ok(context);
    }

    /// <summary>
    /// Get server-owned defaults and selector options for adding a program item to an event.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet("{id:guid}/session-create-context", Name = RouteNames.GetEventSessionCreateContext)]
    [EndpointSummary("Get Event Session Create Context")]
    [EndpointDescription("Returns inherited event defaults, location options, room options, and program sections for the dedicated program item composer.")]
    [ProducesResponseType(typeof(EventSessionCreateContextDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventSessionCreateContextDto>> GetSessionCreateContext(Guid id, CancellationToken cancellationToken = default)
    {
        var context = await _mediator.Send(new GetEventSessionCreateContextRequest { EventId = id }, cancellationToken);
        if (context is null)
            return this.ToNotFoundProblem(EventNotFoundProblem);

        return Ok(context);
    }

    /// <summary>
    /// Get the server-backed program summary for an event.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("{id:guid}/program-summary", Name = RouteNames.GetEventProgramSummary)]
    [EndpointSummary("Get Event Program Summary")]
    [EndpointDescription("Returns program sections, local-day groupings, program items, and server-generated readiness warnings for the event program.")]
    [ProducesResponseType(typeof(EventProgramSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<EventProgramSummaryDto>> GetProgramSummary(Guid id, CancellationToken cancellationToken = default)
    {
        var summary = await _mediator.Send(new GetEventProgramSummaryRequest { EventId = id }, cancellationToken);
        if (summary is null)
            return this.ToNotFoundProblem(EventNotFoundProblem);

        return Ok(summary);
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<EventDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await _mediator.Send(new GetEventDetailsRequest { Id = id }, cancellationToken);
        if (@event == null)
            return this.ToNotFoundProblem(EventNotFoundProblem);

        var halResource = await _resourceAssembler.ToResource(@event, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Download an event as an iCalendar (.ics) file.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("{id:guid}/calendar", Name = RouteNames.GetEventCalendar)]
    [EndpointSummary("Download Event Calendar")]
    [EndpointDescription("Downloads a published public event as an RFC 5545 iCalendar file.")]
    [Produces("text/calendar")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<IActionResult> GetCalendar(Guid id, CancellationToken cancellationToken = default)
    {
        var export = await _mediator.Send(new GetEventCalendarExportRequest(id), cancellationToken);
        if (export is null)
        {
            return this.ToNotFoundProblem(EventNotFoundProblem);
        }

        Uri canonicalUrl = new(_publicUrlBuilder.GetEventUrl(export.EventId));
        string calendarContent = _calendarFileBuilder.Build(export, canonicalUrl);
        string fileName = $"{SanitizeCalendarFileName(export.Slug ?? export.Title)}.ics";

        return File(
            System.Text.Encoding.UTF8.GetBytes(calendarContent),
            "text/calendar; charset=utf-8",
            fileName);
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
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventDraftRequestDto draft, CancellationToken cancellationToken = default)
    {
        var command = new CreateEventCommand { Request = draft.ToCreateEventRequest() };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, CreateValidationProblem);
        }

        return CreatedAtRoute(
            RouteNames.GetEventById,
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Review whether an event is ready to publish.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet("{id:guid}/publish-readiness", Name = RouteNames.GetEventPublishReadiness)]
    [EndpointSummary("Get Event Publish Readiness")]
    [EndpointDescription("Returns machine-readable readiness errors that block publishing an event.")]
    [ProducesResponseType(typeof(EventPublishReadinessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventPublishReadinessDto>> GetPublishReadiness(Guid id, CancellationToken cancellationToken = default)
    {
        var readiness = await _mediator.Send(new GetEventPublishReadinessRequest { Id = id }, cancellationToken);
        return readiness is null ? this.ToNotFoundProblem(EventNotFoundProblem) : Ok(readiness);
    }

    /// <summary>
    /// Publish a draft event.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("{id:guid}/publish", Name = RouteNames.PublishEvent)]
    [EndpointSummary("Publish Event")]
    [EndpointDescription("Publishes a draft event after readiness and concurrency validation. Side effects are written to the transactional outbox.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Publish(Guid id, [FromBody] PublishEventRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new PublishEventCommand
        {
            Id = id,
            Request = request
        }, cancellationToken);

        if (!response.Success)
        {
            return response.FailureCode == "event_publish_concurrency_conflict"
                ? this.ToCommandConflictProblem(response, "Event publish conflict", "Event publishing conflict.")
                : this.ToCommandValidationProblem(response, PublishValidationProblem);
        }

        return Ok(response);
    }

    /// <summary>
    /// Update an existing event draft's scalar shell fields.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("{id:guid}", Name = RouteNames.UpdateEvent)]
    [EndpointSummary("Update Event Draft")]
    [EndpointDescription("Update scalar event draft fields. Lifecycle status and session-derived program projection fields are server-owned and are not accepted by this contract.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateEventDraftRequestDto draft, CancellationToken cancellationToken = default)
    {
        var command = new UpdateEventDraftCommand
        {
            Id = id,
            Draft = draft
        };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, UpdateValidationProblem);
        }

        return Ok(response);
    }

    /// <summary>
    /// Update an event lifecycle status through an explicit status contract.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("{id:guid}/status", Name = RouteNames.UpdateEventStatus)]
    [EndpointSummary("Update Event Status")]
    [EndpointDescription("Update an event lifecycle status through a dedicated contract. Draft metadata updates must use the scalar draft update endpoint.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateStatus(Guid id, [FromBody] UpdateEventStatusDto dto, CancellationToken cancellationToken = default)
    {
        var command = new UpdateEventCommand
        {
            Id = id,
            EventStatusDto = dto
        };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, StatusValidationProblem);
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId?.ToString();

        if (string.IsNullOrEmpty(userId))
        {
            return this.ToAuthenticationRequiredProblem();
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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
                return this.ToNotFoundProblem(EventNotFoundProblem);
            }

            return this.ToCommandValidationProblem(response, IslamicAspectValidationProblem);
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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
                return this.ToNotFoundProblem(EventNotFoundProblem);
            }

            return this.ToCommandValidationProblem(response, TechAspectValidationProblem);
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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

    private static string SanitizeCalendarFileName(string value)
    {
        string sanitized = string.Concat(value
            .Trim()
            .Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '-'));

        sanitized = string.Join(
            '-',
            sanitized.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return string.IsNullOrWhiteSpace(sanitized)
            ? "event"
            : sanitized.ToLowerInvariant();
    }

    public sealed record UpdateEventRequestDto(UpdateEventDto? EventDto, UpdateEventStatusDto? EventStatusDto);
}
