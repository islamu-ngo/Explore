// ABOUTME: Unit tests for AdminContext instance/tenant admin resolution behavior.
// ABOUTME: Validates bootstrap fallback and tenant-role filtering used by admin authorization.

using System.Security.Claims;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Event.Application.UnitTests.Behaviors;

public class AdminContextTests
{
    [Test]
    public async Task IsInstanceAdminAsync_WhenNoPlatformUserRoleAndBootstrapCompletedByUser_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var platformUserRoleRepository = Substitute.For<IPlatformUserRoleRepository>();
        platformUserRoleRepository.IsUserPlatformAdmin(userId).Returns(false);

        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        bootstrapRepository.GetCurrent().Returns(new InstanceBootstrapState
        {
            IsCompleted = true,
            CompletedByUserId = userId
        });

        var tenantMemberRepository = Substitute.For<ITenantMemberRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var sut = CreateSut(CreateHttpContextAccessor(userId), platformUserRoleRepository, bootstrapRepository, tenantMemberRepository, organizationMemberRepository);

        // Act
        var result = await sut.IsInstanceAdminAsync(userId, CancellationToken.None);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsInstanceAdminAsync_WhenPlatformUserRoleLookupThrows_UsesBootstrapFallback()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var platformUserRoleRepository = Substitute.For<IPlatformUserRoleRepository>();
        platformUserRoleRepository.IsUserPlatformAdmin(userId)
            .ThrowsAsync(new InvalidOperationException("relation \"PlatformUserRoles\" does not exist"));

        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        bootstrapRepository.GetCurrent().Returns(new InstanceBootstrapState
        {
            IsCompleted = true,
            CompletedByUserId = userId
        });

        var tenantMemberRepository = Substitute.For<ITenantMemberRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var sut = CreateSut(CreateHttpContextAccessor(userId), platformUserRoleRepository, bootstrapRepository, tenantMemberRepository, organizationMemberRepository);

        // Act
        var result = await sut.IsInstanceAdminAsync(userId, CancellationToken.None);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsInstanceAdminAsync_WhenBootstrapOwnerMissing_AndUserIsDefaultTenantAdmin_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var platformUserRoleRepository = Substitute.For<IPlatformUserRoleRepository>();
        platformUserRoleRepository.IsUserPlatformAdmin(userId).Returns(false);

        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        bootstrapRepository.GetCurrent().Returns(new InstanceBootstrapState
        {
            IsCompleted = true,
            CompletedByUserId = null
        });

        var tenantMemberRepository = Substitute.For<ITenantMemberRepository>();
        tenantMemberRepository.IsTenantAdmin(PlatformDefaults.DefaultTenantId, userId).Returns(true);
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var sut = CreateSut(CreateHttpContextAccessor(userId), platformUserRoleRepository, bootstrapRepository, tenantMemberRepository, organizationMemberRepository);

        // Act
        var result = await sut.IsInstanceAdminAsync(userId, CancellationToken.None);

