// ABOUTME: MediatR command for updating an agenda item.
// ABOUTME: Carries route-owned identity, grouped PATCH data, and server-bound authorization context.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventSessionAgendaItem, AuthorizationActions.Update)]
public class UpdateEventSessionAgendaItemCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventSessionAgendaItemId { get; set; }
    public required UpdateEventSessionAgendaItemDto AgendaItemDto { get; set; }
    public Guid EventSessionId { get; set; }
    public Guid EventId { get; set; }
    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => EventSessionAgendaItemId.ToString();
    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new EventScopedAuthorizationFacts(TenantId, EventId, EventSessionId);
}
