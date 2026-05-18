// ABOUTME: Service implementation for managing instance-level authorization provider configuration.
// ABOUTME: Reads/writes authz provider settings via SystemSettings and verifies Cerbos gRPC endpoints via health check.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Utilities;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Services;

public class AuthorizationProviderConfigurationService : IAuthorizationProviderConfigurationService
{
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly IConfiguration _configuration;
    private readonly CerbosAdminEndpointValidator _adminEndpointValidator;
    private readonly IAuthorizationProviderModeCacheInvalidator _providerModeCacheInvalidator;
    private readonly ICerbosConfigResolver _cerbosConfigResolver;
    private readonly ILogger<AuthorizationProviderConfigurationService> _logger;

    public AuthorizationProviderConfigurationService(
        ISystemSettingRepository systemSettingRepository,
        IConfiguration configuration,
        CerbosAdminEndpointValidator adminEndpointValidator,
        IAuthorizationProviderModeCacheInvalidator providerModeCacheInvalidator,
        ICerbosConfigResolver cerbosConfigResolver,
        ILogger<AuthorizationProviderConfigurationService> logger)
    {
        _systemSettingRepository = systemSettingRepository;
        _configuration = configuration;
        _adminEndpointValidator = adminEndpointValidator;
        _providerModeCacheInvalidator = providerModeCacheInvalidator;
        _cerbosConfigResolver = cerbosConfigResolver;
        _logger = logger;
    }

