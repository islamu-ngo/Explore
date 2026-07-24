// ABOUTME: Focused unit tests for presence-aware instance footer-governance updates.
// ABOUTME: Proves partial lock changes are authorized, write only the supplied key, and invalidate the instance cache.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.Footer;
using Explore.Application.Features.Footer.Handlers.Commands;
using Explore.Application.Features.Footer.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Footer.Commands;

public sealed class UpdateFooterGovernanceSettingsCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenOneLockIsProvided_WritesOnlyThatKeyAndInvalidatesInstanceCache()
    {
        var userId = Guid.CreateVersion7();
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.IsInstanceAdminAsync(userId, Arg.Any<CancellationToken>()).Returns(true);
        var settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        settingsResolver.SetValueAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SettingScope>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var handler = new UpdateFooterGovernanceSettingsCommandHandler(adminContext, settingsResolver);

        var result = await handler.Handle(new UpdateFooterGovernanceSettingsCommand
        {
            UserId = userId,
            Patch = new PatchFooterGovernanceSettingsDto
            {
                LockTenantTemplate = OptionalUpdate<bool>.Set(true)
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await settingsResolver.Received(1).SetValueAsync(
            GovernanceSettingKeys.Footer.LockTenantTemplate,
            Arg.Any<string>(),
            SettingScope.Instance,
            Guid.Empty,
            userId,
            Arg.Any<CancellationToken>());
        await settingsResolver.DidNotReceive().SetValueAsync(
            GovernanceSettingKeys.Footer.LockTenantLinkGroups,
            Arg.Any<string>(),
            Arg.Any<SettingScope>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        settingsResolver.Received(1).InvalidateCache(SettingScope.Instance, null);
    }

    [Test]
    public async Task Handle_WhenPatchIsEmpty_DoesNotWrite()
    {
        var userId = Guid.CreateVersion7();
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.IsInstanceAdminAsync(userId, Arg.Any<CancellationToken>()).Returns(true);
        var settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        var handler = new UpdateFooterGovernanceSettingsCommandHandler(adminContext, settingsResolver);

        var result = await handler.Handle(new UpdateFooterGovernanceSettingsCommand
        {
            UserId = userId,
            Patch = new PatchFooterGovernanceSettingsDto()
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await settingsResolver.DidNotReceive().SetValueAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SettingScope>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        settingsResolver.DidNotReceive().InvalidateCache(Arg.Any<SettingScope?>(), Arg.Any<Guid?>());
    }
}
