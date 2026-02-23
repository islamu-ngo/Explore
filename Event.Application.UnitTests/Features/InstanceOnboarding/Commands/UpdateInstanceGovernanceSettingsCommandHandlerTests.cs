// ABOUTME: Unit tests for UpdateInstanceGovernanceSettingsCommandHandler render-policy validation and authorization behavior.
// ABOUTME: Ensures invalid onboarding render policy is rejected before persistence.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Handlers.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Core;

namespace Event.Application.UnitTests.Features.InstanceOnboarding.Commands;

public class UpdateInstanceGovernanceSettingsCommandHandlerTests
{
    private static readonly Guid TestUserId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000111");

    private readonly IAdminContext _adminContext;
    private readonly IInstanceBootstrapStateRepository _bootstrapStateRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantSettingsRepository _tenantSettingsRepository;
    private readonly IInstanceGovernanceSettingService _governanceSettingService;
    private readonly ILogger<UpdateInstanceGovernanceSettingsCommandHandler> _logger;
    private readonly UpdateInstanceGovernanceSettingsCommandHandler _handler;

    public UpdateInstanceGovernanceSettingsCommandHandlerTests()
    {
        _adminContext = Substitute.For<IAdminContext>();
        _bootstrapStateRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        _tenantRepository = Substitute.For<ITenantRepository>();
        _tenantSettingsRepository = Substitute.For<ITenantSettingsRepository>();
        _governanceSettingService = Substitute.For<IInstanceGovernanceSettingService>();
        _logger = Substitute.For<ILogger<UpdateInstanceGovernanceSettingsCommandHandler>>();

        _handler = new UpdateInstanceGovernanceSettingsCommandHandler(
            _adminContext,
            _bootstrapStateRepository,
            _tenantRepository,
            _tenantSettingsRepository,
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
        await _governanceSettingService.DidNotReceive().ApplySettingsAsync(Arg.Any<Guid>(), Arg.Any<InstanceGovernanceSettingsDto>(), Arg.Any<Guid?>());
    }

    [Test]
    public async Task Handle_WhenOnboardingUsesInteractiveServer_ReturnsValidationFailure()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);

        var settings = CreateValidSettings();
        settings.OnboardingRenderMode = "InteractiveServer";
        settings.DisallowInteractiveServerOnOnboarding = false;

        var result = await _handler.Handle(CreateCommand(settings), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Invalid instance governance settings.");
        await Assert.That(result.Errors).Contains("OnboardingRenderMode cannot be InteractiveServer.");
        await Assert.That(HasWarningLogContaining(
            _logger.ReceivedCalls(),
            "onboarding render-policy guardrail violation",
            "InteractiveServer",
            "DisallowInteractiveServerOnOnboarding")).IsTrue();
        await _governanceSettingService.DidNotReceive().ApplySettingsAsync(Arg.Any<Guid>(), Arg.Any<InstanceGovernanceSettingsDto>(), Arg.Any<Guid?>());
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
        _tenantSettingsRepository.GetByTenant(PlatformDefaults.DefaultTenantId).Returns(new TenantSettings
        {
            TenantId = PlatformDefaults.DefaultTenantId,
            Tenant = tenant
        });

        var settings = CreateValidSettings();

        var result = await _handler.Handle(CreateCommand(settings), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(existingBootstrapId);
        await _governanceSettingService.Received(1).ApplySettingsAsync(PlatformDefaults.DefaultTenantId, Arg.Any<InstanceGovernanceSettingsDto>(), TestUserId);
    }

    private static UpdateInstanceGovernanceSettingsCommand CreateCommand(InstanceGovernanceSettingsDto settings)
    {
        return new UpdateInstanceGovernanceSettingsCommand
        {
            UserId = TestUserId,
            Settings = settings
        };
    }

    private static InstanceGovernanceSettingsDto CreateValidSettings()
    {
        return new InstanceGovernanceSettingsDto
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
            OnboardingRenderMode = "InteractiveAuto",
            OnboardingPrerenderEnabled = false,
            DisallowInteractiveServerOnOnboarding = true,
            DefaultPublicHomePage = "EventList",
            EnableIslamicModule = true,
            EnableTechModule = true
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

    private static bool HasWarningLogContaining(IEnumerable<ICall> calls, params string[] fragments)
    {
        return calls.Any(call =>
        {
            if (!string.Equals(call.GetMethodInfo().Name, nameof(ILogger.Log), StringComparison.Ordinal))
            {
                return false;
            }

            var args = call.GetArguments();
            if (args.Length < 3 || args[0] is not LogLevel logLevel || logLevel != LogLevel.Warning)
            {
                return false;
            }

            var message = args[2]?.ToString() ?? string.Empty;
            return fragments.All(fragment => message.Contains(fragment, StringComparison.OrdinalIgnoreCase));
        });
    }
}
