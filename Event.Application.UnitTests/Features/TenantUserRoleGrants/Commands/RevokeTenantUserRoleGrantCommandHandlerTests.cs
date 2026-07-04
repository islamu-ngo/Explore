// ABOUTME: Unit tests for RevokeTenantUserRoleGrantCommandHandler revocation behavior.
// ABOUTME: Verifies audit field mutation and missing/already-revoked grant short-circuiting.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.TenantUserRoleGrants.Handlers.Commands;
using Explore.Application.Features.TenantUserRoleGrants.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.TenantUserRoleGrants.Commands;

public sealed class RevokeTenantUserRoleGrantCommandHandlerTests
{
    private readonly ITenantUserRoleGrantRepository _tenantUserRoleGrantRepository = Substitute.For<ITenantUserRoleGrantRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly RevokeTenantUserRoleGrantCommandHandler _handler;

    public RevokeTenantUserRoleGrantCommandHandlerTests()
    {
        _handler = new RevokeTenantUserRoleGrantCommandHandler(_tenantUserRoleGrantRepository, _currentUserService);
    }

    [Test]
    public async Task Command_CarriesTenantUserRoleGrantDeleteAuthorizationContext()
    {
        var grantId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new RevokeTenantUserRoleGrantCommand
        {
            Id = grantId,
            TenantId = tenantId
        };
        var attribute = typeof(RevokeTenantUserRoleGrantCommand)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: true)
            .Cast<AuthorizeResourceAttribute>()
            .SingleOrDefault();
        var secureRequest = (ISecureRequest)command;

        await Assert.That(attribute).IsNotNull();
        await Assert.That(attribute!.Resource).IsEqualTo(ResourceKinds.TenantUserRoleGrant);
        await Assert.That(attribute.Action).IsEqualTo(AuthorizationActions.Delete);
        await Assert.That(secureRequest.ResourceId).IsEqualTo(grantId.ToString("D"));
        await Assert.That(secureRequest.ResourceAttributes!["tenantId"]).IsEqualTo(tenantId.ToString("D"));
    }

    [Test]
    public async Task Handle_WhenGrantExists_RevokesGrantAndReturnsTrue()
    {
        var tenantUserRoleGrant = CreateTenantUserRoleGrant();
        var currentUserId = Guid.NewGuid();
        _tenantUserRoleGrantRepository.GetById(tenantUserRoleGrant.Id).Returns(tenantUserRoleGrant);
        _currentUserService.UserId.Returns(currentUserId);

        var result = await _handler.Handle(new RevokeTenantUserRoleGrantCommand { Id = tenantUserRoleGrant.Id }, CancellationToken.None);

        await Assert.That(result).IsTrue();
        await _tenantUserRoleGrantRepository.Received(1).GetById(tenantUserRoleGrant.Id);
        await _tenantUserRoleGrantRepository.Received(1).Update(Arg.Is<TenantUserRoleGrant>(grant =>
            grant.Id == tenantUserRoleGrant.Id
            && grant.RevokedAt != null
            && grant.RevokedBy == currentUserId
            && grant.UpdatedBy == currentUserId));
    }

    [Test]
    public async Task Handle_WhenGrantDoesNotExist_ReturnsFalseAndDoesNotUpdate()
    {
        var grantId = Guid.NewGuid();
        _tenantUserRoleGrantRepository.GetById(grantId).Returns((TenantUserRoleGrant?)null);

        var result = await _handler.Handle(new RevokeTenantUserRoleGrantCommand { Id = grantId }, CancellationToken.None);

        await Assert.That(result).IsFalse();
        await _tenantUserRoleGrantRepository.Received(1).GetById(grantId);
        await _tenantUserRoleGrantRepository.DidNotReceive().Update(Arg.Any<TenantUserRoleGrant>());
    }

    [Test]
    public async Task Handle_WhenGrantAlreadyRevoked_ReturnsFalseAndDoesNotUpdate()
    {
        var tenantUserRoleGrant = CreateTenantUserRoleGrant();
        tenantUserRoleGrant.RevokedAt = DateTime.UtcNow.AddDays(-1);
        _tenantUserRoleGrantRepository.GetById(tenantUserRoleGrant.Id).Returns(tenantUserRoleGrant);

        var result = await _handler.Handle(new RevokeTenantUserRoleGrantCommand { Id = tenantUserRoleGrant.Id }, CancellationToken.None);

        await Assert.That(result).IsFalse();
        await _tenantUserRoleGrantRepository.DidNotReceive().Update(Arg.Any<TenantUserRoleGrant>());
    }

    private static TenantUserRoleGrant CreateTenantUserRoleGrant() => new()
    {
        Id = Guid.NewGuid(),
        TenantUserId = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        RoleId = (int)RoleEnum.TenantMember,
        RoleScopeId = (int)RoleScopeEnum.Tenant,
        TenantUser = null!,
        Tenant = null!,
        Role = null!,
        GrantedAt = DateTime.UtcNow.AddDays(-1)
    };
}
