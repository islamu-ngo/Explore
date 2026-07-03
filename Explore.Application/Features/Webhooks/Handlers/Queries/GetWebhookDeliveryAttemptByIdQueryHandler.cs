// ABOUTME: Handles tenant-scoped webhook delivery attempt detail reads.
// ABOUTME: Maps LocalProvider delivery ledger entities to safe operations DTOs.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Webhooks;
using Explore.Application.Features.Webhooks.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Webhooks.Handlers.Queries;

public sealed class GetWebhookDeliveryAttemptByIdQueryHandler(IWebhookDeliveryAttemptRepository attemptRepository)
    : IRequestHandler<GetWebhookDeliveryAttemptByIdQuery, WebhookDeliveryAttemptDto?>
{
    public async Task<WebhookDeliveryAttemptDto?> Handle(
        GetWebhookDeliveryAttemptByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId == Guid.Empty || request.AttemptId == Guid.Empty)
        {
            return null;
        }

        var attempt = await attemptRepository.GetByTenantAndIdAsync(
            request.TenantId,
            request.AttemptId,
            cancellationToken);

        return attempt is null ? null : WebhookDeliveryAttemptDtoMapper.Map(attempt);
    }
}
