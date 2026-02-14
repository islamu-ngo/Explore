// ABOUTME: DTO for anonymous/authenticated public experience settings resolved via instance->tenant cascade.
// ABOUTME: Powers home-page routing and white-label branding without requiring admin permissions.

namespace Explore.Application.DTOs.Onboarding;

public class PublicExperienceSettingsDto
{
    public Guid TenantId { get; set; }
    public string DeploymentMode { get; set; } = "SingleTenant";
    public string PreferredHomePage { get; set; } = "EventList";
    public string BrandDisplayName { get; set; } = "ISLAMU Explore";
    public string BrandLogoUrl { get; set; } = string.Empty;
    public string BrandFaviconUrl { get; set; } = string.Empty;
    public string BrandCustomCssUrl { get; set; } = string.Empty;
    public string InstanceBaseDomain { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string CustomDomain { get; set; } = string.Empty;
    public bool IsIslamicModuleEnabled { get; set; }
    public bool IsTechModuleEnabled { get; set; }
    public List<string> EnabledModules { get; set; } = new();
}
