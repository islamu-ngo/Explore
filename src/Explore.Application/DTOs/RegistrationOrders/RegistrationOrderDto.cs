// ABOUTME: Safe registration-order read model for lifecycle commands and future API assembly.
// ABOUTME: Excludes purchaser PII, guest capabilities, answers, and participant data by design.

using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.RegistrationOrders;

public sealed class RegistrationOrderDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Guid? AccountUserId { get; init; }
    public Guid? PurchaserActorId { get; init; }
    public int StatusId { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string StatusName { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = string.Empty;
    public long OrganizerDirectedTotalMinor { get; init; }
    public long PlatformFeeTotalMinor { get; init; }
    public long OrganizerEarningsTotalMinor { get; init; }
    public long PlatformContributionTotalMinor { get; init; }
    public long TotalDueMinor { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public DateTime? RejectedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public IReadOnlyList<RegistrationOrderLineDto> Lines { get; init; } = [];

    public static RegistrationOrderDto From(RegistrationOrder order, RegistrationOrderStatusEnum? statusOverride = null)
    {
        ArgumentNullException.ThrowIfNull(order);
        RegistrationOrderStatusEnum status = statusOverride ?? (RegistrationOrderStatusEnum)order.RegistrationOrderStatusId;
        return new RegistrationOrderDto
        {
            Id = order.Id,
            EventId = order.EventId,
            AccountUserId = order.AccountUserId,
            PurchaserActorId = order.PurchaserActorId,
            StatusId = (int)status,
            StatusCode = ToCode(status),
            StatusName = ToName(status),
            CurrencyCode = order.CurrencyCode,
            OrganizerDirectedTotalMinor = order.OrganizerDirectedTotalMinorSnapshot,
            PlatformFeeTotalMinor = order.PlatformFeeTotalMinorSnapshot,
            OrganizerEarningsTotalMinor = order.OrganizerEarningsTotalMinorSnapshot,
            PlatformContributionTotalMinor = order.PlatformContributionTotalMinorSnapshot,
            TotalDueMinor = order.TotalDueMinorSnapshot,
            ExpiresAt = order.ExpiresAt,
            SubmittedAt = order.SubmittedAt,
            ConfirmedAt = order.ConfirmedAt,
            RejectedAt = order.RejectedAt,
            CancelledAt = order.CancelledAt,
            Lines = order.Lines.Select(RegistrationOrderLineDto.From).ToArray()
        };
    }

    private static string ToCode(RegistrationOrderStatusEnum status) => status switch
    {
        RegistrationOrderStatusEnum.Draft => "DRAFT",
        RegistrationOrderStatusEnum.AwaitingIdentity => "AWAITING_IDENTITY",
        RegistrationOrderStatusEnum.AwaitingParticipantDetails => "AWAITING_PARTICIPANT_DETAILS",
        RegistrationOrderStatusEnum.AwaitingRequirements => "AWAITING_REQUIREMENTS",
        RegistrationOrderStatusEnum.ReadyForCheckout => "READY_FOR_CHECKOUT",
        RegistrationOrderStatusEnum.AwaitingPayment => "AWAITING_PAYMENT",
        RegistrationOrderStatusEnum.AwaitingApproval => "AWAITING_APPROVAL",
        RegistrationOrderStatusEnum.Waitlisted => "WAITLISTED",
        RegistrationOrderStatusEnum.Confirmed => "CONFIRMED",
        RegistrationOrderStatusEnum.Rejected => "REJECTED",
        RegistrationOrderStatusEnum.Expired => "EXPIRED",
        RegistrationOrderStatusEnum.Cancelled => "CANCELLED",
        RegistrationOrderStatusEnum.NeedsReconciliation => "NEEDS_RECONCILIATION",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private static string ToName(RegistrationOrderStatusEnum status) => status switch
    {
        RegistrationOrderStatusEnum.Draft => "Draft",
        RegistrationOrderStatusEnum.AwaitingIdentity => "Awaiting identity",
        RegistrationOrderStatusEnum.AwaitingParticipantDetails => "Awaiting participant details",
        RegistrationOrderStatusEnum.AwaitingRequirements => "Awaiting requirements",
        RegistrationOrderStatusEnum.ReadyForCheckout => "Ready for checkout",
        RegistrationOrderStatusEnum.AwaitingPayment => "Awaiting payment",
        RegistrationOrderStatusEnum.AwaitingApproval => "Awaiting approval",
        RegistrationOrderStatusEnum.Waitlisted => "Waitlisted",
        RegistrationOrderStatusEnum.Confirmed => "Confirmed",
        RegistrationOrderStatusEnum.Rejected => "Rejected",
        RegistrationOrderStatusEnum.Expired => "Expired",
        RegistrationOrderStatusEnum.Cancelled => "Cancelled",
        RegistrationOrderStatusEnum.NeedsReconciliation => "Needs reconciliation",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}

public sealed class RegistrationOrderLineDto
{
    public Guid Id { get; init; }
    public Guid TicketTypeId { get; init; }
    public int Quantity { get; init; }
    public string TicketTypeName { get; init; } = string.Empty;
    public long UnitPriceMinor { get; init; }
    public long? ChosenUnitPriceMinor { get; init; }
    public long SubtotalMinor { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;

    public static RegistrationOrderLineDto From(RegistrationOrderLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        return new RegistrationOrderLineDto
        {
            Id = line.Id,
            TicketTypeId = line.TicketTypeId,
            Quantity = line.Quantity,
            TicketTypeName = line.TicketTypeNameSnapshot,
            UnitPriceMinor = line.UnitPriceAmountSnapshot,
            ChosenUnitPriceMinor = line.ChosenUnitPriceAmountSnapshot,
            SubtotalMinor = line.LineSubtotalSnapshot,
            CurrencyCode = line.CurrencyCodeSnapshot
        };
    }
}
