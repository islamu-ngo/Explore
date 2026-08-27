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
    public async Task ResolveAsync_PersonalActorWithDisabledTenantSubmissionPolicy_IsRejected()
    {
        var result = await ResolveAsync(
            ActorForUser(Guid.CreateVersion7()),
            ActorResolutionPath.User,
            userSubmissionEnabled: false);

        await Assert.That(result.Succeeded).IsFalse();
    }

    [Test]
    [Category("TCM110SubmissionPolicyMatrix")]
    public async Task ResolveAsync_OrganizationSubmissionDisabled_UsesOrganizationPolicyAndRejectsBeforeEligibilityReads()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var organizationId = Guid.CreateVersion7();
        var scenario = CreateScenario(
            tenantId,
            userSubmissionEnabled: true,
            organizationSubmissionEnabled: false,
            groupSubmissionEnabled: true);

        var result = await scenario.Resolver.ResolveAsync(userId, organizationId, null, CancellationToken.None);

        await AssertResolvedSettingAsync(scenario.SettingsResolver, GovernanceSettingKeys.Events.OrganizationSubmissionEnabled, tenantId);
        await AssertSettingWasNotResolvedAsync(scenario.SettingsResolver, GovernanceSettingKeys.Events.UserSubmissionEnabled);
        await AssertSettingWasNotResolvedAsync(scenario.SettingsResolver, GovernanceSettingKeys.Events.GroupSubmissionEnabled);
        await Assert.That(result.Succeeded).IsFalse();
        await scenario.OrganizationMemberRepository.DidNotReceive().HasPermissionInOrganization(
            organizationId, userId, PermissionCodes.EventCreate);
        await scenario.ActorRepository.DidNotReceive().GetActorByOrganizationId(organizationId);
        await scenario.OrganizationTenantRepository.DidNotReceive().GetByOrganizationAndTenant(
            organizationId, tenantId, Arg.Any<CancellationToken>());
    }

    [Test]
    [Category("TCM110SubmissionPolicyMatrix")]
    public async Task ResolveAsync_OrganizationSubmissionEnabled_UsesOrganizationPolicyDespiteDisabledGroupPolicy()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var organizationId = Guid.CreateVersion7();
        var actor = ActorForOrganization(organizationId);
        var scenario = CreateScenario(
            tenantId,
            userSubmissionEnabled: true,
            organizationSubmissionEnabled: true,
            groupSubmissionEnabled: false);
        scenario.OrganizationMemberRepository.HasPermissionInOrganization(
            organizationId, userId, PermissionCodes.EventCreate).Returns(true);
        scenario.ActorRepository.GetActorByOrganizationId(organizationId).Returns(actor);
        scenario.OrganizationTenantRepository.GetByOrganizationAndTenant(
                organizationId,
                tenantId,
                Arg.Any<CancellationToken>())
            .Returns(OrganizationParticipation(
                organizationId,
                ApprovalStatusEnum.Approved,
                organizerEligible: true,
                suspended: false,
                deleted: false,
                tenantId));

        var result = await scenario.Resolver.ResolveAsync(userId, organizationId, null, CancellationToken.None);

        await AssertResolvedSettingAsync(scenario.SettingsResolver, GovernanceSettingKeys.Events.OrganizationSubmissionEnabled, tenantId);
        await AssertSettingWasNotResolvedAsync(scenario.SettingsResolver, GovernanceSettingKeys.Events.UserSubmissionEnabled);
        await AssertSettingWasNotResolvedAsync(scenario.SettingsResolver, GovernanceSettingKeys.Events.GroupSubmissionEnabled);
        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.ActorId).IsEqualTo(actor.Id);
    }

    [Test]
    [Category("TCM110SubmissionPolicyMatrix")]
    public async Task ResolveAsync_GroupSubmissionDisabled_UsesGroupPolicyAndRejectsBeforeEligibilityReads()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var groupId = Guid.CreateVersion7();
        var scenario = CreateScenario(
            tenantId,
            userSubmissionEnabled: true,
            organizationSubmissionEnabled: true,
            groupSubmissionEnabled: false);

        var result = await scenario.Resolver.ResolveAsync(userId, null, groupId, CancellationToken.None);

        await AssertResolvedSettingAsync(scenario.SettingsResolver, GovernanceSettingKeys.Events.GroupSubmissionEnabled, tenantId);
        await AssertSettingWasNotResolvedAsync(scenario.SettingsResolver, GovernanceSettingKeys.Events.UserSubmissionEnabled);
        await AssertSettingWasNotResolvedAsync(scenario.SettingsResolver, GovernanceSettingKeys.Events.OrganizationSubmissionEnabled);
        await Assert.That(result.Succeeded).IsFalse();
        await scenario.GroupMemberRepository.DidNotReceive().HasPermissionInGroup(
            groupId, userId, PermissionCodes.EventCreate);
        await scenario.ActorRepository.DidNotReceive().GetActorByGroupId(groupId);
        await scenario.GroupTenantRepository.DidNotReceive().GetByGroupAndTenant(
            groupId, tenantId, Arg.Any<CancellationToken>());
    }

    [Test]
    [Category("TCM110SubmissionPolicyMatrix")]
    public async Task ResolveAsync_GroupSubmissionEnabled_UsesGroupPolicyDespiteDisabledOrganizationPolicy()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var groupId = Guid.CreateVersion7();
        var actor = ActorForGroup(groupId);
        var scenario = CreateScenario(
            tenantId,
            userSubmissionEnabled: true,
            organizationSubmissionEnabled: false,
            groupSubmissionEnabled: true);
        scenario.GroupMemberRepository.HasPermissionInGroup(groupId, userId, PermissionCodes.EventCreate).Returns(true);
        scenario.ActorRepository.GetActorByGroupId(groupId).Returns(actor);
        scenario.GroupTenantRepository.GetByGroupAndTenant(
                groupId,
                tenantId,
                Arg.Any<CancellationToken>())
            .Returns(GroupParticipation(
                groupId,
                ApprovalStatusEnum.Approved,
                organizerEligible: true,
                suspended: false,
                deleted: false,
                tenantId));

        var result = await scenario.Resolver.ResolveAsync(userId, null, groupId, CancellationToken.None);

        await AssertResolvedSettingAsync(scenario.SettingsResolver, GovernanceSettingKeys.Events.GroupSubmissionEnabled, tenantId);
        await AssertSettingWasNotResolvedAsync(scenario.SettingsResolver, GovernanceSettingKeys.Events.UserSubmissionEnabled);
        await AssertSettingWasNotResolvedAsync(scenario.SettingsResolver, GovernanceSettingKeys.Events.OrganizationSubmissionEnabled);
        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.ActorId).IsEqualTo(actor.Id);
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
        GroupTenant? groupParticipation = null,
        bool userSubmissionEnabled = true)
    {
        var tenantId = organizationParticipation?.TenantId
            ?? groupParticipation?.TenantId
            ?? Guid.CreateVersion7();
        var scenario = CreateScenario(tenantId, userSubmissionEnabled);
        var currentUserId = actor.UserId ?? Guid.CreateVersion7();
        switch (resolutionPath)
        {
            case ActorResolutionPath.User:
                scenario.ActorRepository.GetActorByUserId(currentUserId).Returns(actor);
                if (actor.UserId is { } userId)
                {
                    scenario.TenantUserRepository.IsActiveTenantUserAsync(tenantId, userId, Arg.Any<CancellationToken>())
                        .Returns(activeTenantUser);
                }
                break;
            case ActorResolutionPath.Organization:
                scenario.OrganizationMemberRepository.HasPermissionInOrganization(
                        Arg.Any<Guid>(),
                        currentUserId,
                        PermissionCodes.EventCreate)
                    .Returns(true);
                scenario.ActorRepository.GetActorByOrganizationId(Arg.Any<Guid>()).Returns(actor);
                if (actor.OrganizationId is { } organizationId)
                {
                    scenario.OrganizationTenantRepository.GetByOrganizationAndTenant(
                            organizationId,
                            tenantId,
                            Arg.Any<CancellationToken>())
                        .Returns(organizationParticipation);
                }
                break;
            case ActorResolutionPath.Group:
                scenario.GroupMemberRepository.HasPermissionInGroup(
                        Arg.Any<Guid>(),
                        currentUserId,
                        PermissionCodes.EventCreate)
                    .Returns(true);
                scenario.ActorRepository.GetActorByGroupId(Arg.Any<Guid>()).Returns(actor);
                if (actor.GroupId is { } groupId)
                {
                    scenario.GroupTenantRepository.GetByGroupAndTenant(groupId, tenantId, Arg.Any<CancellationToken>())
                        .Returns(groupParticipation);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(resolutionPath), resolutionPath, null);
        }

        return await scenario.Resolver.ResolveAsync(
            currentUserId,
            resolutionPath == ActorResolutionPath.Organization ? actor.OrganizationId : null,
            resolutionPath == ActorResolutionPath.Group ? actor.GroupId : null,
            CancellationToken.None);
    }

    private static ResolverScenario CreateScenario(
        Guid tenantId,
        bool userSubmissionEnabled = true,
        bool organizationSubmissionEnabled = true,
        bool groupSubmissionEnabled = true)
    {
        var actorRepository = Substitute.For<IActorRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();
        var settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        var tenantContext = Substitute.For<ITenantContext>();
        var tenantUserRepository = Substitute.For<ITenantUserRepository>();
        var organizationTenantRepository = Substitute.For<IOrganizationTenantRepository>();
        var groupTenantRepository = Substitute.For<IGroupTenantRepository>();
        tenantContext.TenantId.Returns(tenantId);
        settingsResolver.ResolveAsync<bool>(GovernanceSettingKeys.Events.UserSubmissionEnabled, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(userSubmissionEnabled);
        settingsResolver.ResolveAsync<bool>(GovernanceSettingKeys.Events.OrganizationSubmissionEnabled, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(organizationSubmissionEnabled);
        settingsResolver.ResolveAsync<bool>(GovernanceSettingKeys.Events.GroupSubmissionEnabled, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(groupSubmissionEnabled);

        return new ResolverScenario(
            new EventActorResolver(
                actorRepository,
                organizationMemberRepository,
                groupMemberRepository,
                settingsResolver,
                tenantContext,
                tenantUserRepository,
                organizationTenantRepository,
                groupTenantRepository),
            actorRepository,
            organizationMemberRepository,
            groupMemberRepository,
            settingsResolver,
            tenantUserRepository,
            organizationTenantRepository,
            groupTenantRepository);
    }

    private static async Task AssertResolvedSettingAsync(
        IHierarchicalSettingsResolver settingsResolver,
        string settingKey,
        Guid tenantId) =>
        await settingsResolver.Received(1).ResolveAsync<bool>(
            settingKey,
            Arg.Is<SettingContext>(context => HasTenantId(context, tenantId)),
            Arg.Any<CancellationToken>());

    private static bool HasTenantId(SettingContext? context, Guid tenantId) => context?.TenantId == tenantId;

    private static async Task AssertSettingWasNotResolvedAsync(
        IHierarchicalSettingsResolver settingsResolver,
        string settingKey) =>
        await settingsResolver.DidNotReceive().ResolveAsync<bool>(
            settingKey,
            Arg.Any<SettingContext>(),
            Arg.Any<CancellationToken>());

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
        bool deleted,
        Guid? tenantId = null) => new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId ?? Guid.CreateVersion7(),
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
        bool deleted,
        Guid? tenantId = null) => new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId ?? Guid.CreateVersion7(),
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

    private sealed record ResolverScenario(
        EventActorResolver Resolver,
        IActorRepository ActorRepository,
        IOrganizationMemberRepository OrganizationMemberRepository,
        IGroupMemberRepository GroupMemberRepository,
        IHierarchicalSettingsResolver SettingsResolver,
        ITenantUserRepository TenantUserRepository,
        IOrganizationTenantRepository OrganizationTenantRepository,
        IGroupTenantRepository GroupTenantRepository);

    private enum ActorResolutionPath
    {
        User,
        Organization,
        Group
    }
}
