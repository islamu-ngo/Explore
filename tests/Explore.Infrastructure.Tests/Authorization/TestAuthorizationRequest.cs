// ABOUTME: Test-only builder that expresses a provider scenario in the historical attribute vocabulary.
// ABOUTME: Produces a real AuthorizationRequest whose only policy input is the closed typed fact catalog.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;

namespace Explore.Infrastructure.Tests.Authorization;

internal static class TestAuthorizationRequest
{
    /// <summary>
    /// Mirrors the scenario shape the behavioural corpus was written in. The attribute bag never reaches
    /// the provider: it is translated into the same fact record the trusted resolvers would produce.
    /// </summary>
    public static AuthorizationRequest Create(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? attributes = null,
        AuthorizationScope? scope = null,
        IAuthorizationFacts? facts = null) =>
        new(resourceKind,
            resourceId,
            action,
            scope,
            facts ?? AuthorizationFactsTestFactory.Create(resourceKind, resourceId, attributes));
}
