// ABOUTME: Handles tenant-scoped webhook message audit list reads for management APIs.
// ABOUTME: Applies conservative bounds before repository access and maps entities in Application.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Webhooks;
using Explore.Application.Features.Webhooks.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Webhooks.Handlers.Queries;

public sealed class GetWebhookMessagesQueryHandler(IWebhookMessageRepository messageRepository)
    : IRequestHandler<GetWebhookMessagesQuery, IReadOnlyList<WebhookMessageDto>>
{
    private const int DefaultLimit = 100;
    private const int MaxLimit = 500;

    public async Task<IReadOnlyList<WebhookMessageDto>> Handle(
        GetWebhookMessagesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId == Guid.Empty)
        {
            return [];
        }

        var limit = request.Limit <= 0
            ? DefaultLimit
            : Math.Min(request.Limit, MaxLimit);

        var messages = await messageRepository.ListByTenantAsync(
            request.TenantId,
            limit,
            cancellationToken);

        return messages.Select(WebhookMessageDtoMapper.Map).ToList();
    }
}
