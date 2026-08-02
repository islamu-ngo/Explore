// ABOUTME: Defines the mutually exclusive transport profiles for reusable Explore.Blazor hosting.
// ABOUTME: Split owns YARP and remote API readiness while Combined omits both for in-process composition.

namespace Explore.Blazor.Hosting;

public enum BlazorHostProfile
{
    Split,
    Combined
}

internal sealed record BlazorHostProfileRegistration(BlazorHostProfile Profile);
