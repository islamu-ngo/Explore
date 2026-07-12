// ABOUTME: Client service for instance onboarding and governance through the generated API client.
// ABOUTME: Exposes generated request and response DTOs for all onboarding settings.

using System.Text.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Microsoft.AspNetCore.Components;

namespace Explore.Blazor.Client.Services;

public interface IInstanceOnboardingService
{
    Task<SystemOnboardingStatusDto?> GetSystemOnboardingStatusAsync();
    Task<OnboardingPreflightDto?> GetOnboardingPreflightAsync();
    Task<InstanceOnboardingStatusDto?> GetStatusAsync();
    Task<SetupSecretValidationResultDto> ValidateSecretAsync(string secret);
    Task<BaseCommandResponseOfGuid> CompleteAsync(CompleteInstanceOnboardingRequest completion);

    Task<DeploymentModeDto> GetDeploymentModeAsync();
    Task<ModuleSettingsDto> GetModuleSettingsAsync();
    Task<EventPolicyDto> GetEventPolicyAsync();
    Task<OrganizationPolicyDto> GetOrganizationPolicyAsync();
    Task<BrandingSettingsDto> GetBrandingSettingsAsync();
    Task<DomainSettingsDto> GetDomainSettingsAsync();
    Task<TenantDelegationSettingsDto> GetTenantDelegationAsync();
    Task<RenderPolicySettingsDto> GetRenderPolicyAsync();
    Task<McpGovernanceSettingsDto> GetMcpGovernanceSettingsAsync();
    Task<AiAssistantGovernanceSettingsDto> GetAiAssistantGovernanceSettingsAsync();

    Task<BaseCommandResponseOfGuid> UpdateDeploymentModeAsync(string deploymentMode);
    Task<BaseCommandResponseOfGuid> UpdateModuleSettingsAsync(ModuleSettingsDto settings);
    Task<BaseCommandResponseOfGuid> UpdateEventPolicyAsync(EventPolicyDto settings);
    Task<BaseCommandResponseOfGuid> UpdateOrganizationPolicyAsync(OrganizationPolicyDto settings);
    Task<BaseCommandResponseOfGuid> UpdateBrandingSettingsAsync(BrandingSettingsDto settings);
    Task<BaseCommandResponseOfGuid> UpdateDomainSettingsAsync(DomainSettingsDto settings);
    Task<BaseCommandResponseOfGuid> UpdateTenantDelegationAsync(TenantDelegationSettingsDto settings);
    Task<BaseCommandResponseOfGuid> UpdateRenderPolicyAsync(RenderPolicySettingsDto settings);
    Task<BaseCommandResponseOfGuid> UpdateMcpGovernanceSettingsAsync(McpGovernanceSettingsDto settings);
    Task<BaseCommandResponseOfGuid> UpdateAiAssistantGovernanceSettingsAsync(AiAssistantGovernanceSettingsDto settings);

    Task<HalResourceOfInstanceStorageSettingsDto> GetStorageSettingsAsync();
    Task<BaseCommandResponseOfGuid> UpdateStorageSettingsAsync(HalResourceOfInstanceStorageSettingsDto settings);
    Task<InstanceStorageProviderStatusDto> TestStorageConnectionAsync();
    Task<InstanceStorageUsageDto?> RecalculateStorageUsageAsync();
    Task<InstanceSmtpSettingsDto> GetSmtpSettingsAsync();
    Task<BaseCommandResponseOfGuid> UpdateSmtpSettingsAsync(InstanceSmtpSettingsDto settings);
    Task<SmtpConnectionTestResultDto> TestSmtpConnectionAsync();
    Task<int> GetActiveTenantCountAsync();

