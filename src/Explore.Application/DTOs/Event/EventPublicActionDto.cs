// ABOUTME: API-facing projection of a reviewed external action attached to an event.
// ABOUTME: Exposes normalized lookup metadata and destination disclosure without capability flags.

namespace Explore.Application.DTOs.Event;

public sealed class EventPublicActionDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    public int KindId { get; set; }
    public string? KindCode { get; set; }
    public string? KindName { get; set; }
    public int HealthStateId { get; set; }
    public string? HealthStateCode { get; set; }
    public string? HealthStateName { get; set; }
    public required string Url { get; set; }
    public required string DestinationDomain { get; set; }
    public string? Label { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
}
