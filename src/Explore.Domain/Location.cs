// ABOUTME: Tenant-scoped physical place with optional PII and an irreversible privacy lifecycle.
// ABOUTME: Owns consent-backed Private Home identity, erasure tombstones, rooms, audit, and concurrency.

using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Enums;
using Explore.Domain.Interfaces;
using Explore.Domain.ValueObjects;

namespace Explore.Domain;

public class Location : ITenantEntity, IAuditableEntity, IConcurrencyAware
{
    public const string ErasedPrivateVenueLabel = "Private venue";
    private LocationPii? _pii;
    private string? _fullName;
    private string? _country;
    private string? _city;

    public Guid Id { get; set; }
    public required string FullName
    {
        get => _fullName ?? string.Empty;
        set => _fullName = GuardErasedValue(_fullName, value, nameof(FullName));
    }

    public required string Country
    {
        get => _country ?? string.Empty;
        set => _country = GuardErasedValue(_country, value, nameof(Country));
    }

    public required string City
    {
        get => _city ?? string.Empty;
        set => _city = GuardErasedValue(_city, value, nameof(City));
    }

    public LocationPii? Pii
    {
        get => _pii;
        set
        {
            if (value is not null && LocationPrivacyStateId == (int)LocationPrivacyStateEnum.Erased)
            {
                throw new InvalidOperationException("PII cannot be attached to an erased Location. Create a replacement Location instead.");
            }

            if (value is not null
                && LocationKindId == (int)LocationKindEnum.PrivateHome
                && OwnerUserId is null)
            {
                throw new InvalidOperationException("Active Private Home PII requires an owner.");
            }

            if (value is null
                && _pii is not null
                && LocationPrivacyStateId == (int)LocationPrivacyStateEnum.Active)
            {
                throw new InvalidOperationException("Active PII cannot be cleared without the Location erasure transition.");
            }

            if (value is not null
                && Id != Guid.Empty
                && value.LocationId != Guid.Empty
                && value.LocationId != Id)
            {
                throw new InvalidOperationException("Location PII must belong to the same Location aggregate.");
            }

            _pii = value;
            if (LocationPrivacyStateId != (int)LocationPrivacyStateEnum.Erased)
            {
                LocationPrivacyStateId = value is null
                    ? (int)LocationPrivacyStateEnum.NotProvided
                    : (int)LocationPrivacyStateEnum.Active;
            }
        }
    }

    [NotMapped]
    public string? Address
    {
        get => Pii?.Address;
        set => SetManualAddress(
            value ?? throw new ArgumentNullException(nameof(value)),
            RequireMutablePii().Postcode);
    }

