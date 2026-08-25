// ABOUTME: Unit tests for CreateTenantUserRoleGrantCommandHandler tenant-context and audit behavior.
// ABOUTME: Verifies active TenantUser gating, duplicate prevention, and grant persistence.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantUserRoleGrant;
using Explore.Application.Features.TenantUserRoleGrants.Handlers.Commands;
using Explore.Application.Features.TenantUserRoleGrants.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.TenantUserRoleGrants.Commands;

public sealed class CreateTenantUserRoleGrantCommandHandlerTests
{
    private readonly ITenantUserRoleGrantRepository _tenantUserRoleGrantRepository = Substitute.For<ITenantUserRoleGrantRepository>();
    private readonly ITenantUserRepository _tenantUserRepository = Substitute.For<ITenantUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly CreateTenantUserRoleGrantCommandHandler _handler;

    public CreateTenantUserRoleGrantCommandHandlerTests()
    {
        _handler = new CreateTenantUserRoleGrantCommandHandler(
            _tenantUserRoleGrantRepository,
            _tenantUserRepository,
            _roleRepository,
            _tenantContext,
            _currentUserService);
    }

    [Test]
    public async Task Command_CarriesTenantUserRoleGrantCreateAuthorizationContext()
    {
        var tenantId = Guid.NewGuid();
        var command = new CreateTenantUserRoleGrantCommand
        {
            TenantUserRoleGrantDto = CreateValidDto(),
            TenantId = tenantId
        };
        var attribute = typeof(CreateTenantUserRoleGrantCommand)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: true)
            .Cast<AuthorizeResourceAttribute>()
            .SingleOrDefault();
        var secureRequest = (ISecureRequest)command;

