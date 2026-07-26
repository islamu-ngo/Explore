// ABOUTME: Unit tests for server-side AI acting-actor authorization context resolution.
// ABOUTME: Verifies user, organization-member, and group-member actor eligibility before AI writes persist.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.AiAssistant.Actors;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.AiAssistant.Actors;

public sealed class AiAssistantActorContextServiceTests
{
    private readonly IActorRepository _actorRepository = Substitute.For<IActorRepository>();
    private readonly ITenantUserRepository _tenantUserRepository = Substitute.For<ITenantUserRepository>();
    private readonly IOrganizationMemberRepository _organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
    private readonly IGroupMemberRepository _groupMemberRepository = Substitute.For<IGroupMemberRepository>();

    [Test]
    public async Task ListAuthorizedActorContextsAsync_ReturnsUserAndAllowedMembershipActorsOnly()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var userActorId = Guid.CreateVersion7();
        var allowedOrganizationId = Guid.CreateVersion7();
        var allowedOrganizationActorId = Guid.CreateVersion7();
        var blockedOrganizationId = Guid.CreateVersion7();
        var blockedOrganizationActorId = Guid.CreateVersion7();
        var allowedGroupId = Guid.CreateVersion7();
        var allowedGroupActorId = Guid.CreateVersion7();
        var blockedGroupId = Guid.CreateVersion7();
        var blockedGroupActorId = Guid.CreateVersion7();
        _actorRepository.GetActorByUserIdAndTenantId(userId, tenantId, Arg.Any<CancellationToken>())
            .Returns(CreateUserActor(userActorId, tenantId, userId));
        _organizationMemberRepository.GetOrganizationIdsWhereUserHasPermission(
                userId,
                PermissionCodes.EventCreate,
                Arg.Any<CancellationToken>())
            .Returns([allowedOrganizationId]);
        _organizationMemberRepository.GetMembershipsByUser(userId, Arg.Any<CancellationToken>()).Returns(
        [
            CreateOrganizationMembership(tenantId, allowedOrganizationId, allowedOrganizationActorId, "Allowed Org"),
            CreateOrganizationMembership(tenantId, blockedOrganizationId, blockedOrganizationActorId, "Blocked Org")
        ]);
        _groupMemberRepository.GetGroupIdsWhereUserHasPermission(
                userId,
                PermissionCodes.EventCreate,
                Arg.Any<CancellationToken>())
            .Returns([allowedGroupId]);
        _groupMemberRepository.GetMembershipsByUser(userId, Arg.Any<CancellationToken>()).Returns(
        [
            CreateGroupMembership(tenantId, allowedGroupId, allowedGroupActorId, "Allowed Group"),
            CreateGroupMembership(tenantId, blockedGroupId, blockedGroupActorId, "Blocked Group")
        ]);

        var contexts = await CreateService().ListAuthorizedActorContextsAsync(tenantId, userId, CancellationToken.None);

