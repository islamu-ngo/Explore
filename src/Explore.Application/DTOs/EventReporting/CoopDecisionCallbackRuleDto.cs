// ABOUTME: Coop callback rule metadata associated with a review decision.
// ABOUTME: Captures safe provider rule identifiers without storing raw provider payloads.

namespace Explore.Application.DTOs.EventReporting;

public sealed record CoopDecisionCallbackRuleDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}
