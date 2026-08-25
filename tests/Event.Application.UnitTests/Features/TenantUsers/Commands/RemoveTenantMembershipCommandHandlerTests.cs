// ABOUTME: Tests tenant-scoped membership removal authorization and transaction orchestration.
// ABOUTME: Proves self or tenant-admin removal never depends on global account or Home erasure services.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.TenantUsers.Handlers.Commands;
using Explore.Application.Features.TenantUsers.Requests.Commands;
using Explore.Application.Features.TenantUsers.Validators;
using FluentValidation.TestHelper;
using NSubstitute;

namespace Event.Application.UnitTests.Features.TenantUsers.Commands;

public sealed class RemoveTenantMembershipCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    private readonly ITenantUserRepository _tenantUsers = Substitute.For<ITenantUserRepository>();
    private readonly ITenantUserRoleGrantRepository _roleGrants = Substitute.For<ITenantUserRoleGrantRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    public RemoveTenantMembershipCommandHandlerTests()
    {
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<bool>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<bool>>>()!(call.ArgAt<CancellationToken>(1)));
    }

    [Test]
    public async Task Command_UsesUserUpdateAuthorizationWithExactTenantAndTargetIdentity()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var command = new RemoveTenantMembershipCommand(tenantId, userId);
        var attribute = typeof(RemoveTenantMembershipCommand)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: true)
            .Cast<AuthorizeResourceAttribute>()
            .Single()!;
        var secureRequest = (ISecureRequest)command;

        await Assert.That(attribute.Resource).IsEqualTo(ResourceKinds.User);
        await Assert.That(attribute.Action).IsEqualTo(AuthorizationActions.Update);
        await Assert.That(secureRequest.ResourceId).IsEqualTo(userId.ToString("D"));
        // The membership row is addressed by user id; tenant administration is what the policy weighs.
        await Assert.That(secureRequest.AuthorizationFacts)
            .IsEqualTo(new UserAuthorizationFacts(tenantId, null, null));
    }

    [Test]
    public async Task Validator_RejectsEmptyTenantAndUserIdentifiers()
    {
        var result = await new RemoveTenantMembershipCommandValidator()
            .TestValidateAsync(new RemoveTenantMembershipCommand(Guid.Empty, Guid.Empty));

        result.ShouldHaveValidationErrorFor(command => command.TenantId);
        result.ShouldHaveValidationErrorFor(command => command.UserId);
    }

    [Test]
    public async Task EmptyIdentifierOneFactVariantsFailClosedBeforeStartingTransaction()
    {
        var currentTenantId = Guid.CreateVersion7();
        var currentUserId = Guid.CreateVersion7();
        ConfigureIdentity(currentTenantId, currentUserId);
        var legitimate = new RemoveTenantMembershipCommand(currentTenantId, currentUserId);
        RemoveTenantMembershipCommand[] emptyIdentifierVariants =
        [
            legitimate with { TenantId = Guid.Empty },
            legitimate with { UserId = Guid.Empty }
        ];
        var handler = CreateHandler();

        foreach (RemoveTenantMembershipCommand variant in emptyIdentifierVariants)
        {
            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
                handler.Handle(variant, CancellationToken.None));

            await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<bool>>>(),
                Arg.Any<CancellationToken>());
            await _tenantUsers.DidNotReceive().TryRemoveMembershipAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>());
        }
    }

    [Test]
    public async Task SelfRemoval_ExecutesExactlyOneTenantScopedAtomicMutation()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        ConfigureIdentity(tenantId, userId);
        _tenantUsers.TryRemoveMembershipAsync(tenantId, userId, userId, Now.UtcDateTime, Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = CreateHandler();

        var removed = await handler.Handle(new RemoveTenantMembershipCommand(tenantId, userId), CancellationToken.None);

        await Assert.That(removed).IsTrue();
        await _unitOfWork.Received(1).ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<bool>>>(),
            CancellationToken.None);
        await _roleGrants.DidNotReceive().IsTenantAdminInCurrentTenantAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _tenantUsers.Received(1).TryRemoveMembershipAsync(
            tenantId,
            userId,
            userId,
            Now.UtcDateTime,
            CancellationToken.None);
    }

    [Test]
    public async Task TenantAdminRemoval_RevalidatesCurrentTenantAuthorityInsideTransaction()
    {
        var tenantId = Guid.CreateVersion7();
        var managerId = Guid.CreateVersion7();
        var targetUserId = Guid.CreateVersion7();
        ConfigureIdentity(tenantId, managerId);
        var observedEvents = new List<string>();
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<bool>>>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                observedEvents.Add("transaction-enter");
                try
                {
                    return await call.Arg<Func<CancellationToken, Task<bool>>>()!(call.ArgAt<CancellationToken>(1));
                }
                finally
                {
                    observedEvents.Add("transaction-exit");
                }
            });
        _roleGrants.IsTenantAdminInCurrentTenantAsync(tenantId, managerId, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                observedEvents.Add("fresh-admin-check-denied");
                return false;
            });
        var handler = CreateHandler();
        var targetVariant = new RemoveTenantMembershipCommand(tenantId, managerId) with { UserId = targetUserId };

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            handler.Handle(targetVariant, CancellationToken.None));

        await Assert.That(observedEvents.Count).IsEqualTo(3);
        await Assert.That(observedEvents[0]).IsEqualTo("transaction-enter");
        await Assert.That(observedEvents[1]).IsEqualTo("fresh-admin-check-denied");
        await Assert.That(observedEvents[2]).IsEqualTo("transaction-exit");
        await _roleGrants.Received(1).IsTenantAdminInCurrentTenantAsync(
            tenantId, managerId, CancellationToken.None);
        await _tenantUsers.DidNotReceive().TryRemoveMembershipAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TenantAdminRemovalKeepsTargetAndActorDistinct()
    {
        var tenantId = Guid.CreateVersion7();
        var managerId = Guid.CreateVersion7();
        var targetUserId = Guid.CreateVersion7();
        ConfigureIdentity(tenantId, managerId);
        _roleGrants.IsTenantAdminInCurrentTenantAsync(tenantId, managerId, Arg.Any<CancellationToken>())
            .Returns(true);
        _tenantUsers.TryRemoveMembershipAsync(tenantId, targetUserId, managerId, Now.UtcDateTime, Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = CreateHandler();
        var selfRequest = new RemoveTenantMembershipCommand(tenantId, managerId);
        var targetVariant = selfRequest with { UserId = targetUserId };

        var removed = await handler.Handle(targetVariant, CancellationToken.None);

        await Assert.That(removed).IsTrue();
        await Assert.That(targetVariant.UserId).IsNotEqualTo(managerId);
        await Assert.That(((ISecureRequest)targetVariant).ResourceId).IsEqualTo(targetUserId.ToString("D"));
        await Assert.That(((ISecureRequest)targetVariant).AuthorizationFacts)
            .IsEqualTo(new UserAuthorizationFacts(tenantId, null, null));
        await _roleGrants.Received(1).IsTenantAdminInCurrentTenantAsync(
            tenantId, managerId, CancellationToken.None);
        await _tenantUsers.Received(1).TryRemoveMembershipAsync(
            tenantId, targetUserId, managerId, Now.UtcDateTime, CancellationToken.None);
    }

    [Test]
    public async Task NonManagerRemoval_FailsClosedWithoutMutatingMembership()
    {
        var tenantId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var targetUserId = Guid.CreateVersion7();
        ConfigureIdentity(tenantId, actorId);
        _roleGrants.IsTenantAdminInCurrentTenantAsync(tenantId, actorId, Arg.Any<CancellationToken>())
            .Returns(false);
        var handler = CreateHandler();
        var selfRequest = new RemoveTenantMembershipCommand(tenantId, actorId);
        var changedTarget = selfRequest with { UserId = targetUserId };

        await Assert.ThrowsAsync<AuthorizationException>(() => handler.Handle(
            changedTarget,
            CancellationToken.None));

        await _tenantUsers.DidNotReceive().TryRemoveMembershipAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task WrongAmbientTenantOneFactVariantFailsBeforeStartingTransaction()
    {
        var currentTenantId = Guid.CreateVersion7();
        var currentUserId = Guid.CreateVersion7();
        ConfigureIdentity(currentTenantId, currentUserId);
        var handler = CreateHandler();
        var legitimate = new RemoveTenantMembershipCommand(currentTenantId, currentUserId);
        var wrongTenant = legitimate with { TenantId = Guid.CreateVersion7() };

        await Assert.ThrowsAsync<AuthorizationException>(() => handler.Handle(
            wrongTenant,
            CancellationToken.None));

        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<bool>>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task MissingOrPreviouslyRemovedMembership_ReturnsStableFalse()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        ConfigureIdentity(tenantId, userId);
        _tenantUsers.TryRemoveMembershipAsync(tenantId, userId, userId, Now.UtcDateTime, Arg.Any<CancellationToken>())
            .Returns(false);
        var handler = CreateHandler();

        var removed = await handler.Handle(new RemoveTenantMembershipCommand(tenantId, userId), CancellationToken.None);

        await Assert.That(removed).IsFalse();
    }

    [Test]
    public async Task HandlerConstructor_HasNoGlobalDeletionOrHomeErasureDependency()
    {
        var parameterTypes = typeof(RemoveTenantMembershipCommandHandler)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Select(parameter => parameter.ParameterType.Name)
            .ToArray();

        await Assert.That(parameterTypes).DoesNotContain("IUserRepository");
        await Assert.That(parameterTypes).DoesNotContain("ILocationRepository");
        await Assert.That(parameterTypes).DoesNotContain("IUserLocationPrivacyErasureRepository");
        await Assert.That(parameterTypes).DoesNotContain("IErasureAuthorityClient");
    }

    private RemoveTenantMembershipCommandHandler CreateHandler() => new(
        _tenantUsers,
        _roleGrants,
        _unitOfWork,
        _tenantContext,
        _currentUser,
        new FixedTimeProvider(Now));

    private void ConfigureIdentity(Guid tenantId, Guid currentUserId)
    {
        _tenantContext.TenantId.Returns(tenantId);
        _currentUser.UserId.Returns(currentUserId);
        _currentUser.IsAuthenticated.Returns(true);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
