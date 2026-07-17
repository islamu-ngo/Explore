// ABOUTME: Unit tests for typed-owner webhook consumer and endpoint management handlers.
// ABOUTME: Verifies canonical owner resolution, inherited child scope, authorization, and entity-first persistence.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.Webhooks.Handlers.Commands;
using Explore.Application.Features.Webhooks.Handlers.Queries;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Application.Features.Webhooks.Requests.Queries;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Webhooks;

public sealed class WebhookConsumerHandlersTests
{
    private readonly IWebhookConsumerRepository _consumerRepository =
        Substitute.For<IWebhookConsumerRepository>();
    private readonly IWebhookConsumerProviderBindingRepository _bindingRepository =
        Substitute.For<IWebhookConsumerProviderBindingRepository>();
    private readonly IWebhookEndpointRepository _endpointRepository =
        Substitute.For<IWebhookEndpointRepository>();
    private readonly IWebhookEventTypeRepository _eventTypeRepository =
        Substitute.For<IWebhookEventTypeRepository>();
    private readonly IWebhookOwnershipScopeResolver _ownershipScopeResolver =
        Substitute.For<IWebhookOwnershipScopeResolver>();
    private readonly IWebhookAuditEventWriter _auditWriter =
        Substitute.For<IWebhookAuditEventWriter>();
    private readonly IWebhookProviderCapabilityResolver _capabilityResolver =
        new LocalWebhookProviderCapabilityResolver();
    private readonly IUnitOfWork _unitOfWork = new ImmediateUnitOfWork();

