// ABOUTME: Coop callback action metadata accepted by the moderation integration endpoint.
// ABOUTME: Carries provider action text codes while Application maps them to local decisions.

namespace Explore.Application.DTOs.EventReporting;

public sealed class CoopDecisionCallbackActionDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}
