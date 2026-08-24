// ABOUTME: Handles payment starts and safe retries after account or guest-capability order access is proven.
// ABOUTME: Delegates durable claim and parked-prehandoff retry decisions to the payment contract service.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Features.RegistrationOrders.Requests.Commands;
using Explore.Application.Features.RegistrationOrders.Validators;
using Explore.Application.Services.Registration;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.RegistrationOrders.Handlers.Commands;

public sealed class StartGuestRegistrationPaymentCommandHandler(
    IRegistrationInventoryRepository inventory,
    IGuestCapabilityTokenService capabilities,
    ITenantContext tenant,
    TimeProvider timeProvider,
    RegistrationPaymentContractService payments)
    : IRequestHandler<StartGuestRegistrationPaymentCommand, RegistrationPaymentCommandResultDto>
{
    public async Task<RegistrationPaymentCommandResultDto> Handle(StartGuestRegistrationPaymentCommand request, CancellationToken cancellationToken)
    {
        var validator = new GuestRegistrationOrderAccessCommandValidator<StartGuestRegistrationPaymentCommand>();
        if (!(await validator.ValidateAsync(request, cancellationToken)).IsValid)
        {
            return NotFound();
        }

        RegistrationOrder? order = await RegistrationOrderAccessGuard.GetGuestOrderAsync(
            inventory, capabilities, tenant.TenantId, request.EventId, request.OrderId, request.CapabilityToken, timeProvider, cancellationToken);
        return order is null ? NotFound() : await payments.StartAsync(order, request.Acceptance, cancellationToken);
    }

    private static RegistrationPaymentCommandResultDto NotFound() => PaymentNotFound.Result();
}

public sealed class RetryGuestRegistrationPaymentCommandHandler(
    IRegistrationInventoryRepository inventory,
    IGuestCapabilityTokenService capabilities,
    ITenantContext tenant,
    TimeProvider timeProvider,
    RegistrationPaymentContractService payments)
    : IRequestHandler<RetryGuestRegistrationPaymentCommand, RegistrationPaymentCommandResultDto>
{
    public async Task<RegistrationPaymentCommandResultDto> Handle(RetryGuestRegistrationPaymentCommand request, CancellationToken cancellationToken)
    {
        var validator = new GuestRegistrationOrderAccessCommandValidator<RetryGuestRegistrationPaymentCommand>();
        if (!(await validator.ValidateAsync(request, cancellationToken)).IsValid)
        {
            return PaymentNotFound.Result();
        }

        RegistrationOrder? order = await RegistrationOrderAccessGuard.GetGuestOrderAsync(
            inventory, capabilities, tenant.TenantId, request.EventId, request.OrderId, request.CapabilityToken, timeProvider, cancellationToken);
        return order is null ? PaymentNotFound.Result() : await payments.RetryAsync(order, cancellationToken);
    }
}

public sealed class StartAuthenticatedRegistrationPaymentCommandHandler(
    IRegistrationInventoryRepository inventory,
    ITenantContext tenant,
    ICurrentUserService currentUser,
    TimeProvider timeProvider,
    RegistrationPaymentContractService payments)
    : IRequestHandler<StartAuthenticatedRegistrationPaymentCommand, RegistrationPaymentCommandResultDto>
{
    public async Task<RegistrationPaymentCommandResultDto> Handle(StartAuthenticatedRegistrationPaymentCommand request, CancellationToken cancellationToken)
    {
        var validator = new AuthenticatedRegistrationOrderAccessCommandValidator<StartAuthenticatedRegistrationPaymentCommand>();
        if (!(await validator.ValidateAsync(request, cancellationToken)).IsValid)
        {
            return PaymentNotFound.Result();
        }

        RegistrationOrder? order = await RegistrationOrderAccessGuard.GetCurrentAccountOrderBeforeExpiryAsync(
            inventory, currentUser, tenant.TenantId, request.EventId, request.OrderId, timeProvider, cancellationToken);
        return order is null ? PaymentNotFound.Result() : await payments.StartAsync(order, request.Acceptance, cancellationToken);
    }
}

public sealed class RetryAuthenticatedRegistrationPaymentCommandHandler(
    IRegistrationInventoryRepository inventory,
    ITenantContext tenant,
    ICurrentUserService currentUser,
    TimeProvider timeProvider,
    RegistrationPaymentContractService payments)
    : IRequestHandler<RetryAuthenticatedRegistrationPaymentCommand, RegistrationPaymentCommandResultDto>
{
    public async Task<RegistrationPaymentCommandResultDto> Handle(RetryAuthenticatedRegistrationPaymentCommand request, CancellationToken cancellationToken)
    {
        var validator = new AuthenticatedRegistrationOrderAccessCommandValidator<RetryAuthenticatedRegistrationPaymentCommand>();
        if (!(await validator.ValidateAsync(request, cancellationToken)).IsValid)
        {
            return PaymentNotFound.Result();
        }

        RegistrationOrder? order = await RegistrationOrderAccessGuard.GetCurrentAccountOrderBeforeExpiryAsync(
            inventory, currentUser, tenant.TenantId, request.EventId, request.OrderId, timeProvider, cancellationToken);
        return order is null ? PaymentNotFound.Result() : await payments.RetryAsync(order, cancellationToken);
    }
}

file static class PaymentNotFound
{
    public static RegistrationPaymentCommandResultDto Result() => new()
    {
        FailureCode = "registration_order_not_found",
        Message = "Registration order was not found."
    };
}