        await Assert.That(contexts.Select(context => context.ActorId)).IsEquivalentTo(
        [
            userActorId,
            allowedOrganizationActorId,
            allowedGroupActorId
        ]);
        await Assert.That(contexts.Select(context => context.ActorId)).DoesNotContain(blockedOrganizationActorId);
        await Assert.That(contexts.Select(context => context.ActorId)).DoesNotContain(blockedGroupActorId);
    }

    [Test]
    public async Task ResolveAuthorizedActorAsync_WhenActorIsMissing_DefaultsToFirstAuthorizedActor()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        _actorRepository.GetActorByUserIdAndTenantId(userId, tenantId, Arg.Any<CancellationToken>())
            .Returns(CreateUserActor(actorId, tenantId, userId));
        ConfigureEmptyMemberships(userId);

        var result = await CreateService().ResolveAuthorizedActorAsync(tenantId, userId, null, CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.ActorId).IsEqualTo(actorId);
    }

    [Test]
    public async Task ResolveAuthorizedActorAsync_WhenActorIsNotAuthorized_ReturnsFailure()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        _actorRepository.GetActorByUserIdAndTenantId(userId, tenantId, Arg.Any<CancellationToken>())
            .Returns(CreateUserActor(actorId, tenantId, userId));
        ConfigureEmptyMemberships(userId);

        var result = await CreateService().ResolveAuthorizedActorAsync(
            tenantId,
            userId,
            Guid.CreateVersion7(),
            CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("actor_context_not_authorized");
    }

    [Test]
    public async Task ListAuthorizedActorContextsAsync_ForwardsCancellationTokenToEveryRepository()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        using var cancellation = new CancellationTokenSource();
        CancellationToken token = cancellation.Token;
        ConfigureEmptyMemberships(userId);

        await CreateService().ListAuthorizedActorContextsAsync(tenantId, userId, token);

        await _tenantUserRepository.Received(1).GetByTenantAndUserAsync(tenantId, userId, token);
        await _actorRepository.Received(1).GetActorByUserIdAndTenantId(userId, tenantId, token);
        await _organizationMemberRepository.Received(1)
            .GetOrganizationIdsWhereUserHasPermission(userId, PermissionCodes.EventCreate, token);
        await _organizationMemberRepository.Received(1).GetMembershipsByUser(userId, token);
        await _groupMemberRepository.Received(1)
            .GetGroupIdsWhereUserHasPermission(userId, PermissionCodes.EventCreate, token);
        await _groupMemberRepository.Received(1).GetMembershipsByUser(userId, token);
    }

    private AiAssistantActorContextService CreateService()
        => new(
            _actorRepository,
            _tenantUserRepository,
            _organizationMemberRepository,
            _groupMemberRepository);

    private void ConfigureEmptyMemberships(Guid userId)
    {
        _organizationMemberRepository.GetOrganizationIdsWhereUserHasPermission(
                userId,
                PermissionCodes.EventCreate,
                Arg.Any<CancellationToken>())
            .Returns([]);
        _organizationMemberRepository.GetMembershipsByUser(userId, Arg.Any<CancellationToken>()).Returns([]);
        _groupMemberRepository.GetGroupIdsWhereUserHasPermission(
                userId,
                PermissionCodes.EventCreate,
                Arg.Any<CancellationToken>())
            .Returns([]);
        _groupMemberRepository.GetMembershipsByUser(userId, Arg.Any<CancellationToken>()).Returns([]);
    }

    private static Actor CreateUserActor(Guid actorId, Guid tenantId, Guid userId)
        => new()
        {
            Id = actorId,
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            UserId = userId,
            Pii = new ActorPii { ActorId = actorId, DisplayName = "Amina Yusuf" }
        };

    private static OrganizationMember CreateOrganizationMembership(
        Guid tenantId,
        Guid organizationId,
        Guid actorId,
        string name)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Tenant = null!,
            OrganizationTenantId = Guid.NewGuid(),
            OrganizationTenant = new OrganizationTenant
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Tenant = null!,
                OrganizationId = organizationId,
                ApprovalStatus = null!,
                Organization = new Organization
                {
                    Id = organizationId,
                    Pii = new OrganizationPii { FullName = name },
                    Actor = new Actor
                    {
                        Id = actorId,
                        ActorTypeId = (int)ActorTypeEnum.Organization,
                        ActorType = null!,
                        OrganizationId = organizationId,
                        Pii = new ActorPii { ActorId = actorId, DisplayName = name }
                    }
                }
            },
            UserId = Guid.NewGuid(),
            User = null!,
            Role = null!
        };

    private static GroupMember CreateGroupMembership(Guid tenantId, Guid groupId, Guid actorId, string name)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Tenant = null!,
            GroupTenantId = Guid.NewGuid(),
            GroupTenant = new GroupTenant
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Tenant = null!,
                GroupId = groupId,
                ApprovalStatus = null!,
                Group = new Group
                {
                    Id = groupId,
                    FullName = name,
                    Actor = new Actor
                    {
                        Id = actorId,
                        ActorTypeId = (int)ActorTypeEnum.Group,
                        ActorType = null!,
                        GroupId = groupId,
                        Pii = new ActorPii { ActorId = actorId, DisplayName = name }
                    }
                }
            },
            UserId = Guid.NewGuid(),
            User = null!,
            Role = null!
        };
}
