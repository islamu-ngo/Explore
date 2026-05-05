// ABOUTME: Lookup-table entity for notification recipient scope classifiers.
// ABOUTME: Replaces reuse of ActorType for personal, organization, group, and system notification scopes.

namespace Explore.Domain;

public class NotificationScopeType
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
