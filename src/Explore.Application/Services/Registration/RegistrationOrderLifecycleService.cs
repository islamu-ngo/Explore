// ABOUTME: Orchestrates registration-order state changes, conditional inventory use, and interim admissions.
// ABOUTME: Keeps every lifecycle write in one unit-of-work transaction and creates only PII-free outbox intent.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Registration;

namespace Explore.Application.Services.Registration;

public sealed class RegistrationOrderLifecycleService(
    IRegistrationInventoryRepository inventory,
    IRegistrationParticipantRepository participants,
    IEventTicketCatalogRepository catalogs,
    IPlatformContributionSettingRepository contributionSettings,
    IEventSessionRepository eventSessions,
    IOutboxRepository outbox,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRegistrationOrderLifecycleService
{
    public Task<RegistrationOrderLifecycleResponseDto> SubmitAsync(
        Guid orderId,
        Guid tenantId,
        CancellationToken cancellationToken) => SubmitAsync(orderId, tenantId, null, cancellationToken);

    public async Task<RegistrationOrderLifecycleResponseDto> SubmitAsync(
        Guid orderId,
        Guid tenantId,
        int? platformContributionBasisPoints,
        CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        return await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            RegistrationOrder? order = platformContributionBasisPoints.HasValue
                ? await inventory.GetOrderForUpdateWithLinesAsync(orderId, tenantId, token)
                : await inventory.GetOrderWithLinesAsync(orderId, tenantId, token);
            if (order is null)
            {
                return Missing(orderId);
            }

            RegistrationOrderStatusEnum status = (RegistrationOrderStatusEnum)order.RegistrationOrderStatusId;
            if (status != RegistrationOrderStatusEnum.AwaitingParticipantDetails)
            {
                return Success(order, status, "Registration order is already submitted.");
            }

            if (platformContributionBasisPoints.HasValue)
            {
                if (platformContributionBasisPoints is < 0 or > 10_000)
                {
                    return Failure(orderId, order, "Platform contribution percentage is invalid.");
                }

                RegistrationOrderPlatformContribution? contribution = null;
                if (platformContributionBasisPoints > 0)
                {
                    if (order.OrganizerDirectedTotalMinorSnapshot == 0)
                    {
                        return Failure(orderId, order, "Platform contributions require an existing payable order total.");
                    }

                    PlatformContributionSetting? setting = await contributionSettings.GetActiveAsync(token);
                    if (setting is null || !setting.IsEnabled)
                    {
                        return Failure(orderId, order, "Platform contributions are not enabled.");
                    }

                    if (setting.Options.All(option => option.ContributionBasisPoints != platformContributionBasisPoints.Value))
                    {
                        return Failure(orderId, order, "Platform contribution percentage is invalid.");
                    }

                    contribution = RegistrationOrderPlatformContribution.CreateOrNull(
                        order.Id,
                        order.TenantId,
                        setting,
                        platformContributionBasisPoints.Value,
                        order.OrganizerDirectedTotalMinorSnapshot,
                        order.CurrencyCode);
                }

                order.SetPlatformContribution(contribution);
                order.ApplyTotals(RegistrationOrderTotalsSnapshot.Create(
                    order.CurrencyCode,
                    order.OrganizerDirectedTotalMinorSnapshot,
                    order.PlatformFeeTotalMinorSnapshot,
                    order.OrganizerEarningsTotalMinorSnapshot,
                    contribution?.AmountMinor ?? 0));
                await inventory.SaveChangesAsync(token);
            }

            bool transitioned = await inventory.TryTransitionOrderAsync(
                order.Id,
                tenantId,
                status,
                RegistrationOrderStatusEnum.AwaitingRequirements,
                now,
                token);
            return transitioned
                ? Success(order, RegistrationOrderStatusEnum.AwaitingRequirements, "Registration order submitted.")
                : await CurrentOrConflictAsync(orderId, tenantId, "Registration order changed while it was submitted.", token);
        }, cancellationToken);
    }

    public async Task<RegistrationOrderLifecycleResponseDto> ReadyForCheckoutAsync(Guid orderId, Guid tenantId, CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        RegistrationOrder? initialOrder = await inventory.GetOrderWithLinesAsync(orderId, tenantId, cancellationToken);
        if (initialOrder is null)
        {
            return Missing(orderId);
        }

        RegistrationOrderStatusEnum initialStatus = (RegistrationOrderStatusEnum)initialOrder.RegistrationOrderStatusId;
        if (initialStatus != RegistrationOrderStatusEnum.AwaitingRequirements)
        {
            return Success(initialOrder, initialStatus, "Registration order is already routed for checkout.");
        }

        CapacityReservationPlan plan = await PrepareCapacityReservationPlanAsync(initialOrder, cancellationToken);
        bool requiresApproval = await RequiresApprovalAsync(initialOrder, plan.TicketTypes, cancellationToken);
        try
        {
            return await unitOfWork.ExecuteSerializableAsync(async token =>
            {
                RegistrationOrder? order = await inventory.GetOrderWithLinesAsync(orderId, tenantId, token);
                if (order is null)
                {
                    return Missing(orderId);
                }

                RegistrationOrderStatusEnum status = (RegistrationOrderStatusEnum)order.RegistrationOrderStatusId;
                if (status != RegistrationOrderStatusEnum.AwaitingRequirements)
                {
                    return Success(order, status, "Registration order is already routed for checkout.");
                }

                if (requiresApproval)
                {
                    return await TransitionForApprovalAsync(order, now, token);
                }

                RegistrationInventoryReservationResult reservation = await ReserveCapacityAsync(
                    order,
                    plan,
                    approvalGranted: false,
                    now,
                    token);
                if (reservation.RequiresApproval)
                {
                    return await TransitionForApprovalAsync(order, now, token);
                }

                if (!reservation.Reserved)
                {
                    if (!reservation.ShouldWaitlist)
                    {
                        throw new CapacityUnavailableException();
                    }

                    await ReleaseActiveHoldsForWaitlistAsync(order, now, token);
                    if (!await inventory.TryTransitionOrderAsync(
                            order.Id,
                            tenantId,
                            RegistrationOrderStatusEnum.AwaitingRequirements,
                            RegistrationOrderStatusEnum.ReadyForCheckout,
                            now,
                            token))
                    {
                        throw new LifecycleRaceException();
                    }

                    if (!await inventory.TryTransitionOrderAsync(
                            order.Id,
                            tenantId,
                            RegistrationOrderStatusEnum.ReadyForCheckout,
                            RegistrationOrderStatusEnum.Waitlisted,
                            now,
                            token))
                    {
                        throw new LifecycleRaceException();
                    }

                    return Success(order, RegistrationOrderStatusEnum.Waitlisted, "Registration order is waitlisted while capacity is unavailable.");
                }

                if (!await inventory.TryTransitionOrderAsync(
                        order.Id,
                        tenantId,
                        RegistrationOrderStatusEnum.AwaitingRequirements,
                        RegistrationOrderStatusEnum.ReadyForCheckout,
                        now,
                        token))
                {
                    throw new LifecycleRaceException();
                }

                return order.TotalDueMinorSnapshot == 0
                    ? Success(order, RegistrationOrderStatusEnum.ReadyForCheckout, "Registration order is ready for free finalization.")
                    : await RouteToPaymentAsync(order, now, token);
            }, cancellationToken);
        }
        catch (CapacityUnavailableException)
        {
            return Failure(orderId, initialOrder, "Registration capacity is unavailable.");
        }
        catch (LifecycleRaceException)
        {
            return await CurrentOrConflictAsync(orderId, tenantId, "Registration order changed while it was prepared for checkout.", cancellationToken);
        }
    }

    public async Task<RegistrationOrderLifecycleResponseDto> ApproveAsync(Guid orderId, Guid tenantId, CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        RegistrationOrder? initialOrder = await inventory.GetOrderWithLinesAsync(orderId, tenantId, cancellationToken);
        if (initialOrder is null)
        {
            return Missing(orderId);
        }

        RegistrationOrderStatusEnum initialStatus = (RegistrationOrderStatusEnum)initialOrder.RegistrationOrderStatusId;
        if (initialStatus != RegistrationOrderStatusEnum.AwaitingApproval)
        {
            return Success(initialOrder, initialStatus, "Registration order approval was already resolved.");
        }

        CapacityReservationPlan plan = await PrepareCapacityReservationPlanAsync(initialOrder, cancellationToken);
        try
        {
            return await unitOfWork.ExecuteSerializableAsync(async token =>
            {
                RegistrationOrder? order = await inventory.GetOrderWithLinesAsync(orderId, tenantId, token);
                if (order is null)
                {
                    return Missing(orderId);
                }

                RegistrationOrderStatusEnum status = (RegistrationOrderStatusEnum)order.RegistrationOrderStatusId;
                if (status != RegistrationOrderStatusEnum.AwaitingApproval)
                {
                    return Success(order, status, "Registration order approval was already resolved.");
                }

                RegistrationInventoryReservationResult reservation = await ReserveCapacityAsync(
                    order,
                    plan,
                    approvalGranted: true,
                    now,
                    token);
                if (reservation.RequiresApproval)
                {
                    throw new InvalidOperationException("Registration order approval did not satisfy capacity policy.");
                }

                if (!reservation.Reserved)
                {
                    if (!reservation.ShouldWaitlist)
                    {
                        throw new CapacityUnavailableException();
                    }

                    await ReleaseActiveHoldsForWaitlistAsync(order, now, token);
                    if (!await inventory.TryTransitionOrderAsync(
                            order.Id,
                            tenantId,
                            RegistrationOrderStatusEnum.AwaitingApproval,
                            RegistrationOrderStatusEnum.Waitlisted,
                            now,
                            token))
                    {
                        throw new LifecycleRaceException();
                    }

                    return Success(order, RegistrationOrderStatusEnum.Waitlisted, "Registration order is waitlisted while capacity is unavailable.");
                }

                if (!await inventory.TryTransitionOrderAsync(
                        order.Id,
                        tenantId,
                        RegistrationOrderStatusEnum.AwaitingApproval,
                        RegistrationOrderStatusEnum.ReadyForCheckout,
                        now,
                        token))
                {
                    throw new LifecycleRaceException();
                }

                return order.TotalDueMinorSnapshot == 0
                    ? Success(order, RegistrationOrderStatusEnum.ReadyForCheckout, "Registration order is ready for free finalization.")
                    : await RouteToPaymentAsync(order, now, token);
            }, cancellationToken);
        }
        catch (CapacityUnavailableException)
        {
            return Failure(orderId, initialOrder, "Registration capacity is unavailable.");
        }
        catch (LifecycleRaceException)
        {
            return await CurrentOrConflictAsync(orderId, tenantId, "Registration order changed while it was approved.", cancellationToken);
        }
    }

    public Task<RegistrationOrderLifecycleResponseDto> RejectAsync(Guid orderId, Guid tenantId, CancellationToken cancellationToken) =>
        EndAsync(
            orderId,
            tenantId,
            RegistrationOrderStatusEnum.AwaitingApproval,
            RegistrationOrderStatusEnum.Rejected,
            RegistrationInventoryHoldStatusEnum.Released,
            "Registration order rejected.",
            cancellationToken);

    public async Task<RegistrationOrderLifecycleResponseDto> CancelAsync(Guid orderId, Guid tenantId, CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        Guid outboxMessageId = Guid.CreateVersion7();
        return await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            RegistrationOrder? order = await inventory.GetOrderWithLinesAsync(orderId, tenantId, token);
            if (order is null)
            {
                return Missing(orderId);
            }

            RegistrationOrderStatusEnum status = (RegistrationOrderStatusEnum)order.RegistrationOrderStatusId;
            if (status == RegistrationOrderStatusEnum.Cancelled)
            {
                return Success(order, status, "Registration order is already cancelled.");
            }

            if (!RegistrationOrderRules.CanTransition(status, RegistrationOrderStatusEnum.Cancelled))
            {
                return Failure(order.Id, order, "Registration order cannot be cancelled from its current state.");
            }

            if (!await inventory.TryTransitionOrderAsync(order.Id, tenantId, status, RegistrationOrderStatusEnum.Cancelled, now, token))
            {
                return await CurrentOrConflictAsync(orderId, tenantId, "Registration order changed while it was cancelled.", token);
            }

            await inventory.TryReleaseActiveHoldsForOrderAsync(
                order.Id,
                tenantId,
                RegistrationInventoryHoldStatusEnum.Cancelled,
                now,
                token);
            await outbox.Create(RegistrationOrderOutboxMessageFactory.Create(
                outboxMessageId, order, RegistrationOrderStatusEnum.Cancelled, now));
            return Success(order, RegistrationOrderStatusEnum.Cancelled, "Registration order cancelled.");
        }, cancellationToken);
    }

    public async Task<RegistrationOrderLifecycleResponseDto> FinalizeFreeAsync(Guid orderId, Guid tenantId, CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        RegistrationOrder? initialOrder = await inventory.GetOrderWithLinesAsync(orderId, tenantId, cancellationToken);
        if (initialOrder is null)
        {
            return Missing(orderId);
        }

        RegistrationOrderStatusEnum initialStatus = (RegistrationOrderStatusEnum)initialOrder.RegistrationOrderStatusId;
        if (initialStatus == RegistrationOrderStatusEnum.Confirmed)
        {
            return Success(initialOrder, initialStatus, "Registration order is already confirmed.");
        }

        if (initialStatus != RegistrationOrderStatusEnum.ReadyForCheckout || initialOrder.TotalDueMinorSnapshot != 0)
        {
            return Failure(orderId, initialOrder, "Registration order is not eligible for free finalization.");
        }

        FinalizationPlan plan;
        try
        {
            plan = await PrepareFinalizationAsync(initialOrder, now, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return Failure(orderId, initialOrder, exception.Message);
        }

        try
        {
            return await unitOfWork.ExecuteSerializableAsync(async token =>
            {
                RegistrationOrder? order = await inventory.GetOrderWithLinesAsync(orderId, tenantId, token);
                if (order is null)
                {
                    return Missing(orderId);
                }

                RegistrationOrderStatusEnum status = (RegistrationOrderStatusEnum)order.RegistrationOrderStatusId;
                if (status == RegistrationOrderStatusEnum.Confirmed)
                {
                    return Success(order, status, "Registration order is already confirmed.");
                }

                if (status != RegistrationOrderStatusEnum.ReadyForCheckout || order.ConcurrencyStamp != plan.ConcurrencyStamp)
                {
                    throw new LifecycleRaceException();
                }

                if (order.PurchaserActorId.HasValue)
                {
                    EventTicketCatalogVersion catalog = await GetPinnedCatalogAsync(order, tenantId, token);
                    EventTicketType[] ticketTypes = ResolveTicketTypes(order, catalog);
                    IReadOnlyDictionary<Guid, RegistrationTicketLimitUsage> usage = await inventory.GetTicketLimitUsageAsync(
                        order.EventId,
                        order.TenantId,
                        accountUserId: null,
                        verifiedContactNormalizedEmail: null,
                        order.PurchaserActorId,
                        ticketTypes.Select(ticket => ticket.Id).ToArray(),
                        token);
                    if (ticketTypes.Any(ticket => ticket.PerBookingPartyLimit is int limit &&
                            usage.GetValueOrDefault(ticket.Id)?.BookingPartyQuantity > limit))
                    {
                        return Failure(order.Id, order, "Registration order exceeds its booking-party ticket limit.");
                    }
                }

                if (!await inventory.TryTransitionOrderAsync(
                        order.Id,
                        tenantId,
                        RegistrationOrderStatusEnum.ReadyForCheckout,
                        RegistrationOrderStatusEnum.NeedsReconciliation,
                        now,
                        token))
                {
                    throw new LifecycleRaceException();
                }

                IReadOnlyList<RegistrationInventoryHold> holds = await inventory.GetHoldsByOrderAsync(order.Id, tenantId, token);
                RegistrationInventoryHold[] activeHolds = holds
                    .Where(hold => hold.RegistrationInventoryHoldStatusId == (int)RegistrationInventoryHoldStatusEnum.Active)
                    .ToArray();
                if (!HasValidActiveHolds(activeHolds, plan.CapacityReservations, now)
                    || await inventory.TryConsumeActiveHoldsForOrderAsync(order.Id, tenantId, now, token) != activeHolds.Length)
                {
                    throw new LifecycleRaceException();
                }

                await participants.AddParticipantsAsync(plan.Placeholders, token);
                await inventory.AddEventRegistrationsAsync(plan.Admissions, token);
                await outbox.Create(plan.OutboxMessage);
                if (!await inventory.TryTransitionOrderAsync(
                        order.Id,
                        tenantId,
                        RegistrationOrderStatusEnum.NeedsReconciliation,
                        RegistrationOrderStatusEnum.Confirmed,
                        now,
                        token))
                {
                    throw new LifecycleRaceException();
                }

                return Success(order, RegistrationOrderStatusEnum.Confirmed, "Registration order confirmed.");
            }, cancellationToken);
        }
        catch (LifecycleRaceException)
        {
            RegistrationOrder? current = await inventory.GetOrderWithLinesAsync(orderId, tenantId, cancellationToken);
            return current is not null && (RegistrationOrderStatusEnum)current.RegistrationOrderStatusId == RegistrationOrderStatusEnum.Confirmed
                ? Success(current, RegistrationOrderStatusEnum.Confirmed, "Registration order is already confirmed.")
                : Failure(orderId, current, "Registration order finalization could not reserve its held inventory.");
        }
    }

    public async Task<RegistrationOrderLifecycleResponseDto> RecoverExpiredHoldAsync(
        Guid orderId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        RegistrationOrder? initialOrder = await inventory.GetOrderWithLinesAsync(orderId, tenantId, cancellationToken);
        if (initialOrder is null)
        {
            return Missing(orderId);
        }

        if ((RegistrationOrderStatusEnum)initialOrder.RegistrationOrderStatusId != RegistrationOrderStatusEnum.NeedsReconciliation)
        {
            return Success(initialOrder, (RegistrationOrderStatusEnum)initialOrder.RegistrationOrderStatusId, "Registration order hold recovery was already resolved.");
        }

        CapacityReservationPlan plan = await PrepareCapacityReservationPlanAsync(initialOrder, cancellationToken);
        try
        {
            return await unitOfWork.ExecuteSerializableAsync(async token =>
            {
                RegistrationOrder? order = await inventory.GetOrderWithLinesAsync(orderId, tenantId, token);
                if (order is null)
                {
                    return Missing(orderId);
                }

                RegistrationOrderStatusEnum status = (RegistrationOrderStatusEnum)order.RegistrationOrderStatusId;
                if (status != RegistrationOrderStatusEnum.NeedsReconciliation)
                {
                    return Success(order, status, "Registration order hold recovery was already resolved.");
                }

                RegistrationInventoryReservationResult reservation = await inventory.ReserveRecoveredHoldsAsync(
                    order.EventId,
                    order.TenantId,
                    plan.Reservations,
                    now,
                    token);
                if (reservation.RequiresApproval)
                {
                    throw new InvalidOperationException("Registration order hold recovery cannot bypass approval.");
                }

                RegistrationOrderStatusEnum destination = reservation.Reserved
                    ? RegistrationOrderStatusEnum.ReadyForCheckout
                    : reservation.ShouldWaitlist
                        ? RegistrationOrderStatusEnum.Waitlisted
                        : throw new CapacityUnavailableException();
                if (destination == RegistrationOrderStatusEnum.Waitlisted)
                {
                    await ReleaseActiveHoldsForWaitlistAsync(order, now, token);
                }

                if (!await inventory.TryTransitionOrderAsync(
                        order.Id,
                        tenantId,
                        RegistrationOrderStatusEnum.NeedsReconciliation,
                        destination,
                        now,
                        token))
                {
                    throw new LifecycleRaceException();
                }

                return Success(
                    order,
                    destination,
                    destination == RegistrationOrderStatusEnum.Waitlisted
                        ? "Registration order is waitlisted while recovered capacity is unavailable."
                        : "Registration order hold recovery re-reserved capacity.");
            }, cancellationToken);
        }
        catch (CapacityUnavailableException)
        {
            return Failure(orderId, initialOrder, "Registration capacity is unavailable for hold recovery.");
        }
        catch (LifecycleRaceException)
        {
            return await CurrentRecoveryOrConflictAsync(orderId, tenantId, cancellationToken);
        }
    }

    public async Task<RegistrationOrderDto?> GetAsync(Guid orderId, Guid tenantId, CancellationToken cancellationToken)
    {
        RegistrationOrder? order = await inventory.GetOrderWithLinesAsync(orderId, tenantId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        PlatformContributionSetting? contributionSetting = await contributionSettings.GetActiveAsync(cancellationToken);
        return RegistrationOrderDto.From(order, contributionSetting: contributionSetting);
    }

    public async Task<IReadOnlyList<RegistrationOrderDto>> GetByEventAsync(Guid eventId, Guid tenantId, CancellationToken cancellationToken)
    {
        IReadOnlyList<RegistrationOrder> orders = await inventory.GetOrdersByEventAsync(eventId, tenantId, cancellationToken);
        return orders.Select(order => RegistrationOrderDto.From(order)).ToArray();
    }

    private async Task<RegistrationOrderLifecycleResponseDto> EndAsync(
        Guid orderId,
        Guid tenantId,
        RegistrationOrderStatusEnum expectedStatus,
        RegistrationOrderStatusEnum desiredStatus,
        RegistrationInventoryHoldStatusEnum holdOutcome,
        string message,
        CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        Guid outboxMessageId = Guid.CreateVersion7();
        return await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            RegistrationOrder? order = await inventory.GetOrderWithLinesAsync(orderId, tenantId, token);
            if (order is null)
            {
                return Missing(orderId);
            }

            RegistrationOrderStatusEnum status = (RegistrationOrderStatusEnum)order.RegistrationOrderStatusId;
            if (status == desiredStatus)
            {
                return Success(order, status, message);
            }

            if (status != expectedStatus || !await inventory.TryTransitionOrderAsync(order.Id, tenantId, expectedStatus, desiredStatus, now, token))
            {
                return await CurrentOrConflictAsync(orderId, tenantId, "Registration order changed before its decision was recorded.", token);
            }

            await inventory.TryReleaseActiveHoldsForOrderAsync(order.Id, tenantId, holdOutcome, now, token);
            await outbox.Create(RegistrationOrderOutboxMessageFactory.Create(outboxMessageId, order, desiredStatus, now));
            return Success(order, desiredStatus, message);
        }, cancellationToken);
    }

    private async Task<CapacityReservationPlan> PrepareCapacityReservationPlanAsync(
        RegistrationOrder order,
        CancellationToken cancellationToken)
    {
        EventTicketCatalogVersion? catalog = await GetPinnedCatalogAsync(order, order.TenantId, cancellationToken);
        EventTicketType[] ticketTypes = ResolveTicketTypes(order, catalog);
        return CreateCapacityReservationPlan(order, ticketTypes);
    }

    private async Task<bool> RequiresApprovalAsync(
        RegistrationOrder order,
        IReadOnlyCollection<EventTicketType> ticketTypes,
        CancellationToken cancellationToken)
    {
        if (ticketTypes.Any(ticketType => ticketType.RequiresApproval))
        {
            return true;
        }

        List<EventSession> sessions = await eventSessions.GetSessionsByEvent(order.EventId);
        return ResolveEntitledSessions(ticketTypes, sessions).Any(session => session.RegistrationModeId == (int)RegistrationModeEnum.ApprovalRequired);
    }

    private async Task<RegistrationOrderLifecycleResponseDto> TransitionForApprovalAsync(
        RegistrationOrder order,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!await inventory.TryTransitionOrderAsync(
                order.Id,
                order.TenantId,
                RegistrationOrderStatusEnum.AwaitingRequirements,
                RegistrationOrderStatusEnum.AwaitingApproval,
                now,
                cancellationToken))
        {
            throw new LifecycleRaceException();
        }

        return Success(order, RegistrationOrderStatusEnum.AwaitingApproval, "Registration order submitted for approval.");
    }

    private async Task<RegistrationOrderLifecycleResponseDto> RouteToPaymentAsync(
        RegistrationOrder order,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!await inventory.TryTransitionOrderAsync(
                order.Id,
                order.TenantId,
                RegistrationOrderStatusEnum.ReadyForCheckout,
                RegistrationOrderStatusEnum.AwaitingPayment,
                now,
                cancellationToken))
        {
            throw new LifecycleRaceException();
        }

        return Success(order, RegistrationOrderStatusEnum.AwaitingPayment, "Registration order is awaiting payment.");
    }

    private Task<RegistrationInventoryReservationResult> ReserveCapacityAsync(
        RegistrationOrder order,
        CapacityReservationPlan plan,
        bool approvalGranted,
        DateTime now,
        CancellationToken cancellationToken) =>
        plan.Reservations.Count == 0
            ? Task.FromResult(new RegistrationInventoryReservationResult(Reserved: true, RequiresApproval: false, ShouldWaitlist: false))
            : inventory.ReserveNonTimedHoldsAsync(
                order.EventId,
                order.TenantId,
                plan.Reservations,
                approvalGranted,
                now,
                cancellationToken);

    private async Task ReleaseActiveHoldsForWaitlistAsync(
        RegistrationOrder order,
        DateTime now,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<RegistrationInventoryHold> holds = await inventory.GetHoldsByOrderAsync(
            order.Id,
            order.TenantId,
            cancellationToken);
        int activeHoldCount = holds.Count(hold =>
            hold.RegistrationInventoryHoldStatusId == (int)RegistrationInventoryHoldStatusEnum.Active);
        if (activeHoldCount == 0)
        {
            return;
        }

        int releasedHoldCount = await inventory.TryReleaseActiveHoldsForOrderAsync(
            order.Id,
            order.TenantId,
            RegistrationInventoryHoldStatusEnum.Released,
            now,
            cancellationToken);
        if (releasedHoldCount != activeHoldCount)
        {
            throw new LifecycleRaceException();
        }
    }

    private static CapacityReservationPlan CreateCapacityReservationPlan(
        RegistrationOrder order,
        IEnumerable<EventTicketType> ticketTypes)
    {
        var ticketsById = ticketTypes.ToDictionary(ticketType => ticketType.Id);
        RegistrationInventoryReservation[] reservations = order.Lines
            .OrderBy(line => line.Id)
            .Where(line => ticketsById[line.TicketTypeId].CapacityPoolId.HasValue)
            .Select(line => new RegistrationInventoryReservation(
                Guid.CreateVersion7(),
                order.Id,
                ticketsById[line.TicketTypeId].CapacityPoolId!.Value,
                line.TicketTypeId,
                line.Quantity))
            .ToArray();
        return new CapacityReservationPlan(ticketsById.Values.ToArray(), reservations);
    }

    private static bool HasValidActiveHolds(
        IReadOnlyList<RegistrationInventoryHold> holds,
        CapacityReservationPlan plan,
        DateTime now)
    {
        if (plan.Reservations.Count == 0)
        {
            return holds.Count == 0;
        }

        if (holds.Count != plan.Reservations.Count || holds.Any(hold =>
                hold.RegistrationInventoryHoldStatusId != (int)RegistrationInventoryHoldStatusEnum.Active ||
                hold.ExpiresAt <= now) ||
            holds.GroupBy(hold => hold.TicketTypeId).Any(group => group.Count() != 1))
        {
            return false;
        }

        var holdsByTicketTypeId = holds.ToDictionary(hold => hold.TicketTypeId);
        return plan.Reservations.All(reservation =>
            holdsByTicketTypeId.TryGetValue(reservation.TicketTypeId, out RegistrationInventoryHold? hold) &&
            hold.CapacityPoolId == reservation.CapacityPoolId &&
            hold.Quantity == reservation.Quantity);
    }

    private async Task<FinalizationPlan> PrepareFinalizationAsync(
        RegistrationOrder order,
        DateTime now,
        CancellationToken cancellationToken)
    {
        EventTicketCatalogVersion? catalog = await GetPinnedCatalogAsync(order, order.TenantId, cancellationToken);
        EventTicketType[] ticketTypes = ResolveTicketTypes(order, catalog);
        IReadOnlyList<RegistrationTicketAssignment> assignments =
            await participants.GetAssignmentsWithParticipantsByOrderAsync(order.Id, order.TenantId, cancellationToken);
        List<EventSession> sessions = await eventSessions.GetSessionsByEvent(order.EventId);
        List<EventSession> entitledSessions = ResolveEntitledSessions(ticketTypes, sessions);
        if (entitledSessions.Count == 0)
        {
            throw new InvalidOperationException("Registration order has no materializable session admissions.");
        }

        if (entitledSessions.Any(session => session.RegistrationModeId is not (int)RegistrationModeEnum.Open and not (int)RegistrationModeEnum.ApprovalRequired))
        {
            throw new InvalidOperationException("Registration order includes a session that is not accepting admissions.");
        }

        var ticketTypesById = ticketTypes.ToDictionary(ticketType => ticketType.Id);
        var sessionsById = sessions.ToDictionary(session => session.Id);
        var orderLinesById = order.Lines.ToDictionary(line => line.Id);
        if (assignments.Any(assignment => assignment.TenantId != order.TenantId ||
                assignment.RegistrationOrderId != order.Id ||
                !orderLinesById.ContainsKey(assignment.RegistrationOrderLineId)))
        {
            throw new InvalidOperationException("Ticket assignments do not belong to this registration order.");
        }

        var placeholders = new List<RegistrationParticipant>();
        var admissions = new List<EventRegistration>();
        foreach (RegistrationOrderLine line in order.Lines.OrderBy(line => line.Id))
        {
            EventTicketType ticketType = ticketTypesById[line.TicketTypeId];
            RegistrationTicketAssignment[] lineAssignments = assignments
                .Where(assignment => assignment.RegistrationOrderLineId == line.Id)
                .OrderBy(assignment => assignment.Ordinal)
                .ToArray();
            DateTime? assignmentDeadline = ResolveCommonAssignmentDeadline(lineAssignments);
            try
            {
                if (!RegistrationOrderRules.CanConfirmParticipantAssignments(
                        (ParticipantDataCollectionModeEnum)ticketType.ParticipantDataCollectionModeId,
                        line.Quantity,
                        lineAssignments,
                        assignmentDeadline,
                        now))
                {
                    throw new InvalidOperationException("Ticket participant assignments are incomplete.");
                }
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException(exception.Message, exception);
            }

            var assignmentsByOrdinal = lineAssignments.ToDictionary(assignment => assignment.Ordinal);
            for (int unitOrdinal = 1; unitOrdinal <= line.Quantity; unitOrdinal++)
            {
                RegistrationParticipant? participant = ResolveUnitParticipant(
                    order,
                    ticketType,
                    assignmentsByOrdinal.GetValueOrDefault(unitOrdinal),
                    placeholders);
                if (participant is null)
                {
                    continue;
                }

                foreach ((TicketTypeEntitlement entitlement, EventSession session) in
                         RegistrationAdmissionMaterializer.Expand(ticketType, sessions))
                {
                    admissions.Add(RegistrationAdmissionMaterializer.Create(
                        Guid.CreateVersion7(),
                        Guid.CreateVersion7(),
                        order,
                        line,
                        entitlement,
                        session,
                        participant,
                        unitOrdinal,
                        now));
                }
            }
        }

        CapacityReservationPlan capacityReservations = CreateCapacityReservationPlan(order, ticketTypes);
        return new FinalizationPlan(
            order.ConcurrencyStamp,
            placeholders,
            admissions,
            capacityReservations,
            RegistrationOrderOutboxMessageFactory.Create(
                Guid.CreateVersion7(),
                order,
                RegistrationOrderStatusEnum.Confirmed,
                now,
                admissions.Count));
    }

    private static DateTime? ResolveCommonAssignmentDeadline(
        IReadOnlyCollection<RegistrationTicketAssignment> assignments)
    {
        DateTime[] deadlines = assignments
            .Where(assignment => assignment.AssignmentStatusId == (int)AssignmentStatusEnum.Deferred)
            .Select(assignment => assignment.AssignmentDeadline ?? DateTime.MinValue)
            .Distinct()
            .ToArray();
        return deadlines.Length == 1 ? deadlines[0] : null;
    }

    private static RegistrationParticipant? ResolveUnitParticipant(
        RegistrationOrder order,
        EventTicketType ticketType,
        RegistrationTicketAssignment? assignment,
        ICollection<RegistrationParticipant> placeholders)
    {
        ParticipantDataCollectionModeEnum mode =
            (ParticipantDataCollectionModeEnum)ticketType.ParticipantDataCollectionModeId;
        if (mode == ParticipantDataCollectionModeEnum.DeferredAssignment &&
            assignment?.AssignmentStatusId == (int)AssignmentStatusEnum.Deferred)
        {
            return null;
        }

        if (assignment?.Participant is { } assignedParticipant)
        {
            if (assignedParticipant.Id != assignment.ParticipantId ||
                assignedParticipant.TenantId != order.TenantId ||
                assignedParticipant.RegistrationOrderId != order.Id ||
                !RegistrationOrderRules.IsParticipantEligibleForTicket(assignedParticipant))
            {
                throw new InvalidOperationException("Assigned participant is not eligible for this registration order.");
            }

            return assignedParticipant;
        }

        if (assignment?.ParticipantId is not null)
        {
            throw new InvalidOperationException("Assigned participant details could not be loaded.");
        }

        RegistrationParticipant placeholder = RegistrationParticipant.Create(
            Guid.CreateVersion7(),
            order.TenantId,
            order.Id,
            linkedUserId: null,
            ParticipantTypeEnum.Unnamed,
            guardian: null);
        placeholders.Add(placeholder);
        return placeholder;
    }

    private async Task<EventTicketCatalogVersion> GetPinnedCatalogAsync(
        RegistrationOrder order,
        Guid tenantId,
        CancellationToken cancellationToken) =>
        await catalogs.GetOrderCatalogAsync(order.TicketCatalogVersionId, order.EventId, tenantId, cancellationToken)
        ?? throw new InvalidOperationException("Registration order ticket catalog was not found.");

    private static EventTicketType[] ResolveTicketTypes(RegistrationOrder order, EventTicketCatalogVersion catalog)
    {
        var ticketTypes = catalog.TicketTypes
            .Where(ticketType => !ticketType.IsDeleted)
            .ToDictionary(ticketType => ticketType.Id);
        if (order.Lines.Any(line => !ticketTypes.ContainsKey(line.TicketTypeId)))
        {
            throw new InvalidOperationException("Registration order ticket selections no longer match its pinned catalog.");
        }

        return order.Lines.Select(line => ticketTypes[line.TicketTypeId]).ToArray();
    }

    private static List<EventSession> ResolveEntitledSessions(
        IEnumerable<EventTicketType> ticketTypes,
        IReadOnlyCollection<EventSession> sessions) => ticketTypes
        .SelectMany(ticketType => ticketType.Entitlements)
        .SelectMany(entitlement => ResolveEntitlementSessions(entitlement, sessions))
        .DistinctBy(session => session.Id)
        .ToList();

    private static IEnumerable<EventSession> ResolveEntitlementSessions(
        TicketTypeEntitlement entitlement,
        IEnumerable<EventSession> sessions)
    {
        if ((EntitlementSelectionRuleEnum)entitlement.EntitlementSelectionRuleId is EntitlementSelectionRuleEnum.ChooseOne or EntitlementSelectionRuleEnum.ChooseUpToN)
        {
            throw new InvalidOperationException("Registration order requires a session selection before finalization.");
        }

        return (EntitlementScopeTypeEnum)entitlement.EntitlementScopeTypeId switch
        {
            EntitlementScopeTypeEnum.Event => sessions.Where(session => session.EventId == entitlement.TargetEventId),
            EntitlementScopeTypeEnum.EventDay => sessions.Where(session => session.EventDayId == entitlement.EventDayId),
            EntitlementScopeTypeEnum.EventSession => sessions.Where(session => session.Id == entitlement.EventSessionId),
            _ => throw new InvalidOperationException("Registration order entitlement scope is invalid.")
        };
    }

    private async Task<RegistrationOrderLifecycleResponseDto> CurrentOrConflictAsync(
        Guid orderId,
        Guid tenantId,
        string message,
        CancellationToken cancellationToken)
    {
        RegistrationOrder? current = await inventory.GetOrderWithLinesAsync(orderId, tenantId, cancellationToken);
        return current is null ? Missing(orderId) : Failure(orderId, current, message);
    }

    private async Task<RegistrationOrderLifecycleResponseDto> CurrentRecoveryOrConflictAsync(
        Guid orderId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        RegistrationOrder? current = await inventory.GetOrderWithLinesAsync(orderId, tenantId, cancellationToken);
        return current is null
            ? Missing(orderId)
            : (RegistrationOrderStatusEnum)current.RegistrationOrderStatusId == RegistrationOrderStatusEnum.NeedsReconciliation
                ? Failure(orderId, current, "Registration order changed while hold recovery was recorded.")
                : Success(current, (RegistrationOrderStatusEnum)current.RegistrationOrderStatusId, "Registration order hold recovery was already resolved.");
    }

    private static RegistrationOrderLifecycleResponseDto Success(
        RegistrationOrder order,
        RegistrationOrderStatusEnum status,
        string message) => new()
        {
            Id = order.Id,
            Success = true,
            Message = message,
            Order = RegistrationOrderDto.From(order, status)
        };

    private static RegistrationOrderLifecycleResponseDto Missing(Guid orderId) => new()
    {
        Id = orderId,
        Success = false,
        Message = "Registration order was not found."
    };

    private static RegistrationOrderLifecycleResponseDto Failure(
        Guid orderId,
        RegistrationOrder? order,
        string error) => new()
        {
            Id = orderId,
            Success = false,
            Message = "Registration order lifecycle change failed.",
            Errors = [error],
            Order = order is null ? null : RegistrationOrderDto.From(order)
        };

    private sealed record FinalizationPlan(
        Guid ConcurrencyStamp,
        IReadOnlyList<RegistrationParticipant> Placeholders,
        IReadOnlyList<EventRegistration> Admissions,
        CapacityReservationPlan CapacityReservations,
        OutboxMessage OutboxMessage);

    private sealed record CapacityReservationPlan(
        IReadOnlyCollection<EventTicketType> TicketTypes,
        IReadOnlyList<RegistrationInventoryReservation> Reservations);

    private sealed class LifecycleRaceException : Exception;

    private sealed class CapacityUnavailableException : Exception;
}
