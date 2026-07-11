// ABOUTME: Provides tenant, HAL, and freshness inputs for validating AI plan previews.
// ABOUTME: Uses API/HAL affordances as advisory gating and never grants execution authority.

namespace Explore.Application.Features.AiAssistant.Plans;

public sealed record AiPlanValidationContext(
    Guid TenantId,
    IReadOnlySet<string> AvailableHalLinkRels,
    DateTime UtcNow,
    TimeSpan MaxContextAge);