    Task<AuthProviderConfigurationDto> GetAuthProviderConfigurationAsync();
    Task<AuthProviderConfigurationDto> GetAuthProviderConfigurationAsAdminAsync();
    Task<BaseCommandResponseOfGuid> SaveAuthProviderConfigurationAsync(AuthProviderConfigurationDto config);
    Task<BaseCommandResponseOfGuid> BootstrapKeycloakRealmAsync(KeycloakBootstrapRequestDto request);
    Task<KeycloakRealmDoctorResultDto> RunKeycloakRealmDoctorAsync(KeycloakRealmDoctorRequestDto request);
    Task<KeycloakRealmSyncPlanDto> PreviewKeycloakRealmSyncAsync(KeycloakRealmSyncPreviewRequestDto request);
    Task<KeycloakRealmSyncPlanDto> ApplyKeycloakRealmSyncAsync(KeycloakRealmSyncApplyRequestDto request);
    Task<KeycloakClientSecretRotationResultDto> RotateKeycloakClientSecretAsync(KeycloakClientSecretRotationRequestDto request);
    Task<BaseCommandResponseOfGuid> UpdateAuthProviderConfigurationAsAdminAsync(AuthProviderConfigurationDto config);
    Task<bool> IsAuthProviderConfiguredAsync();
    Task<bool?> GetAuthProviderConfiguredStateAsync();
    Task RefreshAuthSchemesAsync();
    Task<bool> RefreshAuthSessionAsync();

    Task<AuthorizationProviderConfigurationDto> GetAuthorizationProviderConfigurationAsync();
    Task<AuthorizationProviderConfigurationDto> GetAuthorizationProviderConfigurationAsAdminAsync();
    Task<BaseCommandResponseOfGuid> SaveAuthorizationProviderConfigurationAsync(AuthorizationProviderConfigurationDto config);
    Task<BaseCommandResponseOfGuid> UpdateAuthorizationProviderConfigurationAsAdminAsync(AuthorizationProviderConfigurationDto config);
    Task<BaseCommandResponseOfGuid> SyncAuthorizationPolicyPackageAsync();
    Task<BaseCommandResponseOfGuid> SyncAuthorizationPolicyPackageAsAdminAsync();
    Task<BaseCommandResponseOfGuid> VerifyCerbosEndpointAsync(string grpcEndpoint);
    Task<bool> IsAuthorizationProviderConfiguredAsync();
    Task<bool?> GetAuthorizationProviderConfiguredStateAsync();
    Task<bool> ShouldSkipAuthorizationProviderStepAsync();

    Task<AnalyticsGovernanceSettingsDto> GetAnalyticsGovernanceSettingsAsync();
    Task<BaseCommandResponseOfGuid> UpdateAnalyticsGovernanceSettingsAsync(AnalyticsGovernanceSettingsDto settings);
    Task<FooterGovernanceSettingsDto> GetFooterGovernanceSettingsAsync();
    Task<BaseCommandResponseOfGuid> UpdateFooterGovernanceSettingsAsync(FooterGovernanceSettingsDto settings);
}

