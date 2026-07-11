// ABOUTME: Sub-resource DTO for tenant delegation policies.
// ABOUTME: Controls what tenants can self-manage (SMTP, storage, analytics, white-labeling).

namespace Explore.Application.DTOs.Instance;

public class TenantDelegationSettingsDto
{
    public bool AllowTenantSelfServiceRegistration { get; set; }
    public bool AllowTenantWhiteLabeling { get; set; }
    public string DefaultPublicHomePage { get; set; } = "EventList";
    public bool LockTenantHomePagePreference { get; set; }
    public bool LockTenantSmtp { get; set; } = true;
    public bool LockTenantStorage { get; set; } = true;
    public bool LockTenantAnalytics { get; set; } = true;
    public bool LockTenantAiAssistant { get; set; } = true;
    public bool DecentralizationEnabled { get; set; }
    public bool LockDecentralizationEnabled { get; set; }
    public string AuthorizationProvider { get; set; } = "local";
}
