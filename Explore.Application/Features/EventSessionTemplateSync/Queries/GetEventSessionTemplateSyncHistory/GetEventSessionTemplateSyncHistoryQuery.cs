// ABOUTME: Requests a paged audit-backed history of prior event-session template sync executions for one session.
// ABOUTME: Keeps history retrieval read-only and isolated from controller concerns.

using Explore.Application.DTOs.EventSessionTemplateSync;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionTemplateSync.Queries.GetEventSessionTemplateSyncHistory;

public sealed record GetEventSessionTemplateSyncHistoryQuery(Guid EventSessionId, int PageNumber, int PageSize)
    : IRequest<PaginatedResult<EventSessionTemplateSyncHistoryItemDto>>;
