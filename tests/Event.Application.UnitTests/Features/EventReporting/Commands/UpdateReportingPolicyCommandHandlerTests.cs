// ABOUTME: Focused tests for grouped moderation-reporting policy PATCH handlers.
// ABOUTME: Proves omitted lock/provider groups are preserved and provider locks remain group-specific.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventReporting;
using Explore.Application.Features.EventReporting.Handlers.Commands;
using Explore.Application.Features.EventReporting.Requests.Commands;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventReporting.Commands;

public sealed class UpdateReportingPolicyCommandHandlerTests
{
    [Test]
    public async Task ProviderLocks_WhenOnlyGeneralIsSupplied_WritesOnlyGeneral()
    {
        var userId = Guid.CreateVersion7();
        var adminContext = Substitute.For<IAdminContext>();
        var resolver = Substitute.For<IHierarchicalSettingsResolver>();
        adminContext.IsInstanceAdminAsync(userId, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UpdateReportingProviderLocksCommandHandler(adminContext, resolver);

        var result = await handler.Handle(
            new UpdateReportingProviderLocksCommand(
                userId,
                new UpdateReportingProviderLocksDto
                {
                    General = new ReportingProviderLockUpdateDto { Locked = false }
                }),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await resolver.Received(1).SetValueAsync(
            GovernanceSettingKeys.TenantDelegation.LockReportingProviders,
            "false",
            SettingScope.Instance,
            Guid.Empty,
            userId,
            Arg.Any<CancellationToken>());
        await resolver.DidNotReceive().SetValueAsync(
            GovernanceSettingKeys.TenantDelegation.LockTenantOspreyProvider,
            Arg.Any<string>(),
            Arg.Any<SettingScope>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        await resolver.DidNotReceive().SetValueAsync(
            GovernanceSettingKeys.TenantDelegation.LockTenantCoopProvider,
            Arg.Any<string>(),
            Arg.Any<SettingScope>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Routing_WhenOnlyPolicyIsSupplied_IgnoresProviderSpecificLocksAndPreservesProviders()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var tenantContext = Substitute.For<ITenantContext>();
        var adminContext = Substitute.For<IAdminContext>();
        var resolver = Substitute.For<IHierarchicalSettingsResolver>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        tenantContext.TenantId.Returns(tenantId);
        adminContext.IsTenantAdminAsync(tenantId, Arg.Any<CancellationToken>()).Returns(true);
        resolver.ResolveGroupAsync<TenantDelegationSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(CreateDelegation(generalLocked: false, ospreyLocked: true, coopLocked: true));
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        var handler = new UpdateReportingRoutingSettingsCommandHandler(
            tenantContext,
            adminContext,
            resolver,
            unitOfWork);

        var result = await handler.Handle(
            new UpdateReportingRoutingSettingsCommand(
                tenantId,
                userId,
                new UpdateReportingRoutingSettingsDto
                {
                    Policy = new ReportingRoutingPolicyUpdateDto { ExternalSyncEnabled = true }
                }),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await resolver.Received(1).SetValueAsync(
            GovernanceSettingKeys.Reporting.TenantExternalSyncEnabled,
            "true",
            SettingScope.Tenant,
            tenantId,
            userId,
            Arg.Any<CancellationToken>());
        await resolver.DidNotReceive().SetValueAsync(
            GovernanceSettingKeys.Reporting.EnableTenantOspreyProvider,
            Arg.Any<string>(),
            Arg.Any<SettingScope>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        await resolver.DidNotReceive().SetValueAsync(
            GovernanceSettingKeys.Reporting.EnableTenantCoopProvider,
            Arg.Any<string>(),
            Arg.Any<SettingScope>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    private static TenantDelegationSettingGroup CreateDelegation(
        bool generalLocked,
        bool ospreyLocked,
        bool coopLocked)
    {
        var group = new TenantDelegationSettingGroup();
        group.Populate(new Dictionary<string, ResolvedSetting>
        {
            [GovernanceSettingKeys.TenantDelegation.LockReportingProviders] = new()
            {
                Value = generalLocked ? "true" : "false"
            },
            [GovernanceSettingKeys.TenantDelegation.LockTenantOspreyProvider] = new()
            {
                Value = ospreyLocked ? "true" : "false"
            },
            [GovernanceSettingKeys.TenantDelegation.LockTenantCoopProvider] = new()
            {
                Value = coopLocked ? "true" : "false"
            }
        });
        return group;
    }
}
