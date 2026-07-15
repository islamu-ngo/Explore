// ABOUTME: Tests tenant-scoped resume of manually or automatically paused webhook endpoints.
// ABOUTME: Covers legal transitions, ineligible state, provider mode, and optimistic conflicts.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.Webhooks.Handlers.Commands;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Webhooks;

public sealed class ResumeWebhookEndpointCommandHandlerTests
{
    private static readonly DateTime ResumedAt =
        new(2026, 7, 14, 16, 30, 0, DateTimeKind.Utc);

    [Test]
    public async Task Handle_WhenEndpointIsAutoPaused_ResumesWithTenantAndActorIdentity()
    {
        var endpoint = CreateEndpoint(WebhookEndpointStatus.AutoPaused);
        var actorUserId = Guid.CreateVersion7();
        var repository = Substitute.For<IWebhookEndpointRepository>();
        repository.GetByIdForOwnerOperationAsync(endpoint.Id, false, Arg.Any<CancellationToken>())
            .Returns(endpoint);
        repository.TryResumeAsync(
                endpoint.TenantId,
                endpoint.Id,
                endpoint.DeliveryStateVersion,
                ResumedAt,
                actorUserId,
                Arg.Any<CancellationToken>())
            .Returns(true);
        var auditWriter = Substitute.For<IWebhookAuditEventWriter>();
        var handler = new ResumeWebhookEndpointCommandHandler(
            repository,
            auditWriter,
            new InlineUnitOfWork(),
            new FixedTimeProvider(ResumedAt));

        var result = await handler.Handle(new ResumeWebhookEndpointCommand
        {
            EndpointId = endpoint.Id,
            ActorUserId = actorUserId,
            ExpectedDeliveryStateVersion = endpoint.DeliveryStateVersion,
            ReasonCode = "operator.recovered"
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await repository.Received(1).TryResumeAsync(
            endpoint.TenantId,
            endpoint.Id,
            endpoint.DeliveryStateVersion,
            ResumedAt,
            actorUserId,
            Arg.Any<CancellationToken>());
        await auditWriter.Received(1).AppendAsync(
            Arg.Is<WebhookAuditWriteRequest>(audit =>
                audit.Action == WebhookAuditAction.EndpointResumed &&
                audit.ReasonCode == "operator.recovered" &&
                audit.PrincipalKind == WebhookAuditPrincipalKind.User &&
                audit.PrincipalReference == $"user:{actorUserId:D}"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenEndpointIsNotAutoPaused_FailsWithoutMutation()
    {
        var endpoint = CreateEndpoint(WebhookEndpointStatus.Active);
        var repository = Substitute.For<IWebhookEndpointRepository>();
        repository.GetByIdForOwnerOperationAsync(endpoint.Id, false, Arg.Any<CancellationToken>())
            .Returns(endpoint);
        var handler = new ResumeWebhookEndpointCommandHandler(
            repository,
            Substitute.For<IWebhookAuditEventWriter>(),
            new InlineUnitOfWork(),
            new FixedTimeProvider(ResumedAt));

        var result = await handler.Handle(new ResumeWebhookEndpointCommand
        {
            EndpointId = endpoint.Id,
            ActorUserId = Guid.CreateVersion7(),
            ExpectedDeliveryStateVersion = endpoint.DeliveryStateVersion,
            ReasonCode = "operator.recovered"
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_endpoint_not_paused");
        await repository.DidNotReceiveWithAnyArgs().TryResumeAsync(
            default,
            default,
            default,
            default,
            default,
            default);
    }

    [Test]
    public async Task Handle_WhenConditionalResumeLosesRace_ReturnsConflict()
    {
        var endpoint = CreateEndpoint(WebhookEndpointStatus.AutoPaused);
        var repository = Substitute.For<IWebhookEndpointRepository>();
        repository.GetByIdForOwnerOperationAsync(endpoint.Id, false, Arg.Any<CancellationToken>())
            .Returns(endpoint);
        repository.TryResumeAsync(
                endpoint.TenantId,
                endpoint.Id,
                endpoint.DeliveryStateVersion,
                Arg.Any<DateTime>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
        var handler = new ResumeWebhookEndpointCommandHandler(
            repository,
            Substitute.For<IWebhookAuditEventWriter>(),
            new InlineUnitOfWork(),
            new FixedTimeProvider(ResumedAt));

        var result = await handler.Handle(new ResumeWebhookEndpointCommand
        {
            EndpointId = endpoint.Id,
            ActorUserId = Guid.CreateVersion7(),
            ExpectedDeliveryStateVersion = endpoint.DeliveryStateVersion,
            ReasonCode = "operator.recovered"
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_endpoint_resume_conflict");
    }

    private static WebhookEndpoint CreateEndpoint(WebhookEndpointStatus status)
    {
        var tenantId = Guid.CreateVersion7();
        var consumer = new WebhookConsumer
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ConsumerKind = WebhookConsumerKind.Tenant,
            Name = "Operations",
            Status = WebhookConsumerStatus.Active,
            ProviderMode = WebhookProviderMode.Local,
            ConfigurationVersion = 1,
            CreatedAt = DateTime.UtcNow
        };
        return new WebhookEndpoint
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ConsumerId = consumer.Id,
            Consumer = consumer,
            Url = "https://integrator.example/webhook",
            Status = status,
            SecretRef = "resume-test-secret",
            SecretVersion = 1,
            MaxAttempts = 8,
            TimeoutSeconds = 15,
            CreatedAt = DateTime.UtcNow
        };
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class InlineUnitOfWork : IUnitOfWork
    {
        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default) =>
            await operation(ct);

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) =>
            await operation(ct);
    }
}
