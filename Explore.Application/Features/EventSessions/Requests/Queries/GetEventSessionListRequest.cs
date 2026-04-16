// ABOUTME: MediatR query request for fetching a paginated session list.
// ABOUTME: Supports custom property projection filters gated behind tenant feature flag.
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Queries;

public class GetEventSessionListRequest : IRequest<PaginatedResult<EventSessionListDto>>
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    // ===== Custom property projection filters (Layer 3 — tenant-gated) =====

    public List<CustomPropertyFilterCriterion>? CustomPropertyFilters { get; set; }

    public string? CustomPropertySearchTerm { get; set; }
}
