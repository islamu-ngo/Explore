// ABOUTME: Refit interface for instance onboarding and governance settings BFF endpoints.
// ABOUTME: Covers onboarding flow, governance sub-resources, infrastructure settings, auth/authz providers, and analytics/footer governance.

using Refit;

namespace Explore.Blazor.Client.Services;

public interface IInstanceOnboardingApi
{
    // ── Onboarding ───────────────────────────────────────────────────────

    [Get("/api/system/onboarding-status")]
    Task<IApiResponse<SystemOnboardingStatusModel>> GetSystemOnboardingStatusAsync(CancellationToken cancellationToken);

    [Get("/api/system/onboarding-preflight")]
    Task<IApiResponse<OnboardingPreflightModel>> GetOnboardingPreflightAsync(CancellationToken cancellationToken);

    [Get("/api/InstanceOnboarding/status")]
    Task<IApiResponse<InstanceOnboardingStatusModel>> GetStatusAsync(CancellationToken cancellationToken);

    [Post("/api/InstanceOnboarding/validate-secret")]
    Task<IApiResponse<SetupSecretValidationResult>> ValidateSecretAsync([Body] ValidateSecretRequest request, CancellationToken cancellationToken);

    [Post("/api/InstanceOnboarding/complete")]
    Task<IApiResponse<InstanceCommandResponseModel>> CompleteOnboardingAsync([Body] OnboardingCompletionModel completion, CancellationToken cancellationToken);

    // ── Governance Sub-Resource Reads ─────────────────────────────────────

    [Get("/api/instance/settings/deployment-mode")]
    Task<IApiResponse<DeploymentModeModel>> GetDeploymentModeAsync(CancellationToken cancellationToken);

    [Get("/api/instance/settings/modules")]
    Task<IApiResponse<ModuleSettingsModel>> GetModuleSettingsAsync(CancellationToken cancellationToken);

    [Get("/api/instance/settings/events")]
    Task<IApiResponse<EventPolicyModel>> GetEventPolicyAsync(CancellationToken cancellationToken);

    [Get("/api/instance/settings/organizations")]
    Task<IApiResponse<OrganizationPolicyModel>> GetOrganizationPolicyAsync(CancellationToken cancellationToken);

    [Get("/api/instance/settings/branding")]
    Task<IApiResponse<BrandingSettingsModel>> GetBrandingSettingsAsync(CancellationToken cancellationToken);

    [Get("/api/instance/settings/domains")]
    Task<IApiResponse<DomainSettingsModel>> GetDomainSettingsAsync(CancellationToken cancellationToken);

    [Get("/api/instance/settings/tenant-delegation")]
    Task<IApiResponse<TenantDelegationModel>> GetTenantDelegationAsync(CancellationToken cancellationToken);

    [Get("/api/instance/settings/render-policy")]
    Task<IApiResponse<RenderPolicyModel>> GetRenderPolicyAsync(CancellationToken cancellationToken);

    // ── Governance Sub-Resource Writes ────────────────────────────────────

    [Post("/api/instance/settings/deployment-mode")]
    Task<IApiResponse<InstanceCommandResponseModel>> UpdateDeploymentModeAsync([Body] UpdateDeploymentModeRequest request, CancellationToken cancellationToken);

    [Put("/api/instance/settings/modules")]
    Task<IApiResponse<InstanceCommandResponseModel>> UpdateModuleSettingsAsync([Body] ModuleSettingsModel settings, CancellationToken cancellationToken);

    [Put("/api/instance/settings/events")]
    Task<IApiResponse<InstanceCommandResponseModel>> UpdateEventPolicyAsync([Body] EventPolicyModel settings, CancellationToken cancellationToken);

    [Put("/api/instance/settings/organizations")]
    Task<IApiResponse<InstanceCommandResponseModel>> UpdateOrganizationPolicyAsync([Body] OrganizationPolicyModel settings, CancellationToken cancellationToken);

    [Put("/api/instance/settings/branding")]
    Task<IApiResponse<InstanceCommandResponseModel>> UpdateBrandingSettingsAsync([Body] BrandingSettingsModel settings, CancellationToken cancellationToken);

    [Put("/api/instance/settings/domains")]
    Task<IApiResponse<InstanceCommandResponseModel>> UpdateDomainSettingsAsync([Body] DomainSettingsModel settings, CancellationToken cancellationToken);

    [Put("/api/instance/settings/tenant-delegation")]
    Task<IApiResponse<InstanceCommandResponseModel>> UpdateTenantDelegationAsync([Body] TenantDelegationModel settings, CancellationToken cancellationToken);

    [Put("/api/instance/settings/render-policy")]
    Task<IApiResponse<InstanceCommandResponseModel>> UpdateRenderPolicyAsync([Body] RenderPolicyModel settings, CancellationToken cancellationToken);

    // ── Infrastructure Settings ──────────────────────────────────────────

    [Get("/api/instance/settings/storage")]
    Task<IApiResponse<InstanceStorageSettingsModel>> GetStorageSettingsAsync(CancellationToken cancellationToken);

    [Put("/api/instance/settings/storage")]
    Task<IApiResponse<InstanceCommandResponseModel>> UpdateStorageSettingsAsync([Body] InstanceStorageSettingsModel settings, CancellationToken cancellationToken);

    [Post("/api/instance/settings/storage/test")]
    Task<IApiResponse<StorageConnectionTestResult>> TestStorageConnectionAsync(CancellationToken cancellationToken);

    [Get("/api/instance/settings/smtp")]
    Task<IApiResponse<InstanceSmtpSettingsModel>> GetSmtpSettingsAsync(CancellationToken cancellationToken);

