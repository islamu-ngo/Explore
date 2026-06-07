// ABOUTME: Describes route, workflow, and context scopes where an AI tool is relevant.
// ABOUTME: Scopes catalog visibility only and never authorize execution by themselves.

namespace Explore.Application.Features.AiAssistant.Tools;

public sealed record AiToolScopeMetadata(
    IReadOnlySet<string> RouteScopes,
    IReadOnlySet<string> WorkflowScopes,
    IReadOnlySet<string> ContextScopes)
{
    public static AiToolScopeMetadata Empty { get; } = new(
        new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        new HashSet<string>(StringComparer.OrdinalIgnoreCase));
}
