// ABOUTME: Defines the explicit operator action that resumes durable refund campaign generation.
// ABOUTME: Uses event commercial authority and never invokes a payment provider in the HTTP request path.

using Explore.Application.Authorization;
using Explore.Application.DTOs.RegistrationOrders;
using MediatR;

namespace Explore.Application.Features.RegistrationOrders.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManagePaidEventCommerce)]
public sealed record ResumeRefundCampaignCommand(Guid EventId, Guid CampaignId)
    : IRequest<RefundCampaignDto?>, ISecureRequest
{
    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString("D");
    IAuthorizationFacts? ISecureRequest.AuthorizationFacts => new EventScopedAuthorizationFacts(Guid.Empty, EventId);
}
