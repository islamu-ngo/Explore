// ABOUTME: Lookup-table entity for external API key credit renewal periods.
// ABOUTME: Defines how often credit quotas reset (None, Daily, Weekly, Monthly, Yearly).

namespace Explore.Domain;

public class ExternalApiKeyCreditPeriod
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
