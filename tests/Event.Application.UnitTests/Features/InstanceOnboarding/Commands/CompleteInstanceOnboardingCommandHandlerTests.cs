// ABOUTME: Unit tests for first-run instance onboarding completion defaults.
// ABOUTME: Verifies single-tenant convention settings are persisted without creating publisher scopes.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.DTOs.TenantSettings;
using Explore.Application.Features.InstanceOnboarding.Handlers.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Models.PublicExperience;
using Explore.Application.Onboarding;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Settings.Documents;
using Explore.Domain.Settings.Documents.Payloads;
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
    private readonly ITenantCreationService _tenantCreationService = Substitute.For<ITenantCreationService>();
    private readonly ITenantSettingsDocumentRepository _tenantSettingsDocumentRepository = Substitute.For<ITenantSettingsDocumentRepository>();
    private readonly ISystemSettingRepository _systemSettingRepository = Substitute.For<ISystemSettingRepository>();
    private readonly ISetupSecretProvider _setupSecretProvider = Substitute.For<ISetupSecretProvider>();
    private readonly IInstanceBootstrapAuditLogger _bootstrapAuditLogger = Substitute.For<IInstanceBootstrapAuditLogger>();
    private readonly IAdminCacheInvalidator _adminCacheInvalidator = Substitute.For<IAdminCacheInvalidator>();
    private readonly IDeploymentModeProvider _deploymentModeProvider = Substitute.For<IDeploymentModeProvider>();
    private readonly IJwtAuthorityRefreshNotifier _jwtAuthorityRefreshNotifier = Substitute.For<IJwtAuthorityRefreshNotifier>();
    private readonly ITenantBrandingSettingsDocumentProvisioningService _tenantBrandingProvisioningService = Substitute.For<ITenantBrandingSettingsDocumentProvisioningService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CompleteInstanceOnboardingCommandHandler _handler;
    private readonly List<SystemSetting> _capturedUpserts = [];

    public CompleteInstanceOnboardingCommandHandlerTests()
    {
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var op = callInfo.Arg<Func<CancellationToken, Task>>();
                return op!(CancellationToken.None);
            });

        _bootstrapRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(new InstanceBootstrapState
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
        _tenantCreationService
            .CreateInCurrentTransactionAsync(
                Arg.Any<TenantCreationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                TenantCreationRequest creationRequest = callInfo.ArgAt<TenantCreationRequest>(0);
                return new TenantCreationOutcome(
                    new Tenant
                    {
                        Id = creationRequest.TenantId,
                        FullName = creationRequest.FullName,
                        Slug = creationRequest.Slug,
                        TenantStatus = null!,
                        TenantStatusId = creationRequest.TenantStatusId
                    },
                    TenantBrandingSettingsDocumentDefaults.Create(
                        creationRequest.TenantId,
                        creationRequest.FullName),
                    TenantDirectoryOperatorIdentityDocumentDefaults.Create(
                        creationRequest.TenantId,
                        CreateDirectoryOperatorIdentity().ToPayload()));
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
        _systemSettingRepository
            .UpsertAsync(Arg.Any<SystemSetting>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                _capturedUpserts.Add(callInfo.ArgAt<SystemSetting>(0));
                return Task.FromResult<string?>(null);
            });
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
            _tenantCreationService,
            _tenantSettingsDocumentRepository,
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
        _capturedUpserts.Clear();

        var command = new CompleteInstanceOnboardingCommand
        {
            UserId = TestUserId,
            Settings = new CompleteInstanceOnboardingRequest
            {
                DirectoryOperatorIdentity = CreateDirectoryOperatorIdentity(),
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

        using var cancellationSource = new CancellationTokenSource();
        var result = await _handler.Handle(command, cancellationSource.Token);

        await Assert.That(result.IsSuccess).IsTrue();
        await _bootstrapRepository.Received(1).GetCurrent(cancellationSource.Token);
        await Assert.That(_capturedUpserts.Select(setting => setting.SettingKey)).IsEquivalentTo([
            GovernanceSettingKeys.Deployment.Mode,
            GovernanceSettingKeys.Branding.DisplayName,
            GovernanceSettingKeys.Email.FromAddress,
            GovernanceSettingKeys.Domains.InstanceBaseDomain,
            GovernanceSettingKeys.Localization.DefaultLanguage,
            GovernanceSettingKeys.PublicExperience.Mode,
            GovernanceSettingKeys.PublicExperience.EventCatalogLabel,
            GovernanceSettingKeys.Routing.DefaultPublicHomePage,
            GovernanceSettingKeys.PublicExperience.HomeBlocks,
            GovernanceSettingKeys.PublicExperience.Ctas]);
        await Assert.That(_capturedUpserts.Count).IsEqualTo(10);
        await Assert.That(_capturedUpserts.Single(setting => setting.SettingKey == GovernanceSettingKeys.Deployment.Mode).Value)
            .IsEqualTo(JsonSerializer.Serialize(DeploymentMode.SingleTenant.ToString()));
        await Assert.That(_capturedUpserts.Single(setting => setting.SettingKey == GovernanceSettingKeys.Branding.DisplayName).Value)
            .IsEqualTo(JsonSerializer.Serialize("Community Events"));
        await Assert.That(_capturedUpserts.Single(setting => setting.SettingKey == GovernanceSettingKeys.Email.FromAddress).Value)
            .IsEqualTo(JsonSerializer.Serialize("support@example.org"));
        await Assert.That(_capturedUpserts.Single(setting => setting.SettingKey == GovernanceSettingKeys.Domains.InstanceBaseDomain).Value)
            .IsEqualTo(JsonSerializer.Serialize("events.example.org"));
        await Assert.That(_capturedUpserts.Single(setting => setting.SettingKey == GovernanceSettingKeys.Localization.DefaultLanguage).Value)
            .IsEqualTo(JsonSerializer.Serialize("en"));
        await Assert.That(_capturedUpserts.Single(setting => setting.SettingKey == GovernanceSettingKeys.PublicExperience.Mode).Value)
            .IsEqualTo(JsonSerializer.Serialize("DiscoveryCentric"));
        await Assert.That(_capturedUpserts.Single(setting => setting.SettingKey == GovernanceSettingKeys.PublicExperience.EventCatalogLabel).Value)
            .IsEqualTo(JsonSerializer.Serialize("Events"));
        await Assert.That(_capturedUpserts.Single(setting => setting.SettingKey == GovernanceSettingKeys.Routing.DefaultPublicHomePage).Value)
            .IsEqualTo(JsonSerializer.Serialize("EventList"));
        await Assert.That(_capturedUpserts.Single(setting => setting.SettingKey == GovernanceSettingKeys.PublicExperience.HomeBlocks).ValueType)
            .IsEqualTo(SettingValueType.Json);
        await Assert.That(ContainsDefaultHomeBlock(
            _capturedUpserts.Single(setting => setting.SettingKey == GovernanceSettingKeys.PublicExperience.HomeBlocks).Value,
            "Community Events")).IsTrue();
        await Assert.That(_capturedUpserts.Single(setting => setting.SettingKey == GovernanceSettingKeys.PublicExperience.Ctas).ValueType)
            .IsEqualTo(SettingValueType.Json);
        await Assert.That(ContainsDefaultCta(
            _capturedUpserts.Single(setting => setting.SettingKey == GovernanceSettingKeys.PublicExperience.Ctas).Value)).IsTrue();
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
            && auditEvent.DeploymentMode == DeploymentMode.SingleTenant.ToString()
            && HasNoOnboardingPayloadShape(auditEvent)));
    }

    [Test]
    public async Task Handle_SingleTenantWithoutDirectoryIdentity_DoesNotCreateActiveTenant()
    {
        _tenantRepository.GetById(PlatformDefaults.DefaultTenantId).Returns((Tenant?)null);
        _tenantRepository.Create(Arg.Any<Tenant>())
            .Returns(call => call.Arg<Tenant>());
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

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo("tenant_directory_operator_identity_incomplete");
        await Assert.That(result.Errors).IsNull();
        await _tenantCreationService.DidNotReceiveWithAnyArgs()
            .CreateInCurrentTransactionAsync(default!, default);
        await _tenantRepository.DidNotReceiveWithAnyArgs().Create(default!);
    }

    [Test]
    public async Task Handle_SingleTenant_CreatesActiveDefaultTenantThroughAtomicBoundary()
    {
        _tenantRepository.GetById(PlatformDefaults.DefaultTenantId).Returns((Tenant?)null);
        TenantCreationRequest? capturedCreationRequest = null;
        _tenantCreationService
            .CreateInCurrentTransactionAsync(
                Arg.Do<TenantCreationRequest>(request => capturedCreationRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                TenantCreationRequest creationRequest = callInfo.ArgAt<TenantCreationRequest>(0);
                return new TenantCreationOutcome(
                    new Tenant
                    {
                        Id = creationRequest.TenantId,
                        FullName = creationRequest.FullName,
                        Slug = creationRequest.Slug,
                        TenantStatus = null!,
                        TenantStatusId = creationRequest.TenantStatusId
                    },
                    TenantBrandingSettingsDocumentDefaults.Create(
                        creationRequest.TenantId,
                        creationRequest.FullName),
                    TenantDirectoryOperatorIdentityDocumentDefaults.Create(
                        creationRequest.TenantId,
                        CreateDirectoryOperatorIdentity().ToPayload()));
            });
        var command = new CompleteInstanceOnboardingCommand
        {
            UserId = TestUserId,
            Settings = new CompleteInstanceOnboardingRequest
            {
                DirectoryOperatorIdentity = CreateDirectoryOperatorIdentity("Explicit Directory Operator"),
                SiteProfile = new SelfHostOnboardingProfileDto
                {
                    SiteName = "Instance Branding",
                    Locale = "en",
                    TimeZone = "UTC"
                }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(capturedCreationRequest).IsNotNull();
        await Assert.That(capturedCreationRequest!.TenantId).IsEqualTo(PlatformDefaults.DefaultTenantId);
        await Assert.That(capturedCreationRequest.TenantStatusId).IsEqualTo((int)TenantStatusEnum.Active);
        await Assert.That(capturedCreationRequest.ActorUserId).IsEqualTo(TestUserId);
        await Assert.That(capturedCreationRequest.Branding.DocumentId.Version).IsEqualTo(7);
        await Assert.That(capturedCreationRequest.DirectoryOperatorIdentity.DocumentId.Version).IsEqualTo(7);
        TenantDirectoryOperatorIdentitySettings? identity =
            JsonSerializer.Deserialize<TenantDirectoryOperatorIdentitySettings>(
                capturedCreationRequest.DirectoryOperatorIdentity.PayloadJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await Assert.That(identity).IsNotNull();
        await Assert.That(identity!.PublicName).IsEqualTo("Explicit Directory Operator");
        await Assert.That(identity.PublicName).IsNotEqualTo("Instance Branding");
        await _tenantRepository.DidNotReceiveWithAnyArgs().Create(default!);
    }

    [Test]
    public async Task Handle_SingleTenant_WithDetachedActor_CreatesTenantUserByForeignKeyOnly()
    {
        var actorId = Guid.NewGuid();
        _actorRepository.GetActorByUserId(TestUserId).Returns(new Actor
        {
            Id = actorId,
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = new ActorType
            {
                Id = (int)ActorTypeEnum.User,
                FullName = "User",
                MasterCode = "user"
            },
            Pii = new ActorPii { DisplayName = "Setup Admin" },
            UserId = TestUserId
        });
        var command = new CompleteInstanceOnboardingCommand
        {
            UserId = TestUserId,
            Settings = new CompleteInstanceOnboardingRequest
            {
                DirectoryOperatorIdentity = CreateDirectoryOperatorIdentity(),
                SiteProfile = new SelfHostOnboardingProfileDto
                {
                    SiteName = "Community Events",
                    Locale = "en",
                    TimeZone = "UTC"
                }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _tenantUserRepository.Received(1).Create(Arg.Is<TenantUser>(tenantUser =>
            tenantUser.ActorId == actorId && tenantUser.Actor == null));
    }

    [Test]
    public async Task Handle_SingleTenant_WhenSiteNameBlank_UsesTrimmedInstanceNameForDisplayName()
    {
        _capturedUpserts.Clear();

        var command = new CompleteInstanceOnboardingCommand
        {
            UserId = TestUserId,
            Settings = new CompleteInstanceOnboardingRequest
            {
                DirectoryOperatorIdentity = CreateDirectoryOperatorIdentity(),
                InstanceName = "  Trimmed Community Events  ",
                SiteProfile = new SelfHostOnboardingProfileDto
                {
                    SiteName = string.Empty,
                    SupportEmail = "support@example.org",
                    CanonicalUrl = "https://events.example.org/start",
                    Locale = "en",
                    TimeZone = "UTC"
                }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(_capturedUpserts.Single(setting => setting.SettingKey == GovernanceSettingKeys.Branding.DisplayName).Value)
            .IsEqualTo(JsonSerializer.Serialize("Trimmed Community Events"));
        await _tenantBrandingProvisioningService.Received(1).EnsureTenantBrandingDocumentAsync(
            PlatformDefaults.DefaultTenantId,
            "Trimmed Community Events",
            Arg.Any<CancellationToken>());
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

        await Assert.That(result.IsSuccess).IsTrue();
        await _platformUserRoleRepository.Received(1).Create(Arg.Is<PlatformUserRole>(role =>
            role.UserId == TestUserId
            && role.RoleId == (int)RoleEnum.Admin
            && role.GrantedBy == TestUserId));
        await _systemSettingRepository.Received(1).UpsertAsync(Arg.Is<SystemSetting>(setting =>
            setting.SettingKey == GovernanceSettingKeys.Deployment.Mode
            && setting.Value == JsonSerializer.Serialize(DeploymentMode.MultiTenant.ToString())), Arg.Any<CancellationToken>());

        _ = _tenantRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        _ = _tenantRepository.DidNotReceive().Create(Arg.Any<Tenant>());
        await _tenantCreationService.DidNotReceiveWithAnyArgs()
            .CreateInCurrentTransactionAsync(default!, default);
        _ = _tenantUserRepository.DidNotReceive().Create(Arg.Any<TenantUser>());
        _ = _tenantUserRoleGrantRepository.DidNotReceive().Create(Arg.Any<TenantUserRoleGrant>());
        _ = _tenantBrandingProvisioningService.DidNotReceive().EnsureTenantBrandingDocumentAsync(
            Arg.Any<Guid>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
        _ = _systemSettingRepository.DidNotReceive().UpsertAsync(
            Arg.Is<SystemSetting>(setting => setting.SettingKey == GovernanceSettingKeys.PublicExperience.Mode),
            Arg.Any<CancellationToken>());
        _setupSecretProvider.Received(1).Lock();
        _bootstrapAuditLogger.Received(1).Log(Arg.Is<InstanceBootstrapAuditEvent>(auditEvent =>
            auditEvent.EventType == InstanceBootstrapAuditEventType.SetupModeDisabled
            && auditEvent.Operation == "instance_onboarding_complete"
            && auditEvent.Outcome == "disabled"
            && auditEvent.ActorUserId == TestUserId
            && auditEvent.DeploymentMode == DeploymentMode.MultiTenant.ToString()));
    }

    [Test]
    public async Task Handle_WhenPostCommitRefreshFails_StillLocksSetupMode()
    {
        _jwtAuthorityRefreshNotifier.ReloadAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("refresh failed")));
        var command = new CompleteInstanceOnboardingCommand
        {
            UserId = TestUserId,
            Settings = new CompleteInstanceOnboardingRequest
            {
                DirectoryOperatorIdentity = CreateDirectoryOperatorIdentity(),
                SiteProfile = new SelfHostOnboardingProfileDto
                {
                    SiteName = "Community Events",
                    Locale = "en",
                    TimeZone = "UTC"
                }
            }
        };

        await Assert.That(async () => await _handler.Handle(command, CancellationToken.None))
            .Throws<InvalidOperationException>();

        _setupSecretProvider.Received(1).Lock();
    }

    [Test]
    public async Task Handle_MultiTenantDedicatedAdminHost_PersistsNormalizedAdminHost()
    {
        _deploymentModeProvider.GetConfiguredOnboardingModeAsync(Arg.Any<CancellationToken>()).Returns(DeploymentMode.MultiTenant);

        var command = new CompleteInstanceOnboardingCommand
        {
            UserId = TestUserId,
            Settings = new CompleteInstanceOnboardingRequest
            {
                DeploymentMode = DeploymentMode.MultiTenant,
                AdministrationAccessMode = CompleteInstanceOnboardingRequest.DedicatedAdminHostAdministrationAccess,
                AdminHost = "https://Admin.Example.Org/console",
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

        await Assert.That(result.IsSuccess).IsTrue();
        await _systemSettingRepository.Received(1).UpsertAsync(Arg.Is<SystemSetting>(setting =>
            setting.SettingKey == GovernanceSettingKeys.Domains.AdminHost
            && setting.Value == JsonSerializer.Serialize("admin.example.org")), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithBlankSiteProfileName_UsesTrimmedInstanceNameForBrandingFallback()
    {
        var command = new CompleteInstanceOnboardingCommand
        {
            UserId = TestUserId,
            Settings = new CompleteInstanceOnboardingRequest
            {
                DirectoryOperatorIdentity = CreateDirectoryOperatorIdentity(),
                InstanceName = "  Trimmed Instance Name  ",
                SiteProfile = new SelfHostOnboardingProfileDto
                {
                    SiteName = string.Empty,
                    SupportEmail = "support@example.org",
                    CanonicalUrl = "https://events.example.org/start",
                    Locale = "en",
                    TimeZone = "UTC"
                }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _systemSettingRepository.Received(1).UpsertAsync(Arg.Is<SystemSetting>(setting =>
            setting.SettingKey == GovernanceSettingKeys.Branding.DisplayName
            && setting.Value == JsonSerializer.Serialize("Trimmed Instance Name")), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_SingleTenantWithExistingDefaultTenant_UpdatesMandatoryIdentity()
    {
        Tenant existingTenant = new()
        {
            Id = PlatformDefaults.DefaultTenantId,
            FullName = PlatformDefaults.DefaultTenantName,
            Slug = PlatformDefaults.DefaultTenantSlug,
            TenantStatus = null!,
            TenantStatusId = (int)TenantStatusEnum.Active
        };
        TenantSettingsDocument existingIdentity = TenantDirectoryOperatorIdentityDocumentDefaults.Create(
            PlatformDefaults.DefaultTenantId,
            CreateDirectoryOperatorIdentity("Old Operator").ToPayload());
        _tenantRepository.GetById(PlatformDefaults.DefaultTenantId).Returns(existingTenant);
        _tenantSettingsDocumentRepository
            .GetTrackedByTenantAndDocumentKey(
                PlatformDefaults.DefaultTenantId,
                SettingsDocumentKeys.Tenant.DirectoryOperatorIdentity,
                Arg.Any<CancellationToken>())
            .Returns(existingIdentity);
        var command = new CompleteInstanceOnboardingCommand
        {
            UserId = TestUserId,
            Settings = new CompleteInstanceOnboardingRequest
            {
                DirectoryOperatorIdentity = CreateDirectoryOperatorIdentity("Existing Tenant Operator"),
                SiteProfile = new SelfHostOnboardingProfileDto
                {
                    SiteName = "Existing Tenant Brand",
                    Locale = "en",
                    TimeZone = "UTC"
                }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _tenantSettingsDocumentRepository.Received(1).Update(existingIdentity);
        TenantDirectoryOperatorIdentitySettings? payload =
            JsonSerializer.Deserialize<TenantDirectoryOperatorIdentitySettings>(
                existingIdentity.PayloadJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await Assert.That(payload!.PublicName).IsEqualTo("Existing Tenant Operator");
        await _tenantCreationService.DidNotReceiveWithAnyArgs()
            .CreateInCurrentTransactionAsync(default!, default);
    }

    private static TenantDirectoryOperatorIdentityInputDto CreateDirectoryOperatorIdentity(
        string publicName = "Community Events") => new()
    {
        PublicName = publicName,
        LegalName = "Community Events ASBL",
        OperatorKindCode = "registered_organization",
        JurisdictionCountryCode = "BE",
        RegistrationIdentifier = "BE 0123.456.789",
        PublicContactEmail = "legal@example.org",
        LegalNoticeUrl = "https://example.org/legal",
        TermsUrl = "https://example.org/terms",
        PrivacyUrl = "https://example.org/privacy"
    };

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

    private static bool HasNoOnboardingPayloadShape(InstanceBootstrapAuditEvent auditEvent)
    {
        var forbiddenPropertyNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Profile",
            "Payload",
            "Site",
            "SiteName",
            "Email",
            "Url",
            "Locale",
            "TimeZone",
            "Purpose"
        };

        var eventPropertyNames = typeof(InstanceBootstrapAuditEvent)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        return forbiddenPropertyNames.All(propertyName => !eventPropertyNames.Contains(propertyName))
            && string.IsNullOrWhiteSpace(auditEvent.RouteName)
            && string.IsNullOrWhiteSpace(auditEvent.TraceId)
            && string.IsNullOrWhiteSpace(auditEvent.FailureCode)
            && string.IsNullOrWhiteSpace(auditEvent.Provider)
            && string.IsNullOrWhiteSpace(auditEvent.Mode)
            && string.IsNullOrWhiteSpace(auditEvent.Realm)
            && string.IsNullOrWhiteSpace(auditEvent.ClientId);
    }
}
