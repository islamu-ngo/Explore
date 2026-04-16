// ABOUTME: Lookup classifying an agenda item (Intro, Talk, Q&A, Break, Prayer, Outro, Logistics, Custom).
// ABOUTME: Shared by event-level EventAgendaItem and session-level EventSessionAgendaItem.

namespace Explore.Domain;

public class ScheduleItemKind
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
