// ABOUTME: Local authorization parity tests for account-scoped registration orders.
// ABOUTME: Verifies fallback access is tenant-bound and never grants another buyer's order.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Settings;
using Explore.Domain.Constants;
using Explore.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Authorization;

public sealed class RegistrationOrderFallbackAuthorizationTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly Guid _orderId = Guid.CreateVersion7();
    private readonly Guid _accountUserId = Guid.CreateVersion7();
    private readonly IAdminContext _adminContext = Substitute.For<IAdminContext>();
    private readonly IMachinePrincipalAccessor _machinePrincipalAccessor = Substitute.For<IMachinePrincipalAccessor>();
    private readonly IEventAuthoritySnapshotService _eventAuthority = Substitute.For<IEventAuthoritySnapshotService>();
    private readonly IOrganizationMemberRepository _organizationMembers = Substitute.For<IOrganizationMemberRepository>();
    private readonly IGroupMemberRepository _groupMembers = Substitute.For<IGroupMemberRepository>();
    private readonly IHierarchicalSettingsResolver _settings = Substitute.For<IHierarchicalSettingsResolver>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    [Test]
    public async Task IsAllowed_AccountOwnerMayViewAndUseLifecycleActionsOnOwnTenantOrder()
    {
        FallbackAuthorizationService service = CreateService();
        _adminContext.UserId.Returns(_accountUserId);

        bool canView = await service.IsAllowedAsync(
            ResourceKinds.RegistrationOrder,
            _orderId.ToString("D"),
            AuthorizationActions.RegistrationOrders.View,
            Attributes());
        bool canCancel = await service.IsAllowedAsync(
            ResourceKinds.RegistrationOrder,
            _orderId.ToString("D"),
            AuthorizationActions.RegistrationOrders.Cancel,
            Attributes());
        bool canContinue = await service.IsAllowedAsync(
            ResourceKinds.RegistrationOrder,
            _orderId.ToString("D"),
            AuthorizationActions.RegistrationOrders.Continue,
            Attributes());
        bool canFinalize = await service.IsAllowedAsync(
            ResourceKinds.RegistrationOrder,
            _orderId.ToString("D"),
            AuthorizationActions.RegistrationOrders.Finalize,
            Attributes());

        await Assert.That(canView).IsTrue();
        await Assert.That(canCancel).IsTrue();
        await Assert.That(canContinue).IsTrue();
        await Assert.That(canFinalize).IsTrue();
    }

    [Test]
    public async Task IsAllowed_DifferentAccountOrTenantIsDenied()
    {
        FallbackAuthorizationService service = CreateService();
        _adminContext.UserId.Returns(Guid.CreateVersion7());

        bool differentAccount = await service.IsAllowedAsync(
            ResourceKinds.RegistrationOrder,
            _orderId.ToString("D"),
            AuthorizationActions.RegistrationOrders.View,
            Attributes());
        _adminContext.UserId.Returns(_accountUserId);
        bool differentTenant = await service.IsAllowedAsync(
            ResourceKinds.RegistrationOrder,
            _orderId.ToString("D"),
            AuthorizationActions.RegistrationOrders.View,
            Attributes(_tenantId == Guid.Empty ? Guid.CreateVersion7() : Guid.CreateVersion7()));

        await Assert.That(differentAccount).IsFalse();
        await Assert.That(differentTenant).IsFalse();
    }

    [Test]
    public async Task IsAllowed_RegistrationManagerMayViewButNotMutateAnOrder()
    {
        FallbackAuthorizationService service = CreateService();
        Guid managerUserId = Guid.CreateVersion7();
        _adminContext.UserId.Returns(managerUserId);
        _eventAuthority.GetForUserAndEventsAsync(_tenantId, managerUserId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new EventAuthoritySnapshot(
                _tenantId,
                managerUserId,
                new Dictionary<Guid, EventAuthorityForUser>
                {
                    [_eventId] = new(
                        new HashSet<string>(),
                        new HashSet<string> { PermissionCodes.EventRegistrationManage },
                        false,
                        true)
                }));

        bool canView = await service.IsAllowedAsync(
            ResourceKinds.RegistrationOrder,
            _orderId.ToString("D"),
            AuthorizationActions.RegistrationOrders.View,
            Attributes());
        bool canCancel = await service.IsAllowedAsync(
            ResourceKinds.RegistrationOrder,
            _orderId.ToString("D"),
            AuthorizationActions.RegistrationOrders.Cancel,
            Attributes());
        bool canContinue = await service.IsAllowedAsync(
            ResourceKinds.RegistrationOrder,
            _orderId.ToString("D"),
            AuthorizationActions.RegistrationOrders.Continue,
            Attributes());
        bool canFinalize = await service.IsAllowedAsync(
            ResourceKinds.RegistrationOrder,
            _orderId.ToString("D"),
            AuthorizationActions.RegistrationOrders.Finalize,
            Attributes());

        await Assert.That(canView).IsTrue();
        await Assert.That(canCancel).IsFalse();
        await Assert.That(canContinue).IsFalse();
        await Assert.That(canFinalize).IsFalse();
    }

    private FallbackAuthorizationService CreateService()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _machinePrincipalAccessor.IsMachineCaller.Returns(false);

        return new FallbackAuthorizationService(
            _adminContext,
            _machinePrincipalAccessor,
            _eventAuthority,
            _organizationMembers,
            _groupMembers,
            _settings,
            _tenantContext,
            Substitute.For<ILogger<FallbackAuthorizationService>>());
    }

    private Dictionary<string, object> Attributes(Guid? tenantId = null) => new()
    {
        ["tenantId"] = (tenantId ?? _tenantId).ToString("D"),
        ["eventId"] = _eventId.ToString("D"),
        ["accountUserId"] = _accountUserId.ToString("D")
    };
}
