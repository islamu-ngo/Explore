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
    private readonly InstanceGovernanceSettingService _service;

    public InstanceGovernanceSettingServiceTests()
    {
        _systemSettingRepository = Substitute.For<ISystemSettingRepository>();
        _tenantCapabilityRepository = Substitute.For<ITenantCapabilityRepository>();
        _moduleDefinitionRepository = Substitute.For<IModuleDefinitionRepository>();

        _service = new InstanceGovernanceSettingService(
            _systemSettingRepository,
            _tenantCapabilityRepository,
            _moduleDefinitionRepository);
    }

    [Test]
    public async Task ReadSettingsAsync_WhenRenderPolicySettingsMissing_ReturnsSafeDefaults()
    {
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);

        var result = await _service.ReadSettingsAsync();

        await Assert.That(result.RenderPolicyVersion).IsEqualTo(1);
        await Assert.That(result.RenderPolicyPreset).IsEqualTo("SeoBalanced");
        await Assert.That(result.PublicSeoRenderMode).IsEqualTo("InteractiveAuto");
        await Assert.That(result.PublicSeoPrerenderEnabled).IsTrue();
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
            RenderPolicyPreset = "SeoBalanced",
            EnableAdvancedRenderPolicyOverrides = false,
            PublicSeoRenderMode = "InteractiveAuto",
            PublicSeoPrerenderEnabled = true,
            OperationalRenderMode = "InteractiveAuto",
            OperationalPrerenderEnabled = false,
            AdminRenderMode = "InteractiveAuto",
            AdminPrerenderEnabled = false,
            OnboardingRenderMode = "InteractiveServer",
            OnboardingPrerenderEnabled = false,
            DisallowInteractiveServerOnOnboarding = false,
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
}
