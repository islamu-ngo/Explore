// ABOUTME: Unit tests for UpdateInstanceGovernanceSettingsCommandHandler render-policy validation and authorization behavior.
// ABOUTME: Verifies admin authorization, validation, governance service delegation, and operator-controlled deployment mode locking.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Instance;
using Explore.Application.Features.InstanceOnboarding.Handlers.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Notifications;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Settings;
using MediatR;
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
    private readonly IDeploymentModeProvider _deploymentModeProvider;
    private readonly ILogger<UpdateInstanceGovernanceSettingsCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly UpdateInstanceGovernanceSettingsCommandHandler _handler;

    public UpdateInstanceGovernanceSettingsCommandHandlerTests()
    {
        _adminContext = Substitute.For<IAdminContext>();
        _bootstrapStateRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        _tenantRepository = Substitute.For<ITenantRepository>();
        _governanceSettingService = Substitute.For<IInstanceGovernanceSettingService>();
        _deploymentModeProvider = Substitute.For<IDeploymentModeProvider>();
        _logger = Substitute.For<ILogger<UpdateInstanceGovernanceSettingsCommandHandler>>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _mediator = Substitute.For<IMediator>();

        // Execute the lambda so inner repo logic runs in tests
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<Guid>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var op = callInfo.Arg<Func<CancellationToken, Task<Guid>>>();
                return op!(CancellationToken.None);
            });

        _handler = new UpdateInstanceGovernanceSettingsCommandHandler(
            _adminContext,
            _bootstrapStateRepository,
            _tenantRepository,
            _governanceSettingService,
            _deploymentModeProvider,
            _logger,
            _unitOfWork,
            _mediator);
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
        _bootstrapStateRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(new InstanceBootstrapState
        {
            Id = existingBootstrapId,
            CreatedAt = DateTime.UtcNow
        });

        var tenant = CreateDefaultTenant();
        _tenantRepository.GetById(PlatformDefaults.DefaultTenantId).Returns(tenant);

        var settings = CreateValidSettings();
        settings.RenderPolicy.OnboardingRenderMode = "InteractiveServer";
        settings.RenderPolicy.DisallowInteractiveServerOnOnboarding = false;

        using var cancellationSource = new CancellationTokenSource();
        var result = await _handler.Handle(CreateCommand(settings), cancellationSource.Token);

        await Assert.That(result.Success).IsTrue();
        await _governanceSettingService.Received(1).ApplySettingsAsync(PlatformDefaults.DefaultTenantId, Arg.Any<InstanceGovernanceSettings>(), TestUserId);
        await _bootstrapStateRepository.Received(1).GetCurrent(cancellationSource.Token);
    }

    [Test]
    public async Task Handle_WhenSettingsValid_AppliesGovernanceSettings()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);

        var existingBootstrapId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000112");
        _bootstrapStateRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(new InstanceBootstrapState
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

    [Test]
    public async Task Handle_WhenChangingDeploymentMode_ReturnsOperatorControlledFailure()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);
        _bootstrapStateRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(new InstanceBootstrapState
        {
            Id = Guid.NewGuid(),
            SelectedDeploymentMode = "MultiTenant",
            CreatedAt = DateTime.UtcNow
        });

        var result = await _handler.Handle(CreateCommand(CreateSettingsWithMode(DeploymentMode.SingleTenant)), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("DeploymentModeChangeRequiresOperatorConfiguration");
        await Assert.That(result.Message).IsEqualTo("Deployment mode is operator-controlled.");
        await _governanceSettingService.DidNotReceive().ApplySettingsAsync(Arg.Any<Guid?>(), Arg.Any<InstanceGovernanceSettings>(), Arg.Any<Guid?>());
    }

    [Test]
    public async Task Handle_WhenKeepingMultiTenantMode_SucceedsWithoutActiveTenantCountCheck()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);
        _bootstrapStateRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(new InstanceBootstrapState
        {
            Id = Guid.NewGuid(),
            SelectedDeploymentMode = "MultiTenant",
            CreatedAt = DateTime.UtcNow
        });

        var result = await _handler.Handle(CreateCommand(CreateSettingsWithMode(DeploymentMode.MultiTenant)), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.FailureCode).IsNull();
        await _tenantRepository.DidNotReceive().GetActiveTenantCountAsync();
    }

    [Test]
    public async Task Handle_WhenChangingFromSingleToMultiTenant_ReturnsOperatorControlledFailure()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);
        _bootstrapStateRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(new InstanceBootstrapState
        {
            Id = Guid.NewGuid(),
            SelectedDeploymentMode = "SingleTenant",
            CreatedAt = DateTime.UtcNow
        });

        var result = await _handler.Handle(CreateCommand(CreateSettingsWithMode(DeploymentMode.MultiTenant)), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("DeploymentModeChangeRequiresOperatorConfiguration");
        await _tenantRepository.DidNotReceive().GetActiveTenantCountAsync();
    }

    [Test]
    public async Task Handle_WhenBootstrapModeMissing_UsesSingleTenantConvention()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);
        _bootstrapStateRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(new InstanceBootstrapState
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        });
        _tenantRepository.GetById(PlatformDefaults.DefaultTenantId).Returns(CreateDefaultTenant());

        var result = await _handler.Handle(CreateCommand(CreateSettingsWithMode(DeploymentMode.SingleTenant)), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _governanceSettingService.Received(1).ApplySettingsAsync(
            PlatformDefaults.DefaultTenantId,
            Arg.Is<InstanceGovernanceSettings>(settings => settings != null && settings.DeploymentMode.Mode == DeploymentMode.SingleTenant),
            TestUserId);
    }

    [Test]
    public async Task Handle_WhenKeepingSingleTenantMode_DoesNotCheckActiveTenantCount()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);
        _bootstrapStateRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(new InstanceBootstrapState
        {
            Id = Guid.NewGuid(),
            SelectedDeploymentMode = "SingleTenant",
            CreatedAt = DateTime.UtcNow
        });
        _tenantRepository.GetById(PlatformDefaults.DefaultTenantId).Returns(CreateDefaultTenant());

        var result = await _handler.Handle(CreateCommand(CreateSettingsWithMode(DeploymentMode.SingleTenant)), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _tenantRepository.DidNotReceive().GetActiveTenantCountAsync();
    }

    [Test]
    public async Task Handle_WhenOuterTransactionRollsBack_DoesNotInvalidateLocationCaches()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);
        _bootstrapStateRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(new InstanceBootstrapState
        {
            Id = Guid.CreateVersion7(),
            SelectedDeploymentMode = "SingleTenant",
            CreatedAt = DateTime.UtcNow
        });
        _tenantRepository.GetById(PlatformDefaults.DefaultTenantId).Returns(CreateDefaultTenant());
        var locationPrivacyMutations = Substitute.For<ILocationPrivacyGovernanceMutationService>();
        _governanceSettingService.ApplySettingsAsync(
                Arg.Any<Guid?>(),
                Arg.Any<InstanceGovernanceSettings>(),
                Arg.Any<Guid?>())
            .Returns(new InstanceGovernanceSettingApplyResult(
                [AcceptedMutation(new LocationPrivacyProjectionIdentity(
                    Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7()))],
                CreateLocationNotifications()));
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<Guid>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => RollBackAfterOperationAsync(
                callInfo.Arg<Func<CancellationToken, Task<Guid>>>()));
        var handler = CreateHandler(locationPrivacyMutations);
        InstanceGovernanceSettings settings = CreateValidSettings();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(CreateCommand(settings), CancellationToken.None));

        await locationPrivacyMutations.DidNotReceive().InvalidateMutationAsync(
            Arg.Any<SettingScope>(),
            Arg.Any<Guid?>(),
            Arg.Any<IReadOnlyList<LocationPrivacyProjectionIdentity>>(),
            Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Publish(
            Arg.Any<SettingChangedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenOuterTransactionCommits_InvalidatesAllCorrectionTagsOnceAfterCommit()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);
        _bootstrapStateRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(new InstanceBootstrapState
        {
            Id = Guid.CreateVersion7(),
            SelectedDeploymentMode = "SingleTenant",
            CreatedAt = DateTime.UtcNow
        });
        _tenantRepository.GetById(PlatformDefaults.DefaultTenantId).Returns(CreateDefaultTenant());
        LocationPrivacyProjectionIdentity first = new(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());
        LocationPrivacyProjectionIdentity second = new(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());
        IReadOnlyList<LocationPrivacyGovernanceMutationResult> mutationResults =
        [
            AcceptedMutation(first),
            AcceptedMutation(first),
            AcceptedMutation(second),
            AcceptedMutation(),
            AcceptedMutation()
        ];
        _governanceSettingService.ApplySettingsAsync(
                Arg.Any<Guid?>(),
                Arg.Any<InstanceGovernanceSettings>(),
                Arg.Any<Guid?>())
            .Returns(new InstanceGovernanceSettingApplyResult(
                mutationResults,
                CreateLocationNotifications()));
        bool committed = false;
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<Guid>>>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task<Guid>>>();
                Guid result = await operation(CancellationToken.None);
                committed = true;
                return result;
            });
        bool invalidatedBeforeCommit = false;
        bool notificationPublishedBeforeCommit = false;
        _mediator.Publish(Arg.Any<SettingChangedNotification>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                notificationPublishedBeforeCommit |= !committed;
                return Task.CompletedTask;
            });
        var locationPrivacyMutations = Substitute.For<ILocationPrivacyGovernanceMutationService>();
        locationPrivacyMutations.InvalidateMutationAsync(
                Arg.Any<SettingScope>(),
                Arg.Any<Guid?>(),
                Arg.Any<IReadOnlyList<LocationPrivacyProjectionIdentity>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                invalidatedBeforeCommit = !committed;
                return Task.CompletedTask;
            });
        var handler = CreateHandler(locationPrivacyMutations);
        InstanceGovernanceSettings settings = CreateValidSettings();

        var result = await handler.Handle(CreateCommand(settings), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(invalidatedBeforeCommit).IsFalse();
        await Assert.That(notificationPublishedBeforeCommit).IsFalse();
        await _mediator.Received(5).Publish(
            Arg.Is<SettingChangedNotification>(notification =>
                CreateLocationNotifications().Select(item => item.Key).Contains(notification.Key)),
            Arg.Any<CancellationToken>());
        await locationPrivacyMutations.Received(1).InvalidateMutationAsync(
            SettingScope.Instance,
            null,
            Arg.Is<IReadOnlyList<LocationPrivacyProjectionIdentity>>(items =>
                items.Count == 2 && items.Contains(first) && items.Contains(second)),
            Arg.Any<CancellationToken>());
        await locationPrivacyMutations.DidNotReceive().InvalidateScopeAsync(
            Arg.Any<SettingScope>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    private static UpdateInstanceGovernanceSettingsCommand CreateCommand(InstanceGovernanceSettings settings)
    {
        return new UpdateInstanceGovernanceSettingsCommand
        {
            UserId = TestUserId,
            Settings = settings
        };
    }

    private UpdateInstanceGovernanceSettingsCommandHandler CreateHandler(
        ILocationPrivacyGovernanceMutationService locationPrivacyMutations) => new(
        _adminContext,
        _bootstrapStateRepository,
        _tenantRepository,
        _governanceSettingService,
        _deploymentModeProvider,
        _logger,
        _unitOfWork,
        _mediator,
        locationPrivacyMutations);

    private static SettingChangedNotification[] CreateLocationNotifications() =>
    [
        CreateNotification(GovernanceSettingKeys.LocationPrivacy.AllowHomeLocations),
        CreateNotification(GovernanceSettingKeys.LocationPrivacy.AllowPublicExactAddress),
        CreateNotification(GovernanceSettingKeys.LocationPrivacy.AllowPublicCoordinates),
        CreateNotification(GovernanceSettingKeys.LocationPrivacy.MinimumHomeAudience),
        CreateNotification(GovernanceSettingKeys.LocationPrivacy.DefaultRevealOffset)
    ];

    private static SettingChangedNotification CreateNotification(string key) => new(
        key,
        oldValue: null,
        newValue: "value",
        SettingSource.SystemDefault,
        tenantId: null,
        TestUserId,
        DateTime.UtcNow);

    private static LocationPrivacyGovernanceMutationResult AcceptedMutation(
        params LocationPrivacyProjectionIdentity[] corrected) =>
        new(true, null, null, corrected);

    private static async Task<Guid> RollBackAfterOperationAsync(
        Func<CancellationToken, Task<Guid>> operation)
    {
        await operation(CancellationToken.None);
        throw new InvalidOperationException("forced outer rollback");
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
            AiAssistant = new AiAssistantGovernanceSettingsDto(),
            Mcp = new McpGovernanceSettingsDto(),
            TenantDelegation = new TenantDelegationSettingsDto
            {
                DefaultPublicHomePage = "EventList"
            },
            AdminPortal = new AdminPortalSettingsDto(),
            LocationPrivacy = new LocationPrivacyGovernanceSettingsDto(),
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

    private static InstanceGovernanceSettings CreateSettingsWithMode(DeploymentMode mode)
    {
        return new InstanceGovernanceSettings
        {
            DeploymentMode = new DeploymentModeDto { Mode = mode },
            Modules = new ModuleSettingsDto
            {
                EnableIslamicModule = true,
                EnableTechModule = true
            },
            EventPolicy = new EventPolicyDto(),
            OrganizationPolicy = new OrganizationPolicyDto(),
            Branding = new BrandingSettingsDto(),
            Domains = new DomainSettingsDto(),
            AiAssistant = new AiAssistantGovernanceSettingsDto(),
            Mcp = new McpGovernanceSettingsDto(),
            TenantDelegation = new TenantDelegationSettingsDto
            {
                DefaultPublicHomePage = "EventList"
            },
            AdminPortal = new AdminPortalSettingsDto(),
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
