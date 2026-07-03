// ABOUTME: Unit tests for Svix App Portal command handling.
// ABOUTME: Verifies validation and provider-neutral portal service mapping for webhook management.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.Webhooks.Handlers.Commands;
using Explore.Application.Features.Webhooks.Requests.Commands;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Webhooks;

public sealed class OpenSvixAppPortalCommandHandlerTests
{
    private readonly IWebhookProviderPortalService _portalService = Substitute.For<IWebhookProviderPortalService>();

    [Test]
    public async Task Command_RequiresWebhookOpenProviderPortalAuthorization()
    {
        var attribute = typeof(OpenSvixAppPortalCommand)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: true)
            .Cast<AuthorizeResourceAttribute>()
            .SingleOrDefault();

        await Assert.That(attribute).IsNotNull();
        await Assert.That(attribute!.Resource).IsEqualTo(ResourceKinds.Webhook);
        await Assert.That(attribute.Action).IsEqualTo(AuthorizationActions.Webhooks.OpenProviderPortal);
        await Assert.That(typeof(ISecureRequest).IsAssignableFrom(typeof(OpenSvixAppPortalCommand))).IsTrue();
    }

    [Test]
    public async Task Handle_WhenProviderSucceeds_ReturnsPortalAccessDto()
    {
        var tenantId = Guid.CreateVersion7();
        var consumerId = Guid.CreateVersion7();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        _portalService.CreateAccessAsync(Arg.Any<WebhookProviderPortalAccessInput>(), Arg.Any<CancellationToken>())
            .Returns(WebhookProviderPortalAccessResult.Success(
                "https://svix.example/app-portal/session",
                "portal-token",
                expiresAt));
        var handler = new OpenSvixAppPortalCommandHandler(_portalService);

        var result = await handler.Handle(
            new OpenSvixAppPortalCommand
            {
                TenantId = tenantId,
                ConsumerId = consumerId,
                SessionId = "session-1",
                ExpiresInSeconds = 900,
                FeatureFlags = ["endpoints"]
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotNull();
        await Assert.That(result.Id!.Url).IsEqualTo("https://svix.example/app-portal/session");
        await Assert.That(result.Id.Token).IsEqualTo("portal-token");
        await Assert.That(result.Id.ExpiresAt).IsEqualTo(expiresAt);
        await _portalService.Received(1).CreateAccessAsync(
            Arg.Is<WebhookProviderPortalAccessInput>(input =>
                input.TenantId == tenantId &&
                input.ConsumerId == consumerId &&
                input.SessionId == "session-1" &&
                input.ExpiresIn == TimeSpan.FromSeconds(900) &&
                input.FeatureFlags.Contains("endpoints")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenExpiresInSecondsInvalid_ReturnsValidationFailure()
    {
        var handler = new OpenSvixAppPortalCommandHandler(_portalService);

        var result = await handler.Handle(
            new OpenSvixAppPortalCommand
            {
                TenantId = Guid.CreateVersion7(),
                SessionId = "session-1",
                ExpiresInSeconds = 0
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_portal_validation_failed");
        await _portalService.DidNotReceive().CreateAccessAsync(
            Arg.Any<WebhookProviderPortalAccessInput>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenProviderFails_MapsFailureCategoryAndRetryability()
    {
        _portalService.CreateAccessAsync(Arg.Any<WebhookProviderPortalAccessInput>(), Arg.Any<CancellationToken>())
            .Returns(WebhookProviderPortalAccessResult.Failure(
                "svix_provider_unavailable",
                isRetryable: true,
                "SvixApi:503"));
        var handler = new OpenSvixAppPortalCommandHandler(_portalService);

        var result = await handler.Handle(
            new OpenSvixAppPortalCommand
            {
                TenantId = Guid.CreateVersion7(),
                SessionId = "session-1"
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("svix_provider_unavailable");
        await Assert.That(result.IsRetryable).IsTrue();
        await Assert.That(result.Errors).Contains("SvixApi:503");
    }
}
