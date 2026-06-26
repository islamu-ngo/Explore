// ABOUTME: Classifies AI tool risk for assistant UX, inventories, and plan previews.
// ABOUTME: Descriptive only; authorization and execution still rely on API/HAL/CQRS checks.

namespace Explore.Application.Features.AiAssistant.Tools;

public enum AiToolRiskClass
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
}
