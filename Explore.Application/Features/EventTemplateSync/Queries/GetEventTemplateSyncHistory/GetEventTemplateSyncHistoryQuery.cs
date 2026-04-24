// ABOUTME: Requests a paged audit-backed history of prior event template sync executions for one event.
// ABOUTME: Keeps history retrieval read-only and isolated from controller concerns.

using Explore.Application.DTOs.EventTemplateSync;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTemplateSync.Queries.GetEventTemplateSyncHistory;

public sealed record GetEventTemplateSyncHistoryQuery(Guid EventId, int PageNumber, int PageSize)
    : IRequest<PaginatedResult<EventTemplateSyncHistoryItemDto>>;
