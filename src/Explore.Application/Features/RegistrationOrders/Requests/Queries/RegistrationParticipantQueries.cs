// ABOUTME: Defines order-scoped participant and ticket-assignment reads for handler and future API composition.
// ABOUTME: Returns application DTOs rather than persistence entities.

using Explore.Application.DTOs.RegistrationOrders;
using MediatR;

namespace Explore.Application.Features.RegistrationOrders.Requests.Queries;

public sealed record GetRegistrationOrderParticipantsQuery(Guid RegistrationOrderId)
    : IRequest<RegistrationOrderParticipantsDto?>;
