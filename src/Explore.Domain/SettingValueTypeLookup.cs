// ABOUTME: Lookup-table entity for system setting value data types.
// ABOUTME: IDs mirror SettingValueType values for validation and storage metadata.

namespace Explore.Domain;

public class SettingValueTypeLookup
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
