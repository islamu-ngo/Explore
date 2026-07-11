// ABOUTME: Lookup-table entity for external API key lifecycle statuses used by authentication and management flows.
// ABOUTME: Stores metadata for each status including whether it represents a usable state for API key authentication.

namespace Explore.Domain;

public class ExternalApiKeyStatus
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
    public bool IsUsable { get; set; }
}
