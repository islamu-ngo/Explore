// ABOUTME: Tests effective footer link-group lock enforcement across all mutation handlers.
// ABOUTME: Proves multi-tenant denial precedes repository access and single-tenant bypass permits writes.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Exceptions;
using Explore.Application.Features.Footer.Handlers.Commands;
using Explore.Application.Features.Footer.Requests.Commands;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain;
using Explore.Domain.Constants;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Footer.Commands;

public sealed class FooterLinkMutationGuardTests
{
    [Test]
    public async Task AllMutationHandlers_WhenMultiTenantLinkGroupsAreLocked_DenyBeforeRepositoryAccess()
    {
        var tenantId = Guid.NewGuid();
        var settingsResolver = CreateSettingsResolver(lockLinkGroups: true);
        var deploymentModeProvider = Substitute.For<IDeploymentModeProvider>();
        deploymentModeProvider.IsSingleTenantAsync(Arg.Any<CancellationToken>()).Returns(false);
        var guard = new FooterLinkMutationGuard(settingsResolver, deploymentModeProvider);
        var groupRepository = Substitute.For<IFooterLinkGroupRepository>();
        var linkRepository = Substitute.For<IFooterLinkRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);
        var groupId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        Func<Task>[] mutations =
        [
            async () => await new CreateFooterLinkGroupCommandHandler(groupRepository, guard).Handle(
                new CreateFooterLinkGroupCommand { TenantId = tenantId, UserId = Guid.NewGuid(), Title = "Main" },
                CancellationToken.None),
            async () => await new UpdateFooterLinkGroupCommandHandler(groupRepository, tenantContext, guard).Handle(
                new UpdateFooterLinkGroupCommand
                {
                    TenantId = tenantId,
                    GroupId = groupId,
                    Update = new()
                    {
                        Title = new() { Value = "Main" },
                        IsActive = new() { Value = true }
                    }
                },
                CancellationToken.None),
            async () => await new DeleteFooterLinkGroupCommandHandler(groupRepository, linkRepository, unitOfWork, tenantContext, guard).Handle(
                new DeleteFooterLinkGroupCommand { TenantId = tenantId, UserId = Guid.NewGuid(), GroupId = groupId },
                CancellationToken.None),
            async () => await new ReorderFooterLinkGroupsCommandHandler(groupRepository, unitOfWork, tenantContext, guard).Handle(
                new ReorderFooterLinkGroupsCommand { TenantId = tenantId, UserId = Guid.NewGuid(), OrderedGroupIds = [groupId] },
                CancellationToken.None),
            async () => await new CreateFooterLinkCommandHandler(groupRepository, linkRepository, tenantContext, settingsResolver, guard).Handle(
                new CreateFooterLinkCommand { TenantId = tenantId, UserId = Guid.NewGuid(), GroupId = groupId, Label = "Home", Url = "/" },
                CancellationToken.None),
            async () => await new UpdateFooterLinkCommandHandler(linkRepository, tenantContext, settingsResolver, guard).Handle(
                new UpdateFooterLinkCommand
                {
                    TenantId = tenantId,
                    LinkId = linkId,
                    Update = new()
                    {
                        Label = new() { Value = "Home" },
                        Url = new() { Value = "/" },
                        IsActive = new() { Value = true }
                    }
                },
                CancellationToken.None),
            async () => await new DeleteFooterLinkCommandHandler(groupRepository, linkRepository, tenantContext, guard).Handle(
                new DeleteFooterLinkCommand { TenantId = tenantId, UserId = Guid.NewGuid(), LinkId = linkId },
                CancellationToken.None)
        ];

        foreach (var mutate in mutations)
            await Assert.ThrowsAsync<AuthorizationException>(mutate);

        await groupRepository.DidNotReceiveWithAnyArgs().GetMaxOrderAsync(default, default);
        await groupRepository.DidNotReceiveWithAnyArgs().GetById(default);
        await groupRepository.DidNotReceiveWithAnyArgs().GetByTenantIdAsync(default, default);
        await linkRepository.DidNotReceiveWithAnyArgs().GetById(default);
        await unitOfWork.DidNotReceiveWithAnyArgs().ExecuteInTransactionAsync(
            default!,
            default);
    }

    [Test]
    public async Task CreateLinkGroup_WhenSingleTenant_IgnoresRawLockAndWrites()
    {
        var tenantId = Guid.NewGuid();
        var settingsResolver = CreateSettingsResolver(lockLinkGroups: true);
        var deploymentModeProvider = Substitute.For<IDeploymentModeProvider>();
        deploymentModeProvider.IsSingleTenantAsync(Arg.Any<CancellationToken>()).Returns(true);
        var guard = new FooterLinkMutationGuard(settingsResolver, deploymentModeProvider);
        var groupRepository = Substitute.For<IFooterLinkGroupRepository>();
        groupRepository.GetMaxOrderAsync(tenantId, Arg.Any<CancellationToken>()).Returns(2);
        groupRepository.Create(Arg.Any<TenantFooterLinkGroup>())
            .Returns(call => call.Arg<TenantFooterLinkGroup>());
        var handler = new CreateFooterLinkGroupCommandHandler(groupRepository, guard);

        var result = await handler.Handle(
            new CreateFooterLinkGroupCommand { TenantId = tenantId, UserId = Guid.NewGuid(), Title = "Main" },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await groupRepository.Received(1).Create(Arg.Is<TenantFooterLinkGroup>(group =>
            group.TenantId == tenantId && group.Title == "Main" && group.Order == 3));
        await settingsResolver.DidNotReceive().ResolveGroupAsync<FooterSettingGroup>(
            Arg.Any<SettingContext>(),
            Arg.Any<CancellationToken>());
    }

    private static IHierarchicalSettingsResolver CreateSettingsResolver(bool lockLinkGroups)
    {
        var settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        var group = new FooterSettingGroup();
        group.Populate(new Dictionary<string, ResolvedSetting>
        {
            [GovernanceSettingKeys.Footer.LockTenantLinkGroups] = new()
            {
                Value = SettingValueSerializer.Serialize(lockLinkGroups)
            }
        });
        settingsResolver.ResolveGroupAsync<FooterSettingGroup>(
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(group);
        return settingsResolver;
    }
}