        await Assert.That(attribute).IsNotNull();
        await Assert.That(attribute!.Resource).IsEqualTo(ResourceKinds.TenantUserRoleGrant);
        await Assert.That(attribute.Action).IsEqualTo(AuthorizationActions.Create);
        await Assert.That(secureRequest.ResourceId).IsEqualTo(tenantId.ToString("D"));
        await Assert.That(secureRequest.AuthorizationFacts).IsEqualTo(new TenantScopedAuthorizationFacts(tenantId));
    }

    [Test]
    public async Task Handle_WithValidDto_CreatesGrantWithContextTenantAndAuditFields()
    {
        var dto = CreateValidDto();
        var tenantIdFromContext = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var createdId = Guid.NewGuid();
        var tenantUser = CreateTenantUser(dto.TenantUserId, tenantIdFromContext, TenantUserStatusEnum.Active);

        SetupValidLookups(dto, tenantUser, tenantIdFromContext);
        _currentUserService.UserId.Returns(currentUserId);
        _tenantUserRoleGrantRepository.GetByTenantUserAndRole(tenantIdFromContext, dto.TenantUserId, dto.RoleId)
            .Returns((TenantUserRoleGrant?)null);
        _tenantUserRoleGrantRepository.Create(Arg.Any<TenantUserRoleGrant>()).Returns(callInfo =>
        {
            var grant = callInfo.Arg<TenantUserRoleGrant>();
            grant.Id = createdId;
            return grant;
        });

        var beforeHandle = DateTime.UtcNow;
        var result = await _handler.Handle(new CreateTenantUserRoleGrantCommand { TenantUserRoleGrantDto = dto }, CancellationToken.None);
        var afterHandle = DateTime.UtcNow;

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsEqualTo(createdId);
        await Assert.That(result.Message).IsEqualTo("Tenant user role grant created successfully.");
        await _tenantUserRoleGrantRepository.Received(1).Create(Arg.Is<TenantUserRoleGrant>(grant =>
            grant.TenantUserId == tenantUser.Id
            && grant.RoleId == dto.RoleId
            && grant.TenantId == tenantIdFromContext
            && grant.RoleScopeId == (int)RoleScopeEnum.Tenant
            && grant.GrantedBy == currentUserId
            && grant.GrantedAt >= beforeHandle
            && grant.GrantedAt <= afterHandle));
    }

    [Test]
    public async Task Handle_WhenTenantUserIsNotActive_ReturnsFailureAndDoesNotPersist()
    {
        var dto = CreateValidDto();
        var tenantIdFromContext = Guid.NewGuid();
        var tenantUser = CreateTenantUser(dto.TenantUserId, tenantIdFromContext, TenantUserStatusEnum.Suspended);
        SetupValidLookups(dto, tenantUser, tenantIdFromContext);

        var result = await _handler.Handle(new CreateTenantUserRoleGrantCommand { TenantUserRoleGrantDto = dto }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Tenant user role grant creation failed.");
        await Assert.That(result.Errors).Contains("Tenant-local user must be active before a role can be granted.");
        await _tenantUserRoleGrantRepository.DidNotReceive().Create(Arg.Any<TenantUserRoleGrant>());
    }

    [Test]
    public async Task Handle_WhenValidationFails_ReturnsFailureAndDoesNotPersist()
    {
        var dto = CreateValidDto();
        _tenantUserRepository.Exists(dto.TenantUserId).Returns(false);
        _roleRepository.GetByIdAsync(dto.RoleId).Returns(CreateTenantRole(dto.RoleId));

        var result = await _handler.Handle(new CreateTenantUserRoleGrantCommand { TenantUserRoleGrantDto = dto }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Tenant user role grant creation failed.");
        await Assert.That(result.Errors).Contains("Tenant user does not exist");
        await _tenantUserRoleGrantRepository.DidNotReceive().Create(Arg.Any<TenantUserRoleGrant>());
    }

    [Test]
    public async Task Handle_WhenActiveGrantAlreadyExists_ReturnsFailureAndDoesNotPersist()
    {
        var dto = CreateValidDto();
        var tenantIdFromContext = Guid.NewGuid();
        var tenantUser = CreateTenantUser(dto.TenantUserId, tenantIdFromContext, TenantUserStatusEnum.Active);
        SetupValidLookups(dto, tenantUser, tenantIdFromContext);
        _tenantUserRoleGrantRepository.GetByTenantUserAndRole(tenantIdFromContext, dto.TenantUserId, dto.RoleId)
            .Returns(new TenantUserRoleGrant
            {
                Id = Guid.NewGuid(),
                TenantId = tenantIdFromContext,
                Tenant = null!,
                TenantUserId = dto.TenantUserId,
                TenantUser = null!,
                RoleId = dto.RoleId,
                Role = null!,
                RoleScopeId = (int)RoleScopeEnum.Tenant,
                GrantedAt = DateTime.UtcNow.AddDays(-1)
            });

        var result = await _handler.Handle(new CreateTenantUserRoleGrantCommand { TenantUserRoleGrantDto = dto }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors).Contains("An active grant for this tenant user and role already exists.");
        await _tenantUserRoleGrantRepository.DidNotReceive().Create(Arg.Any<TenantUserRoleGrant>());
    }

    private void SetupValidLookups(CreateTenantUserRoleGrantDto dto, TenantUser tenantUser, Guid contextTenantId)
    {
        _tenantContext.TenantId.Returns(contextTenantId);
        _tenantUserRepository.Exists(dto.TenantUserId).Returns(true);
        _tenantUserRepository.GetById(dto.TenantUserId).Returns(tenantUser);
        _roleRepository.GetByIdAsync(dto.RoleId).Returns(CreateTenantRole(dto.RoleId));
    }

    private static CreateTenantUserRoleGrantDto CreateValidDto() => new()
    {
        TenantUserId = Guid.NewGuid(),
        RoleId = (int)RoleEnum.TenantAdmin
    };

    private static Role CreateTenantRole(int roleId) => new()
    {
        Id = roleId,
        MasterCode = "tenant.admin",
        FullName = "Tenant Admin",
        Scope = RoleScopeEnum.Tenant
    };

    private static TenantUser CreateTenantUser(Guid id, Guid tenantId, TenantUserStatusEnum status) => new()
    {
        Id = id,
        TenantId = tenantId,
        Tenant = null!,
        UserId = Guid.NewGuid(),
        User = null!,
        StatusId = (int)status
    };
}
