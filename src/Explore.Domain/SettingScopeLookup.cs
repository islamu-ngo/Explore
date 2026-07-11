// ABOUTME: Lookup-table entity for configuration and settings hierarchy scope levels.
// ABOUTME: Includes System plus cascading settings scopes used by audit and secret bindings.

namespace Explore.Domain;

public class SettingScopeLookup
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
