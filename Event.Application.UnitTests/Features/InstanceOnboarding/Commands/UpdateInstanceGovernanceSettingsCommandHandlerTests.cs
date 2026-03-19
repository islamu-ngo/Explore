// ABOUTME: Unit tests for UpdateInstanceGovernanceSettingsCommandHandler render-policy validation and authorization behavior.
// ABOUTME: Verifies admin authorization, validation, and governance service delegation.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Instance;
using Explore.Application.Features.InstanceOnboarding.Handlers.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.InstanceOnboarding.Commands;

public class UpdateInstanceGovernanceSettingsCommandHandlerTests
{
    private static readonly Guid TestUserId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000111");

    private readonly IAdminContext _adminContext;
    private readonly IInstanceBootstrapStateRepository _bootstrapStateRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IInstanceGovernanceSettingService _governanceSettingService;
    private readonly ILogger<UpdateInstanceGovernanceSettingsCommandHandler> _logger;
    private readonly UpdateInstanceGovernanceSettingsCommandHandler _handler;

    public UpdateInstanceGovernanceSettingsCommandHandlerTests()
    {
        _adminContext = Substitute.For<IAdminContext>();
        _bootstrapStateRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        _tenantRepository = Substitute.For<ITenantRepository>();
        _governanceSettingService = Substitute.For<IInstanceGovernanceSettingService>();
        _logger = Substitute.For<ILogger<UpdateInstanceGovernanceSettingsCommandHandler>>();

        _handler = new UpdateInstanceGovernanceSettingsCommandHandler(
            _adminContext,
            _bootstrapStateRepository,
            _tenantRepository,
            _governanceSettingService,
            _logger);
    }

    [Test]
    public async Task Handle_WhenUserIsNotInstanceAdmin_ReturnsUnauthorized()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(CreateCommand(CreateValidSettings()), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("Only instance administrators");
        await _governanceSettingService.DidNotReceive().ApplySettingsAsync(Arg.Any<Guid?>(), Arg.Any<InstanceGovernanceSettings>(), Arg.Any<Guid?>());
    }

    [Test]
    public async Task Handle_WhenOnboardingUsesInteractiveServer_AcceptsAndDelegates()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);

        var existingBootstrapId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000112");
        _bootstrapStateRepository.GetCurrent().Returns(new InstanceBootstrapState
        {
            Id = existingBootstrapId,
            CreatedAt = DateTime.UtcNow
        });

        var tenant = CreateDefaultTenant();
        _tenantRepository.GetById(PlatformDefaults.DefaultTenantId).Returns(tenant);

        var settings = CreateValidSettings();
        settings.RenderPolicy.OnboardingRenderMode = "InteractiveServer";
        settings.RenderPolicy.DisallowInteractiveServerOnOnboarding = false;

        var result = await _handler.Handle(CreateCommand(settings), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _governanceSettingService.Received(1).ApplySettingsAsync(PlatformDefaults.DefaultTenantId, Arg.Any<InstanceGovernanceSettings>(), TestUserId);
    }

    [Test]
    public async Task Handle_WhenSettingsValid_AppliesGovernanceSettings()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);

        var existingBootstrapId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000112");
        _bootstrapStateRepository.GetCurrent().Returns(new InstanceBootstrapState
        {
            Id = existingBootstrapId,
            CreatedAt = DateTime.UtcNow
        });

        var tenant = CreateDefaultTenant();
        _tenantRepository.GetById(PlatformDefaults.DefaultTenantId).Returns(tenant);

        var settings = CreateValidSettings();

        var result = await _handler.Handle(CreateCommand(settings), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(existingBootstrapId);
        await _governanceSettingService.Received(1).ApplySettingsAsync(PlatformDefaults.DefaultTenantId, Arg.Any<InstanceGovernanceSettings>(), TestUserId);
    }

    private static UpdateInstanceGovernanceSettingsCommand CreateCommand(InstanceGovernanceSettings settings)
    {
        return new UpdateInstanceGovernanceSettingsCommand
        {
            UserId = TestUserId,
            Settings = settings
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

    private static Tenant CreateDefaultTenant()
    {
        return new Tenant
        {
            Id = PlatformDefaults.DefaultTenantId,
            FullName = PlatformDefaults.DefaultTenantName,
            Slug = PlatformDefaults.DefaultTenantSlug,
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = new TenantStatus
            {
                Id = (int)TenantStatusEnum.Active,
                MasterCode = "ACTIVE",
                FullName = "Active",
                IsActiveState = true
            }
        };
    }

}
