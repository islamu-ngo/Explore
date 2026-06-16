// ABOUTME: Requests a paged audit-backed history of prior event-session template sync executions for one session.
// ABOUTME: Authorizes history retrieval with the same custom-property template view resource metadata used by HAL links.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionTemplateSync;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionTemplateSync.Queries.GetEventSessionTemplateSyncHistory;

[AuthorizeResource(ResourceKinds.CustomPropertyTemplate, AuthorizationActions.CustomPropertyTemplates.View)]
public sealed record GetEventSessionTemplateSyncHistoryQuery(Guid EventSessionId, int PageNumber, int PageSize)
    : IRequest<PaginatedResult<EventSessionTemplateSyncHistoryItemDto>>, ISecureRequest
{
    string? ISecureRequest.ResourceId => EventSessionId.ToString();
}
