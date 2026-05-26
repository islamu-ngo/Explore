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
    private readonly ITenantMemberRepository _tenantMemberRepository = Substitute.For<ITenantMemberRepository>();
    private readonly ITenantUserRepository _tenantUserRepository = Substitute.For<ITenantUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IActorRepository _actorRepository = Substitute.For<IActorRepository>();
    private readonly IUserExternalLoginRepository _externalLoginRepository = Substitute.For<IUserExternalLoginRepository>();
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly ISystemSettingRepository _systemSettingRepository = Substitute.For<ISystemSettingRepository>();
    private readonly ISetupSecretProvider _setupSecretProvider = Substitute.For<ISetupSecretProvider>();
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
        _tenantMemberRepository.Create(Arg.Any<TenantMember>()).Returns(callInfo => callInfo.Arg<TenantMember>()!);
        _tenantUserRepository.Create(Arg.Any<TenantUser>()).Returns(callInfo => callInfo.Arg<TenantUser>()!);

        _handler = new CompleteInstanceOnboardingCommandHandler(
            _bootstrapRepository,
            _platformUserRoleRepository,
            _tenantMemberRepository,
            _tenantUserRepository,
            _roleRepository,
            _userRepository,
            _actorRepository,
            _externalLoginRepository,
            _tenantRepository,
            _systemSettingRepository,
            _setupSecretProvider,
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
        await _tenantMemberRepository.Received(1).Create(Arg.Is<TenantMember>(member =>
            member != null
            && member.TenantId == PlatformDefaults.DefaultTenantId
            && member.UserId == TestUserId
            && member.RoleId == (int)RoleEnum.TenantAdmin));
        await _tenantBrandingProvisioningService.Received(1).EnsureTenantBrandingDocumentAsync(
            PlatformDefaults.DefaultTenantId,
            "Community Events",
            Arg.Any<CancellationToken>());
        _setupSecretProvider.Received(1).Lock();
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
