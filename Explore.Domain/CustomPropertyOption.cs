// ABOUTME: EAV entity representing a selectable option for Option-type custom properties.
// ABOUTME: Supports hierarchical options via self-referencing ParentOptionId.

using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class CustomPropertyOption : IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; set; }

    [ForeignKey(nameof(Definition))]
    public Guid CustomPropertyDefinitionId { get; set; }
    public CustomPropertyDefinition? Definition { get; set; }

    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Value { get; set; }

    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }

    [ForeignKey(nameof(ParentOption))]
    public Guid? ParentOptionId { get; set; }
    public CustomPropertyOption? ParentOption { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Navigation — child options
    private readonly List<CustomPropertyOption> _childOptions = [];
    public IReadOnlyCollection<CustomPropertyOption> ChildOptions => _childOptions.AsReadOnly();
}
