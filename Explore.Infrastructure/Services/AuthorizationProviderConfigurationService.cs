// ABOUTME: Service implementation for managing instance-level authorization provider configuration.
// ABOUTME: Reads/writes authz provider settings via SystemSettings and verifies Cerbos gRPC endpoints via health check.

using System.Text.Json;
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
    private readonly ILogger<AuthorizationProviderConfigurationService> _logger;

    public AuthorizationProviderConfigurationService(
        ISystemSettingRepository systemSettingRepository,
        IConfiguration configuration,
        ILogger<AuthorizationProviderConfigurationService> logger)
    {
        _systemSettingRepository = systemSettingRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthorizationProviderConfigurationDto> ReadConfigurationAsync()
    {
        var providerSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Security.AuthorizationProvider);
        var grpcEndpointSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Cerbos.GrpcEndpoint);

        var envEndpoint = GrpcEndpointNormalizer.Normalize(_configuration["Cerbos:GrpcEndpoint"]);
        var detectedFromEnv = !string.IsNullOrWhiteSpace(envEndpoint)
                              && !envEndpoint.Equals("http://localhost:3593", StringComparison.OrdinalIgnoreCase);

        var provider = DeserializeString(providerSetting?.Value, "local");
        var grpcEndpoint = GrpcEndpointNormalizer.Normalize(DeserializeString(grpcEndpointSetting?.Value, string.Empty));

        // If env-detected endpoint exists and no explicit setting saved, use env value
        if (string.IsNullOrWhiteSpace(grpcEndpoint) && detectedFromEnv)
        {
            grpcEndpoint = envEndpoint;
        }

        return new AuthorizationProviderConfigurationDto
        {
            Provider = provider,
            CerbosGrpcEndpoint = grpcEndpoint,
            CerbosDetectedFromEnvironment = detectedFromEnv
        };
    }

    public async Task ApplyConfigurationAsync(AuthorizationProviderConfigurationDto configuration)
    {
        var normalizedEndpoint = GrpcEndpointNormalizer.Normalize(configuration.CerbosGrpcEndpoint);

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
            JsonSerializer.Serialize(configuration.Provider.Equals("cerbos", StringComparison.OrdinalIgnoreCase) ? normalizedEndpoint : string.Empty),
            SettingValueType.String,
            true,
            "Security",
            2,
            "Cerbos PDP gRPC endpoint for authorization requests");
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