    [NotMapped]
    public string? Postcode
    {
        get => Pii?.Postcode;
        set => SetManualAddress(
            RequireMutablePii().Address,
            value ?? throw new ArgumentNullException(nameof(value)));
    }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }

    [NotMapped]
    public double? Latitude
    {
        get => Pii?.Latitude;
    }

    [NotMapped]
    public double? Longitude
    {
        get => Pii?.Longitude;
    }

    public GeoCoordinate? GetCoordinate() => Pii?.GetCoordinate();

    public string? Timezone { get; set; }

    [ForeignKey(nameof(LocationKind))]
    public int LocationKindId { get; private set; } = (int)LocationKindEnum.Unclassified;
    public LocationKind? LocationKind { get; private set; }

    [ForeignKey(nameof(LocationPrivacyState))]
    public int LocationPrivacyStateId { get; private set; } = (int)LocationPrivacyStateEnum.NotProvided;
    public LocationPrivacyState? LocationPrivacyState { get; private set; }

    [ForeignKey(nameof(OwnerUser))]
    public Guid? OwnerUserId { get; private set; }
    public User? OwnerUser { get; private set; }
    public DateTime? PiiErasedAtUtc { get; private set; }
    public LocationPrivacyErasureReasonEnum? PiiErasureReason { get; private set; }

    public ICollection<LocationRoom> Rooms { get; set; } = new List<LocationRoom>();

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public Guid ConcurrencyStamp { get; set; }

    public void AttachPii(LocationPii pii)
    {
        ArgumentNullException.ThrowIfNull(pii);
        if (Id != Guid.Empty)
        {
            pii.LocationId = Id;
        }

        Pii = pii;
    }

    public void SetManualAddress(string address, string postcode) =>
        SetAddress(address, postcode, coordinate: null);

    public void SetProviderAddress(
        string address,
        string postcode,
        GeoCoordinate? coordinate) =>
        SetAddress(address, postcode, coordinate);

    private void SetAddress(
        string address,
        string postcode,
        GeoCoordinate? coordinate)
    {
        EnsureNotErased();
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(postcode);

        if (Pii is null)
        {
            AttachPii(LocationPii.Create(address, postcode, coordinate));
            return;
        }

        Pii.SetAddress(address, postcode, coordinate);
    }

    public void ClassifyAsPrivateHome(Guid currentUserId)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new ArgumentException("Current user id is required.", nameof(currentUserId));
        }

        EnsureNotErased();
        if (LocationKindId == (int)LocationKindEnum.PrivateHome)
        {
            if (OwnerUserId == currentUserId)
            {
                return;
            }

            if (OwnerUserId.HasValue)
            {
                throw new InvalidOperationException("Private Home ownership changes require explicit owner consent.");
            }
        }

        LocationKindId = (int)LocationKindEnum.PrivateHome;
        OwnerUserId = currentUserId;
        OwnerUser = null;
    }

    public void ClassifyAs(LocationKindEnum kind)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (kind == LocationKindEnum.PrivateHome)
        {
            throw new ArgumentException("Private Home classification requires the current owner.", nameof(kind));
        }

        EnsureNotErased();
        if (LocationKindId == (int)LocationKindEnum.PrivateHome && Pii is not null)
        {
            throw new InvalidOperationException("An active Private Home cannot be reclassified to bypass ownership and erasure.");
        }

        LocationKindId = (int)kind;
        OwnerUserId = null;
        OwnerUser = null;
    }

    public void TransferPrivateHomeOwnership(LocationOwnershipConsent consent)
    {
        ArgumentNullException.ThrowIfNull(consent);
        EnsureNotErased();

        if (LocationKindId != (int)LocationKindEnum.PrivateHome)
        {
            throw new InvalidOperationException("Only Private Home ownership can be transferred.");
        }

        if (consent.NewOwnerUserId == Guid.Empty
            || consent.ConsentedByUserId != consent.NewOwnerUserId
            || consent.ConsentedAtUtc == default
            || string.IsNullOrWhiteSpace(consent.ConsentVersion))
        {
            throw new ArgumentException("The new owner must provide explicit versioned consent.", nameof(consent));
        }

        OwnerUserId = consent.NewOwnerUserId;
        OwnerUser = null;
        UpdatedAt = consent.ConsentedAtUtc.ToUniversalTime();
        UpdatedBy = consent.ConsentedByUserId;
    }

    public void EraseOwnedPii(DateTime erasedAtUtc, LocationPrivacyErasureReasonEnum reason)
    {
        if (LocationPrivacyStateId == (int)LocationPrivacyStateEnum.Erased)
        {
            return;
        }

        if (LocationKindId != (int)LocationKindEnum.PrivateHome)
        {
            throw new InvalidOperationException("Owned PII erasure applies only to Private Home locations.");
        }

        if (erasedAtUtc == default)
        {
            throw new ArgumentException("Erasure timestamp is required.", nameof(erasedAtUtc));
        }

        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        var priorOwnerId = OwnerUserId;
        if (Rooms.Any(room => room.Id == Guid.Empty)
            || Rooms.Select(room => room.Id).Distinct().Count() != Rooms.Count)
        {
            throw new InvalidOperationException("Every room requires a unique persisted id before privacy erasure.");
        }

        _pii = null;
        OwnerUserId = null;
        OwnerUser = null;
        LocationPrivacyStateId = (int)LocationPrivacyStateEnum.Erased;
        PiiErasedAtUtc = erasedAtUtc.ToUniversalTime();
        PiiErasureReason = reason;
        _fullName = ErasedPrivateVenueLabel;
        _city = string.Empty;

        foreach (var room in Rooms)
        {
            room.TombstoneForPrivacyErasure(PiiErasedAtUtc.Value, priorOwnerId);
        }

        UpdatedAt = PiiErasedAtUtc;
        UpdatedBy = priorOwnerId;
        ConcurrencyStamp = Guid.CreateVersion7();
    }

    private LocationPii RequireMutablePii()
    {
        EnsureNotErased();
        return Pii ?? throw new InvalidOperationException("Location PII has not been provided.");
    }

    private void EnsureNotErased()
    {
        if (LocationPrivacyStateId == (int)LocationPrivacyStateEnum.Erased)
        {
            throw new InvalidOperationException("An erased Location cannot be changed or resurrected.");
        }
    }

    private string GuardErasedValue(string? currentValue, string value, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (LocationPrivacyStateId != (int)LocationPrivacyStateEnum.Erased)
        {
            return value;
        }

        var expectedErasedValue = propertyName switch
        {
            nameof(FullName) => ErasedPrivateVenueLabel,
            nameof(City) => string.Empty,
            _ => currentValue
        };
        if (expectedErasedValue is not null
            && !string.Equals(expectedErasedValue, value, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{propertyName} cannot be changed after Location erasure.");
        }

        return value;
    }
}
