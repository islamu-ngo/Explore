// ABOUTME: Lookup-table entity for external API key ownership levels.
// ABOUTME: IDs mirror ExternalApiKeyOwnerType values for machine-caller authorization.

namespace Explore.Domain;

public class ExternalApiKeyOwnerTypeLookup
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
