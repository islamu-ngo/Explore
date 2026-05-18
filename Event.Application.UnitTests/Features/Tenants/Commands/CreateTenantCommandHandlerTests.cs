// ABOUTME: Unit tests for CreateTenantCommandHandler covering tenant creation, slug uniqueness, and admin assignment.
// ABOUTME: Verifies idempotent admin assignment and graceful handling of missing roles.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Features.Tenants.Handlers.Commands.CreateTenant;
using Explore.Application.Features.Tenants.Requests.Commands;
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
    private readonly ITenantMemberRepository _tenantMemberRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ITenantBrandingSettingsDocumentProvisioningService _tenantBrandingProvisioningService;
    private readonly ILogger<CreateTenantCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateTenantCommandHandler _handler;

    public CreateTenantCommandHandlerTests()
    {
        _tenantRepository = Substitute.For<ITenantRepository>();
        _tenantMemberRepository = Substitute.For<ITenantMemberRepository>();
        _roleRepository = Substitute.For<IRoleRepository>();
        _tenantBrandingProvisioningService = Substitute.For<ITenantBrandingSettingsDocumentProvisioningService>();
        _tenantBrandingProvisioningService
            .EnsureTenantBrandingDocumentAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(TenantBrandingSettingsDocumentDefaults.Create(call.ArgAt<Guid>(0), call.ArgAt<string?>(1))));
        _logger = Substitute.For<ILogger<CreateTenantCommandHandler>>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        // Execute lambdas inline — InMemory provider path
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<Guid>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var op = callInfo.Arg<Func<CancellationToken, Task<Guid>>>();
                return op(CancellationToken.None);
            });

        _handler = new CreateTenantCommandHandler(
            _tenantRepository,
            _tenantMemberRepository,
            _roleRepository,
            _tenantBrandingProvisioningService,
            _logger,
            _unitOfWork);
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
        await _tenantMemberRepository.DidNotReceive().Create(Arg.Any<TenantMember>());
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
    public async Task Handle_WhenAssignCurrentUserAsTenantAdmin_CreatesTenantMember()
    {
        var dto = CreateValidDto(assignAdmin: true);
        var tenantId = Guid.NewGuid();
        _tenantRepository.GetTenantBySlug(dto.Slug).Returns((Tenant?)null);
        _tenantRepository.Create(Arg.Any<Tenant>()).Returns(new Tenant { Id = tenantId, FullName = dto.FullName, Slug = dto.Slug, TenantStatus = null! });
        _roleRepository.GetByMasterCodeAsync("tenant.admin").Returns(new Role { Id = (int)RoleEnum.TenantAdmin, MasterCode = "tenant.admin", FullName = "Admin", Scope = RoleScopeEnum.Tenant });
        _tenantMemberRepository.GetByTenantAndUser(tenantId, TestUserId).Returns((TenantMember?)null);

        var result = await _handler.Handle(
            new CreateTenantCommand { TenantDto = dto, RequestingUserId = TestUserId },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _tenantMemberRepository.Received(1).Create(Arg.Is<TenantMember>(m =>
            m.TenantId == tenantId && m.UserId == TestUserId && m.RoleId == (int)RoleEnum.TenantAdmin));
    }

    [Test]
    public async Task Handle_WhenAssignAdminFlagFalse_DoesNotCreateTenantMember()
    {
        var dto = CreateValidDto(assignAdmin: false);
        var tenantId = Guid.NewGuid();
        _tenantRepository.GetTenantBySlug(dto.Slug).Returns((Tenant?)null);
        _tenantRepository.Create(Arg.Any<Tenant>()).Returns(new Tenant { Id = tenantId, FullName = dto.FullName, Slug = dto.Slug, TenantStatus = null! });

        var result = await _handler.Handle(
            new CreateTenantCommand { TenantDto = dto, RequestingUserId = TestUserId },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _tenantMemberRepository.DidNotReceive().Create(Arg.Any<TenantMember>());
        await _roleRepository.DidNotReceive().GetByMasterCodeAsync(Arg.Any<string>());
    }

    [Test]
    public async Task Handle_WhenUserAlreadyTenantMember_DoesNotCreateDuplicate()
    {
        var dto = CreateValidDto(assignAdmin: true);
        var tenantId = Guid.NewGuid();
        _tenantRepository.GetTenantBySlug(dto.Slug).Returns((Tenant?)null);
        _tenantRepository.Create(Arg.Any<Tenant>()).Returns(new Tenant { Id = tenantId, FullName = dto.FullName, Slug = dto.Slug, TenantStatus = null! });
        _roleRepository.GetByMasterCodeAsync("tenant.admin").Returns(new Role { Id = (int)RoleEnum.TenantAdmin, MasterCode = "tenant.admin", FullName = "Admin", Scope = RoleScopeEnum.Tenant });
        _tenantMemberRepository.GetByTenantAndUser(tenantId, TestUserId)
            .Returns(new TenantMember { TenantId = tenantId, UserId = TestUserId, Tenant = null!, User = null!, Role = null!, RoleId = (int)RoleEnum.TenantAdmin });

        var result = await _handler.Handle(
            new CreateTenantCommand { TenantDto = dto, RequestingUserId = TestUserId },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _tenantMemberRepository.DidNotReceive().Create(Arg.Any<TenantMember>());
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
        await _tenantMemberRepository.DidNotReceive().Create(Arg.Any<TenantMember>());
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
