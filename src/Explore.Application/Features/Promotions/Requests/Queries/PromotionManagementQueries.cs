// ABOUTME: Defines organizer promotion management queries for safe list and detail projections.
// ABOUTME: Uses paid-commerce authorization metadata without exposing internal authority identifiers in JSON.

using Explore.Application.Authorization;
using Explore.Application.Features.Promotions;
using MediatR;

namespace Explore.Application.Features.Promotions.Requests.Queries;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManagePaidEventCommerce)]
public sealed record ListPromotionManagementQuery(Guid EventId, Guid TicketCatalogVersionId)
    : IRequest<IReadOnlyList<PromotionManagementDto>>, ISecureRequest
{
    public string? ResourceId => EventId.ToString();

    public IDictionary<string, object>? ResourceAttributes => new Dictionary<string, object>
    {
        ["eventId"] = EventId.ToString()
    };
}

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManagePaidEventCommerce)]
public sealed record GetPromotionManagementQuery(Guid EventId, Guid PromotionDefinitionId)
    : IRequest<PromotionManagementDto?>, ISecureRequest
{
    public string? ResourceId => EventId.ToString();

    public IDictionary<string, object>? ResourceAttributes => new Dictionary<string, object>
    {
        ["eventId"] = EventId.ToString()
    };
}
