// ABOUTME: Defines the tenant-scoped registration-order aggregate for buyer, ticket-line, and pre-payment workflow facts.
// ABOUTME: Keeps purchaser PII and platform contributions separate while pinning participation, catalog, and totals snapshots.

using Explore.Domain.Enums;
using Explore.Domain.Interfaces;
using Explore.Domain.Services.Registration;
using Explore.Domain.ValueObjects;

namespace Explore.Domain;

public sealed class RegistrationOrder : ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    private readonly List<RegistrationOrderLine> _lines = [];

    private RegistrationOrder()
    {
    }

    private RegistrationOrder(
        Guid id,
        Guid tenantId,
        Guid eventId,
        Guid? accountUserId,
        Guid? purchaserActorId,
        BookingPartyTypeEnum bookingPartyType,
        Guid ticketCatalogVersionId,
        RegistrationParticipationSnapshot participationSnapshot,
        Guid? registrationWorkflowVersionId,
        CapabilityTokenHash? guestAccessTokenHash,
        string currencyCode,
        DateTime createdAt,
        DateTime? expiresAt)
    {
        Id = id;
        TenantId = tenantId;
        EventId = eventId;
        AccountUserId = accountUserId;
        PurchaserActorId = purchaserActorId;
        BookingPartyTypeId = (int)bookingPartyType;
        TicketCatalogVersionId = ticketCatalogVersionId;
        ParticipationSnapshot = participationSnapshot;
        ParticipationConfigurationVersionSnapshot = participationSnapshot.ConfigurationVersion;
        RegistrationWorkflowVersionId = registrationWorkflowVersionId;
        GuestAccessTokenHash = guestAccessTokenHash;
        CurrencyCode = currencyCode;
        RegistrationOrderStatusId = (int)RegistrationOrderStatusEnum.Draft;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; set; }

    public Guid EventId { get; private set; }

    public Guid? AccountUserId { get; private set; }

    public Guid? PurchaserActorId { get; private set; }

    public int BookingPartyTypeId { get; private set; }

    public BookingPartyType? BookingPartyType { get; private set; }

    public int RegistrationOrderStatusId { get; private set; }

    public RegistrationOrderStatus? RegistrationOrderStatus { get; private set; }

    public Guid TicketCatalogVersionId { get; private set; }

    public RegistrationParticipationSnapshot ParticipationSnapshot { get; private set; } = null!;

    public Guid ParticipationConfigurationVersionSnapshot { get; private set; }

    public Guid? RegistrationWorkflowVersionId { get; private set; }

    public CapabilityTokenHash? GuestAccessTokenHash { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public DateTime? ExpiresAt { get; private set; }

    public DateTime? SubmittedAt { get; private set; }

    public DateTime? ConfirmedAt { get; private set; }

    public DateTime? RejectedAt { get; private set; }

    public DateTime? CancelledAt { get; private set; }

    public RegistrationOrderPii? Pii { get; private set; }

    public RegistrationOrderPlatformContribution? PlatformContribution { get; private set; }

    public long OrganizerDirectedTotalMinorSnapshot { get; private set; }

    public long PlatformFeeTotalMinorSnapshot { get; private set; }

    public long OrganizerEarningsTotalMinorSnapshot { get; private set; }

    public long PlatformContributionTotalMinorSnapshot { get; private set; }

    public long TotalDueMinorSnapshot { get; private set; }

    public IReadOnlyCollection<RegistrationOrderLine> Lines => _lines.AsReadOnly();

    public Guid ConcurrencyStamp { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public static RegistrationOrder Create(
        Guid tenantId,
        Guid eventId,
        Guid? accountUserId,
        Guid? purchaserActorId,
        BookingPartyTypeEnum bookingPartyType,
        Guid ticketCatalogVersionId,
        RegistrationParticipationSnapshot participationSnapshot,
        Guid? registrationWorkflowVersionId,
        CapabilityTokenHash? guestAccessTokenHash,
        string currencyCode,
        DateTime createdAt,
        DateTime? expiresAt) => Create(
        Guid.CreateVersion7(),
        tenantId,
        eventId,
        accountUserId,
        purchaserActorId,
        bookingPartyType,
        ticketCatalogVersionId,
        participationSnapshot,
        registrationWorkflowVersionId,
        guestAccessTokenHash,
        currencyCode,
        createdAt,
        expiresAt);

    public static RegistrationOrder Create(
        Guid id,
        Guid tenantId,
        Guid eventId,
        Guid? accountUserId,
        Guid? purchaserActorId,
        BookingPartyTypeEnum bookingPartyType,
        Guid ticketCatalogVersionId,
        RegistrationParticipationSnapshot participationSnapshot,
        Guid? registrationWorkflowVersionId,
        CapabilityTokenHash? guestAccessTokenHash,
        string currencyCode,
        DateTime createdAt,
        DateTime? expiresAt)
    {
        if (id == Guid.Empty || tenantId == Guid.Empty || eventId == Guid.Empty || ticketCatalogVersionId == Guid.Empty ||
            accountUserId == Guid.Empty || purchaserActorId == Guid.Empty || registrationWorkflowVersionId == Guid.Empty ||
            !Enum.IsDefined(bookingPartyType) || (accountUserId is null && guestAccessTokenHash is null))
        {
            throw new ArgumentException("Registration order identity and booking facts are invalid.");
        }

        ArgumentNullException.ThrowIfNull(participationSnapshot);
        DateTime normalizedCreatedAt = EnsureUtc(createdAt, nameof(createdAt));
        DateTime? normalizedExpiresAt = expiresAt.HasValue ? EnsureUtc(expiresAt.Value, nameof(expiresAt)) : null;
        if (normalizedExpiresAt.HasValue && normalizedExpiresAt.Value <= normalizedCreatedAt)
        {
            throw new ArgumentException("Order expiry must be after creation.", nameof(expiresAt));
        }

        return new RegistrationOrder(
            id,
            tenantId,
            eventId,
            accountUserId,
            purchaserActorId,
            bookingPartyType,
            ticketCatalogVersionId,
            participationSnapshot,
            registrationWorkflowVersionId,
            guestAccessTokenHash,
            CurrencyMetadata.Get(currencyCode).Code,
            normalizedCreatedAt,
            normalizedExpiresAt);
    }

    public void AddLine(RegistrationOrderLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        EnsureCommercialFactsMutable();

        if (line.RegistrationOrderId != Id || line.TenantId != TenantId || line.TicketCatalogVersionId != TicketCatalogVersionId ||
            !string.Equals(line.CurrencyCodeSnapshot, CurrencyCode, StringComparison.Ordinal) ||
            _lines.Any(existing => existing.TicketTypeId == line.TicketTypeId))
        {
            throw new ArgumentException("Order line does not match the pinned order commercial context.", nameof(line));
        }

        _lines.Add(line);
    }

    public void SetPii(RegistrationOrderPii pii)
    {
        ArgumentNullException.ThrowIfNull(pii);
        if (pii.RegistrationOrderId != Id || pii.TenantId != TenantId)
        {
            throw new ArgumentException("Purchaser PII does not belong to this order.", nameof(pii));
        }


        Pii = pii;
    }

    public bool TryLinkGuestOrderToAccount(Guid accountUserId, string verifiedNormalizedEmail)
    {
        if (accountUserId == Guid.Empty || string.IsNullOrWhiteSpace(verifiedNormalizedEmail) || GuestAccessTokenHash is null)
        {
            throw new ArgumentException("Guest order linking requires an account, verified email, and guest order capability.");
        }

        if (!string.Equals(Pii?.NormalizedEmail, verifiedNormalizedEmail.Trim().ToUpperInvariant(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The verified account email does not match the registration order contact email.");
        }

        if (AccountUserId == accountUserId)
        {
            return false;
        }

        if (AccountUserId.HasValue)
        {
            throw new InvalidOperationException("Registration order is already linked to another account.");
        }

        AccountUserId = accountUserId;
        BumpConcurrency(Guid.CreateVersion7());
        return true;
    }

    public void SetPlatformContribution(RegistrationOrderPlatformContribution? contribution)
    {
        EnsureCommercialFactsMutable();
        if (contribution is not null &&
            (contribution.RegistrationOrderId != Id || contribution.TenantId != TenantId || contribution.CurrencyCode != CurrencyCode || contribution.AmountMinor <= 0))
        {
            throw new ArgumentException("Platform contribution does not match the order.", nameof(contribution));
        }

        PlatformContribution = contribution;
    }

    public void ApplyTotals(RegistrationOrderTotalsSnapshot totals)
    {
        ArgumentNullException.ThrowIfNull(totals);
        EnsureCommercialFactsMutable();

        long lineTotal = _lines.Aggregate(0L, static (total, line) => MinorUnitMath.Add(total, line.LineSubtotalSnapshot));
        long contributionTotal = PlatformContribution?.AmountMinor ?? 0;
        if (!string.Equals(totals.CurrencyCode, CurrencyCode, StringComparison.Ordinal) ||
            totals.OrganizerDirectedTotalMinor != lineTotal || totals.PlatformContributionTotalMinor != contributionTotal)
        {
            throw new ArgumentException("Order totals do not match the pinned order snapshots.", nameof(totals));
        }

        OrganizerDirectedTotalMinorSnapshot = totals.OrganizerDirectedTotalMinor;
        PlatformFeeTotalMinorSnapshot = totals.PlatformFeeTotalMinor;
        OrganizerEarningsTotalMinorSnapshot = totals.OrganizerEarningsTotalMinor;
        PlatformContributionTotalMinorSnapshot = totals.PlatformContributionTotalMinor;
        TotalDueMinorSnapshot = totals.TotalDueMinor;
    }

    public void TransitionTo(RegistrationOrderStatusEnum desiredStatus, DateTime timestamp)
    {
        DateTime utcTimestamp = EnsureUtc(timestamp, nameof(timestamp));
        RegistrationOrderStatusEnum currentStatus = (RegistrationOrderStatusEnum)RegistrationOrderStatusId;
        RegistrationOrderRules.EnsureCanTransition(currentStatus, desiredStatus);

        if (currentStatus == desiredStatus)
        {
            return;
        }

        if (currentStatus == RegistrationOrderStatusEnum.ReadyForCheckout &&
            desiredStatus is RegistrationOrderStatusEnum.Confirmed or RegistrationOrderStatusEnum.AwaitingPayment &&
            desiredStatus != RegistrationOrderRules.GetCheckoutDestination(GetVerifiedTotalDueSnapshot()))
        {
            throw new InvalidOperationException("The order checkout destination does not match its snapshotted total.");
        }

        RegistrationOrderStatusId = (int)desiredStatus;

        if (desiredStatus is RegistrationOrderStatusEnum.AwaitingPayment or RegistrationOrderStatusEnum.AwaitingApproval or RegistrationOrderStatusEnum.Confirmed)
        {
            SubmittedAt ??= utcTimestamp;
        }

        if (desiredStatus == RegistrationOrderStatusEnum.Confirmed)
        {
            ConfirmedAt = utcTimestamp;
        }
        else if (desiredStatus == RegistrationOrderStatusEnum.Rejected)
        {
            RejectedAt = utcTimestamp;
        }
        else if (desiredStatus == RegistrationOrderStatusEnum.Cancelled)
        {
            CancelledAt = utcTimestamp;
        }
    }

    public bool TryBeginHoldExpiryRecovery(DateTime timestamp)
    {
        RegistrationOrderStatusEnum currentStatus = (RegistrationOrderStatusEnum)RegistrationOrderStatusId;
        if (currentStatus == RegistrationOrderStatusEnum.NeedsReconciliation ||
            !RegistrationOrderRules.CanTransition(currentStatus, RegistrationOrderStatusEnum.NeedsReconciliation))
        {
            return false;
        }

        TransitionTo(RegistrationOrderStatusEnum.NeedsReconciliation, timestamp);
        return true;
    }

    public bool TryResolveHoldExpiryRecovery(bool capacityReReserved, DateTime timestamp)
    {
        if ((RegistrationOrderStatusEnum)RegistrationOrderStatusId != RegistrationOrderStatusEnum.NeedsReconciliation)
        {
            return false;
        }

        TransitionTo(
            capacityReReserved
                ? RegistrationOrderStatusEnum.ReadyForCheckout
                : RegistrationOrderStatusEnum.Waitlisted,
            timestamp);
        return true;
    }

    public void BumpConcurrency(Guid concurrencyStamp)
    {
        if (concurrencyStamp == Guid.Empty)
        {
            throw new ArgumentException("Order concurrency stamp is required.", nameof(concurrencyStamp));
        }

        ConcurrencyStamp = concurrencyStamp;
    }

    private void EnsureCommercialFactsMutable()
    {
        if (RegistrationOrderRules.IsTerminalForCurrentWorkstream((RegistrationOrderStatusEnum)RegistrationOrderStatusId))
        {
            throw new InvalidOperationException("Commercial facts are frozen after the order reaches its current workflow boundary.");
        }
    }

    private long GetVerifiedTotalDueSnapshot()
    {
        long lineTotal = _lines.Aggregate(0L, static (total, line) => MinorUnitMath.Add(total, line.LineSubtotalSnapshot));
        long contributionTotal = PlatformContribution?.AmountMinor ?? 0;
        long expectedTotalDue = MinorUnitMath.Add(lineTotal, contributionTotal);

        if (OrganizerDirectedTotalMinorSnapshot != lineTotal ||
            PlatformContributionTotalMinorSnapshot != contributionTotal ||
            TotalDueMinorSnapshot != expectedTotalDue ||
            PlatformFeeTotalMinorSnapshot < 0 ||
            PlatformFeeTotalMinorSnapshot > lineTotal ||
            OrganizerEarningsTotalMinorSnapshot != lineTotal - PlatformFeeTotalMinorSnapshot)
        {
            throw new InvalidOperationException("Order totals must match the immutable line and contribution snapshots before checkout.");
        }

        return TotalDueMinorSnapshot;
    }

    private static DateTime EnsureUtc(DateTime value, string parameterName)
    {
        if (value == default || value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be a non-default UTC value.", parameterName);
        }

        return value;
    }
}
