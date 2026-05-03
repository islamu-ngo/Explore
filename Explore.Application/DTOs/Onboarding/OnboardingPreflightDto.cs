// ABOUTME: Read model for convention-first onboarding preflight status.
// ABOUTME: Separates launch-blocking checks from operational warnings for setup UI/API consumers.

namespace Explore.Application.DTOs.Onboarding;

public sealed class OnboardingPreflightDto
{
    public string DeploymentMode { get; set; } = "SingleTenant";
    public bool IsReadyToLaunch => BlockingChecks.All(check => check.Status == OnboardingPreflightCheckStatus.Pass);
    public List<OnboardingPreflightCheckDto> BlockingChecks { get; set; } = [];
    public List<OnboardingPreflightCheckDto> WarningChecks { get; set; } = [];
}

public sealed class OnboardingPreflightCheckDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Severity { get; set; } = OnboardingPreflightCheckSeverity.Blocking;
    public string Status { get; set; } = OnboardingPreflightCheckStatus.Pass;
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
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
