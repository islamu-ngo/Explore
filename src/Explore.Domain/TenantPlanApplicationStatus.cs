// ABOUTME: Lookup-table entity for tenant plan application audit outcomes.
// ABOUTME: Normalizes successful and failed plan apply or rollback attempts.

namespace Explore.Domain;

public class TenantPlanApplicationStatus
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
    public bool IsSuccessful { get; set; }
}
