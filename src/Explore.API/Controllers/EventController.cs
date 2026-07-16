// ABOUTME: REST API controller for event CRUD operations with advanced filtering, pagination, and HATEOAS support.
// ABOUTME: Supports specification-based queries, soft-delete recovery, and complex event discovery with multiple filter dimensions.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Filters;
using Explore.API.Hateoas;
using Explore.API.Models;
using Explore.API.Services.Calendar;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventProgram;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Features.EventPrograms.Requests.Queries;
using Explore.Application.Features.Events.Moderation;
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

    private static readonly ApiValidationProblemDescriptor ImportValidationProblem = new(
        "event",
        "Event validation failed",
        "Event import failed.");

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

    private static readonly ApiValidationProblemDescriptor ArchiveValidationProblem = new(
        "event",
        "Event validation failed",
        "Event archive failed.");

    private static readonly ApiValidationProblemDescriptor CancelValidationProblem = new(
        "event",
        "Event validation failed",
        "Event cancel failed.");

    private static readonly ApiValidationProblemDescriptor FilterValidationProblem = new(
        "eventFilter",
        "Event filter validation failed",
        "Event filtering failed.");

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
        "Supports filtering by category, tag, format, madhab, language, date range, and free-text search. " +
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
        if (Request.Query.Keys.Any(static key =>
                string.Equals(key, "locationIds", StringComparison.OrdinalIgnoreCase)))
        {
            return this.ToValidationProblem(
                FilterValidationProblem,
                "The locationIds filter is not available on public event discovery.");
        }

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
            View = ParseTemporalView(filter.View),
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
    /// Get actor-owned events visible to the current principal for management/profile contexts.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet("management/by-actor/{actorId:guid}", Name = RouteNames.GetManagedEventsByActor)]
    [EndpointSummary("Get Managed Events By Actor")]
    [EndpointDescription("Returns actor-owned events that the current principal is authorized to view in management contexts, including drafts and moderated events hidden from public discovery.")]
    [ProducesResponseType(typeof(HalCollectionResource<EventListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<HalCollectionResource<EventListDto>>> GetManagedByActor(
        Guid actorId,
        [FromQuery] PaginationQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetManagedEventsByActorRequest
        {
            ActorId = actorId,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            result,
            RouteNames.GetManagedEventsByActor,
            additionalRouteValues: new { actorId },
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
    [PrivateNoStore]
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
    public async Task<ActionResult<EventProgramSummaryDto>> GetProgramSummary(Guid id, CancellationToken cancellationToken = default)
    {
        var summary = await _mediator.Send(new GetEventProgramSummaryRequest { EventId = id }, cancellationToken);
        if (summary is null)
            return this.ToNotFoundProblem(EventNotFoundProblem);

        return Ok(summary);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [PrivateNoStore]
    [HttpGet("{id:guid}/management-program-summary", Name = RouteNames.GetManagedEventProgramSummary)]
    [EndpointSummary("Get Managed Event Program Summary")]
    [EndpointDescription("Returns draft and published program sections, items, and readiness warnings for authorized event management.")]
    [ProducesResponseType(typeof(EventProgramSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventProgramSummaryDto>> GetManagedProgramSummary(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var summary = await _mediator.Send(new GetManagedEventProgramSummaryRequest { EventId = id }, cancellationToken);
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
    /// Get public event details by slug-code URL token.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("public/{slugCode}", Name = RouteNames.GetEventByPublicCode)]
    [EndpointSummary("Get Event Details By Public Code")]
    [EndpointDescription("Get full public event details from a clean slug-code URL token.")]
    [ProducesResponseType(typeof(HalResource<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<EventDto>>> GetByPublicCode(string slugCode, CancellationToken cancellationToken = default)
    {
        var @event = await _mediator.Send(new GetPublicEventDetailsRequest { SlugCode = slugCode }, cancellationToken);
        if (@event == null)
            return this.ToNotFoundProblem(EventNotFoundProblem);

        var halResource = await _resourceAssembler.ToResource(@event, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Get authorized management event details by ID, including moderated events.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet("{id:guid}/management-detail", Name = RouteNames.GetEventManagementDetails)]
    [EndpointSummary("Get Event Management Details")]
    [EndpointDescription("Returns full event details for authorized management views, including moderated events that public detail routes intentionally hide.")]
    [ProducesResponseType(typeof(HalResource<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HalResource<EventDto>>> GetManagementDetails(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await _mediator.Send(new GetEventManagementDetailsRequest { Id = id }, cancellationToken);
        if (@event == null)
            return this.ToNotFoundProblem(EventNotFoundProblem);

        var halResource = await _resourceAssembler.ToResource(@event, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Get authorized moderation audit history for an event.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet("{id:guid}/moderation/history", Name = RouteNames.GetEventModerationHistory)]
    [EndpointSummary("Get Event Moderation History")]
    [EndpointDescription("Returns safe moderation audit metadata for authorized management views. Event text, slugs, URLs, image identifiers, and storage object paths are never included.")]
    [ProducesResponseType(typeof(IReadOnlyList<EventModerationHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<EventModerationHistoryDto>>> GetModerationHistory(Guid id, CancellationToken cancellationToken = default)
    {
        var history = await _mediator.Send(new GetEventModerationHistoryRequest { Id = id }, cancellationToken);
        return history is null ? this.ToNotFoundProblem(EventNotFoundProblem) : Ok(history);
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
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

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("import", Name = RouteNames.ImportEvent)]
    [EndpointSummary("Import Event")]
    [EndpointDescription("Imports an event from an external source or backfill path with provenance metadata.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Import([FromBody] ImportEventRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new ImportEventCommand
        {
            Request = request
        }, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, ImportValidationProblem);
        }

        return CreatedAtRoute(
            RouteNames.GetEventManagementDetails,
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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
    /// Hide an event after reversible administrative moderation.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("{id:guid}/moderation/light", Name = RouteNames.ModerateEventLight)]
    [EndpointSummary("Light Moderate Event")]
    [EndpointDescription("Moves an event to the Moderated status using the reversible light-moderation authorization action.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> ModerateLight(
        Guid id,
        [FromBody] EventModerationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeLightModerationMetadata(id, request, out var metadata, out var problem))
        {
            return problem!;
        }

        var response = await _mediator.Send(new ModerateEventCommand
        {
            Id = id,
            ReasonCode = metadata!.ReasonCode,
            CorrelationId = metadata.CorrelationId
        }, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, StatusValidationProblem);
        }

        return Ok(response);
    }

    /// <summary>
    /// Irreversibly redact unsafe event content after administrative moderation.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("{id:guid}/moderation/heavy", Name = RouteNames.ModerateEventHeavy)]
    [EndpointSummary("Heavy Redact Event")]
    [EndpointDescription("Redacts event-owned text, detaches event images, queues provider-backed image deletion, and moves the event to the Moderated status using the heavy-moderation authorization action.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> ModerateHeavy(
        Guid id,
        [FromBody] EventModerationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeHeavyModerationMetadata(id, request, out var metadata, out var problem))
        {
            return problem!;
        }

        var response = await _mediator.Send(new HeavyRedactEventCommand
        {
            Id = id,
            ReasonCode = metadata!.ReasonCode,
            CorrelationId = metadata.CorrelationId
        }, cancellationToken);

        if (!response.Success)
        {
            return response.FailureCode == HeavyRedactEventCommand.StorageDeletionPendingFailureCode
                ? this.ToServiceUnavailableProblem(
                    "Event heavy redaction image deletion pending",
                    response.Message ?? "Event heavy redaction completed, but image deletion is pending retry.",
                    response.FailureCode)
                : this.ToCommandValidationProblem(response, StatusValidationProblem);
        }

        return Ok(response);
    }

    /// <summary>
    /// Restore a reversibly moderated event to the published lifecycle state.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("{id:guid}/moderation/unmoderate", Name = RouteNames.UnmoderateEvent)]
    [EndpointSummary("Unmoderate Event")]
    [EndpointDescription("Returns a reversibly light-moderated event to Published. Irreversible heavy redactions cannot be unmoderated.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Unmoderate(
        Guid id,
        [FromBody] EventModerationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeUnmoderationMetadata(id, request, out var metadata, out var problem))
        {
            return problem!;
        }

        var response = await _mediator.Send(new UnmoderateEventCommand
        {
            Id = id,
            ReasonCode = metadata!.ReasonCode,
            CorrelationId = metadata.CorrelationId
        }, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, StatusValidationProblem);
        }

        return Ok(response);
    }

    /// <summary>
    /// Partially update an existing event's editable property groups.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPatch("{id:guid}", Name = RouteNames.UpdateEvent)]
    [EndpointSummary("Update Event")]
    [EndpointDescription("Partially update editable event shell fields. Route ID is authoritative and If-Match must contain the current event concurrency stamp.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(
        Guid id,
        [FromBody] UpdateEventDto updateDto,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseConcurrencyStamp(ifMatch, out var expectedConcurrencyStamp))
        {
            return this.ToValidationProblem(
                UpdateValidationProblem,
                "If-Match header is required and must contain the current event concurrency stamp.");
        }

        var command = new UpdateEventCommand
        {
            EventId = id,
            ExpectedConcurrencyStamp = expectedConcurrencyStamp,
            UpdateEventDto = updateDto
        };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, UpdateValidationProblem);
        }

        return Ok(response);
    }

    /// <summary>
    /// Archive an event. Tolerant lifecycle transition — no public outbox events.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("{id:guid}/archive", Name = RouteNames.ArchiveEvent)]
    [EndpointSummary("Archive Event")]
    [EndpointDescription("Archives an event after concurrency validation. Archived events are removed from public discovery. No public outbox events are emitted.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Archive(Guid id, [FromBody] ArchiveEventRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new ArchiveEventCommand
        {
            Id = id,
            Request = request
        }, cancellationToken);

        if (!response.Success)
        {
            return response.FailureCode == "event_archive_concurrency_conflict"
                ? this.ToCommandConflictProblem(response, "Event archive conflict", "Event archive conflict.")
                : this.ToCommandValidationProblem(response, ArchiveValidationProblem);
        }

        return Ok(response);
    }

    /// <summary>
    /// Cancel an event. Tolerant lifecycle transition — no public outbox events.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("{id:guid}/cancel", Name = RouteNames.CancelEvent)]
    [EndpointSummary("Cancel Event")]
    [EndpointDescription("Cancels an event after concurrency validation. Registrations and public calls to action stop being available. No public outbox events are emitted.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Cancel(Guid id, [FromBody] CancelEventRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new CancelEventCommand
        {
            Id = id,
            Request = request
        }, cancellationToken);

        if (!response.Success)
        {
            return response.FailureCode == "event_cancel_concurrency_conflict"
                ? this.ToCommandConflictProblem(response, "Event cancel conflict", "Event cancel conflict.")
                : this.ToCommandValidationProblem(response, CancelValidationProblem);
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

    private static TagFilterMode ParseTagFilterMode(string? value, TagFilterMode defaultValue) =>
        value?.ToLowerInvariant() switch
        {
            "and" => TagFilterMode.And,
            "or" => TagFilterMode.Or,
            _ => defaultValue
        };

    private static TemporalView? ParseTemporalView(string? value) =>
        Enum.TryParse<TemporalView>(value, ignoreCase: true, out var view) ? view : null;

    private bool TryNormalizeLightModerationMetadata(
        Guid eventId,
        EventModerationRequestDto request,
        out EventModerationReasonMetadata? metadata,
        out ActionResult<BaseCommandResponse<Guid>>? problem)
        => TryNormalizeModerationMetadata(
            eventId,
            request,
            EventModerationReasonCodePolicy.TryNormalizeLight,
            out metadata,
            out problem);

    private bool TryNormalizeHeavyModerationMetadata(
        Guid eventId,
        EventModerationRequestDto request,
        out EventModerationReasonMetadata? metadata,
        out ActionResult<BaseCommandResponse<Guid>>? problem)
        => TryNormalizeModerationMetadata(
            eventId,
            request,
            EventModerationReasonCodePolicy.TryNormalizeHeavy,
            out metadata,
            out problem);

    private bool TryNormalizeUnmoderationMetadata(
        Guid eventId,
        EventModerationRequestDto request,
        out EventModerationReasonMetadata? metadata,
        out ActionResult<BaseCommandResponse<Guid>>? problem)
        => TryNormalizeModerationMetadata(
            eventId,
            request,
            EventModerationReasonCodePolicy.TryNormalizeUnmoderation,
            out metadata,
            out problem);

    private bool TryNormalizeModerationMetadata(
        Guid eventId,
        EventModerationRequestDto request,
        ModerationMetadataNormalizer normalize,
        out EventModerationReasonMetadata? metadata,
        out ActionResult<BaseCommandResponse<Guid>>? problem)
    {
        if (normalize(request.ReasonCode, request.CorrelationId, out var normalized, out var failureCode, out var error))
        {
            metadata = normalized;
            problem = null;
            return true;
        }

        metadata = null;
        problem = this.ToCommandValidationProblem(new BaseCommandResponse<Guid>
        {
            Id = eventId,
            Success = false,
            Message = error,
            Errors = [error ?? "Moderation metadata is invalid."],
            FailureCode = failureCode
        }, StatusValidationProblem);
        return false;
    }

    private delegate bool ModerationMetadataNormalizer(
        string? reasonCode,
        string? correlationId,
        out EventModerationReasonMetadata metadata,
        out string? failureCode,
        out string? error);

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
