// ABOUTME: Updates organizer-authored commercial disclosures on the current ticket catalog draft.
// ABOUTME: Uses existing ticket management authorization and domain disclosure normalization.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTicketing.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManageTickets)]
public sealed class UpdateEventTicketCatalogCommercialDisclosuresCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventId { get; init; }
    public string? MerchantDisclosureText { get; init; }
    public string? RefundPolicyDisclosureText { get; init; }
    public string? SupportContactDisclosureText { get; init; }

    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString();

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object> { ["eventId"] = EventId.ToString() };
}
