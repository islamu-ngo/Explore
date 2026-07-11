// ABOUTME: Unit tests for GetTenantUserRoleGrantDetailsRequestHandler detail-query behavior.
// ABOUTME: Covers repository detail lookup, DTO mapping, and null-result short-circuiting.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantUserRoleGrant;
using Explore.Application.Features.TenantUserRoleGrants.Handlers.Queries;
using Explore.Application.Features.TenantUserRoleGrants.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.TenantUserRoleGrants.Queries;

public sealed class GetTenantUserRoleGrantDetailsRequestHandlerTests
{
    private readonly ITenantUserRoleGrantRepository _tenantUserRoleGrantRepository = Substitute.For<ITenantUserRoleGrantRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly GetTenantUserRoleGrantDetailsRequestHandler _handler;

    public GetTenantUserRoleGrantDetailsRequestHandlerTests()
    {
        _handler = new GetTenantUserRoleGrantDetailsRequestHandler(_tenantUserRoleGrantRepository, _mapper);
    }

    [Test]
    public async Task Handle_WhenGrantExists_ReturnsMappedDto()
    {
        var grant = CreateTenantUserRoleGrant();
        var expectedDto = new TenantUserRoleGrantDto
        {
            Id = grant.Id,
            TenantUserId = grant.TenantUserId,
            UserId = grant.TenantUser.UserId,
            UserEmail = grant.TenantUser.User.Email,
            UserFullName = "Amina Rahman",
            TenantId = grant.TenantId,
            TenantFullName = grant.Tenant.FullName,
            RoleId = grant.RoleId,
            RoleName = grant.Role.FullName,
            GrantedAt = grant.GrantedAt,
            GrantedBy = grant.GrantedBy,
            CreatedAt = grant.CreatedAt,
            UpdatedAt = grant.UpdatedAt
        };
        _tenantUserRoleGrantRepository.GetGrantWithDetails(grant.Id).Returns(grant);
        _mapper.Map<TenantUserRoleGrantDto>(grant).Returns(expectedDto);

        var result = await _handler.Handle(new GetTenantUserRoleGrantDetailsRequest { Id = grant.Id }, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(grant.Id);
        await Assert.That(result.UserEmail).IsEqualTo("amina.rahman@example.test");
        await Assert.That(result.TenantFullName).IsEqualTo("Community Tenant");
        await Assert.That(result.RoleName).IsEqualTo("Tenant Admin");
        await _tenantUserRoleGrantRepository.Received(1).GetGrantWithDetails(grant.Id);
        _mapper.Received(1).Map<TenantUserRoleGrantDto>(grant);
    }

    [Test]
    public async Task Handle_WhenGrantDoesNotExist_ReturnsNullAndSkipsMapping()
    {
        var grantId = Guid.NewGuid();
        _tenantUserRoleGrantRepository.GetGrantWithDetails(grantId).Returns((TenantUserRoleGrant?)null);

        var result = await _handler.Handle(new GetTenantUserRoleGrantDetailsRequest { Id = grantId }, CancellationToken.None);

        await Assert.That(result).IsNull();
        await _tenantUserRoleGrantRepository.Received(1).GetGrantWithDetails(grantId);
        _mapper.DidNotReceive().Map<TenantUserRoleGrantDto>(Arg.Any<TenantUserRoleGrant>());
    }

    private static TenantUserRoleGrant CreateTenantUserRoleGrant()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var grantedAt = DateTime.UtcNow.AddDays(-2);
        var createdAt = DateTime.UtcNow.AddDays(-3);

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
                        Email = "amina.rahman@example.test",
                        FirstName = "Amina",
                        LastName = "Rahman"
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
            RoleId = (int)RoleEnum.TenantAdmin,
            RoleScopeId = (int)RoleScopeEnum.Tenant,
            Role = new Role
            {
                Id = (int)RoleEnum.TenantAdmin,
                MasterCode = "tenant_admin",
                FullName = "Tenant Admin",
                Scope = RoleScopeEnum.Tenant
            },
            GrantedAt = grantedAt,
            GrantedBy = Guid.NewGuid(),
            CreatedAt = createdAt
        };
    }
}
