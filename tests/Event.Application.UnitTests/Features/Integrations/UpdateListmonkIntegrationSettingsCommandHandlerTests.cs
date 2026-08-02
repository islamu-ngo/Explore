// ABOUTME: Focused tests for grouped Listmonk integration settings PATCH handling.
// ABOUTME: Verifies omitted connection settings are preserved while behavior settings update.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.Integrations;
using Explore.Application.Features.Integrations.Listmonk.Handlers.Commands;
using Explore.Application.Features.Integrations.Listmonk.Requests.Commands;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using MediatR;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Integrations;

public sealed class UpdateListmonkIntegrationSettingsCommandHandlerTests
{
    [Test]
    public async Task Handle_BehaviorOnly_PreservesPersistedConnectionSettings()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var resolver = Substitute.For<IHierarchicalSettingsResolver>();
        var adminContext = Substitute.For<IAdminContext>();
        var tenantContext = Substitute.For<ITenantContext>();
        var currentUser = Substitute.For<ICurrentUserService>();
        var publisher = Substitute.For<IPublisher>();
        tenantContext.TenantId.Returns(tenantId);
        currentUser.UserId.Returns(userId);
        adminContext.IsTenantAdminAsync(tenantId, Arg.Any<CancellationToken>()).Returns(true);
        resolver.ResolveAsync<bool>(GovernanceSettingKeys.Integrations.Listmonk.Enabled, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>()).Returns(false);
        resolver.ResolveAsync<string>(GovernanceSettingKeys.Integrations.Listmonk.InstanceUrl, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>()).Returns("https://listmonk.example.test");
        resolver.ResolveAsync<int>(GovernanceSettingKeys.Integrations.Listmonk.DefaultListId, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>()).Returns(9);
        resolver.ResolveAsync<bool>(GovernanceSettingKeys.Integrations.Listmonk.PreconfirmSubscriptions, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>()).Returns(true);
        resolver.ResolveAsync<bool>(GovernanceSettingKeys.Integrations.Listmonk.SyncOnRegistration, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>()).Returns(false);
        resolver.ResolveBatchAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<SettingContext>(), Arg.Any<CancellationToken>()).Returns([]);
        var handler = new UpdateListmonkIntegrationSettingsCommandHandler(
            resolver,
            adminContext,
            tenantContext,
            currentUser,
            publisher);

        var result = await handler.Handle(
            new UpdateListmonkIntegrationSettingsCommand
            {
                Dto = new UpdateListmonkIntegrationSettingsDto
                {
                    Behavior = new ListmonkBehaviorUpdateDto
                    {
                        Enabled = true,
                        PreconfirmSubscriptions = false,
                        SyncOnRegistration = true
                    }
                }
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await resolver.Received(1).SetValueAsync(
            GovernanceSettingKeys.Integrations.Listmonk.Enabled,
            "true",
            SettingScope.Tenant,
            tenantId,
            userId,
            Arg.Any<CancellationToken>());
        await resolver.DidNotReceive().SetValueAsync(
            GovernanceSettingKeys.Integrations.Listmonk.InstanceUrl,
            Arg.Any<string>(),
            Arg.Any<SettingScope>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        await resolver.DidNotReceive().SetValueAsync(
            GovernanceSettingKeys.Integrations.Listmonk.DefaultListId,
            Arg.Any<string>(),
            Arg.Any<SettingScope>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }
}
