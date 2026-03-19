// ABOUTME: Unit tests for InstanceGovernanceSettingService runtime render-policy defaults and persistence behavior.
// ABOUTME: Verifies batch resolution via IHierarchicalSettingsResolver, safe defaults, and normalization.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Instance;
using Explore.Application.Services;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

public class InstanceGovernanceSettingServiceTests
{
    private readonly IHierarchicalSettingsResolver _resolver;
    private readonly SettingUpsertService _upsertService;
    private readonly IModuleCapabilityService _moduleCapabilityService;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly InstanceGovernanceSettingService _service;

    public InstanceGovernanceSettingServiceTests()
    {
        _resolver = Substitute.For<IHierarchicalSettingsResolver>();
        _systemSettingRepository = Substitute.For<ISystemSettingRepository>();
        var mediator = Substitute.For<IMediator>();
        _upsertService = new SettingUpsertService(_systemSettingRepository, mediator);
        _moduleCapabilityService = Substitute.For<IModuleCapabilityService>();
        var logger = Substitute.For<ILogger<InstanceGovernanceSettingService>>();

        _service = new InstanceGovernanceSettingService(
            _resolver,
            _upsertService,
            _moduleCapabilityService,
            logger);
    }

    [Test]
    public async Task ReadSettingsAsync_WhenRenderPolicySettingsMissing_ReturnsSafeDefaults()
    {
        SetupEmptyBatchResolve();

        var result = await _service.ReadSettingsAsync();

        await Assert.That(result.RenderPolicy.RenderPolicyVersion).IsEqualTo(1);
        await Assert.That(result.RenderPolicy.RenderPolicyPreset).IsEqualTo("AllInteractiveServer");
        await Assert.That(result.RenderPolicy.PublicSeoRenderMode).IsEqualTo("InteractiveServer");
        await Assert.That(result.RenderPolicy.PublicSeoPrerenderEnabled).IsFalse();
        await Assert.That(result.RenderPolicy.OnboardingRenderMode).IsEqualTo("InteractiveAuto");
        await Assert.That(result.RenderPolicy.DisallowInteractiveServerOnOnboarding).IsTrue();
    }

    [Test]
    public async Task ReadSettingsAsync_UsesResolverBatchLoading_NotIndividualQueries()
    {
        SetupEmptyBatchResolve();

        await _service.ReadSettingsAsync();

        await _resolver.Received(1).ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<SettingContext>(c => c.TenantId == null),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplySettingsAsync_WhenOnboardingInteractiveServerProvided_NormalizesAndPersistsGuardrail()
    {
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);

        var settings = CreateValidSettings();
        settings.RenderPolicy.OnboardingRenderMode = "InteractiveServer";
        settings.RenderPolicy.DisallowInteractiveServerOnOnboarding = true;

        await _service.ApplySettingsAsync(Guid.NewGuid(), settings, Guid.NewGuid());

        await _systemSettingRepository.Received().Create(Arg.Is<SystemSetting>(
            s => s.SettingKey == GovernanceSettingKeys.Routing.RenderPolicy.Onboarding.RenderMode
                 && s.Value == "\"InteractiveAuto\""));

        await _systemSettingRepository.Received().Create(Arg.Is<SystemSetting>(
            s => s.SettingKey == GovernanceSettingKeys.Routing.RenderPolicy.DisallowInteractiveServerOnOnboarding
                 && s.Value == "true"));
    }

    [Test]
    public async Task ReadEffectiveSettingsForTenantAsync_WhenOverrideDisabled_ReturnsInstanceSettings()
    {
        SetupEmptyBatchResolve();

        var tenantId = Guid.NewGuid();
        var result = await _service.ReadEffectiveSettingsForTenantAsync(tenantId);

        await Assert.That(result.RenderPolicy.RenderPolicyPreset).IsEqualTo("AllInteractiveServer");
        await Assert.That(result.RenderPolicy.AllowTenantRenderPolicyOverride).IsFalse();
    }

    [Test]
    public async Task ReadEffectiveSettingsForTenantAsync_WhenOverrideEnabled_AppliesTenantPreset()
    {
        var instanceResolved = CreateResolvedSettingsWithOverrideEnabled();
        _resolver.ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<SettingContext>(c => c.TenantId == null),
            Arg.Any<CancellationToken>())
            .Returns(instanceResolved);

