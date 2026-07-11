// ABOUTME: Discriminated result type for AI run status polling.
// ABOUTME: Distinguishes success, not-found/transient failure, and authentication rejection (401).

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services.Ai;

public readonly struct AiRunStatusResult
{
    public HalResourceOfAiRunDto? Run { get; }
    public bool IsUnauthorized { get; }
    public bool Success => Run is not null;

    private AiRunStatusResult(HalResourceOfAiRunDto? run, bool isUnauthorized)
    {
        Run = run;
        IsUnauthorized = isUnauthorized;
    }

    public static AiRunStatusResult Ok(HalResourceOfAiRunDto run) => new(run, false);
    public static AiRunStatusResult NotFound() => new(null, false);
    public static AiRunStatusResult Unauthorized() => new(null, true);
}
