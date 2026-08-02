// ABOUTME: MCP read tools for public event discovery and details.
// ABOUTME: Delegates through MediatR queries so MCP does not bypass API/Application visibility rules.

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Explore.API.Hateoas;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventAgendaItem;
using Explore.Application.DTOs.EventCustomProperty;
using Explore.Application.DTOs.EventDay;
using Explore.Application.DTOs.EventProgram;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.DTOs.EventRoleAssignment;
using Explore.Application.DTOs.EventSession;
using Explore.Application.DTOs.EventSessionGroup;
using Explore.Application.DTOs.EventSessionTemplate;
using Explore.Application.DTOs.EventTemplate;
using Explore.Application.DTOs.Location;
using Explore.Application.Exceptions;
using Explore.Application.Features.AiAssistant.Disclosure;
using Explore.Application.Features.EventAgendaItems.Requests.Queries;
using Explore.Application.Features.EventCustomProperties.Requests.Queries;
using Explore.Application.Features.EventDays.Requests.Queries;
using Explore.Application.Features.EventPrograms.Requests.Queries;
using Explore.Application.Features.RegistrationOrders.Requests.Queries;
using Explore.Application.Features.EventRoleAssignments.Requests.Queries;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Features.EventSessionGroups.Requests.Queries;
using Explore.Application.Features.EventSessions.Requests.Queries;
using Explore.Application.Features.EventSessionTemplates.Requests.Queries;
using Explore.Application.Features.EventSessionTemplateSync.Queries.GetEventSessionTemplateDiff;
using Explore.Application.Features.EventSessionTemplateSync.Queries.GetEventSessionTemplateSyncHistory;
using Explore.Application.Features.EventTemplates.Requests.Queries;
using Explore.Application.Features.EventTemplateSync.Queries.GetEventTemplateDiff;
using Explore.Application.Features.EventTemplateSync.Queries.GetEventTemplateSyncHistory;
using Explore.Application.Hateoas;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;
using EventSessionTemplateDiffDto = Explore.Application.DTOs.EventSessionTemplateSync.TemplateDiffDto;
using EventSessionTemplateSyncHistoryItemDto = Explore.Application.DTOs.EventSessionTemplateSync.EventSessionTemplateSyncHistoryItemDto;
using EventTemplateDiffDto = Explore.Application.DTOs.EventTemplateSync.TemplateDiffDto;
using EventTemplateSyncHistoryItemDto = Explore.Application.DTOs.EventTemplateSync.EventTemplateSyncHistoryItemDto;

namespace Explore.API.Mcp;