public sealed class InstanceOnboardingService(
    IEventApiClient api,
    IBffAuthApi bffAuthApi,
    ILogger<InstanceOnboardingService> logger,
    NavigationManager navigation) : IInstanceOnboardingService
{
    public Task<SystemOnboardingStatusDto?> GetSystemOnboardingStatusAsync() =>
        GetOptionalAsync(ct => api.GetSystemOnboardingStatusAsync(cancellationToken: ct), "system onboarding status");

    public Task<OnboardingPreflightDto?> GetOnboardingPreflightAsync() =>
        GetOptionalAsync(ct => api.GetSystemOnboardingPreflightAsync(cancellationToken: ct), "onboarding preflight");

    public async Task<InstanceOnboardingStatusDto?> GetStatusAsync()
    {
        var resource = await GetOptionalAsync(
            ct => api.GetInstanceOnboardingStatusAsync(cancellationToken: ct),
            "instance onboarding status");
        return resource.ToDto();
    }

    public async Task<SetupSecretValidationResultDto> ValidateSecretAsync(string secret)
    {
        try
        {
            return await api.ValidateInstanceSetupSecretAsync(
                new ValidateSetupSecretRequest { Secret = secret },
                cancellationToken: CancellationToken.None);
        }
        catch (ApiException ex) when (ex.StatusCode == 429)
        {
            logger.LogWarning("Setup secret validation rate-limited (429).");
            return new SetupSecretValidationResultDto { Valid = false };
        }
        catch (ApiException ex) when (ex.StatusCode == 410)
        {
            return new SetupSecretValidationResultDto { Valid = false };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to validate setup secret.");
            return new SetupSecretValidationResultDto { Valid = false };
        }
    }

    public async Task<BaseCommandResponseOfGuid> CompleteAsync(CompleteInstanceOnboardingRequest completion)
    {
        try
        {
            return await api.CompleteInstanceOnboardingAsync(completion, cancellationToken: CancellationToken.None);
        }
        catch (ApiException<ValidationProblemDetails> ex)
        {
            logger.LogError(ex, "Failed to complete onboarding due to validation errors. HTTP Status={StatusCode}", ex.StatusCode);
            return MapValidationProblemDetails(ex.Result);
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            if (await RefreshAuthSessionAsync())
            {
                return await SendCommandAsync(
                    ct => api.CompleteInstanceOnboardingAsync(completion, cancellationToken: ct));
            }

            return MapCommandApiException(ex);
        }
        catch (ApiException ex)
        {
            logger.LogError(ex, "Failed to complete onboarding.");
            return MapCommandApiException(ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to complete onboarding.");
            return FailedCommandResponse("Request failed.");
        }
    }

    public Task<DeploymentModeDto> GetDeploymentModeAsync() =>
        GetSettingsAsync(ct => api.GetInstanceDeploymentModeAsync(cancellationToken: ct), () => new());

    public Task<ModuleSettingsDto> GetModuleSettingsAsync() =>
        GetSettingsAsync(ct => api.GetInstanceModuleSettingsAsync(cancellationToken: ct), () => new());

    public Task<EventPolicyDto> GetEventPolicyAsync() =>
        GetSettingsAsync(ct => api.GetInstanceEventPolicyAsync(cancellationToken: ct), () => new());

    public Task<OrganizationPolicyDto> GetOrganizationPolicyAsync() =>
        GetSettingsAsync(ct => api.GetInstanceOrganizationPolicyAsync(cancellationToken: ct), () => new());

    public Task<BrandingSettingsDto> GetBrandingSettingsAsync() =>
        GetSettingsAsync(ct => api.GetInstanceBrandingSettingsAsync(cancellationToken: ct), () => new());

    public Task<DomainSettingsDto> GetDomainSettingsAsync() =>
        GetSettingsAsync(ct => api.GetInstanceDomainSettingsAsync(cancellationToken: ct), () => new());

    public Task<TenantDelegationSettingsDto> GetTenantDelegationAsync() =>
        GetSettingsAsync(ct => api.GetInstanceTenantDelegationSettingsAsync(cancellationToken: ct), () => new());

    public Task<RenderPolicySettingsDto> GetRenderPolicyAsync() =>
        GetSettingsAsync(ct => api.GetInstanceRenderPolicySettingsAsync(cancellationToken: ct), () => new());

    public Task<McpGovernanceSettingsDto> GetMcpGovernanceSettingsAsync() =>
        GetSettingsAsync(ct => api.GetInstanceMcpGovernanceSettingsAsync(cancellationToken: ct), () => new());

    public Task<AiAssistantGovernanceSettingsDto> GetAiAssistantGovernanceSettingsAsync() =>
        GetSettingsAsync(ct => api.GetInstanceAiAssistantGovernanceSettingsAsync(cancellationToken: ct), () => new());

    public Task<BaseCommandResponseOfGuid> UpdateDeploymentModeAsync(string deploymentMode) =>
        SendCommandAsync(ct => api.UpdateInstanceDeploymentModeAsync(
            new UpdateDeploymentModeRequest { DeploymentMode = deploymentMode }, cancellationToken: ct));

    public Task<BaseCommandResponseOfGuid> UpdateModuleSettingsAsync(ModuleSettingsDto settings) =>
        SendCommandAsync(ct => api.UpdateInstanceModuleSettingsAsync(settings, cancellationToken: ct));

    public Task<BaseCommandResponseOfGuid> UpdateEventPolicyAsync(EventPolicyDto settings) =>
        SendCommandAsync(ct => api.UpdateInstanceEventPolicyAsync(settings, cancellationToken: ct));

    public Task<BaseCommandResponseOfGuid> UpdateOrganizationPolicyAsync(OrganizationPolicyDto settings) =>
        SendCommandAsync(ct => api.UpdateInstanceOrganizationPolicyAsync(settings, cancellationToken: ct));

    public Task<BaseCommandResponseOfGuid> UpdateBrandingSettingsAsync(BrandingSettingsDto settings) =>
        SendCommandAsync(ct => api.UpdateInstanceBrandingSettingsAsync(settings, cancellationToken: ct));

    public Task<BaseCommandResponseOfGuid> UpdateDomainSettingsAsync(DomainSettingsDto settings) =>
        SendCommandAsync(ct => api.UpdateInstanceDomainSettingsAsync(settings, cancellationToken: ct));

    public Task<BaseCommandResponseOfGuid> UpdateTenantDelegationAsync(TenantDelegationSettingsDto settings) =>
        SendCommandAsync(ct => api.UpdateInstanceTenantDelegationSettingsAsync(settings, cancellationToken: ct));

    public Task<BaseCommandResponseOfGuid> UpdateRenderPolicyAsync(RenderPolicySettingsDto settings) =>
        SendCommandAsync(ct => api.UpdateInstanceRenderPolicySettingsAsync(settings, cancellationToken: ct));

    public Task<BaseCommandResponseOfGuid> UpdateMcpGovernanceSettingsAsync(McpGovernanceSettingsDto settings) =>
        SendCommandAsync(ct => api.UpdateInstanceMcpGovernanceSettingsAsync(settings, cancellationToken: ct));

    public Task<BaseCommandResponseOfGuid> UpdateAiAssistantGovernanceSettingsAsync(AiAssistantGovernanceSettingsDto settings) =>
        SendCommandAsync(ct => api.UpdateInstanceAiAssistantGovernanceSettingsAsync(settings, cancellationToken: ct));

    public async Task<HalResourceOfInstanceStorageSettingsDto> GetStorageSettingsAsync()
    {
        try
        {
            return (await api.GetInstanceStorageSettingsAsync(
                cancellationToken: CancellationToken.None)).InitializeForEditing();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch storage settings.");
            return new HalResourceOfInstanceStorageSettingsDto().InitializeForEditing();
        }
    }

    public Task<BaseCommandResponseOfGuid> UpdateStorageSettingsAsync(HalResourceOfInstanceStorageSettingsDto settings) =>
        settings.HasLink("edit")
            ? SendCommandAsync(ct => api.UpdateInstanceStorageSettingsAsync(settings.ToUpdateRequest(), cancellationToken: ct))
            : Task.FromResult(FailedCommandResponse("The API did not expose a storage settings edit affordance."));

    public async Task<InstanceStorageProviderStatusDto> TestStorageConnectionAsync()
    {
        try
        {
            return await api.TestInstanceStorageConnectionAsync(cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to test storage connection.");
            return new InstanceStorageProviderStatusDto
            {
                IsAvailable = false,
                Message = "Storage provider test failed."
            };
        }
    }

    public async Task<InstanceStorageUsageDto?> RecalculateStorageUsageAsync()
    {
        try
        {
            return await api.RecalculateInstanceStorageUsageAsync(cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to recalculate storage usage.");
            return null;
        }
    }

    public Task<InstanceSmtpSettingsDto> GetSmtpSettingsAsync() =>
        GetSettingsAsync(ct => api.GetInstanceSmtpSettingsAsync(cancellationToken: ct), () => new());

    public Task<BaseCommandResponseOfGuid> UpdateSmtpSettingsAsync(InstanceSmtpSettingsDto settings) =>
        SendCommandAsync(ct => api.UpdateInstanceSmtpSettingsAsync(settings, cancellationToken: ct));

    public async Task<SmtpConnectionTestResultDto> TestSmtpConnectionAsync()
    {
        try
        {
            return await api.TestInstanceSmtpConnectionAsync(cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to test SMTP connection.");
            return new SmtpConnectionTestResultDto { Success = false, Message = "SMTP connection test failed." };
        }
    }

    public async Task<int> GetActiveTenantCountAsync()
    {
        try
        {
            return await api.GetActiveTenantCountAsync(cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve active tenant count.");
            return 0;
        }
    }

    public Task<AuthProviderConfigurationDto> GetAuthProviderConfigurationAsync() =>
        GetSettingsAsync(ct => api.GetInstanceOnboardingAuthProviderConfigurationAsync(cancellationToken: ct), () => new());

    public Task<AuthProviderConfigurationDto> GetAuthProviderConfigurationAsAdminAsync() =>
        GetSettingsAsync(ct => api.GetInstanceAuthProviderConfigurationAsync(cancellationToken: ct), () => new());

    public Task<BaseCommandResponseOfGuid> SaveAuthProviderConfigurationAsync(AuthProviderConfigurationDto config) =>
        SendCommandAsync(ct => api.SaveInstanceOnboardingAuthProviderConfigurationAsync(config, cancellationToken: ct));

    public async Task<BaseCommandResponseOfGuid> BootstrapKeycloakRealmAsync(KeycloakBootstrapRequestDto request)
    {
        ApplyKeycloakBootstrapBrowserDefaults(request);
        var result = await SendCommandAsync(
            ct => api.BootstrapInstanceOnboardingKeycloakRealmAsync(request, cancellationToken: ct));
        if (result.Success == true)
        {
            await RefreshAuthSchemesAsync();
        }

        return result;
    }

    public Task<KeycloakRealmDoctorResultDto> RunKeycloakRealmDoctorAsync(KeycloakRealmDoctorRequestDto request) =>
        GetSettingsAsync(
            ct => api.RunInstanceKeycloakRealmDoctorAsync(request, cancellationToken: ct),
            () => BlockedDoctor("Keycloak diagnostics failed. Check admin access and retry."));

    public Task<KeycloakRealmSyncPlanDto> PreviewKeycloakRealmSyncAsync(KeycloakRealmSyncPreviewRequestDto request) =>
        GetSettingsAsync(
            ct => api.PreviewInstanceKeycloakRealmSyncAsync(request, cancellationToken: ct),
            () => BlockedPlan("Keycloak sync preview failed. Check admin access and retry."));

    public Task<KeycloakRealmSyncPlanDto> ApplyKeycloakRealmSyncAsync(KeycloakRealmSyncApplyRequestDto request) =>
        GetSettingsAsync(
            ct => api.ApplyInstanceKeycloakRealmSyncAsync(request, cancellationToken: ct),
            () => BlockedPlan("Keycloak sync apply failed. Check admin access and retry."));

    public Task<KeycloakClientSecretRotationResultDto> RotateKeycloakClientSecretAsync(KeycloakClientSecretRotationRequestDto request) =>
        GetSettingsAsync(
            ct => api.RotateInstanceKeycloakClientSecretAsync(request, cancellationToken: ct),
            () => BlockedRotation("Keycloak client-secret rotation failed. Check admin access and retry."));

    public Task<BaseCommandResponseOfGuid> UpdateAuthProviderConfigurationAsAdminAsync(AuthProviderConfigurationDto config) =>
        SendCommandAsync(ct => api.UpdateInstanceAuthProviderConfigurationAsync(config, cancellationToken: ct));

    public async Task<bool> IsAuthProviderConfiguredAsync() =>
        await GetAuthProviderConfiguredStateAsync() ?? false;

    public async Task<bool?> GetAuthProviderConfiguredStateAsync()
    {
        try
        {
            return (await api.GetInstanceAuthProviderConfigurationStatusAsync(
                cancellationToken: CancellationToken.None)).Configured;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check auth provider configuration status.");
            return null;
        }
    }

    public Task<AuthorizationProviderConfigurationDto> GetAuthorizationProviderConfigurationAsync() =>
        api.GetInstanceOnboardingAuthorizationProviderConfigurationInternalAsync(cancellationToken: CancellationToken.None);

    public Task<AuthorizationProviderConfigurationDto> GetAuthorizationProviderConfigurationAsAdminAsync() =>
        api.GetInstanceAuthorizationProviderConfigurationAsync(cancellationToken: CancellationToken.None);

    public Task<BaseCommandResponseOfGuid> SaveAuthorizationProviderConfigurationAsync(AuthorizationProviderConfigurationDto config) =>
        SendCommandAsync(ct => api.SaveInstanceOnboardingAuthorizationProviderConfigurationAsync(config, cancellationToken: ct));

    public Task<BaseCommandResponseOfGuid> UpdateAuthorizationProviderConfigurationAsAdminAsync(AuthorizationProviderConfigurationDto config) =>
        SendCommandAsync(ct => api.UpdateInstanceAuthorizationProviderConfigurationAsync(config, cancellationToken: ct));

    public Task<BaseCommandResponseOfGuid> SyncAuthorizationPolicyPackageAsync() =>
        SendCommandAsync(ct => api.SyncInstanceOnboardingAuthorizationPolicyPackageAsync(cancellationToken: ct));

    public Task<BaseCommandResponseOfGuid> SyncAuthorizationPolicyPackageAsAdminAsync() =>
        SendCommandAsync(ct => api.SyncInstanceAuthorizationPolicyPackageAsync(cancellationToken: ct));

    public Task<BaseCommandResponseOfGuid> VerifyCerbosEndpointAsync(string grpcEndpoint) =>
        SendCommandAsync(ct => api.VerifyInstanceOnboardingAuthorizationProviderEndpointAsync(
            body: new VerifyCerbosEndpointRequest { GrpcEndpoint = grpcEndpoint }, cancellationToken: ct));

    public async Task<bool> IsAuthorizationProviderConfiguredAsync() =>
        await GetAuthorizationProviderConfiguredStateAsync() ?? false;

    public async Task<bool> ShouldSkipAuthorizationProviderStepAsync()
    {
        // Use the lightweight, non-rate-limited status endpoint rather than the full
        // GetAuthorizationProviderConfigurationAsync() call, which hits the
        // SetupSecretPolicy-rate-limited /internal endpoint (5 req/60s per IP).
        // Both this method and the authz-provider page fire on the same render pass,
        // so using the full config endpoint here causes 429s.
        try
        {
            var status = await api.GetInstanceAuthorizationProviderConfigurationStatusAsync(
                cancellationToken: CancellationToken.None);
            var deploymentFailed = string.Equals(
                status.AuthorizationProviderBootstrapStatus,
                "failed",
                StringComparison.OrdinalIgnoreCase);

            return status.Configured == true ||
                   (status.AuthorizationProviderManagedByDeployment == true && !deploymentFailed);
        }
        catch (Exception ex)
        {
            logger.LogError(
                "Failed to resolve the authorization-provider setup destination. FailureType={FailureType}",
                ex.GetType().Name);
            return false;
        }
    }

    public async Task<bool?> GetAuthorizationProviderConfiguredStateAsync()
    {
        try
        {
            return (await api.GetInstanceAuthorizationProviderConfigurationStatusAsync(
                cancellationToken: CancellationToken.None)).Configured;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check authorization provider configuration status.");
            return null;
        }
    }

    public async Task RefreshAuthSchemesAsync()
    {
        try
        {
            var response = await bffAuthApi.RefreshSchemesAsync(CancellationToken.None);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Failed to refresh auth schemes. Status: {StatusCode}", (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh auth schemes.");
        }
    }

    public async Task<bool> RefreshAuthSessionAsync()
    {
        try
        {
            var response = await bffAuthApi.RefreshSessionInternalAsync(CancellationToken.None);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            logger.LogWarning("Failed to refresh auth session. Status: {StatusCode}", (int)response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh auth session.");
            return false;
        }
    }

    public Task<AnalyticsGovernanceSettingsDto> GetAnalyticsGovernanceSettingsAsync() =>
        GetSettingsAsync(ct => api.GetInstanceAnalyticsGovernanceSettingsAsync(cancellationToken: ct), () => new());

    public Task<BaseCommandResponseOfGuid> UpdateAnalyticsGovernanceSettingsAsync(AnalyticsGovernanceSettingsDto settings) =>
        SendCommandAsync(ct => api.UpdateInstanceAnalyticsGovernanceSettingsAsync(settings, cancellationToken: ct));

    public Task<FooterGovernanceSettingsDto> GetFooterGovernanceSettingsAsync() =>
        GetSettingsAsync(ct => api.GetFooterGovernanceSettingsAsync(cancellationToken: ct), () => new());

    public Task<BaseCommandResponseOfGuid> UpdateFooterGovernanceSettingsAsync(FooterGovernanceSettingsDto settings) =>
        SendCommandAsync(ct => api.UpdateFooterGovernanceSettingsAsync(settings, cancellationToken: ct));

    private void ApplyKeycloakBootstrapBrowserDefaults(KeycloakBootstrapRequestDto request)
    {
        if (!Uri.TryCreate(navigation.BaseUri, UriKind.Absolute, out var baseUri))
        {
            return;
        }

        var origin = $"{baseUri.Scheme}://{baseUri.Host}{(baseUri.IsDefaultPort ? string.Empty : $":{baseUri.Port}")}";
        request.BlazorRedirectUris = MergeBootstrapValues(request.BlazorRedirectUris, $"{origin.TrimEnd('/')}/*");
        request.BlazorWebOrigins = MergeBootstrapValues(request.BlazorWebOrigins, "+");
    }

    private async Task<T?> GetOptionalAsync<T>(Func<CancellationToken, Task<T>> apiCall, string description)
        where T : class
    {
        try
        {
            return await apiCall(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch {Description}.", description);
            return null;
        }
    }

    private async Task<T> GetSettingsAsync<T>(Func<CancellationToken, Task<T>> apiCall, Func<T> fallback)
        where T : class
    {
        try
        {
            return await apiCall(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch {Type}.", typeof(T).Name);
            return fallback();
        }
    }

    private async Task<BaseCommandResponseOfGuid> SendCommandAsync(
        Func<CancellationToken, Task<BaseCommandResponseOfGuid>> apiCall)
    {
        try
        {
            return await apiCall(CancellationToken.None);
        }
        catch (ApiException<ValidationProblemDetails> ex)
        {
            // NSwag generates typed ApiException<ValidationProblemDetails> for 400 responses
            // declared as [ProducesResponseType(typeof(ValidationProblemDetails), 400)].
            // The base ApiException.Response string may be empty when the typed result is populated;
            // extract errors from the strongly-typed result instead.
            logger.LogError(ex, "Failed to send command. HTTP Status={StatusCode}", ex.StatusCode);
            return MapValidationProblemDetails(ex.Result);
        }
        catch (ApiException ex)
        {
            logger.LogError(ex, "Failed to send command.");
            return MapCommandApiException(ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send command.");
            return FailedCommandResponse(ex.Message);
        }
    }

    private static BaseCommandResponseOfGuid MapValidationProblemDetails(ValidationProblemDetails? details)
    {
        if (details is null)
        {
            return FailedCommandResponse("Request failed with validation errors.");
        }

        var result = FailedCommandResponse(details.Detail ?? details.Title ?? "Validation failed.");
        result.Message = details.Detail ?? details.Title ?? result.Message;

        if (details.Errors is { Count: > 0 })
        {
            var fieldErrors = details.Errors
                .Values
                .Where(msgs => msgs is { Count: > 0 })
                .SelectMany(msgs => msgs)
                .Where(msg => !string.IsNullOrWhiteSpace(msg))
                .ToArray();

            if (fieldErrors.Length > 0)
            {
                result.Errors = fieldErrors;
            }
        }

        return result;
    }

    private static BaseCommandResponseOfGuid MapCommandApiException(ApiException exception)
    {
        var result = FailedCommandResponse($"Failed with status {exception.StatusCode}.");
        if (string.IsNullOrWhiteSpace(exception.Response))
        {
            return result;
        }

        try
        {
            using var document = JsonDocument.Parse(exception.Response);
            var root = document.RootElement;
            if (root.TryGetProperty("message", out var message))
            {
                result.Message = message.GetString();
            }

            if (root.TryGetProperty("failureCode", out var failureCode))
            {
                result.FailureCode = failureCode.GetString();
            }

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
            {
                result.Errors = errors.EnumerateArray()
                    .Select(item => item.GetString() ?? string.Empty)
                    .ToArray();
            }
            else if (root.TryGetProperty("errors", out errors) && errors.ValueKind == JsonValueKind.Object)
            {
                result.Errors = errors.EnumerateObject()
                    .Where(field => field.Value.ValueKind == JsonValueKind.Array)
                    .SelectMany(field => field.Value.EnumerateArray())
                    .Select(item => item.GetString() ?? string.Empty)
                    .ToArray();
            }
        }
        catch (JsonException)
        {
        }

        return result;
    }

    private static BaseCommandResponseOfGuid FailedCommandResponse(string message) =>
        new() { Success = false, Message = message, Errors = [message] };

    private static ICollection<string> MergeBootstrapValues(ICollection<string>? currentValues, string requiredValue) =>
        (currentValues ?? [])
            .Append(requiredValue)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static KeycloakRealmDoctorResultDto BlockedDoctor(string message) => new()
    {
        OverallStatus = "blocked",
        Message = message,
        Checks = [new KeycloakRealmDoctorCheckDto
        {
            Code = "keycloak_doctor_failed",
            Name = "Keycloak diagnostics",
            Status = "blocked",
            Message = message
        }]
    };

    private static KeycloakRealmSyncPlanDto BlockedPlan(string message) => new()
    {
        Status = "blocked",
        Message = message,
        Operations = [new KeycloakRealmSyncOperationDto
        {
            OperationId = "keycloak_sync_failed",
            Category = "inspection",
            TargetType = "realm",
            Target = "Keycloak",
            Action = "none",
            Status = "blocked",
            Summary = message,
            Reason = "The sync operation could not be completed safely."
        }]
    };

    private static KeycloakClientSecretRotationResultDto BlockedRotation(string message) => new()
    {
        Status = "blocked",
        Message = message,
        Operations = [new KeycloakRealmSyncOperationDto
        {
            OperationId = "keycloak_client_secret_rotation_failed",
            Category = "client-secret",
            TargetType = "client",
            Action = "update",
            Status = "blocked",
            Summary = message,
            Reason = "The client-secret rotation could not be completed safely."
        }]
    };
}
