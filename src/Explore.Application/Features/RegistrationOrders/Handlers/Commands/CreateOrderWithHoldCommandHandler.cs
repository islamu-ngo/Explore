// ABOUTME: Creates one order and its capacity holds under a serializable transaction with retry-stable identities.
// ABOUTME: Reserves capacity before PII, converts unavailable capacity to a waitlist, and performs no external I/O.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.RegistrationOrders.Requests.Commands;
using Explore.Application.Features.RegistrationOrders.Validators;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.RegistrationOrders.Handlers.Commands;

public sealed class CreateOrderWithHoldCommandHandler(
    IEventRepository events,
    IEventTicketCatalogRepository catalogs,
    IRegistrationInventoryRepository inventory,
    IPlatformFeePolicyRepository feePolicies,
    ITenantContext tenant,
    IOrganizerEarningsCalculator earningsCalculator,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateRegistrationOrderWithHoldCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        CreateRegistrationOrderWithHoldCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await new CreateRegistrationOrderWithHoldCommandValidator()
            .ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Invalid(request.EventId, validation.Errors.Select(error => error.ErrorMessage));
        }

        Event? eventTarget = await events.GetAuthorizationTargetByIdAsync(request.EventId, cancellationToken);
        if (eventTarget is null || eventTarget.TenantId != tenant.TenantId ||
            eventTarget.ParticipationConfiguration is not
            {
                ParticipationHandlingModeId: (int)ParticipationHandlingModeEnum.PlatformManaged
            } participationConfiguration)
        {
            return Missing(request.EventId);
        }

        Guid orderId = Guid.CreateVersion7();
        DateTime createdAt = timeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyDictionary<Guid, StableLineIds> stableLineIds = request.Lines
            .OrderBy(line => line.TicketTypeId)
            .ToDictionary(
                line => line.TicketTypeId,
                _ => new StableLineIds(Guid.CreateVersion7(), Guid.CreateVersion7()));
        RegistrationParticipationSnapshot participation = RegistrationParticipationSnapshot.From(participationConfiguration);

        try
        {
            return await unitOfWork.ExecuteSerializableAsync(async token =>
            {
                RegistrationOrder? existing = await inventory.GetOrderByIdAsync(orderId, tenant.TenantId, token);
                if (existing is not null)
                {
                    return Success(existing.Id, "Registration order already created.");
                }

                EventTicketCatalogVersion? catalog = await catalogs.GetPublishedCatalogAsync(
                    request.EventId,
                    tenant.TenantId,
                    token);
                if (catalog is null || catalog.Id != request.TicketCatalogVersionId)
                {
                    return Missing(request.EventId);
                }

                Dictionary<Guid, EventTicketType> ticketTypes = catalog.TicketTypes
                    .Where(ticketType => !ticketType.IsDeleted)
                    .ToDictionary(ticketType => ticketType.Id);
                if (request.Lines.Any(line => !ticketTypes.ContainsKey(line.TicketTypeId)))
                {
                    return Missing(request.EventId);
                }

                PlatformFeePolicy? feePolicy = await feePolicies.GetActiveAsync(token);
                var preparedLines = request.Lines
                    .OrderBy(line => line.TicketTypeId)
                    .Select(line => new PreparedLine(line, ticketTypes[line.TicketTypeId], stableLineIds[line.TicketTypeId]))
                    .ToArray();
                if (preparedLines.Any(line => line.Selection.Quantity > line.Ticket.PerOrderLimit))
                {
                    return Invalid(request.EventId, "A ticket quantity exceeds its per-order limit.");
                }

                IReadOnlyDictionary<Guid, RegistrationTicketLimitUsage> usageByTicket = await inventory.GetTicketLimitUsageAsync(
                    request.EventId,
                    tenant.TenantId,
                    request.AccountUserId,
                    NormalizeVerifiedContact(request.VerifiedContactNormalizedEmail),
                    request.PurchaserActorId,
                    preparedLines.Select(line => line.Ticket.Id).ToArray(),
                    token);
                if (TryGetLimitViolation(request, preparedLines, usageByTicket, out string? limitError))
                {
                    return LimitExceeded(request.EventId, limitError!);
                }

                Guid[] poolIds = preparedLines
                    .Where(line => line.Ticket.CapacityPoolId.HasValue)
                    .Select(line => line.Ticket.CapacityPoolId!.Value)
                    .Distinct()
                    .Order()
                    .ToArray();
                IReadOnlyList<EventCapacityPool> pools = await inventory.GetPoolsForUpdateAsync(
                    poolIds,
                    request.EventId,
                    tenant.TenantId,
                    token);
                Dictionary<Guid, EventCapacityPool> poolsById = pools.ToDictionary(pool => pool.Id);
                if (poolsById.Count != poolIds.Length || poolsById.Values.Any(pool => !pool.IsActive))
                {
                    return Missing(request.EventId);
                }

                if (!TryGetHoldPolicies(poolsById, out Dictionary<Guid, CapacityHoldPolicyEnum> holdPolicies))
                {
                    return Invalid(request.EventId, "Capacity pool hold policy is invalid.");
                }

                bool reservesCapacityOnSelection = holdPolicies.Values.Any(ReservesCapacityOnSelection);
                IReadOnlySet<Guid> fullPoolIds = await GetFullPoolIdsAsync(preparedLines, poolsById, holdPolicies, token);
                if (fullPoolIds.Any(poolId => holdPolicies[poolId] == CapacityHoldPolicyEnum.TimedHoldOnSelection))
                {
                    return CapacityUnavailable(request.EventId);
                }

                bool isWaitlisted = fullPoolIds.Any(poolId => holdPolicies[poolId] == CapacityHoldPolicyEnum.WaitlistWhenFull);
                bool submitsForApproval = holdPolicies.Values.Any(policy => policy == CapacityHoldPolicyEnum.ApprovalNoHold);
                if (isWaitlisted && submitsForApproval)
                {
                    return IncompatiblePolicy(request.EventId);
                }

                DateTime? expiresAt = !isWaitlisted && reservesCapacityOnSelection
                    ? poolsById.Values
                        .Where(pool => holdPolicies[pool.Id] == CapacityHoldPolicyEnum.TimedHoldOnSelection)
                        .Min(pool => createdAt.AddSeconds(pool.HoldDurationSeconds))
                    : null;
                RegistrationOrder order = RegistrationOrder.Create(
                    orderId,
                    tenant.TenantId,
                    request.EventId,
                    request.AccountUserId,
                    request.PurchaserActorId,
                    request.BookingPartyType,
                    catalog.Id,
                    participation,
                    registrationWorkflowVersionId: null,
                    request.GuestAccessTokenHash,
                    catalog.CurrencyCode,
                    createdAt,
                    expiresAt);

                foreach (PreparedLine preparedLine in preparedLines)
                {
                    order.AddLine(RegistrationOrderLine.Create(
                        preparedLine.Ids.LineId,
                        catalog,
                        preparedLine.Ticket,
                        order.Id,
                        preparedLine.Selection.Quantity,
                        preparedLine.Selection.ChosenUnitPriceMinor,
                        feePolicy));
                }

                long lineTotal = order.Lines.Sum(line => line.LineSubtotalSnapshot);
                OrganizerEarnings earnings = earningsCalculator.Calculate(catalog.CurrencyCode, lineTotal, feePolicy);
                order.ApplyTotals(RegistrationOrderTotalsSnapshot.Create(
                    catalog.CurrencyCode,
                    earnings.OrganizerDirectedTotalMinor,
                    earnings.PlatformFeeMinor,
                    earnings.OrganizerEarningsMinor,
                    platformContributionTotalMinor: 0));

                RegistrationInventoryHold[] holds = isWaitlisted || !reservesCapacityOnSelection
                    ? []
                    : CreateHolds(order, preparedLines, poolsById, holdPolicies, createdAt);
                TransitionForCreation(
                    order,
                    isWaitlisted,
                    submitsForApproval,
                    createdAt);
                await inventory.AddOrderWithHoldsAsync(order, holds, token);
                await inventory.SaveChangesAsync(token);
                return Success(order.Id, isWaitlisted ? "Registration order waitlisted." : "Registration order created.");
            }, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return Invalid(request.EventId, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Invalid(request.EventId, exception.Message);
        }
    }

    private async Task<IReadOnlySet<Guid>> GetFullPoolIdsAsync(
        IReadOnlyCollection<PreparedLine> lines,
        Dictionary<Guid, EventCapacityPool> poolsById,
        IReadOnlyDictionary<Guid, CapacityHoldPolicyEnum> holdPolicies,
        CancellationToken cancellationToken)
    {
        var fullPoolIds = new HashSet<Guid>();
        foreach (IGrouping<Guid, PreparedLine> group in lines
            .Where(line => line.Ticket.CapacityPoolId.HasValue)
            .Where(line => ChecksCapacityOnSelection(holdPolicies[line.Ticket.CapacityPoolId!.Value]))
            .GroupBy(line => line.Ticket.CapacityPoolId!.Value)
            .OrderBy(group => group.Key))
        {
            EventCapacityPool pool = poolsById[group.Key];
            if (pool.MaximumQuantity is null ||
                pool.CapacityOversellPolicyId == (int)CapacityOversellPolicyEnum.Allow)
            {
                continue;
            }

            int allocated = await inventory.GetAllocatedQuantityAsync(pool.Id, tenant.TenantId, cancellationToken);
            int requested = checked(group.Sum(line => line.Selection.Quantity));
            if (allocated > pool.MaximumQuantity.Value - requested)
            {
                fullPoolIds.Add(pool.Id);
            }
        }

        return fullPoolIds;
    }

    private static RegistrationInventoryHold[] CreateHolds(
        RegistrationOrder order,
        IEnumerable<PreparedLine> lines,
        Dictionary<Guid, EventCapacityPool> poolsById,
        IReadOnlyDictionary<Guid, CapacityHoldPolicyEnum> holdPolicies,
        DateTime createdAt) => lines
        .Where(line => line.Ticket.CapacityPoolId.HasValue)
        .Where(line => ReservesCapacityOnSelection(holdPolicies[line.Ticket.CapacityPoolId!.Value]))
        .Select(line =>
        {
            EventCapacityPool pool = poolsById[line.Ticket.CapacityPoolId!.Value];
            return RegistrationInventoryHold.Create(
                line.Ids.HoldId,
                order.Id,
                pool.Id,
                line.Ticket.Id,
                order.TenantId,
                line.Selection.Quantity,
                createdAt,
                createdAt.AddSeconds(pool.HoldDurationSeconds));
        })
        .ToArray();

    private static void TransitionForCreation(
        RegistrationOrder order,
        bool isFull,
        bool submitsForApproval,
        DateTime createdAt)
    {
        order.TransitionTo(RegistrationOrderStatusEnum.AwaitingParticipantDetails, createdAt);
        if (submitsForApproval)
        {
            order.TransitionTo(RegistrationOrderStatusEnum.AwaitingRequirements, createdAt);
            order.TransitionTo(RegistrationOrderStatusEnum.AwaitingApproval, createdAt);
            return;
        }

        if (!isFull)
        {
            return;
        }

        order.TransitionTo(RegistrationOrderStatusEnum.AwaitingRequirements, createdAt);
        order.TransitionTo(RegistrationOrderStatusEnum.ReadyForCheckout, createdAt);
        order.TransitionTo(RegistrationOrderStatusEnum.Waitlisted, createdAt);
    }

    private static bool TryGetHoldPolicies(
        IReadOnlyDictionary<Guid, EventCapacityPool> poolsById,
        out Dictionary<Guid, CapacityHoldPolicyEnum> holdPolicies)
    {
        holdPolicies = poolsById.ToDictionary(pool => pool.Key, pool => (CapacityHoldPolicyEnum)pool.Value.CapacityHoldPolicyId);
        return holdPolicies.Values.All(policy => Enum.IsDefined(policy));
    }

    private static bool ChecksCapacityOnSelection(CapacityHoldPolicyEnum policy) => policy is
        CapacityHoldPolicyEnum.TimedHoldOnSelection or CapacityHoldPolicyEnum.WaitlistWhenFull;

    private static bool ReservesCapacityOnSelection(CapacityHoldPolicyEnum policy) =>
        policy == CapacityHoldPolicyEnum.TimedHoldOnSelection;

    private static bool TryGetLimitViolation(
        CreateRegistrationOrderWithHoldCommand request,
        IEnumerable<PreparedLine> lines,
        IReadOnlyDictionary<Guid, RegistrationTicketLimitUsage> usageByTicket,
        out string? error)
    {
        foreach (PreparedLine line in lines)
        {
            usageByTicket.TryGetValue(line.Ticket.Id, out RegistrationTicketLimitUsage? usage);
            int accountQuantity = usage?.AccountQuantity ?? 0;
            int verifiedContactQuantity = usage?.VerifiedContactQuantity ?? 0;
            int bookingPartyQuantity = usage?.BookingPartyQuantity ?? 0;

            if ((request.AccountUserId.HasValue && line.Ticket.PerAccountLimit is int accountLimit &&
                 accountQuantity > accountLimit - line.Selection.Quantity) ||
                (request.VerifiedContactNormalizedEmail is not null && line.Ticket.PerVerifiedContactLimit is int contactLimit &&
                 verifiedContactQuantity > contactLimit - line.Selection.Quantity) ||
                (request.PurchaserActorId.HasValue && line.Ticket.PerBookingPartyLimit is int bookingPartyLimit &&
                 bookingPartyQuantity > bookingPartyLimit - line.Selection.Quantity))
            {
                error = "A ticket quantity exceeds an account, verified-contact, or booking-party limit.";
                return true;
            }
        }

        error = null;
        return false;
    }

    private static string? NormalizeVerifiedContact(string? verifiedContactNormalizedEmail) =>
        string.IsNullOrWhiteSpace(verifiedContactNormalizedEmail)
            ? null
            : verifiedContactNormalizedEmail.Trim().ToUpperInvariant();

    private static BaseCommandResponse<Guid> Success(Guid orderId, string message) => new()
    {
        Id = orderId,
        Success = true,
        Message = message
    };

    private static BaseCommandResponse<Guid> Missing(Guid id) => new()
    {
        Id = id,
        Success = false,
        FailureCode = "registration_order_not_found",
        Message = "Registration order configuration was not found.",
        Errors = ["Registration order configuration was not found."]
    };

    private static BaseCommandResponse<Guid> Invalid(Guid id, string error) => Invalid(id, [error]);

    private static BaseCommandResponse<Guid> Invalid(Guid id, IEnumerable<string> errors) => new()
    {
        Id = id,
        Success = false,
        FailureCode = "registration_order_validation_failed",
        Message = "Registration order configuration is invalid.",
        Errors = errors.ToList()
    };

    private static BaseCommandResponse<Guid> LimitExceeded(Guid id, string error) => new()
    {
        Id = id,
        Success = false,
        FailureCode = "registration_order_limit_exceeded",
        Message = "Registration order quantity limit was exceeded.",
        Errors = [error]
    };

    private static BaseCommandResponse<Guid> CapacityUnavailable(Guid id) => new()
    {
        Id = id,
        Success = false,
        FailureCode = "registration_order_capacity_unavailable",
        Message = "Registration capacity is unavailable.",
        Errors = ["Registration capacity is unavailable."]
    };

    private static BaseCommandResponse<Guid> IncompatiblePolicy(Guid id) => new()
    {
        Id = id,
        Success = false,
        FailureCode = "registration_order_policy_incompatible",
        Message = "Registration capacity policies cannot produce one consistent order state.",
        Errors = ["A waitlisted order cannot also await approval."]
    };

    private sealed record StableLineIds(Guid LineId, Guid HoldId);

    private sealed record PreparedLine(
        RegistrationOrderLineSelection Selection,
        EventTicketType Ticket,
        StableLineIds Ids);
}
