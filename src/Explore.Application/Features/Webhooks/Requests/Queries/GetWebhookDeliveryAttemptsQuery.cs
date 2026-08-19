// ABOUTME: Authorized query for typed owner-scoped Local webhook delivery attempt audit rows.
// ABOUTME: Supports bounded message and endpoint filters inside canonical configuration ownership.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Webhooks;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Queries;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.ViewDelivery)]
public sealed class GetWebhookDeliveryAttemptsQuery
    : IRequest<IReadOnlyList<WebhookDeliveryAttemptDto>>, ISecureRequest, IWebhookOwnerScopedRequest
{
    public int OwnerKindId { get; init; }

    public Guid? OwnerId { get; init; }

    public Guid? MessageId { get; init; }

    public Guid? EndpointId { get; init; }

    public int Limit { get; init; } = 100;

    string? ISecureRequest.ResourceId => OwnerId?.ToString("D");

}
