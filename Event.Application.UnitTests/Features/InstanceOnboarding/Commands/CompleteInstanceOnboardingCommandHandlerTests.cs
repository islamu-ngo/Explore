// ABOUTME: Unit tests for first-run instance onboarding completion defaults.
// ABOUTME: Verifies single-tenant convention settings are persisted without creating publisher scopes.

using System.Text.Json;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Handlers.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Models.PublicExperience;
using Explore.Application.Onboarding;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.InstanceOnboarding.Commands;

public class CompleteInstanceOnboardingCommandHandlerTests
{
    private static readonly Guid TestUserId = Guid.NewGuid();

    private readonly IInstanceBootstrapStateRepository _bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
    private readonly IPlatformUserRoleRepository _platformUserRoleRepository = Substitute.For<IPlatformUserRoleRepository>();
    private readonly ITenantUserRoleGrantRepository _tenantUserRoleGrantRepository = Substitute.For<ITenantUserRoleGrantRepository>();
    private readonly ITenantUserRepository _tenantUserRepository = Substitute.For<ITenantUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IActorRepository _actorRepository = Substitute.For<IActorRepository>();
    private readonly IUserExternalLoginRepository _externalLoginRepository = Substitute.For<IUserExternalLoginRepository>();
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly ISystemSettingRepository _systemSettingRepository = Substitute.For<ISystemSettingRepository>();
    private readonly ISetupSecretProvider _setupSecretProvider = Substitute.For<ISetupSecretProvider>();
    private readonly IInstanceBootstrapAuditLogger _bootstrapAuditLogger = Substitute.For<IInstanceBootstrapAuditLogger>();
    private readonly IAdminCacheInvalidator _adminCacheInvalidator = Substitute.For<IAdminCacheInvalidator>();
    private readonly IDeploymentModeProvider _deploymentModeProvider = Substitute.For<IDeploymentModeProvider>();
    private readonly IJwtAuthorityRefreshNotifier _jwtAuthorityRefreshNotifier = Substitute.For<IJwtAuthorityRefreshNotifier>();
    private readonly ITenantBrandingSettingsDocumentProvisioningService _tenantBrandingProvisioningService = Substitute.For<ITenantBrandingSettingsDocumentProvisioningService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CompleteInstanceOnboardingCommandHandler _handler;

