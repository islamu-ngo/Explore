// ABOUTME: Unit tests for GetTenantUserRoleGrantListRequestHandler list-query behavior.
// ABOUTME: Covers repository forwarding, DTO list mapping, and empty-list results.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantUserRoleGrant;
using Explore.Application.Features.TenantUserRoleGrants.Handlers.Queries;
using Explore.Application.Features.TenantUserRoleGrants.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.TenantUserRoleGrants.Queries;

public sealed class GetTenantUserRoleGrantListRequestHandlerTests
{
    private readonly ITenantUserRoleGrantRepository _tenantUserRoleGrantRepository = Substitute.For<ITenantUserRoleGrantRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly GetTenantUserRoleGrantListRequestHandler _handler;

    public GetTenantUserRoleGrantListRequestHandlerTests()
    {
        _handler = new GetTenantUserRoleGrantListRequestHandler(_tenantUserRoleGrantRepository, _mapper);
    }

    [Test]
    public async Task Handle_WhenMembersExist_ReturnsMappedDtos()
    {
        var tenantId = Guid.NewGuid();
        var grants = new List<TenantUserRoleGrant>
        {
            CreateTenantUserRoleGrant(tenantId, "amina.rahman@example.test", RoleEnum.TenantAdmin),
            CreateTenantUserRoleGrant(tenantId, "yusuf.khan@example.test", RoleEnum.TenantMember)
        };
        var expectedDtos = new List<TenantUserRoleGrantListDto>
        {
            CreateListDto(grants[0], "Amina Rahman", "Tenant Admin"),
            CreateListDto(grants[1], "Yusuf Khan", "Tenant Member")
        };
        _tenantUserRoleGrantRepository.GetGrantsWithDetails().Returns(grants);
        _mapper.Map<List<TenantUserRoleGrantListDto>>(grants).Returns(expectedDtos);

        var result = await _handler.Handle(new GetTenantUserRoleGrantListRequest(), CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].UserEmail).IsEqualTo("amina.rahman@example.test");
        await Assert.That(result[1].RoleName).IsEqualTo("Tenant Member");
        await _tenantUserRoleGrantRepository.Received(1).GetGrantsWithDetails();
        _mapper.Received(1).Map<List<TenantUserRoleGrantListDto>>(grants);
    }

    [Test]
    public async Task Handle_WhenNoMembersExist_ReturnsEmptyMappedList()
    {
        var grants = new List<TenantUserRoleGrant>();
        var expectedDtos = new List<TenantUserRoleGrantListDto>();
        _tenantUserRoleGrantRepository.GetGrantsWithDetails().Returns(grants);
        _mapper.Map<List<TenantUserRoleGrantListDto>>(grants).Returns(expectedDtos);

        var result = await _handler.Handle(new GetTenantUserRoleGrantListRequest(), CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(0);
        await _tenantUserRoleGrantRepository.Received(1).GetGrantsWithDetails();
        _mapper.Received(1).Map<List<TenantUserRoleGrantListDto>>(grants);
    }

    private static TenantUserRoleGrant CreateTenantUserRoleGrant(Guid tenantId, string email, RoleEnum role)
    {
        var userId = Guid.NewGuid();
        var grantedAt = DateTime.UtcNow.AddDays(-1);
        var roleName = role == RoleEnum.TenantAdmin ? "Tenant Admin" : "Tenant Member";

        return new TenantUserRoleGrant
        {
            Id = Guid.NewGuid(),
            TenantUserId = Guid.NewGuid(),
            TenantUser = new TenantUser
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Tenant = null!,
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
                StatusId = (int)TenantUserStatusEnum.Active
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
            RoleScopeId = (int)RoleScopeEnum.Tenant,
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

    private static TenantUserRoleGrantListDto CreateListDto(TenantUserRoleGrant grant, string userFullName, string roleName) => new()
    {
        Id = grant.Id,
        TenantUserId = grant.TenantUserId,
        UserId = grant.TenantUser.UserId,
        UserEmail = grant.TenantUser.User.Email,
        UserFullName = userFullName,
        TenantId = grant.TenantId,
        TenantFullName = grant.Tenant.FullName,
        RoleId = grant.RoleId,
        RoleName = roleName,
        GrantedAt = grant.GrantedAt
    };
}
