using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Domain;

public class Tenant
{
    public Guid Id { get; set; }
    public string FullName { get; set; }
    public string Slug { get; set; }
    public bool IsActive { get; set; }

    /// <summary>
    /// Collection of customizable navigation links for this tenant.
    /// </summary>
    public ICollection<TenantNavigationLink> NavigationLinks { get; set; } = new List<TenantNavigationLink>();
}
