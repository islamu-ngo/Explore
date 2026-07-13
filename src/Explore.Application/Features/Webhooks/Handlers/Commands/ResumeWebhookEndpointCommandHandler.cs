// ABOUTME: Applies the manual transition from AutoPaused to Active for one tenant webhook endpoint.
// ABOUTME: Fails closed for missing, archived, disabled, active, or concurrently changed endpoints.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Webhooks.Handlers.Commands;

public sealed class ResumeWebhookEndpointCommandHandler(IWebhookEndpointRepository endpointRepository)
    : IRequestHandler<ResumeWebhookEndpointCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        ResumeWebhookEndpointCommand request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId == Guid.Empty
            || request.EndpointId == Guid.Empty
            || request.ActorUserId == Guid.Empty)
        {
            return Failure(
                request.EndpointId,
                "webhook_endpoint_resume_validation_failed",
                "Tenant, endpoint, and actor identifiers are required.");
        }

        var endpoint = await endpointRepository.GetByTenantAndIdAsync(
            request.TenantId,
            request.EndpointId,
            cancellationToken);
        if (endpoint is null || endpoint.Status == WebhookEndpointStatus.Archived)
        {
            return Failure(
                request.EndpointId,
                "webhook_endpoint_not_found",
                "Webhook endpoint was not found.");
        }

        if (endpoint.Status != WebhookEndpointStatus.AutoPaused)
        {
            return Failure(
                request.EndpointId,
                "webhook_endpoint_not_auto_paused",
                "Only an automatically paused webhook endpoint can be resumed.");
        }

        var resumed = await endpointRepository.TryResumeAsync(
            request.TenantId,
            request.EndpointId,
            DateTime.UtcNow,
            request.ActorUserId,
            cancellationToken);
        if (!resumed)
        {
            return Failure(
                request.EndpointId,
                "webhook_endpoint_resume_conflict",
                "Webhook endpoint state changed before it could be resumed.");
        }

        return new BaseCommandResponse<Guid>
        {
            Id = request.EndpointId,
            Success = true,
            Message = "Webhook endpoint resumed."
        };
    }

    private static BaseCommandResponse<Guid> Failure(Guid endpointId, string code, string message) =>
        new()
        {
            Id = endpointId,
            Success = false,
            Message = message,
            FailureCode = code,
            Errors = [message]
        };
}
