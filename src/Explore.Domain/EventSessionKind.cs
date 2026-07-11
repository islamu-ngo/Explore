// ABOUTME: Lookup classifying a program item/session (talk, workshop, panel, activity, etc.).
// ABOUTME: Used by event session composer flows to present event-appropriate labels and options.

namespace Explore.Domain;

public class EventSessionKind
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
