// ABOUTME: Unit tests for tenant policy effective setting resolution.
// ABOUTME: Protects MCP runtime defaults and tenant-lock behavior in the tenant admin read model.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Constants;
using MediatR;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

public sealed class TenantPolicySettingServiceTests
{
    private readonly ISystemSettingRepository _systemSettings = Substitute.For<ISystemSettingRepository>();
    private readonly ITenantSettingRepository _tenantSettings = Substitute.For<ITenantSettingRepository>();
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly TenantPolicySettingService _service;

    public TenantPolicySettingServiceTests()
    {
        _systemSettings.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);
        _tenantSettings.GetByTenantAndKey(Arg.Any<Guid>(), Arg.Any<string>()).Returns((TenantSetting?)null);
        _tenants.GetById(Arg.Any<Guid>()).Returns((Tenant?)null);

        _service = new TenantPolicySettingService(
            _systemSettings,
            _tenantSettings,
            _tenants,
            Substitute.For<IMediator>());
    }

    [Test]
    public async Task ReadEffectiveTenantSettingsAsync_WhenMcpSettingsMissing_DefaultsRuntimeMcpEnabled()
    {
        var result = await _service.ReadEffectiveTenantSettingsAsync(Guid.NewGuid());

        await Assert.That(result.McpEnabled).IsTrue();
        await Assert.That(result.McpEnableLegacySse).IsFalse();
    }

    [Test]
    public async Task ReadEffectiveTenantSettingsAsync_WhenMcpLocked_IgnoresTenantOverride()
    {
        var tenantId = Guid.NewGuid();
        _systemSettings.GetByKey(GovernanceSettingKeys.Deployment.Mode)
            .Returns(CreateSystemSetting(GovernanceSettingKeys.Deployment.Mode, "\"MultiTenant\""));
        _systemSettings.GetByKey(GovernanceSettingKeys.TenantDelegation.LockMcp)
            .Returns(CreateSystemSetting(GovernanceSettingKeys.TenantDelegation.LockMcp, "true"));
        _systemSettings.GetByKey(GovernanceSettingKeys.Mcp.Enabled)
            .Returns(CreateSystemSetting(GovernanceSettingKeys.Mcp.Enabled, "true"));
        _tenantSettings.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Mcp.Enabled)
            .Returns(CreateTenantSetting(tenantId, GovernanceSettingKeys.Mcp.Enabled, "false"));

        var result = await _service.ReadEffectiveTenantSettingsAsync(tenantId);

        await Assert.That(result.CanOverrideMcp).IsFalse();
        await Assert.That(result.McpEnabled).IsTrue();
    }

    [Test]
    public async Task ReadEffectiveTenantSettingsAsync_WhenMcpUnlocked_AppliesTenantOverride()
    {
        var tenantId = Guid.NewGuid();
        _systemSettings.GetByKey(GovernanceSettingKeys.Deployment.Mode)
            .Returns(CreateSystemSetting(GovernanceSettingKeys.Deployment.Mode, "\"MultiTenant\""));
        _systemSettings.GetByKey(GovernanceSettingKeys.TenantDelegation.LockMcp)
            .Returns(CreateSystemSetting(GovernanceSettingKeys.TenantDelegation.LockMcp, "false"));
        _systemSettings.GetByKey(GovernanceSettingKeys.Mcp.Enabled)
            .Returns(CreateSystemSetting(GovernanceSettingKeys.Mcp.Enabled, "true"));
        _tenantSettings.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Mcp.Enabled)
            .Returns(CreateTenantSetting(tenantId, GovernanceSettingKeys.Mcp.Enabled, "false"));

        var result = await _service.ReadEffectiveTenantSettingsAsync(tenantId);

        await Assert.That(result.CanOverrideMcp).IsTrue();
        await Assert.That(result.McpEnabled).IsFalse();
    }

    private static SystemSetting CreateSystemSetting(string key, string value) => new()
    {
        SettingKey = key,
        Value = value
    };

    private static TenantSetting CreateTenantSetting(Guid tenantId, string key, string value) => new()
    {
        TenantId = tenantId,
        Tenant = null!,
        SettingKey = key,
        Value = value
    };
}
