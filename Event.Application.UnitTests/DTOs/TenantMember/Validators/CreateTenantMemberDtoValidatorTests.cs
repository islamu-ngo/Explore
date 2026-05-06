// ABOUTME: Unit tests for CreateTenantMemberDtoValidator repository-backed validation.
// ABOUTME: Covers required user/role IDs and existence checks without persistence dependencies.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantMember;
using Explore.Application.DTOs.TenantMember.Validators;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.DTOs.TenantMember.Validators;

public sealed class CreateTenantMemberDtoValidatorTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly CreateTenantMemberDtoValidator _validator;

    public CreateTenantMemberDtoValidatorTests()
    {
        _validator = new CreateTenantMemberDtoValidator(_userRepository, _roleRepository);
    }

    [Test]
    public async Task Validate_WithExistingUserAndRole_ReturnsValid()
    {
        var dto = CreateValidDto();
        SetupExistingUserAndRole(dto);

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WhenUserIdIsEmpty_ReturnsRequiredError()
    {
        var dto = CreateValidDto();
        dto.UserId = Guid.Empty;
        _roleRepository.GetByIdAsync(dto.RoleId).Returns(CreateTenantRole(dto.RoleId));

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("User ID is required");
        await _userRepository.DidNotReceive().Exists(Guid.Empty);
    }

    [Test]
    public async Task Validate_WhenUserDoesNotExist_ReturnsExistenceError()
    {
        var dto = CreateValidDto();
        _userRepository.Exists(dto.UserId).Returns(false);
        _roleRepository.GetByIdAsync(dto.RoleId).Returns(CreateTenantRole(dto.RoleId));

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("User does not exist");
    }

    [Test]
    public async Task Validate_WhenRoleIdIsEmpty_ReturnsRequiredError()
    {
        var dto = CreateValidDto();
        dto.RoleId = 0;
        _userRepository.Exists(dto.UserId).Returns(true);

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
        _userRepository.Exists(dto.UserId).Returns(true);
        _roleRepository.GetByIdAsync(dto.RoleId).Returns((Role?)null);

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("Role does not exist");
    }

    private void SetupExistingUserAndRole(CreateTenantMemberDto dto)
    {
        _userRepository.Exists(dto.UserId).Returns(true);
        _roleRepository.GetByIdAsync(dto.RoleId).Returns(CreateTenantRole(dto.RoleId));
    }

    private static CreateTenantMemberDto CreateValidDto() => new()
    {
        UserId = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        RoleId = (int)RoleEnum.TenantAdmin
    };

    private static Role CreateTenantRole(int roleId) => new()
    {
        Id = roleId,
        MasterCode = "tenant.admin",
        FullName = "Tenant Admin",
        Scope = RoleScopeEnum.Tenant
    };
}
