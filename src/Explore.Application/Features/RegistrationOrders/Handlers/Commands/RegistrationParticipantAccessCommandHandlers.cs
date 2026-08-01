// ABOUTME: Gates participant mutations through current-account ownership or an opaque guest order capability.
// ABOUTME: Dispatches the existing participant CQRS commands only after the shared order access guard succeeds.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.RegistrationOrders.Handlers;
using Explore.Application.Features.RegistrationOrders.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.RegistrationOrders.Handlers.Commands;

public sealed class MutateGuestRegistrationParticipantsCommandHandler(
    IRegistrationInventoryRepository inventory,
    IGuestCapabilityTokenService capabilities,
    ITenantContext tenant,
    TimeProvider timeProvider,
    ISender sender)
    : IRequestHandler<MutateGuestRegistrationParticipantsCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        MutateGuestRegistrationParticipantsCommand request,
        CancellationToken cancellationToken) =>
        request.Mutation.RegistrationOrderId != request.OrderId ||
        await RegistrationOrderAccessGuard.GetGuestOrderAsync(
            inventory, capabilities, tenant.TenantId, request.EventId, request.OrderId,
            request.CapabilityToken, timeProvider, cancellationToken) is null
            ? RegistrationOrderAccessGuard.ParticipantNotFound(request.OrderId)
            : await sender.Send(request.Mutation, cancellationToken);
}

public sealed class MutateAuthenticatedRegistrationParticipantsCommandHandler(
    IRegistrationInventoryRepository inventory,
    ITenantContext tenant,
    ICurrentUserService currentUser,
    ISender sender)
    : IRequestHandler<MutateAuthenticatedRegistrationParticipantsCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        MutateAuthenticatedRegistrationParticipantsCommand request,
        CancellationToken cancellationToken) =>
        request.Mutation.RegistrationOrderId != request.OrderId ||
        await RegistrationOrderAccessGuard.GetCurrentAccountOrderAsync(
            inventory, currentUser, tenant.TenantId, request.EventId, request.OrderId, cancellationToken) is null
            ? RegistrationOrderAccessGuard.ParticipantNotFound(request.OrderId)
            : await sender.Send(request.Mutation, cancellationToken);
}