        var tenantResolved = new List<ResolvedSetting>
        {
            CreateResolvedSetting(GovernanceSettingKeys.Routing.RenderPolicy.Preset, "\"SeoBalanced\"")
        };
        _resolver.ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<SettingContext>(c => c.TenantId != null),
            Arg.Any<CancellationToken>())
            .Returns(tenantResolved);

        var tenantId = Guid.NewGuid();
        var result = await _service.ReadEffectiveSettingsForTenantAsync(tenantId);

        await Assert.That(result.RenderPolicy.RenderPolicyPreset).IsEqualTo("SeoBalanced");
    }

    [Test]
    public async Task ReadEffectiveSettingsForTenantAsync_WhenRouteGroupLocked_IgnoresTenantOverride()
    {
        var instanceResolved = CreateResolvedSettingsWithOverrideEnabled();
        instanceResolved.Add(CreateResolvedSetting(
            GovernanceSettingKeys.Routing.RenderPolicy.LockTenantPublicSeo, "true"));
        _resolver.ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<SettingContext>(c => c.TenantId == null),
            Arg.Any<CancellationToken>())
            .Returns(instanceResolved);

        var tenantResolved = new List<ResolvedSetting>
        {
            CreateResolvedSetting(GovernanceSettingKeys.Routing.RenderPolicy.PublicSeo.RenderMode, "\"InteractiveWebAssembly\"")
        };
        _resolver.ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<SettingContext>(c => c.TenantId != null),
            Arg.Any<CancellationToken>())
            .Returns(tenantResolved);

        var tenantId = Guid.NewGuid();
        var result = await _service.ReadEffectiveSettingsForTenantAsync(tenantId);

        await Assert.That(result.RenderPolicy.PublicSeoRenderMode).IsEqualTo("InteractiveServer");
    }

    [Test]
    public async Task ReadEffectiveSettingsForTenantAsync_WhenRouteGroupUnlocked_AppliesTenantOverride()
    {
        var instanceResolved = CreateResolvedSettingsWithOverrideEnabled();
        _resolver.ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<SettingContext>(c => c.TenantId == null),
            Arg.Any<CancellationToken>())
            .Returns(instanceResolved);

        var tenantResolved = new List<ResolvedSetting>
        {
            CreateResolvedSetting(GovernanceSettingKeys.Routing.RenderPolicy.Operational.RenderMode, "\"InteractiveWebAssembly\"")
        };
        _resolver.ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<SettingContext>(c => c.TenantId != null),
            Arg.Any<CancellationToken>())
            .Returns(tenantResolved);

        var tenantId = Guid.NewGuid();
        var result = await _service.ReadEffectiveSettingsForTenantAsync(tenantId);

        await Assert.That(result.RenderPolicy.OperationalRenderMode).IsEqualTo("InteractiveWebAssembly");
    }

    [Test]
    public async Task ReadSettingsAsync_ReadsNewLockFields()
    {
        var resolved = new List<ResolvedSetting>
        {
            CreateResolvedSetting(GovernanceSettingKeys.Routing.RenderPolicy.AllowTenantOverride, "true"),
            CreateResolvedSetting(GovernanceSettingKeys.Routing.RenderPolicy.LockTenantPublicSeo, "true")
        };
        _resolver.ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<SettingContext>(),
            Arg.Any<CancellationToken>())
            .Returns(resolved);

        var result = await _service.ReadSettingsAsync();

        await Assert.That(result.RenderPolicy.AllowTenantRenderPolicyOverride).IsTrue();
        await Assert.That(result.RenderPolicy.LockTenantPublicSeoRenderPolicy).IsTrue();
        await Assert.That(result.RenderPolicy.LockTenantOperationalRenderPolicy).IsFalse();
        await Assert.That(result.RenderPolicy.LockTenantAdminRenderPolicy).IsFalse();
    }

    [Test]
    public async Task ApplyModuleSettingsAsync_DelegatesCapabilitySync_ToModuleCapabilityService()
    {
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        await _service.ApplyModuleSettingsAsync(tenantId, new ModuleSettingsDto
        {
            EnableIslamicModule = true,
            EnableTechModule = false
        }, actorId);

        await _moduleCapabilityService.Received(1).SyncTenantModuleCapabilitiesAsync(
            tenantId, true, false, actorId);
    }

    // ── Helpers ──────────────────────────────────────

    private void SetupEmptyBatchResolve()
    {
        _resolver.ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<SettingContext>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<ResolvedSetting>());
    }

    private static List<ResolvedSetting> CreateResolvedSettingsWithOverrideEnabled()
    {
        return
        [
            CreateResolvedSetting(GovernanceSettingKeys.Routing.RenderPolicy.AllowTenantOverride, "true")
        ];
    }

    private static ResolvedSetting CreateResolvedSetting(string key, string value, bool isLocked = false)
    {
        return new ResolvedSetting
        {
            Key = key,
            Value = value,
            IsLocked = isLocked,
            Source = SettingSource.SystemDefault
        };
    }

    private static InstanceGovernanceSettings CreateValidSettings()
    {
        return new InstanceGovernanceSettings
        {
            DeploymentMode = new DeploymentModeDto { Mode = DeploymentMode.SingleTenant },
            Modules = new ModuleSettingsDto
            {
                EnableIslamicModule = true,
                EnableTechModule = true
            },
            EventPolicy = new EventPolicyDto(),
            OrganizationPolicy = new OrganizationPolicyDto(),
            Branding = new BrandingSettingsDto(),
            Domains = new DomainSettingsDto(),
            TenantDelegation = new TenantDelegationSettingsDto
            {
                DefaultPublicHomePage = "EventList"
            },
            RenderPolicy = new RenderPolicySettingsDto
            {
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
                DisallowInteractiveServerOnOnboarding = false
            }
        };
    }
}
