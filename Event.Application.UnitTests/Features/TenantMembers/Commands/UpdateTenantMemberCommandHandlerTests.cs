// ABOUTME: Unit tests for UpdateTenantMemberCommandHandler validation and persistence behavior.
// ABOUTME: Covers successful mapping, not-found handling, and validator short-circuiting.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantMember;
using Explore.Application.Features.TenantMembers.Handlers.Commands;
using Explore.Application.Features.TenantMembers.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.TenantMembers.Commands;

public sealed class UpdateTenantMemberCommandHandlerTests
{
    private readonly ITenantMemberRepository _tenantMemberRepository = Substitute.For<ITenantMemberRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly UpdateTenantMemberCommandHandler _handler;

    public UpdateTenantMemberCommandHandlerTests()
    {
        _handler = new UpdateTenantMemberCommandHandler(
            _tenantMemberRepository,
            _userRepository,
            _roleRepository,
            _mapper);
    }

    [Test]
    public async Task Handle_WithValidDto_MapsAndPersistsExistingTenantMember()
    {
        var dto = CreateValidDto();
        var existingMember = CreateTenantMember(dto.Id, dto.UserId, dto.TenantId, (int)RoleEnum.TenantMember);
        SetupValidLookups(dto);
        _tenantMemberRepository.GetById(dto.Id).Returns(existingMember);

        var result = await _handler.Handle(new UpdateTenantMemberCommand { TenantMemberDto = dto }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(existingMember.Id);
        await Assert.That(result.Message).IsEqualTo("Tenant Member updated successfully.");
        _mapper.Received(1).Map(dto, existingMember);
        await _tenantMemberRepository.Received(1).Update(existingMember);
    }

    [Test]
    public async Task Handle_WhenTenantMemberDoesNotExist_ReturnsNotFoundAndDoesNotPersist()
    {
        var dto = CreateValidDto();
        SetupValidLookups(dto);
        _tenantMemberRepository.GetById(dto.Id).Returns((TenantMember?)null);

        var result = await _handler.Handle(new UpdateTenantMemberCommand { TenantMemberDto = dto }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Tenant Member not found.");
        _mapper.DidNotReceive().Map(Arg.Any<UpdateTenantMemberDto>(), Arg.Any<TenantMember>());
        await _tenantMemberRepository.DidNotReceive().Update(Arg.Any<TenantMember>());
    }

    [Test]
    public async Task Handle_WhenValidationFails_ReturnsErrorsAndSkipsRepositoryRead()
    {
        var dto = CreateValidDto();
        _userRepository.Exists(dto.UserId).Returns(false);
        _roleRepository.GetByIdAsync(dto.RoleId).Returns(CreateTenantRole(dto.RoleId));

        var result = await _handler.Handle(new UpdateTenantMemberCommand { TenantMemberDto = dto }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Tenant Member update failed.");
        await Assert.That(result.Errors).Contains("User does not exist");
        await _tenantMemberRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        _mapper.DidNotReceive().Map(Arg.Any<UpdateTenantMemberDto>(), Arg.Any<TenantMember>());
        await _tenantMemberRepository.DidNotReceive().Update(Arg.Any<TenantMember>());
    }

    private void SetupValidLookups(UpdateTenantMemberDto dto)
    {
        _userRepository.Exists(dto.UserId).Returns(true);
        _roleRepository.GetByIdAsync(dto.RoleId).Returns(CreateTenantRole(dto.RoleId));
    }

    private static UpdateTenantMemberDto CreateValidDto() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        RoleId = (int)RoleEnum.TenantAdmin
    };

    private static TenantMember CreateTenantMember(Guid id, Guid userId, Guid tenantId, int roleId) => new()
    {
        Id = id,
        UserId = userId,
        TenantId = tenantId,
        RoleId = roleId,
        User = null!,
        Tenant = null!,
        Role = null!
    };

    private static Role CreateTenantRole(int roleId) => new()
    {
        Id = roleId,
        MasterCode = "tenant.admin",
        FullName = "Tenant Admin",
        Scope = RoleScopeEnum.Tenant
    };
}
