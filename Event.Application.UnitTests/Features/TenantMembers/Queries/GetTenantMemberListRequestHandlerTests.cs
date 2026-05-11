// ABOUTME: Unit tests for GetTenantMemberListRequestHandler list-query behavior.
// ABOUTME: Covers repository forwarding, DTO list mapping, and empty-list results.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantMember;
using Explore.Application.Features.TenantMembers.Handlers.Queries;
using Explore.Application.Features.TenantMembers.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.TenantMembers.Queries;

public sealed class GetTenantMemberListRequestHandlerTests
{
    private readonly ITenantMemberRepository _tenantMemberRepository = Substitute.For<ITenantMemberRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly GetTenantMemberListRequestHandler _handler;

    public GetTenantMemberListRequestHandlerTests()
    {
        _handler = new GetTenantMemberListRequestHandler(_tenantMemberRepository, _mapper);
    }

    [Test]
    public async Task Handle_WhenMembersExist_ReturnsMappedDtos()
    {
        var tenantId = Guid.NewGuid();
        var members = new List<TenantMember>
        {
            CreateTenantMember(tenantId, "amina.rahman@example.test", RoleEnum.TenantAdmin),
            CreateTenantMember(tenantId, "yusuf.khan@example.test", RoleEnum.TenantMember)
        };
        var expectedDtos = new List<TenantMemberListDto>
        {
            CreateListDto(members[0], "Amina Rahman", "Tenant Admin"),
            CreateListDto(members[1], "Yusuf Khan", "Tenant Member")
        };
        _tenantMemberRepository.GetMembersWithDetails().Returns(members);
        _mapper.Map<List<TenantMemberListDto>>(members).Returns(expectedDtos);

        var result = await _handler.Handle(new GetTenantMemberListRequest(), CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].UserEmail).IsEqualTo("amina.rahman@example.test");
        await Assert.That(result[1].RoleName).IsEqualTo("Tenant Member");
        await _tenantMemberRepository.Received(1).GetMembersWithDetails();
        _mapper.Received(1).Map<List<TenantMemberListDto>>(members);
    }

    [Test]
    public async Task Handle_WhenNoMembersExist_ReturnsEmptyMappedList()
    {
        var members = new List<TenantMember>();
        var expectedDtos = new List<TenantMemberListDto>();
        _tenantMemberRepository.GetMembersWithDetails().Returns(members);
        _mapper.Map<List<TenantMemberListDto>>(members).Returns(expectedDtos);

        var result = await _handler.Handle(new GetTenantMemberListRequest(), CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(0);
        await _tenantMemberRepository.Received(1).GetMembersWithDetails();
        _mapper.Received(1).Map<List<TenantMemberListDto>>(members);
    }

    private static TenantMember CreateTenantMember(Guid tenantId, string email, RoleEnum role)
    {
        var userId = Guid.NewGuid();
        var grantedAt = DateTime.UtcNow.AddDays(-1);
        var roleName = role == RoleEnum.TenantAdmin ? "Tenant Admin" : "Tenant Member";

        return new TenantMember
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            User = new User
            {
                Id = userId,
                Pii = new UserPii
                {
                    Email = email,
                    FirstName = email.StartsWith("amina", StringComparison.Ordinal) ? "Amina" : "Yusuf",
                    LastName = email.StartsWith("amina", StringComparison.Ordinal) ? "Rahman" : "Khan"
                }
            },
            TenantId = tenantId,
            Tenant = new Tenant
            {
                Id = tenantId,
                FullName = "Community Tenant",
                Slug = "community-tenant",
                TenantStatus = null!
            },
            RoleId = (int)role,
            Role = new Role
            {
                Id = (int)role,
                MasterCode = role == RoleEnum.TenantAdmin ? "tenant_admin" : "tenant_member",
                FullName = roleName,
                Scope = RoleScopeEnum.Tenant
            },
            GrantedAt = grantedAt
        };
    }

    private static TenantMemberListDto CreateListDto(TenantMember member, string userFullName, string roleName) => new()
    {
        Id = member.Id,
        UserId = member.UserId,
        UserEmail = member.User.Email,
        UserFullName = userFullName,
        TenantId = member.TenantId,
        TenantFullName = member.Tenant.FullName,
        RoleId = member.RoleId,
        RoleName = roleName,
        GrantedAt = member.GrantedAt
    };
}
