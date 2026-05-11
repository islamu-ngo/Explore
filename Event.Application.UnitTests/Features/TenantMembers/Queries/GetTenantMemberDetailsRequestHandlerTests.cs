// ABOUTME: Unit tests for GetTenantMemberDetailsRequestHandler detail-query behavior.
// ABOUTME: Covers repository detail lookup, DTO mapping, and null-result short-circuiting.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantMember;
using Explore.Application.Features.TenantMembers.Handlers.Queries;
using Explore.Application.Features.TenantMembers.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.TenantMembers.Queries;

public sealed class GetTenantMemberDetailsRequestHandlerTests
{
    private readonly ITenantMemberRepository _tenantMemberRepository = Substitute.For<ITenantMemberRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly GetTenantMemberDetailsRequestHandler _handler;

    public GetTenantMemberDetailsRequestHandlerTests()
    {
        _handler = new GetTenantMemberDetailsRequestHandler(_tenantMemberRepository, _mapper);
    }

    [Test]
    public async Task Handle_WhenMemberExists_ReturnsMappedDto()
    {
        var member = CreateTenantMember();
        var expectedDto = new TenantMemberDto
        {
            Id = member.Id,
            UserId = member.UserId,
            UserEmail = member.User.Email,
            UserFullName = "Amina Rahman",
            TenantId = member.TenantId,
            TenantFullName = member.Tenant.FullName,
            RoleId = member.RoleId,
            RoleName = member.Role.FullName,
            GrantedAt = member.GrantedAt,
            GrantedBy = member.GrantedBy,
            CreatedAt = member.CreatedAt,
            UpdatedAt = member.UpdatedAt
        };
        _tenantMemberRepository.GetMemberWithDetails(member.Id).Returns(member);
        _mapper.Map<TenantMemberDto>(member).Returns(expectedDto);

        var result = await _handler.Handle(new GetTenantMemberDetailsRequest { Id = member.Id }, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(member.Id);
        await Assert.That(result.UserEmail).IsEqualTo("amina.rahman@example.test");
        await Assert.That(result.TenantFullName).IsEqualTo("Community Tenant");
        await Assert.That(result.RoleName).IsEqualTo("Tenant Admin");
        await _tenantMemberRepository.Received(1).GetMemberWithDetails(member.Id);
        _mapper.Received(1).Map<TenantMemberDto>(member);
    }

    [Test]
    public async Task Handle_WhenMemberDoesNotExist_ReturnsNullAndSkipsMapping()
    {
        var memberId = Guid.NewGuid();
        _tenantMemberRepository.GetMemberWithDetails(memberId).Returns((TenantMember?)null);

        var result = await _handler.Handle(new GetTenantMemberDetailsRequest { Id = memberId }, CancellationToken.None);

        await Assert.That(result).IsNull();
        await _tenantMemberRepository.Received(1).GetMemberWithDetails(memberId);
        _mapper.DidNotReceive().Map<TenantMemberDto>(Arg.Any<TenantMember>());
    }

    private static TenantMember CreateTenantMember()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var grantedAt = DateTime.UtcNow.AddDays(-2);
        var createdAt = DateTime.UtcNow.AddDays(-3);

        return new TenantMember
        {
            Id = Guid.NewGuid(),
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
            TenantId = tenantId,
            Tenant = new Tenant
            {
                Id = tenantId,
                FullName = "Community Tenant",
                Slug = "community-tenant",
                TenantStatus = null!
            },
            RoleId = (int)RoleEnum.TenantAdmin,
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
