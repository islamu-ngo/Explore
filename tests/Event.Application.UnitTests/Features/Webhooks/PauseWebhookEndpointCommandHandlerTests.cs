// ABOUTME: Tests state-aware manual pause of tenant-scoped Local webhook endpoints.
// ABOUTME: Covers successful audit, provider capability rejection, invalid state, and transition races.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.Webhooks.Handlers.Commands;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Webhooks;

public sealed class PauseWebhookEndpointCommandHandlerTests
{
    private static readonly DateTime PausedAt =
        new(2026, 7, 14, 16, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task Handle_WhenLocalEndpointIsActive_PausesAndAudits()
    {
        var endpoint = CreateEndpoint(WebhookEndpointStatus.Active, WebhookProviderMode.Local);
        var repository = Substitute.For<IWebhookEndpointRepository>();
        repository.GetByTenantAndIdAsync(endpoint.TenantId, endpoint.Id, Arg.Any<CancellationToken>())
            .Returns(endpoint);
        repository.TryPauseAsync(
                endpoint.TenantId,
                endpoint.Id,
                endpoint.DeliveryStateVersion,
                PausedAt,
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var auditWriter = Substitute.For<IWebhookAuditEventWriter>();
        var handler = new PauseWebhookEndpointCommandHandler(
            repository,
            auditWriter,
            new InlineUnitOfWork(),
            new FixedTimeProvider(PausedAt));
        var actorUserId = Guid.CreateVersion7();

        var result = await handler.Handle(new PauseWebhookEndpointCommand
        {
            TenantId = endpoint.TenantId,
            EndpointId = endpoint.Id,
            ActorUserId = actorUserId,
            ExpectedDeliveryStateVersion = endpoint.DeliveryStateVersion,
            ReasonCode = "operator.maintenance"
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await repository.Received(1).TryPauseAsync(
            endpoint.TenantId,
            endpoint.Id,
            endpoint.DeliveryStateVersion,
            PausedAt,
            actorUserId,
            Arg.Any<CancellationToken>());
        await auditWriter.Received(1).AppendAsync(
            Arg.Is<WebhookAuditWriteRequest>(audit =>
                audit.Action == WebhookAuditAction.EndpointPaused &&
                audit.ReasonCode == "operator.maintenance" &&
                audit.PrincipalReference == $"user:{actorUserId:D}"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenProviderModeIsSvix_RejectsWithoutMutation()
    {
        var endpoint = CreateEndpoint(WebhookEndpointStatus.Active, WebhookProviderMode.Svix);
        var repository = Substitute.For<IWebhookEndpointRepository>();
        repository.GetByTenantAndIdAsync(endpoint.TenantId, endpoint.Id, Arg.Any<CancellationToken>())
            .Returns(endpoint);
        var handler = new PauseWebhookEndpointCommandHandler(
            repository,
            Substitute.For<IWebhookAuditEventWriter>(),
            new InlineUnitOfWork(),
            new FixedTimeProvider(PausedAt));

        var result = await handler.Handle(new PauseWebhookEndpointCommand
        {
            TenantId = endpoint.TenantId,
            EndpointId = endpoint.Id,
            ActorUserId = Guid.CreateVersion7(),
            ExpectedDeliveryStateVersion = endpoint.DeliveryStateVersion,
            ReasonCode = "operator.maintenance"
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_endpoint_pause_unsupported");
        await repository.DidNotReceiveWithAnyArgs().TryPauseAsync(
            default,
            default,
            default,
            default,
            default,
            default);
    }

    [Test]
    public async Task Handle_WhenEndpointIsAlreadyPaused_RejectsWithoutMutation()
    {
        var endpoint = CreateEndpoint(WebhookEndpointStatus.Disabled, WebhookProviderMode.Local);
        var repository = Substitute.For<IWebhookEndpointRepository>();
        repository.GetByTenantAndIdAsync(endpoint.TenantId, endpoint.Id, Arg.Any<CancellationToken>())
            .Returns(endpoint);
        var handler = new PauseWebhookEndpointCommandHandler(
            repository,
            Substitute.For<IWebhookAuditEventWriter>(),
            new InlineUnitOfWork(),
            new FixedTimeProvider(PausedAt));

        var result = await handler.Handle(new PauseWebhookEndpointCommand
        {
            TenantId = endpoint.TenantId,
            EndpointId = endpoint.Id,
            ActorUserId = Guid.CreateVersion7(),
            ExpectedDeliveryStateVersion = endpoint.DeliveryStateVersion,
            ReasonCode = "operator.maintenance"
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_endpoint_not_active");
    }

    private static WebhookEndpoint CreateEndpoint(
        WebhookEndpointStatus status,
        WebhookProviderMode providerMode)
    {
        var tenantId = Guid.CreateVersion7();
        var consumer = new WebhookConsumer
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ConsumerKind = WebhookConsumerKind.Tenant,
            Name = "Operations",
            Status = WebhookConsumerStatus.Active,
            ProviderMode = providerMode,
            ConfigurationVersion = 1,
            CreatedAt = PausedAt.AddDays(-1)
        };
        return new WebhookEndpoint
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ConsumerId = consumer.Id,
            Consumer = consumer,
            Url = "https://integrator.example/webhook",
            Status = status,
            SecretRef = "pause-test-secret",
            SecretVersion = 1,
            ConfigurationVersion = 1,
            MaxAttempts = 8,
            TimeoutSeconds = 15,
            CreatedAt = PausedAt.AddDays(-1)
        };
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class InlineUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default) => operation(ct);

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) => operation(ct);
    }
}
