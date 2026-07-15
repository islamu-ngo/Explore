// ABOUTME: Handles endpoint archive requests with persisted-owner not-found behavior.
// ABOUTME: Archives instead of deleting rows so delivery history and provider links remain auditable.

using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Webhooks.Handlers.Commands;

public sealed class ArchiveWebhookEndpointCommandHandler(
    IWebhookEndpointRepository endpointRepository,
    IWebhookProviderCapabilityResolver capabilityResolver,
    IWebhookAuditEventWriter auditWriter,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ArchiveWebhookEndpointCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        ArchiveWebhookEndpointCommand request,
        CancellationToken cancellationToken)
    {
        if (request.EndpointId == Guid.Empty)
        {
            return Failure("webhook_endpoint_validation_failed", ["Endpoint id is required."]);
        }

        var endpoint = await endpointRepository.GetByIdForOwnerOperationAsync(
            request.EndpointId,
            forUpdate: false,
            cancellationToken);
        if (endpoint is null)
        {
            return Failure("webhook_endpoint_not_found", ["Webhook endpoint was not found."]);
        }

        if (endpoint.Consumer is null)
        {
            return Failure(
                "webhook_endpoint_management_unavailable",
                ["Webhook consumer was not found."]);
        }

        if (!WebhookEndpointCapabilityPolicy.CanManageLocalEndpoint(
                capabilityResolver,
                endpoint.Consumer.ProviderMode,
                out var capabilityFailure))
        {
            return Failure(
                "webhook_endpoint_management_unavailable",
                [capabilityFailure]);
        }

        if (endpoint.Status != WebhookEndpointStatus.Archived)
        {
            await unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                await endpointRepository.ArchiveAsync(
                    endpoint.TenantId,
                    request.EndpointId,
                    DateTime.UtcNow,
                    token);
                await auditWriter.AppendAsync(
                    new WebhookAuditWriteRequest(
                        endpoint.TenantId,
                        WebhookAuditAction.EndpointArchived,
                        WebhookAuditTargetKind.Endpoint,
                        endpoint.Id,
                        "endpoint_archived",
                        WebhookAuditOutcome.Succeeded,
                        SafeBeforeJson: JsonSerializer.Serialize(new
                        {
                            status = endpoint.Status.ToString(),
                            endpoint.DeliveryStateVersion
                        }),
                        SafeAfterJson: JsonSerializer.Serialize(new
                        {
                            status = WebhookEndpointStatus.Archived.ToString(),
                            deliveryStateVersion = endpoint.DeliveryStateVersion + 1
                        }),
                        ConfigurationVersion: $"endpoint-v{endpoint.ConfigurationVersion}:delivery-v{endpoint.DeliveryStateVersion + 1}",
                        EffectiveScopeKind: endpoint.Consumer.Ownership.AuditScopeKind,
                        EffectiveScopeId: endpoint.Consumer.OwnerId),
                    token);
            }, cancellationToken);
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
