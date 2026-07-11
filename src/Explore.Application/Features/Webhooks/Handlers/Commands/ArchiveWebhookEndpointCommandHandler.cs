// ABOUTME: Handles endpoint archive requests with tenant-scoped not-found behavior.
// ABOUTME: Archives instead of deleting rows so delivery history and provider links remain auditable.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Webhooks.Handlers.Commands;

public sealed class ArchiveWebhookEndpointCommandHandler(IWebhookEndpointRepository endpointRepository)
    : IRequestHandler<ArchiveWebhookEndpointCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        ArchiveWebhookEndpointCommand request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId == Guid.Empty || request.EndpointId == Guid.Empty)
        {
            return Failure("webhook_endpoint_validation_failed", ["Tenant id and endpoint id are required."]);
        }

        var endpoint = await endpointRepository.GetByTenantAndIdAsync(
            request.TenantId,
            request.EndpointId,
            cancellationToken);
        if (endpoint is null)
        {
            return Failure("webhook_endpoint_not_found", ["Webhook endpoint was not found."]);
        }

        if (endpoint.Status != WebhookEndpointStatus.Archived)
        {
            await endpointRepository.ArchiveAsync(
                request.TenantId,
                request.EndpointId,
                DateTime.UtcNow,
                cancellationToken);
        }

        return new BaseCommandResponse<Guid>
        {
            Id = request.EndpointId,
            Success = true,
            Message = "Webhook endpoint archived."
        };
    }

    private static BaseCommandResponse<Guid> Failure(string code, IReadOnlyList<string> errors) =>
        new()
        {
            Success = false,
            Message = errors[0],
            FailureCode = code,
            Errors = errors.ToList()
        };
}
