// ABOUTME: First-class relational theme aggregate for platform-owned and tenant-owned UI themes.
// ABOUTME: Stores only curated palette tokens and metadata; selection still happens through hierarchical settings references.

namespace Explore.Domain;

using Explore.Domain.Interfaces;
using Explore.Domain.ValueObjects;

public class UiTheme : IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public required string ThemeKey { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public required UiThemePalette LightPalette { get; set; }
    public required UiThemePalette DarkPalette { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public uint RowVersion { get; set; }
}
