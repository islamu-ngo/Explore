// ABOUTME: Read model for an event capacity pool in a ticket catalog.
// ABOUTME: Includes the persisted pool identifier for management responses.
namespace Explore.Application.DTOs.EventTicketing;

public sealed class EventCapacityPoolDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int? MaximumQuantity { get; init; }
    public int HoldDurationSeconds { get; init; }
    public int CapacityOversellPolicyId { get; init; }
    public bool IsActive { get; init; }
}
