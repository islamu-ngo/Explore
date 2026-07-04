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
        _systemSettings.GetAllSettings(Arg.Any<string?>()).Returns([]);
        _tenantSettings.GetByTenantAndKey(Arg.Any<Guid>(), Arg.Any<string>()).Returns((TenantSetting?)null);
        _tenantSettings.GetAllForTenant(Arg.Any<Guid>()).Returns([]);
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
        UseSystemSettings(
            CreateSystemSetting(GovernanceSettingKeys.Deployment.Mode, "\"MultiTenant\""),
            CreateSystemSetting(GovernanceSettingKeys.TenantDelegation.LockMcp, "true"),
            CreateSystemSetting(GovernanceSettingKeys.Mcp.Enabled, "true"));
        UseTenantSettings(tenantId, CreateTenantSetting(tenantId, GovernanceSettingKeys.Mcp.Enabled, "false"));

        var result = await _service.ReadEffectiveTenantSettingsAsync(tenantId);

        await Assert.That(result.CanOverrideMcp).IsFalse();
        await Assert.That(result.McpEnabled).IsTrue();
    }

    [Test]
    public async Task ReadEffectiveTenantSettingsAsync_WhenMcpUnlocked_AppliesTenantOverride()
    {
        var tenantId = Guid.NewGuid();
        UseSystemSettings(
            CreateSystemSetting(GovernanceSettingKeys.Deployment.Mode, "\"MultiTenant\""),
            CreateSystemSetting(GovernanceSettingKeys.TenantDelegation.LockMcp, "false"),
            CreateSystemSetting(GovernanceSettingKeys.Mcp.Enabled, "true"));
        UseTenantSettings(tenantId, CreateTenantSetting(tenantId, GovernanceSettingKeys.Mcp.Enabled, "false"));

        var result = await _service.ReadEffectiveTenantSettingsAsync(tenantId);

        await Assert.That(result.CanOverrideMcp).IsTrue();
        await Assert.That(result.McpEnabled).IsFalse();
    }

    [Test]
    public async Task ReadEffectiveTenantSettingsAsync_UsesBatchedSettingReads()
    {
        var tenantId = Guid.NewGuid();

        await _service.ReadEffectiveTenantSettingsAsync(tenantId);

        await _systemSettings.Received(1).GetAllSettings(Arg.Any<string?>());
        await _tenantSettings.Received(1).GetAllForTenant(tenantId);
        await _systemSettings.DidNotReceive().GetByKey(Arg.Any<string>());
        await _tenantSettings.DidNotReceive().GetByTenantAndKey(Arg.Any<Guid>(), Arg.Any<string>());
    }

    private void UseSystemSettings(params SystemSetting[] settings)
    {
        _systemSettings.GetAllSettings(Arg.Any<string?>()).Returns(settings.ToList());
    }

    private void UseTenantSettings(Guid tenantId, params TenantSetting[] settings)
    {
        _tenantSettings.GetAllForTenant(tenantId).Returns(settings.ToList());
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
