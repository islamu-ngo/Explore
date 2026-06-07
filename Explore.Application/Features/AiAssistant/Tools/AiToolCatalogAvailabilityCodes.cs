// ABOUTME: Defines stable availability codes for scoped AI tool catalog entries.
// ABOUTME: Keeps assistant, MCP, and tests aligned without mixing catalog visibility with execution authority.

namespace Explore.Application.Features.AiAssistant.Tools;

public static class AiToolCatalogAvailabilityCodes
{
    public const string Available = "available";
    public const string MissingHalAffordance = "missing_hal_affordance";
}
