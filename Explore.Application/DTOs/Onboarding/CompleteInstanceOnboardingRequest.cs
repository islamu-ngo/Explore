// ABOUTME: Minimal onboarding payload — captures only the decisions made during first-run wizard.
// ABOUTME: All other settings are configurable post-onboarding via instance admin endpoints.

using System.Text.Json.Serialization;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Onboarding;

public sealed class CompleteInstanceOnboardingRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DeploymentMode DeploymentMode { get; set; } = DeploymentMode.SingleTenant;
    public string? InstanceName { get; set; }
}
