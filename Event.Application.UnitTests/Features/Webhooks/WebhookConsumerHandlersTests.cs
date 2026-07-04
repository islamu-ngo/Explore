// ABOUTME: Unit tests for webhook consumer command and query handlers.
// ABOUTME: Verifies tenant-scoped validation, entity persistence, and DTO projection behavior.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.Webhooks.Handlers.Commands;
using Explore.Application.Features.Webhooks.Handlers.Queries;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Application.Features.Webhooks.Requests.Queries;
using Explore.Application.Webhooks;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Webhooks;

public sealed class WebhookConsumerHandlersTests
{
    private readonly IWebhookConsumerRepository _consumerRepository = Substitute.For<IWebhookConsumerRepository>();
    private readonly IWebhookEndpointRepository _endpointRepository = Substitute.For<IWebhookEndpointRepository>();
    private readonly IWebhookEventTypeRepository _eventTypeRepository = Substitute.For<IWebhookEventTypeRepository>();
    private readonly IWebhookMessageRepository _messageRepository = Substitute.For<IWebhookMessageRepository>();
    private readonly IWebhookDeliveryAttemptRepository _attemptRepository = Substitute.For<IWebhookDeliveryAttemptRepository>();
    private readonly IWebhookDeliveryDrainService _deliveryDrainService = Substitute.For<IWebhookDeliveryDrainService>();
    private readonly IWebhookPayloadBuilder _payloadBuilder = new DefaultWebhookPayloadBuilder(new WebhookEventTypeRegistry());

    [Test]
    public async Task CreateCommand_RequiresWebhookCreateAuthorization()
    {
        var attribute = typeof(CreateWebhookConsumerCommand)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: true)
            .Cast<AuthorizeResourceAttribute>()
            .SingleOrDefault();

