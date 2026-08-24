// ABOUTME: Defines account- and capability-scoped registration payment start and safe retry commands.
// ABOUTME: Commands carry only order lineage and never accept provider URLs, amounts, or provider identifiers.

using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.RegistrationOrders.Requests.Commands;

public interface IAuthenticatedRegistrationPaymentSecureRequest
    : IAuthenticatedRegistrationOrderAccessCommand, ISecureRequest
{
    string? ISecureRequest.ResourceId => OrderId == Guid.Empty ? null : OrderId.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new RegistrationOrderAuthorizationFacts(Guid.Empty, EventId, null);
}

public sealed record StartGuestRegistrationPaymentCommand(
    Guid EventId,
    Guid OrderId,
    string? CapabilityToken,
    PaidOrderAcceptanceAcknowledgementDto? Acceptance)
    : IRequest<RegistrationPaymentCommandResultDto>, IGuestRegistrationOrderAccessCommand;

public sealed record RetryGuestRegistrationPaymentCommand(Guid EventId, Guid OrderId, string? CapabilityToken)
    : IRequest<RegistrationPaymentCommandResultDto>, IGuestRegistrationOrderAccessCommand;

[AuthorizeResource(ResourceKinds.RegistrationOrder, AuthorizationActions.RegistrationOrders.Continue)]
public sealed record StartAuthenticatedRegistrationPaymentCommand(
    Guid EventId,
    Guid OrderId,
    PaidOrderAcceptanceAcknowledgementDto? Acceptance)
    : IRequest<RegistrationPaymentCommandResultDto>, IAuthenticatedRegistrationPaymentSecureRequest;

[AuthorizeResource(ResourceKinds.RegistrationOrder, AuthorizationActions.RegistrationOrders.Continue)]
public sealed record RetryAuthenticatedRegistrationPaymentCommand(Guid EventId, Guid OrderId)
    : IRequest<RegistrationPaymentCommandResultDto>, IAuthenticatedRegistrationPaymentSecureRequest;
