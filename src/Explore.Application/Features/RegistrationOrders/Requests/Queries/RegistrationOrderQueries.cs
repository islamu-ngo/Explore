// ABOUTME: Defines safe registration-order reads for lifecycle responses and future order surfaces.
// ABOUTME: Query contracts exclude purchaser PII, guest capability values, answers, and participant details.

using Explore.Application.DTOs.RegistrationOrders;
using MediatR;

namespace Explore.Application.Features.RegistrationOrders.Requests.Queries;

public sealed record GetRegistrationOrderQuery(Guid OrderId) : IRequest<RegistrationOrderDto?>;

public sealed record GetEventRegistrationOrdersQuery(Guid EventId) : IRequest<IReadOnlyList<RegistrationOrderDto>>;
