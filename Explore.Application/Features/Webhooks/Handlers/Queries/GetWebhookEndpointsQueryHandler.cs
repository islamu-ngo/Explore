// ABOUTME: Handles tenant-scoped webhook endpoint list queries.
// ABOUTME: Caps list size and maps endpoint entities into secret-safe DTOs.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Webhooks;
using Explore.Application.Features.Webhooks.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Webhooks.Handlers.Queries;

public sealed class GetWebhookEndpointsQueryHandler(
    IWebhookEndpointRepository endpointRepository)
    : IRequestHandler<GetWebhookEndpointsQuery, IReadOnlyList<WebhookEndpointDto>>
{
    private const int DefaultLimit = 100;
    private const int MaxLimit = 500;

    public async Task<IReadOnlyList<WebhookEndpointDto>> Handle(
        GetWebhookEndpointsQuery request,
        CancellationToken cancellationToken)
    {
        var limit = request.Limit <= 0
            ? DefaultLimit
            : Math.Min(request.Limit, MaxLimit);

        var endpoints = await endpointRepository.ListByTenantAsync(
            request.TenantId,
            request.ConsumerId,
            limit,
            cancellationToken);

        return endpoints
            .Select(WebhookEndpointDtoMapper.Map)
            .ToArray();
    }
}
