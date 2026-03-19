// ABOUTME: Shared Layer 3 custom-property option for a tenant-scoped definition.
// ABOUTME: Uses namespaced machine keys so labels can change without breaking semantics.

using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class CustomPropertyOption : IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; set; }

    [ForeignKey(nameof(Definition))]
    public Guid CustomPropertyDefinitionId { get; set; }
    public CustomPropertyDefinition? Definition { get; set; }

    public required string Namespace { get; set; }
    public required string Key { get; set; }
    public required string DisplayName { get; set; }
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
