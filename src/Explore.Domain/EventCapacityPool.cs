// ABOUTME: Defines an event-owned capacity resource that multiple ticket types may consume.
// ABOUTME: Keeps capacity scope explicit before atomic inventory holds are introduced.

using Explore.Domain.Enums;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public sealed class EventCapacityPool : ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    private EventCapacityPool()
    {
    }

    private EventCapacityPool(
        Guid tenantId,
        Guid eventId,
        string name,
        int? maximumQuantity,
        int holdDurationSeconds,
        CapacityOversellPolicyEnum oversellPolicy,
        bool isActive)
    {
        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        EventId = eventId;
        Name = name;
        MaximumQuantity = maximumQuantity;
        HoldDurationSeconds = holdDurationSeconds;
        CapacityOversellPolicyId = (int)oversellPolicy;
        IsActive = isActive;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; set; }

    public Guid EventId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int? MaximumQuantity { get; private set; }

    public int HoldDurationSeconds { get; private set; }

    public int CapacityOversellPolicyId { get; private set; }

    public CapacityOversellPolicy? CapacityOversellPolicy { get; private set; }

    public bool IsActive { get; private set; }

    public Guid ConcurrencyStamp { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public static EventCapacityPool Create(
        Guid tenantId,
        Guid eventId,
        string name,
        int? maximumQuantity,
        int holdDurationSeconds,
        CapacityOversellPolicyEnum oversellPolicy,
        bool isActive)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant is required.", nameof(tenantId));
        }

        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event is required.", nameof(eventId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Capacity pool name is required.", nameof(name));
        }

        if (maximumQuantity is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumQuantity), "Maximum quantity must be positive when provided.");
        }

        if (holdDurationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(holdDurationSeconds), "Hold duration must be positive.");
        }

        if (!Enum.IsDefined(oversellPolicy))
        {
            throw new ArgumentOutOfRangeException(nameof(oversellPolicy));
        }

        return new EventCapacityPool(tenantId, eventId, name.Trim(), maximumQuantity, holdDurationSeconds, oversellPolicy, isActive);
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    public void Delete(DateTime deletedAtUtc, Guid deletedBy)
    {
        if (deletedAtUtc == default)
        {
            throw new ArgumentException("Deletion timestamp is required.", nameof(deletedAtUtc));
        }

        if (deletedBy == Guid.Empty)
        {
            throw new ArgumentException("Deleting actor is required.", nameof(deletedBy));
        }

        if (IsDeleted)
        {
            return;
        }

        DateTime normalizedDeletedAt = deletedAtUtc.ToUniversalTime();
        IsDeleted = true;
        DeletedAt = normalizedDeletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = normalizedDeletedAt;
        UpdatedBy = deletedBy;
        ConcurrencyStamp = Guid.CreateVersion7();
    }

    public void Update(string name, int? maximumQuantity, int holdDurationSeconds, CapacityOversellPolicyEnum oversellPolicy, bool isActive)
    {
        var updated = Create(TenantId, EventId, name, maximumQuantity, holdDurationSeconds, oversellPolicy, isActive);
        Name = updated.Name; MaximumQuantity = updated.MaximumQuantity; HoldDurationSeconds = updated.HoldDurationSeconds; CapacityOversellPolicyId = updated.CapacityOversellPolicyId; IsActive = updated.IsActive;
    }
}
