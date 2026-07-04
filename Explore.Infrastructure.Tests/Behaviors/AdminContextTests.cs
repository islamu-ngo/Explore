// ABOUTME: Unit tests for AdminContext instance/tenant admin resolution behavior.
// ABOUTME: Validates bootstrap fallback and tenant-role filtering used by admin authorization.

using System.Security.Claims;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Explore.Infrastructure.Tests.Behaviors;

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

        var tenantUserRoleGrantRepository = Substitute.For<ITenantUserRoleGrantRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var sut = CreateSut(CreateHttpContextAccessor(userId), platformUserRoleRepository, bootstrapRepository, tenantUserRoleGrantRepository, organizationMemberRepository);

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

        var tenantUserRoleGrantRepository = Substitute.For<ITenantUserRoleGrantRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var sut = CreateSut(CreateHttpContextAccessor(userId), platformUserRoleRepository, bootstrapRepository, tenantUserRoleGrantRepository, organizationMemberRepository);

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

        var tenantUserRoleGrantRepository = Substitute.For<ITenantUserRoleGrantRepository>();
        tenantUserRoleGrantRepository.IsTenantAdmin(PlatformDefaults.DefaultTenantId, userId).Returns(true);
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var sut = CreateSut(CreateHttpContextAccessor(userId), platformUserRoleRepository, bootstrapRepository, tenantUserRoleGrantRepository, organizationMemberRepository);

        // Act
        var result = await sut.IsInstanceAdminAsync(userId, CancellationToken.None);

        // Assert
        await Assert.That(result).IsTrue();
        await tenantUserRoleGrantRepository.Received(1).IsTenantAdmin(PlatformDefaults.DefaultTenantId, userId);
    }

    [Test]
    public async Task IsTenantAdminAsync_UsesTenantAdminRoleCheck()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var platformUserRoleRepository = Substitute.For<IPlatformUserRoleRepository>();
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        var tenantUserRoleGrantRepository = Substitute.For<ITenantUserRoleGrantRepository>();
        tenantUserRoleGrantRepository.IsTenantAdmin(tenantId, userId).Returns(true);
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();

        var sut = CreateSut(CreateHttpContextAccessor(userId), platformUserRoleRepository, bootstrapRepository, tenantUserRoleGrantRepository, organizationMemberRepository);

        // Act
        var result = await sut.IsTenantAdminAsync(tenantId, CancellationToken.None);

        // Assert
        await Assert.That(result).IsTrue();
        await tenantUserRoleGrantRepository.Received(1).IsTenantAdmin(tenantId, userId);
        await tenantUserRoleGrantRepository.DidNotReceive().HasActiveTenantUserRoleGrant(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Test]
    public async Task ResolveUserIdAsync_WithGoogleIdpAlias_UsesNormalizedExternalLoginProvider()
    {
        var userId = Guid.NewGuid();
        const string subject = "sXzmb2sFh0rG8tVveiNrP3td";

        var externalLoginRepository = Substitute.For<IUserExternalLoginRepository>();
        externalLoginRepository
            .GetByProviderAndKey(AuthSchemeNames.Google.ToLowerInvariant(), subject)
            .Returns(NewExternalLogin(userId, AuthSchemeNames.Google.ToLowerInvariant(), subject));

        var sut = CreateSut(
            CreateHttpContextAccessor(CreateExternalPrincipal(subject, idp: "google-oauth2")),
            Substitute.For<IPlatformUserRoleRepository>(),
            Substitute.For<IInstanceBootstrapStateRepository>(),
            Substitute.For<ITenantUserRoleGrantRepository>(),
            Substitute.For<IOrganizationMemberRepository>(),
            userExternalLoginRepository: externalLoginRepository);

        var result = await sut.ResolveUserIdAsync(CancellationToken.None);

        await Assert.That(result).IsEqualTo(userId);
        await externalLoginRepository.Received(1)
            .GetByProviderAndKey(AuthSchemeNames.Google.ToLowerInvariant(), subject);
    }

    [Test]
    public async Task ResolveUserIdAsync_WithAtprotoDidClaim_UsesDidProviderKey()
    {
        var userId = Guid.NewGuid();
        const string subject = "handle.example.com";
        const string did = "did:plc:example";

        var externalLoginRepository = Substitute.For<IUserExternalLoginRepository>();
        externalLoginRepository
            .GetByProviderAndKey(AuthSchemeNames.Atproto.ToLowerInvariant(), did)
            .Returns(NewExternalLogin(userId, AuthSchemeNames.Atproto.ToLowerInvariant(), did));

        var sut = CreateSut(
            CreateHttpContextAccessor(CreateExternalPrincipal(subject, idp: "atproto", did: did)),
            Substitute.For<IPlatformUserRoleRepository>(),
            Substitute.For<IInstanceBootstrapStateRepository>(),
            Substitute.For<ITenantUserRoleGrantRepository>(),
            Substitute.For<IOrganizationMemberRepository>(),
            userExternalLoginRepository: externalLoginRepository);

        var result = await sut.ResolveUserIdAsync(CancellationToken.None);

        await Assert.That(result).IsEqualTo(userId);
        await externalLoginRepository.Received(1)
            .GetByProviderAndKey(AuthSchemeNames.Atproto.ToLowerInvariant(), did);
    }

    [Test]
    public async Task ResolveUserIdAsync_WithNoProviderHints_DefaultsToKeycloakExternalLogin()
    {
        var userId = Guid.NewGuid();
        const string subject = "keycloak-subject";

        var externalLoginRepository = Substitute.For<IUserExternalLoginRepository>();
        externalLoginRepository
            .GetByProviderAndKey(AuthSchemeNames.Keycloak.ToLowerInvariant(), subject)
            .Returns(NewExternalLogin(userId, AuthSchemeNames.Keycloak.ToLowerInvariant(), subject));

        var sut = CreateSut(
            CreateHttpContextAccessor(CreateExternalPrincipal(subject)),
            Substitute.For<IPlatformUserRoleRepository>(),
            Substitute.For<IInstanceBootstrapStateRepository>(),
            Substitute.For<ITenantUserRoleGrantRepository>(),
            Substitute.For<IOrganizationMemberRepository>(),
            userExternalLoginRepository: externalLoginRepository);

        var result = await sut.ResolveUserIdAsync(CancellationToken.None);

        await Assert.That(result).IsEqualTo(userId);
        await externalLoginRepository.Received(1)
            .GetByProviderAndKey(AuthSchemeNames.Keycloak.ToLowerInvariant(), subject);
    }

    [Test]
    public async Task ResolveUserIdAsync_WithVerifiedGoogleEmail_UsesEmailFallbackWhenExternalLoginMissing()
    {
        var userId = Guid.NewGuid();
        const string subject = "google-subject";
        const string email = "USER@Example.COM";

        var externalLoginRepository = Substitute.For<IUserExternalLoginRepository>();
        externalLoginRepository
            .GetByProviderAndKey(AuthSchemeNames.Google.ToLowerInvariant(), subject)
            .Returns((UserExternalLogin?)null);

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserByEmail("user@example.com").Returns(NewUser(userId, "user@example.com"));

        var sut = CreateSut(
            CreateHttpContextAccessor(CreateExternalPrincipal(
                subject,
                issuer: "https://accounts.google.com",
                email: email,
                emailVerified: "true")),
            Substitute.For<IPlatformUserRoleRepository>(),
            Substitute.For<IInstanceBootstrapStateRepository>(),
            Substitute.For<ITenantUserRoleGrantRepository>(),
            Substitute.For<IOrganizationMemberRepository>(),
            userExternalLoginRepository: externalLoginRepository,
            userRepository: userRepository);

        var result = await sut.ResolveUserIdAsync(CancellationToken.None);

        await Assert.That(result).IsEqualTo(userId);
        await userRepository.Received(1).GetUserByEmail("user@example.com");
    }

    [Test]
    public async Task ResolveUserIdAsync_WithUnverifiedGoogleEmail_DoesNotUseEmailFallback()
    {
        const string subject = "google-subject";

        var externalLoginRepository = Substitute.For<IUserExternalLoginRepository>();
        externalLoginRepository
            .GetByProviderAndKey(AuthSchemeNames.Google.ToLowerInvariant(), subject)
            .Returns((UserExternalLogin?)null);

        var userRepository = Substitute.For<IUserRepository>();
        var sut = CreateSut(
            CreateHttpContextAccessor(CreateExternalPrincipal(
                subject,
                idp: "google",
                email: "user@example.com",
                emailVerified: "false")),
            Substitute.For<IPlatformUserRoleRepository>(),
            Substitute.For<IInstanceBootstrapStateRepository>(),
            Substitute.For<ITenantUserRoleGrantRepository>(),
            Substitute.For<IOrganizationMemberRepository>(),
            userExternalLoginRepository: externalLoginRepository,
            userRepository: userRepository);

        var result = await sut.ResolveUserIdAsync(CancellationToken.None);

        await Assert.That(result).IsNull();
        await userRepository.DidNotReceive().GetUserByEmail(Arg.Any<string>());
    }

    [Test]
    public async Task IsGroupAdminAsync_WhenMembershipHasGroupAdminRole_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var platformUserRoleRepository = Substitute.For<IPlatformUserRoleRepository>();
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        var tenantUserRoleGrantRepository = Substitute.For<ITenantUserRoleGrantRepository>();
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
            tenantUserRoleGrantRepository,
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
        var tenantUserRoleGrantRepository = Substitute.For<ITenantUserRoleGrantRepository>();
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
            tenantUserRoleGrantRepository,
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
        var tenantUserRoleGrantRepository = Substitute.For<ITenantUserRoleGrantRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();

        groupMemberRepository.GetByGroupAndUser(groupId, userId).Returns((GroupMember?)null);

        var sut = CreateSutWithGroupMembers(
            CreateHttpContextAccessor(userId),
            platformUserRoleRepository,
            bootstrapRepository,
            tenantUserRoleGrantRepository,
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
        var tenantUserRoleGrantRepository = Substitute.For<ITenantUserRoleGrantRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) });

        var sut = CreateSutWithGroupMembers(
            httpContextAccessor,
            platformUserRoleRepository,
            bootstrapRepository,
            tenantUserRoleGrantRepository,
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
        var tenantUserRoleGrantRepository = Substitute.For<ITenantUserRoleGrantRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();
        groupMemberRepository.GetMembershipsByUser(userId).Returns(memberships);

        var sut = CreateSutWithGroupMembers(
            CreateHttpContextAccessor(userId),
            platformUserRoleRepository,
            bootstrapRepository,
            tenantUserRoleGrantRepository,
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
        ITenantUserRoleGrantRepository tenantUserRoleGrantRepository,
        IOrganizationMemberRepository organizationMemberRepository,
        IGroupMemberRepository groupMemberRepository)
    {
        var userExternalLoginRepository = Substitute.For<IUserExternalLoginRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var deploymentModeProvider = Substitute.For<IDeploymentModeProvider>();
        var logger = Substitute.For<ILogger<AdminContext>>();

        return new AdminContext(
            httpContextAccessor,
            platformUserRoleRepository,
            instanceBootstrapStateRepository,
            tenantUserRoleGrantRepository,
            organizationMemberRepository,
            groupMemberRepository,
            userExternalLoginRepository,
            userRepository,
            cache,
            deploymentModeProvider,
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

        var memberships = new List<TenantUserRoleGrant>
        {
            NewGrant(userId, adminTenantId, RoleEnum.TenantAdmin),
            NewGrant(userId, ownerTenantId, RoleEnum.TenantAdmin),
            NewGrant(userId, memberTenantId, RoleEnum.TenantMember)
        };

        var platformUserRoleRepository = Substitute.For<IPlatformUserRoleRepository>();
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        var tenantUserRoleGrantRepository = Substitute.For<ITenantUserRoleGrantRepository>();
        tenantUserRoleGrantRepository.GetByUserId(userId).Returns(memberships);
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();

        var sut = CreateSut(CreateHttpContextAccessor(userId), platformUserRoleRepository, bootstrapRepository, tenantUserRoleGrantRepository, organizationMemberRepository);

        // Act
        var result = await sut.GetAdminTenantIdsAsync(userId, CancellationToken.None);

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result).Contains(adminTenantId);
        await Assert.That(result).Contains(ownerTenantId);
        await Assert.That(result).DoesNotContain(memberTenantId);
    }

    private static TenantUserRoleGrant NewGrant(Guid userId, Guid tenantId, RoleEnum role) => new()
    {
        Id = Guid.NewGuid(),
        TenantUserId = Guid.NewGuid(),
        TenantUser = new TenantUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Tenant = null!,
            UserId = userId,
            User = null!,
            StatusId = (int)TenantUserStatusEnum.Active
        },
        TenantId = tenantId,
        Tenant = null!,
        RoleId = (int)role,
        RoleScopeId = (int)RoleScopeEnum.Tenant,
        Role = null!
    };

    private static AdminContext CreateSut(
        IHttpContextAccessor httpContextAccessor,
        IPlatformUserRoleRepository platformUserRoleRepository,
        IInstanceBootstrapStateRepository instanceBootstrapStateRepository,
        ITenantUserRoleGrantRepository tenantUserRoleGrantRepository,
        IOrganizationMemberRepository organizationMemberRepository,
        IUserExternalLoginRepository? userExternalLoginRepository = null,
        IUserRepository? userRepository = null)
    {
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var deploymentModeProvider = Substitute.For<IDeploymentModeProvider>();
        var logger = Substitute.For<ILogger<AdminContext>>();

        return new AdminContext(
            httpContextAccessor,
            platformUserRoleRepository,
            instanceBootstrapStateRepository,
            tenantUserRoleGrantRepository,
            organizationMemberRepository,
            groupMemberRepository,
            userExternalLoginRepository ?? Substitute.For<IUserExternalLoginRepository>(),
            userRepository ?? Substitute.For<IUserRepository>(),
            cache,
            deploymentModeProvider,
            logger);
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(ClaimsPrincipal principal)
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        httpContextAccessor.HttpContext.Returns(httpContext);
        return httpContextAccessor;
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(Guid userId)
    {
        return CreateHttpContextAccessor(CreatePrincipal(userId));
    }

    private static ClaimsPrincipal CreatePrincipal(Guid userId)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("internal_user_id", userId.ToString()),
            new Claim("sub", userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        ], "TestAuth");

        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal CreateExternalPrincipal(
        string subject,
        string? idp = null,
        string? issuer = null,
        string? did = null,
        string? email = null,
        string? emailVerified = null)
    {
        var claims = new List<Claim> { new("sub", subject) };
        if (!string.IsNullOrWhiteSpace(idp))
            claims.Add(new Claim("idp", idp));

        if (!string.IsNullOrWhiteSpace(issuer))
            claims.Add(new Claim("iss", issuer));

        if (!string.IsNullOrWhiteSpace(did))
            claims.Add(new Claim("did", did));

        if (!string.IsNullOrWhiteSpace(email))
            claims.Add(new Claim("email", email));

        if (!string.IsNullOrWhiteSpace(emailVerified))
            claims.Add(new Claim("email_verified", emailVerified));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private static UserExternalLogin NewExternalLogin(Guid userId, string provider, string providerKey)
    {
        return new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            User = NewUser(userId, "user@example.com"),
            TenantId = Guid.NewGuid(),
            Tenant = null!,
            Provider = provider,
            ProviderKey = providerKey,
            ProviderDisplayName = provider
        };
    }

    private static User NewUser(Guid userId, string email)
    {
        return new User
        {
            Id = userId,
            Pii = new UserPii
            {
                UserId = userId,
                Email = email,
                FirstName = "Test",
                LastName = "User"
            }
        };
    }
}
