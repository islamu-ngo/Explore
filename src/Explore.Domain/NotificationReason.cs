// ABOUTME: Lookup entity for notification reasons (e.g., Mention, Assignment, Subscription).
// ABOUTME: Follows the same pattern as NotificationType — seeded via LookupTableSeeder.

namespace Explore.Domain;

public class NotificationReason
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
