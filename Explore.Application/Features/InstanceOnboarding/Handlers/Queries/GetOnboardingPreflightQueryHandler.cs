// ABOUTME: Builds onboarding preflight checks from existing setup, tenancy, auth, and settings state.
// ABOUTME: Keeps launch blockers distinct from operational warnings without introducing new persistence.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Queries;

public sealed class GetOnboardingPreflightQueryHandler(
    IInstanceBootstrapStateRepository instanceBootstrapStateRepository,
    IDeploymentModeProvider deploymentModeProvider,
    ISetupSecretProvider setupSecretProvider,
    ITenantRepository tenantRepository,
    ISystemSettingRepository systemSettingRepository,
    IConfiguration configuration)
    : IRequestHandler<GetOnboardingPreflightQuery, OnboardingPreflightDto>
{
    public async Task<OnboardingPreflightDto> Handle(GetOnboardingPreflightQuery request, CancellationToken cancellationToken)
    {
        var result = new OnboardingPreflightDto();
        var bootstrap = await instanceBootstrapStateRepository.GetCurrent();
        var onboardingCompleted = bootstrap?.IsCompleted == true;
        var deploymentMode = onboardingCompleted
            ? await deploymentModeProvider.GetCurrentModeAsync(cancellationToken)
            : await deploymentModeProvider.GetConfiguredOnboardingModeAsync(cancellationToken);

        result.DeploymentMode = deploymentMode.ToString();

        AddSetupSecretCheck(result, onboardingCompleted);
        AddRepositoryReachabilityCheck(result, bootstrap);
        AddMigrationCheck(result);
        AddDeploymentModeCheck(result, deploymentMode);
        await AddDefaultTenantCheckAsync(result, deploymentMode, onboardingCompleted);
        await AddAuthConfigurationCheckAsync(result);
        await AddCanonicalHostCheckAsync(result);
        await AddOperationalWarningsAsync(result);

        return result;
    }

    private void AddSetupSecretCheck(OnboardingPreflightDto result, bool onboardingCompleted)
    {
        if (onboardingCompleted)
        {
            AddBlocking(result, "setup_secret", "Setup secret", OnboardingPreflightCheckStatus.Pass, "Onboarding is already completed and setup mode is locked.");
            return;
        }

        if (setupSecretProvider.IsTimedOut)
        {
            AddBlocking(result, "setup_secret", "Setup secret", OnboardingPreflightCheckStatus.Fail, "The generated setup secret window has expired.", "Set SETUP_SECRET and restart the app, or restart to generate a fresh development setup secret.");
            return;
        }

        if (!setupSecretProvider.IsSetupModeActive)
        {
            AddBlocking(result, "setup_secret", "Setup secret", OnboardingPreflightCheckStatus.Fail, "Setup mode is not active.", "The setup secret provider is locked or has not initialized setup state.");
            return;
        }

        var source = setupSecretProvider.IsFromEnvironmentVariable ? "environment" : "generated startup log";
        AddBlocking(result, "setup_secret", "Setup secret", OnboardingPreflightCheckStatus.Pass, $"Setup secret is active from {source}.");
    }

    private static void AddRepositoryReachabilityCheck(OnboardingPreflightDto result, object? bootstrap)
    {
        AddBlocking(
            result,
            "database_reachable",
            "Database reachable",
            OnboardingPreflightCheckStatus.Pass,
            bootstrap is null
                ? "Database read completed and no completed bootstrap state exists yet."
                : "Database read completed and bootstrap state is available.");
    }

    private static void AddMigrationCheck(OnboardingPreflightDto result)
    {
        AddWarning(result, "database_migrations", "Database migrations", "Application repositories are reachable; migration freshness is enforced by the migration service and build pipeline.");
    }

    private static void AddDeploymentModeCheck(OnboardingPreflightDto result, DeploymentMode deploymentMode)
    {
        AddBlocking(result, "deployment_mode", "Deployment mode", OnboardingPreflightCheckStatus.Pass, $"Deployment mode resolved to {deploymentMode}.");
    }

    private async Task AddDefaultTenantCheckAsync(OnboardingPreflightDto result, DeploymentMode deploymentMode, bool onboardingCompleted)
    {
        if (deploymentMode != DeploymentMode.SingleTenant)
        {
            AddBlocking(result, "default_tenant", "Default tenant", OnboardingPreflightCheckStatus.Pass, "Default tenant is not required for MultiTenant preflight.");
            return;
        }

        var defaultTenant = await tenantRepository.GetById(PlatformDefaults.DefaultTenantId);
        if (!onboardingCompleted && defaultTenant is null)
        {
            AddBlocking(result, "default_tenant", "Default tenant", OnboardingPreflightCheckStatus.Pass, "SingleTenant default tenant will be created during instance completion.");
            return;
        }

        AddBlocking(
            result,
            "default_tenant",
            "Default tenant",
            defaultTenant is null ? OnboardingPreflightCheckStatus.Fail : OnboardingPreflightCheckStatus.Pass,
            defaultTenant is null
                ? "SingleTenant default tenant has not been created yet."
                : "SingleTenant default tenant exists.",
            defaultTenant is null ? "Complete instance onboarding to create the internal default tenant." : null);
    }

    private async Task AddAuthConfigurationCheckAsync(OnboardingPreflightDto result)
    {
        var keycloakReady = HasConfigurationValue("Keycloak:Authority")
            && (HasConfigurationValue("Keycloak:ClientId") || HasConfigurationValue("Keycloak:Audience"));
        var storedKeycloak = await HasEnabledSettingAsync(GovernanceSettingKeys.Authentication.KeycloakEnabled)
            && await HasSettingValueAsync(GovernanceSettingKeys.Authentication.KeycloakAuthority)
            && await HasSettingValueAsync(GovernanceSettingKeys.Authentication.KeycloakClientId);
        var atprotoReady = await HasEnabledSettingAsync(GovernanceSettingKeys.Authentication.AtprotoLoginEnabled)
            && await HasSettingValueAsync(GovernanceSettingKeys.Authentication.AtprotoPublicUrl);
        var googleReady = await HasEnabledSettingAsync(GovernanceSettingKeys.Authentication.GoogleSsoEnabled)
            && await HasSettingValueAsync(GovernanceSettingKeys.Authentication.GoogleClientId);

        AddBlocking(
            result,
            "auth_config",
            "Authentication configuration",
            keycloakReady || storedKeycloak || atprotoReady || googleReady
                ? OnboardingPreflightCheckStatus.Pass
                : OnboardingPreflightCheckStatus.Fail,
            keycloakReady || storedKeycloak || atprotoReady || googleReady
                ? "At least one authentication provider has enough configuration to continue."
                : "No authentication provider appears ready for admin sign-in.",
            keycloakReady || storedKeycloak || atprotoReady || googleReady
                ? null
                : "Configure Keycloak, AT Protocol, or Google SSO before completing onboarding.");
    }

    private async Task AddCanonicalHostCheckAsync(OnboardingPreflightDto result)
    {
        var configuredHost = configuration["PublicBaseUrl"]
            ?? configuration["App:PublicBaseUrl"]
            ?? configuration["ASPNETCORE_URLS"];
        var storedDomain = await HasSettingValueAsync(GovernanceSettingKeys.Domains.InstanceBaseDomain);

        AddBlocking(
            result,
            "canonical_host",
            "Canonical host",
            !string.IsNullOrWhiteSpace(configuredHost) || storedDomain
                ? OnboardingPreflightCheckStatus.Pass
                : OnboardingPreflightCheckStatus.Fail,
            !string.IsNullOrWhiteSpace(configuredHost) || storedDomain
                ? "Canonical host/domain is available from configuration or Site Profile settings."
                : "Canonical host/domain is not configured.",
            !string.IsNullOrWhiteSpace(configuredHost) || storedDomain
                ? null
                : "Set the canonical URL in Site Profile or provide a public base URL configuration before launch.");
    }

    private async Task AddOperationalWarningsAsync(OnboardingPreflightDto result)
    {
        if (!await HasSettingValueAsync(GovernanceSettingKeys.Email.SmtpHost))
        {
            AddWarning(result, "smtp", "SMTP", "SMTP is not configured; email delivery features may be unavailable after launch.");
        }

        if (!await HasSettingValueAsync(GovernanceSettingKeys.Storage.BucketName))
        {
            AddWarning(result, "object_storage", "Object storage", "Object storage is not configured; media uploads may be limited after launch.");
        }

        AddWarning(result, "backups", "Backups", "Backup verification is not configured in application settings; confirm database backups operationally.");
        AddWarning(result, "observability", "Logs, metrics, and health", "Confirm logs, metrics, and health endpoint monitoring in the hosting environment.");
        AddWarning(result, "public_exposure", "Public exposure", "Review public URL, search visibility, and signup/submission policies before announcing the site.");
    }

    private bool HasConfigurationValue(string key) => !string.IsNullOrWhiteSpace(configuration[key]);

    private async Task<bool> HasEnabledSettingAsync(string key)
    {
        var value = await GetSettingValueAsync(key);
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "\"true\"", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> HasSettingValueAsync(string key) => !string.IsNullOrWhiteSpace(await GetSettingValueAsync(key));

    private async Task<string?> GetSettingValueAsync(string key)
    {
        var setting = await systemSettingRepository.GetByKey(key);
        return string.IsNullOrWhiteSpace(setting?.Value)
            ? null
            : setting.Value.Trim('"');
    }

    private static void AddBlocking(OnboardingPreflightDto result, string code, string name, string status, string message, string? detail = null)
    {
        result.BlockingChecks.Add(new OnboardingPreflightCheckDto
        {
            Code = code,
            Name = name,
            Severity = OnboardingPreflightCheckSeverity.Blocking,
            Status = status,
            Message = message,
            Detail = detail
        });
    }

    private static void AddWarning(OnboardingPreflightDto result, string code, string name, string message, string? detail = null)
    {
        result.WarningChecks.Add(new OnboardingPreflightCheckDto
        {
            Code = code,
            Name = name,
            Severity = OnboardingPreflightCheckSeverity.Warning,
            Status = OnboardingPreflightCheckStatus.Warning,
            Message = message,
            Detail = detail
        });
    }
}
