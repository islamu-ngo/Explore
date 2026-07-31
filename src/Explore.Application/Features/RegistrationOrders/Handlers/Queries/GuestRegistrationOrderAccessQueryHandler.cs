// ABOUTME: Resolves a guest order only after validating its full tenant/event/order/capability scope.
// ABOUTME: Maps all malformed, missing, expired, and mismatched access attempts to generic absence.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Features.RegistrationOrders.Handlers;
using Explore.Application.Features.RegistrationOrders.Requests.Commands;
using Explore.Application.Features.RegistrationOrders.Requests.Queries;
using Explore.Application.Features.RegistrationOrders.Validators;
using MediatR;

namespace Explore.Application.Features.RegistrationOrders.Handlers.Queries;

public sealed class GetGuestRegistrationOrderQueryHandler(
    IRegistrationInventoryRepository inventory,
    IRegistrationOrderLifecycleService lifecycle,
    IGuestCapabilityTokenService capabilities,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : IRequestHandler<GetGuestRegistrationOrderQuery, GuestRegistrationOrderDto?>
{
    public async Task<GuestRegistrationOrderDto?> Handle(GetGuestRegistrationOrderQuery request, CancellationToken cancellationToken)
    {
        var command = new ContinueGuestRegistrationOrderCommand(request.EventId, request.OrderId, request.CapabilityToken);
        if (!(await new GuestRegistrationOrderAccessCommandValidator<ContinueGuestRegistrationOrderCommand>()
                .ValidateAsync(command, cancellationToken)).IsValid ||
            await RegistrationOrderAccessGuard.GetGuestOrderAsync(
                inventory,
                capabilities,
                tenant.TenantId,
                request.EventId,
                request.OrderId,
                request.CapabilityToken,
                timeProvider,
                cancellationToken) is null)
        {
            return null;
        }

        RegistrationOrderDto? order = await lifecycle.GetAsync(request.OrderId, tenant.TenantId, cancellationToken);
        return order is null ? null : GuestRegistrationOrderDto.From(order);
    }
}
