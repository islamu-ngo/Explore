// ABOUTME: Requests the public ticket-selection composition for one eligible event.
// ABOUTME: Carries only the route-owned event identifier into the query handler.

using Explore.Application.DTOs.RegistrationOrders;
using MediatR;

namespace Explore.Application.Features.RegistrationOrders.Requests.Queries;

public sealed record GetRegistrationCheckoutCompositionQuery(Guid EventId)
    : IRequest<RegistrationCheckoutCompositionDto?>;
