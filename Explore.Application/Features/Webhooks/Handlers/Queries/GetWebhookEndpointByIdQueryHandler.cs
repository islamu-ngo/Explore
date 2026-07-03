// ABOUTME: Handles tenant-scoped webhook endpoint detail queries.
// ABOUTME: Returns null for missing rows and maps found endpoints into secret-safe DTOs.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Webhooks;
using Explore.Application.Features.Webhooks.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Webhooks.Handlers.Queries;

public sealed class GetWebhookEndpointByIdQueryHandler(
    IWebhookEndpointRepository endpointRepository)
    : IRequestHandler<GetWebhookEndpointByIdQuery, WebhookEndpointDto?>
{
    public async Task<WebhookEndpointDto?> Handle(
        GetWebhookEndpointByIdQuery request,
        CancellationToken cancellationToken)
    {
        var endpoint = await endpointRepository.GetByTenantAndIdAsync(
            request.TenantId,
            request.EndpointId,
            cancellationToken);

        return endpoint is null
            ? null
            : WebhookEndpointDtoMapper.Map(endpoint);
    }
}
