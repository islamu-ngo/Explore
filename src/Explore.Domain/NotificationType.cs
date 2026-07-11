// ABOUTME: Lookup entity for notification types (e.g., RegistrationConfirmed, ApprovalGranted).
// ABOUTME: Follows the same pattern as ApprovalStatus — seeded via LookupTableSeeder.

namespace Explore.Domain;

public class NotificationType
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
