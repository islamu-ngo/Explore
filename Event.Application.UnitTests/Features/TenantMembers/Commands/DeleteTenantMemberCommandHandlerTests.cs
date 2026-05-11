// ABOUTME: Unit tests for DeleteTenantMemberCommandHandler delete and missing-member behavior.
// ABOUTME: Verifies the boolean response contract and repository short-circuiting.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.TenantMembers.Handlers.Commands;
using Explore.Application.Features.TenantMembers.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.TenantMembers.Commands;

public sealed class DeleteTenantMemberCommandHandlerTests
{
    private readonly ITenantMemberRepository _tenantMemberRepository = Substitute.For<ITenantMemberRepository>();
    private readonly DeleteTenantMemberCommandHandler _handler;

    public DeleteTenantMemberCommandHandlerTests()
    {
        _handler = new DeleteTenantMemberCommandHandler(_tenantMemberRepository);
    }

    [Test]
    public async Task Handle_WhenTenantMemberExists_DeletesMemberAndReturnsTrue()
    {
        var tenantMember = CreateTenantMember();
        _tenantMemberRepository.GetById(tenantMember.Id).Returns(tenantMember);

        var result = await _handler.Handle(new DeleteTenantMemberCommand { Id = tenantMember.Id }, CancellationToken.None);

        await Assert.That(result).IsTrue();
        await _tenantMemberRepository.Received(1).GetById(tenantMember.Id);
        await _tenantMemberRepository.Received(1).Delete(tenantMember);
    }

    [Test]
    public async Task Handle_WhenTenantMemberDoesNotExist_ReturnsFalseAndDoesNotDelete()
    {
        var tenantMemberId = Guid.NewGuid();
        _tenantMemberRepository.GetById(tenantMemberId).Returns((TenantMember?)null);

        var result = await _handler.Handle(new DeleteTenantMemberCommand { Id = tenantMemberId }, CancellationToken.None);

        await Assert.That(result).IsFalse();
        await _tenantMemberRepository.Received(1).GetById(tenantMemberId);
        await _tenantMemberRepository.DidNotReceive().Delete(Arg.Any<TenantMember>());
    }

    private static TenantMember CreateTenantMember() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        RoleId = (int)RoleEnum.TenantMember,
        User = null!,
        Tenant = null!,
        Role = null!
    };
}
