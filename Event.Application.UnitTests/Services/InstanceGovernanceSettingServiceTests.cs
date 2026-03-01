// ABOUTME: Unit tests for InstanceGovernanceSettingService runtime render-policy defaults and persistence behavior.
// ABOUTME: Verifies safe defaults and onboarding InteractiveServer guardrail normalization.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Modules;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

public class InstanceGovernanceSettingServiceTests
{
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ITenantCapabilityRepository _tenantCapabilityRepository;
    private readonly IModuleDefinitionRepository _moduleDefinitionRepository;
    private readonly ITenantSettingRepository _tenantSettingRepository;
    private readonly InstanceGovernanceSettingService _service;

    public InstanceGovernanceSettingServiceTests()
    {
        _systemSettingRepository = Substitute.For<ISystemSettingRepository>();
        _tenantCapabilityRepository = Substitute.For<ITenantCapabilityRepository>();
        _moduleDefinitionRepository = Substitute.For<IModuleDefinitionRepository>();
        _tenantSettingRepository = Substitute.For<ITenantSettingRepository>();

        _service = new InstanceGovernanceSettingService(
            _systemSettingRepository,
            _tenantCapabilityRepository,
            _moduleDefinitionRepository,
            _tenantSettingRepository);
    }

    [Test]
    public async Task ReadSettingsAsync_WhenRenderPolicySettingsMissing_ReturnsSafeDefaults()
    {
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);

        var result = await _service.ReadSettingsAsync();

