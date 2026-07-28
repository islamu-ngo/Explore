// ABOUTME: Authenticated API controller for outgoing webhook provider management actions.
// ABOUTME: Exposes backend-generated Svix App Portal access without leaking provider credentials.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Extensions;
using Explore.API.Hateoas;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.DTOs.Webhooks;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Application.Features.Webhooks.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/webhooks")]
[ApiController]
[Authorize]
[EndpointClassification(EndpointClass.Authenticated)]
[Produces(HateoasConstants.JsonMediaType)]
public sealed class WebhooksController(
    IMediator mediator,
    ITenantContext tenantContext,
    IWebhookOwnershipScopeResolver webhookOwnershipScopeResolver,
    IResourceAssembler<WebhookConsumerDto, WebhookConsumerDto> webhookConsumerAssembler,
    IResourceAssembler<WebhookEndpointDto, WebhookEndpointDto> webhookEndpointAssembler,
    IResourceAssembler<WebhookMessageDto, WebhookMessageDto> webhookMessageAssembler,
    IResourceAssembler<WebhookDeliveryAttemptDto, WebhookDeliveryAttemptDto> webhookDeliveryAttemptAssembler) : ExploreControllerBase
{
    private static readonly ApiValidationProblemDescriptor ConsumerValidationProblem = new(
        "webhookConsumer",
        "Webhook consumer validation failed",
        "Webhook consumer command failed.");

    private static readonly ApiValidationProblemDescriptor PortalValidationProblem = new(
        "webhookProviderPortal",
        "Webhook provider portal validation failed",
        "Webhook provider portal access could not be created.");

    private static readonly ApiValidationProblemDescriptor ProviderBindingValidationProblem = new(
        "webhookProviderBinding",
        "Webhook provider binding repair validation failed",
        "Webhook provider binding could not be repaired.");

    private static readonly ApiValidationProblemDescriptor EndpointValidationProblem = new(
        "webhookEndpoint",
        "Webhook endpoint validation failed",
        "Webhook endpoint command failed.");

    private static readonly ApiValidationProblemDescriptor RetryValidationProblem = new(
        "webhookDeliveryRetry",
        "Webhook delivery retry validation failed",
        "Webhook delivery retry command failed.");

    private static readonly ApiValidationProblemDescriptor IncomingRedriveValidationProblem = new(
        "incomingWebhookRedrive",
        "Incoming webhook redrive validation failed",
        "Incoming webhook redrive command failed.");

    private static readonly ApiNotFoundProblemDescriptor ConsumerNotFoundProblem = new(
        "Webhook consumer not found",
        "Webhook consumer was not found.",
        "webhook_consumer_not_found");

    private static readonly ApiNotFoundProblemDescriptor EndpointNotFoundProblem = new(
        "Webhook endpoint not found",
        "Webhook endpoint was not found.",
        "webhook_endpoint_not_found");

    private static readonly ApiNotFoundProblemDescriptor MessageNotFoundProblem = new(
        "Webhook message not found",
        "Webhook message was not found.",
        "webhook_message_not_found");

    private static readonly ApiNotFoundProblemDescriptor MessagePayloadNotFoundProblem = new(
        "Webhook payload not found",
        "Webhook payload was not found.",
        "webhook_message_payload_not_found");

    private static readonly ApiNotFoundProblemDescriptor DeliveryAttemptNotFoundProblem = new(
        "Webhook delivery attempt not found",
        "Webhook delivery attempt was not found.",
        "webhook_delivery_attempt_not_found");

    private static readonly ApiNotFoundProblemDescriptor IncomingWebhookNotFoundProblem = new(
        "Incoming webhook not found",
        "Incoming webhook was not found.",
        "incoming_webhook_not_found");

    [HttpGet("event-types", Name = RouteNames.GetWebhookEventTypes)]
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [EndpointSummary("Get webhook event types")]
    [EndpointDescription("Returns the canonical outgoing webhook event catalog with schema and example payload metadata.")]
    [OutputCache(PolicyName = "LookupData")]
    [ProducesResponseType(typeof(IReadOnlyList<WebhookEventTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WebhookEventTypeDto>>> GetEventTypes(
        CancellationToken cancellationToken = default)
    {
        var eventTypes = await mediator.Send(new GetWebhookEventTypesQuery(), cancellationToken);
        return Ok(eventTypes);
    }

    [HttpGet("consumers", Name = RouteNames.GetWebhookConsumers)]
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [EndpointSummary("Get webhook consumers")]
    [EndpointDescription("Returns outgoing webhook consumers for one typed owner with HAL management affordances.")]
    [OutputCache(PolicyName = "ListData")]
    [ProducesResponseType(typeof(HalCollectionResource<WebhookConsumerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    public async Task<ActionResult<HalCollectionResource<WebhookConsumerDto>>> GetConsumers(
        [FromQuery] int ownerKindId,
        [FromQuery] Guid? ownerId = null,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var consumers = await mediator.Send(
            new GetWebhookConsumersQuery
            {
                OwnerKindId = ownerKindId,
                OwnerId = ownerId,
                Limit = limit
            },
            cancellationToken);

        var resource = await webhookConsumerAssembler.ToCollectionResource(
            consumers,
            RouteNames.GetWebhookConsumers,
            await CreateCollectionRouteValuesAsync(ownerKindId, ownerId, limit, cancellationToken),
            HttpContext);

        return Ok(resource);
    }

    [HttpGet("consumers/{consumerId:guid}", Name = RouteNames.GetWebhookConsumerById)]
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [EndpointSummary("Get webhook consumer")]
    [EndpointDescription("Returns one owner-authorized outgoing webhook consumer with HAL management affordances.")]
    [ProducesResponseType(typeof(HalResource<WebhookConsumerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    public async Task<ActionResult<HalResource<WebhookConsumerDto>>> GetConsumer(
        Guid consumerId,
        CancellationToken cancellationToken = default)
    {
        var consumer = await mediator.Send(
            new GetWebhookConsumerByIdQuery
            {
                ConsumerId = consumerId
            },
            cancellationToken);

        if (consumer is null)
        {
            return this.ToNotFoundProblem(ConsumerNotFoundProblem);
        }

        var resource = await webhookConsumerAssembler.ToResource(consumer, HttpContext);
        return Ok(resource);
    }

    [HttpPost("consumers", Name = RouteNames.CreateWebhookConsumer)]
    [EndpointSummary("Create webhook consumer")]
    [EndpointDescription("Creates a tenant-scoped outgoing webhook consumer for Local, Svix, Composite, DryRun, or Disabled provider modes.")]
    [Consumes(HateoasConstants.JsonMediaType)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> CreateConsumer(
        [FromBody] CreateWebhookConsumerRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(
            new CreateWebhookConsumerCommand
            {
                OwnerId = request.OwnerId,
                ConsumerKindId = request.ConsumerKindId,
                Name = request.Name,
                ProviderModeId = request.ProviderModeId
            },
            cancellationToken);

        if (!response.Success)
        {
            return ToWebhookConsumerProblem(response);
        }

        return CreatedAtRoute(
            RouteNames.GetWebhookConsumerById,
            new { consumerId = response.Id },
            response);
    }

    [HttpPatch("consumers/{consumerId:guid}/provider-mode", Name = RouteNames.UpdateWebhookConsumerProviderMode)]
    [EndpointSummary("Change webhook consumer provider mode")]
    [EndpointDescription("Changes the provider mode for new deliveries while preserving already materialized work on its immutable delivery snapshots.")]
    [Consumes(HateoasConstants.JsonMediaType)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateConsumerProviderMode(
        Guid consumerId,
        [FromBody] UpdateWebhookConsumerProviderModeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProviderMode is null)
        {
            return this.ToValidationProblem(ConsumerValidationProblem, "ProviderMode group is required.");
        }

        var providerMode = request.ProviderMode;
        var response = await mediator.Send(
            new UpdateWebhookConsumerProviderModeCommand
            {
                ConsumerId = consumerId,
                ProviderModeId = providerMode.ProviderModeId,
                ExpectedConfigurationVersion = providerMode.ExpectedConfigurationVersion,
                PendingWorkDecisionId = providerMode.PendingWorkDecisionId,
                PendingWorkReason = providerMode.PendingWorkReason,
                AcknowledgeUncertainProviderPublications = providerMode.AcknowledgeUncertainProviderPublications
            },
            cancellationToken);

        return response.Success ? Ok(response) : ToWebhookConsumerProblem(response);
    }

    [HttpPost("consumers/{consumerId:guid}/provider-binding/repair", Name = RouteNames.RepairWebhookProviderBinding)]
    [EndpointSummary("Repair webhook provider binding")]
    [EndpointDescription("Verifies self-hosted provider ownership and atomically creates or rebinds the consumer application mapping.")]
    [Consumes(HateoasConstants.JsonMediaType)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> RepairProviderBinding(
        Guid consumerId,
        [FromBody] RepairWebhookProviderBindingRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(
            new RepairWebhookProviderBindingCommand
            {
                ConsumerId = consumerId,
                ExternalApplicationId = request.ExternalApplicationId,
                ReasonCode = request.ReasonCode
            },
            cancellationToken);

        return response.Success
            ? Ok(response)
            : ToProviderBindingRepairProblem(response);
    }

    [HttpGet("endpoints", Name = RouteNames.GetWebhookEndpoints)]
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [EndpointSummary("Get webhook endpoints")]
    [EndpointDescription("Returns outgoing webhook endpoints for one typed owner with HAL management affordances.")]
    [OutputCache(PolicyName = "ListData")]
    [ProducesResponseType(typeof(HalCollectionResource<WebhookEndpointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    public async Task<ActionResult<HalCollectionResource<WebhookEndpointDto>>> GetEndpoints(
        [FromQuery] int ownerKindId,
        [FromQuery] Guid? ownerId = null,
        [FromQuery] Guid? consumerId = null,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var normalizedConsumerId = consumerId == Guid.Empty ? null : consumerId;
        var endpoints = await mediator.Send(
            new GetWebhookEndpointsQuery
            {
                OwnerKindId = ownerKindId,
                OwnerId = ownerId,
                ConsumerId = normalizedConsumerId,
                Limit = limit
            },
            cancellationToken);

        var resource = await webhookEndpointAssembler.ToCollectionResource(
            endpoints,
            RouteNames.GetWebhookEndpoints,
            await CreateCollectionRouteValuesAsync(
                ownerKindId,
                ownerId,
                limit,
                cancellationToken,
                consumerId: normalizedConsumerId),
            HttpContext);

        return Ok(resource);
    }

    [HttpGet("endpoints/{endpointId:guid}", Name = RouteNames.GetWebhookEndpointById)]
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [EndpointSummary("Get webhook endpoint")]
    [EndpointDescription("Returns one owner-authorized outgoing webhook endpoint with subscription metadata.")]
    [ProducesResponseType(typeof(HalResource<WebhookEndpointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    public async Task<ActionResult<HalResource<WebhookEndpointDto>>> GetEndpoint(
        Guid endpointId,
        CancellationToken cancellationToken = default)
    {
        var endpoint = await mediator.Send(
            new GetWebhookEndpointByIdQuery
            {
                EndpointId = endpointId
            },
            cancellationToken);

        if (endpoint is null)
        {
            return this.ToNotFoundProblem(EndpointNotFoundProblem);
        }

        var resource = await webhookEndpointAssembler.ToResource(endpoint, HttpContext);
        return Ok(resource);
    }

    [HttpPost("endpoints", Name = RouteNames.CreateWebhookEndpoint)]
    [EndpointSummary("Create webhook endpoint")]
    [EndpointDescription("Creates an outgoing webhook endpoint that inherits its persisted consumer owner.")]
    [Consumes(HateoasConstants.JsonMediaType)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> CreateEndpoint(
        [FromBody] CreateWebhookEndpointRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(
            new CreateWebhookEndpointCommand
            {
                ConsumerId = request.ConsumerId,
                Url = request.Url,
                Description = request.Description,
                SecretRef = request.SecretRef,
                EventTypeIds = request.EventTypeIds,
                MaxAttempts = request.MaxAttempts,
                TimeoutSeconds = request.TimeoutSeconds,
                RateLimitPerMinute = request.RateLimitPerMinute
            },
            cancellationToken);

        if (!response.Success)
        {
            return ToWebhookEndpointProblem(response);
        }

        return CreatedAtRoute(
            RouteNames.GetWebhookEndpointById,
            new { endpointId = response.Id },
            response);
    }

    [HttpPatch("endpoints/{endpointId:guid}", Name = RouteNames.UpdateWebhookEndpoint)]
    [EndpointSummary("Update webhook endpoint")]
    [EndpointDescription("Updates a tenant-scoped outgoing webhook endpoint and replaces its event type subscriptions.")]
    [Consumes(HateoasConstants.JsonMediaType)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateEndpoint(
        Guid endpointId,
        [FromBody] UpdateWebhookEndpointRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(
            new UpdateWebhookEndpointCommand
            {
                EndpointId = endpointId,
                Destination = request.Destination,
                Subscriptions = request.Subscriptions,
                DeliveryPolicy = request.DeliveryPolicy,
                Governance = request.Governance
            },
            cancellationToken);

        if (!response.Success)
        {
            return ToWebhookEndpointProblem(response);
        }

        return Ok(response);
    }

    [HttpDelete("endpoints/{endpointId:guid}", Name = RouteNames.DeleteWebhookEndpoint)]
    [EndpointSummary("Delete webhook endpoint")]
    [EndpointDescription("Archives a tenant-scoped outgoing webhook endpoint while preserving delivery history.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.DefaultPolicy)]
    public async Task<ActionResult> DeleteEndpoint(
        Guid endpointId,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(
            new ArchiveWebhookEndpointCommand
            {
                EndpointId = endpointId
            },
            cancellationToken);

        if (!response.Success)
        {
            return ToWebhookEndpointProblem(response).Result!;
        }

        return NoContent();
    }

    [HttpPost("endpoints/{endpointId:guid}/rotate-secret", Name = RouteNames.RotateWebhookEndpointSecret)]
    [EndpointSummary("Rotate webhook endpoint secret")]
    [EndpointDescription("Rotates the signing secret reference for a tenant-scoped outgoing webhook endpoint.")]
    [Consumes(HateoasConstants.JsonMediaType)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.DefaultPolicy)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> RotateEndpointSecret(
        Guid endpointId,
        [FromBody] RotateWebhookEndpointSecretRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(
            new RotateWebhookEndpointSecretCommand
            {
                EndpointId = endpointId,
                NewSecretRef = request.NewSecretRef,
                PreviousSecretValidForSeconds = request.PreviousSecretValidForSeconds,
                ExpectedConfigurationVersion = request.ExpectedConfigurationVersion,
                PendingWorkDecisionId = request.PendingWorkDecisionId,
                PendingWorkReason = request.PendingWorkReason,
                AcknowledgeUncertainProviderPublications = request.AcknowledgeUncertainProviderPublications
            },
            cancellationToken);

        if (!response.Success)
        {
            return ToWebhookEndpointProblem(response);
        }

        return Ok(response);
    }

    [HttpPost("endpoints/{endpointId:guid}/test", Name = RouteNames.TestWebhookEndpoint)]
    [EndpointSummary("Test webhook endpoint")]
    [EndpointDescription("Schedules a signed LocalProvider test delivery to a tenant-scoped outgoing webhook endpoint.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.DefaultPolicy)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> TestEndpoint(
        Guid endpointId,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(
            new TestWebhookEndpointCommand
            {
                EndpointId = endpointId,
                SourceTenantId = tenantContext.TenantId
            },
            cancellationToken);

        if (!response.Success)
        {
            return ToWebhookEndpointProblem(response);
        }

        return Ok(response);
    }

    [HttpPost("svix/app-portal", Name = RouteNames.OpenSvixAppPortal)]
    [EndpointSummary("Open Svix App Portal")]
    [EndpointDescription("Creates a short-lived Svix App Portal URL for a verified webhook consumer binding.")]
    [Consumes(HateoasConstants.JsonMediaType)]
    [ProducesResponseType(typeof(WebhookProviderPortalAccessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.ComplexPolicy)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<WebhookProviderPortalAccessDto>> OpenSvixAppPortal(
        [FromBody] OpenSvixAppPortalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new OpenSvixAppPortalCommand
            {
                ConsumerId = request.ConsumerId,
                SessionId = ResolvePortalSessionId(),
                ExpiresInSeconds = request.ExpiresInSeconds
            },
            cancellationToken);

        if (result.Success && result.Id is not null)
        {
            return Ok(result.Id);
        }

        return ToWebhookPortalProblem(result);
    }

    [HttpGet("messages", Name = RouteNames.GetWebhookMessages)]
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [EndpointSummary("Get webhook messages")]
    [EndpointDescription("Returns tenant-scoped outgoing webhook messages with safe delivery-history metadata.")]
    [ProducesResponseType(typeof(HalCollectionResource<WebhookMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    public async Task<ActionResult<HalCollectionResource<WebhookMessageDto>>> GetMessages(
        [FromQuery] int ownerKindId,
        [FromQuery] Guid? ownerId = null,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var messages = await mediator.Send(
            new GetWebhookMessagesQuery
            {
                OwnerKindId = ownerKindId,
                OwnerId = ownerId,
                Limit = limit
            },
            cancellationToken);

        var resource = await webhookMessageAssembler.ToCollectionResource(
            messages,
            RouteNames.GetWebhookMessages,
            await CreateCollectionRouteValuesAsync(ownerKindId, ownerId, limit, cancellationToken),
            HttpContext);

        return Ok(resource);
    }

    [HttpGet("messages/{messageId:guid}", Name = RouteNames.GetWebhookMessageById)]
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [EndpointSummary("Get webhook message")]
    [EndpointDescription("Returns one tenant-scoped outgoing webhook message without raw payload JSON.")]
    [ProducesResponseType(typeof(HalResource<WebhookMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    public async Task<ActionResult<HalResource<WebhookMessageDto>>> GetMessage(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var message = await mediator.Send(
            new GetWebhookMessageByIdQuery
            {
                MessageId = messageId
            },
            cancellationToken);

        if (message is null)
        {
            return this.ToNotFoundProblem(MessageNotFoundProblem);
        }

        var resource = await webhookMessageAssembler.ToResource(message, HttpContext);
        return Ok(resource);
    }

    [HttpGet("messages/{messageId:guid}/payload", Name = RouteNames.GetWebhookMessagePayload)]
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [EndpointSummary("Get webhook message payload")]
    [EndpointDescription("Returns separately authorized exact outgoing webhook payload bytes as base64 while retained.")]
    [ProducesResponseType(typeof(WebhookMessagePayloadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    [ResponseCache(Duration = 0, NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<WebhookMessagePayloadDto>> GetMessagePayload(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetWebhookMessagePayloadQuery
            {
                MessageId = messageId
            },
            cancellationToken);

        return result.Status switch
        {
            WebhookMessagePayloadReadStatus.Available when result.Payload is not null => Ok(result.Payload),
            WebhookMessagePayloadReadStatus.Gone => this.ToGoneProblem(
                "Webhook payload no longer retained",
                "The webhook payload is no longer available because its retention period ended.",
                "webhook_message_payload_gone"),
            _ => this.ToNotFoundProblem(MessagePayloadNotFoundProblem)
        };
    }

    [HttpGet("delivery-attempts", Name = RouteNames.GetWebhookDeliveryAttempts)]
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [EndpointSummary("Get webhook delivery attempts")]
    [EndpointDescription("Returns tenant-scoped LocalProvider delivery attempt audit rows with optional message and endpoint filters.")]
    [ProducesResponseType(typeof(HalCollectionResource<WebhookDeliveryAttemptDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    public async Task<ActionResult<HalCollectionResource<WebhookDeliveryAttemptDto>>> GetDeliveryAttempts(
        [FromQuery] int ownerKindId,
        [FromQuery] Guid? ownerId = null,
        [FromQuery] Guid? messageId = null,
        [FromQuery] Guid? endpointId = null,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var normalizedMessageId = messageId is { } requestedMessageId && requestedMessageId != Guid.Empty
            ? requestedMessageId
            : (Guid?)null;
        var normalizedEndpointId = endpointId is { } requestedEndpointId && requestedEndpointId != Guid.Empty
            ? requestedEndpointId
            : (Guid?)null;
        var attempts = await mediator.Send(
            new GetWebhookDeliveryAttemptsQuery
            {
                OwnerKindId = ownerKindId,
                OwnerId = ownerId,
                MessageId = normalizedMessageId,
                EndpointId = normalizedEndpointId,
                Limit = limit
            },
            cancellationToken);

        var resource = await webhookDeliveryAttemptAssembler.ToCollectionResource(
            attempts,
            RouteNames.GetWebhookDeliveryAttempts,
            await CreateCollectionRouteValuesAsync(
                ownerKindId,
                ownerId,
                limit,
                cancellationToken,
                messageId: normalizedMessageId,
                endpointId: normalizedEndpointId),
            HttpContext);

        return Ok(resource);
    }

    [HttpGet("delivery-attempts/{attemptId:guid}", Name = RouteNames.GetWebhookDeliveryAttemptById)]
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [EndpointSummary("Get webhook delivery attempt")]
    [EndpointDescription("Returns one tenant-scoped LocalProvider delivery attempt audit row.")]
    [ProducesResponseType(typeof(HalResource<WebhookDeliveryAttemptDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    public async Task<ActionResult<HalResource<WebhookDeliveryAttemptDto>>> GetDeliveryAttempt(
        Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        var attempt = await mediator.Send(
            new GetWebhookDeliveryAttemptByIdQuery
            {
                AttemptId = attemptId
            },
            cancellationToken);

        if (attempt is null)
        {
            return this.ToNotFoundProblem(DeliveryAttemptNotFoundProblem);
        }

        var resource = await webhookDeliveryAttemptAssembler.ToResource(attempt, HttpContext);
        return Ok(resource);
    }

    [HttpPost("delivery-attempts/{attemptId:guid}/retry", Name = RouteNames.RetryWebhookDeliveryAttempt)]
    [EndpointSummary("Retry webhook delivery attempt")]
    [EndpointDescription("Schedules a manual LocalProvider retry for a failed or abandoned webhook delivery attempt.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.DefaultPolicy)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> RetryDeliveryAttempt(
        Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(
            new RetryWebhookDeliveryAttemptCommand
            {
                AttemptId = attemptId
            },
            cancellationToken);

        return response.Success
            ? Ok(response)
            : ToWebhookDeliveryRetryProblem(response);
    }

    [HttpPost("incoming/{incomingWebhookMessageId:guid}/redrive", Name = RouteNames.RedriveIncomingWebhook)]
    [EndpointSummary("Redrive incoming webhook")]
    [EndpointDescription("Schedules a new processing generation for one tenant-scoped dead-lettered incoming webhook.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.DefaultPolicy)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> RedriveIncomingWebhook(
        Guid incomingWebhookMessageId,
        [FromBody] RedriveIncomingWebhookRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(
            new RedriveIncomingWebhookCommand
            {
                TenantId = tenantContext.TenantId,
                IncomingWebhookMessageId = incomingWebhookMessageId,
                ExpectedProcessingGeneration = request.ExpectedProcessingGeneration,
                Reason = request.Reason
            },
            cancellationToken);

        return response.Success
            ? Ok(response)
            : ToIncomingWebhookRedriveProblem(response);
    }

    private ActionResult<BaseCommandResponse<Guid>> ToWebhookConsumerProblem(BaseCommandResponse<Guid> response)
    {
        if (string.Equals(response.FailureCode, "webhook_consumer_not_found", StringComparison.Ordinal))
        {
            return ApiProblemFactory.ToProblemResult(
                ApiProblemFactory.CreateNotFoundProblem(HttpContext, ConsumerNotFoundProblem));
        }

        if (response.FailureCode is "webhook_consumer_name_conflict" or
            "webhook_consumer_configuration_conflict" or
            "webhook_consumer_provider_mode_unchanged" or
            "webhook_consumer_provider_mode_unavailable" or
            "webhook_consumer_provider_mode_local_target_required" or
            "webhook_consumer_provider_mode_binding_required" or
            "webhook_consumer_provider_mode_pending_migration_unsupported" or
            "webhook_consumer_provider_mode_uncertain_publications")
        {
            return this.ToCommandConflictProblem(
                response,
                "Webhook consumer configuration conflict",
                response.Message ?? "Webhook consumer configuration could not be changed.");
        }

        return this.ToCommandValidationProblem(response, ConsumerValidationProblem);
    }

    private ActionResult<BaseCommandResponse<Guid>> ToProviderBindingRepairProblem(
        BaseCommandResponse<Guid> response)
    {
        return response.FailureCode switch
        {
            "webhook_consumer_not_found" => ApiProblemFactory.ToProblemResult(
                ApiProblemFactory.CreateNotFoundProblem(HttpContext, ConsumerNotFoundProblem)),
            "webhook_provider_binding_mismatched" or
                "webhook_provider_application_not_found" or
                "webhook_provider_binding_repair_not_available" or
                "webhook_provider_binding_repair_conflict" => this.ToCommandConflictProblem(
                    response,
                    "Webhook provider binding cannot be repaired",
                    response.Message ?? "Webhook provider binding repair is unavailable."),
            "svix_provider_not_enabled" or
                "svix_self_hosted_profile_unsupported" or
                "webhook_provider_profile_unavailable" or
                "webhook_instance_identity_unavailable" or
                "svix_auth_token_secret_missing" or
                "svix_auth_token_unresolved" or
                "svix_auth_failed" or
                "svix_provider_unavailable" => ApiProblemFactory.ToProblemResult(
                    ApiProblemFactory.CreateServiceUnavailableProblem(
                        HttpContext,
                        "Webhook provider binding repair unavailable",
                        response.Message ?? "Webhook provider binding repair is temporarily unavailable.",
                        response.FailureCode ?? "webhook_provider_binding_repair_unavailable")),
            _ => this.ToCommandValidationProblem(response, ProviderBindingValidationProblem)
        };
    }

    private ActionResult<BaseCommandResponse<Guid>> ToWebhookEndpointProblem(BaseCommandResponse<Guid> response)
    {
        return response.FailureCode switch
        {
            "webhook_endpoint_url_conflict" => this.ToCommandConflictProblem(
                response,
                "Webhook endpoint already exists",
                "Webhook endpoint URL is already configured for this consumer."),
            "webhook_consumer_not_found" => ApiProblemFactory.ToProblemResult(
                ApiProblemFactory.CreateNotFoundProblem(HttpContext, ConsumerNotFoundProblem)),
            "webhook_endpoint_not_found" => ApiProblemFactory.ToProblemResult(
                ApiProblemFactory.CreateNotFoundProblem(HttpContext, EndpointNotFoundProblem)),
            _ => this.ToCommandValidationProblem(response, EndpointValidationProblem)
        };
    }

    private ActionResult<BaseCommandResponse<Guid>> ToWebhookDeliveryRetryProblem(BaseCommandResponse<Guid> response)
    {
        return response.FailureCode switch
        {
            "webhook_delivery_attempt_not_found" => ApiProblemFactory.ToProblemResult(
                ApiProblemFactory.CreateNotFoundProblem(HttpContext, DeliveryAttemptNotFoundProblem)),
            "webhook_delivery_retry_deferred" or
                "webhook_delivery_attempt_active" or
                "webhook_delivery_attempt_not_retryable" => this.ToCommandConflictProblem(
                    response,
                    "Webhook delivery retry not available",
                    response.Message ?? "Webhook delivery retry cannot be scheduled for this attempt."),
            _ => this.ToCommandValidationProblem(response, RetryValidationProblem)
        };
    }

    private ActionResult<BaseCommandResponse<Guid>> ToIncomingWebhookRedriveProblem(BaseCommandResponse<Guid> response)
    {
        return response.FailureCode switch
        {
            "incoming_webhook_not_found" => ApiProblemFactory.ToProblemResult(
                ApiProblemFactory.CreateNotFoundProblem(HttpContext, IncomingWebhookNotFoundProblem)),
            "incoming_webhook_redrive_generation_conflict" or
                "incoming_webhook_redrive_active_lease" or
                "incoming_webhook_redrive_not_eligible" => this.ToCommandConflictProblem(
                    response,
                    "Incoming webhook redrive not available",
                    response.Message ?? "Incoming webhook cannot be redriven in its current state."),
            _ => this.ToCommandValidationProblem(response, IncomingRedriveValidationProblem)
        };
    }

    private async Task<WebhookCollectionRouteValues> CreateCollectionRouteValuesAsync(
        int ownerKindId,
        Guid? ownerId,
        int limit,
        CancellationToken cancellationToken,
        Guid? consumerId = null,
        Guid? messageId = null,
        Guid? endpointId = null)
    {
        var resolution = await webhookOwnershipScopeResolver.ResolveAsync(
            ownerKindId,
            ownerId,
            cancellationToken);
        var ownership = resolution.Scope ?? throw new InvalidOperationException(
            "Webhook collection ownership was not resolved after request authorization.");

        return new WebhookCollectionRouteValues(
            ownership,
            limit,
            consumerId,
            messageId,
            endpointId);
    }

    private string ResolvePortalSessionId()
    {
        return ResolveProviderSubject()
            ?? CurrentUserId?.ToString("D")
            ?? string.Empty;
    }

    private ActionResult<WebhookProviderPortalAccessDto> ToWebhookPortalProblem(
        WebhookProviderPortalAccessCommandResponse response)
    {
        var code = string.IsNullOrWhiteSpace(response.FailureCode)
            ? "webhook_provider_portal_failed"
            : response.FailureCode;
        var detail = response.Message ?? "Webhook provider portal access could not be created.";

        return code switch
        {
            "webhook_portal_validation_failed" or "webhook_portal_session_required" =>
                ApiProblemFactory.ToProblemResult(ApiProblemFactory.CreateValidationProblem(
                    HttpContext,
                    PortalValidationProblem,
                    response.Errors ?? [detail],
                    detail,
                    code)),

            "webhook_consumer_not_found" =>
                ApiProblemFactory.ToProblemResult(ApiProblemFactory.CreateNotFoundProblem(
                    HttpContext,
                    ConsumerNotFoundProblem)),

            "webhook_consumer_disabled"
                or "webhook_provider_binding_unverified"
                or "webhook_provider_binding_mismatched"
                or "webhook_provider_capability_unavailable" =>
                ApiProblemFactory.ToProblemResult(ApiProblemFactory.CreateConflictProblem(
                    HttpContext,
                    "Webhook provider portal unavailable",
                    detail,
                    code)),

            "svix_provider_not_enabled"
                or "svix_app_portal_disabled"
                or "svix_auth_token_secret_missing"
                or "svix_auth_token_unresolved" =>
                ApiProblemFactory.ToProblemResult(ApiProblemFactory.CreateServiceUnavailableProblem(
                    HttpContext,
                    "Webhook provider portal unavailable",
                    detail,
                    code)),

            _ when response.IsRetryable =>
                ApiProblemFactory.ToProblemResult(ApiProblemFactory.CreateServiceUnavailableProblem(
                    HttpContext,
                    "Webhook provider temporarily unavailable",
                    detail,
                    code)),

            _ =>
                ApiProblemFactory.ToProblemResult(ApiProblemFactory.CreateBadGatewayProblem(
                    HttpContext,
                    "Webhook provider request failed",
                    detail,
                    code))
        };
    }

    private sealed class WebhookCollectionRouteValues(
        WebhookOwnershipScope ownership,
        int limit,
        Guid? consumerId,
        Guid? messageId,
        Guid? endpointId) : ICollectionAuthorizationContext
    {
        private readonly IReadOnlyDictionary<string, object> _authorizationResourceAttributes =
            ResourceDescriptors.GetWebhookOwnerAttributes(ownership);

        public int OwnerKindId => (int)ownership.Kind;

        public Guid OwnerId => ownership.OwnerId;

        public int Limit => limit;

        public Guid? ConsumerId => consumerId;

        public Guid? MessageId => messageId;

        public Guid? EndpointId => endpointId;

        string ICollectionAuthorizationContext.AuthorizationResourceId => ownership.OwnerId.ToString();

        IReadOnlyDictionary<string, object> ICollectionAuthorizationContext.AuthorizationResourceAttributes =>
            _authorizationResourceAttributes;
    }
}
