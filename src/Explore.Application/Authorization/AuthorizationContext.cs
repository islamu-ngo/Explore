// ABOUTME: Carries request-specific resource context resolved before policy authorization.
// ABOUTME: Lets per-command enrichers replace hard-coded pipeline type branches.

namespace Explore.Application.Authorization;

public sealed record AuthorizationContext(
    string? ResourceId,
    IDictionary<string, object>? Attributes,
    IAuthorizationFacts? Facts = null);
