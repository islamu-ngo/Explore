// ABOUTME: Defines purchaser, Studio, and BFF-only registration payment status and checkout-target queries.
// ABOUTME: Public status projections are sanitized while the checkout target remains separately access-guarded.

using Explore.Application.Authorization;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Features.RegistrationOrders.Requests.Commands;
using MediatR;

namespace Explore.Application.Features.RegistrationOrders.Requests.Queries;

public sealed record GetGuestRegistrationPaymentQuery(Guid EventId, Guid OrderId, string? CapabilityToken)
    : IRequest<RegistrationPaymentDto?>, IGuestRegistrationOrderAccessCommand;

public sealed record GetAuthenticatedRegistrationPaymentQuery(Guid EventId, Guid OrderId)
    : IRequest<RegistrationPaymentDto?>, IAuthenticatedRegistrationOrderAccessCommand;

public sealed record GetGuestRegistrationPaymentCheckoutTargetQuery(Guid EventId, Guid OrderId, string? CapabilityToken)
    : IRequest<RegistrationPaymentCheckoutTargetDto?>, IGuestRegistrationOrderAccessCommand;

public sealed record GetAuthenticatedRegistrationPaymentCheckoutTargetQuery(Guid EventId, Guid OrderId)
    : IRequest<RegistrationPaymentCheckoutTargetDto?>, IAuthenticatedRegistrationOrderAccessCommand;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManagePaidEventCommerce)]
public sealed record GetStudioRegistrationPaymentQuery(Guid EventId, Guid OrderId)
    : IRequest<RegistrationPaymentDto?>, ISecureRequest
{
    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString("D");
    IAuthorizationFacts? ISecureRequest.AuthorizationFacts => new EventScopedAuthorizationFacts(Guid.Empty, EventId);
}
