// ABOUTME: Owns one immutable-on-publication ticket catalog revision for an event.
// ABOUTME: Provides draft-only ticket, entitlement, and pricing mutation plus independent draft cloning.

using Explore.Domain.Enums;
using Explore.Domain.Interfaces;
using Explore.Domain.Services.Registration;
using Explore.Domain.ValueObjects;

namespace Explore.Domain;

public sealed class EventTicketCatalogVersion : ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    private readonly List<EventTicketType> _ticketTypes = [];

    private EventTicketCatalogVersion()
    {
    }

    private EventTicketCatalogVersion(Guid tenantId, Guid eventId, string currencyCode, int versionNumber)
    {
        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        EventId = eventId;
        CurrencyCode = currencyCode;
        VersionNumber = versionNumber;
        TicketCatalogStatusId = (int)TicketCatalogStatusEnum.Draft;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; set; }

    public Guid EventId { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public int VersionNumber { get; private set; }

    public int TicketCatalogStatusId { get; private set; }

    public Guid ConcurrencyStamp { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public IReadOnlyCollection<EventTicketType> TicketTypes => _ticketTypes.AsReadOnly();

    public static EventTicketCatalogVersion Create(Guid tenantId, Guid eventId, string currencyCode, int versionNumber)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant is required.", nameof(tenantId));
        }

        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event is required.", nameof(eventId));
        }

        if (versionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(versionNumber));
        }

        return new EventTicketCatalogVersion(tenantId, eventId, CurrencyMetadata.Get(currencyCode).Code, versionNumber);
    }

    public void AddTicketType(EventTicketType ticketType, EventCapacityPool? capacityPool)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(ticketType);

        if (ticketType.CatalogId != Id || ticketType.TenantId != TenantId)
        {
            throw new ArgumentException("Ticket type must belong to this draft catalog and tenant.", nameof(ticketType));
        }

        if (!string.Equals(ticketType.CurrencyCode, CurrencyCode, StringComparison.Ordinal))
        {
            throw new ArgumentException("All ticket types in a catalog must use the catalog currency.", nameof(ticketType));
        }

        TicketCatalogRules.ValidateCapacityPool(this, capacityPool);
        ticketType.SetCapacityPool(capacityPool);
        _ticketTypes.Add(ticketType);
    }

    public void UpdateTicketPricing(
        EventTicketType ticketType,
        TicketPricingModeEnum pricingMode,
        long? fixedPriceMinor,
        long? minimumPriceMinor,
        long? suggestedPriceMinor)
    {
        EnsureDraft();
        EnsureContains(ticketType);
        ticketType.UpdatePricing(pricingMode, fixedPriceMinor, minimumPriceMinor, suggestedPriceMinor);
    }

    public void AddEntitlement(EventTicketType ticketType, TicketTypeEntitlement entitlement)
    {
        EnsureDraft();
        EnsureContains(ticketType);
        ArgumentNullException.ThrowIfNull(entitlement);

        TicketCatalogRules.ValidateEntitlement(this, ticketType, entitlement);
        ticketType.AddEntitlement(entitlement);
    }

    public void UpdateTicketType(EventTicketType ticketType, string name, TicketPricingModeEnum pricingMode, long? fixedPriceMinor, long? minimumPriceMinor, long? suggestedPriceMinor, ParticipantDataCollectionModeEnum participantDataCollectionMode, EventCapacityPool? capacityPool, int? minimumAge, int? maximumAge, bool requiresGuardian, bool requiresApproval, int? perOrderLimit, int? perAccountLimit, int? perVerifiedContactLimit, int? perBookingPartyLimit, IEnumerable<TicketTypeEntitlement> entitlements)
    {
        EnsureDraft(); EnsureContains(ticketType); TicketCatalogRules.ValidateCapacityPool(this, capacityPool);
        ticketType.Update(name, pricingMode, fixedPriceMinor, minimumPriceMinor, suggestedPriceMinor, participantDataCollectionMode, minimumAge, maximumAge, requiresGuardian, requiresApproval, perOrderLimit, perAccountLimit, perVerifiedContactLimit, perBookingPartyLimit);
        ticketType.SetCapacityPool(capacityPool);
        foreach (var entitlement in entitlements) TicketCatalogRules.ValidateEntitlement(this, ticketType, entitlement);
        ticketType.ReplaceEntitlements(entitlements);
    }

    public void DeleteTicketType(EventTicketType ticketType)
    {
        EnsureDraft(); EnsureContains(ticketType); ticketType.IsDeleted = true;
    }

    public void Publish()
    {
        EnsureDraft();
        TicketCatalogRules.ValidateForPublication(this);
        TicketCatalogStatusId = (int)TicketCatalogStatusEnum.Published;
    }

    public void Retire()
    {
        if (TicketCatalogStatusId != (int)TicketCatalogStatusEnum.Published)
        {
            throw new InvalidOperationException("Only a published ticket catalog can be retired.");
        }

        TicketCatalogStatusId = (int)TicketCatalogStatusEnum.Retired;
        ConcurrencyStamp = Guid.CreateVersion7();
    }

    public EventTicketCatalogVersion CloneToDraft()
    {
        if (TicketCatalogStatusId != (int)TicketCatalogStatusEnum.Published)
        {
            throw new InvalidOperationException("Only a published ticket catalog can be cloned into a draft.");
        }

        EventTicketCatalogVersion clone = Create(TenantId, EventId, CurrencyCode, checked(VersionNumber + 1));
        foreach (EventTicketType ticketType in _ticketTypes)
        {
            clone._ticketTypes.Add(ticketType.CloneTo(clone.Id));
        }

        return clone;
    }

    private void EnsureDraft()
    {
        if (TicketCatalogStatusId != (int)TicketCatalogStatusEnum.Draft)
        {
            throw new InvalidOperationException("Published or retired ticket catalogs are immutable.");
        }
    }

    private void EnsureContains(EventTicketType ticketType)
    {
        ArgumentNullException.ThrowIfNull(ticketType);
        if (!_ticketTypes.Contains(ticketType))
        {
            throw new ArgumentException("Ticket type does not belong to this catalog.", nameof(ticketType));
        }
    }
}
