// ABOUTME: Handles tenant-scoped webhook consumer list reads for management APIs.
// ABOUTME: Applies conservative bounds before repository access and maps entities in Application.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Webhooks;
using Explore.Application.Features.Webhooks.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Webhooks.Handlers.Queries;

public sealed class GetWebhookConsumersQueryHandler(IWebhookConsumerRepository consumerRepository)
    : IRequestHandler<GetWebhookConsumersQuery, IReadOnlyList<WebhookConsumerDto>>
{
    private const int DefaultLimit = 100;
    private const int MaxLimit = 500;

    public async Task<IReadOnlyList<WebhookConsumerDto>> Handle(
        GetWebhookConsumersQuery request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId == Guid.Empty)
        {
            return [];
        }

        var limit = request.Limit <= 0
            ? DefaultLimit
            : Math.Min(request.Limit, MaxLimit);

        var consumers = await consumerRepository.ListByTenantAsync(
            request.TenantId,
            limit,
            cancellationToken);

        return consumers.Select(WebhookConsumerDtoMapper.Map).ToList();
    }
}
