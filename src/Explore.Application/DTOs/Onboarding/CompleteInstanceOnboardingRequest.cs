// ABOUTME: Minimal onboarding payload — captures only the decisions made during first-run wizard.
// ABOUTME: All other settings are configurable post-onboarding via instance admin endpoints.

using System.Text.Json.Serialization;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Onboarding;

public sealed class CompleteInstanceOnboardingRequest
{
    public const string EmbeddedAdministrationAccess = "Embedded";
    public const string DedicatedAdminHostAdministrationAccess = "DedicatedAdminHost";
    public const string SeparateControlPlaneAppAdministrationAccess = "SeparateControlPlaneApp";

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DeploymentMode DeploymentMode { get; set; } = DeploymentMode.SingleTenant;
    public SelfHostOnboardingProfileDto SiteProfile { get; set; } = new();
    public string AdministrationAccessMode { get; set; } = EmbeddedAdministrationAccess;
    public string? AdminHost { get; set; }
    public string? InstanceName { get; set; }
}