    [Put("/api/instance/settings/smtp")]
    Task<IApiResponse<InstanceCommandResponseModel>> UpdateSmtpSettingsAsync([Body] InstanceSmtpSettingsModel settings, CancellationToken cancellationToken);

    [Post("/api/instance/settings/smtp/test")]
    Task<IApiResponse<SmtpConnectionTestResult>> TestSmtpConnectionAsync(CancellationToken cancellationToken);

    [Get("/api/Tenant/count")]
    Task<HttpResponseMessage> GetActiveTenantCountAsync(CancellationToken cancellationToken);

    // ── Auth Provider Configuration ──────────────────────────────────────

    [Get("/api/instance/settings/auth-provider")]
    Task<IApiResponse<AuthProviderConfigurationModel>> GetAuthProviderConfigurationAsync(CancellationToken cancellationToken);

    [Put("/api/InstanceOnboarding/auth-provider-configuration")]
    Task<IApiResponse<InstanceCommandResponseModel>> SaveAuthProviderConfigurationAsync([Body] AuthProviderConfigurationModel config, CancellationToken cancellationToken);

    [Post("/api/InstanceOnboarding/auth-provider-configuration/keycloak-bootstrap")]
    Task<IApiResponse<InstanceCommandResponseModel>> BootstrapKeycloakRealmAsync([Body] KeycloakBootstrapRequestModel request, CancellationToken cancellationToken);

    [Post("/api/instance/settings/auth-provider/keycloak/doctor")]
    Task<IApiResponse<KeycloakRealmDoctorResultModel>> RunKeycloakRealmDoctorAsync([Body] KeycloakRealmDoctorRequestModel request, CancellationToken cancellationToken);

    [Post("/api/instance/settings/auth-provider/keycloak/sync-preview")]
    Task<IApiResponse<KeycloakRealmSyncPlanModel>> PreviewKeycloakRealmSyncAsync([Body] KeycloakRealmSyncPreviewRequestModel request, CancellationToken cancellationToken);

    [Put("/api/instance/settings/auth-provider")]
    Task<IApiResponse<InstanceCommandResponseModel>> UpdateAuthProviderConfigurationAsAdminAsync([Body] AuthProviderConfigurationModel config, CancellationToken cancellationToken);

    [Get("/api/instance/settings/auth-provider/status")]
    Task<IApiResponse<AuthProviderConfiguredResult>> IsAuthProviderConfiguredAsync(CancellationToken cancellationToken);

    // ── Authorization Provider Configuration ─────────────────────────────

    [Get("/api/InstanceOnboarding/authz-provider-configuration/internal")]
    Task<IApiResponse<AuthorizationProviderConfigurationModel>> GetAuthorizationProviderConfigurationAsync(CancellationToken cancellationToken);

    [Get("/api/instance/settings/authz-provider")]
    Task<IApiResponse<AuthorizationProviderConfigurationModel>> GetAuthorizationProviderConfigurationAsAdminAsync(CancellationToken cancellationToken);

    [Put("/api/InstanceOnboarding/authz-provider-configuration")]
    Task<IApiResponse<InstanceCommandResponseModel>> SaveAuthorizationProviderConfigurationAsync([Body] AuthorizationProviderConfigurationModel config, CancellationToken cancellationToken);

    [Put("/api/instance/settings/authz-provider")]
    Task<IApiResponse<InstanceCommandResponseModel>> UpdateAuthorizationProviderConfigurationAsAdminAsync([Body] AuthorizationProviderConfigurationModel config, CancellationToken cancellationToken);

    [Post("/api/InstanceOnboarding/authz-provider-configuration/sync")]
    Task<IApiResponse<InstanceCommandResponseModel>> SyncAuthorizationPolicyPackageAsync(CancellationToken cancellationToken);

    [Post("/api/instance/settings/authz-provider/sync")]
    Task<IApiResponse<InstanceCommandResponseModel>> SyncAuthorizationPolicyPackageAsAdminAsync(CancellationToken cancellationToken);

    [Post("/api/InstanceOnboarding/authz-provider-configuration/verify")]
    Task<IApiResponse<InstanceCommandResponseModel>> VerifyCerbosEndpointAsync([Body] VerifyCerbosEndpointRequest request, CancellationToken cancellationToken);

    [Get("/api/instance/settings/authz-provider/status")]
    Task<IApiResponse<AuthorizationProviderConfiguredResult>> IsAuthorizationProviderConfiguredAsync(CancellationToken cancellationToken);

    // ── Analytics Governance ─────────────────────────────────────────────

    [Get("/api/instance/settings/analytics-governance")]
    Task<IApiResponse<Models.Analytics.AnalyticsGovernanceSettingsModel>> GetAnalyticsGovernanceSettingsAsync(CancellationToken cancellationToken);

    [Put("/api/instance/settings/analytics-governance")]
    Task<IApiResponse<InstanceCommandResponseModel>> UpdateAnalyticsGovernanceSettingsAsync([Body] Models.Analytics.AnalyticsGovernanceSettingsModel settings, CancellationToken cancellationToken);

    // ── Footer Governance ────────────────────────────────────────────────

    [Get("/api/instance/settings/footer-governance")]
    Task<IApiResponse<FooterGovernanceSettingsModel>> GetFooterGovernanceSettingsAsync(CancellationToken cancellationToken);

    [Put("/api/instance/settings/footer-governance")]
    Task<IApiResponse<InstanceCommandResponseModel>> UpdateFooterGovernanceSettingsAsync([Body] FooterGovernanceSettingsModel settings, CancellationToken cancellationToken);
}

public sealed class ValidateSecretRequest
{
    public required string Secret { get; init; }
}

public sealed class UpdateDeploymentModeRequest
{
    public required string DeploymentMode { get; init; }
}

public sealed class VerifyCerbosEndpointRequest
{
    public required string GrpcEndpoint { get; init; }
}
