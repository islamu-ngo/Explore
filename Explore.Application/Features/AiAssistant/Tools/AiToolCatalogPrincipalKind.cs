// ABOUTME: Identifies the caller shape for scoped AI tool catalog views.
// ABOUTME: Catalog principal kind is descriptive and never bypasses API authorization checks.

namespace Explore.Application.Features.AiAssistant.Tools;

public enum AiToolCatalogPrincipalKind
{
    User = 1,
    Machine = 2,
}
