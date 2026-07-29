// ABOUTME: Unit tests for subscribing the current tenant user to organization/group actors.
// ABOUTME: Verifies active TenantUser enforcement and idempotent durable row reactivation.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ActorSubscription;
using Explore.Application.Features.ActorSubscriptions.Handlers.Commands;
using Explore.Application.Features.ActorSubscriptions.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.ActorSubscriptions.Commands;

public class SubscribeToActorCommandHandlerTests
{
    private readonly IActorSubscriptionRepository _actorSubscriptionRepository = Substitute.For<IActorSubscriptionRepository>();
    private readonly IActorRepository _actorRepository = Substitute.For<IActorRepository>();
    private readonly ITenantUserRepository _tenantUserRepository = Substitute.For<ITenantUserRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly SubscribeToActorCommandHandler _handler;

    public SubscribeToActorCommandHandlerTests()
    {
        _handler = new SubscribeToActorCommandHandler(
            _actorSubscriptionRepository,
            _actorRepository,
            _tenantUserRepository,
            _tenantContext,
            _currentUserService);
    }

    [Test]
    public async Task Handle_WithSupportedTarget_CreatesActiveSubscription()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantUser = CreateTenantUser(tenantId, userId);
        var targetActor = CreateActor(tenantId, ActorTypeEnum.Organization);
        var createdSubscriptionId = Guid.NewGuid();

        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(userId);
        _tenantUserRepository.GetByTenantAndUserAsync(tenantId, userId, Arg.Any<CancellationToken>()).Returns(tenantUser);
        _actorRepository.GetLocallyDiscoverableSubscriptionTargetAsync(
                tenantId,
                targetActor.Id,
                Arg.Any<CancellationToken>())
            .Returns(targetActor);
        _actorSubscriptionRepository.GetBySubscriberAndTargetAsync(tenantId, tenantUser.Id, targetActor.Id, true, Arg.Any<CancellationToken>())
            .Returns((ActorSubscription?)null);
        _actorSubscriptionRepository.Create(Arg.Any<ActorSubscription>()).Returns(callInfo =>
        {
            var subscription = callInfo.Arg<ActorSubscription>();
            subscription.Id = createdSubscriptionId;
            return subscription;
        });

        var result = await _handler.Handle(
            new SubscribeToActorCommand { Subscription = new SubscribeToActorDto { TargetActorId = targetActor.Id } },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(createdSubscriptionId);
        await _actorSubscriptionRepository.Received(1).Create(Arg.Is<ActorSubscription>(subscription =>
            subscription.TenantId == tenantId
            && subscription.SubscriberTenantUserId == tenantUser.Id
            && subscription.SubscriberUserId == userId
            && subscription.TargetActorId == targetActor.Id
            && subscription.StatusId == (int)ActorSubscriptionStatusEnum.Active
            && subscription.NotificationLevelId == (int)ActorSubscriptionNotificationLevelEnum.All));
    }

    [Test]
    public async Task Handle_WithInactiveTenantUser_ReturnsFailure()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantUser = CreateTenantUser(tenantId, userId, TenantUserStatusEnum.Suspended);

        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(userId);
        _tenantUserRepository.GetByTenantAndUserAsync(tenantId, userId, Arg.Any<CancellationToken>()).Returns(tenantUser);

        var result = await _handler.Handle(
            new SubscribeToActorCommand { Subscription = new SubscribeToActorDto { TargetActorId = Guid.NewGuid() } },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("An active tenant-local user is required before subscribing.");
        await _actorSubscriptionRepository.DidNotReceive().Create(Arg.Any<ActorSubscription>());
    }

    [Test]
    public async Task Handle_WithUnsubscribedExistingRow_ReactivatesSameSubscription()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantUser = CreateTenantUser(tenantId, userId);
        var targetActor = CreateActor(tenantId, ActorTypeEnum.Group);
        var existingSubscription = CreateSubscription(tenantId, tenantUser, targetActor, ActorSubscriptionStatusEnum.Unsubscribed);

        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(userId);
        _tenantUserRepository.GetByTenantAndUserAsync(tenantId, userId, Arg.Any<CancellationToken>()).Returns(tenantUser);
        _actorRepository.GetLocallyDiscoverableSubscriptionTargetAsync(
                tenantId,
                targetActor.Id,
                Arg.Any<CancellationToken>())
            .Returns(targetActor);
        _actorSubscriptionRepository.GetBySubscriberAndTargetAsync(tenantId, tenantUser.Id, targetActor.Id, true, Arg.Any<CancellationToken>())
            .Returns(existingSubscription);

        var result = await _handler.Handle(
            new SubscribeToActorCommand { Subscription = new SubscribeToActorDto { TargetActorId = targetActor.Id } },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(existingSubscription.Id);
        await Assert.That(existingSubscription.StatusId).IsEqualTo((int)ActorSubscriptionStatusEnum.Active);
        await Assert.That(existingSubscription.NotificationLevelId).IsEqualTo((int)ActorSubscriptionNotificationLevelEnum.All);
        await Assert.That(existingSubscription.UnsubscribedAt).IsNull();
        await _actorSubscriptionRepository.Received(1).Update(existingSubscription);
    }

    private static TenantUser CreateTenantUser(Guid tenantId, Guid userId, TenantUserStatusEnum status = TenantUserStatusEnum.Active)
    {
        return new TenantUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Tenant = null!,
            UserId = userId,
            User = null!,
            ActorId = Guid.NewGuid(),
            StatusId = (int)status
        };
    }

    private static Actor CreateActor(Guid tenantId, ActorTypeEnum actorType)
    {
        var actor = new Actor
        {
            Id = Guid.NewGuid(),
            ActorTypeId = (int)actorType,
            ActorType = null!,
            Pii = new ActorPii { ActorId = Guid.NewGuid(), DisplayName = "Target actor" }
        };
        if (actorType == ActorTypeEnum.Organization)
        {
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Pii = new OrganizationPii { FullName = "Target organization" },
                Actor = actor
            };
            organization.TenantParticipations.Add(new OrganizationTenant
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Tenant = null!,
                OrganizationId = organization.Id,
                Organization = organization,
                ApprovalStatus = null!
            });
            actor.OrganizationId = organization.Id;
            actor.Organization = organization;
        }
        else
        {
            var group = new Group { Id = Guid.NewGuid(), FullName = "Target group", Actor = actor };
            group.TenantParticipations.Add(new GroupTenant
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Tenant = null!,
                GroupId = group.Id,
                Group = group,
                ApprovalStatus = null!
            });
            actor.GroupId = group.Id;
            actor.Group = group;
        }

        return actor;
    }

    private static ActorSubscription CreateSubscription(Guid tenantId, TenantUser tenantUser, Actor targetActor, ActorSubscriptionStatusEnum status)
    {
        return new ActorSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Tenant = null!,
            SubscriberTenantUserId = tenantUser.Id,
            SubscriberTenantUser = tenantUser,
            SubscriberUserId = tenantUser.UserId,
            SubscriberUser = null!,
            TargetActorId = targetActor.Id,
            TargetActor = targetActor,
            TargetActorTypeId = targetActor.ActorTypeId,
            TargetActorType = null!,
            StatusId = (int)status,
            Status = null!,
            NotificationLevelId = (int)ActorSubscriptionNotificationLevelEnum.None,
            NotificationLevel = null!,
            SubscribedAt = DateTime.UtcNow.AddDays(-1),
            UnsubscribedAt = DateTime.UtcNow.AddHours(-1)
        };
    }
}
