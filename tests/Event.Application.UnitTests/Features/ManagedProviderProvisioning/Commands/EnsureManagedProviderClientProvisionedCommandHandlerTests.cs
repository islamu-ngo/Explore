// ABOUTME: Unit tests for managed provider provisioning command semantics and authority boundaries.
// ABOUTME: Verifies tenant-scoped admin grants, tenant-scoped actors, and optional organizer creation.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.ManagedProviderProvisioning;
using Explore.Application.DTOs.Management;
using Explore.Application.Features.ControlPlane.Plans;
using Explore.Application.Features.ManagedProviderProvisioning.Handlers.Commands;
using Explore.Application.Features.ManagedProviderProvisioning.Requests.Commands;
using Explore.Application.Features.Management;
using Explore.Application.Management;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Modules;
using Explore.Domain.Settings.Documents;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.ManagedProviderProvisioning.Commands;

public class EnsureManagedProviderClientProvisionedCommandHandlerTests
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IUserExternalLoginRepository _userExternalLoginRepository;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly ITenantUserProfileRepository _tenantUserProfileRepository;
    private readonly ITenantUserRoleGrantRepository _tenantUserRoleGrantRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IExternalBindingRepository _externalBindingRepository;
    private readonly IInstanceBootstrapStateRepository _instanceBootstrapStateRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EnsureManagedProviderClientProvisionedCommandHandler _handler;

    public EnsureManagedProviderClientProvisionedCommandHandlerTests()
    {
        _tenantRepository = Substitute.For<ITenantRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _actorRepository = Substitute.For<IActorRepository>();
        _userExternalLoginRepository = Substitute.For<IUserExternalLoginRepository>();
        _tenantUserRepository = Substitute.For<ITenantUserRepository>();
        _tenantUserProfileRepository = Substitute.For<ITenantUserProfileRepository>();
        _tenantUserRoleGrantRepository = Substitute.For<ITenantUserRoleGrantRepository>();
        _roleRepository = Substitute.For<IRoleRepository>();
        _organizationRepository = Substitute.For<IOrganizationRepository>();
        _organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        _groupRepository = Substitute.For<IGroupRepository>();
        _groupMemberRepository = Substitute.For<IGroupMemberRepository>();
        _externalBindingRepository = Substitute.For<IExternalBindingRepository>();
        _instanceBootstrapStateRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<ManagedProviderClientProvisioningResultDto>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ManagedProviderClientProvisioningResultDto>>>()(CancellationToken.None));

        _tenantRepository.Create(Arg.Any<Tenant>()).Returns(call => call.Arg<Tenant>());
        _userRepository.Create(Arg.Any<User>()).Returns(call => call.Arg<User>());
        _userRepository.Update(Arg.Any<User>()).Returns(Task.CompletedTask);
        _actorRepository.Create(Arg.Any<Actor>()).Returns(call => call.Arg<Actor>());
        _tenantUserRepository.Create(Arg.Any<TenantUser>()).Returns(call => call.Arg<TenantUser>());
        _tenantUserProfileRepository.Create(Arg.Any<TenantUserProfile>()).Returns(call => call.Arg<TenantUserProfile>());
        _userExternalLoginRepository.Create(Arg.Any<UserExternalLogin>()).Returns(call => call.Arg<UserExternalLogin>());
        _tenantUserRoleGrantRepository.Create(Arg.Any<TenantUserRoleGrant>()).Returns(call => call.Arg<TenantUserRoleGrant>());
        _organizationRepository.Create(Arg.Any<Organization>()).Returns(call => call.Arg<Organization>());
        _organizationRepository.Update(Arg.Any<Organization>()).Returns(Task.CompletedTask);
        _organizationMemberRepository.Create(Arg.Any<OrganizationMember>()).Returns(call => call.Arg<OrganizationMember>());
        _groupRepository.Create(Arg.Any<Group>()).Returns(call => call.Arg<Group>());
        _groupRepository.Update(Arg.Any<Group>()).Returns(Task.CompletedTask);
        _groupMemberRepository.Create(Arg.Any<GroupMember>()).Returns(call => call.Arg<GroupMember>());
        _externalBindingRepository.Create(Arg.Any<ExternalBinding>()).Returns(call => call.Arg<ExternalBinding>());
        _externalBindingRepository.Update(Arg.Any<ExternalBinding>()).Returns(Task.CompletedTask);
        _userRepository.GetUsersByNormalizedEmailAsync(
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<User>());
        _roleRepository.GetByMasterCodeAsync("tenant.admin").Returns(new Role
        {
            Id = (int)RoleEnum.TenantAdmin,
            MasterCode = "tenant.admin",
            FullName = "Tenant Administrator",
            Scope = RoleScopeEnum.Tenant
        });

        var tenantPlanRepository = Substitute.For<ITenantPlanRepository>();
        var managedOptions = Microsoft.Extensions.Options.Options.Create(new ManagedControlPlaneOptions());
        var tenantSettingRepository = Substitute.For<ITenantSettingRepository>();
        var systemSettingRepository = Substitute.For<ISystemSettingRepository>();
        var managedOperationRepository = Substitute.For<IManagedTenantProvisioningOperationRepository>();
        var managedTenantProvisioningPreflight = new ManagedTenantProvisioningPreflight(
            _tenantRepository,
            tenantPlanRepository,
            Substitute.For<IModuleDefinitionRepository>(),
            tenantSettingRepository,
            systemSettingRepository,
            Substitute.For<ITenantBrandingSettingsDocumentLockService>(),
            new TenantPlanStorageQuotaCeilingPolicy(systemSettingRepository),
            managedOptions);

        _handler = new EnsureManagedProviderClientProvisionedCommandHandler(
            _tenantRepository,
            _userRepository,
            _actorRepository,
            _userExternalLoginRepository,
            _tenantUserRepository,
            _tenantUserProfileRepository,
            _tenantUserRoleGrantRepository,
            _roleRepository,
            _organizationRepository,
            _organizationMemberRepository,
            _groupRepository,
            _groupMemberRepository,
            _externalBindingRepository,
            Substitute.For<ITenantOnboardingStateRepository>(),
            managedOperationRepository,
            tenantPlanRepository,
            Substitute.For<ITenantCapabilityRepository>(),
            tenantSettingRepository,
            Substitute.For<ITenantSettingsDocumentRepository>(),
            Substitute.For<IEmailDispatchOutboxRepository>(),
            Substitute.For<IAuditLogRepository>(),
            Substitute.For<ITenantBrandingSettingsDocumentProvisioningService>(),
            Substitute.For<ITypedSettingsDocumentResolver>(),
            Substitute.For<IHierarchicalSettingsResolver>(),
            managedTenantProvisioningPreflight,
            new TenantActivationCapacityPolicy(
                _instanceBootstrapStateRepository,
                _tenantRepository,
                managedOperationRepository,
                managedOptions),
            managedOptions,
            ImmediateSettingMutationLock.Instance,
            _unitOfWork,
            Substitute.For<ILogger<EnsureManagedProviderClientProvisionedCommandHandler>>());
    }

    [Test]
    public async Task Handle_WithValidRequest_CreatesTenantUserActorExternalLoginAndTenantAdminMembership()
    {
        var request = new EnsureManagedProviderClientProvisionedCommand { ProvisioningDto = CreateValidDto() };
        _tenantRepository.GetTenantBySlug("erp-customer").Returns((Tenant?)null);
        _userExternalLoginRepository.GetByProviderAndKey("keycloak", "external-admin-1").Returns((UserExternalLogin?)null);
        _actorRepository.GetActorByUserIdAndTenantId(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns((Actor?)null);
        _tenantUserRepository.GetByTenantAndUserAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((TenantUser?)null);
        _tenantUserProfileRepository.GetByTenantUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((TenantUserProfile?)null);
        _tenantUserRoleGrantRepository.GetByTenantAndUser(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns((TenantUserRoleGrant?)null);

        var result = await _handler.Handle(request, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotNull();
        await _tenantRepository.Received(1).Create(Arg.Is<Tenant>(tenant =>
            tenant.FullName == "ERP Customer" &&
            tenant.Slug == "erp-customer" &&
            tenant.TenantStatusId == (int)TenantStatusEnum.Active));
        await _actorRepository.Received(1).Create(Arg.Is<Actor>(actor =>
            actor.ActorTypeId == (int)ActorTypeEnum.User &&
            actor.TenantId == result.Id!.TenantId &&
            actor.UserId == result.Id.UserId));
        await _userExternalLoginRepository.Received(1).Create(Arg.Is<UserExternalLogin>(login =>
            login.TenantId == result.Id!.TenantId &&
            login.UserId == result.Id.UserId &&
            login.Provider == "keycloak" &&
            login.ProviderKey == "external-admin-1"));
        await _tenantUserRepository.Received(1).Create(Arg.Is<TenantUser>(tenantUser =>
            tenantUser.TenantId == result.Id!.TenantId &&
            tenantUser.UserId == result.Id.UserId &&
            tenantUser.ActorId == result.Id.UserActorId &&
            tenantUser.StatusId == (int)TenantUserStatusEnum.Active));
        await _tenantUserProfileRepository.Received(1).Create(Arg.Is<TenantUserProfile>(profile =>
            profile.TenantId == result.Id!.TenantId &&
            profile.TenantUserId == result.Id.TenantUserId &&
            profile.DisplayNameOverride == "Amina Admin" &&
            profile.ContactEmailOverride == "admin@example.com"));
        await _tenantUserRoleGrantRepository.Received(1).Create(Arg.Is<TenantUserRoleGrant>(grant =>
            grant.TenantId == result.Id!.TenantId &&
            grant.TenantUserId == result.Id.TenantUserId &&
            grant.RoleId == (int)RoleEnum.TenantAdmin &&
            grant.RoleScopeId == (int)RoleScopeEnum.Tenant));
        await _externalBindingRepository.Received().Create(Arg.Is<ExternalBinding>(binding =>
            binding.ExternalType == ExternalBindingTypes.External.ProviderCustomer &&
            binding.InternalType == ExternalBindingTypes.Internal.Tenant &&
            binding.InternalId == result.Id!.TenantId &&
            binding.ScopeTenantId == null));
        await _externalBindingRepository.Received().Create(Arg.Is<ExternalBinding>(binding =>
            binding.ExternalType == ExternalBindingTypes.External.ExternalAdminUser &&
            binding.InternalType == ExternalBindingTypes.Internal.User &&
            binding.InternalId == result.Id!.UserId &&
            binding.ScopeTenantId == result.Id.TenantId));
        await _externalBindingRepository.Received().Create(Arg.Is<ExternalBinding>(binding =>
            binding.ExternalType == ExternalBindingTypes.External.ExternalAdminTenantUser &&
            binding.InternalType == ExternalBindingTypes.Internal.TenantUser &&
            binding.InternalId == result.Id!.TenantUserId &&
            binding.ScopeTenantId == result.Id.TenantId));
    }

    [Test]
    public async Task Handle_WithOrganizationOrganizer_CreatesApprovedOrganizationActorAndOrgAdminMembershipInsideTenant()
    {
        var request = new EnsureManagedProviderClientProvisionedCommand
        {
            ProvisioningDto = CreateValidDto(new ManagedProviderOrganizerDto
            {
                Kind = ManagedProviderOrganizerKindDto.Organization,
                FullName = "Customer Legal Entity",
                Email = "org@example.com"
            })
        };
        _tenantRepository.GetTenantBySlug("erp-customer").Returns((Tenant?)null);
        _userExternalLoginRepository.GetByProviderAndKey("keycloak", "external-admin-1").Returns((UserExternalLogin?)null);
        _actorRepository.GetActorByUserIdAndTenantId(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns((Actor?)null);
        _tenantUserRoleGrantRepository.GetByTenantAndUser(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns((TenantUserRoleGrant?)null);
        _organizationMemberRepository.GetByOrganizationAndUser(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns((OrganizationMember?)null);

        var result = await _handler.Handle(request, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id!.OrganizerKind).IsEqualTo(ManagedProviderOrganizerKindDto.Organization);
        await _organizationRepository.Received(1).Create(Arg.Is<Organization>(organization =>
            organization.TenantId == result.Id.TenantId &&
            organization.FullName == "Customer Legal Entity" &&
            organization.ApprovalStatusId == (int)ApprovalStatusEnum.Approved));
        await _actorRepository.Received(1).Create(Arg.Is<Actor>(actor =>
            actor.ActorTypeId == (int)ActorTypeEnum.Organization &&
            actor.TenantId == result.Id.TenantId &&
            actor.OrganizationId == result.Id.OrganizerId));
        await _organizationMemberRepository.Received(1).Create(Arg.Is<OrganizationMember>(member =>
            member.TenantId == result.Id.TenantId &&
            member.UserId == result.Id.UserId &&
            member.RoleId == (int)RoleEnum.OrgAdmin));
    }

    [Test]
    public async Task Handle_WithGroupOrganizer_CreatesApprovedGroupActorAndGroupAdminMembershipInsideTenant()
    {
        var request = new EnsureManagedProviderClientProvisionedCommand
        {
            ProvisioningDto = CreateValidDto(new ManagedProviderOrganizerDto
            {
                Kind = ManagedProviderOrganizerKindDto.Group,
                FullName = "Customer Community Group",
                Description = "Informal organizer"
            })
        };
        _tenantRepository.GetTenantBySlug("erp-customer").Returns((Tenant?)null);
        _userExternalLoginRepository.GetByProviderAndKey("keycloak", "external-admin-1").Returns((UserExternalLogin?)null);
        _actorRepository.GetActorByUserIdAndTenantId(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns((Actor?)null);
        _tenantUserRoleGrantRepository.GetByTenantAndUser(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns((TenantUserRoleGrant?)null);
        _groupMemberRepository.GetByGroupAndUser(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns((GroupMember?)null);

        var result = await _handler.Handle(request, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id!.OrganizerKind).IsEqualTo(ManagedProviderOrganizerKindDto.Group);
        await _groupRepository.Received(1).Create(Arg.Is<Group>(group =>
            group.TenantId == result.Id.TenantId &&
            group.FullName == "Customer Community Group" &&
            group.ApprovalStatusId == (int)ApprovalStatusEnum.Approved));
        await _actorRepository.Received(1).Create(Arg.Is<Actor>(actor =>
            actor.ActorTypeId == (int)ActorTypeEnum.Group &&
            actor.TenantId == result.Id.TenantId &&
            actor.GroupId == result.Id.OrganizerId));
        await _groupMemberRepository.Received(1).Create(Arg.Is<GroupMember>(member =>
            member.TenantId == result.Id.TenantId &&
            member.UserId == result.Id.UserId &&
            member.RoleId == (int)RoleEnum.GroupAdmin));
    }

    [Test]
    public async Task Handle_WhenTenantSlugExists_ReturnsFailureBeforeCreatingRecords()
    {
        var request = new EnsureManagedProviderClientProvisionedCommand { ProvisioningDto = CreateValidDto() };
        _tenantRepository.GetTenantBySlug("erp-customer").Returns(new Tenant
        {
            FullName = "Existing",
            Slug = "erp-customer",
            TenantStatus = null!
        });

        var result = await _handler.Handle(request, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<ManagedProviderClientProvisioningResultDto>>>(),
            Arg.Any<CancellationToken>());
        await _tenantUserRoleGrantRepository.DidNotReceive().Create(Arg.Any<TenantUserRoleGrant>());
    }

    [Test]
    public async Task Handle_WhenVerifiedIdentityMatchesNormalizedEmail_ReusesExistingUser()
    {
        Guid existingUserId = Guid.CreateVersion7();
        var existingUser = new User
        {
            Id = existingUserId,
            Pii = new UserPii
            {
                Email = "admin@example.com",
                FirstName = "Existing",
                LastName = "Administrator"
            },
            EmailVerified = true
        };
        _tenantRepository.GetTenantBySlug("erp-customer").Returns((Tenant?)null);
        _userExternalLoginRepository.GetByProviderAndKey("keycloak", "external-admin-1")
            .Returns((UserExternalLogin?)null);
        _userRepository.GetUsersByNormalizedEmailAsync(
                "admin@example.com",
                Arg.Any<CancellationToken>())
            .Returns([existingUser]);

        BaseCommandResponse<ManagedProviderClientProvisioningResultDto> result = await _handler.Handle(
            new EnsureManagedProviderClientProvisionedCommand { ProvisioningDto = CreateValidDto() },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id!.UserId).IsEqualTo(existingUserId);
        await _userRepository.DidNotReceive().Create(Arg.Any<User>());
    }

    [Test]
    public async Task Handle_WhenUnverifiedIdentityMatchesEmail_RejectsUnsafeMergeBeforeMutation()
    {
        _userExternalLoginRepository.GetByProviderAndKey("keycloak", "external-admin-1")
            .Returns((UserExternalLogin?)null);
        _userRepository.GetUsersByNormalizedEmailAsync(
                "admin@example.com",
                Arg.Any<CancellationToken>())
            .Returns([
                new User
                {
                    Id = Guid.CreateVersion7(),
                    Pii = new UserPii
                    {
                        Email = "ADMIN@example.com",
                        FirstName = "Unverified",
                        LastName = "Account"
                    },
                    EmailVerified = false
                }
            ]);

        BaseCommandResponse<ManagedProviderClientProvisioningResultDto> result = await _handler.Handle(
            new EnsureManagedProviderClientProvisionedCommand
            {
                ProvisioningDto = CreateValidDto(emailVerified: false)
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("tenant_administrator_email_match_denied");
        await _tenantRepository.DidNotReceive().Create(Arg.Any<Tenant>());
    }

    [Test]
    public async Task Handle_WhenProviderCustomerBindingExists_RehydratesResultWithoutCreatingRecords()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        var tenantUserProfileId = Guid.NewGuid();
        var userActorId = Guid.NewGuid();
        var loginId = Guid.NewGuid();
        var tenantUserRoleGrantId = Guid.NewGuid();
        var request = new EnsureManagedProviderClientProvisionedCommand { ProvisioningDto = CreateValidDto() };

        _externalBindingRepository
            .GetByExternalKeyAsync("erp-provider", "erp", ExternalBindingTypes.External.ProviderCustomer, "customer-123", null, Arg.Any<CancellationToken>())
            .Returns(new ExternalBinding
            {
                ProviderKey = "erp-provider",
                ExternalSystem = "erp",
                ExternalType = ExternalBindingTypes.External.ProviderCustomer,
                ExternalId = "customer-123",
                InternalType = ExternalBindingTypes.Internal.Tenant,
                InternalId = tenantId
            });
        _tenantRepository.GetById(tenantId).Returns(new Tenant
        {
            Id = tenantId,
            FullName = "ERP Customer",
            Slug = "erp-customer",
            TenantStatus = null!
        });
        _externalBindingRepository
            .GetByExternalKeyAsync("erp-provider", "keycloak", ExternalBindingTypes.External.ExternalAdminUser, "external-admin-1", tenantId, Arg.Any<CancellationToken>())
            .Returns(new ExternalBinding
            {
                ProviderKey = "erp-provider",
                ExternalSystem = "keycloak",
                ExternalType = ExternalBindingTypes.External.ExternalAdminUser,
                ExternalId = "external-admin-1",
                InternalType = ExternalBindingTypes.Internal.User,
                InternalId = userId,
                ScopeTenantId = tenantId
            });
        _externalBindingRepository
            .GetByExternalKeyAsync("erp-provider", "keycloak", ExternalBindingTypes.External.ExternalAdminUserActor, "external-admin-1", tenantId, Arg.Any<CancellationToken>())
            .Returns(new ExternalBinding
            {
                ProviderKey = "erp-provider",
                ExternalSystem = "keycloak",
                ExternalType = ExternalBindingTypes.External.ExternalAdminUserActor,
                ExternalId = "external-admin-1",
                InternalType = ExternalBindingTypes.Internal.Actor,
                InternalId = userActorId,
                ScopeTenantId = tenantId
            });
        _externalBindingRepository
            .GetByExternalKeyAsync("erp-provider", "keycloak", ExternalBindingTypes.External.ExternalAdminTenantUser, "external-admin-1", tenantId, Arg.Any<CancellationToken>())
            .Returns(new ExternalBinding
            {
                ProviderKey = "erp-provider",
                ExternalSystem = "keycloak",
                ExternalType = ExternalBindingTypes.External.ExternalAdminTenantUser,
                ExternalId = "external-admin-1",
                InternalType = ExternalBindingTypes.Internal.TenantUser,
                InternalId = tenantUserId,
                ScopeTenantId = tenantId
            });
        _externalBindingRepository
            .GetByExternalKeyAsync("erp-provider", "keycloak", ExternalBindingTypes.External.ExternalAdminTenantUserProfile, "external-admin-1", tenantId, Arg.Any<CancellationToken>())
            .Returns(new ExternalBinding
            {
                ProviderKey = "erp-provider",
                ExternalSystem = "keycloak",
                ExternalType = ExternalBindingTypes.External.ExternalAdminTenantUserProfile,
                ExternalId = "external-admin-1",
                InternalType = ExternalBindingTypes.Internal.TenantUserProfile,
                InternalId = tenantUserProfileId,
                ScopeTenantId = tenantId
            });
        _externalBindingRepository
            .GetByExternalKeyAsync("erp-provider", "keycloak", ExternalBindingTypes.External.ExternalAdminUserLogin, "external-admin-1", tenantId, Arg.Any<CancellationToken>())
            .Returns(new ExternalBinding
            {
                ProviderKey = "erp-provider",
                ExternalSystem = "keycloak",
                ExternalType = ExternalBindingTypes.External.ExternalAdminUserLogin,
                ExternalId = "external-admin-1",
                InternalType = ExternalBindingTypes.Internal.UserExternalLogin,
                InternalId = loginId,
                ScopeTenantId = tenantId
            });
        _userRepository.GetById(userId).Returns(new User
        {
            Id = userId,
            Pii = new UserPii
            {
                Email = "admin@example.com",
                FirstName = "Amina",
                LastName = "Admin"
            }
        });
        _tenantUserRoleGrantRepository.GetByTenantAndUser(tenantId, userId).Returns(new TenantUserRoleGrant
        {
            Id = tenantUserRoleGrantId,
            TenantId = tenantId,
            TenantUserId = tenantUserId,
            RoleId = (int)RoleEnum.TenantAdmin,
            RoleScopeId = (int)RoleScopeEnum.Tenant,
            Tenant = null!,
            TenantUser = null!,
            Role = null!
        });

        var result = await _handler.Handle(request, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id!.TenantId).IsEqualTo(tenantId);
        await Assert.That(result.Id.UserId).IsEqualTo(userId);
        await Assert.That(result.Id.TenantUserId).IsEqualTo(tenantUserId);
        await Assert.That(result.Id.TenantUserProfileId).IsEqualTo(tenantUserProfileId);
        await Assert.That(result.Id.UserActorId).IsEqualTo(userActorId);
        await Assert.That(result.Id.UserExternalLoginId).IsEqualTo(loginId);
        await Assert.That(result.Id.TenantUserRoleGrantId).IsEqualTo(tenantUserRoleGrantId);
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<ManagedProviderClientProvisioningResultDto>>>(),
            Arg.Any<CancellationToken>());
        await _tenantRepository.DidNotReceive().Create(Arg.Any<Tenant>());
    }

    [Test]
    public async Task EnsureAsync_WithManagementRequest_AppliesBootstrapInsideLockedProvisionerTransaction()
    {
        Guid operationId = Guid.CreateVersion7();
        Guid outboxMessageId = Guid.CreateVersion7();
        Guid planVersionId = Guid.CreateVersion7();
        var plan = new TenantPlan
        {
            Id = Guid.CreateVersion7(),
            Key = "standard",
            DisplayName = "Standard"
        };
        var version = new TenantPlanVersion
        {
            Id = planVersionId,
            TenantPlanId = plan.Id,
            TenantPlan = plan,
            VersionNumber = 1,
            TenantPlanStatusId = (int)TenantPlanStatusEnum.Published,
            CurrencyCode = "EUR",
            BillingPeriod = "month",
            IsActiveForProvisioning = true
        };
        var planRepository = Substitute.For<ITenantPlanRepository>();
        planRepository.GetVersionAsync(planVersionId, Arg.Any<CancellationToken>()).Returns(version);
        planRepository.CreateAssignmentAsync(
                Arg.Any<TenantPlanAssignment>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<TenantPlanAssignment>());
        var moduleRepository = Substitute.For<IModuleDefinitionRepository>();
        moduleRepository.GetActiveByKeysAsync(
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ModuleDefinition>());
        var tenantSettingRepository = Substitute.For<ITenantSettingRepository>();
        var systemSettingRepository = Substitute.For<ISystemSettingRepository>();
        var brandingLockService = Substitute.For<ITenantBrandingSettingsDocumentLockService>();
        brandingLockService.GetLockStateAsync(Arg.Any<CancellationToken>())
            .Returns(TenantBrandingSettingsDocumentLockState.AllowAll);
        brandingLockService.ValidateAllowedChanges(
                Arg.Any<Explore.Domain.Settings.Documents.Payloads.BrandingSettings>(),
                Arg.Any<Explore.Domain.Settings.Documents.Payloads.BrandingSettings>(),
                Arg.Any<TenantBrandingSettingsDocumentLockState>())
            .Returns(Array.Empty<string>());
        var managedOptions = Microsoft.Extensions.Options.Options.Create(new ManagedControlPlaneOptions
        {
            Enabled = true,
            MaximumTenantCount = 10
        });
        var mutationLock = new TrackingSettingMutationLock();
        var operationRepository = Substitute.For<IManagedTenantProvisioningOperationRepository>();
        operationRepository.CountActiveReservationsAsync(
                Arg.Any<CancellationToken>(),
                operationId)
            .Returns(0);
        operationRepository.TryCompleteAsync(
                operationId,
                outboxMessageId,
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => mutationLock.RecordCompletion());
        _instanceBootstrapStateRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(new InstanceBootstrapState
        {
            Id = Guid.CreateVersion7(),
            IsCompleted = true,
            SelectedDeploymentMode = DeploymentMode.MultiTenant.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        var onboardingRepository = Substitute.For<ITenantOnboardingStateRepository>();
        onboardingRepository.Create(Arg.Any<TenantOnboardingState>())
            .Returns(call => call.Arg<TenantOnboardingState>());
        var tenantCapabilityRepository = Substitute.For<ITenantCapabilityRepository>();
        var tenantSettingsDocumentRepository = Substitute.For<ITenantSettingsDocumentRepository>();
        tenantSettingsDocumentRepository.Update(Arg.Any<TenantSettingsDocument>())
            .Returns(Task.CompletedTask);
        var brandingService = Substitute.For<ITenantBrandingSettingsDocumentProvisioningService>();
        brandingService.EnsureTenantBrandingDocumentAsync(
                Arg.Any<Guid>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(TenantBrandingSettingsDocumentDefaults.Create(
                call.ArgAt<Guid>(0),
                call.ArgAt<string?>(1))));
        var emailRepository = Substitute.For<IEmailDispatchOutboxRepository>();
        var auditRepository = Substitute.For<IAuditLogRepository>();
        auditRepository.Create(Arg.Any<AuditLog>()).Returns(call => call.Arg<AuditLog>());
        var preflight = new ManagedTenantProvisioningPreflight(
            _tenantRepository,
            planRepository,
            moduleRepository,
            tenantSettingRepository,
            systemSettingRepository,
            brandingLockService,
            new TenantPlanStorageQuotaCeilingPolicy(systemSettingRepository),
            managedOptions);
        var handler = new EnsureManagedProviderClientProvisionedCommandHandler(
            _tenantRepository,
            _userRepository,
            _actorRepository,
            _userExternalLoginRepository,
            _tenantUserRepository,
            _tenantUserProfileRepository,
            _tenantUserRoleGrantRepository,
            _roleRepository,
            _organizationRepository,
            _organizationMemberRepository,
            _groupRepository,
            _groupMemberRepository,
            _externalBindingRepository,
            onboardingRepository,
            operationRepository,
            planRepository,
            tenantCapabilityRepository,
            tenantSettingRepository,
            tenantSettingsDocumentRepository,
            emailRepository,
            auditRepository,
            brandingService,
            Substitute.For<ITypedSettingsDocumentResolver>(),
            Substitute.For<IHierarchicalSettingsResolver>(),
            preflight,
            new TenantActivationCapacityPolicy(
                _instanceBootstrapStateRepository,
                _tenantRepository,
                operationRepository,
                managedOptions),
            managedOptions,
            mutationLock,
            _unitOfWork,
            Substitute.For<ILogger<EnsureManagedProviderClientProvisionedCommandHandler>>());
        var managementRequest = new ManagementTenantProvisioningRequestDto
        {
            ExternalRequestId = "request-1",
            ExternalCustomerReference = "customer-123",
            TenantName = "ERP Customer",
            TenantSlug = "erp-customer",
            Administrator = new ManagementTenantAdministratorDto
            {
                ExternalIdentity = new ManagementTenantExternalIdentityDto
                {
                    IdentityProvider = "keycloak",
                    Subject = "external-admin-1",
                    Email = "admin@example.com",
                    FirstName = "Amina",
                    LastName = "Admin",
                    EmailVerified = true
                }
            },
            Plan = new ManagementTenantPlanDto
            {
                Key = "standard",
                VersionId = planVersionId,
                Quotas = []
            },
            ApprovedModules = [],
            InitialSettings = [new("appearance.language", "\"en\"")]
        };
        var provisioningDto = CreateValidDto(
            providerKey: "islamu-event-control-plane",
            externalSystem: "control-plane");
        ManagementTenantProvisioningRequestDto normalizedRequest =
            ManagedTenantProvisioningRequestCodec.Normalize(managementRequest);
        Guid managedInstanceId = Guid.CreateVersion7();
        operationRepository.GetByIdAsNoTrackingAsync(operationId, Arg.Any<CancellationToken>())
            .Returns(new ManagedTenantProvisioningOperation
            {
                Id = operationId,
                ManagedInstanceId = managedInstanceId,
                ExternalRequestId = normalizedRequest.ExternalRequestId,
                ExternalCustomerReference = normalizedRequest.ExternalCustomerReference,
                RequestHash = ManagedTenantProvisioningRequestCodec.ComputeHash(normalizedRequest),
                RequestJson = ManagedTenantProvisioningRequestCodec.Serialize(normalizedRequest),
                TenantSlug = normalizedRequest.TenantSlug,
                CurrentOutboxMessageId = outboxMessageId,
                Status = ManagedTenantProvisioningStatus.Processing,
                CreatedAt = DateTime.UtcNow
            });

        BaseCommandResponse<ManagedProviderClientProvisioningResultDto> result = await handler.EnsureAsync(
            provisioningDto,
            managementRequest,
            operationId,
            outboxMessageId,
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(mutationLock.CompletionObservedInside).IsTrue();
        await onboardingRepository.Received(1).Create(Arg.Is<TenantOnboardingState>(state =>
            state.TenantId == result.Id!.TenantId
            && !state.IsCompleted
            && state.CurrentStep == 0
            && state.TotalSteps == 4));
        await planRepository.Received(1).CreateAssignmentAsync(
            Arg.Is<TenantPlanAssignment>(assignment =>
                assignment.AssignedByUserId == result.Id!.UserId
                && assignment.CreatedBy == null),
            Arg.Any<CancellationToken>());
        await tenantSettingRepository.Received(1).UpsertManyForTenantAsync(
            result.Id!.TenantId,
            Arg.Any<IReadOnlyCollection<TenantSettingOverrideUpsert>>(),
            result.Id.UserId,
            Arg.Any<CancellationToken>());
        await tenantSettingsDocumentRepository.Received(1).Update(
            Arg.Is<TenantSettingsDocument>(document => document.UpdatedBy == null));
        await auditRepository.Received(1).Create(
            Arg.Is<AuditLog>(audit => audit.ActorId == null
                && audit.NewValues != null
                && audit.NewValues.Contains(operationId.ToString("D"), StringComparison.Ordinal)
                && audit.NewValues.Contains(managedInstanceId.ToString("D"), StringComparison.Ordinal)));
        await _externalBindingRepository.Received(1).Create(
            Arg.Is<ExternalBinding>(binding =>
                binding.ExternalType == ExternalBindingTypes.External.ManagedTenantProvisioningOperation
                && binding.ExternalId == operationId.ToString("D")
                && binding.InternalId == result.Id.TenantId
                && binding.ScopeTenantId == result.Id.TenantId));
    }

    [Test]
    public async Task HandlerConstructor_DoesNotDependOnPlatformRoleRepository()
    {
        var constructorParameterTypes = typeof(EnsureManagedProviderClientProvisionedCommandHandler)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToList();

        await Assert.That(constructorParameterTypes).DoesNotContain(typeof(IPlatformUserRoleRepository));
    }

    private static ManagedProviderClientProvisioningDto CreateValidDto(
        ManagedProviderOrganizerDto? organizer = null,
        string providerKey = "erp-provider",
        string externalSystem = "erp",
        bool emailVerified = true) => new()
        {
            ProviderKey = providerKey,
            ExternalSystem = externalSystem,
            ExternalCustomerId = "customer-123",
            TenantFullName = "ERP Customer",
            TenantSlug = "erp-customer",
            ActivateTenant = true,
            ExternalAdmin = new ManagedProviderExternalAdminDto
            {
                IdentityProvider = "keycloak",
                Subject = "external-admin-1",
                Email = "admin@example.com",
                FirstName = "Amina",
                LastName = "Admin",
                EmailVerified = emailVerified
            },
            Organizer = organizer
        };

    private sealed class ImmediateSettingMutationLock : ISettingMutationLock
    {
        internal static readonly ImmediateSettingMutationLock Instance = new();

        public Task<T> ExecuteAsync<T>(
            string canonicalSettingKey,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) => operation(cancellationToken);

        public Task<T> ExecuteManyAsync<T>(
            IEnumerable<string> canonicalSettingKeys,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) => operation(cancellationToken);
    }

    private sealed class TrackingSettingMutationLock : ISettingMutationLock
    {
        internal bool CompletionObservedInside { get; private set; }

        private bool IsExecuting { get; set; }

        internal bool RecordCompletion()
        {
            CompletionObservedInside = IsExecuting;
            return CompletionObservedInside;
        }

        public Task<T> ExecuteAsync<T>(
            string canonicalSettingKey,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) =>
            ExecuteManyAsync([canonicalSettingKey], operation, cancellationToken);

        public async Task<T> ExecuteManyAsync<T>(
            IEnumerable<string> canonicalSettingKeys,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            IsExecuting = true;
            try
            {
                return await operation(cancellationToken);
            }
            finally
            {
                IsExecuting = false;
            }
        }
    }
}