[McpServerToolType]
public sealed class EventManagementMcpTools(
    IMediator mediator,
    IUserContext userContext,
    ITenantContext tenantContext,
    IResourceAssembler<EventDto, EventListDto> eventResourceAssembler,
    IHttpContextAccessor httpContextAccessor,
    IAiContextGateway aiContextGateway)
{
    private const int DefaultPublicEventPageSize = 10;
    private const int MaxPublicEventPageSize = 25;
    private const int DefaultMyEventsPageSize = 10;
    private const int MaxMyEventsPageSize = 25;
    private const int MaxSearchTermLength = 120;
    private const int MaxShortTextLength = 500;
    private const int MaxLongTextLength = 2_000;
    private const int MaxPublicProgramSections = 10;
    private const int MaxPublicProgramSessionGroups = 50;
    private const int MaxPublicProgramDays = 30;
    private const int MaxPublicProgramItems = 100;
    private const int MaxPublicSessions = 100;
    private const int MaxReadinessWarnings = 25;
    private const int MaxPublishReadinessErrors = 25;
    private const int MaxCreationPublisherOptions = 50;
    private const int DefaultManagementPageSize = 10;
    private const int MaxManagementPageSize = 25;
    private const int MaxManagedSessions = 100;
    private const int MaxManagedSessionGroups = 50;
    private const int MaxManagedDays = 30;
    private const int MaxManagedAgendaItems = 100;
    private const int MaxCustomPropertyDefinitions = 25;
    private const int MaxCustomPropertyValues = 100;
    private const int MaxManagedRegistrations = 100;
    private const int MaxTeamMembers = 50;
    private const int MaxAssignableRolePresets = 50;
    private const int MaxPermissionCodes = 100;
    private const int MaxTemplateCatalogItems = 25;
    private const int MaxSyncKeys = 50;
    private const int MaxSyncHistoryItems = 25;

    [McpServerTool(
        Name = "search_public_events",
        Title = "Search public events",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [AllowAnonymous]
    [Description("Search published public ISLAMU events with bounded pagination. Draft, archived, private, and members-only events are not returned.")]
    public async Task<string> SearchPublicEventsAsync(
        [Description("Optional free-text search across public event titles and descriptions. Values longer than 120 characters are truncated.")]
        string? searchTerm = null,
        [Description("One-based page number. Values below 1 are treated as 1.")]
        int pageNumber = 1,
        [Description("Requested page size. Values are capped at 25 for MCP public discovery.")]
        int pageSize = DefaultPublicEventPageSize,
        [Description("Optional sort field: date, title, views, or createdAt. Unknown values use the API default.")]
        string? sortBy = null,
        [Description("True sorts descending; false sorts ascending.")]
        bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var startedAt = Stopwatch.GetTimestamp();
        using var activity = McpAdapterTelemetry.StartToolCall("search_public_events", projected: false);

        try
        {
            var normalizedPageNumber = Math.Max(1, pageNumber);
            var normalizedPageSize = pageSize <= 0
                ? DefaultPublicEventPageSize
                : Math.Clamp(pageSize, 1, MaxPublicEventPageSize);
            var pageSizeWasClamped = normalizedPageSize != pageSize;

            var result = await mediator.Send(
                new GetEventListRequest
                {
                    PageNumber = normalizedPageNumber,
                    PageSize = normalizedPageSize,
                    SearchTerm = NormalizeSearchTerm(searchTerm),
                    SortBy = NormalizeSortBy(sortBy),
                    SortDescending = sortDescending
                },
                cancellationToken);

            var descriptor = new EventMcpSearchResultDescriptor(
                result.PageNumber,
                result.PageSize,
                pageSizeWasClamped,
                result.TotalCount,
                result.TotalPages,
                result.HasPreviousPage,
                result.HasNextPage,
                result.Items.Select(MapSummary).ToArray());

            McpAdapterTelemetry.MarkSuccess(activity);
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                "search_public_events",
                projected: false,
                outcome: "succeeded");

            return JsonSerializer.Serialize(
                descriptor,
                EventManagementMcpJsonContext.Default.EventMcpSearchResultDescriptor);
        }
        catch (OperationCanceledException)
        {
            McpAdapterTelemetry.MarkCancelled(activity);
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                "search_public_events",
                projected: false,
                outcome: "cancelled",
                failureCode: "cancelled");
            throw;
        }
        catch
        {
            McpAdapterTelemetry.MarkFailure(activity, "unknown");
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                "search_public_events",
                projected: false,
                outcome: "failed",
                failureCode: "unknown");
            throw;
        }
    }

    [McpServerTool(
        Name = "get_public_event",
        Title = "Get public event",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [AllowAnonymous]
    [Description("Read bounded public details for a published public event. Hidden, draft, archived, private, or missing events return a safe not-found descriptor.")]
    public async Task<string> GetPublicEventAsync(
        [Description("Event identifier to read.")]
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var startedAt = Stopwatch.GetTimestamp();
        using var activity = McpAdapterTelemetry.StartToolCall("get_public_event", projected: false);

        try
        {
            var eventDto = await GetPublicEventOrNullAsync(eventId, cancellationToken);
            var descriptor = eventDto is null
                ? EventMcpEventResultDescriptor.NotFound(eventId)
                : new EventMcpEventResultDescriptor(
                    Found: true,
                    EventId: eventDto.Id,
                    FailureCode: null,
                    Event: MapDetail(eventDto));

            McpAdapterTelemetry.MarkSuccess(activity);
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                "get_public_event",
                projected: false,
                outcome: "succeeded");

            return JsonSerializer.Serialize(
                descriptor,
                EventManagementMcpJsonContext.Default.EventMcpEventResultDescriptor);
        }
        catch (OperationCanceledException)
        {
            McpAdapterTelemetry.MarkCancelled(activity);
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                "get_public_event",
                projected: false,
                outcome: "cancelled",
                failureCode: "cancelled");
            throw;
        }
        catch
        {
            McpAdapterTelemetry.MarkFailure(activity, "unknown");
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                "get_public_event",
                projected: false,
                outcome: "failed",
                failureCode: "unknown");
            throw;
        }
    }

    [McpServerTool(
        Name = "get_public_event_program_summary",
        Title = "Get public event program summary",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [AllowAnonymous]
    [Description("Read a bounded public program summary for a published public event. Hidden, draft, archived, private, or missing events return a safe not-found descriptor.")]
    public async Task<string> GetPublicEventProgramSummaryAsync(
        [Description("Event identifier whose public program summary should be read.")]
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "get_public_event_program_summary";
        var startedAt = Stopwatch.GetTimestamp();
        using var activity = McpAdapterTelemetry.StartToolCall(toolName, projected: false);

        try
        {
            var publicEvent = await GetPublicEventOrNullAsync(eventId, cancellationToken);
            var summary = publicEvent is null
                ? null
                : await mediator.Send(new GetEventProgramSummaryRequest { EventId = eventId }, cancellationToken);
            EventMcpProgramResultDescriptor descriptor;
            if (summary is null)
            {
                descriptor = EventMcpProgramResultDescriptor.NotFound(eventId);
            }
            else
            {
                EnforcePublicProgramLocationDisclosureCeiling(summary);
                descriptor = new EventMcpProgramResultDescriptor(
                    Found: true,
                    EventId: summary.EventId,
                    FailureCode: null,
                    Program: MapProgram(summary));
            }

            McpAdapterTelemetry.MarkSuccess(activity);
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "succeeded");

            return JsonSerializer.Serialize(
                descriptor,
                EventManagementMcpJsonContext.Default.EventMcpProgramResultDescriptor);
        }
        catch (OperationCanceledException)
        {
            McpAdapterTelemetry.MarkCancelled(activity);
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "cancelled",
                failureCode: "cancelled");
            throw;
        }
        catch
        {
            McpAdapterTelemetry.MarkFailure(activity, "unknown");
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "failed",
                failureCode: "unknown");
            throw;
        }
    }

    [McpServerTool(
        Name = "list_public_event_sessions",
        Title = "List public event sessions",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [AllowAnonymous]
    [Description("List bounded public sessions for a published public event. Hidden, draft, archived, private, or missing events return a safe not-found descriptor.")]
    public async Task<string> ListPublicEventSessionsAsync(
        [Description("Event identifier whose public sessions should be listed.")]
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "list_public_event_sessions";
        var startedAt = Stopwatch.GetTimestamp();
        using var activity = McpAdapterTelemetry.StartToolCall(toolName, projected: false);

        try
        {
            var publicEvent = await GetPublicEventOrNullAsync(eventId, cancellationToken);
            var sessions = publicEvent is null
                ? null
                : await mediator.Send(new GetSessionsByEventRequest { EventId = eventId }, cancellationToken);
            EventMcpSessionListResultDescriptor descriptor;
            if (sessions is null)
            {
                descriptor = EventMcpSessionListResultDescriptor.NotFound(eventId);
            }
            else
            {
                EnforcePublicSessionLocationDisclosureCeiling(sessions);
                descriptor = MapSessions(eventId, sessions);
            }

            McpAdapterTelemetry.MarkSuccess(activity);
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "succeeded");

            return JsonSerializer.Serialize(
                descriptor,
                EventManagementMcpJsonContext.Default.EventMcpSessionListResultDescriptor);
        }
        catch (OperationCanceledException)
        {
            McpAdapterTelemetry.MarkCancelled(activity);
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "cancelled",
                failureCode: "cancelled");
            throw;
        }
        catch
        {
            McpAdapterTelemetry.MarkFailure(activity, "unknown");
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "failed",
                failureCode: "unknown");
            throw;
        }
    }

    [McpServerTool(
        Name = "list_my_events",
        Title = "List my events",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Authorize(Policy = McpAuthorizationPolicies.EventManagementRead)]
    [Description("List bounded event summaries managed by the authenticated principal. Requires MCP read authorization and never accepts a caller-supplied user id.")]
    public async Task<string> ListMyEventsAsync(
        [Description("One-based page number. Values below 1 are treated as 1.")]
        int pageNumber = 1,
        [Description("Requested page size. Values are capped at 25 for MCP management reads.")]
        int pageSize = DefaultMyEventsPageSize,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "list_my_events";
        var startedAt = Stopwatch.GetTimestamp();
        using var activity = McpAdapterTelemetry.StartToolCall(toolName, projected: false);

        try
        {
            var userId = userContext.GetRequiredUserId();
            var normalizedPageNumber = Math.Max(1, pageNumber);
            var normalizedPageSize = pageSize <= 0
                ? DefaultMyEventsPageSize
                : Math.Clamp(pageSize, 1, MaxMyEventsPageSize);
            var pageSizeWasClamped = normalizedPageSize != pageSize;

            var result = await mediator.Send(
                new GetMyEventsRequest
                {
                    UserId = userId.ToString(),
                    PageNumber = normalizedPageNumber,
                    PageSize = normalizedPageSize
                },
                cancellationToken);

            var descriptor = new EventMcpMyEventsResultDescriptor(
                result.PageNumber,
                result.PageSize,
                pageSizeWasClamped,
                result.TotalCount,
                result.TotalPages,
                result.HasPreviousPage,
                result.HasNextPage,
                result.Items.Select(MapSummary).ToArray());

            McpAdapterTelemetry.MarkSuccess(activity);
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "succeeded");

            return JsonSerializer.Serialize(
                descriptor,
                EventManagementMcpJsonContext.Default.EventMcpMyEventsResultDescriptor);
        }
        catch (OperationCanceledException)
        {
            McpAdapterTelemetry.MarkCancelled(activity);
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "cancelled",
                failureCode: "cancelled");
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            McpAdapterTelemetry.MarkFailure(activity, "unauthorized");
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "failed",
                failureCode: "unauthorized");
            throw;
        }
        catch
        {
            McpAdapterTelemetry.MarkFailure(activity, "unknown");
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "failed",
                failureCode: "unknown");
            throw;
        }
    }

    [McpServerTool(
        Name = "get_event_creation_context",
        Title = "Get event creation context",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Authorize(Policy = McpAuthorizationPolicies.EventManagementRead)]
    [Description("Read the authenticated principal's tenant event-creation policy and server-issued publisher options. Requires MCP read authorization and does not inspect roles or claims in MCP.")]
    public async Task<string> GetEventCreationContextAsync(CancellationToken cancellationToken = default)
    {
        const string toolName = "get_event_creation_context";
        var startedAt = Stopwatch.GetTimestamp();
        using var activity = McpAdapterTelemetry.StartToolCall(toolName, projected: false);

        try
        {
            var context = await mediator.Send(new GetEventCreationContextRequest(), cancellationToken);
            var descriptor = MapCreationContext(context);

            McpAdapterTelemetry.MarkSuccess(activity);
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "succeeded");

            return JsonSerializer.Serialize(
                descriptor,
                EventManagementMcpJsonContext.Default.EventMcpCreationContextDescriptor);
        }
        catch (OperationCanceledException)
        {
            McpAdapterTelemetry.MarkCancelled(activity);
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "cancelled",
                failureCode: "cancelled");
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            McpAdapterTelemetry.MarkFailure(activity, "unauthorized");
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "failed",
                failureCode: "unauthorized");
            throw;
        }
        catch
        {
            McpAdapterTelemetry.MarkFailure(activity, "unknown");
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "failed",
                failureCode: "unknown");
            throw;
        }
    }

    [McpServerTool(
        Name = "get_event_publish_readiness",
        Title = "Get event publish readiness",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Authorize(Policy = McpAuthorizationPolicies.EventManagementRead)]
    [Description("Read bounded publish-readiness errors for a managed draft event. Requires MCP read authorization and the REST HAL publish-readiness affordance.")]
    public async Task<string> GetEventPublishReadinessAsync(
        [Description("Event identifier whose publish readiness should be read.")]
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "get_event_publish_readiness";
        var startedAt = Stopwatch.GetTimestamp();
        using var activity = McpAdapterTelemetry.StartToolCall(toolName, projected: false);

        try
        {
            var descriptor = await BuildPublishReadinessResultAsync(eventId, cancellationToken);

            McpAdapterTelemetry.MarkSuccess(activity);
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "succeeded");

            return JsonSerializer.Serialize(
                descriptor,
                EventManagementMcpJsonContext.Default.EventMcpPublishReadinessResultDescriptor);
        }
        catch (OperationCanceledException)
        {
            McpAdapterTelemetry.MarkCancelled(activity);
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "cancelled",
                failureCode: "cancelled");
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            McpAdapterTelemetry.MarkFailure(activity, "unauthorized");
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "failed",
                failureCode: "unauthorized");
            throw;
        }
        catch
        {
            McpAdapterTelemetry.MarkFailure(activity, "unknown");
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "failed",
                failureCode: "unknown");
            throw;
        }
    }

    [McpServerTool(
        Name = "get_event_program_management_context",
        Title = "Get event program management context",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Authorize(Policy = McpAuthorizationPolicies.EventManagementRead)]
    [Description("Read bounded organizer program context for sessions, program sections, days, and agenda items. Requires MCP read authorization and the REST HAL edit affordance for the event.")]
    public Task<string> GetEventProgramManagementContextAsync(
        [Description("Event identifier whose organizer program context should be read.")]
        Guid eventId,
        CancellationToken cancellationToken = default)
        => ExecuteManagementReadToolAsync(
            "get_event_program_management_context",
            token => BuildProgramManagementContextAsync(eventId, token),
            EventManagementMcpJsonContext.Default.EventMcpProgramManagementResultDescriptor,
            cancellationToken);

    [McpServerTool(
        Name = "get_event_custom_properties_context",
        Title = "Get event custom properties context",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Authorize(Policy = McpAuthorizationPolicies.EventManagementRead)]
    [Description("Read bounded event-local custom property definitions and values. Requires MCP read authorization and the REST HAL edit affordance for the event.")]
    public Task<string> GetEventCustomPropertiesContextAsync(
        [Description("Event identifier whose custom property context should be read.")]
        Guid eventId,
        [Description("One-based definition page number. Values below 1 are treated as 1.")]
        int pageNumber = 1,
        [Description("Requested definition page size. Values are capped at 25 for MCP management reads.")]
        int pageSize = DefaultManagementPageSize,
        CancellationToken cancellationToken = default)
        => ExecuteManagementReadToolAsync(
            "get_event_custom_properties_context",
            token => BuildCustomPropertiesContextAsync(eventId, pageNumber, pageSize, token),
            EventManagementMcpJsonContext.Default.EventMcpCustomPropertiesResultDescriptor,
            cancellationToken);

    [McpServerTool(
        Name = "get_event_registrations_context",
        Title = "Get event registrations context",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Authorize(Policy = McpAuthorizationPolicies.EventManagementRead)]
    [Description("Read a bounded event-scoped registration-order management page. Requires MCP read authorization and the REST HAL order affordance for the event.")]
    public Task<string> GetEventRegistrationsContextAsync(
        [Description("Event identifier whose registration context should be read.")]
        Guid eventId,
        [Description("One-based registration page number. Values below 1 are treated as 1.")]
        int pageNumber = 1,
        [Description("Requested registration page size. Values are capped at 100 for MCP management reads.")]
        int pageSize = DefaultManagementPageSize,
        CancellationToken cancellationToken = default)
        => ExecuteManagementReadToolAsync(
            "get_event_registrations_context",
            token => BuildRegistrationsContextAsync(eventId, pageNumber, pageSize, token),
            EventManagementMcpJsonContext.Default.EventMcpRegistrationsContextResultDescriptor,
            cancellationToken);

    [McpServerTool(
        Name = "get_event_team_context",
        Title = "Get event team context",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Authorize(Policy = McpAuthorizationPolicies.EventManagementRead)]
    [Description("Read bounded event-team members, current caller permissions, and assignable role presets. Requires MCP read authorization and the REST HAL team affordance for the event.")]
    public Task<string> GetEventTeamContextAsync(
        [Description("Event identifier whose team context should be read.")]
        Guid eventId,
        [Description("True includes inactive assignments when the underlying team query authorizes them.")]
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
        => ExecuteManagementReadToolAsync(
            "get_event_team_context",
            token => BuildTeamContextAsync(eventId, includeInactive, token),
            EventManagementMcpJsonContext.Default.EventMcpTeamContextResultDescriptor,
            cancellationToken);

    [McpServerTool(
        Name = "get_event_template_catalog_context",
        Title = "Get event template catalog context",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Authorize(Policy = McpAuthorizationPolicies.EventManagementRead)]
    [Description("Read bounded event-template catalog entries and, optionally, session templates for a selected event template. Requires MCP read authorization and the REST HAL edit affordance for the event.")]
    public Task<string> GetEventTemplateCatalogContextAsync(
        [Description("Event identifier used to gate template catalog discovery through the event's REST HAL edit affordance.")]
        Guid eventId,
        [Description("Optional event-type lookup id. When omitted, the event's own EventTypeId is used if present.")]
        int? eventTypeId = null,
        [Description("Optional parent event template id whose session templates should be listed.")]
        Guid? eventTemplateId = null,
        [Description("One-based template page number. Values below 1 are treated as 1.")]
        int pageNumber = 1,
        [Description("Requested template page size. Values are capped at 25 for MCP management reads.")]
        int pageSize = DefaultManagementPageSize,
        [Description("One-based session-template page number when eventTemplateId is supplied.")]
        int sessionTemplatePageNumber = 1,
        [Description("Requested session-template page size. Values are capped at 25 for MCP management reads.")]
        int sessionTemplatePageSize = DefaultManagementPageSize,
        CancellationToken cancellationToken = default)
        => ExecuteManagementReadToolAsync(
            "get_event_template_catalog_context",
            token => BuildTemplateCatalogContextAsync(
                eventId,
                eventTypeId,
                eventTemplateId,
                pageNumber,
                pageSize,
                sessionTemplatePageNumber,
                sessionTemplatePageSize,
                token),
            EventManagementMcpJsonContext.Default.EventMcpTemplateCatalogResultDescriptor,
            cancellationToken);

    [McpServerTool(
        Name = "get_event_template_sync_context",
        Title = "Get event template sync context",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Authorize(Policy = McpAuthorizationPolicies.EventManagementRead)]
    [Description("Read bounded event-template sync diff and history context. Requires MCP read authorization and the REST HAL edit affordance for the event.")]
    public Task<string> GetEventTemplateSyncContextAsync(
        [Description("Event identifier whose template sync context should be read.")]
        Guid eventId,
        [Description("Optional target template version for diff computation. Omit to read history only.")]
        int? targetTemplateVersion = null,
        [Description("One-based sync history page number. Values below 1 are treated as 1.")]
        int historyPageNumber = 1,
        [Description("Requested sync history page size. Values are capped at 25 for MCP management reads.")]
        int historyPageSize = DefaultManagementPageSize,
        CancellationToken cancellationToken = default)
        => ExecuteManagementReadToolAsync(
            "get_event_template_sync_context",
            token => BuildEventTemplateSyncContextAsync(
                eventId,
                targetTemplateVersion,
                historyPageNumber,
                historyPageSize,
                token),
            EventManagementMcpJsonContext.Default.EventMcpTemplateSyncContextResultDescriptor,
            cancellationToken);

    [McpServerTool(
        Name = "get_event_session_template_sync_context",
        Title = "Get event session template sync context",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Authorize(Policy = McpAuthorizationPolicies.EventManagementRead)]
    [Description("Read bounded event-session template sync diff and history context. Requires MCP read authorization, the parent event id, and the REST HAL edit affordance for that event.")]
    public Task<string> GetEventSessionTemplateSyncContextAsync(
        [Description("Parent event identifier used to gate the session sync read through the event's REST HAL edit affordance.")]
        Guid eventId,
        [Description("Event session identifier whose template sync context should be read.")]
        Guid sessionId,
        [Description("Optional target session-template version for diff computation. Omit to read history only.")]
        int? targetTemplateVersion = null,
        [Description("One-based sync history page number. Values below 1 are treated as 1.")]
        int historyPageNumber = 1,
        [Description("Requested sync history page size. Values are capped at 25 for MCP management reads.")]
        int historyPageSize = DefaultManagementPageSize,
        CancellationToken cancellationToken = default)
        => ExecuteManagementReadToolAsync(
            "get_event_session_template_sync_context",
            token => BuildEventSessionTemplateSyncContextAsync(
                eventId,
                sessionId,
                targetTemplateVersion,
                historyPageNumber,
                historyPageSize,
                token),
            EventManagementMcpJsonContext.Default.EventMcpSessionTemplateSyncContextResultDescriptor,
            cancellationToken);

    private async Task<EventMcpPublishReadinessResultDescriptor> BuildPublishReadinessResultAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var eventDto = await mediator.Send(new GetEventManagementDetailsRequest { Id = eventId }, cancellationToken);
        if (eventDto is null)
        {
            return EventMcpPublishReadinessResultDescriptor.NotFound(eventId);
        }

        var links = await GetEventHalLinksAsync(eventDto);
        if (!links.ContainsKey(LinkRelations.PublishReadiness))
        {
            return EventMcpPublishReadinessResultDescriptor.Unavailable(eventDto.Id);
        }

        var readiness = await mediator.Send(
            new GetEventPublishReadinessRequest { Id = eventDto.Id },
            cancellationToken);
        if (readiness is null)
        {
            return EventMcpPublishReadinessResultDescriptor.NotFound(eventDto.Id);
        }

        var truncatedFields = new List<string>();
        var readinessDescriptor = EventManagementMcpReadinessMapper.Map(
            readiness,
            MaxPublishReadinessErrors,
            MaxShortTextLength,
            truncatedFields);

        return new EventMcpPublishReadinessResultDescriptor(
            Found: true,
            EventId: eventDto.Id,
            Available: true,
            FailureCode: null,
            PublishReadiness: readinessDescriptor,
            TruncatedFields: truncatedFields);
    }

    private async Task<string> ExecuteManagementReadToolAsync<TDescriptor>(
        string toolName,
        Func<CancellationToken, Task<TDescriptor>> buildDescriptor,
        JsonTypeInfo<TDescriptor> jsonTypeInfo,
        CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();
        using var activity = McpAdapterTelemetry.StartToolCall(toolName, projected: false);

        try
        {
            var descriptor = await buildDescriptor(cancellationToken);

            McpAdapterTelemetry.MarkSuccess(activity);
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "succeeded");

            return JsonSerializer.Serialize(descriptor, jsonTypeInfo);
        }
        catch (OperationCanceledException)
        {
            McpAdapterTelemetry.MarkCancelled(activity);
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "cancelled",
                failureCode: "cancelled");
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            McpAdapterTelemetry.MarkFailure(activity, "unauthorized");
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "failed",
                failureCode: "unauthorized");
            throw;
        }
        catch
        {
            McpAdapterTelemetry.MarkFailure(activity, "unknown");
            McpAdapterTelemetry.RecordToolCall(
                Stopwatch.GetElapsedTime(startedAt),
                toolName,
                projected: false,
                outcome: "failed",
                failureCode: "unknown");
            throw;
        }
    }

    private async Task<EventMcpProgramManagementResultDescriptor> BuildProgramManagementContextAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var gate = await BuildManagementReadGateAsync(eventId, LinkRelations.Edit, cancellationToken);
        if (!gate.Found)
        {
            return EventMcpProgramManagementResultDescriptor.NotFound(eventId);
        }

        if (!gate.IsAvailable)
        {
            return EventMcpProgramManagementResultDescriptor.Unavailable(eventId);
        }

        var eventDto = gate.Event!;
        var sessions = await mediator.Send(new GetManagedSessionsByEventRequest { EventId = eventDto.Id }, cancellationToken);
        var sessionGroups = await mediator.Send(
            new GetManagedEventSessionGroupsByEventRequest { EventId = eventDto.Id },
            cancellationToken);
        var days = await mediator.Send(
            new GetManagedEventDaysByEventRequest { EventId = eventDto.Id },
            cancellationToken);
        var agendaItems = await mediator.Send(
            new GetManagedEventAgendaItemsByEventRequest { EventId = eventDto.Id },
            cancellationToken);

        EnforceManagedProgramLocationDisclosureCeiling(sessions, sessionGroups);

        return new EventMcpProgramManagementResultDescriptor(
            Found: true,
            EventId: eventDto.Id,
            Available: true,
            FailureCode: null,
            Context: MapProgramManagementContext(eventDto, sessions, sessionGroups, days, agendaItems));
    }

    private async Task<EventMcpCustomPropertiesResultDescriptor> BuildCustomPropertiesContextAsync(
        Guid eventId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var gate = await BuildManagementReadGateAsync(eventId, LinkRelations.Edit, cancellationToken);
        if (!gate.Found)
        {
            return EventMcpCustomPropertiesResultDescriptor.NotFound(eventId);
        }

        if (!gate.IsAvailable)
        {
            return EventMcpCustomPropertiesResultDescriptor.Unavailable(eventId);
        }

        var eventDto = gate.Event!;
        var (normalizedPageNumber, normalizedPageSize, pageSizeWasClamped) =
            NormalizeManagementPage(pageNumber, pageSize, MaxCustomPropertyDefinitions);

        var definitions = await mediator.Send(
            new GetEventCustomPropertyDefinitionListRequest
            {
                EventId = eventDto.Id,
                PageNumber = normalizedPageNumber,
                PageSize = normalizedPageSize
            },
            cancellationToken);
        var values = await mediator.Send(new GetEventCustomPropertyValuesRequest { EventId = eventDto.Id }, cancellationToken);

        var truncatedFields = new List<string>();
        var returnedValues = values
            .Take(MaxCustomPropertyValues)
            .Select(value => MapCustomPropertyValue(value, truncatedFields))
            .ToArray();
        if (values.Count > returnedValues.Length)
        {
            truncatedFields.Add("Values");
        }

        var context = new EventMcpCustomPropertiesContextDescriptor(
            eventDto.Id,
            eventDto.ConcurrencyStamp,
            definitions.PageNumber,
            definitions.PageSize,
            pageSizeWasClamped,
            definitions.TotalCount,
            definitions.TotalPages,
            definitions.HasPreviousPage,
            definitions.HasNextPage,
            definitions.Items.Count,
            definitions.Items.Select(definition => MapCustomPropertyDefinition(definition, truncatedFields)).ToArray(),
            values.Count,
            returnedValues.Length,
            values.Count > returnedValues.Length,
            returnedValues,
            truncatedFields);

        return new EventMcpCustomPropertiesResultDescriptor(
            Found: true,
            EventId: eventDto.Id,
            Available: true,
            FailureCode: null,
            Context: context);
    }

    private async Task<EventMcpTeamContextResultDescriptor> BuildTeamContextAsync(
        Guid eventId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var gate = await BuildManagementReadGateAsync(eventId, LinkRelations.Team, cancellationToken);
        if (!gate.Found)
        {
            return EventMcpTeamContextResultDescriptor.NotFound(eventId);
        }

        if (!gate.IsAvailable)
        {
            return EventMcpTeamContextResultDescriptor.Unavailable(eventId);
        }

        var eventDto = gate.Event!;
        var userId = userContext.GetRequiredUserId();
        var teamMembers = await mediator.Send(
            new GetEventTeamListRequest
            {
                TenantId = tenantContext.TenantId,
                EventId = eventDto.Id,
                IncludeInactive = includeInactive
            },
            cancellationToken);
        var permissions = await mediator.Send(
            new GetCurrentUserEventPermissionsRequest
            {
                TenantId = tenantContext.TenantId,
                EventId = eventDto.Id,
                UserId = userId
            },
            cancellationToken);
        var assignablePresets = await mediator.Send(
            new GetAssignableEventRolePresetsRequest
            {
                TenantId = tenantContext.TenantId,
                EventId = eventDto.Id,
                AssignerUserId = userId
            },
            cancellationToken);

        var truncatedFields = new List<string>();
        var returnedMembers = teamMembers
            .Take(MaxTeamMembers)
            .Select(member => MapTeamMember(member, truncatedFields))
            .ToArray();
        var returnedPresets = assignablePresets
            .Take(MaxAssignableRolePresets)
            .Select(preset => MapRolePreset(preset, truncatedFields))
            .ToArray();

        if (teamMembers.Count > returnedMembers.Length)
        {
            truncatedFields.Add("TeamMembers");
        }

        if (assignablePresets.Count > returnedPresets.Length)
        {
            truncatedFields.Add("AssignableRolePresets");
        }

        var context = new EventMcpTeamContextDescriptor(
            eventDto.Id,
            eventDto.ConcurrencyStamp,
            includeInactive,
            MapCurrentUserPermissions(permissions, truncatedFields),
            teamMembers.Count,
            returnedMembers.Length,
            teamMembers.Count > returnedMembers.Length,
            returnedMembers,
            assignablePresets.Count,
            returnedPresets.Length,
            assignablePresets.Count > returnedPresets.Length,
            returnedPresets,
            truncatedFields);

        return new EventMcpTeamContextResultDescriptor(
            Found: true,
            EventId: eventDto.Id,
            Available: true,
            FailureCode: null,
            Context: context);
    }

    private async Task<EventMcpRegistrationsContextResultDescriptor> BuildRegistrationsContextAsync(
        Guid eventId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var gate = await BuildManagementReadGateAsync(eventId, LinkRelations.Edit, cancellationToken);
        if (!gate.Found)
        {
            return EventMcpRegistrationsContextResultDescriptor.NotFound(eventId);
        }

        if (!gate.IsAvailable)
        {
            return EventMcpRegistrationsContextResultDescriptor.Unavailable(eventId);
        }

        var eventDto = gate.Event!;
        var (normalizedPageNumber, normalizedPageSize, pageSizeWasClamped) =
            NormalizeManagementPage(pageNumber, pageSize, MaxManagedRegistrations);
        IReadOnlyList<RegistrationOrderDto> orders = await mediator.Send(
            new GetEventRegistrationOrdersQuery(eventDto.Id),
            cancellationToken);
        int totalOrderCount = orders.Count;
        int totalPages = totalOrderCount == 0
            ? 0
            : (int)Math.Ceiling(totalOrderCount / (double)normalizedPageSize);
        RegistrationOrderDto[] page = orders
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToArray();

        var truncatedFields = new List<string>();
        var context = new EventMcpRegistrationsContextDescriptor(
            eventDto.Id,
            eventDto.ConcurrencyStamp,
            normalizedPageNumber,
            normalizedPageSize,
            pageSizeWasClamped,
            totalOrderCount,
            totalPages,
            normalizedPageNumber > 1 && totalPages > 0,
            normalizedPageNumber < totalPages,
            page.Select(order => MapRegistrationOrder(order, truncatedFields)).ToArray(),
            truncatedFields);

        return new EventMcpRegistrationsContextResultDescriptor(
            Found: true,
            EventId: eventDto.Id,
            Available: true,
            FailureCode: null,
            Context: context);
    }

    private async Task<EventMcpTemplateCatalogResultDescriptor> BuildTemplateCatalogContextAsync(
        Guid eventId,
        int? eventTypeId,
        Guid? eventTemplateId,
        int pageNumber,
        int pageSize,
        int sessionTemplatePageNumber,
        int sessionTemplatePageSize,
        CancellationToken cancellationToken)
    {
        var gate = await BuildManagementReadGateAsync(eventId, LinkRelations.Edit, cancellationToken);
        if (!gate.Found)
        {
            return EventMcpTemplateCatalogResultDescriptor.NotFound(eventId);
        }

        if (!gate.IsAvailable)
        {
            return EventMcpTemplateCatalogResultDescriptor.Unavailable(eventId);
        }

        var eventDto = gate.Event!;
        var (normalizedPageNumber, normalizedPageSize, pageSizeWasClamped) =
            NormalizeManagementPage(pageNumber, pageSize, MaxTemplateCatalogItems);
        var templates = await mediator.Send(
            new GetEventTemplateListRequest
            {
                EventTypeId = eventTypeId ?? eventDto.EventTypeId,
                PageNumber = normalizedPageNumber,
                PageSize = normalizedPageSize
            },
            cancellationToken);

        EventMcpSessionTemplateCatalogPageDescriptor? sessionTemplates = null;
        if (eventTemplateId.HasValue)
        {
            var (normalizedSessionPageNumber, normalizedSessionPageSize, sessionPageSizeWasClamped) =
                NormalizeManagementPage(sessionTemplatePageNumber, sessionTemplatePageSize, MaxTemplateCatalogItems);
            var sessionTemplatePage = await mediator.Send(
                new GetEventSessionTemplateListRequest
                {
                    EventTemplateId = eventTemplateId.Value,
                    PageNumber = normalizedSessionPageNumber,
                    PageSize = normalizedSessionPageSize
                },
                cancellationToken);

            sessionTemplates = new EventMcpSessionTemplateCatalogPageDescriptor(
                eventTemplateId.Value,
                sessionTemplatePage.PageNumber,
                sessionTemplatePage.PageSize,
                sessionPageSizeWasClamped,
                sessionTemplatePage.TotalCount,
                sessionTemplatePage.TotalPages,
                sessionTemplatePage.HasPreviousPage,
                sessionTemplatePage.HasNextPage,
                sessionTemplatePage.Items.Select(MapSessionTemplate).ToArray());
        }

        var truncatedFields = new List<string>();
        var context = new EventMcpTemplateCatalogContextDescriptor(
            eventDto.Id,
            eventDto.ConcurrencyStamp,
            eventTypeId ?? eventDto.EventTypeId,
            templates.PageNumber,
            templates.PageSize,
            pageSizeWasClamped,
            templates.TotalCount,
            templates.TotalPages,
            templates.HasPreviousPage,
            templates.HasNextPage,
            templates.Items.Select(template => MapTemplate(template, truncatedFields)).ToArray(),
            sessionTemplates,
            truncatedFields);

        return new EventMcpTemplateCatalogResultDescriptor(
            Found: true,
            EventId: eventDto.Id,
            Available: true,
            FailureCode: null,
            Context: context);
    }

    private async Task<EventMcpTemplateSyncContextResultDescriptor> BuildEventTemplateSyncContextAsync(
        Guid eventId,
        int? targetTemplateVersion,
        int historyPageNumber,
        int historyPageSize,
        CancellationToken cancellationToken)
    {
        var gate = await BuildManagementReadGateAsync(eventId, LinkRelations.Edit, cancellationToken);
        if (!gate.Found)
        {
            return EventMcpTemplateSyncContextResultDescriptor.NotFound(eventId);
        }

        if (!gate.IsAvailable)
        {
            return EventMcpTemplateSyncContextResultDescriptor.Unavailable(eventId);
        }

        var eventDto = gate.Event!;
        var (normalizedHistoryPageNumber, normalizedHistoryPageSize, historyPageSizeWasClamped) =
            NormalizeManagementPage(historyPageNumber, historyPageSize, MaxSyncHistoryItems);
        var history = await mediator.Send(
            new GetEventTemplateSyncHistoryQuery(eventDto.Id, normalizedHistoryPageNumber, normalizedHistoryPageSize),
            cancellationToken);
        var diffRead = await ReadEventTemplateDiffAsync(eventDto.Id, targetTemplateVersion, cancellationToken);
        var truncatedFields = new List<string>();

        var context = new EventMcpTemplateSyncContextDescriptor(
            eventDto.Id,
            eventDto.ConcurrencyStamp,
            targetTemplateVersion,
            diffRead.IsAvailable,
            diffRead.FailureCode,
            diffRead.Diff,
            MapEventTemplateSyncHistory(history, historyPageSizeWasClamped, truncatedFields),
            truncatedFields);

        return new EventMcpTemplateSyncContextResultDescriptor(
            Found: true,
            EventId: eventDto.Id,
            Available: true,
            FailureCode: null,
            Context: context);
    }

    private async Task<EventMcpSessionTemplateSyncContextResultDescriptor> BuildEventSessionTemplateSyncContextAsync(
        Guid eventId,
        Guid sessionId,
        int? targetTemplateVersion,
        int historyPageNumber,
        int historyPageSize,
        CancellationToken cancellationToken)
    {
        var gate = await BuildManagementReadGateAsync(eventId, LinkRelations.Edit, cancellationToken);
        if (!gate.Found)
        {
            return EventMcpSessionTemplateSyncContextResultDescriptor.NotFound(eventId, sessionId);
        }

        if (!gate.IsAvailable)
        {
            return EventMcpSessionTemplateSyncContextResultDescriptor.Unavailable(eventId, sessionId);
        }

        var sessions = await mediator.Send(new GetManagedSessionsByEventRequest { EventId = eventId }, cancellationToken);
        if (sessions.All(session => session.Id != sessionId))
        {
            return EventMcpSessionTemplateSyncContextResultDescriptor.NotFound(eventId, sessionId);
        }

        var eventDto = gate.Event!;
        var (normalizedHistoryPageNumber, normalizedHistoryPageSize, historyPageSizeWasClamped) =
            NormalizeManagementPage(historyPageNumber, historyPageSize, MaxSyncHistoryItems);
        var history = await mediator.Send(
            new GetEventSessionTemplateSyncHistoryQuery(sessionId, normalizedHistoryPageNumber, normalizedHistoryPageSize),
            cancellationToken);
        var diffRead = await ReadEventSessionTemplateDiffAsync(sessionId, targetTemplateVersion, cancellationToken);
        var truncatedFields = new List<string>();

        var context = new EventMcpSessionTemplateSyncContextDescriptor(
            eventDto.Id,
            sessionId,
            eventDto.ConcurrencyStamp,
            targetTemplateVersion,
            diffRead.IsAvailable,
            diffRead.FailureCode,
            diffRead.Diff,
            MapEventSessionTemplateSyncHistory(history, historyPageSizeWasClamped, truncatedFields),
            truncatedFields);

        return new EventMcpSessionTemplateSyncContextResultDescriptor(
            Found: true,
            EventId: eventDto.Id,
            SessionId: sessionId,
            Available: true,
            FailureCode: null,
            Context: context);
    }

    private async Task<EventMcpManagementReadGate> BuildManagementReadGateAsync(
        Guid eventId,
        string requiredLinkRelation,
        CancellationToken cancellationToken)
    {
        var eventDto = await mediator.Send(new GetEventManagementDetailsRequest { Id = eventId }, cancellationToken);
        if (eventDto is null)
        {
            return EventMcpManagementReadGate.NotFound();
        }

        var links = await GetEventHalLinksAsync(eventDto);
        return links.ContainsKey(requiredLinkRelation)
            ? EventMcpManagementReadGate.ForAvailable(eventDto, links)
            : EventMcpManagementReadGate.Unavailable(eventDto, links);
    }

    private async Task<EventMcpTemplateDiffRead> ReadEventTemplateDiffAsync(
        Guid eventId,
        int? targetTemplateVersion,
        CancellationToken cancellationToken)
    {
        if (!targetTemplateVersion.HasValue)
        {
            return EventMcpTemplateDiffRead.NotRequested();
        }

        if (targetTemplateVersion.Value <= 0)
        {
            return EventMcpTemplateDiffRead.Unavailable("invalid_target_template_version");
        }

        try
        {
            var response = await mediator.Send(
                new GetEventTemplateDiffQuery(eventId, targetTemplateVersion.Value),
                cancellationToken);
            return response.Success && response.Id is not null
                ? EventMcpTemplateDiffRead.WithDiff(MapTemplateDiff(response.Id))
                : EventMcpTemplateDiffRead.Unavailable(response.FailureCode ?? "not_available");
        }
        catch (NotFoundException)
        {
            return EventMcpTemplateDiffRead.Unavailable("not_found");
        }
    }

    private async Task<EventMcpTemplateDiffRead> ReadEventSessionTemplateDiffAsync(
        Guid sessionId,
        int? targetTemplateVersion,
        CancellationToken cancellationToken)
    {
        if (!targetTemplateVersion.HasValue)
        {
            return EventMcpTemplateDiffRead.NotRequested();
        }

        if (targetTemplateVersion.Value <= 0)
        {
            return EventMcpTemplateDiffRead.Unavailable("invalid_target_template_version");
        }

        try
        {
            var response = await mediator.Send(
                new GetEventSessionTemplateDiffQuery(sessionId, targetTemplateVersion.Value),
                cancellationToken);
            return response.Success && response.Id is not null
                ? EventMcpTemplateDiffRead.WithDiff(MapTemplateDiff(response.Id))
                : EventMcpTemplateDiffRead.Unavailable(response.FailureCode ?? "not_available");
        }
        catch (NotFoundException)
        {
            return EventMcpTemplateDiffRead.Unavailable("not_found");
        }
    }

    private async Task<IReadOnlyDictionary<string, HalLink>> GetEventHalLinksAsync(EventDto eventDto)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is required for MCP event HAL capability planning.");
        var halResource = await eventResourceAssembler.ToResource(eventDto, httpContext);
        return halResource.Links;
    }

    private static EventMcpCreationContextDescriptor MapCreationContext(EventCreationContextDto dto)
    {
        var truncatedFields = new List<string>();
        var publisherOptions = dto.PublisherOptions
            .Take(MaxCreationPublisherOptions)
            .Select(option => MapCreationPublisherOption(option, truncatedFields))
            .ToArray();

        if (dto.PublisherOptions.Count > MaxCreationPublisherOptions)
        {
            truncatedFields.Add("PublisherOptions");
        }

        return new EventMcpCreationContextDescriptor(
            dto.CanCreate,
            dto.AllowPersonalPublishing,
            dto.AllowOrganizationPublishing,
            dto.AllowGroupPublishing,
            dto.RequiresApproval,
            TrimToNull(dto.DefaultPublisherMode, MaxShortTextLength, truncatedFields, nameof(dto.DefaultPublisherMode)),
            TrimToNull(dto.UnavailableReason, MaxShortTextLength, truncatedFields, nameof(dto.UnavailableReason)),
            dto.PublisherOptions.Count,
            publisherOptions.Length,
            dto.PublisherOptions.Count > MaxCreationPublisherOptions,
            publisherOptions,
            truncatedFields);
    }

    private static EventMcpCreationPublisherOptionDescriptor MapCreationPublisherOption(
        EventCreationPublisherOptionDto dto,
        ICollection<string> truncatedFields)
        => new(
            TrimToEmpty(dto.PublisherMode, MaxShortTextLength, truncatedFields, nameof(dto.PublisherMode)),
            dto.PublisherId,
            TrimToEmpty(dto.DisplayName, MaxShortTextLength, truncatedFields, nameof(dto.DisplayName)),
            dto.CanPublish,
            TrimToNull(dto.Reason, MaxShortTextLength, truncatedFields, nameof(dto.Reason)));

    private static EventMcpProgramManagementContextDescriptor MapProgramManagementContext(
        EventDto eventDto,
        IReadOnlyCollection<EventSessionListDto> sessions,
        IReadOnlyCollection<EventSessionGroupListDto> sessionGroups,
        IReadOnlyCollection<EventDayListDto> days,
        IReadOnlyCollection<EventAgendaItemListDto> agendaItems)
    {
        var truncatedFields = new List<string>();
        var returnedSessions = sessions
            .Take(MaxManagedSessions)
            .Select(session => MapSession(session))
            .ToArray();
        var returnedSessionGroups = sessionGroups
            .Take(MaxManagedSessionGroups)
            .Select(group => MapSessionGroup(group, truncatedFields))
            .ToArray();
        var returnedDays = days
            .Take(MaxManagedDays)
            .Select(day => MapDay(day, truncatedFields))
            .ToArray();
        var returnedAgendaItems = agendaItems
            .Take(MaxManagedAgendaItems)
            .Select(item => MapAgendaItem(item, truncatedFields))
            .ToArray();

        if (sessions.Count > returnedSessions.Length)
        {
            truncatedFields.Add("Sessions");
        }

        if (sessionGroups.Count > returnedSessionGroups.Length)
        {
            truncatedFields.Add("SessionGroups");
        }

        if (days.Count > returnedDays.Length)
        {
            truncatedFields.Add("Days");
        }

        if (agendaItems.Count > returnedAgendaItems.Length)
        {
            truncatedFields.Add("AgendaItems");
        }

        return new EventMcpProgramManagementContextDescriptor(
            eventDto.Id,
            eventDto.ConcurrencyStamp,
            sessions.Count,
            returnedSessions.Length,
            sessions.Count > returnedSessions.Length,
            returnedSessions,
            sessionGroups.Count,
            returnedSessionGroups.Length,
            sessionGroups.Count > returnedSessionGroups.Length,
            returnedSessionGroups,
            days.Count,
            returnedDays.Length,
            days.Count > returnedDays.Length,
            returnedDays,
            agendaItems.Count,
            returnedAgendaItems.Length,
            agendaItems.Count > returnedAgendaItems.Length,
            returnedAgendaItems,
            truncatedFields);
    }

    private static EventMcpSessionGroupDescriptor MapSessionGroup(
        EventSessionGroupListDto dto,
        ICollection<string> truncatedFields)
        => new(
            dto.Id,
            dto.EventId,
            TrimToEmpty(dto.Name, MaxShortTextLength, truncatedFields, nameof(dto.Name)),
            TrimToNull(dto.Slug, MaxShortTextLength, truncatedFields, nameof(dto.Slug)),
            TrimToNull(dto.Color, MaxShortTextLength, truncatedFields, nameof(dto.Color)),
            dto.SortOrder,
            dto.IsPublished);

    private static EventMcpDayDescriptor MapDay(
        EventDayListDto dto,
        ICollection<string> truncatedFields)
        => new(
            dto.Id,
            dto.EventId,
            dto.LocalDate,
            TrimToNull(dto.Label, MaxShortTextLength, truncatedFields, nameof(dto.Label)),
            dto.SortOrder,
            dto.IsPublished,
            dto.AllowsDayScopeRegistration);

    private static EventMcpAgendaItemDescriptor MapAgendaItem(
        EventAgendaItemListDto dto,
        ICollection<string> truncatedFields)
        => new(
            dto.Id,
            dto.EventId,
            TrimToEmpty(dto.Title, MaxShortTextLength, truncatedFields, nameof(dto.Title)),
            dto.StartTime,
            dto.EndTime,
            dto.LocalStartDate,
            dto.LocalStartTime,
            dto.LocalEndTime,
            dto.KindId,
            TrimToNull(dto.KindFullName, MaxShortTextLength, truncatedFields, nameof(dto.KindFullName)),
            dto.SortOrder);

    private static EventMcpCustomPropertyDefinitionDescriptor MapCustomPropertyDefinition(
        EventCustomPropertyDefinitionListDto dto,
        ICollection<string> truncatedFields)
        => new(
            dto.Id,
            dto.EventId,
            TrimToEmpty(dto.Namespace, MaxShortTextLength, truncatedFields, nameof(dto.Namespace)),
            TrimToEmpty(dto.Key, MaxShortTextLength, truncatedFields, nameof(dto.Key)),
            TrimToEmpty(dto.DisplayName, MaxShortTextLength, truncatedFields, nameof(dto.DisplayName)),
            dto.PropertyType.ToString(),
            dto.IsRequired,
            dto.IsActive,
            dto.SortOrder,
            dto.ExposureLevel.ToString(),
            dto.SourceTemplateId.HasValue,
            dto.OptionCount);

    private static EventMcpCustomPropertyValueDescriptor MapCustomPropertyValue(
        EventCustomPropertyValueDto dto,
        ICollection<string> truncatedFields)
    {
        var (valueType, value) = FormatCustomPropertyValue(dto, truncatedFields);
        return new EventMcpCustomPropertyValueDescriptor(
            dto.Id,
            dto.EventCustomPropertyDefinitionId,
            dto.EventId,
            dto.Ordinal,
            valueType,
            value);
    }

    private static (string ValueType, string? Value) FormatCustomPropertyValue(
        EventCustomPropertyValueDto dto,
        ICollection<string> truncatedFields)
    {
        if (dto.TextValue is not null)
        {
            return ("Text", TrimToNull(dto.TextValue, MaxShortTextLength, truncatedFields, nameof(dto.TextValue)));
        }

        if (dto.NumberValue.HasValue)
        {
            return ("Number", dto.NumberValue.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (dto.BooleanValue.HasValue)
        {
            return ("Boolean", dto.BooleanValue.Value ? "true" : "false");
        }

        if (dto.DateTimeValue.HasValue)
        {
            return ("DateTime", dto.DateTimeValue.Value.ToString("O", CultureInfo.InvariantCulture));
        }

        if (dto.OptionId.HasValue)
        {
            return ("Option", dto.OptionId.Value.ToString("D"));
        }

        return ("Empty", null);
    }

    private static EventMcpRegistrationDescriptor MapRegistrationOrder(
        RegistrationOrderDto dto,
        ICollection<string> truncatedFields)
        => new(
            dto.Id,
            dto.EventId,
            dto.StatusId,
            TrimToNull(dto.StatusCode, MaxShortTextLength, truncatedFields, nameof(dto.StatusCode)),
            TrimToNull(dto.StatusName, MaxShortTextLength, truncatedFields, nameof(dto.StatusName)),
            TrimToNull(dto.CurrencyCode, MaxShortTextLength, truncatedFields, nameof(dto.CurrencyCode)),
            dto.TotalDueMinor,
            dto.ExpiresAt);

    private static EventMcpTeamMemberDescriptor MapTeamMember(
        EventTeamMemberDto dto,
        ICollection<string> truncatedFields)
        => new(
            dto.AssignmentId,
            TrimToEmpty(dto.UserEmail, MaxShortTextLength, truncatedFields, nameof(dto.UserEmail)),
            TrimToEmpty(dto.UserFullName, MaxShortTextLength, truncatedFields, nameof(dto.UserFullName)),
            TrimToEmpty(dto.RoleName, MaxShortTextLength, truncatedFields, nameof(dto.RoleName)),
            TrimToEmpty(dto.RoleMasterCode, MaxShortTextLength, truncatedFields, nameof(dto.RoleMasterCode)),
            dto.Status.ToString(),
            dto.StartsAtUtc,
            dto.ExpiresAtUtc,
            dto.IsEffective);

    private static EventMcpCurrentUserPermissionsDescriptor MapCurrentUserPermissions(
        CurrentUserEventPermissionsDto dto,
        ICollection<string> truncatedFields)
    {
        var roleCodes = dto.RoleCodes
            .WhereNotBlank()
            .Take(MaxPermissionCodes)
            .ToArray();
        var permissionCodes = dto.PermissionCodes
            .WhereNotBlank()
            .Take(MaxPermissionCodes)
            .ToArray();

        if (dto.RoleCodes.Count > roleCodes.Length)
        {
            truncatedFields.Add("CurrentUserPermissions.RoleCodes");
        }

        if (dto.PermissionCodes.Count > permissionCodes.Length)
        {
            truncatedFields.Add("CurrentUserPermissions.PermissionCodes");
        }

        return new EventMcpCurrentUserPermissionsDescriptor(
            dto.EventId,
            dto.HasAnyRole,
            dto.IsOwner,
            dto.IsManager,
            roleCodes,
            permissionCodes);
    }

    private static EventMcpRolePresetDescriptor MapRolePreset(
        EventRolePresetDto dto,
        ICollection<string> truncatedFields)
    {
        var permissionCodes = dto.PermissionCodes
            .WhereNotBlank()
            .Take(MaxPermissionCodes)
            .ToArray();
        if (dto.PermissionCodes.Count > permissionCodes.Length)
        {
            truncatedFields.Add($"AssignableRolePreset:{dto.MasterCode}:PermissionCodes");
        }

        return new EventMcpRolePresetDescriptor(
            dto.RoleId,
            TrimToEmpty(dto.MasterCode, MaxShortTextLength, truncatedFields, nameof(dto.MasterCode)),
            TrimToEmpty(dto.FullName, MaxShortTextLength, truncatedFields, nameof(dto.FullName)),
            TrimToNull(dto.Description, MaxShortTextLength, truncatedFields, nameof(dto.Description)),
            permissionCodes);
    }

    private static EventMcpTemplateDescriptor MapTemplate(
        EventTemplateListDto dto,
        ICollection<string> truncatedFields)
        => new(
            dto.Id,
            TrimToEmpty(dto.TemplateKey, MaxShortTextLength, truncatedFields, nameof(dto.TemplateKey)),
            TrimToEmpty(dto.DisplayName, MaxShortTextLength, truncatedFields, nameof(dto.DisplayName)),
            TrimToNull(dto.Description, MaxShortTextLength, truncatedFields, nameof(dto.Description)),
            dto.EventTypeId,
            dto.Version,
            dto.IsPublished,
            dto.IsActive,
            dto.SortOrder,
            dto.DefinitionCount);

    private static EventMcpSessionTemplateDescriptor MapSessionTemplate(EventSessionTemplateListDto dto)
    {
        var truncatedFields = new List<string>();
        return new EventMcpSessionTemplateDescriptor(
            dto.Id,
            dto.EventTemplateId,
            TrimToEmpty(dto.SessionTemplateKey, MaxShortTextLength, truncatedFields, nameof(dto.SessionTemplateKey)),
            TrimToEmpty(dto.DisplayName, MaxShortTextLength, truncatedFields, nameof(dto.DisplayName)),
            TrimToNull(dto.Description, MaxShortTextLength, truncatedFields, nameof(dto.Description)),
            dto.Version,
            dto.IsPublished,
            dto.IsActive,
            dto.SortOrder,
            dto.DefinitionCount,
            truncatedFields);
    }

    private static EventMcpTemplateDiffDescriptor MapTemplateDiff(EventTemplateDiffDto dto)
        => new(
            dto.TargetTemplateVersion,
            dto.BaseProvenanceVersion,
            CountTemplateDiffChanges(dto),
            dto.AddedDefinitions.Select(definition => CompositeKey(definition.Namespace, definition.Key)).Take(MaxSyncKeys).ToArray(),
            dto.ModifiedDefinitions.Select(definition => CompositeKey(definition.Namespace, definition.Key)).Take(MaxSyncKeys).ToArray(),
            dto.RetiredDefinitions.Select(definition => CompositeKey(definition.Namespace, definition.Key)).Take(MaxSyncKeys).ToArray(),
            dto.AddedOptions.Select(option => CompositeKey(option.Namespace, option.Key)).Take(MaxSyncKeys).ToArray(),
            dto.ModifiedOptions.Select(option => CompositeKey(option.Namespace, option.Key)).Take(MaxSyncKeys).ToArray(),
            dto.RetiredOptions.Select(option => CompositeKey(option.Namespace, option.Key)).Take(MaxSyncKeys).ToArray(),
            dto.UntouchedLocalDefinitions.Select(definition => CompositeKey(definition.Namespace, definition.Key)).Take(MaxSyncKeys).ToArray(),
            BuildTemplateDiffTruncatedFields(
                dto.AddedDefinitions.Count,
                dto.ModifiedDefinitions.Count,
                dto.RetiredDefinitions.Count,
                dto.AddedOptions.Count,
                dto.ModifiedOptions.Count,
                dto.RetiredOptions.Count,
                dto.UntouchedLocalDefinitions.Count));

    private static EventMcpTemplateDiffDescriptor MapTemplateDiff(EventSessionTemplateDiffDto dto)
        => new(
            dto.TargetTemplateVersion,
            dto.BaseProvenanceVersion,
            CountTemplateDiffChanges(dto),
            dto.AddedDefinitions.Select(definition => CompositeKey(definition.Namespace, definition.Key)).Take(MaxSyncKeys).ToArray(),
            dto.ModifiedDefinitions.Select(definition => CompositeKey(definition.Namespace, definition.Key)).Take(MaxSyncKeys).ToArray(),
            dto.RetiredDefinitions.Select(definition => CompositeKey(definition.Namespace, definition.Key)).Take(MaxSyncKeys).ToArray(),
            dto.AddedOptions.Select(option => CompositeKey(option.Namespace, option.Key)).Take(MaxSyncKeys).ToArray(),
            dto.ModifiedOptions.Select(option => CompositeKey(option.Namespace, option.Key)).Take(MaxSyncKeys).ToArray(),
            dto.RetiredOptions.Select(option => CompositeKey(option.Namespace, option.Key)).Take(MaxSyncKeys).ToArray(),
            dto.UntouchedLocalDefinitions.Select(definition => CompositeKey(definition.Namespace, definition.Key)).Take(MaxSyncKeys).ToArray(),
            BuildTemplateDiffTruncatedFields(
                dto.AddedDefinitions.Count,
                dto.ModifiedDefinitions.Count,
                dto.RetiredDefinitions.Count,
                dto.AddedOptions.Count,
                dto.ModifiedOptions.Count,
                dto.RetiredOptions.Count,
                dto.UntouchedLocalDefinitions.Count));

    private static EventMcpTemplateSyncHistoryPageDescriptor MapEventTemplateSyncHistory(
        Explore.Application.Responses.PaginatedResult<EventTemplateSyncHistoryItemDto> page,
        bool pageSizeWasClamped,
        ICollection<string> truncatedFields)
        => new(
            page.PageNumber,
            page.PageSize,
            pageSizeWasClamped,
            page.TotalCount,
            page.TotalPages,
            page.HasPreviousPage,
            page.HasNextPage,
            page.Items.Select(item => MapTemplateSyncHistoryItem(
                item.BaseProvenanceVersion,
                item.TargetTemplateVersion,
                item.Applied,
                item.Skipped,
                item.Conflicts.Select(conflict => (conflict.Key, conflict.Reason)).ToArray(),
                item.SyncedAt,
                truncatedFields)).ToArray());

    private static EventMcpTemplateSyncHistoryPageDescriptor MapEventSessionTemplateSyncHistory(
        Explore.Application.Responses.PaginatedResult<EventSessionTemplateSyncHistoryItemDto> page,
        bool pageSizeWasClamped,
        ICollection<string> truncatedFields)
        => new(
            page.PageNumber,
            page.PageSize,
            pageSizeWasClamped,
            page.TotalCount,
            page.TotalPages,
            page.HasPreviousPage,
            page.HasNextPage,
            page.Items.Select(item => MapTemplateSyncHistoryItem(
                item.BaseProvenanceVersion,
                item.TargetTemplateVersion,
                item.Applied,
                item.Skipped,
                item.Conflicts.Select(conflict => (conflict.Key, conflict.Reason)).ToArray(),
                item.SyncedAt,
                truncatedFields)).ToArray());

    private static EventMcpTemplateSyncHistoryItemDescriptor MapTemplateSyncHistoryItem(
        int baseProvenanceVersion,
        int targetTemplateVersion,
        IReadOnlyList<string> applied,
        IReadOnlyList<string> skipped,
        IReadOnlyList<(string Key, string Reason)> conflicts,
        DateTimeOffset syncedAt,
        ICollection<string> truncatedFields)
    {
        var appliedKeys = applied.WhereNotBlank().Take(MaxSyncKeys).ToArray();
        var skippedKeys = skipped.WhereNotBlank().Take(MaxSyncKeys).ToArray();
        var conflictItems = conflicts
            .Take(MaxSyncKeys)
            .Select(conflict => new EventMcpTemplateSyncConflictDescriptor(
                TrimToEmpty(conflict.Key, MaxShortTextLength, truncatedFields, "SyncConflict.Key"),
                TrimToEmpty(conflict.Reason, MaxShortTextLength, truncatedFields, "SyncConflict.Reason")))
            .ToArray();

        if (applied.Count > appliedKeys.Length)
        {
            truncatedFields.Add("SyncHistory.Applied");
        }

        if (skipped.Count > skippedKeys.Length)
        {
            truncatedFields.Add("SyncHistory.Skipped");
        }

        if (conflicts.Count > conflictItems.Length)
        {
            truncatedFields.Add("SyncHistory.Conflicts");
        }

        return new EventMcpTemplateSyncHistoryItemDescriptor(
            baseProvenanceVersion,
            targetTemplateVersion,
            applied.Count,
            appliedKeys,
            skipped.Count,
            skippedKeys,
            conflicts.Count,
            conflictItems,
            syncedAt);
    }

    private static EventMcpSummaryDescriptor MapSummary(EventListDto dto)
        => new(
            dto.Id,
            dto.Title,
            TrimToNull(dto.Subtitle, MaxShortTextLength),
            TrimToNull(dto.Description, MaxShortTextLength),
            dto.Slug,
            dto.EventTypeFullName,
            dto.ActorDisplayName,
            dto.EventStatusFullName,
            dto.VisibilityTypeFullName,
            dto.EventFormatFullName,
            dto.FirstSessionDate,
            dto.LastSessionDate,
            dto.Timezone,
            dto.SessionCount,
            dto.ParticipationConfiguration?.ParticipationHandlingModeCode,
            dto.ParticipationConfiguration?.ParticipationHandlingModeName,
            dto.ParticipationConfiguration?.AdvanceRegistrationObligationCode,
            dto.ParticipationConfiguration?.AdvanceRegistrationObligationName,
            dto.ParticipationConfiguration?.IdentityAccessModeCode,
            dto.ParticipationConfiguration?.IdentityAccessModeName,
            dto.ParticipationConfiguration?.GuestRecoveryPolicy);

    private static EventMcpDetailDescriptor MapDetail(EventDto dto)
    {
        var truncatedFields = new List<string>();

        return new EventMcpDetailDescriptor(
            dto.Id,
            dto.Title,
            TrimToNull(dto.Subtitle, MaxShortTextLength, truncatedFields, nameof(dto.Subtitle)),
            TrimToNull(dto.Description, MaxShortTextLength, truncatedFields, nameof(dto.Description)),
            TrimToNull(dto.Content, MaxLongTextLength, truncatedFields, nameof(dto.Content)),
            dto.Slug,
            dto.EventTypeFullName,
            dto.ActorDisplayName,
            dto.EventStatusFullName,
            dto.VisibilityTypeFullName,
            dto.EventFormatFullName,
            dto.FirstSessionDate,
            dto.LastSessionDate,
            dto.Timezone,
            dto.SessionCount,
            dto.ParticipationConfiguration?.ParticipationHandlingModeCode,
            dto.ParticipationConfiguration?.ParticipationHandlingModeName,
            dto.ParticipationConfiguration?.AdvanceRegistrationObligationCode,
            dto.ParticipationConfiguration?.AdvanceRegistrationObligationName,
            dto.ParticipationConfiguration?.IdentityAccessModeCode,
            dto.ParticipationConfiguration?.IdentityAccessModeName,
            dto.ParticipationConfiguration?.GuestRecoveryPolicy,
            dto.Categories.Select(category => category.FullName).WhereNotBlank().ToArray(),
            dto.Tags.Select(tag => tag.FullName).WhereNotBlank().ToArray(),
            dto.AvailableAspects.WhereNotBlank().ToArray(),
            truncatedFields);
    }

    private static EventMcpProgramSummaryDescriptor MapProgram(EventProgramSummaryDto dto)
    {
        var truncatedFields = new List<string>();
        var remainingProgramItems = MaxPublicProgramItems;
        var programItemsWereTruncated = false;
        var warningCount = dto.ReadinessWarnings.Count;
        var readinessWarnings = dto.ReadinessWarnings
            .Take(MaxReadinessWarnings)
            .Select(warning => MapWarning(warning, truncatedFields))
            .ToArray();

        var sections = new List<EventMcpProgramSectionDescriptor>();
        foreach (var section in dto.Sections.Take(MaxPublicProgramSections))
        {
            sections.Add(MapProgramSection(section, truncatedFields, ref remainingProgramItems, ref programItemsWereTruncated));
        }

        if (dto.Sections.Count > sections.Count)
        {
            programItemsWereTruncated = true;
            truncatedFields.Add("Program.Sections");
        }

        if (programItemsWereTruncated)
        {
            truncatedFields.Add("Program.Items");
        }

        if (warningCount > MaxReadinessWarnings)
        {
            truncatedFields.Add("ReadinessWarnings");
        }

        return new EventMcpProgramSummaryDescriptor(
            dto.EventId,
            TrimToEmpty(dto.EventTitle, MaxShortTextLength, truncatedFields, nameof(dto.EventTitle)),
            TrimToNull(dto.TimeZoneId, MaxShortTextLength, truncatedFields, nameof(dto.TimeZoneId)),
            dto.Sections.Count,
            CountProgramItems(dto),
            warningCount,
            programItemsWereTruncated,
            warningCount > MaxReadinessWarnings,
            sections,
            readinessWarnings,
            truncatedFields);
    }

    private static EventMcpProgramSectionDescriptor MapProgramSection(
        EventProgramSectionDto dto,
        ICollection<string> truncatedFields,
        ref int remainingProgramItems,
        ref bool programItemsWereTruncated)
    {
        var sessionGroups = new List<EventMcpProgramSessionGroupDescriptor>();
        foreach (var group in dto.SessionGroups.Take(MaxPublicProgramSessionGroups))
        {
            sessionGroups.Add(MapProgramSessionGroup(group, truncatedFields, ref remainingProgramItems, ref programItemsWereTruncated));
        }

        if (dto.SessionGroups.Count > sessionGroups.Count)
        {
            programItemsWereTruncated = true;
            truncatedFields.Add("ProgramSection.SessionGroups");
        }

        return new EventMcpProgramSectionDescriptor(
            TrimToEmpty(dto.SectionKey, MaxShortTextLength, truncatedFields, nameof(dto.SectionKey)),
            TrimToEmpty(dto.Title, MaxShortTextLength, truncatedFields, nameof(dto.Title)),
            dto.SortOrder,
            sessionGroups);
    }

    private static EventMcpProgramSessionGroupDescriptor MapProgramSessionGroup(
        EventProgramSessionGroupSectionDto dto,
        ICollection<string> truncatedFields,
        ref int remainingProgramItems,
        ref bool programItemsWereTruncated)
    {
        var days = new List<EventMcpProgramDayDescriptor>();
        foreach (var day in dto.Days.Take(MaxPublicProgramDays))
        {
            days.Add(MapProgramDay(day, truncatedFields, ref remainingProgramItems, ref programItemsWereTruncated));
        }

        if (dto.Days.Count > days.Count)
        {
            programItemsWereTruncated = true;
            truncatedFields.Add("ProgramSessionGroup.Days");
        }

        return new EventMcpProgramSessionGroupDescriptor(
            dto.SessionGroupId,
            TrimToEmpty(dto.Title, MaxShortTextLength, truncatedFields, nameof(dto.Title)),
            dto.SortOrder,
            TrimToNull(dto.Color, MaxShortTextLength, truncatedFields, nameof(dto.Color)),
            MapPublicLocation(dto.EventLocation, truncatedFields),
            days);
    }

    private static EventMcpProgramDayDescriptor MapProgramDay(
        EventProgramDayGroupDto dto,
        ICollection<string> truncatedFields,
        ref int remainingProgramItems,
        ref bool programItemsWereTruncated)
    {
        if (!dto.LocalDate.HasValue)
        {
            throw new InvalidOperationException("Public program days must have a local date.");
        }

        var items = new List<EventMcpProgramItemDescriptor>();

        foreach (var item in dto.Items)
        {
            if (remainingProgramItems <= 0)
            {
                programItemsWereTruncated = true;
                break;
            }

            items.Add(MapProgramItem(item, truncatedFields));
            remainingProgramItems--;
        }

        return new EventMcpProgramDayDescriptor(
            dto.LocalDate.Value,
            TrimToEmpty(dto.DisplayLabel, MaxShortTextLength, truncatedFields, nameof(dto.DisplayLabel)),
            items);
    }

    private static EventMcpProgramItemDescriptor MapProgramItem(
        EventProgramItemDto dto,
        ICollection<string> truncatedFields)
    {
        if (!dto.StartsAtUtc.HasValue || !dto.EndsAtUtc.HasValue || !dto.LocalDate.HasValue ||
            !dto.LocalStartTime.HasValue || !dto.LocalEndTime.HasValue)
        {
            throw new InvalidOperationException("Public program items must be fully scheduled.");
        }

        var warningCount = dto.ReadinessWarnings.Count;
        var warnings = dto.ReadinessWarnings
            .Take(MaxReadinessWarnings)
            .Select(warning => MapWarning(warning, truncatedFields))
            .ToArray();

        if (warningCount > MaxReadinessWarnings)
        {
            truncatedFields.Add($"ProgramItem:{dto.SessionId}:ReadinessWarnings");
        }

        return new EventMcpProgramItemDescriptor(
            dto.SessionId,
            TrimToEmpty(dto.Title, MaxShortTextLength, truncatedFields, nameof(dto.Title)),
            dto.EventSessionKindId,
            TrimToNull(dto.EventSessionKindName, MaxShortTextLength, truncatedFields, nameof(dto.EventSessionKindName)),
            TrimToNull(dto.EventSessionKindMasterCode, MaxShortTextLength, truncatedFields, nameof(dto.EventSessionKindMasterCode)),
            dto.StartsAtUtc.Value,
            dto.EndsAtUtc.Value,
            dto.LocalDate.Value,
            dto.LocalStartTime.Value,
            dto.LocalEndTime.Value,
            dto.SortOrder,
            dto.SessionGroupId,
            MapPublicLocation(dto.EventLocation, truncatedFields),
            dto.Capacity,
            TrimToNull(dto.RegistrationModeName, MaxShortTextLength, truncatedFields, nameof(dto.RegistrationModeName)),
            warnings);
    }

    private static EventMcpReadinessWarningDescriptor MapWarning(
        EventProgramReadinessWarningDto dto,
        ICollection<string> truncatedFields)
        => new(
            TrimToEmpty(dto.Path, MaxShortTextLength, truncatedFields, nameof(dto.Path)),
            TrimToEmpty(dto.Severity, MaxShortTextLength, truncatedFields, nameof(dto.Severity)),
            TrimToEmpty(dto.Message, MaxShortTextLength, truncatedFields, nameof(dto.Message)));

    private static EventMcpSessionListResultDescriptor MapSessions(Guid eventId, IReadOnlyCollection<EventSessionListDto> sessions)
    {
        var returnedSessions = sessions
            .Take(MaxPublicSessions)
            .Select(session => MapSession(session, includePublicLocation: true))
            .ToArray();

        return new EventMcpSessionListResultDescriptor(
            Found: true,
            EventId: eventId,
            FailureCode: null,
            TotalCount: sessions.Count,
            ReturnedCount: returnedSessions.Length,
            SessionsWereTruncated: sessions.Count > MaxPublicSessions,
            Sessions: returnedSessions);
    }

    private static EventMcpSessionSummaryDescriptor MapSession(
        EventSessionListDto dto,
        bool includePublicLocation = false)
    {
        var truncatedFields = new List<string>();

        return new EventMcpSessionSummaryDescriptor(
            dto.Id,
            dto.EventId,
            TrimToEmpty(dto.EventTitle, MaxShortTextLength, truncatedFields, nameof(dto.EventTitle)),
            TrimToNull(dto.Title, MaxShortTextLength, truncatedFields, nameof(dto.Title)),
            TrimToNull(dto.Slug, MaxShortTextLength, truncatedFields, nameof(dto.Slug)),
            dto.EventSessionKindId,
            TrimToNull(dto.EventSessionKindFullName, MaxShortTextLength, truncatedFields, nameof(dto.EventSessionKindFullName)),
            TrimToNull(dto.EventSessionKindMasterCode, MaxShortTextLength, truncatedFields, nameof(dto.EventSessionKindMasterCode)),
            dto.StartTime,
            dto.EndTime,
            dto.LocalStartDate,
            dto.LocalStartTime,
            dto.LocalEndTime,
            dto.SortOrder,
            dto.MaxAudienceAttendees,
            TrimToNull(dto.RegistrationModeFullName, MaxShortTextLength, truncatedFields, nameof(dto.RegistrationModeFullName)),
            dto.SessionGroups
                .OrderByDescending(group => group.IsPrimary)
                .ThenBy(group => group.SortOrder)
                .Select(group => group.Name)
                .WhereNotBlank()
                .Take(10)
                .ToArray(),
            includePublicLocation ? MapPublicLocation(dto.EventLocation, truncatedFields) : null,
            truncatedFields);
    }

    private static EventMcpLocationDescriptor? MapPublicLocation(
        EventLocationPublicDto? dto,
        ICollection<string> truncatedFields)
    {
        if (dto is null)
        {
            return null;
        }

        EventLocationPublicFieldsDto? fields = dto.Fields;
        return new EventMcpLocationDescriptor(
            dto.EventLocationId,
            dto.State.ToString(),
            TrimToNull(fields?.Country, MaxShortTextLength, truncatedFields, nameof(EventLocationPublicFieldsDto.Country)),
            TrimToNull(fields?.Timezone, MaxShortTextLength, truncatedFields, nameof(EventLocationPublicFieldsDto.Timezone)),
            TrimToNull(fields?.City, MaxShortTextLength, truncatedFields, nameof(EventLocationPublicFieldsDto.City)),
            TrimToNull(fields?.VenueName, MaxShortTextLength, truncatedFields, nameof(EventLocationPublicFieldsDto.VenueName)),
            TrimToNull(fields?.RoomName, MaxShortTextLength, truncatedFields, nameof(EventLocationPublicFieldsDto.RoomName)),
            TrimToNull(fields?.StreetAddress, MaxShortTextLength, truncatedFields, nameof(EventLocationPublicFieldsDto.StreetAddress)),
            TrimToNull(fields?.Postcode, MaxShortTextLength, truncatedFields, nameof(EventLocationPublicFieldsDto.Postcode)),
            fields?.Latitude,
            fields?.Longitude,
            TrimToNull(fields?.FormattedAddress, MaxLongTextLength, truncatedFields, nameof(EventLocationPublicFieldsDto.FormattedAddress)),
            TrimToNull(fields?.MapUrl, MaxLongTextLength, truncatedFields, nameof(EventLocationPublicFieldsDto.MapUrl)),
            TrimToNull(fields?.Geohash, MaxShortTextLength, truncatedFields, nameof(EventLocationPublicFieldsDto.Geohash)));
    }

    private void EnforcePublicSessionLocationDisclosureCeiling(
        IEnumerable<EventSessionListDto> sessions)
    {
        var requests = sessions
            .Select(session => session.EventLocation)
            .Where(location => location is not null)
            .Cast<EventLocationPublicDto>()
            .Distinct()
            .Select(CreatePublicEventLocationSanitizationInput)
            .ToArray();

        EnforceSuccessfulPublicLocationSanitization(requests, aiContextGateway.SanitizeMany(requests));
    }

    private void EnforcePublicProgramLocationDisclosureCeiling(EventProgramSummaryDto program)
    {
        var locations = new List<EventLocationPublicDto>();
        foreach (var group in program.Sections.SelectMany(section => section.SessionGroups))
        {
            if (group.EventLocation is not null)
            {
                locations.Add(group.EventLocation);
            }

            locations.AddRange(group.Days
                .SelectMany(day => day.Items)
                .Where(item => item.EventLocation is not null)
                .Select(item => item.EventLocation!));
        }

        AiContextSanitizationInput[] requests = locations
            .Distinct()
            .Select(CreatePublicEventLocationSanitizationInput)
            .ToArray();
        EnforceSuccessfulPublicLocationSanitization(
            requests,
            aiContextGateway.SanitizeMany(requests));
    }

    private void EnforceManagedProgramLocationDisclosureCeiling(
        IReadOnlyList<EventSessionListDto> sessions,
        IReadOnlyList<EventSessionGroupListDto> sessionGroups)
    {
        var requests = sessions
            .Select(session => CreateZeroDisclosureLocationSanitizationInput(
                session.LocationId,
                session.LocationFullName,
                session.LocationCity,
                session.RoomId,
                session.RoomName))
            .Concat(sessionGroups.Select(group => CreateZeroDisclosureLocationSanitizationInput(
                group.LocationId,
                group.LocationName,
                roomId: group.RoomId,
                roomName: group.RoomName)))
            .ToArray();

        EnforceSuccessfulZeroLocationSanitization(requests, aiContextGateway.SanitizeMany(requests));
    }

    private static AiContextSanitizationInput CreatePublicEventLocationSanitizationInput(
        EventLocationPublicDto location)
    {
        EventLocationPublicFieldsDto? fields = location.Fields;
        return new AiContextSanitizationInput(
            EntityName: nameof(EventLocationPublicDto),
            Fields: new Dictionary<string, object?>
            {
                [nameof(EventLocationPublicDto.EventLocationId)] = location.EventLocationId,
                [nameof(EventLocationPublicDto.State)] = location.State,
                [nameof(EventLocationPublicFieldsDto.Country)] = fields?.Country,
                [nameof(EventLocationPublicFieldsDto.Timezone)] = fields?.Timezone,
                [nameof(EventLocationPublicFieldsDto.City)] = fields?.City,
                [nameof(EventLocationPublicFieldsDto.VenueName)] = fields?.VenueName,
                [nameof(EventLocationPublicFieldsDto.RoomName)] = fields?.RoomName,
                [nameof(EventLocationPublicFieldsDto.StreetAddress)] = fields?.StreetAddress,
                [nameof(EventLocationPublicFieldsDto.Postcode)] = fields?.Postcode,
                [nameof(EventLocationPublicFieldsDto.Latitude)] = fields?.Latitude,
                [nameof(EventLocationPublicFieldsDto.Longitude)] = fields?.Longitude,
                [nameof(EventLocationPublicFieldsDto.FormattedAddress)] = fields?.FormattedAddress,
                [nameof(EventLocationPublicFieldsDto.MapUrl)] = fields?.MapUrl,
                [nameof(EventLocationPublicFieldsDto.Geohash)] = fields?.Geohash
            },
            ProviderTrustTier: AiProviderTrustTierEnum.Unknown,
            ViewerScope: AiViewerScopeEnum.Public,
            GrantedFieldKeys: new HashSet<string>(),
            PiiDisclosureEnabled: false,
            MaxSensitivity: AiContextSensitivityEnum.Public);
    }

    private static AiContextSanitizationInput CreateZeroDisclosureLocationSanitizationInput(
        Guid? locationId = null,
        string? locationName = null,
        string? locationCity = null,
        Guid? roomId = null,
        string? roomName = null)
        => new(
            EntityName: "LocationPii",
            Fields: new Dictionary<string, object?>
            {
                ["LocationId"] = locationId,
                ["LocationName"] = locationName,
                ["LocationCity"] = locationCity,
                ["RoomId"] = roomId,
                ["RoomName"] = roomName
            },
            ProviderTrustTier: AiProviderTrustTierEnum.Unknown,
            ViewerScope: AiViewerScopeEnum.Public,
            GrantedFieldKeys: new HashSet<string>(),
            PiiDisclosureEnabled: false,
            MaxSensitivity: AiContextSensitivityEnum.Public);

    private static void EnforceSuccessfulPublicLocationSanitization(
        IReadOnlyList<AiContextSanitizationInput> requests,
        IReadOnlyList<AiContextSanitizedEnvelope> envelopes)
    {
        if (requests.Count != envelopes.Count)
        {
            throw new InvalidOperationException("AI context disclosure failed for public EventLocation data.");
        }

        for (var index = 0; index < requests.Count; index++)
        {
            AiContextSanitizationInput request = requests[index];
            AiContextSanitizedEnvelope envelope = envelopes[index];
            if (!string.Equals(request.EntityName, envelope.EntityName, StringComparison.Ordinal)
                || !envelope.Succeeded
                || envelope.RedactedFieldNames.Count != 0
                || envelope.DeniedFieldNames.Count != 0
                || envelope.DisclosedFields.Count != request.Fields.Count
                || request.Fields.Any(field => !IsExactAllowedField(envelope.DisclosedFields, field)))
            {
                throw new InvalidOperationException("AI context disclosure failed for public EventLocation data.");
            }
        }
    }

    private static bool IsExactAllowedField(
        IReadOnlyList<AiContextDisclosedField> disclosedFields,
        KeyValuePair<string, object?> expected)
    {
        AiContextDisclosedField[] matches = disclosedFields
            .Where(field => string.Equals(field.Name, expected.Key, StringComparison.Ordinal))
            .ToArray();
        return matches.Length == 1
            && matches[0].AppliedRule == AiContextDisclosureRuleEnum.Allow
            && Equals(matches[0].Value, expected.Value);
    }

    private static void EnforceSuccessfulZeroLocationSanitization(
        IReadOnlyList<AiContextSanitizationInput> requests,
        IReadOnlyList<AiContextSanitizedEnvelope> envelopes)
    {
        if (requests.Count != envelopes.Count)
        {
            throw new InvalidOperationException("AI context disclosure failed for public location data.");
        }

        for (var index = 0; index < requests.Count; index++)
        {
            var envelope = envelopes[index];
            if (!string.Equals(requests[index].EntityName, envelope.EntityName, StringComparison.Ordinal)
                || !envelope.Succeeded
                || envelope.DisclosedFields.Any(field => field.Value is not null))
            {
                throw new InvalidOperationException("AI context disclosure failed for public location data.");
            }
        }
    }

    private async Task<EventDto?> GetPublicEventOrNullAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var eventDto = await mediator.Send(new GetEventDetailsRequest { Id = eventId }, cancellationToken);
        return IsPublishedPublicEvent(eventDto) ? eventDto : null;
    }

    private static bool IsPublishedPublicEvent(EventDto? eventDto)
        => eventDto is not null
            && eventDto.EventStatusId == (int)EventStatusEnum.Published
            && eventDto.VisibilityTypeId == (int)VisibilityTypeEnum.Public;

    private static int CountProgramItems(EventProgramSummaryDto dto)
        => dto.Sections.Sum(section => section.SessionGroups.Sum(group => group.Days.Sum(day => day.Items.Count)));

    private static string? NormalizeSearchTerm(string? searchTerm)
        => TrimToNull(searchTerm, MaxSearchTermLength);

    private static string? NormalizeSortBy(string? sortBy)
    {
        var normalized = TrimToNull(sortBy, MaxShortTextLength)?.ToLowerInvariant();
        return normalized switch
        {
            "date" => "date",
            "title" => "title",
            "views" => "views",
            "createdat" => "createdAt",
            _ => null
        };
    }

    private static (int PageNumber, int PageSize, bool PageSizeWasClamped) NormalizeManagementPage(
        int pageNumber,
        int pageSize,
        int maxPageSize)
    {
        var normalizedPageNumber = Math.Max(1, pageNumber);
        var normalizedPageSize = pageSize <= 0
            ? DefaultManagementPageSize
            : Math.Clamp(pageSize, 1, maxPageSize);
        return (normalizedPageNumber, normalizedPageSize, normalizedPageSize != pageSize);
    }

    private static int CountTemplateDiffChanges(EventTemplateDiffDto dto)
        => dto.AddedDefinitions.Count
           + dto.ModifiedDefinitions.Count
           + dto.RetiredDefinitions.Count
           + dto.AddedOptions.Count
           + dto.ModifiedOptions.Count
           + dto.RetiredOptions.Count;

    private static int CountTemplateDiffChanges(EventSessionTemplateDiffDto dto)
        => dto.AddedDefinitions.Count
           + dto.ModifiedDefinitions.Count
           + dto.RetiredDefinitions.Count
           + dto.AddedOptions.Count
           + dto.ModifiedOptions.Count
           + dto.RetiredOptions.Count;

    private static IReadOnlyList<string> BuildTemplateDiffTruncatedFields(
        int addedDefinitions,
        int modifiedDefinitions,
        int retiredDefinitions,
        int addedOptions,
        int modifiedOptions,
        int retiredOptions,
        int untouchedLocalDefinitions)
    {
        var truncatedFields = new List<string>();
        AddIfTruncated(addedDefinitions, "Diff.AddedDefinitions", truncatedFields);
        AddIfTruncated(modifiedDefinitions, "Diff.ModifiedDefinitions", truncatedFields);
        AddIfTruncated(retiredDefinitions, "Diff.RetiredDefinitions", truncatedFields);
        AddIfTruncated(addedOptions, "Diff.AddedOptions", truncatedFields);
        AddIfTruncated(modifiedOptions, "Diff.ModifiedOptions", truncatedFields);
        AddIfTruncated(retiredOptions, "Diff.RetiredOptions", truncatedFields);
        AddIfTruncated(untouchedLocalDefinitions, "Diff.UntouchedLocalDefinitions", truncatedFields);
        return truncatedFields;
    }

    private static void AddIfTruncated(int count, string fieldName, ICollection<string> truncatedFields)
    {
        if (count > MaxSyncKeys)
        {
            truncatedFields.Add(fieldName);
        }
    }

    private static string CompositeKey(string? @namespace, string? key)
    {
        var normalizedNamespace = TrimToNull(@namespace, MaxShortTextLength);
        var normalizedKey = TrimToNull(key, MaxShortTextLength);
        return normalizedNamespace is null
            ? normalizedKey ?? string.Empty
            : $"{normalizedNamespace}.{normalizedKey ?? string.Empty}";
    }

    private static string? TrimToNull(string? value, int maxLength)
        => TrimToNull(value, maxLength, truncatedFields: null, fieldName: null);

    private static string TrimToEmpty(
        string? value,
        int maxLength,
        ICollection<string>? truncatedFields,
        string? fieldName)
        => TrimToNull(value, maxLength, truncatedFields, fieldName) ?? string.Empty;

    private static string? TrimToNull(
        string? value,
        int maxLength,
        ICollection<string>? truncatedFields,
        string? fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        if (!string.IsNullOrWhiteSpace(fieldName))
        {
            truncatedFields?.Add(fieldName);
        }

        return trimmed[..maxLength];
    }

    private sealed record EventMcpManagementReadGate(
        bool Found,
        bool IsAvailable,
        EventDto? Event,
        IReadOnlyDictionary<string, HalLink> Links)
    {
        public static EventMcpManagementReadGate NotFound()
            => new(false, false, null, new Dictionary<string, HalLink>(StringComparer.Ordinal));

        public static EventMcpManagementReadGate Unavailable(
            EventDto eventDto,
            IReadOnlyDictionary<string, HalLink> links)
            => new(true, false, eventDto, links);

        public static EventMcpManagementReadGate ForAvailable(
            EventDto eventDto,
            IReadOnlyDictionary<string, HalLink> links)
            => new(true, true, eventDto, links);
    }

    private sealed record EventMcpTemplateDiffRead(
        bool IsAvailable,
        string? FailureCode,
        EventMcpTemplateDiffDescriptor? Diff)
    {
        public static EventMcpTemplateDiffRead NotRequested()
            => new(false, "not_requested", null);

        public static EventMcpTemplateDiffRead Unavailable(string failureCode)
            => new(false, failureCode, null);

        public static EventMcpTemplateDiffRead WithDiff(EventMcpTemplateDiffDescriptor diff)
            => new(true, null, diff);
    }
}

file static class EventManagementMcpEnumerableExtensions
{
    public static IEnumerable<string> WhereNotBlank(this IEnumerable<string?> values)
        => values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim());
}