        // Assert
        await Assert.That(result).IsTrue();
        await tenantMemberRepository.Received(1).IsTenantAdmin(PlatformDefaults.DefaultTenantId, userId);
    }

    [Test]
    public async Task IsTenantAdminAsync_UsesTenantAdminRoleCheck()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var platformUserRoleRepository = Substitute.For<IPlatformUserRoleRepository>();
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        var tenantMemberRepository = Substitute.For<ITenantMemberRepository>();
        tenantMemberRepository.IsTenantAdmin(tenantId, userId).Returns(true);
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();

        var sut = CreateSut(CreateHttpContextAccessor(userId), platformUserRoleRepository, bootstrapRepository, tenantMemberRepository, organizationMemberRepository);

        // Act
        var result = await sut.IsTenantAdminAsync(tenantId, CancellationToken.None);

        // Assert
        await Assert.That(result).IsTrue();
        await tenantMemberRepository.Received(1).IsTenantAdmin(tenantId, userId);
        await tenantMemberRepository.DidNotReceive().IsTenantMember(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Test]
    public async Task IsGroupAdminAsync_WhenMembershipHasGroupAdminRole_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var platformUserRoleRepository = Substitute.For<IPlatformUserRoleRepository>();
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        var tenantMemberRepository = Substitute.For<ITenantMemberRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();

        groupMemberRepository.GetByGroupAndUser(groupId, userId).Returns(new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            Group = null!,
            UserId = userId,
            User = null!,
            RoleId = (int)RoleEnum.GroupAdmin,
            Role = null!,
            TenantId = Guid.NewGuid(),
            Tenant = null!
        });

        var sut = CreateSutWithGroupMembers(
            CreateHttpContextAccessor(userId),
            platformUserRoleRepository,
            bootstrapRepository,
            tenantMemberRepository,
            organizationMemberRepository,
            groupMemberRepository);

        // Act
        var result = await sut.IsGroupAdminAsync(groupId, CancellationToken.None);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsGroupAdminAsync_WhenMembershipHasNonAdminRole_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var platformUserRoleRepository = Substitute.For<IPlatformUserRoleRepository>();
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        var tenantMemberRepository = Substitute.For<ITenantMemberRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();

        groupMemberRepository.GetByGroupAndUser(groupId, userId).Returns(new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            Group = null!,
            UserId = userId,
            User = null!,
            RoleId = (int)RoleEnum.GroupMember,
            Role = null!,
            TenantId = Guid.NewGuid(),
            Tenant = null!
        });

        var sut = CreateSutWithGroupMembers(
            CreateHttpContextAccessor(userId),
            platformUserRoleRepository,
            bootstrapRepository,
            tenantMemberRepository,
            organizationMemberRepository,
            groupMemberRepository);

        // Act
        var result = await sut.IsGroupAdminAsync(groupId, CancellationToken.None);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsGroupAdminAsync_WhenNoMembership_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var platformUserRoleRepository = Substitute.For<IPlatformUserRoleRepository>();
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        var tenantMemberRepository = Substitute.For<ITenantMemberRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();

        groupMemberRepository.GetByGroupAndUser(groupId, userId).Returns((GroupMember?)null);

        var sut = CreateSutWithGroupMembers(
            CreateHttpContextAccessor(userId),
            platformUserRoleRepository,
            bootstrapRepository,
            tenantMemberRepository,
            organizationMemberRepository,
            groupMemberRepository);

        // Act
        var result = await sut.IsGroupAdminAsync(groupId, CancellationToken.None);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsGroupAdminAsync_WithNoAuthenticatedUser_ReturnsFalse()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        var platformUserRoleRepository = Substitute.For<IPlatformUserRoleRepository>();
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        var tenantMemberRepository = Substitute.For<ITenantMemberRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) });

        var sut = CreateSutWithGroupMembers(
            httpContextAccessor,
            platformUserRoleRepository,
            bootstrapRepository,
            tenantMemberRepository,
            organizationMemberRepository,
            groupMemberRepository);

        // Act
        var result = await sut.IsGroupAdminAsync(groupId, CancellationToken.None);

        // Assert
        await Assert.That(result).IsFalse();
        await groupMemberRepository.DidNotReceive().GetByGroupAndUser(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Test]
    public async Task GetAdminGroupIdsAsync_ForUserId_FiltersNonAdminMemberships()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminGroupId = Guid.NewGuid();
        var memberGroupId = Guid.NewGuid();

        var tenantId = Guid.NewGuid();
        var memberships = new List<GroupMember>
        {
            new()
            {
                Id = Guid.NewGuid(),
                GroupId = adminGroupId,
                Group = null!,
                UserId = userId,
                User = null!,
                RoleId = (int)RoleEnum.GroupAdmin,
                Role = null!,
                TenantId = tenantId,
                Tenant = null!
            },
            new()
            {
                Id = Guid.NewGuid(),
                GroupId = memberGroupId,
                Group = null!,
                UserId = userId,
                User = null!,
                RoleId = (int)RoleEnum.GroupMember,
                Role = null!,
                TenantId = tenantId,
                Tenant = null!
            }
        };

        var platformUserRoleRepository = Substitute.For<IPlatformUserRoleRepository>();
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        var tenantMemberRepository = Substitute.For<ITenantMemberRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();
        groupMemberRepository.GetMembershipsByUser(userId).Returns(memberships);

        var sut = CreateSutWithGroupMembers(
            CreateHttpContextAccessor(userId),
            platformUserRoleRepository,
            bootstrapRepository,
            tenantMemberRepository,
            organizationMemberRepository,
            groupMemberRepository);

        // Act
        var result = await sut.GetAdminGroupIdsAsync(userId, CancellationToken.None);

        // Assert
        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result).Contains(adminGroupId);
        await Assert.That(result).DoesNotContain(memberGroupId);
    }

    private static AdminContext CreateSutWithGroupMembers(
        IHttpContextAccessor httpContextAccessor,
        IPlatformUserRoleRepository platformUserRoleRepository,
        IInstanceBootstrapStateRepository instanceBootstrapStateRepository,
        ITenantMemberRepository tenantMemberRepository,
        IOrganizationMemberRepository organizationMemberRepository,
        IGroupMemberRepository groupMemberRepository)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = Substitute.For<ILogger<AdminContext>>();

        return new AdminContext(
            httpContextAccessor,
            platformUserRoleRepository,
            instanceBootstrapStateRepository,
            tenantMemberRepository,
            organizationMemberRepository,
            groupMemberRepository,
            cache,
            logger);
    }

    [Test]
    public async Task GetAdminTenantIdsAsync_FiltersOutNonAdminMemberships()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminTenantId = Guid.NewGuid();
        var ownerTenantId = Guid.NewGuid();
        var memberTenantId = Guid.NewGuid();

        var memberships = new List<TenantMember>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                User = null!,
                TenantId = adminTenantId,
                Tenant = null!,
                RoleId = (int)RoleEnum.TenantAdmin,
                Role = null!
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                User = null!,
                TenantId = ownerTenantId,
                Tenant = null!,
                RoleId = (int)RoleEnum.TenantAdmin,
                Role = null!
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                User = null!,
                TenantId = memberTenantId,
                Tenant = null!,
                RoleId = (int)RoleEnum.TenantMember,
                Role = null!
            }
        };

        var platformUserRoleRepository = Substitute.For<IPlatformUserRoleRepository>();
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        var tenantMemberRepository = Substitute.For<ITenantMemberRepository>();
        tenantMemberRepository.GetByUserId(userId).Returns(memberships);
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();

        var sut = CreateSut(CreateHttpContextAccessor(userId), platformUserRoleRepository, bootstrapRepository, tenantMemberRepository, organizationMemberRepository);

        // Act
        var result = await sut.GetAdminTenantIdsAsync(userId, CancellationToken.None);

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result).Contains(adminTenantId);
        await Assert.That(result).Contains(ownerTenantId);
        await Assert.That(result).DoesNotContain(memberTenantId);
    }

    private static AdminContext CreateSut(
        IHttpContextAccessor httpContextAccessor,
        IPlatformUserRoleRepository platformUserRoleRepository,
        IInstanceBootstrapStateRepository instanceBootstrapStateRepository,
        ITenantMemberRepository tenantMemberRepository,
        IOrganizationMemberRepository organizationMemberRepository)
    {
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = Substitute.For<ILogger<AdminContext>>();

        return new AdminContext(
            httpContextAccessor,
            platformUserRoleRepository,
            instanceBootstrapStateRepository,
            tenantMemberRepository,
            organizationMemberRepository,
            groupMemberRepository,
            cache,
            logger);
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(Guid userId)
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext
        {
            User = CreatePrincipal(userId)
        };

        httpContextAccessor.HttpContext.Returns(httpContext);
        return httpContextAccessor;
    }

    private static ClaimsPrincipal CreatePrincipal(Guid userId)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        ], "TestAuth");

        return new ClaimsPrincipal(identity);
    }
}
