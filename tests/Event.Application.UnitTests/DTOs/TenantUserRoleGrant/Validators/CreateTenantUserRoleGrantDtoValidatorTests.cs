// ABOUTME: Unit tests for CreateTenantUserRoleGrantDtoValidator repository-backed validation.
// ABOUTME: Covers required tenant-user/role IDs, existence checks, and tenant role scope enforcement.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantUserRoleGrant;
using Explore.Application.DTOs.TenantUserRoleGrant.Validators;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.DTOs.TenantUserRoleGrant.Validators;

public sealed class CreateTenantUserRoleGrantDtoValidatorTests
{
    private readonly ITenantUserRepository _tenantUserRepository = Substitute.For<ITenantUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly CreateTenantUserRoleGrantDtoValidator _validator;

    public CreateTenantUserRoleGrantDtoValidatorTests()
    {
        _validator = new CreateTenantUserRoleGrantDtoValidator(_tenantUserRepository, _roleRepository);
    }

    [Test]
    public async Task Validate_WithExistingTenantUserAndRole_ReturnsValid()
    {
        var dto = CreateValidDto();
        SetupExistingTenantUserAndRole(dto);

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WhenTenantUserIdIsEmpty_ReturnsRequiredError()
    {
        var dto = CreateValidDto();
        dto = dto with { TenantUserId = Guid.Empty };
        _roleRepository.GetByIdAsync(dto.RoleId).Returns(CreateRole(dto.RoleId, RoleScopeEnum.Tenant));

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("Tenant user ID is required");
        await _tenantUserRepository.DidNotReceive().Exists(Guid.Empty);
    }

    [Test]
    public async Task Validate_WhenTenantUserDoesNotExist_ReturnsExistenceError()
    {
        var dto = CreateValidDto();
        _tenantUserRepository.Exists(dto.TenantUserId).Returns(false);
        _roleRepository.GetByIdAsync(dto.RoleId).Returns(CreateRole(dto.RoleId, RoleScopeEnum.Tenant));

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("Tenant user does not exist");
    }

    [Test]
    public async Task Validate_WhenRoleIdIsEmpty_ReturnsRequiredError()
    {
        var dto = CreateValidDto();
        dto = dto with { RoleId = 0 };
        _tenantUserRepository.Exists(dto.TenantUserId).Returns(true);

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("Role ID is required");
        await _roleRepository.DidNotReceive().GetByIdAsync(0);
    }

    [Test]
    public async Task Validate_WhenRoleDoesNotExist_ReturnsExistenceError()
    {
        var dto = CreateValidDto();
        _tenantUserRepository.Exists(dto.TenantUserId).Returns(true);
        _roleRepository.GetByIdAsync(dto.RoleId).Returns((Role?)null);

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("Role does not exist");
    }

    [Test]
    public async Task Validate_WhenRoleIsNotTenantScoped_ReturnsScopeError()
    {
        var dto = CreateValidDto();
        _tenantUserRepository.Exists(dto.TenantUserId).Returns(true);
        _roleRepository.GetByIdAsync(dto.RoleId).Returns(CreateRole(dto.RoleId, RoleScopeEnum.Platform));

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("Role must be a tenant-scoped role");
    }

    private void SetupExistingTenantUserAndRole(CreateTenantUserRoleGrantDto dto)
    {
        _tenantUserRepository.Exists(dto.TenantUserId).Returns(true);
        _roleRepository.GetByIdAsync(dto.RoleId).Returns(CreateRole(dto.RoleId, RoleScopeEnum.Tenant));
    }

    private static CreateTenantUserRoleGrantDto CreateValidDto() => new()
    {
        TenantUserId = Guid.NewGuid(),
        RoleId = (int)RoleEnum.TenantAdmin
    };

    private static Role CreateRole(int roleId, RoleScopeEnum roleScope) => new()
    {
        Id = roleId,
        MasterCode = "tenant.admin",
        FullName = "Tenant Admin",
        Scope = roleScope
    };
}
