using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Enums;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class Tenant : IAuditableEntity
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }

    [ForeignKey("TenantStatus")]
    public int TenantStatusId { get; set; }
    public required TenantStatus TenantStatus { get; set; }

    [NotMapped]
    public bool IsActive => TenantStatus?.IsActiveState ?? TenantStatusId == (int)TenantStatusEnum.Active;

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Collection of customizable navigation links for this tenant.
    /// </summary>
    public ICollection<TenantNavigationLink> NavigationLinks { get; private set; } = new List<TenantNavigationLink>();
}
