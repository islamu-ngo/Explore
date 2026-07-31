// ABOUTME: Handles authenticated registration-order starts and lifecycle commands for the current account only.
// ABOUTME: Delegates all transactional state changes to the shared order lifecycle service after ownership checks.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Features.RegistrationOrders.Handlers;
using Explore.Application.Features.RegistrationOrders.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.RegistrationOrders.Handlers.Commands;

public sealed class StartAuthenticatedRegistrationOrderCommandHandler(
    IRegistrationOrderStarter starter,
    ICurrentUserService currentUser)
    : IRequestHandler<StartAuthenticatedRegistrationOrderCommand, BaseCommandResponse<Guid>>
{
    public Task<BaseCommandResponse<Guid>> Handle(
        StartAuthenticatedRegistrationOrderCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            return Task.FromResult(new BaseCommandResponse<Guid>
            {
                Id = request.EventId,
                Success = false,
                FailureCode = "registration_order_authentication_required",
                Message = "Registration order requires an authenticated account.",
                Errors = ["Registration order requires an authenticated account."]
            });
        }

        return starter.StartAsync(new CreateRegistrationOrderWithHoldCommand
        {
            EventId = request.EventId,
            TicketCatalogVersionId = request.TicketCatalogVersionId,
            AccountUserId = userId,
            BookingPartyType = request.BookingPartyType,
            PlatformContributionBasisPoints = request.PlatformContributionBasisPoints,
            Lines = request.Lines
        }, cancellationToken);
    }
}

public sealed class ContinueAuthenticatedRegistrationOrderCommandHandler(
    IRegistrationInventoryRepository inventory,
    IRegistrationOrderLifecycleService lifecycle,
    ITenantContext tenant,
    ICurrentUserService currentUser)
    : IRequestHandler<ContinueAuthenticatedRegistrationOrderCommand, RegistrationOrderLifecycleResponseDto>
{
    public Task<RegistrationOrderLifecycleResponseDto> Handle(
        ContinueAuthenticatedRegistrationOrderCommand request,
        CancellationToken cancellationToken) => RegistrationOrderAccessGuard.ExecuteCurrentAccountAsync(
        request,
        inventory,
        tenant,
        currentUser,
        (orderId, tenantId, token) => lifecycle.SubmitAsync(
            orderId,
            tenantId,
            request.PlatformContributionBasisPoints,
            token),
        cancellationToken);
}

public sealed class FinalizeAuthenticatedRegistrationOrderCommandHandler(
    IRegistrationInventoryRepository inventory,
    IRegistrationOrderLifecycleService lifecycle,
    ITenantContext tenant,
    ICurrentUserService currentUser)
    : IRequestHandler<FinalizeAuthenticatedRegistrationOrderCommand, RegistrationOrderLifecycleResponseDto>
{
    public async Task<RegistrationOrderLifecycleResponseDto> Handle(
        FinalizeAuthenticatedRegistrationOrderCommand request,
        CancellationToken cancellationToken) => await RegistrationOrderAccessGuard.ExecuteCurrentAccountAsync(
        request, inventory, tenant, currentUser, lifecycle.FinalizeFreeAsync, cancellationToken);
}

public sealed class CancelAuthenticatedRegistrationOrderCommandHandler(
    IRegistrationInventoryRepository inventory,
    IRegistrationOrderLifecycleService lifecycle,
    ITenantContext tenant,
    ICurrentUserService currentUser)
    : IRequestHandler<CancelAuthenticatedRegistrationOrderCommand, RegistrationOrderLifecycleResponseDto>
{
    public async Task<RegistrationOrderLifecycleResponseDto> Handle(
        CancelAuthenticatedRegistrationOrderCommand request,
        CancellationToken cancellationToken) => await RegistrationOrderAccessGuard.ExecuteCurrentAccountAsync(
        request, inventory, tenant, currentUser, lifecycle.CancelAsync, cancellationToken);
}
