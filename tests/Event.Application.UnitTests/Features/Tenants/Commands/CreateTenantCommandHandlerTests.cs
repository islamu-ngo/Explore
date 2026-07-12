// ABOUTME: Unit tests for CreateTenantCommandHandler covering tenant creation, slug uniqueness, and admin assignment.
// ABOUTME: Verifies idempotent admin assignment and graceful handling of missing roles.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Features.Management;
using Explore.Application.Features.Tenants.Handlers.Commands.CreateTenant;
using Explore.Application.Features.Tenants.Requests.Commands;
using Explore.Application.Management;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Settings.Documents;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Tenants.Commands;

public class CreateTenantCommandHandlerTests
{
    private static readonly Guid TestUserId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000222");

    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantUserRoleGrantRepository _tenantUserRoleGrantRepository;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ITenantBrandingSettingsDocumentProvisioningService _tenantBrandingProvisioningService;
    private readonly ILogger<CreateTenantCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateTenantCommandHandler _handler;

    public CreateTenantCommandHandlerTests()
    {
        _tenantRepository = Substitute.For<ITenantRepository>();
        _tenantUserRoleGrantRepository = Substitute.For<ITenantUserRoleGrantRepository>();
        _tenantUserRepository = Substitute.For<ITenantUserRepository>();
        _roleRepository = Substitute.For<IRoleRepository>();
        _tenantBrandingProvisioningService = Substitute.For<ITenantBrandingSettingsDocumentProvisioningService>();
        _tenantBrandingProvisioningService
            .EnsureTenantBrandingDocumentAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(TenantBrandingSettingsDocumentDefaults.Create(call.ArgAt<Guid>(0), call.ArgAt<string?>(1))));
        _logger = Substitute.For<ILogger<CreateTenantCommandHandler>>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        // Execute lambdas inline — InMemory provider path
        _unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var op = callInfo.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>();
                return op(CancellationToken.None);
            });
        _tenantUserRepository.Create(Arg.Any<TenantUser>()).Returns(callInfo =>
        {
            var tenantUser = callInfo.Arg<TenantUser>();
            tenantUser.Id = tenantUser.Id == Guid.Empty ? Guid.NewGuid() : tenantUser.Id;
            return tenantUser;
        });

        _handler = new CreateTenantCommandHandler(
            _tenantRepository,
            _tenantUserRoleGrantRepository,
            _tenantUserRepository,
            _roleRepository,
            _tenantBrandingProvisioningService,
            _logger,
            _unitOfWork,
            Substitute.For<ISettingMutationLock>(),
            new TenantActivationCapacityPolicy(
                Substitute.For<IInstanceBootstrapStateRepository>(),
                _tenantRepository,
                Substitute.For<IManagedTenantProvisioningOperationRepository>(),
                Microsoft.Extensions.Options.Options.Create(new ManagedControlPlaneOptions())));
    }

    [Test]
    public async Task Handle_WithValidDto_CreatesTenantAndReturnsSuccess()
    {
        var dto = CreateValidDto();
        var createdTenant = new Tenant { Id = Guid.NewGuid(), FullName = dto.FullName, Slug = dto.Slug, TenantStatus = null!, TenantStatusId = (int)TenantStatusEnum.Provisioning };
        _tenantRepository.GetTenantBySlug(dto.Slug).Returns((Tenant?)null);
        _tenantRepository.Create(Arg.Any<Tenant>()).Returns(createdTenant);

        var result = await _handler.Handle(new CreateTenantCommand { TenantDto = dto }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(createdTenant.Id);
        await _tenantRepository.Received(1).Create(Arg.Any<Tenant>());
        await _tenantBrandingProvisioningService.Received(1).EnsureTenantBrandingDocumentAsync(createdTenant.Id, dto.FullName, Arg.Any<CancellationToken>());
        await _tenantUserRoleGrantRepository.DidNotReceive().Create(Arg.Any<TenantUserRoleGrant>());
    }

    [Test]
    public async Task Handle_WhenSlugAlreadyExists_ReturnsFailure()
    {
        var dto = CreateValidDto();
        _tenantRepository.GetTenantBySlug(dto.Slug).Returns(new Tenant { FullName = "Other", Slug = dto.Slug, TenantStatus = null! });

        var result = await _handler.Handle(new CreateTenantCommand { TenantDto = dto }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("slug already exists");
        await _tenantRepository.DidNotReceive().Create(Arg.Any<Tenant>());
        await _tenantBrandingProvisioningService.DidNotReceive().EnsureTenantBrandingDocumentAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenAssignCurrentUserAsTenantAdmin_CreatesTenantUserRoleGrant()
    {
        var dto = CreateValidDto(assignAdmin: true);
        var tenantId = Guid.NewGuid();
        _tenantRepository.GetTenantBySlug(dto.Slug).Returns((Tenant?)null);
        _tenantRepository.Create(Arg.Any<Tenant>()).Returns(new Tenant { Id = tenantId, FullName = dto.FullName, Slug = dto.Slug, TenantStatus = null! });
        _roleRepository.GetByMasterCodeAsync("tenant.admin").Returns(new Role { Id = (int)RoleEnum.TenantAdmin, MasterCode = "tenant.admin", FullName = "Admin", Scope = RoleScopeEnum.Tenant });
        _tenantUserRoleGrantRepository.GetByTenantAndUser(tenantId, TestUserId).Returns((TenantUserRoleGrant?)null);
        _tenantUserRepository.GetByTenantAndUserAsync(tenantId, TestUserId, Arg.Any<CancellationToken>()).Returns((TenantUser?)null);

        var result = await _handler.Handle(
            new CreateTenantCommand { TenantDto = dto, RequestingUserId = TestUserId },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _tenantUserRepository.Received(1).Create(Arg.Is<TenantUser>(tenantUser =>
            tenantUser.TenantId == tenantId &&
            tenantUser.UserId == TestUserId &&
            tenantUser.StatusId == (int)TenantUserStatusEnum.Active));
        await _tenantUserRoleGrantRepository.Received(1).Create(Arg.Is<TenantUserRoleGrant>(grant =>
            grant.TenantId == tenantId &&
            grant.RoleId == (int)RoleEnum.TenantAdmin &&
            grant.RoleScopeId == (int)RoleScopeEnum.Tenant &&
            grant.GrantedBy == TestUserId));
    }

    [Test]
    public async Task Handle_WhenAssignAdminFlagFalse_DoesNotCreateTenantUserRoleGrant()
    {
        var dto = CreateValidDto(assignAdmin: false);
        var tenantId = Guid.NewGuid();
        _tenantRepository.GetTenantBySlug(dto.Slug).Returns((Tenant?)null);
        _tenantRepository.Create(Arg.Any<Tenant>()).Returns(new Tenant { Id = tenantId, FullName = dto.FullName, Slug = dto.Slug, TenantStatus = null! });

        var result = await _handler.Handle(
            new CreateTenantCommand { TenantDto = dto, RequestingUserId = TestUserId },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _tenantUserRoleGrantRepository.DidNotReceive().Create(Arg.Any<TenantUserRoleGrant>());
        await _roleRepository.DidNotReceive().GetByMasterCodeAsync(Arg.Any<string>());
    }

    [Test]
    public async Task Handle_WhenUserAlreadyHasTenantUserRoleGrant_DoesNotCreateDuplicate()
    {
        var dto = CreateValidDto(assignAdmin: true);
        var tenantId = Guid.NewGuid();
        _tenantRepository.GetTenantBySlug(dto.Slug).Returns((Tenant?)null);
        _tenantRepository.Create(Arg.Any<Tenant>()).Returns(new Tenant { Id = tenantId, FullName = dto.FullName, Slug = dto.Slug, TenantStatus = null! });
        _roleRepository.GetByMasterCodeAsync("tenant.admin").Returns(new Role { Id = (int)RoleEnum.TenantAdmin, MasterCode = "tenant.admin", FullName = "Admin", Scope = RoleScopeEnum.Tenant });
        _tenantUserRoleGrantRepository.GetByTenantAndUser(tenantId, TestUserId)
            .Returns(new TenantUserRoleGrant { TenantId = tenantId, TenantUserId = Guid.NewGuid(), Tenant = null!, TenantUser = null!, Role = null!, RoleId = (int)RoleEnum.TenantAdmin });

        var result = await _handler.Handle(
            new CreateTenantCommand { TenantDto = dto, RequestingUserId = TestUserId },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _tenantUserRoleGrantRepository.DidNotReceive().Create(Arg.Any<TenantUserRoleGrant>());
    }

    [Test]
    public async Task Handle_WhenTenantAdminRoleNotFound_CreatesTenantWithoutMember()
    {
        var dto = CreateValidDto(assignAdmin: true);
        var tenantId = Guid.NewGuid();
        _tenantRepository.GetTenantBySlug(dto.Slug).Returns((Tenant?)null);
        _tenantRepository.Create(Arg.Any<Tenant>()).Returns(new Tenant { Id = tenantId, FullName = dto.FullName, Slug = dto.Slug, TenantStatus = null! });
        _roleRepository.GetByMasterCodeAsync("tenant.admin").Returns((Role?)null);
        _roleRepository.GetByIdAsync((int)RoleEnum.TenantAdmin).Returns((Role?)null);

        var result = await _handler.Handle(
            new CreateTenantCommand { TenantDto = dto, RequestingUserId = TestUserId },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _tenantUserRoleGrantRepository.DidNotReceive().Create(Arg.Any<TenantUserRoleGrant>());
    }

    private static CreateTenantDto CreateValidDto(bool assignAdmin = false) =>
        new()
        {
            FullName = "Test Tenant",
            Slug = "test-tenant",
            IsActive = false,
            AssignCurrentUserAsTenantAdmin = assignAdmin
        };
}
