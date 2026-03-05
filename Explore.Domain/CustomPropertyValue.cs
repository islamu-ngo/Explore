// ABOUTME: EAV entity storing a custom property value for a specific entity instance.
// ABOUTME: Uses typed columns (TextValue, NumberValue, etc.) with polymorphic EntityId (no DB FK).

using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class CustomPropertyValue : ITenantEntity, IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; set; }

    [ForeignKey(nameof(Definition))]
    public Guid CustomPropertyDefinitionId { get; set; }
    public CustomPropertyDefinition? Definition { get; set; }

    /// <summary>
    /// Polymorphic reference to Event.Id, Organization.Id, or Group.Id.
    /// Discriminated by the parent definition's EntityTypeName. No DB FK constraint.
    /// </summary>
    public Guid EntityId { get; set; }

    // Typed value columns — only one is populated per row, determined by Definition.PropertyType
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateTimeOffset? DateTimeValue { get; set; }

    [ForeignKey(nameof(Option))]
    public Guid? OptionId { get; set; }
    public CustomPropertyOption? Option { get; set; }

    // Tenant
    [ForeignKey(nameof(Tenant))]
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