        await Assert.That(result.RenderPolicyVersion).IsEqualTo(1);
        await Assert.That(result.RenderPolicyPreset).IsEqualTo("AllInteractiveServer");
        await Assert.That(result.PublicSeoRenderMode).IsEqualTo("InteractiveServer");
        await Assert.That(result.PublicSeoPrerenderEnabled).IsFalse();
        await Assert.That(result.OnboardingRenderMode).IsEqualTo("InteractiveAuto");
        await Assert.That(result.DisallowInteractiveServerOnOnboarding).IsTrue();
    }

    [Test]
    public async Task ApplySettingsAsync_WhenOnboardingInteractiveServerProvided_NormalizesAndPersistsGuardrail()
    {
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);
        _moduleDefinitionRepository.GetByKey(Arg.Any<string>()).Returns((ModuleDefinition?)null);

        var settings = new InstanceGovernanceSettingsDto
        {
            DeploymentMode = "SingleTenant",
            RenderPolicyVersion = 1,
            RenderPolicyPreset = "AllInteractiveServer",
            EnableAdvancedRenderPolicyOverrides = false,
            GlobalRenderMode = "InteractiveServer",
            GlobalPrerenderEnabled = false,
            PublicSeoRenderMode = "InteractiveServer",
            PublicSeoPrerenderEnabled = false,
            OperationalRenderMode = "InteractiveServer",
            OperationalPrerenderEnabled = false,
            AdminRenderMode = "InteractiveServer",
            AdminPrerenderEnabled = false,
            OnboardingRenderMode = "InteractiveServer",
            OnboardingPrerenderEnabled = false,
            DisallowInteractiveServerOnOnboarding = true,
            DefaultPublicHomePage = "EventList",
            EnableIslamicModule = true,
            EnableTechModule = true,
            AuthorizationProvider = "local"
        };

        await _service.ApplySettingsAsync(Guid.NewGuid(), settings, Guid.NewGuid());

        await _systemSettingRepository.Received().Create(Arg.Is<SystemSetting>(
            s => s.SettingKey == GovernanceSettingKeys.RoutingRenderPolicyOnboardingRenderMode
                 && s.Value == "\"InteractiveAuto\""));

        await _systemSettingRepository.Received().Create(Arg.Is<SystemSetting>(
            s => s.SettingKey == GovernanceSettingKeys.RoutingRenderPolicyDisallowInteractiveServerOnOnboarding
                 && s.Value == "true"));
    }

    [Test]
    public async Task ReadEffectiveSettingsForTenantAsync_WhenOverrideDisabled_ReturnsInstanceSettings()
    {
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);

        var tenantId = Guid.NewGuid();
        _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.RoutingRenderPolicyPreset)
            .Returns(new TenantSetting { Tenant = null!, TenantId = tenantId, SettingKey = GovernanceSettingKeys.RoutingRenderPolicyPreset, Value = "\"SeoBalanced\"" });

        var result = await _service.ReadEffectiveSettingsForTenantAsync(tenantId);

        await Assert.That(result.RenderPolicyPreset).IsEqualTo("AllInteractiveServer");
        await Assert.That(result.AllowTenantRenderPolicyOverride).IsFalse();
    }

    [Test]
    public async Task ReadEffectiveSettingsForTenantAsync_WhenOverrideEnabled_AppliesTenantPreset()
    {
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingRenderPolicyAllowTenantOverride)
            .Returns(new SystemSetting { SettingKey = GovernanceSettingKeys.RoutingRenderPolicyAllowTenantOverride, Value = "true" });

        var tenantId = Guid.NewGuid();
        _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.RoutingRenderPolicyPreset)
            .Returns(new TenantSetting { Tenant = null!, TenantId = tenantId, SettingKey = GovernanceSettingKeys.RoutingRenderPolicyPreset, Value = "\"SeoBalanced\"" });

        var result = await _service.ReadEffectiveSettingsForTenantAsync(tenantId);

        await Assert.That(result.RenderPolicyPreset).IsEqualTo("SeoBalanced");
    }

    [Test]
    public async Task ReadEffectiveSettingsForTenantAsync_WhenRouteGroupLocked_IgnoresTenantOverride()
    {
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingRenderPolicyAllowTenantOverride)
            .Returns(new SystemSetting { SettingKey = GovernanceSettingKeys.RoutingRenderPolicyAllowTenantOverride, Value = "true" });
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingRenderPolicyLockTenantPublicSeo)
            .Returns(new SystemSetting { SettingKey = GovernanceSettingKeys.RoutingRenderPolicyLockTenantPublicSeo, Value = "true" });

        var tenantId = Guid.NewGuid();
        _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.RoutingRenderPolicyPublicSeoRenderMode)
            .Returns(new TenantSetting { Tenant = null!, TenantId = tenantId, SettingKey = GovernanceSettingKeys.RoutingRenderPolicyPublicSeoRenderMode, Value = "\"InteractiveWebAssembly\"" });

        var result = await _service.ReadEffectiveSettingsForTenantAsync(tenantId);

        await Assert.That(result.PublicSeoRenderMode).IsEqualTo("InteractiveServer");
    }

    [Test]
    public async Task ReadEffectiveSettingsForTenantAsync_WhenRouteGroupUnlocked_AppliesTenantOverride()
    {
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingRenderPolicyAllowTenantOverride)
            .Returns(new SystemSetting { SettingKey = GovernanceSettingKeys.RoutingRenderPolicyAllowTenantOverride, Value = "true" });

        var tenantId = Guid.NewGuid();
        _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.RoutingRenderPolicyOperationalRenderMode)
            .Returns(new TenantSetting { Tenant = null!, TenantId = tenantId, SettingKey = GovernanceSettingKeys.RoutingRenderPolicyOperationalRenderMode, Value = "\"InteractiveWebAssembly\"" });

        var result = await _service.ReadEffectiveSettingsForTenantAsync(tenantId);

        await Assert.That(result.OperationalRenderMode).IsEqualTo("InteractiveWebAssembly");
    }

    [Test]
    public async Task ReadSettingsAsync_ReadsNewLockFields()
    {
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingRenderPolicyAllowTenantOverride)
            .Returns(new SystemSetting { SettingKey = GovernanceSettingKeys.RoutingRenderPolicyAllowTenantOverride, Value = "true" });
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingRenderPolicyLockTenantPublicSeo)
            .Returns(new SystemSetting { SettingKey = GovernanceSettingKeys.RoutingRenderPolicyLockTenantPublicSeo, Value = "true" });

        var result = await _service.ReadSettingsAsync();

        await Assert.That(result.AllowTenantRenderPolicyOverride).IsTrue();
        await Assert.That(result.LockTenantPublicSeoRenderPolicy).IsTrue();
        await Assert.That(result.LockTenantOperationalRenderPolicy).IsFalse();
        await Assert.That(result.LockTenantAdminRenderPolicy).IsFalse();
    }
}
