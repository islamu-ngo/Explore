// ABOUTME: Defines TUnit category constants for Aspire-backed Playwright browser tests.
// ABOUTME: Keeps E2E and runtime email filters stable without scattering string literals.

namespace Explore.Blazor.Client.E2ETests.Fixtures;

public static class E2ETestCategories
{
    public const string E2E = "E2E";
    public const string Email = "Email";
    public const string Runtime = "Runtime";
    public const string Slow = "Slow";
    public const string Manual = "Manual";
}
