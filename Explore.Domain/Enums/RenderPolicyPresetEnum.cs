// ABOUTME: Enumerates admin-facing routing render-policy presets for runtime governance.
// ABOUTME: Enables type-safe preset handling before advanced override resolution.

namespace Explore.Domain.Enums;

public enum RenderPolicyPresetEnum
{
    SeoBalanced = 1,
    AllPrerendered = 2,
    AllInteractiveAutoNoPrerender = 3,
    CustomAdvanced = 4,
    AllInteractiveServer = 5
}
