// ABOUTME: Defines account- and capability-scoped registration payment start and safe retry commands.
// ABOUTME: Commands carry only order lineage and never accept provider URLs, amounts, or provider identifiers.

using Explore.Application.DTOs.RegistrationOrders;
using MediatR;

namespace Explore.Application.Features.RegistrationOrders.Requests.Commands;

public sealed record StartGuestRegistrationPaymentCommand(Guid EventId, Guid OrderId, string? CapabilityToken)
    : IRequest<RegistrationPaymentCommandResultDto>, IGuestRegistrationOrderAccessCommand;

public sealed record RetryGuestRegistrationPaymentCommand(Guid EventId, Guid OrderId, string? CapabilityToken)
    : IRequest<RegistrationPaymentCommandResultDto>, IGuestRegistrationOrderAccessCommand;

public sealed record StartAuthenticatedRegistrationPaymentCommand(Guid EventId, Guid OrderId)
    : IRequest<RegistrationPaymentCommandResultDto>, IAuthenticatedRegistrationOrderAccessCommand;

public sealed record RetryAuthenticatedRegistrationPaymentCommand(Guid EventId, Guid OrderId)
    : IRequest<RegistrationPaymentCommandResultDto>, IAuthenticatedRegistrationOrderAccessCommand;
