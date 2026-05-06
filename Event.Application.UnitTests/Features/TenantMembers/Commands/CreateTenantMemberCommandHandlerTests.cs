// ABOUTME: Unit tests for CreateTenantMemberCommandHandler tenant-context and audit behavior.
// ABOUTME: Verifies validation failure boundaries and successful tenant member persistence.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantMember;
using Explore.Application.Features.TenantMembers.Handlers.Commands;
using Explore.Application.Features.TenantMembers.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.TenantMembers.Commands;

public sealed class CreateTenantMemberCommandHandlerTests
{
    private readonly ITenantMemberRepository _tenantMemberRepository = Substitute.For<ITenantMemberRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly CreateTenantMemberCommandHandler _handler;

    public CreateTenantMemberCommandHandlerTests()
    {
        _handler = new CreateTenantMemberCommandHandler(
            _tenantMemberRepository,
            _userRepository,
            _roleRepository,
            _tenantContext,
            _currentUserService,
            _mapper);
    }

    [Test]
    public async Task Handle_WithValidDto_CreatesTenantMemberWithContextTenantAndAuditFields()
    {
        var dto = CreateValidDto();
        var tenantIdFromContext = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var createdId = Guid.NewGuid();
        SetupValidLookups(dto);
        _tenantContext.TenantId.Returns(tenantIdFromContext);
        _currentUserService.UserId.Returns(currentUserId);
        _mapper.Map<TenantMember>(dto).Returns(new TenantMember
        {
            UserId = dto.UserId,
            TenantId = dto.TenantId,
            RoleId = dto.RoleId,
            User = null!,
            Tenant = null!,
            Role = null!
        });
        _tenantMemberRepository.Create(Arg.Any<TenantMember>()).Returns(callInfo =>
        {
            var member = callInfo.Arg<TenantMember>();
            member.Id = createdId;
            return member;
        });

        var beforeHandle = DateTime.UtcNow;
        var result = await _handler.Handle(new CreateTenantMemberCommand { TenantMemberDto = dto }, CancellationToken.None);
        var afterHandle = DateTime.UtcNow;

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(createdId);
        await Assert.That(result.Message).IsEqualTo("Tenant Member created successfully.");
        await _tenantMemberRepository.Received(1).Create(Arg.Is<TenantMember>(member =>
            member.UserId == dto.UserId
            && member.RoleId == dto.RoleId
            && member.TenantId == tenantIdFromContext
            && member.GrantedBy == currentUserId
            && member.GrantedAt >= beforeHandle
            && member.GrantedAt <= afterHandle));
    }

    [Test]
    public async Task Handle_WithClientTenantId_OverridesTenantIdFromTenantContext()
    {
        var dto = CreateValidDto();
        var clientTenantId = dto.TenantId;
        var contextTenantId = Guid.NewGuid();
        SetupValidLookups(dto);
        _tenantContext.TenantId.Returns(contextTenantId);
        _mapper.Map<TenantMember>(dto).Returns(new TenantMember
        {
            UserId = dto.UserId,
            TenantId = clientTenantId,
            RoleId = dto.RoleId,
            User = null!,
            Tenant = null!,
            Role = null!
        });
        _tenantMemberRepository.Create(Arg.Any<TenantMember>()).Returns(callInfo => callInfo.Arg<TenantMember>());

        var result = await _handler.Handle(new CreateTenantMemberCommand { TenantMemberDto = dto }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _tenantMemberRepository.Received(1).Create(Arg.Is<TenantMember>(member =>
            member.TenantId == contextTenantId
            && member.TenantId != clientTenantId));
    }

    [Test]
    public async Task Handle_WhenValidationFails_ReturnsFailureAndDoesNotPersist()
    {
        var dto = CreateValidDto();
        _userRepository.Exists(dto.UserId).Returns(false);
        _roleRepository.GetByIdAsync(dto.RoleId).Returns(CreateTenantRole(dto.RoleId));

        var result = await _handler.Handle(new CreateTenantMemberCommand { TenantMemberDto = dto }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Tenant Member creation failed.");
        await Assert.That(result.Errors).Contains("User does not exist");
        await _tenantMemberRepository.DidNotReceive().Create(Arg.Any<TenantMember>());
        _mapper.DidNotReceive().Map<TenantMember>(Arg.Any<CreateTenantMemberDto>());
    }

    private void SetupValidLookups(CreateTenantMemberDto dto)
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
