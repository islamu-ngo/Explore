// ABOUTME: Lookup row for one supported notification preference delivery channel.
// ABOUTME: Restricts the matrix to explicitly supported channels such as Email and In-App.

namespace Explore.Domain;

public class NotificationPreferenceChannel
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
