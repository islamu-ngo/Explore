// ABOUTME: Verifies event creation resolves only globally active, tenant-eligible managed actors.
// ABOUTME: Covers personal, organization, group, and external actor eligibility after authority checks.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

[Category("EventActorEligibility")]
public sealed class EventActorResolverTests
{
    [Test]
    public async Task ResolveAsync_ActivePersonalActorWithActiveTenantUser_IsResolved()
    {
        var userId = Guid.CreateVersion7();

        var result = await ResolveAsync(ActorForUser(userId), ActorResolutionPath.User);

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task ResolveAsync_PersonalActorWithInactiveTenantUser_IsRejected()
    {
        var userId = Guid.CreateVersion7();

        var result = await ResolveAsync(
            ActorForUser(userId),
            ActorResolutionPath.User,
            activeTenantUser: false);

        await Assert.That(result.Succeeded).IsFalse();
    }

    [Test]
    [Arguments(true, false)]
    [Arguments(false, true)]
    public async Task ResolveAsync_SuspendedOrDeletedActor_IsRejected(bool suspended, bool deleted)
    {
        var actor = ActorForUser(Guid.CreateVersion7());
        actor.IsSuspended = suspended;
        actor.IsDeleted = deleted;

        var result = await ResolveAsync(actor, ActorResolutionPath.User);

        await Assert.That(result.Succeeded).IsFalse();
    }

    [Test]
    [Arguments(ApprovalStatusEnum.Pending, true, false, false)]
    [Arguments(ApprovalStatusEnum.Approved, false, false, false)]
    [Arguments(ApprovalStatusEnum.Approved, true, true, false)]
    [Arguments(ApprovalStatusEnum.Approved, true, false, true)]
    public async Task ResolveAsync_OrganizationActorWithIneligibleParticipation_IsRejected(
        ApprovalStatusEnum approvalStatus,
        bool organizerEligible,
        bool suspended,
        bool deleted)
    {
        var organizationId = Guid.CreateVersion7();
        var participation = OrganizationParticipation(organizationId, approvalStatus, organizerEligible, suspended, deleted);

        var result = await ResolveAsync(
            ActorForOrganization(organizationId),
            ActorResolutionPath.Organization,
            organizationParticipation: participation);

        await Assert.That(result.Succeeded).IsFalse();
    }

    [Test]
    [Arguments(ApprovalStatusEnum.Pending, true, false, false)]
    [Arguments(ApprovalStatusEnum.Approved, false, false, false)]
    [Arguments(ApprovalStatusEnum.Approved, true, true, false)]
    [Arguments(ApprovalStatusEnum.Approved, true, false, true)]
    public async Task ResolveAsync_GroupActorWithIneligibleParticipation_IsRejected(
        ApprovalStatusEnum approvalStatus,
        bool organizerEligible,
        bool suspended,
        bool deleted)
    {
        var groupId = Guid.CreateVersion7();
        var participation = GroupParticipation(groupId, approvalStatus, organizerEligible, suspended, deleted);

        var result = await ResolveAsync(
            ActorForGroup(groupId),
            ActorResolutionPath.Group,
            groupParticipation: participation);

        await Assert.That(result.Succeeded).IsFalse();
    }

    [Test]
    public async Task ResolveAsync_ApprovedEligibleOrganizationActor_IsResolvedWithoutPublicVisibility()
    {
        var organizationId = Guid.CreateVersion7();
        var participation = OrganizationParticipation(
            organizationId,
            ApprovalStatusEnum.Approved,
            organizerEligible: true,
            suspended: false,
            deleted: false);

        var result = await ResolveAsync(
            ActorForOrganization(organizationId),
            ActorResolutionPath.Organization,
            organizationParticipation: participation);

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task ResolveAsync_ApprovedEligibleGroupActor_IsResolvedWithoutPublicVisibility()
    {
        var groupId = Guid.CreateVersion7();
        var participation = GroupParticipation(
            groupId,
            ApprovalStatusEnum.Approved,
            organizerEligible: true,
            suspended: false,
            deleted: false);

        var result = await ResolveAsync(
            ActorForGroup(groupId),
            ActorResolutionPath.Group,
            groupParticipation: participation);

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task ResolveAsync_ObservedExternalActor_IsRejected()
    {
        var actor = new Actor
        {
            Id = Guid.CreateVersion7(),
            ActorTypeId = (int)ActorTypeEnum.ExternalUnclassified,
            ActorType = null!,
            ExternalActorSubjectId = Guid.CreateVersion7(),
            Pii = new ActorPii { DisplayName = "Observed external actor" }
        };

        var result = await ResolveAsync(actor, ActorResolutionPath.User);

        await Assert.That(result.Succeeded).IsFalse();
    }

    private static async Task<EventActorResult> ResolveAsync(
        Actor actor,
        ActorResolutionPath resolutionPath,
        bool activeTenantUser = true,
        OrganizationTenant? organizationParticipation = null,
        GroupTenant? groupParticipation = null)
    {
        var tenantId = organizationParticipation?.TenantId
            ?? groupParticipation?.TenantId
            ?? Guid.CreateVersion7();
        var actorRepository = Substitute.For<IActorRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();
        var tenantUserRepository = Substitute.For<ITenantUserRepository>();
        var organizationTenantRepository = Substitute.For<IOrganizationTenantRepository>();
        var groupTenantRepository = Substitute.For<IGroupTenantRepository>();
        var settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        settingsResolver.ResolveAsync<bool>(
                "events.user_submission_enabled",
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);
        var resolver = new EventActorResolver(
            actorRepository,
            organizationMemberRepository,
            groupMemberRepository,
            settingsResolver,
            tenantContext,
            tenantUserRepository,
            organizationTenantRepository,
            groupTenantRepository);

        var currentUserId = actor.UserId ?? Guid.CreateVersion7();
        switch (resolutionPath)
        {
            case ActorResolutionPath.User:
                actorRepository.GetActorByUserId(currentUserId).Returns(actor);
                if (actor.UserId is { } userId)
                {
                    tenantUserRepository.IsActiveTenantUserAsync(tenantId, userId, Arg.Any<CancellationToken>())
                        .Returns(activeTenantUser);
                }
                break;
            case ActorResolutionPath.Organization:
                organizationMemberRepository.HasPermissionInOrganization(
                        Arg.Any<Guid>(),
                        currentUserId,
                        PermissionCodes.EventCreate)
                    .Returns(true);
                actorRepository.GetActorByOrganizationId(Arg.Any<Guid>()).Returns(actor);
                if (actor.OrganizationId is { } organizationId)
                {
                    organizationTenantRepository.GetByOrganizationAndTenant(
                            organizationId,
                            tenantId,
                            Arg.Any<CancellationToken>())
                        .Returns(organizationParticipation);
                }
                break;
            case ActorResolutionPath.Group:
                groupMemberRepository.HasPermissionInGroup(
                        Arg.Any<Guid>(),
                        currentUserId,
                        PermissionCodes.EventCreate)
                    .Returns(true);
                actorRepository.GetActorByGroupId(Arg.Any<Guid>()).Returns(actor);
                if (actor.GroupId is { } groupId)
                {
                    groupTenantRepository.GetByGroupAndTenant(groupId, tenantId, Arg.Any<CancellationToken>())
                        .Returns(groupParticipation);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(resolutionPath), resolutionPath, null);
        }

        return await resolver.ResolveAsync(
            currentUserId,
            resolutionPath == ActorResolutionPath.Organization ? actor.OrganizationId : null,
            resolutionPath == ActorResolutionPath.Group ? actor.GroupId : null,
            CancellationToken.None);
    }

    private static Actor ActorForUser(Guid userId) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        ActorTypeId = (int)ActorTypeEnum.User,
        ActorType = null!,
        Pii = new ActorPii { DisplayName = "Personal publisher" }
    };

    private static Actor ActorForOrganization(Guid organizationId) => new()
    {
        Id = Guid.CreateVersion7(),
        OrganizationId = organizationId,
        ActorTypeId = (int)ActorTypeEnum.Organization,
        ActorType = null!,
        Pii = new ActorPii { DisplayName = "Organization publisher" }
    };

    private static Actor ActorForGroup(Guid groupId) => new()
    {
        Id = Guid.CreateVersion7(),
        GroupId = groupId,
        ActorTypeId = (int)ActorTypeEnum.Group,
        ActorType = null!,
        Pii = new ActorPii { DisplayName = "Group publisher" }
    };

    private static OrganizationTenant OrganizationParticipation(
        Guid organizationId,
        ApprovalStatusEnum approvalStatus,
        bool organizerEligible,
        bool suspended,
        bool deleted) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = Guid.CreateVersion7(),
        Tenant = null!,
        OrganizationId = organizationId,
        Organization = null!,
        ApprovalStatusId = (int)approvalStatus,
        ApprovalStatus = null!,
        IsVisible = false,
        IsOrganizerEligible = organizerEligible,
        IsSuspended = suspended,
        IsDeleted = deleted
    };

    private static GroupTenant GroupParticipation(
        Guid groupId,
        ApprovalStatusEnum approvalStatus,
        bool organizerEligible,
        bool suspended,
        bool deleted) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = Guid.CreateVersion7(),
        Tenant = null!,
        GroupId = groupId,
        Group = null!,
        ApprovalStatusId = (int)approvalStatus,
        ApprovalStatus = null!,
        IsVisible = false,
        IsOrganizerEligible = organizerEligible,
        IsSuspended = suspended,
        IsDeleted = deleted
    };

    private enum ActorResolutionPath
    {
        User,
        Organization,
        Group
    }
}
