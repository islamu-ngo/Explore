// ABOUTME: Carries route, workflow, tenant, principal, and HAL context for AI tool catalog scoping.
// ABOUTME: Keeps catalog visibility separate from execution authority and confirmation checks.

namespace Explore.Application.Features.AiAssistant.Tools;

public sealed record AiToolCatalogQuery(
    Guid TenantId,
    bool IsAuthenticated,
    AiToolCatalogPrincipalKind PrincipalKind,
    string? RoutePath,
    string? WorkflowScope,
    string? ContextScope,
    IReadOnlySet<string> AvailableHalLinkRels);
