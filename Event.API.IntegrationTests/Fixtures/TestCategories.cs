// ABOUTME: Defines test category constants for the hybrid test taxonomy.
// ABOUTME: Used with TUnit [Category] attribute to selectively run Fast, Security, PolicyContract, or E2E tests.

namespace Event.Api.IntegrationTests.Fixtures;

/// <summary>
/// Test category constants for the hybrid test taxonomy.
/// Use <c>[Category(TestCategories.Fast)]</c> to tag tests.
/// Run selective TUnit categories via <c>dotnet test --treenode-filter "////[Category=Security]"</c>.
/// </summary>
public static class TestCategories
{
    /// <summary>
    /// Fast integration tests using TestAuthHandler and mocks.
    /// No Docker containers required. Default for developer inner-loop.
    /// </summary>
    public const string Fast = "Fast";

    /// <summary>
    /// Security integration tests using real Keycloak + Cerbos containers.
    /// Validates the actual OIDC token validation and ABAC policy pipeline.
    /// </summary>
    public const string Security = "Security";

    /// <summary>
    /// Policy contract tests that exercise Cerbos policies directly via gRPC.
    /// Does not go through the API layer; validates policy decisions in isolation.
    /// </summary>
    public const string PolicyContract = "PolicyContract";

    /// <summary>
    /// End-to-end tests running the full stack (Playwright + Blazor + API + Keycloak + Cerbos + Postgres).
    /// </summary>
    public const string E2E = "E2E";
}
