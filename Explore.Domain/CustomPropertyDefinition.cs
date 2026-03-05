// ABOUTME: EAV entity defining a custom property that can be attached to Event, Organization, or Group.
// ABOUTME: Scoped by EntityTypeName + optional EventTypeId + TenantId, inspired by Plane's custom properties.

using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Enums;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class CustomPropertyDefinition : ITenantEntity, IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; set; }

    public EntityTypeName EntityTypeName { get; set; }

    [ForeignKey(nameof(EventType))]
    public int? EventTypeId { get; set; }
    public EventType? EventType { get; set; }

    [ForeignKey(nameof(Tenant))]
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }

    public PropertyType PropertyType { get; set; }

    public bool IsRequired { get; set; }
    public bool IsMulti { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }

    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Navigation — readonly collections
    private readonly List<CustomPropertyOption> _options = [];
    public IReadOnlyCollection<CustomPropertyOption> Options => _options.AsReadOnly();

    private readonly List<CustomPropertyValue> _values = [];
    public IReadOnlyCollection<CustomPropertyValue> Values => _values.AsReadOnly();
}
