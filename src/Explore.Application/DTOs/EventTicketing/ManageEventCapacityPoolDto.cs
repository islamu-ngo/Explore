// ABOUTME: Write model for creating or updating an event capacity pool.
// ABOUTME: Omits the persisted pool identifier; update identity comes from the route.
namespace Explore.Application.DTOs.EventTicketing;

public sealed class ManageEventCapacityPoolDto
{
    public string Name { get; init; } = string.Empty;
    public int? MaximumQuantity { get; init; }
    public int HoldDurationSeconds { get; init; }
    public int CapacityHoldPolicyId { get; init; }
    public int CapacityOversellPolicyId { get; init; }
    public bool IsActive { get; init; }
}
