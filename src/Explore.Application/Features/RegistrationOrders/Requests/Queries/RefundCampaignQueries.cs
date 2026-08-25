// ABOUTME: Defines event-authorized refund campaign reads for organizer and trust/safety operations.
// ABOUTME: Carries event lineage as typed authorization facts and exposes no buyer or provider identifiers.

using Explore.Application.Authorization;
using Explore.Application.DTOs.RegistrationOrders;
using MediatR;

namespace Explore.Application.Features.RegistrationOrders.Requests.Queries;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManagePaidEventCommerce)]
public sealed record GetRefundCampaignsQuery(Guid EventId)
    : IRequest<IReadOnlyList<RefundCampaignDto>>, ISecureRequest
{
    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString("D");
    IAuthorizationFacts? ISecureRequest.AuthorizationFacts => new EventScopedAuthorizationFacts(Guid.Empty, EventId);
}

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManagePaidEventCommerce)]
public sealed record GetRefundCampaignQuery(Guid EventId, Guid CampaignId)
    : IRequest<RefundCampaignDto?>, ISecureRequest
{
    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString("D");
    IAuthorizationFacts? ISecureRequest.AuthorizationFacts => new EventScopedAuthorizationFacts(Guid.Empty, EventId);
}
