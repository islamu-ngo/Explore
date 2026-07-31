// ABOUTME: Handles guest registration-order starts and lifecycle commands through one scoped capability check.
// ABOUTME: Delegates creation and transaction-sensitive transitions to the established Application services.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Features.RegistrationOrders.Handlers;
using Explore.Application.Features.RegistrationOrders.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.RegistrationOrders.Handlers.Commands;

public sealed class StartGuestRegistrationOrderCommandHandler(
    IRegistrationOrderStarter starter,
    IGuestCapabilityTokenService capabilities)
    : IRequestHandler<StartGuestRegistrationOrderCommand, GuestRegistrationOrderStartDto>
{
    public async Task<GuestRegistrationOrderStartDto> Handle(
        StartGuestRegistrationOrderCommand request,
        CancellationToken cancellationToken)
    {
        GuestCapabilityTokenIssue capability = capabilities.Issue();
        BaseCommandResponse<Guid> response = await starter.StartAsync(new CreateRegistrationOrderWithHoldCommand
        {
            EventId = request.EventId,
            TicketCatalogVersionId = request.TicketCatalogVersionId,
            BookingPartyType = request.BookingPartyType,
            GuestAccessTokenHash = capability.Hash,
            PlatformContributionBasisPoints = request.PlatformContributionBasisPoints,
            Lines = request.Lines
        }, cancellationToken);

        return new GuestRegistrationOrderStartDto
        {
            Id = response.Id,
            Success = response.Success,
            Message = response.Message,
            FailureCode = response.FailureCode,
            Errors = response.Errors,
            GuestCapabilityToken = response.Success ? capability.RawToken : null
        };
    }
}

public sealed class ContinueGuestRegistrationOrderCommandHandler(
    IRegistrationInventoryRepository inventory,
    IRegistrationOrderLifecycleService lifecycle,
    IGuestCapabilityTokenService capabilities,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : IRequestHandler<ContinueGuestRegistrationOrderCommand, GuestRegistrationOrderLifecycleResponseDto>
{
    public async Task<GuestRegistrationOrderLifecycleResponseDto> Handle(
        ContinueGuestRegistrationOrderCommand request,
        CancellationToken cancellationToken) => GuestRegistrationOrderLifecycleResponseDto.From(
        await RegistrationOrderAccessGuard.ExecuteGuestAsync(
            request,
            inventory,
            capabilities,
            tenant,
            timeProvider,
            (orderId, tenantId, token) => lifecycle.SubmitAsync(
                orderId,
                tenantId,
                request.PlatformContributionBasisPoints,
                token),
            cancellationToken));
}

public sealed class FinalizeGuestRegistrationOrderCommandHandler(
    IRegistrationInventoryRepository inventory,
    IRegistrationOrderLifecycleService lifecycle,
    IGuestCapabilityTokenService capabilities,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : IRequestHandler<FinalizeGuestRegistrationOrderCommand, GuestRegistrationOrderLifecycleResponseDto>
{
    public async Task<GuestRegistrationOrderLifecycleResponseDto> Handle(
        FinalizeGuestRegistrationOrderCommand request,
        CancellationToken cancellationToken) => GuestRegistrationOrderLifecycleResponseDto.From(
        await RegistrationOrderAccessGuard.ExecuteGuestAsync(
            request, inventory, capabilities, tenant, timeProvider, lifecycle.FinalizeFreeAsync, cancellationToken));
}

public sealed class CancelGuestRegistrationOrderCommandHandler(
    IRegistrationInventoryRepository inventory,
    IRegistrationOrderLifecycleService lifecycle,
    IGuestCapabilityTokenService capabilities,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : IRequestHandler<CancelGuestRegistrationOrderCommand, GuestRegistrationOrderLifecycleResponseDto>
{
    public async Task<GuestRegistrationOrderLifecycleResponseDto> Handle(
        CancelGuestRegistrationOrderCommand request,
        CancellationToken cancellationToken) => GuestRegistrationOrderLifecycleResponseDto.From(
        await RegistrationOrderAccessGuard.ExecuteGuestAsync(
            request, inventory, capabilities, tenant, timeProvider, lifecycle.CancelAsync, cancellationToken));
}
