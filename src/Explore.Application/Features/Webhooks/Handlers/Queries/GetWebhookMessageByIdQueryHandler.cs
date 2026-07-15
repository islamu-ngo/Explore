// ABOUTME: Handles persisted-owner webhook message detail reads for management APIs.
// ABOUTME: Returns safe metadata DTOs without raw payload JSON.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Webhooks;
using Explore.Application.Features.Webhooks.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Webhooks.Handlers.Queries;

public sealed class GetWebhookMessageByIdQueryHandler(IWebhookMessageRepository messageRepository)
    : IRequestHandler<GetWebhookMessageByIdQuery, WebhookMessageDto?>
{
    public async Task<WebhookMessageDto?> Handle(
        GetWebhookMessageByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (request.MessageId == Guid.Empty)
        {
            return null;
        }

        var message = await messageRepository.GetByIdForOwnerOperationAsync(
            request.MessageId,
            cancellationToken);

        return message is null ? null : WebhookMessageDtoMapper.Map(message);
    }
}
