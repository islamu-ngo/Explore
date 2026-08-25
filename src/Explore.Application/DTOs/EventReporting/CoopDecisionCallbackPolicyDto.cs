// ABOUTME: Coop callback policy metadata associated with a review decision.
// ABOUTME: Keeps provider policy identifiers bounded and separate from raw payload storage.

namespace Explore.Application.DTOs.EventReporting;

public sealed record CoopDecisionCallbackPolicyDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}
