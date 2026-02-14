using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.PublicExperience.Handlers.Queries;
using Explore.Application.Features.PublicExperience.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Constants;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.PublicExperience.Queries;

public class GetPublicExperienceSettingsQueryHandlerTests
{
    private readonly ITenantContext _tenantContext;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ITenantPolicySettingService _policySettingService;
    private readonly IModuleService _moduleService;
    private readonly GetPublicExperienceSettingsQueryHandler _handler;

    public GetPublicExperienceSettingsQueryHandlerTests()
    {
        _tenantContext = Substitute.For<ITenantContext>();
        _systemSettingRepository = Substitute.For<ISystemSettingRepository>();
        _policySettingService = Substitute.For<ITenantPolicySettingService>();
        _moduleService = Substitute.For<IModuleService>();

        _handler = new GetPublicExperienceSettingsQueryHandler(
            _tenantContext,
            _systemSettingRepository,
            _policySettingService,
            _moduleService);
    }

    [Test]
    public async Task Handle_WithEnabledIslamicAndTechModules_SetsCapabilityFlagsAndEnabledModuleList()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);

        _policySettingService.ReadEffectiveTenantSettingsAsync(tenantId).Returns(new TenantPolicySettingsDto
        {
            PreferredHomePage = "EventList",
            BrandDisplayName = "Tenant Brand"
        });

        _moduleService.GetEnabledModulesAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ModuleInfo>
            {
                new() { ModuleKey = "Mod_Islamic", Name = "Islamic" },
                new() { ModuleKey = "Mod_Tech", Name = "Tech" },
                new() { ModuleKey = "Mod_Other", Name = "Other" }
            });

        _systemSettingRepository.GetByKey(GovernanceSettingKeys.DeploymentMode).Returns(new SystemSetting
        {
            SettingKey = GovernanceSettingKeys.DeploymentMode,
            Value = "\"MultiTenant\""
        });

        // Act
        var result = await _handler.Handle(new GetPublicExperienceSettingsQuery(), CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.TenantId).IsEqualTo(tenantId);
        await Assert.That(result.DeploymentMode).IsEqualTo("MultiTenant");
        await Assert.That(result.IsIslamicModuleEnabled).IsTrue();
        await Assert.That(result.IsTechModuleEnabled).IsTrue();
        await Assert.That(result.EnabledModules).Contains("Mod_Islamic");
        await Assert.That(result.EnabledModules).Contains("Mod_Tech");
        await Assert.That(result.EnabledModules).Contains("Mod_Other");

        await _moduleService.Received(1).GetEnabledModulesAsync(tenantId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenDeploymentModeSettingIsMissing_DefaultsToSingleTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);

        _policySettingService.ReadEffectiveTenantSettingsAsync(tenantId).Returns(new TenantPolicySettingsDto());
        _moduleService.GetEnabledModulesAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ModuleInfo>());
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.DeploymentMode).Returns((SystemSetting?)null);

        // Act
        var result = await _handler.Handle(new GetPublicExperienceSettingsQuery(), CancellationToken.None);

        // Assert
        await Assert.That(result.DeploymentMode).IsEqualTo("SingleTenant");
        await Assert.That(result.IsIslamicModuleEnabled).IsFalse();
        await Assert.That(result.IsTechModuleEnabled).IsFalse();
        await Assert.That(result.EnabledModules.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_WhenDeploymentModeIsRawStringWithoutJson_UsesTrimmedRawValue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);

        _policySettingService.ReadEffectiveTenantSettingsAsync(tenantId).Returns(new TenantPolicySettingsDto());
        _moduleService.GetEnabledModulesAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ModuleInfo> { new() { ModuleKey = "Mod_Islamic", Name = "Islamic" } });

        _systemSettingRepository.GetByKey(GovernanceSettingKeys.DeploymentMode).Returns(new SystemSetting
        {
            SettingKey = GovernanceSettingKeys.DeploymentMode,
            Value = "SingleTenant"
        });

        // Act
        var result = await _handler.Handle(new GetPublicExperienceSettingsQuery(), CancellationToken.None);

        // Assert
        await Assert.That(result.DeploymentMode).IsEqualTo("SingleTenant");
        await Assert.That(result.IsIslamicModuleEnabled).IsTrue();
        await Assert.That(result.IsTechModuleEnabled).IsFalse();
    }
}