    [Test]
    [Arguments(WebhookConsumerKind.Tenant)]
    [Arguments(WebhookConsumerKind.Organization)]
    [Arguments(WebhookConsumerKind.Group)]
    [Arguments(WebhookConsumerKind.User)]
    [Arguments(WebhookConsumerKind.Instance)]
    public async Task CreateConsumer_UsesResolvedCanonicalOwner(WebhookConsumerKind ownerKind)
    {
        var ownership = CreateOwnership(ownerKind);
        _ownershipScopeResolver.ResolveAsync(
                (int)ownerKind,
                ownership.OwnerId,
                Arg.Any<CancellationToken>())
            .Returns(WebhookOwnershipScopeResolution.Resolved(ownership));
        _consumerRepository.GetByOwnerAndNameAsync(
                ownership,
                "Operations",
                Arg.Any<CancellationToken>())
            .Returns((WebhookConsumer?)null);
        _consumerRepository.CreateAsync(
                Arg.Any<WebhookConsumer>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<WebhookConsumer>());
        var handler = new CreateWebhookConsumerCommandHandler(
            _consumerRepository,
            _ownershipScopeResolver,
            _capabilityResolver,
            _auditWriter,
            _unitOfWork);

        var result = await handler.Handle(
            new CreateWebhookConsumerCommand
            {
                ConsumerKindId = (int)ownerKind,
                OwnerId = ownership.OwnerId,
                Name = "  Operations  ",
                ProviderModeId = (int)WebhookProviderMode.Local
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _consumerRepository.Received(1).CreateAsync(
            Arg.Is<WebhookConsumer>(consumer =>
                consumer.ConsumerKind == ownerKind &&
                consumer.OwnerId == ownership.OwnerId &&
                consumer.TenantId == ownership.TenantId &&
                consumer.InstanceId == ownership.InstanceId &&
                consumer.OrganizationId == ownership.OrganizationId &&
                consumer.GroupId == ownership.GroupId &&
                consumer.OwnerUserId == ownership.UserId &&
                consumer.Name == "Operations"),
            Arg.Any<CancellationToken>());
        await _auditWriter.Received(1).AppendAsync(
            Arg.Is<WebhookAuditWriteRequest>(audit =>
                audit.TenantId == ownership.TenantId &&
                audit.EffectiveScopeKind == ownership.AuditScopeKind &&
                audit.EffectiveScopeId == ownership.OwnerId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateConsumer_WhenOwnerResolutionFails_DoesNotPersist()
    {
        var ownerId = Guid.CreateVersion7();
        _ownershipScopeResolver.ResolveAsync(
                (int)WebhookConsumerKind.Organization,
                ownerId,
                Arg.Any<CancellationToken>())
            .Returns(WebhookOwnershipScopeResolution.Failed(
                "webhook_owner_organization_not_found",
                "Organization owner was not found."));
        var handler = new CreateWebhookConsumerCommandHandler(
            _consumerRepository,
            _ownershipScopeResolver,
            _capabilityResolver,
            _auditWriter,
            _unitOfWork);

        var result = await handler.Handle(
            new CreateWebhookConsumerCommand
            {
                ConsumerKindId = (int)WebhookConsumerKind.Organization,
                OwnerId = ownerId,
                Name = "Operations",
                ProviderModeId = (int)WebhookProviderMode.Local
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_owner_organization_not_found");
        await _consumerRepository.DidNotReceive().CreateAsync(
            Arg.Any<WebhookConsumer>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(WebhookConsumerKind.Tenant)]
    [Arguments(WebhookConsumerKind.Organization)]
    [Arguments(WebhookConsumerKind.Group)]
    [Arguments(WebhookConsumerKind.User)]
    [Arguments(WebhookConsumerKind.Instance)]
    public async Task ListConsumers_UsesExactResolvedOwner(WebhookConsumerKind ownerKind)
    {
        var ownership = CreateOwnership(ownerKind);
        var consumer = CreateConsumer(ownership);
        _ownershipScopeResolver.ResolveAsync(
                (int)ownerKind,
                ownership.OwnerId,
                Arg.Any<CancellationToken>())
            .Returns(WebhookOwnershipScopeResolution.Resolved(ownership));
        _consumerRepository.ListByOwnerAsync(
                ownership,
                25,
                Arg.Any<CancellationToken>())
            .Returns([consumer]);
        var handler = new GetWebhookConsumersQueryHandler(
            _consumerRepository,
            _bindingRepository,
            _capabilityResolver,
            _ownershipScopeResolver);

        var result = await handler.Handle(
            new GetWebhookConsumersQuery
            {
                OwnerKindId = (int)ownerKind,
                OwnerId = ownership.OwnerId,
                Limit = 25
            },
            CancellationToken.None);

        await Assert.That(result).HasSingleItem();
        await Assert.That(result[0].ConsumerKindId).IsEqualTo((int)ownerKind);
        await Assert.That(result[0].OwnerId).IsEqualTo(ownership.OwnerId);
        await _consumerRepository.Received(1).ListByOwnerAsync(
            ownership,
            25,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ConsumerDetail_UsesPersistedOwnerRepositoryBoundary()
    {
        var consumer = CreateConsumer(CreateOwnership(WebhookConsumerKind.Group));
        _consumerRepository.GetByIdForOwnerOperationAsync(
                consumer.Id,
                false,
                Arg.Any<CancellationToken>())
            .Returns(consumer);
        var handler = new GetWebhookConsumerByIdQueryHandler(
            _consumerRepository,
            _bindingRepository,
            _capabilityResolver);

        var result = await handler.Handle(
            new GetWebhookConsumerByIdQuery { ConsumerId = consumer.Id },
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.OwnerId).IsEqualTo(consumer.OwnerId);
        await Assert.That(result.ConsumerKindId).IsEqualTo((int)WebhookConsumerKind.Group);
    }

    [Test]
    [Arguments(WebhookConsumerKind.Tenant)]
    [Arguments(WebhookConsumerKind.Organization)]
    [Arguments(WebhookConsumerKind.Group)]
    [Arguments(WebhookConsumerKind.User)]
    [Arguments(WebhookConsumerKind.Instance)]
    public async Task CreateEndpoint_InheritsConsumerConfigurationScope(WebhookConsumerKind ownerKind)
    {
        var ownership = CreateOwnership(ownerKind);
        var consumer = CreateConsumer(ownership);
        var eventType = CreateEventType();
        _consumerRepository.GetByIdForOwnerOperationAsync(
                consumer.Id,
                false,
                Arg.Any<CancellationToken>())
            .Returns(consumer);
        _endpointRepository.GetByConsumerAndUrlForOwnerOperationAsync(
                consumer.Id,
                "https://integrator.example/webhook",
                Arg.Any<CancellationToken>())
            .Returns((WebhookEndpoint?)null);
        _eventTypeRepository.GetByIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([eventType]);
        _endpointRepository.CreateWithSubscriptionsAsync(
                Arg.Any<WebhookEndpoint>(),
                Arg.Any<IReadOnlyCollection<WebhookEndpointSubscription>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<WebhookEndpoint>());
        var handler = new CreateWebhookEndpointCommandHandler(
            _endpointRepository,
            _consumerRepository,
            _eventTypeRepository,
            _capabilityResolver,
            _auditWriter,
            _unitOfWork);

        var result = await handler.Handle(
            new CreateWebhookEndpointCommand
            {
                ConsumerId = consumer.Id,
                Url = "https://integrator.example/webhook",
                SecretRef = "configuration:webhook:endpoint-secret",
                EventTypeIds = [eventType.Id]
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _endpointRepository.Received(1).CreateWithSubscriptionsAsync(
            Arg.Is<WebhookEndpoint>(endpoint =>
                endpoint.ConsumerId == consumer.Id &&
                endpoint.TenantId == ownership.TenantId &&
                endpoint.InstanceId == ownership.InstanceId),
            Arg.Is<IReadOnlyCollection<WebhookEndpointSubscription>>(subscriptions =>
                subscriptions.Count == 1 &&
                subscriptions.Single().TenantId == ownership.TenantId &&
                subscriptions.Single().InstanceId == ownership.InstanceId &&
                subscriptions.Single().EventTypeId == eventType.Id),
            Arg.Any<CancellationToken>());
        await _auditWriter.Received(1).AppendAsync(
            Arg.Is<WebhookAuditWriteRequest>(audit =>
                audit.TenantId == ownership.TenantId &&
                audit.EffectiveScopeKind == ownership.AuditScopeKind &&
                audit.EffectiveScopeId == ownership.OwnerId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(WebhookConsumerKind.Tenant)]
    [Arguments(WebhookConsumerKind.Organization)]
    [Arguments(WebhookConsumerKind.Group)]
    [Arguments(WebhookConsumerKind.User)]
    [Arguments(WebhookConsumerKind.Instance)]
    public async Task ListEndpoints_UsesExactOwnerAndConsumerFilter(WebhookConsumerKind ownerKind)
    {
        var ownership = CreateOwnership(ownerKind);
        var consumer = CreateConsumer(ownership);
        var endpoint = CreateEndpoint(consumer);
        _ownershipScopeResolver.ResolveAsync(
                (int)ownerKind,
                ownership.OwnerId,
                Arg.Any<CancellationToken>())
            .Returns(WebhookOwnershipScopeResolution.Resolved(ownership));
        _endpointRepository.ListByOwnerAsync(
                ownership,
                consumer.Id,
                50,
                Arg.Any<CancellationToken>())
            .Returns([endpoint]);
        var handler = new GetWebhookEndpointsQueryHandler(
            _endpointRepository,
            _ownershipScopeResolver);

        var result = await handler.Handle(
            new GetWebhookEndpointsQuery
            {
                OwnerKindId = (int)ownerKind,
                OwnerId = ownership.OwnerId,
                ConsumerId = consumer.Id,
                Limit = 50
            },
            CancellationToken.None);

        await Assert.That(result).HasSingleItem();
        await Assert.That(result[0].OwnerKindId).IsEqualTo((int)ownerKind);
        await Assert.That(result[0].OwnerId).IsEqualTo(ownership.OwnerId);
        await _endpointRepository.Received(1).ListByOwnerAsync(
            ownership,
            consumer.Id,
            50,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EndpointDetail_UsesPersistedOwnerRepositoryBoundary()
    {
        var consumer = CreateConsumer(CreateOwnership(WebhookConsumerKind.User));
        var endpoint = CreateEndpoint(consumer);
        _endpointRepository.GetByIdForOwnerOperationAsync(
                endpoint.Id,
                false,
                Arg.Any<CancellationToken>())
            .Returns(endpoint);
        var handler = new GetWebhookEndpointByIdQueryHandler(_endpointRepository);

        var result = await handler.Handle(
            new GetWebhookEndpointByIdQuery { EndpointId = endpoint.Id },
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.OwnerKindId).IsEqualTo((int)WebhookConsumerKind.User);
        await Assert.That(result.OwnerId).IsEqualTo(consumer.OwnerId);
    }

    [Test]
    [Arguments(typeof(CreateWebhookConsumerCommand), AuthorizationActions.Webhooks.Create)]
    [Arguments(typeof(GetWebhookConsumersQuery), AuthorizationActions.Webhooks.View)]
    [Arguments(typeof(GetWebhookConsumerByIdQuery), AuthorizationActions.Webhooks.View)]
    [Arguments(typeof(CreateWebhookEndpointCommand), AuthorizationActions.Webhooks.Create)]
    [Arguments(typeof(GetWebhookEndpointsQuery), AuthorizationActions.Webhooks.View)]
    [Arguments(typeof(GetWebhookEndpointByIdQuery), AuthorizationActions.Webhooks.View)]
    public async Task ManagementRequests_DeclareWebhookAuthorization(
        Type requestType,
        string expectedAction)
    {
        var attribute = requestType
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: true)
            .Cast<AuthorizeResourceAttribute>()
            .SingleOrDefault();

        await Assert.That(attribute).IsNotNull();
        await Assert.That(attribute!.Resource).IsEqualTo(ResourceKinds.Webhook);
        await Assert.That(attribute.Action).IsEqualTo(expectedAction);
        await Assert.That(typeof(ISecureRequest).IsAssignableFrom(requestType)).IsTrue();
    }

    private static WebhookOwnershipScope CreateOwnership(WebhookConsumerKind ownerKind)
    {
        var tenantId = Guid.CreateVersion7();
        var ownerId = Guid.CreateVersion7();
        return ownerKind switch
        {
            WebhookConsumerKind.Tenant => WebhookOwnershipScope.Create(
                ownerKind, ownerId, null, null, null, null),
            WebhookConsumerKind.Organization => WebhookOwnershipScope.Create(
                ownerKind, tenantId, null, ownerId, null, null),
            WebhookConsumerKind.Group => WebhookOwnershipScope.Create(
                ownerKind, tenantId, null, null, ownerId, null),
            WebhookConsumerKind.User => WebhookOwnershipScope.Create(
                ownerKind, tenantId, null, null, null, ownerId),
            WebhookConsumerKind.Instance => WebhookOwnershipScope.Create(
                ownerKind, null, ownerId, null, null, null),
            _ => throw new ArgumentOutOfRangeException(nameof(ownerKind))
        };
    }

    private static WebhookConsumer CreateConsumer(WebhookOwnershipScope ownership) =>
        WebhookConsumer.Create(
            ownership,
            "Operations",
            WebhookProviderMode.Local,
            DateTime.UtcNow);

    private static WebhookEventType CreateEventType() =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Name = "event.published",
            GroupName = "event",
            Description = "Raised when an event is published.",
            SchemaJson = "{}",
            SchemaVersion = 1,
            IsPublic = true,
            IsEnabled = true,
            PayloadRetentionDays = 14,
            CreatedAt = DateTime.UtcNow
        };

    private static WebhookEndpoint CreateEndpoint(WebhookConsumer consumer) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = consumer.TenantId,
            InstanceId = consumer.InstanceId,
            ConsumerId = consumer.Id,
            Consumer = consumer,
            Url = "https://integrator.example/webhook",
            Status = WebhookEndpointStatus.Active,
            SecretRef = "configuration:webhook:endpoint-secret",
            SecretVersion = 1,
            SecretActivatedAt = DateTime.UtcNow,
            ConfigurationVersion = 1,
            MaxAttempts = 8,
            TimeoutSeconds = 15,
            CreatedAt = DateTime.UtcNow
        };

    private sealed class LocalWebhookProviderCapabilityResolver : IWebhookProviderCapabilityResolver
    {
        public WebhookProviderModeCapabilityResolution Resolve(WebhookProviderMode providerMode) =>
            new(
                providerMode,
                true,
                WebhookProviderCapability.EndpointManagement |
                WebhookProviderCapability.EventCatalog,
                WebhookProviderCapability.None,
                null,
                null,
                "test-capability-v1",
                null);
    }

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default) =>
            operation(cancellationToken);

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) =>
            operation(cancellationToken);

        public Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) =>
            ExecuteInTransactionAsync(operation, cancellationToken);
    }
}
