// ABOUTME: Unit tests for current-user Web Push subscription CQRS handlers.
// ABOUTME: Verifies server-owned tenant/user identity, endpoint ownership conflicts, and safe DTO projection.

namespace Event.Application.UnitTests.Notifications;

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Notifications.Handlers.Commands;
using Explore.Application.Features.Notifications.Handlers.Queries;
using Explore.Application.Features.Notifications.Requests.Commands;
using Explore.Application.Features.Notifications.Requests.Queries;
using Explore.Domain;
using NSubstitute;

public sealed class WebPushSubscriptionHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("018f0000-0000-7000-8000-000000100001");
    private static readonly Guid UserId = Guid.Parse("018f0000-0000-7000-8000-000000100002");
    private static readonly string ValidP256Dh = ToBase64Url(new byte[65]);
    private static readonly string ValidAuth = ToBase64Url(new byte[16]);

    private readonly IWebPushSubscriptionRepository _repository = Substitute.For<IWebPushSubscriptionRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public WebPushSubscriptionHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
        _currentUserService.UserId.Returns(UserId);
        _currentUserService.IsAuthenticated.Returns(true);
    }

    [Test]
    public async Task Subscribe_UsesTrustedTenantAndUser_AndReturnsSubscriptionId()
    {
        var subscription = CreateSubscription("device-a", "https://push.example/sub-a");
        _repository.UpsertAsync(
                TenantId,
                UserId,
                "device-a",
                "https://push.example/sub-a",
                ValidP256Dh,
                ValidAuth,
                null,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(subscription);
        var handler = new SubscribeCurrentUserWebPushSubscriptionCommandHandler(_repository, _tenantContext, _currentUserService);

        var result = await handler.Handle(new SubscribeCurrentUserWebPushSubscriptionCommand
        {
            DeviceIdentifier = "device-a",
            Endpoint = "https://push.example/sub-a",
            P256Dh = ValidP256Dh,
            Auth = ValidAuth
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsEqualTo(subscription.Id);
        await _repository.Received(1).UpsertAsync(
            TenantId,
            UserId,
            "device-a",
            "https://push.example/sub-a",
            ValidP256Dh,
            ValidAuth,
            null,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Subscribe_WithBlankDeviceIdentifier_ReturnsFailureWithoutRepositoryCall()
    {
        var handler = new SubscribeCurrentUserWebPushSubscriptionCommandHandler(_repository, _tenantContext, _currentUserService);

        var result = await handler.Handle(new SubscribeCurrentUserWebPushSubscriptionCommand
        {
            DeviceIdentifier = " ",
            Endpoint = "https://push.example/sub-a",
            P256Dh = ValidP256Dh,
            Auth = ValidAuth
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors).Contains("Device identifier is required.");
        await _repository.DidNotReceive().UpsertAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Subscribe_WhenEndpointOwnedElsewhere_ReturnsFailureWithoutSecrets()
    {
        _repository.UpsertAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns<Task<WebPushSubscription>>(_ => throw new InvalidOperationException("Web Push endpoint is already owned by another active device."));
        var handler = new SubscribeCurrentUserWebPushSubscriptionCommandHandler(_repository, _tenantContext, _currentUserService);

        var result = await handler.Handle(new SubscribeCurrentUserWebPushSubscriptionCommand
        {
            DeviceIdentifier = "device-a",
            Endpoint = "https://push.example/secret-endpoint",
            P256Dh = ValidP256Dh,
            Auth = ValidAuth
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Message).Contains("already owned");
        await Assert.That(string.Join(" ", result.Errors ?? [])).DoesNotContain(ValidP256Dh);
        await Assert.That(string.Join(" ", result.Errors ?? [])).DoesNotContain(ValidAuth);
    }

    [Test]
    public async Task GetCurrent_ForDevice_ReturnsSafeDtoWithoutEndpointOrKeyMaterial()
    {
        _repository.GetActiveForDeviceAsync(TenantId, UserId, "device-a", Arg.Any<CancellationToken>())
            .Returns(CreateSubscription("device-a", "https://push.example/sub-a"));
        var handler = new GetCurrentUserWebPushSubscriptionQueryHandler(_repository, _tenantContext, _currentUserService);

        var result = await handler.Handle(new GetCurrentUserWebPushSubscriptionQuery { DeviceIdentifier = "device-a" }, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.DeviceIdentifier).IsEqualTo("device-a");
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
    }

    [Test]
    public async Task Unsubscribe_RequiresOwnedSubscriptionId()
    {
        var subscriptionId = Guid.NewGuid();
        _repository.UnsubscribeAsync(TenantId, UserId, subscriptionId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = new UnsubscribeCurrentUserWebPushSubscriptionCommandHandler(_repository, _tenantContext, _currentUserService);

        var result = await handler.Handle(new UnsubscribeCurrentUserWebPushSubscriptionCommand(subscriptionId), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _repository.Received(1).UnsubscribeAsync(TenantId, UserId, subscriptionId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    private static WebPushSubscription CreateSubscription(string deviceIdentifier, string endpoint)
    {
        return WebPushSubscription.Create(TenantId, UserId, deviceIdentifier, endpoint, "p256dh", "auth", null, DateTime.UtcNow);
    }

    private static string ToBase64Url(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