    public CompleteInstanceOnboardingCommandHandlerTests()
    {
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var op = callInfo.Arg<Func<CancellationToken, Task>>();
                return op!(CancellationToken.None);
            });

        _bootstrapRepository.GetCurrent().Returns(new InstanceBootstrapState
        {
            Id = Guid.NewGuid(),
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        });
        _deploymentModeProvider.GetConfiguredOnboardingModeAsync(Arg.Any<CancellationToken>()).Returns(DeploymentMode.SingleTenant);
        _userRepository.GetById(TestUserId).Returns(new User
        {
            Id = TestUserId,
            Pii = new UserPii
            {
                Email = "setup@example.org",
                FirstName = "Setup",
                LastName = "Admin"
            }
        });
        _tenantRepository.GetById(PlatformDefaults.DefaultTenantId).Returns(new Tenant
        {
            Id = PlatformDefaults.DefaultTenantId,
            FullName = PlatformDefaults.DefaultTenantName,
            Slug = PlatformDefaults.DefaultTenantSlug,
            TenantStatus = null!,
            TenantStatusId = (int)TenantStatusEnum.Active
        });
        _roleRepository.GetByMasterCodeAsync("platform.admin").Returns(new Role
        {
            Id = (int)RoleEnum.Admin,
            MasterCode = "platform.admin",
            FullName = "Platform Admin",
            Scope = RoleScopeEnum.Platform
        });
        _roleRepository.GetByMasterCodeAsync("tenant.admin").Returns(new Role
        {
            Id = (int)RoleEnum.TenantAdmin,
            MasterCode = "tenant.admin",
            FullName = "Tenant Admin",
            Scope = RoleScopeEnum.Tenant
        });
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);
        _systemSettingRepository.Create(Arg.Any<SystemSetting>()).Returns(callInfo => callInfo.Arg<SystemSetting>()!);
        _platformUserRoleRepository.Create(Arg.Any<PlatformUserRole>()).Returns(callInfo => callInfo.Arg<PlatformUserRole>()!);
        _tenantUserRoleGrantRepository.Create(Arg.Any<TenantUserRoleGrant>()).Returns(callInfo => callInfo.Arg<TenantUserRoleGrant>()!);
        _tenantUserRepository.Create(Arg.Any<TenantUser>()).Returns(callInfo =>
        {
            var tenantUser = callInfo.Arg<TenantUser>();
            tenantUser.Id = tenantUser.Id == Guid.Empty ? Guid.NewGuid() : tenantUser.Id;
            return tenantUser;
        });

        _handler = new CompleteInstanceOnboardingCommandHandler(
            _bootstrapRepository,
            _platformUserRoleRepository,
            _tenantUserRoleGrantRepository,
            _tenantUserRepository,
            _roleRepository,
            _userRepository,
            _actorRepository,
            _externalLoginRepository,
            _tenantRepository,
            _systemSettingRepository,
            _setupSecretProvider,
            _bootstrapAuditLogger,
            _adminCacheInvalidator,
            _deploymentModeProvider,
            _jwtAuthorityRefreshNotifier,
            _tenantBrandingProvisioningService,
            Substitute.For<ILogger<CompleteInstanceOnboardingCommandHandler>>(),
            _unitOfWork);
    }

    [Test]
    public async Task Handle_SingleTenant_PersistsSiteProfileAndConventionDefaults()
    {
        var command = new CompleteInstanceOnboardingCommand
        {
            UserId = TestUserId,
            Settings = new CompleteInstanceOnboardingRequest
            {
                SiteProfile = new SelfHostOnboardingProfileDto
                {
                    SiteName = "Community Events",
                    SupportEmail = "support@example.org",
                    CanonicalUrl = "https://events.example.org/start",
                    Locale = "en",
                    TimeZone = "UTC"
                }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _systemSettingRepository.Received(1).Create(Arg.Is<SystemSetting>(setting =>
            setting != null
            && setting.SettingKey == GovernanceSettingKeys.Branding.DisplayName
            && setting.Value == JsonSerializer.Serialize("Community Events")));
        await _systemSettingRepository.Received(1).Create(Arg.Is<SystemSetting>(setting =>
            setting != null
            && setting.SettingKey == GovernanceSettingKeys.Email.FromAddress
            && setting.Value == JsonSerializer.Serialize("support@example.org")));
        await _systemSettingRepository.Received(1).Create(Arg.Is<SystemSetting>(setting =>
            setting != null
            && setting.SettingKey == GovernanceSettingKeys.Domains.InstanceBaseDomain
            && setting.Value == JsonSerializer.Serialize("events.example.org")));
        await _systemSettingRepository.Received(1).Create(Arg.Is<SystemSetting>(setting =>
            setting != null
            && setting.SettingKey == GovernanceSettingKeys.PublicExperience.Mode
            && setting.Value == JsonSerializer.Serialize("DiscoveryCentric")));
        await _systemSettingRepository.Received(1).Create(Arg.Is<SystemSetting>(setting =>
            setting != null
            && setting.SettingKey == GovernanceSettingKeys.PublicExperience.EventCatalogLabel
            && setting.Value == JsonSerializer.Serialize("Events")));
        await _systemSettingRepository.Received(1).Create(Arg.Is<SystemSetting>(setting =>
            setting != null
            && setting.SettingKey == GovernanceSettingKeys.Routing.DefaultPublicHomePage
            && setting.Value == JsonSerializer.Serialize("EventList")));
        await _systemSettingRepository.Received(1).Create(Arg.Is<SystemSetting>(setting =>
            setting != null
            && setting.SettingKey == GovernanceSettingKeys.PublicExperience.HomeBlocks
            && setting.ValueType == SettingValueType.Json
            && ContainsDefaultHomeBlock(setting.Value, "Community Events")));
        await _systemSettingRepository.Received(1).Create(Arg.Is<SystemSetting>(setting =>
            setting != null
            && setting.SettingKey == GovernanceSettingKeys.PublicExperience.Ctas
            && setting.ValueType == SettingValueType.Json
            && ContainsDefaultCta(setting.Value)));
        await _tenantUserRepository.Received(1).Create(Arg.Is<TenantUser>(tenantUser =>
            tenantUser != null
            && tenantUser.TenantId == PlatformDefaults.DefaultTenantId
            && tenantUser.UserId == TestUserId
            && tenantUser.StatusId == (int)TenantUserStatusEnum.Active
            && tenantUser.CreatedBy == TestUserId));
        await _tenantUserRoleGrantRepository.Received(1).Create(Arg.Is<TenantUserRoleGrant>(grant =>
            grant != null
            && grant.TenantId == PlatformDefaults.DefaultTenantId
            && grant.RoleId == (int)RoleEnum.TenantAdmin
            && grant.RoleScopeId == (int)RoleScopeEnum.Tenant
            && grant.GrantedBy == TestUserId));
        await _tenantBrandingProvisioningService.Received(1).EnsureTenantBrandingDocumentAsync(
            PlatformDefaults.DefaultTenantId,
            "Community Events",
            Arg.Any<CancellationToken>());
        _setupSecretProvider.Received(1).Lock();
        _bootstrapAuditLogger.Received(1).Log(Arg.Is<InstanceBootstrapAuditEvent>(auditEvent =>
            auditEvent.EventType == InstanceBootstrapAuditEventType.SetupModeDisabled
            && auditEvent.Operation == "instance_onboarding_complete"
            && auditEvent.Outcome == "disabled"
            && auditEvent.ActorUserId == TestUserId
            && auditEvent.DeploymentMode == DeploymentMode.SingleTenant.ToString()));
    }

    [Test]
    public async Task Handle_MultiTenant_AssignsPlatformAdminWithoutDefaultTenantAdmin()
    {
        _deploymentModeProvider.GetConfiguredOnboardingModeAsync(Arg.Any<CancellationToken>()).Returns(DeploymentMode.MultiTenant);

        var command = new CompleteInstanceOnboardingCommand
        {
            UserId = TestUserId,
            Settings = new CompleteInstanceOnboardingRequest
            {
                DeploymentMode = DeploymentMode.SingleTenant,
                SiteProfile = new SelfHostOnboardingProfileDto
                {
                    SiteName = "Multi Tenant Events",
                    SupportEmail = "support@example.org",
                    CanonicalUrl = "https://events.example.org/start",
                    Locale = "en",
                    TimeZone = "UTC"
                }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _platformUserRoleRepository.Received(1).Create(Arg.Is<PlatformUserRole>(role =>
            role.UserId == TestUserId
            && role.RoleId == (int)RoleEnum.Admin
            && role.GrantedBy == TestUserId));
        await _systemSettingRepository.Received(1).Create(Arg.Is<SystemSetting>(setting =>
            setting.SettingKey == GovernanceSettingKeys.Deployment.Mode
            && setting.Value == JsonSerializer.Serialize(DeploymentMode.MultiTenant.ToString())));

        _ = _tenantRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        _ = _tenantRepository.DidNotReceive().Create(Arg.Any<Tenant>());
        _ = _tenantUserRepository.DidNotReceive().Create(Arg.Any<TenantUser>());
        _ = _tenantUserRoleGrantRepository.DidNotReceive().Create(Arg.Any<TenantUserRoleGrant>());
        _ = _tenantBrandingProvisioningService.DidNotReceive().EnsureTenantBrandingDocumentAsync(
            Arg.Any<Guid>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
        _ = _systemSettingRepository.DidNotReceive().Create(Arg.Is<SystemSetting>(setting =>
            setting.SettingKey == GovernanceSettingKeys.PublicExperience.Mode));
        _setupSecretProvider.Received(1).Lock();
        _bootstrapAuditLogger.Received(1).Log(Arg.Is<InstanceBootstrapAuditEvent>(auditEvent =>
            auditEvent.EventType == InstanceBootstrapAuditEventType.SetupModeDisabled
            && auditEvent.Operation == "instance_onboarding_complete"
            && auditEvent.Outcome == "disabled"
            && auditEvent.ActorUserId == TestUserId
            && auditEvent.DeploymentMode == DeploymentMode.MultiTenant.ToString()));
    }

    private static bool ContainsDefaultHomeBlock(string value, string expectedTitle)
    {
        var config = JsonSerializer.Deserialize<PublicExperienceHomeBlocksConfig>(value);
        var block = config?.Blocks.SingleOrDefault();

        return block is not null
            && block.Id == "hero"
            && block.Kind == PublicExperienceHomeBlockKind.Hero
            && block.Title == expectedTitle
            && block.LinkUrl == "/events";
    }

    private static bool ContainsDefaultCta(string value)
    {
        var config = JsonSerializer.Deserialize<PublicExperienceCtasConfig>(value);
        var cta = config?.Ctas.SingleOrDefault();

        return cta is not null
            && cta.Id == "browse-events"
            && cta.Url == "/events"
            && cta.Placement == PublicExperienceCtaPlacement.Hero
            && cta.Style == PublicExperienceCtaStyle.Primary;
    }
}
