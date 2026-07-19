// ABOUTME: Requests a bounded source-aware public event page merged from local and governed ATProto projections.
// ABOUTME: Keeps the existing local event query contract unchanged for management, MCP, and internal consumers.

using Explore.Application.DTOs.PublicExperience;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Federation.Atproto.Requests.Queries;

public sealed record GetPublicEventDiscoveryRequest(GetEventListRequest Criteria)
    : IRequest<PaginatedResult<EventDiscoveryItemDto>>;

public sealed record GetAtprotoEventSourceQuery(Guid AtprotoRecordId) : IRequest<string?>;