    public async Task<AuthorizationProviderConfigurationDto> ReadConfigurationAsync()
    {
        var providerSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Security.AuthorizationProvider);
        var grpcEndpointSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Cerbos.GrpcEndpoint);
        var adminEndpointSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Cerbos.CustomAdminEndpoint);
        var adminUsernameSetting = await _systemSettingRepository.GetByKey(InfrastructureSecretSettingKeys.Cerbos.CustomAdminUsername);
        var adminPasswordSetting = await _systemSettingRepository.GetByKey(InfrastructureSecretSettingKeys.Cerbos.CustomAdminPassword);

        // Preserve operator's raw value end-to-end. Detection compares against the local default using
        // a normalized form, but the value surfaced to UI/storage stays exactly as the operator typed it
        // in Infisical/env (e.g. `cerbosgrpc.openislamu.org:443`, no auto-prepended scheme).
        var rawEnvEndpoint = _configuration["Cerbos:GrpcEndpoint"]?.Trim() ?? string.Empty;
        var detectedFromEnv = !string.IsNullOrWhiteSpace(rawEnvEndpoint)
                              && !GrpcEndpointNormalizer.Normalize(rawEnvEndpoint)
                                  .Equals("http://localhost:3593", StringComparison.OrdinalIgnoreCase);

        var provider = DeserializeString(providerSetting?.Value, "local");
        var grpcEndpoint = DeserializeString(grpcEndpointSetting?.Value, string.Empty);

        if (string.IsNullOrWhiteSpace(grpcEndpoint) && detectedFromEnv)
        {
            grpcEndpoint = rawEnvEndpoint;
        }

        return new AuthorizationProviderConfigurationDto
        {
            Provider = provider,
            CerbosGrpcEndpoint = grpcEndpoint,
            CerbosAdminEndpoint = DeserializeString(adminEndpointSetting?.Value, string.Empty),
            CerbosAdminUsername = null,
            CerbosAdminPassword = null,
            CerbosAdminUsernameConfigured = !string.IsNullOrWhiteSpace(DeserializeString(adminUsernameSetting?.Value, string.Empty)),
            CerbosAdminPasswordConfigured = !string.IsNullOrWhiteSpace(DeserializeString(adminPasswordSetting?.Value, string.Empty)),
            CerbosDetectedFromEnvironment = detectedFromEnv
        };
    }

    public async Task ApplyConfigurationAsync(AuthorizationProviderConfigurationDto configuration)
    {
        var rawEndpoint = configuration.CerbosGrpcEndpoint?.Trim() ?? string.Empty;
        var isCerbosProvider = configuration.Provider.Equals("cerbos", StringComparison.OrdinalIgnoreCase);
        var rawAdminEndpoint = configuration.CerbosAdminEndpoint?.Trim() ?? string.Empty;
        Uri? normalizedAdminEndpoint = null;

        if (isCerbosProvider && !string.IsNullOrWhiteSpace(rawAdminEndpoint))
        {
            if (!_adminEndpointValidator.TryNormalize(rawAdminEndpoint, isByo: true, out normalizedAdminEndpoint, out var warning))
                throw new InvalidOperationException(warning);
        }

        await UpsertSettingAsync(
            GovernanceSettingKeys.Security.AuthorizationProvider,
            JsonSerializer.Serialize(configuration.Provider.ToLowerInvariant()),
            SettingValueType.String,
            true,
            "Security",
            1,
            "Authorization provider: 'cerbos' for external PDP, 'local' for built-in RBAC");

        await UpsertSettingAsync(
            GovernanceSettingKeys.Cerbos.GrpcEndpoint,
            JsonSerializer.Serialize(isCerbosProvider ? rawEndpoint : string.Empty),
            SettingValueType.String,
            true,
            "Security",
            2,
            "Cerbos PDP gRPC endpoint for authorization requests");

        if (!isCerbosProvider)
        {
            _cerbosConfigResolver.InvalidateCache();
            _providerModeCacheInvalidator.InvalidateInstanceMode();
            return;
        }

        if (normalizedAdminEndpoint is not null)
        {
            await UpsertSettingAsync(
                GovernanceSettingKeys.Cerbos.CustomAdminEndpoint,
                JsonSerializer.Serialize(normalizedAdminEndpoint.GetLeftPart(UriPartial.Path).TrimEnd('/')),
                SettingValueType.String,
                true,
                "Security",
                3,
                "Cerbos Admin API endpoint for policy package publishing");
        }

        if (!string.IsNullOrWhiteSpace(configuration.CerbosAdminUsername))
        {
            await UpsertSettingAsync(
                InfrastructureSecretSettingKeys.Cerbos.CustomAdminUsername,
                JsonSerializer.Serialize(configuration.CerbosAdminUsername.Trim()),
                SettingValueType.String,
                true,
                "Security",
                4,
                "Cerbos Admin API username");
        }

        if (!string.IsNullOrWhiteSpace(configuration.CerbosAdminPassword))
        {
            await UpsertSettingAsync(
                InfrastructureSecretSettingKeys.Cerbos.CustomAdminPassword,
                JsonSerializer.Serialize(configuration.CerbosAdminPassword),
                SettingValueType.String,
                true,
                "Security",
                5,
                "Cerbos Admin API password");
        }

        _cerbosConfigResolver.InvalidateCache();
        _providerModeCacheInvalidator.InvalidateInstanceMode();
    }

    public async Task<bool> IsConfiguredAsync()
    {
        var providerSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Security.AuthorizationProvider);
        return providerSetting is not null && !string.IsNullOrWhiteSpace(providerSetting.Value);
    }

    public async Task<bool> VerifyCerbosEndpointAsync(string grpcEndpoint, CancellationToken cancellationToken = default)
    {
        var normalizedEndpoint = GrpcEndpointNormalizer.Normalize(grpcEndpoint);
        if (string.IsNullOrWhiteSpace(normalizedEndpoint))
        {
            return false;
        }

        try
        {
            using var channel = GrpcChannel.ForAddress(
                normalizedEndpoint,
                CerbosGrpcChannelOptionsFactory.Create());

            var client = new Health.HealthClient(channel);
            var response = await client.CheckAsync(
                new HealthCheckRequest(),
                deadline: DateTime.UtcNow.AddSeconds(5),
                cancellationToken: cancellationToken);

            var isHealthy = response.Status == HealthCheckResponse.Types.ServingStatus.Serving;
            _logger.LogInformation("Cerbos gRPC health check for {Endpoint}: {Status}", normalizedEndpoint, response.Status);
            return isHealthy;
        }
        catch (RpcException rpcEx)
        {
            _logger.LogWarning(
                rpcEx,
                "Cerbos gRPC health check failed for endpoint {Endpoint}: gRPC status={GrpcStatusCode} detail={GrpcStatusDetail}",
                normalizedEndpoint,
                rpcEx.StatusCode,
                rpcEx.Status.Detail);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Cerbos gRPC health check failed for endpoint {Endpoint}: exception={ExceptionType} message={ExceptionMessage}",
                normalizedEndpoint,
                ex.GetType().FullName,
                ex.Message);
            return false;
        }
    }

    public Task<bool> VerifyCerbosAdminEndpointAsync(string adminEndpoint, CancellationToken cancellationToken = default)
    {
        var isSafe = _adminEndpointValidator.TryNormalize(adminEndpoint, isByo: true, out _, out var warning);
        if (!isSafe)
        {
            _logger.LogWarning("Cerbos Admin API endpoint validation failed: {Reason}", warning);
        }

        return Task.FromResult(isSafe);
    }

    private async Task UpsertSettingAsync(
        string settingKey,
        string value,
        SettingValueType valueType,
        bool isLocked,
        string category,
        int displayOrder,
        string description)
    {
        var existing = await _systemSettingRepository.GetByKey(settingKey);

        if (existing == null)
        {
            await _systemSettingRepository.Create(new SystemSetting
            {
                SettingKey = settingKey,
                Value = value,
                ValueType = valueType,
                IsLocked = isLocked,
                Description = description,
                Category = category,
                DisplayOrder = displayOrder,
                CreatedAt = DateTime.UtcNow
            });

            return;
        }

        existing.Value = value;
        existing.ValueType = valueType;
        existing.IsLocked = isLocked;
        existing.Description = description;
        existing.Category = category;
        existing.DisplayOrder = displayOrder;
        existing.UpdatedAt = DateTime.UtcNow;

        await _systemSettingRepository.Update(existing);
    }

    private static string DeserializeString(string? rawValue, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return defaultValue;

        try
        {
            var deserialized = JsonSerializer.Deserialize<string>(rawValue);
            return string.IsNullOrWhiteSpace(deserialized) ? defaultValue : deserialized;
        }
        catch
        {
            return rawValue.Trim('"');
        }
    }
}
