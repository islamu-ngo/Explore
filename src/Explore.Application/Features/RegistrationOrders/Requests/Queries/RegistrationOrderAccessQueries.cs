// ABOUTME: Defines account- and capability-scoped registration-order read contracts.
// ABOUTME: Requires a full order/event/capability tuple for anonymous order visibility.

using Explore.Application.DTOs.RegistrationOrders;
using MediatR;

namespace Explore.Application.Features.RegistrationOrders.Requests.Queries;

public sealed record GetGuestRegistrationOrderQuery(Guid EventId, Guid OrderId, string? CapabilityToken)
    : IRequest<GuestRegistrationOrderDto?>;

public sealed record GetCurrentRegistrationOrderQuery(Guid OrderId)
    : IRequest<RegistrationOrderDto?>;
