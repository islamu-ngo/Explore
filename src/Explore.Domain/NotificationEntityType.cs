// ABOUTME: Lookup entity for notification entity types (e.g., Event, Organization, Group).
// ABOUTME: Follows the same pattern as ApprovalStatus — seeded via LookupTableSeeder.

namespace Explore.Domain;

public class NotificationEntityType
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
