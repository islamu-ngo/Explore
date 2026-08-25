// ABOUTME: Read model for convention-first onboarding preflight status.
// ABOUTME: Separates launch-blocking checks from operational warnings for setup UI/API consumers.

namespace Explore.Application.DTOs.Onboarding;

public sealed record OnboardingPreflightDto
{
    public string DeploymentMode { get; set; } = "SingleTenant";
    public bool IsReadyToLaunch => BlockingChecks.All(check => check.Status == OnboardingPreflightCheckStatus.Pass);
    public List<OnboardingPreflightCheckDto> BlockingChecks { get; init; } = [];
    public List<OnboardingPreflightCheckDto> WarningChecks { get; init; } = [];
}

public sealed record OnboardingPreflightCheckDto
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Severity { get; init; } = OnboardingPreflightCheckSeverity.Blocking;
    public string Status { get; init; } = OnboardingPreflightCheckStatus.Pass;
    public string Message { get; init; } = string.Empty;
    public string? Detail { get; init; }
}

public static class OnboardingPreflightCheckSeverity
{
    public const string Blocking = "Blocking";
    public const string Warning = "Warning";
}

public static class OnboardingPreflightCheckStatus
{
    public const string Pass = "Pass";
    public const string Fail = "Fail";
    public const string Warning = "Warning";
}
