// ABOUTME: Lookup-table entity for tenant plan version lifecycle statuses.
// ABOUTME: Keeps SaaS tier draft, published, and archived states normalized in persistence.

namespace Explore.Domain;

public class TenantPlanStatus
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
    public bool AllowsProvisioning { get; set; }
}
