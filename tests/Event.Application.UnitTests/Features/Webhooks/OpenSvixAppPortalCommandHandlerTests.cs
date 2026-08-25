// ABOUTME: Unit tests for Svix App Portal command handling.
// ABOUTME: Verifies validation and provider-neutral portal service mapping for webhook management.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.Webhooks.Handlers.Commands;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Webhooks;

public sealed class OpenSvixAppPortalCommandHandlerTests
{
    private readonly IWebhookProviderPortalService _portalService = Substitute.For<IWebhookProviderPortalService>();
    private readonly IWebhookAuditEventWriter _auditWriter = Substitute.For<IWebhookAuditEventWriter>();
    private readonly IWebhookConsumerRepository _consumerRepository = Substitute.For<IWebhookConsumerRepository>();

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
        var providerBindingId = Guid.CreateVersion7();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        _consumerRepository.GetByIdForOwnerOperationAsync(consumerId, false, Arg.Any<CancellationToken>())
            .Returns(CreateConsumer(tenantId, consumerId));
        _portalService.CreateAccessAsync(Arg.Any<WebhookProviderPortalAccessInput>(), Arg.Any<CancellationToken>())
            .Returns(WebhookProviderPortalAccessResult.Success(
                "https://svix.example/app-portal/session",
                "portal-token",
                expiresAt,
                providerBindingId,
                "portal-policy-v1"));
        var handler = CreateHandler();

        var result = await handler.Handle(
            new OpenSvixAppPortalCommand
            {
                ConsumerId = consumerId,
                SessionId = "session-1",
                ExpiresInSeconds = 900
            },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsNotNull();
        await Assert.That(result.Id!.Url).IsEqualTo("https://svix.example/app-portal/session");
        await Assert.That(result.Id.Token).IsEqualTo("portal-token");
        await Assert.That(result.Id.ExpiresAt).IsEqualTo(expiresAt);
        await _portalService.Received(1).CreateAccessAsync(
            Arg.Is<WebhookProviderPortalAccessInput>(input =>
                input.ConsumerId == consumerId &&
                input.SessionId == "session-1" &&
                input.ExpiresIn == TimeSpan.FromSeconds(900)),
            Arg.Any<CancellationToken>());
        await _auditWriter.Received(1).AppendAsync(
            Arg.Is<WebhookAuditWriteRequest>(audit =>
                audit.TenantId == tenantId &&
                audit.TargetId == consumerId &&
                audit.Action == WebhookAuditAction.PortalAccessIssued &&
                audit.SafeAfterJson != null &&
                audit.SafeAfterJson.Contains(providerBindingId.ToString("D"), StringComparison.OrdinalIgnoreCase)),
            CancellationToken.None);
    }

    [Test]
    public async Task Handle_WhenExpiresInSecondsInvalid_ReturnsValidationFailure()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new OpenSvixAppPortalCommand
            {
                SessionId = "session-1",
                ExpiresInSeconds = 0
            },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
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
        var consumerId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        _consumerRepository.GetByIdForOwnerOperationAsync(consumerId, false, Arg.Any<CancellationToken>())
            .Returns(CreateConsumer(tenantId, consumerId));
        var handler = CreateHandler();

        var result = await handler.Handle(
            new OpenSvixAppPortalCommand
            {
                ConsumerId = consumerId,
                SessionId = "session-1"
            },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("svix_provider_unavailable");
        await Assert.That(result.IsRetryable).IsTrue();
        await Assert.That(result.Errors).Contains("SvixApi:503");
        await _auditWriter.Received(1).AppendAsync(
            Arg.Is<WebhookAuditWriteRequest>(audit =>
                audit.Action == WebhookAuditAction.PortalAccessRejected &&
                audit.SafeAfterJson != null &&
                audit.SafeAfterJson.Contains("svix_provider_unavailable", StringComparison.Ordinal) &&
                !audit.SafeAfterJson.Contains("SvixApi:503", StringComparison.Ordinal)),
            CancellationToken.None);
    }

    private OpenSvixAppPortalCommandHandler CreateHandler() =>
        new(_portalService, _auditWriter, _consumerRepository);

    private static WebhookConsumer CreateConsumer(Guid tenantId, Guid consumerId) =>
        new()
        {
            Id = consumerId,
            TenantId = tenantId,
            ConsumerKind = WebhookConsumerKind.Tenant,
            Name = "Portal consumer",
            Status = WebhookConsumerStatus.Active,
            ProviderMode = WebhookProviderMode.Svix,
            ConfigurationVersion = 1,
            CreatedAt = DateTime.UtcNow
        };
}