        await Assert.That(attribute).IsNotNull();
        await Assert.That(attribute!.Resource).IsEqualTo(ResourceKinds.Webhook);
        await Assert.That(attribute.Action).IsEqualTo(AuthorizationActions.Webhooks.Create);
        await Assert.That(typeof(ISecureRequest).IsAssignableFrom(typeof(CreateWebhookConsumerCommand))).IsTrue();
    }

    [Test]
    public async Task QueryRequests_RequireWebhookViewAuthorization()
    {
        var listAttribute = typeof(GetWebhookConsumersQuery)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: true)
            .Cast<AuthorizeResourceAttribute>()
            .SingleOrDefault();
        var detailAttribute = typeof(GetWebhookConsumerByIdQuery)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: true)
            .Cast<AuthorizeResourceAttribute>()
            .SingleOrDefault();

        await Assert.That(listAttribute).IsNotNull();
        await Assert.That(listAttribute!.Resource).IsEqualTo(ResourceKinds.Webhook);
        await Assert.That(listAttribute.Action).IsEqualTo(AuthorizationActions.Webhooks.View);
        await Assert.That(detailAttribute).IsNotNull();
        await Assert.That(detailAttribute!.Resource).IsEqualTo(ResourceKinds.Webhook);
        await Assert.That(detailAttribute.Action).IsEqualTo(AuthorizationActions.Webhooks.View);
    }

    [Test]
    public async Task EndpointRequests_RequireWebhookAuthorization()
    {
        var listAttribute = typeof(GetWebhookEndpointsQuery)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: true)
            .Cast<AuthorizeResourceAttribute>()
            .SingleOrDefault();
        var detailAttribute = typeof(GetWebhookEndpointByIdQuery)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: true)
            .Cast<AuthorizeResourceAttribute>()
            .SingleOrDefault();
        var createAttribute = typeof(CreateWebhookEndpointCommand)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: true)
            .Cast<AuthorizeResourceAttribute>()
            .SingleOrDefault();
        var updateAttribute = typeof(UpdateWebhookEndpointCommand)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: true)
            .Cast<AuthorizeResourceAttribute>()
            .SingleOrDefault();
        var rotateAttribute = typeof(RotateWebhookEndpointSecretCommand)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: true)
            .Cast<AuthorizeResourceAttribute>()
            .SingleOrDefault();
        var testAttribute = typeof(TestWebhookEndpointCommand)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: true)
            .Cast<AuthorizeResourceAttribute>()
            .SingleOrDefault();
        var archiveAttribute = typeof(ArchiveWebhookEndpointCommand)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: true)
            .Cast<AuthorizeResourceAttribute>()
            .SingleOrDefault();

        await Assert.That(listAttribute).IsNotNull();
        await Assert.That(listAttribute!.Resource).IsEqualTo(ResourceKinds.Webhook);
        await Assert.That(listAttribute.Action).IsEqualTo(AuthorizationActions.Webhooks.View);
        await Assert.That(detailAttribute).IsNotNull();
        await Assert.That(detailAttribute!.Action).IsEqualTo(AuthorizationActions.Webhooks.View);
        await Assert.That(createAttribute).IsNotNull();
        await Assert.That(createAttribute!.Resource).IsEqualTo(ResourceKinds.Webhook);
        await Assert.That(createAttribute.Action).IsEqualTo(AuthorizationActions.Webhooks.Create);
        await Assert.That(updateAttribute).IsNotNull();
        await Assert.That(updateAttribute!.Resource).IsEqualTo(ResourceKinds.Webhook);
        await Assert.That(updateAttribute.Action).IsEqualTo(AuthorizationActions.Webhooks.Update);
        await Assert.That(rotateAttribute).IsNotNull();
        await Assert.That(rotateAttribute!.Resource).IsEqualTo(ResourceKinds.Webhook);
        await Assert.That(rotateAttribute.Action).IsEqualTo(AuthorizationActions.Webhooks.RotateSecret);
        await Assert.That(testAttribute).IsNotNull();
        await Assert.That(testAttribute!.Resource).IsEqualTo(ResourceKinds.Webhook);
        await Assert.That(testAttribute.Action).IsEqualTo(AuthorizationActions.Webhooks.Test);
        await Assert.That(archiveAttribute).IsNotNull();
        await Assert.That(archiveAttribute!.Resource).IsEqualTo(ResourceKinds.Webhook);
        await Assert.That(archiveAttribute.Action).IsEqualTo(AuthorizationActions.Webhooks.Delete);
    }

    [Test]
    public async Task DeliveryAuditRequests_RequireWebhookDeliveryAuthorization()
    {
        var listMessagesAttribute = typeof(GetWebhookMessagesQuery)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: true)
            .Cast<AuthorizeResourceAttribute>()
            .SingleOrDefault();
        var detailMessageAttribute = typeof(GetWebhookMessageByIdQuery)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: true)
            .Cast<AuthorizeResourceAttribute>()
            .SingleOrDefault();
        var listAttemptsAttribute = typeof(GetWebhookDeliveryAttemptsQuery)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: true)
            .Cast<AuthorizeResourceAttribute>()
            .SingleOrDefault();
        var detailAttemptAttribute = typeof(GetWebhookDeliveryAttemptByIdQuery)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: true)
            .Cast<AuthorizeResourceAttribute>()
            .SingleOrDefault();
        var retryAttribute = typeof(RetryWebhookDeliveryAttemptCommand)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: true)
            .Cast<AuthorizeResourceAttribute>()
            .SingleOrDefault();

        await Assert.That(listMessagesAttribute).IsNotNull();
        await Assert.That(listMessagesAttribute!.Resource).IsEqualTo(ResourceKinds.Webhook);
        await Assert.That(listMessagesAttribute.Action).IsEqualTo(AuthorizationActions.Webhooks.ViewDelivery);
        await Assert.That(detailMessageAttribute).IsNotNull();
        await Assert.That(detailMessageAttribute!.Action).IsEqualTo(AuthorizationActions.Webhooks.ViewDelivery);
        await Assert.That(listAttemptsAttribute).IsNotNull();
        await Assert.That(listAttemptsAttribute!.Action).IsEqualTo(AuthorizationActions.Webhooks.ViewDelivery);
        await Assert.That(detailAttemptAttribute).IsNotNull();
        await Assert.That(detailAttemptAttribute!.Action).IsEqualTo(AuthorizationActions.Webhooks.ViewDelivery);
        await Assert.That(retryAttribute).IsNotNull();
        await Assert.That(retryAttribute!.Resource).IsEqualTo(ResourceKinds.Webhook);
        await Assert.That(retryAttribute.Action).IsEqualTo(AuthorizationActions.Webhooks.Retry);
        await Assert.That(typeof(ISecureRequest).IsAssignableFrom(typeof(GetWebhookMessagesQuery))).IsTrue();
        await Assert.That(typeof(ISecureRequest).IsAssignableFrom(typeof(GetWebhookMessageByIdQuery))).IsTrue();
        await Assert.That(typeof(ISecureRequest).IsAssignableFrom(typeof(GetWebhookDeliveryAttemptsQuery))).IsTrue();
        await Assert.That(typeof(ISecureRequest).IsAssignableFrom(typeof(GetWebhookDeliveryAttemptByIdQuery))).IsTrue();
        await Assert.That(typeof(ISecureRequest).IsAssignableFrom(typeof(RetryWebhookDeliveryAttemptCommand))).IsTrue();
    }

    [Test]
    public async Task CreateHandler_WhenRequestValid_PersistsActiveConsumer()
    {
        var tenantId = Guid.CreateVersion7();
        WebhookConsumer? captured = null;
        var persisted = CreateConsumer(tenantId, "Tenant automation", WebhookProviderMode.Local);
        _consumerRepository.GetByTenantAndNameAsync(tenantId, "Tenant automation", Arg.Any<CancellationToken>())
            .Returns((WebhookConsumer?)null);
        _consumerRepository.CreateAsync(Arg.Do<WebhookConsumer>(consumer => captured = consumer), Arg.Any<CancellationToken>())
            .Returns(persisted);
        var handler = new CreateWebhookConsumerCommandHandler(_consumerRepository);

        var result = await handler.Handle(
            new CreateWebhookConsumerCommand
            {
                TenantId = tenantId,
                ConsumerKindId = (int)WebhookConsumerKind.Tenant,
                Name = " Tenant automation ",
                ProviderModeId = (int)WebhookProviderMode.Local,
                ExternalProviderAppId = " tenant-app "
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.TenantId).IsEqualTo(tenantId);
        await Assert.That(captured.Name).IsEqualTo("Tenant automation");
        await Assert.That(captured.ConsumerKind).IsEqualTo(WebhookConsumerKind.Tenant);
        await Assert.That(captured.Status).IsEqualTo(WebhookConsumerStatus.Active);
        await Assert.That(captured.ProviderMode).IsEqualTo(WebhookProviderMode.Local);
        await Assert.That(captured.ExternalProviderAppId).IsEqualTo("tenant-app");
    }

    [Test]
    public async Task CreateHandler_WhenNameAlreadyExists_ReturnsConflictFailure()
    {
        var tenantId = Guid.CreateVersion7();
        _consumerRepository.GetByTenantAndNameAsync(tenantId, "Tenant automation", Arg.Any<CancellationToken>())
            .Returns(new WebhookConsumer
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenantId,
                ConsumerKind = WebhookConsumerKind.Tenant,
                Name = "Tenant automation",
                Status = WebhookConsumerStatus.Active,
                ProviderMode = WebhookProviderMode.Local
            });
        var handler = new CreateWebhookConsumerCommandHandler(_consumerRepository);

        var result = await handler.Handle(
            new CreateWebhookConsumerCommand
            {
                TenantId = tenantId,
                ConsumerKindId = (int)WebhookConsumerKind.Tenant,
                Name = "Tenant automation",
                ProviderModeId = (int)WebhookProviderMode.Local
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_consumer_name_conflict");
        await _consumerRepository.DidNotReceive().CreateAsync(
            Arg.Any<WebhookConsumer>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateHandler_WhenRequestInvalid_DoesNotQueryRepository()
    {
        var handler = new CreateWebhookConsumerCommandHandler(_consumerRepository);

        var result = await handler.Handle(
            new CreateWebhookConsumerCommand
            {
                TenantId = Guid.Empty,
                ConsumerKindId = 999,
                Name = "",
                ProviderModeId = 999
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_consumer_validation_failed");
        await _consumerRepository.DidNotReceive().GetByTenantAndNameAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListHandler_MapsConsumersAndCapsLimit()
    {
        var tenantId = Guid.CreateVersion7();
        var consumer = CreateConsumer(tenantId, "Tenant automation", WebhookProviderMode.Svix);
        _consumerRepository.ListByTenantAsync(tenantId, 500, Arg.Any<CancellationToken>())
            .Returns([consumer]);
        var handler = new GetWebhookConsumersQueryHandler(_consumerRepository);

        var result = await handler.Handle(
            new GetWebhookConsumersQuery
            {
                TenantId = tenantId,
                Limit = 10_000
            },
            CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Id).IsEqualTo(consumer.Id);
        await Assert.That(result[0].ProviderModeId).IsEqualTo((int)WebhookProviderMode.Svix);
        await Assert.That(result[0].ProviderModeName).IsEqualTo(nameof(WebhookProviderMode.Svix));
        await _consumerRepository.Received(1).ListByTenantAsync(
            tenantId,
            500,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DetailHandler_WhenConsumerExists_ReturnsMappedDto()
    {
        var tenantId = Guid.CreateVersion7();
        var consumer = CreateConsumer(tenantId, "Tenant automation", WebhookProviderMode.Local);
        _consumerRepository.GetByTenantAndIdAsync(tenantId, consumer.Id, Arg.Any<CancellationToken>())
            .Returns(consumer);
        var handler = new GetWebhookConsumerByIdQueryHandler(_consumerRepository);

        var result = await handler.Handle(
            new GetWebhookConsumerByIdQuery
            {
                TenantId = tenantId,
                ConsumerId = consumer.Id
            },
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("Tenant automation");
        await Assert.That(result.StatusName).IsEqualTo(nameof(WebhookConsumerStatus.Active));
    }

    [Test]
    public async Task CreateEndpointHandler_WhenRequestValid_PersistsActiveEndpointWithSubscriptions()
    {
        var tenantId = Guid.CreateVersion7();
        var consumer = CreateConsumer(tenantId, "Tenant automation", WebhookProviderMode.Local);
        var eventType = CreateEventType("event.published");
        WebhookEndpoint? capturedEndpoint = null;
        IReadOnlyCollection<WebhookEndpointSubscription>? capturedSubscriptions = null;
        _consumerRepository.GetByTenantAndIdAsync(tenantId, consumer.Id, Arg.Any<CancellationToken>())
            .Returns(consumer);
        _endpointRepository.GetByTenantConsumerAndUrlAsync(
                tenantId,
                consumer.Id,
                "https://integrator.example/webhooks/islamu",
                Arg.Any<CancellationToken>())
            .Returns((WebhookEndpoint?)null);
        _eventTypeRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([eventType]);
        _endpointRepository.CreateWithSubscriptionsAsync(
                Arg.Do<WebhookEndpoint>(endpoint => capturedEndpoint = endpoint),
                Arg.Do<IReadOnlyCollection<WebhookEndpointSubscription>>(subscriptions => capturedSubscriptions = subscriptions),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<WebhookEndpoint>());
        var handler = new CreateWebhookEndpointCommandHandler(
            _endpointRepository,
            _consumerRepository,
            _eventTypeRepository);

        var result = await handler.Handle(
            new CreateWebhookEndpointCommand
            {
                TenantId = tenantId,
                ConsumerId = consumer.Id,
                Url = " https://integrator.example/webhooks/islamu ",
                Description = " Integrator endpoint ",
                SecretRef = " configuration:Webhooks:EndpointSecrets:integrator ",
                EventTypeIds = [eventType.Id],
                MaxAttempts = 8,
                TimeoutSeconds = 15,
                RateLimitPerMinute = 60
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(capturedEndpoint).IsNotNull();
        await Assert.That(capturedEndpoint!.TenantId).IsEqualTo(tenantId);
        await Assert.That(capturedEndpoint.ConsumerId).IsEqualTo(consumer.Id);
        await Assert.That(capturedEndpoint.Url).IsEqualTo("https://integrator.example/webhooks/islamu");
        await Assert.That(capturedEndpoint.Description).IsEqualTo("Integrator endpoint");
        await Assert.That(capturedEndpoint.SecretRef).IsEqualTo("configuration:Webhooks:EndpointSecrets:integrator");
        await Assert.That(capturedEndpoint.Status).IsEqualTo(WebhookEndpointStatus.Active);
        await Assert.That(capturedEndpoint.SecretVersion).IsEqualTo(1);
        await Assert.That(capturedSubscriptions).IsNotNull();
        await Assert.That(capturedSubscriptions!.Count).IsEqualTo(1);
        await Assert.That(capturedSubscriptions.Single().EventTypeId).IsEqualTo(eventType.Id);
        await Assert.That(capturedSubscriptions.Single().IsEnabled).IsTrue();
    }

    [Test]
    public async Task CreateEndpointHandler_WhenRequestInvalid_DoesNotQueryRepositories()
    {
        var handler = new CreateWebhookEndpointCommandHandler(
            _endpointRepository,
            _consumerRepository,
            _eventTypeRepository);

        var result = await handler.Handle(
            new CreateWebhookEndpointCommand
            {
                TenantId = Guid.Empty,
                ConsumerId = Guid.Empty,
                Url = "ftp://example.invalid/hook",
                SecretRef = "",
                EventTypeIds = []
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_endpoint_validation_failed");
        await _consumerRepository.DidNotReceive().GetByTenantAndIdAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        await _endpointRepository.DidNotReceive().CreateWithSubscriptionsAsync(
            Arg.Any<WebhookEndpoint>(),
            Arg.Any<IReadOnlyCollection<WebhookEndpointSubscription>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateEndpointHandler_WhenConsumerMissing_ReturnsNotFoundFailure()
    {
        var tenantId = Guid.CreateVersion7();
        var consumerId = Guid.CreateVersion7();
        _consumerRepository.GetByTenantAndIdAsync(tenantId, consumerId, Arg.Any<CancellationToken>())
            .Returns((WebhookConsumer?)null);
        var handler = new CreateWebhookEndpointCommandHandler(
            _endpointRepository,
            _consumerRepository,
            _eventTypeRepository);

        var result = await handler.Handle(CreateEndpointCommand(tenantId, consumerId, [Guid.CreateVersion7()]), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_consumer_not_found");
        await _endpointRepository.DidNotReceive().CreateWithSubscriptionsAsync(
            Arg.Any<WebhookEndpoint>(),
            Arg.Any<IReadOnlyCollection<WebhookEndpointSubscription>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateEndpointHandler_WhenEventTypeMissing_ReturnsInvalidEventTypeFailure()
    {
        var tenantId = Guid.CreateVersion7();
        var consumer = CreateConsumer(tenantId, "Tenant automation", WebhookProviderMode.Local);
        _consumerRepository.GetByTenantAndIdAsync(tenantId, consumer.Id, Arg.Any<CancellationToken>())
            .Returns(consumer);
        _endpointRepository.GetByTenantConsumerAndUrlAsync(
                tenantId,
                consumer.Id,
                "https://integrator.example/webhooks/islamu",
                Arg.Any<CancellationToken>())
            .Returns((WebhookEndpoint?)null);
        _eventTypeRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var handler = new CreateWebhookEndpointCommandHandler(
            _endpointRepository,
            _consumerRepository,
            _eventTypeRepository);

        var result = await handler.Handle(CreateEndpointCommand(tenantId, consumer.Id, [Guid.CreateVersion7()]), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_endpoint_event_types_invalid");
        await _endpointRepository.DidNotReceive().CreateWithSubscriptionsAsync(
            Arg.Any<WebhookEndpoint>(),
            Arg.Any<IReadOnlyCollection<WebhookEndpointSubscription>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EndpointListHandler_MapsEndpointsAndCapsLimit()
    {
        var tenantId = Guid.CreateVersion7();
        var endpoint = CreateEndpoint(tenantId, CreateConsumer(tenantId, "Tenant automation", WebhookProviderMode.Local));
        _endpointRepository.ListByTenantAsync(tenantId, endpoint.ConsumerId, 500, Arg.Any<CancellationToken>())
            .Returns([endpoint]);
        var handler = new GetWebhookEndpointsQueryHandler(_endpointRepository);

        var result = await handler.Handle(
            new GetWebhookEndpointsQuery
            {
                TenantId = tenantId,
                ConsumerId = endpoint.ConsumerId,
                Limit = 10_000
            },
            CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Id).IsEqualTo(endpoint.Id);
        await Assert.That(result[0].ProviderModeId).IsEqualTo((int)WebhookProviderMode.Local);
        await Assert.That(result[0].ProviderModeName).IsEqualTo(nameof(WebhookProviderMode.Local));
        await Assert.That(result[0].SecretVersion).IsEqualTo(1);
        await Assert.That(result[0].Subscriptions.Count).IsEqualTo(1);
        await _endpointRepository.Received(1).ListByTenantAsync(
            tenantId,
            endpoint.ConsumerId,
            500,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EndpointDetailHandler_WhenEndpointExists_ReturnsMappedDto()
    {
        var tenantId = Guid.CreateVersion7();
        var endpoint = CreateEndpoint(tenantId, CreateConsumer(tenantId, "Tenant automation", WebhookProviderMode.Local));
        _endpointRepository.GetByTenantAndIdAsync(tenantId, endpoint.Id, Arg.Any<CancellationToken>())
            .Returns(endpoint);
        var handler = new GetWebhookEndpointByIdQueryHandler(_endpointRepository);

        var result = await handler.Handle(
            new GetWebhookEndpointByIdQuery
            {
                TenantId = tenantId,
                EndpointId = endpoint.Id
            },
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Url).IsEqualTo(endpoint.Url);
        await Assert.That(result.StatusName).IsEqualTo(nameof(WebhookEndpointStatus.Active));
        await Assert.That(result.ProviderModeName).IsEqualTo(nameof(WebhookProviderMode.Local));
        await Assert.That(result.Subscriptions.Single().EventTypeName).IsEqualTo("event.published");
    }

    [Test]
    public async Task UpdateEndpointHandler_WhenRequestValid_UpdatesEndpointAndReplacesSubscriptions()
    {
        var tenantId = Guid.CreateVersion7();
        var endpoint = CreateEndpoint(tenantId, CreateConsumer(tenantId, "Tenant automation", WebhookProviderMode.Local));
        var replacementEventType = CreateEventType("registration.created");
        IReadOnlyCollection<WebhookEndpointSubscription>? capturedSubscriptions = null;
        _endpointRepository.GetByTenantAndIdForUpdateAsync(tenantId, endpoint.Id, Arg.Any<CancellationToken>())
            .Returns(endpoint);
        _endpointRepository.GetByTenantConsumerAndUrlAsync(
                tenantId,
                endpoint.ConsumerId,
                "https://integrator.example/hooks/updated",
                Arg.Any<CancellationToken>())
            .Returns((WebhookEndpoint?)null);
        _eventTypeRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([replacementEventType]);
        _endpointRepository.UpdateWithSubscriptionsAsync(
                Arg.Any<WebhookEndpoint>(),
                Arg.Do<IReadOnlyCollection<WebhookEndpointSubscription>>(subscriptions => capturedSubscriptions = subscriptions),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<WebhookEndpoint>());
        var handler = new UpdateWebhookEndpointCommandHandler(_endpointRepository, _eventTypeRepository);

        var result = await handler.Handle(
            new UpdateWebhookEndpointCommand
            {
                TenantId = tenantId,
                EndpointId = endpoint.Id,
                Url = " https://integrator.example/hooks/updated ",
                Description = " Updated endpoint ",
                EventTypeIds = [replacementEventType.Id],
                MaxAttempts = 6,
                TimeoutSeconds = 12,
                RateLimitPerMinute = 120
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(endpoint.Url).IsEqualTo("https://integrator.example/hooks/updated");
        await Assert.That(endpoint.Description).IsEqualTo("Updated endpoint");
        await Assert.That(endpoint.MaxAttempts).IsEqualTo(6);
        await Assert.That(endpoint.TimeoutSeconds).IsEqualTo(12);
        await Assert.That(endpoint.RateLimitPerMinute).IsEqualTo(120);
        await Assert.That(capturedSubscriptions).IsNotNull();
        await Assert.That(capturedSubscriptions!.Single().EventTypeId).IsEqualTo(replacementEventType.Id);
    }

    [Test]
    public async Task UpdateEndpointHandler_WhenEndpointMissing_ReturnsNotFoundFailure()
    {
        var tenantId = Guid.CreateVersion7();
        var endpointId = Guid.CreateVersion7();
        _endpointRepository.GetByTenantAndIdForUpdateAsync(tenantId, endpointId, Arg.Any<CancellationToken>())
            .Returns((WebhookEndpoint?)null);
        var handler = new UpdateWebhookEndpointCommandHandler(_endpointRepository, _eventTypeRepository);

        var result = await handler.Handle(
            CreateUpdateEndpointCommand(tenantId, endpointId, [Guid.CreateVersion7()]),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_endpoint_not_found");
        await _endpointRepository.DidNotReceive().UpdateWithSubscriptionsAsync(
            Arg.Any<WebhookEndpoint>(),
            Arg.Any<IReadOnlyCollection<WebhookEndpointSubscription>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateEndpointHandler_WhenUrlConflicts_ReturnsConflictFailure()
    {
        var tenantId = Guid.CreateVersion7();
        var endpoint = CreateEndpoint(tenantId, CreateConsumer(tenantId, "Tenant automation", WebhookProviderMode.Local));
        var conflictingEndpoint = CreateEndpoint(tenantId, endpoint.Consumer!);
        conflictingEndpoint.Id = Guid.CreateVersion7();
        _endpointRepository.GetByTenantAndIdForUpdateAsync(tenantId, endpoint.Id, Arg.Any<CancellationToken>())
            .Returns(endpoint);
        _endpointRepository.GetByTenantConsumerAndUrlAsync(
                tenantId,
                endpoint.ConsumerId,
                "https://integrator.example/webhooks/islamu",
                Arg.Any<CancellationToken>())
            .Returns(conflictingEndpoint);
        var handler = new UpdateWebhookEndpointCommandHandler(_endpointRepository, _eventTypeRepository);

        var result = await handler.Handle(
            CreateUpdateEndpointCommand(tenantId, endpoint.Id, [Guid.CreateVersion7()]),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_endpoint_url_conflict");
        await _eventTypeRepository.DidNotReceive().GetByIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateEndpointHandler_WhenRequestInvalid_DoesNotQueryRepositories()
    {
        var handler = new UpdateWebhookEndpointCommandHandler(_endpointRepository, _eventTypeRepository);

        var result = await handler.Handle(
            new UpdateWebhookEndpointCommand
            {
                TenantId = Guid.Empty,
                EndpointId = Guid.Empty,
                Url = "file:///tmp/hook",
                EventTypeIds = []
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_endpoint_validation_failed");
        await _endpointRepository.DidNotReceive().GetByTenantAndIdForUpdateAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RotateEndpointSecretHandler_WhenRequestValid_RotatesSecretReferenceAndPreservesPreviousWindow()
    {
        var tenantId = Guid.CreateVersion7();
        var endpoint = CreateEndpoint(tenantId, CreateConsumer(tenantId, "Tenant automation", WebhookProviderMode.Local));
        endpoint.SecretRef = "configuration:Webhooks:EndpointSecrets:integrator:v1";
        endpoint.SecretVersion = 3;
        WebhookEndpoint? captured = null;
        _endpointRepository.GetByTenantAndIdForUpdateAsync(tenantId, endpoint.Id, Arg.Any<CancellationToken>())
            .Returns(endpoint);
        _endpointRepository.UpdateAsync(Arg.Do<WebhookEndpoint>(updated => captured = updated), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<WebhookEndpoint>());
        var handler = new RotateWebhookEndpointSecretCommandHandler(_endpointRepository);
        var before = DateTime.UtcNow;

        var result = await handler.Handle(
            new RotateWebhookEndpointSecretCommand
            {
                TenantId = tenantId,
                EndpointId = endpoint.Id,
                NewSecretRef = " configuration:Webhooks:EndpointSecrets:integrator:v2 ",
                PreviousSecretValidForSeconds = 3_600
            },
            CancellationToken.None);
        var after = DateTime.UtcNow;

        await Assert.That(result.Success).IsTrue();
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.SecretRef).IsEqualTo("configuration:Webhooks:EndpointSecrets:integrator:v2");
        await Assert.That(captured.PreviousSecretRef).IsEqualTo("configuration:Webhooks:EndpointSecrets:integrator:v1");
        await Assert.That(captured.SecretVersion).IsEqualTo(4);
        await Assert.That(captured.PreviousSecretValidUntil).IsNotNull();
        await Assert.That(captured.PreviousSecretValidUntil!.Value >= before.AddSeconds(3_600)).IsTrue();
        await Assert.That(captured.PreviousSecretValidUntil.Value <= after.AddSeconds(3_605)).IsTrue();
    }

    [Test]
    public async Task RotateEndpointSecretHandler_WhenRequestInvalid_DoesNotQueryRepository()
    {
        var handler = new RotateWebhookEndpointSecretCommandHandler(_endpointRepository);

        var result = await handler.Handle(
            new RotateWebhookEndpointSecretCommand
            {
                TenantId = Guid.Empty,
                EndpointId = Guid.Empty,
                NewSecretRef = "",
                PreviousSecretValidForSeconds = -1
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_endpoint_secret_validation_failed");
        await _endpointRepository.DidNotReceive().GetByTenantAndIdForUpdateAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RotateEndpointSecretHandler_WhenEndpointMissing_ReturnsNotFoundFailure()
    {
        var tenantId = Guid.CreateVersion7();
        var endpointId = Guid.CreateVersion7();
        _endpointRepository.GetByTenantAndIdForUpdateAsync(tenantId, endpointId, Arg.Any<CancellationToken>())
            .Returns((WebhookEndpoint?)null);
        var handler = new RotateWebhookEndpointSecretCommandHandler(_endpointRepository);

        var result = await handler.Handle(
            new RotateWebhookEndpointSecretCommand
            {
                TenantId = tenantId,
                EndpointId = endpointId,
                NewSecretRef = "configuration:Webhooks:EndpointSecrets:integrator:v2"
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_endpoint_not_found");
        await _endpointRepository.DidNotReceive().UpdateAsync(
            Arg.Any<WebhookEndpoint>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RotateEndpointSecretHandler_WhenSecretReferenceUnchanged_ReturnsValidationFailure()
    {
        var tenantId = Guid.CreateVersion7();
        var endpoint = CreateEndpoint(tenantId, CreateConsumer(tenantId, "Tenant automation", WebhookProviderMode.Local));
        _endpointRepository.GetByTenantAndIdForUpdateAsync(tenantId, endpoint.Id, Arg.Any<CancellationToken>())
            .Returns(endpoint);
        var handler = new RotateWebhookEndpointSecretCommandHandler(_endpointRepository);

        var result = await handler.Handle(
            new RotateWebhookEndpointSecretCommand
            {
                TenantId = tenantId,
                EndpointId = endpoint.Id,
                NewSecretRef = endpoint.SecretRef
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_endpoint_secret_unchanged");
        await _endpointRepository.DidNotReceive().UpdateAsync(
            Arg.Any<WebhookEndpoint>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TestEndpointHandler_WhenEndpointActiveLocal_SchedulesMessageAndDeliveryAttempt()
    {
        var tenantId = Guid.CreateVersion7();
        var endpoint = CreateEndpoint(tenantId, CreateConsumer(tenantId, "Tenant automation", WebhookProviderMode.Local));
        WebhookMessage? capturedMessage = null;
        WebhookDeliveryAttempt? capturedAttempt = null;
        _endpointRepository.GetByTenantAndIdAsync(tenantId, endpoint.Id, Arg.Any<CancellationToken>())
            .Returns(endpoint);
        _messageRepository.CreateAsync(Arg.Do<WebhookMessage>(message => capturedMessage = message), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<WebhookMessage>());
        _attemptRepository.CreateAsync(Arg.Do<WebhookDeliveryAttempt>(attempt => capturedAttempt = attempt), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<WebhookDeliveryAttempt>());
        var handler = new TestWebhookEndpointCommandHandler(
            _endpointRepository,
            _messageRepository,
            _attemptRepository,
            _payloadBuilder);
        var before = DateTime.UtcNow;

        var result = await handler.Handle(
            new TestWebhookEndpointCommand
            {
                TenantId = tenantId,
                EndpointId = endpoint.Id
            },
            CancellationToken.None);
        var after = DateTime.UtcNow;

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(capturedMessage).IsNotNull();
        await Assert.That(capturedMessage!.Id).IsEqualTo(result.Id);
        await Assert.That(capturedMessage.EventType).IsEqualTo(WebhookEventNames.WebhookTest);
        await Assert.That(capturedMessage.AggregateKind).IsEqualTo("WebhookEndpoint");
        await Assert.That(capturedMessage.AggregateId).IsEqualTo(endpoint.Id);
        await Assert.That(capturedMessage.ConsumerId).IsEqualTo(endpoint.ConsumerId);
        await Assert.That(capturedMessage.ProviderMode).IsEqualTo(WebhookProviderMode.Local);
        await Assert.That(capturedMessage.Status).IsEqualTo(WebhookMessageStatus.Pending);
        await Assert.That(capturedMessage.PayloadJson).Contains(endpoint.Id.ToString("D"));
        await Assert.That(capturedMessage.PayloadJson).DoesNotContain(endpoint.SecretRef);
        await Assert.That(capturedMessage.PayloadRetentionUntil >= before.AddDays(1).AddSeconds(-1)).IsTrue();
        await Assert.That(capturedMessage.PayloadRetentionUntil <= after.AddDays(1).AddSeconds(1)).IsTrue();
        await Assert.That(capturedAttempt).IsNotNull();
        await Assert.That(capturedAttempt!.TenantId).IsEqualTo(tenantId);
        await Assert.That(capturedAttempt.MessageId).IsEqualTo(capturedMessage.Id);
        await Assert.That(capturedAttempt.EndpointId).IsEqualTo(endpoint.Id);
        await Assert.That(capturedAttempt.AttemptNumber).IsEqualTo(1);
        await Assert.That(capturedAttempt.Status).IsEqualTo(WebhookDeliveryAttemptStatus.Scheduled);
        await Assert.That(capturedAttempt.ScheduledAt >= before.AddSeconds(-1)).IsTrue();
        await Assert.That(capturedAttempt.ScheduledAt <= after.AddSeconds(1)).IsTrue();
        await _messageRepository.Received(1).MarkProviderQueuedAsync(
            tenantId,
            capturedMessage.Id,
            null,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TestEndpointHandler_WhenConsumerProviderIsSvix_ReturnsProviderManagedFailure()
    {
        var tenantId = Guid.CreateVersion7();
        var endpoint = CreateEndpoint(tenantId, CreateConsumer(tenantId, "Tenant automation", WebhookProviderMode.Svix));
        _endpointRepository.GetByTenantAndIdAsync(tenantId, endpoint.Id, Arg.Any<CancellationToken>())
            .Returns(endpoint);
        var handler = new TestWebhookEndpointCommandHandler(
            _endpointRepository,
            _messageRepository,
            _attemptRepository,
            _payloadBuilder);

        var result = await handler.Handle(
            new TestWebhookEndpointCommand
            {
                TenantId = tenantId,
                EndpointId = endpoint.Id
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_endpoint_test_provider_managed");
        await _messageRepository.DidNotReceive().CreateAsync(
            Arg.Any<WebhookMessage>(),
            Arg.Any<CancellationToken>());
        await _attemptRepository.DidNotReceive().CreateAsync(
            Arg.Any<WebhookDeliveryAttempt>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TestEndpointHandler_WhenRequestInvalid_DoesNotQueryRepository()
    {
        var handler = new TestWebhookEndpointCommandHandler(
            _endpointRepository,
            _messageRepository,
            _attemptRepository,
            _payloadBuilder);

        var result = await handler.Handle(
            new TestWebhookEndpointCommand
            {
                TenantId = Guid.Empty,
                EndpointId = Guid.Empty
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_endpoint_test_validation_failed");
        await _endpointRepository.DidNotReceive().GetByTenantAndIdAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task MessageListHandler_MapsSafeMetadataAndCapsLimit()
    {
        var tenantId = Guid.CreateVersion7();
        var consumer = CreateConsumer(tenantId, "Tenant automation", WebhookProviderMode.Local);
        var message = CreateMessage(tenantId, consumer);
        _messageRepository.ListByTenantAsync(tenantId, 500, Arg.Any<CancellationToken>())
            .Returns([message]);
        var handler = new GetWebhookMessagesQueryHandler(_messageRepository);

        var result = await handler.Handle(
            new GetWebhookMessagesQuery
            {
                TenantId = tenantId,
                Limit = 10_000
            },
            CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Id).IsEqualTo(message.Id);
        await Assert.That(result[0].TenantId).IsEqualTo(tenantId);
        await Assert.That(result[0].ConsumerName).IsEqualTo("Tenant automation");
        await Assert.That(result[0].PayloadHash).IsEqualTo(message.PayloadHash);
        await Assert.That(result[0].ProviderModeName).IsEqualTo(nameof(WebhookProviderMode.Local));
        await Assert.That(result[0].StatusName).IsEqualTo(nameof(WebhookMessageStatus.Queued));
        await Assert.That(typeof(Explore.Application.DTOs.Webhooks.WebhookMessageDto).GetProperty("PayloadJson")).IsNull();
        await _messageRepository.Received(1).ListByTenantAsync(
            tenantId,
            500,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task MessageDetailHandler_UsesTenantScopedRepositoryLookup()
    {
        var tenantId = Guid.CreateVersion7();
        var message = CreateMessage(tenantId, CreateConsumer(tenantId, "Tenant automation", WebhookProviderMode.Local));
        _messageRepository.GetByTenantAndIdAsync(tenantId, message.Id, Arg.Any<CancellationToken>())
            .Returns(message);
        var handler = new GetWebhookMessageByIdQueryHandler(_messageRepository);

        var result = await handler.Handle(
            new GetWebhookMessageByIdQuery
            {
                TenantId = tenantId,
                MessageId = message.Id
            },
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(message.Id);
        await Assert.That(result.PayloadHash).IsEqualTo(message.PayloadHash);
        await Assert.That(result.ProviderMessageId).IsEqualTo(message.ProviderMessageId);
        await _messageRepository.Received(1).GetByTenantAndIdAsync(
            tenantId,
            message.Id,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task MessageDetailHandler_WhenIdentifiersAreEmpty_DoesNotQueryRepository()
    {
        var handler = new GetWebhookMessageByIdQueryHandler(_messageRepository);

        var result = await handler.Handle(
            new GetWebhookMessageByIdQuery
            {
                TenantId = Guid.Empty,
                MessageId = Guid.Empty
            },
            CancellationToken.None);

        await Assert.That(result).IsNull();
        await _messageRepository.DidNotReceive().GetByTenantAndIdAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeliveryAttemptListHandler_NormalizesFiltersMapsSafeMetadataAndCapsLimit()
    {
        var tenantId = Guid.CreateVersion7();
        var endpoint = CreateEndpoint(tenantId, CreateConsumer(tenantId, "Tenant automation", WebhookProviderMode.Local));
        var message = CreateMessage(tenantId, endpoint.Consumer!);
        var attempt = CreateDeliveryAttempt(tenantId, message, endpoint, WebhookDeliveryAttemptStatus.Failed);
        _attemptRepository.ListByTenantAsync(
                tenantId,
                null,
                endpoint.Id,
                500,
                Arg.Any<CancellationToken>())
            .Returns([attempt]);
        var handler = new GetWebhookDeliveryAttemptsQueryHandler(_attemptRepository);

        var result = await handler.Handle(
            new GetWebhookDeliveryAttemptsQuery
            {
                TenantId = tenantId,
                MessageId = Guid.Empty,
                EndpointId = endpoint.Id,
                Limit = 10_000
            },
            CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Id).IsEqualTo(attempt.Id);
        await Assert.That(result[0].TenantId).IsEqualTo(tenantId);
        await Assert.That(result[0].MessageEventType).IsEqualTo(WebhookEventNames.EventPublished);
        await Assert.That(result[0].EndpointUrl).IsEqualTo(endpoint.Url);
        await Assert.That(result[0].ResponseBodyPreview).IsEqualTo("upstream returned 500");
        await Assert.That(typeof(Explore.Application.DTOs.Webhooks.WebhookDeliveryAttemptDto).GetProperty("ResponseBody")).IsNull();
        await Assert.That(typeof(Explore.Application.DTOs.Webhooks.WebhookDeliveryAttemptDto).GetProperty("SecretRef")).IsNull();
        await _attemptRepository.Received(1).ListByTenantAsync(
            tenantId,
            null,
            endpoint.Id,
            500,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeliveryAttemptDetailHandler_UsesTenantScopedRepositoryLookup()
    {
        var tenantId = Guid.CreateVersion7();
        var endpoint = CreateEndpoint(tenantId, CreateConsumer(tenantId, "Tenant automation", WebhookProviderMode.Local));
        var message = CreateMessage(tenantId, endpoint.Consumer!);
        var attempt = CreateDeliveryAttempt(tenantId, message, endpoint, WebhookDeliveryAttemptStatus.Abandoned);
        _attemptRepository.GetByTenantAndIdAsync(tenantId, attempt.Id, Arg.Any<CancellationToken>())
            .Returns(attempt);
        var handler = new GetWebhookDeliveryAttemptByIdQueryHandler(_attemptRepository);

        var result = await handler.Handle(
            new GetWebhookDeliveryAttemptByIdQuery
            {
                TenantId = tenantId,
                AttemptId = attempt.Id
            },
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(attempt.Id);
        await Assert.That(result.StatusName).IsEqualTo(nameof(WebhookDeliveryAttemptStatus.Abandoned));
        await Assert.That(result.ResponseBodyPreview).IsEqualTo("upstream returned 500");
        await _attemptRepository.Received(1).GetByTenantAndIdAsync(
            tenantId,
            attempt.Id,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeliveryAttemptDetailHandler_WhenIdentifiersAreEmpty_DoesNotQueryRepository()
    {
        var handler = new GetWebhookDeliveryAttemptByIdQueryHandler(_attemptRepository);

        var result = await handler.Handle(
            new GetWebhookDeliveryAttemptByIdQuery
            {
                TenantId = Guid.Empty,
                AttemptId = Guid.Empty
            },
            CancellationToken.None);

        await Assert.That(result).IsNull();
        await _attemptRepository.DidNotReceive().GetByTenantAndIdAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RetryDeliveryAttemptHandler_SchedulesManualRetryWithTenantAndAttempt()
    {
        var tenantId = Guid.CreateVersion7();
        var attemptId = Guid.CreateVersion7();
        var retryAttemptId = Guid.CreateVersion7();
        _deliveryDrainService.ScheduleManualRetryAsync(tenantId, attemptId, Arg.Any<CancellationToken>())
            .Returns(new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.RetryScheduled, retryAttemptId));
        var handler = new RetryWebhookDeliveryAttemptCommandHandler(_deliveryDrainService);

        var result = await handler.Handle(
            new RetryWebhookDeliveryAttemptCommand
            {
                TenantId = tenantId,
                AttemptId = attemptId
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(retryAttemptId);
        await _deliveryDrainService.Received(1).ScheduleManualRetryAsync(
            tenantId,
            attemptId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RetryDeliveryAttemptHandler_WhenRetryAlreadyDeferred_ReturnsConflictCode()
    {
        var tenantId = Guid.CreateVersion7();
        var attemptId = Guid.CreateVersion7();
        _deliveryDrainService.ScheduleManualRetryAsync(tenantId, attemptId, Arg.Any<CancellationToken>())
            .Returns(new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.Deferred, attemptId));
        var handler = new RetryWebhookDeliveryAttemptCommandHandler(_deliveryDrainService);

        var result = await handler.Handle(
            new RetryWebhookDeliveryAttemptCommand
            {
                TenantId = tenantId,
                AttemptId = attemptId
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Id).IsEqualTo(attemptId);
        await Assert.That(result.FailureCode).IsEqualTo("webhook_delivery_retry_deferred");
        await Assert.That(result.Errors).Contains("A scheduled or active delivery attempt already exists for this message and endpoint.");
    }

    [Test]
    public async Task RetryDeliveryAttemptHandler_WhenRequestInvalid_DoesNotCallDrainService()
    {
        var handler = new RetryWebhookDeliveryAttemptCommandHandler(_deliveryDrainService);

        var result = await handler.Handle(
            new RetryWebhookDeliveryAttemptCommand
            {
                TenantId = Guid.Empty,
                AttemptId = Guid.Empty
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_delivery_retry_validation_failed");
        await _deliveryDrainService.DidNotReceive().ScheduleManualRetryAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ArchiveEndpointHandler_WhenEndpointExists_ArchivesEndpoint()
    {
        var tenantId = Guid.CreateVersion7();
        var endpoint = CreateEndpoint(tenantId, CreateConsumer(tenantId, "Tenant automation", WebhookProviderMode.Local));
        _endpointRepository.GetByTenantAndIdAsync(tenantId, endpoint.Id, Arg.Any<CancellationToken>())
            .Returns(endpoint);
        var handler = new ArchiveWebhookEndpointCommandHandler(_endpointRepository);

        var result = await handler.Handle(
            new ArchiveWebhookEndpointCommand
            {
                TenantId = tenantId,
                EndpointId = endpoint.Id
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _endpointRepository.Received(1).ArchiveAsync(
            tenantId,
            endpoint.Id,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ArchiveEndpointHandler_WhenEndpointMissing_ReturnsNotFoundFailure()
    {
        var tenantId = Guid.CreateVersion7();
        var endpointId = Guid.CreateVersion7();
        _endpointRepository.GetByTenantAndIdAsync(tenantId, endpointId, Arg.Any<CancellationToken>())
            .Returns((WebhookEndpoint?)null);
        var handler = new ArchiveWebhookEndpointCommandHandler(_endpointRepository);

        var result = await handler.Handle(
            new ArchiveWebhookEndpointCommand
            {
                TenantId = tenantId,
                EndpointId = endpointId
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("webhook_endpoint_not_found");
        await _endpointRepository.DidNotReceive().ArchiveAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    private static WebhookConsumer CreateConsumer(
        Guid tenantId,
        string name,
        WebhookProviderMode providerMode) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ConsumerKind = WebhookConsumerKind.Tenant,
            Name = name,
            Status = WebhookConsumerStatus.Active,
            ProviderMode = providerMode,
            CreatedAt = DateTime.UtcNow
        };

    private static CreateWebhookEndpointCommand CreateEndpointCommand(
        Guid tenantId,
        Guid consumerId,
        IReadOnlyList<Guid> eventTypeIds) =>
        new()
        {
            TenantId = tenantId,
            ConsumerId = consumerId,
            Url = "https://integrator.example/webhooks/islamu",
            SecretRef = "configuration:Webhooks:EndpointSecrets:integrator",
            EventTypeIds = eventTypeIds
        };

    private static UpdateWebhookEndpointCommand CreateUpdateEndpointCommand(
        Guid tenantId,
        Guid endpointId,
        IReadOnlyList<Guid> eventTypeIds) =>
        new()
        {
            TenantId = tenantId,
            EndpointId = endpointId,
            Url = "https://integrator.example/webhooks/islamu",
            EventTypeIds = eventTypeIds
        };

    private static WebhookEndpoint CreateEndpoint(Guid tenantId, WebhookConsumer consumer)
    {
        var eventType = CreateEventType("event.published");
        var endpoint = new WebhookEndpoint
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ConsumerId = consumer.Id,
            Consumer = consumer,
            Url = "https://integrator.example/webhooks/islamu",
            Description = "Integrator endpoint",
            Status = WebhookEndpointStatus.Active,
            SecretRef = "configuration:Webhooks:EndpointSecrets:integrator",
            SecretVersion = 1,
            MaxAttempts = 8,
            TimeoutSeconds = 15,
            RateLimitPerMinute = 60,
            CreatedAt = DateTime.UtcNow
        };

        endpoint.Subscriptions.Add(new WebhookEndpointSubscription
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            EndpointId = endpoint.Id,
            Endpoint = endpoint,
            EventTypeId = eventType.Id,
            EventType = eventType,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow
        });

        return endpoint;
    }

    private static WebhookEventType CreateEventType(string name) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            GroupName = "event",
            Description = "Raised when an event changes.",
            SchemaJson = "{}",
            SchemaVersion = 1,
            IsPublic = true,
            IsEnabled = true,
            PayloadRetentionDays = 14,
            CreatedAt = DateTime.UtcNow
        };

    private static WebhookMessage CreateMessage(Guid tenantId, WebhookConsumer consumer) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            EventType = WebhookEventNames.EventPublished,
            EventId = Guid.CreateVersion7().ToString("D"),
            AggregateKind = "Event",
            AggregateId = Guid.CreateVersion7(),
            ConsumerId = consumer.Id,
            Consumer = consumer,
            PayloadJson = """{"secret":"must-not-leak"}""",
            PayloadHash = "sha256:8f4a3db2",
            PayloadRetentionUntil = DateTime.UtcNow.AddDays(14),
            ProviderMode = WebhookProviderMode.Local,
            ProviderMessageId = "local-message-1",
            Status = WebhookMessageStatus.Queued,
            CreatedAt = DateTime.UtcNow
        };

    private static WebhookDeliveryAttempt CreateDeliveryAttempt(
        Guid tenantId,
        WebhookMessage message,
        WebhookEndpoint endpoint,
        WebhookDeliveryAttemptStatus status) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            MessageId = message.Id,
            Message = message,
            EndpointId = endpoint.Id,
            Endpoint = endpoint,
            AttemptNumber = 2,
            Status = status,
            ScheduledAt = DateTime.UtcNow.AddMinutes(-5),
            SentAt = DateTime.UtcNow.AddMinutes(-4),
            CompletedAt = DateTime.UtcNow.AddMinutes(-4),
            HttpStatusCode = 500,
            FailureCategory = "server_error",
            ResponseBodyPreview = "upstream returned 500",
            DurationMs = 150,
            NextRetryAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow
        };
}
