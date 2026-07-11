// ABOUTME: Handles Svix App Portal access creation through the provider-neutral webhook portal contract.
// ABOUTME: Keeps provider SDK calls in Infrastructure while preserving command-response API conventions.

using Explore.Application.Contracts.Webhooks;
using Explore.Application.DTOs.Webhooks;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Handlers.Commands;

public sealed class OpenSvixAppPortalCommandHandler(IWebhookProviderPortalService portalService)
    : IRequestHandler<OpenSvixAppPortalCommand, WebhookProviderPortalAccessCommandResponse>
{
    private const string ValidationFailure = "webhook_portal_validation_failed";

    public async Task<WebhookProviderPortalAccessCommandResponse> Handle(
        OpenSvixAppPortalCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return Failure(
                "Webhook provider portal access validation failed.",
                ValidationFailure,
                isRetryable: false,
                validationErrors);
        }

        var result = await portalService.CreateAccessAsync(
            new WebhookProviderPortalAccessInput(
                request.TenantId,
                request.ConsumerId,
                request.SessionId,
                request.ReadOnly,
                request.ExpiresInSeconds is { } seconds ? TimeSpan.FromSeconds(seconds) : null,
                request.FeatureFlags),
            cancellationToken);

        if (!result.Succeeded)
        {
            var failureCategory = string.IsNullOrWhiteSpace(result.FailureCategory)
                ? "webhook_provider_portal_failed"
                : result.FailureCategory;

            return Failure(
                ResolveFailureMessage(failureCategory),
                failureCategory,
                result.IsRetryable,
                [result.SafeDetail ?? failureCategory]);
        }

        return new WebhookProviderPortalAccessCommandResponse
        {
            Success = true,
            Message = "Webhook provider portal access created.",
            Id = new WebhookProviderPortalAccessDto
            {
                Url = result.Url!,
                Token = result.Token,
                ExpiresAt = result.ExpiresAt!.Value
            }
        };
    }

    private static List<string> Validate(OpenSvixAppPortalCommand request)
    {
        var errors = new List<string>();

        if (request.TenantId == Guid.Empty)
        {
            errors.Add("TenantId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            errors.Add("SessionId is required.");
        }

        if (request.ExpiresInSeconds is <= 0)
        {
            errors.Add("ExpiresInSeconds must be greater than zero when provided.");
        }

        return errors;
    }

    private static WebhookProviderPortalAccessCommandResponse Failure(
        string message,
        string failureCode,
        bool isRetryable,
        List<string> errors)
        => new()
        {
            Success = false,
            Message = message,
            FailureCode = failureCode,
            IsRetryable = isRetryable,
            Errors = errors
        };

    private static string ResolveFailureMessage(string failureCategory) => failureCategory switch
    {
        "svix_provider_not_enabled" => "Svix webhook provider is not enabled.",
        "svix_app_portal_disabled" => "Svix App Portal is disabled.",
        "webhook_consumer_not_found" => "Webhook consumer was not found.",
        "webhook_portal_session_required" => "Webhook provider portal session id is required.",
        "svix_auth_token_secret_missing" => "Svix auth token secret reference is not configured.",
        "svix_auth_token_unresolved" => "Svix auth token secret could not be resolved.",
        "svix_auth_failed" => "Svix rejected the configured credentials.",
        "svix_request_rejected" => "Svix rejected the App Portal request.",
        "svix_provider_unavailable" => "Svix is temporarily unavailable.",
        _ => "Webhook provider portal access could not be created."
    };
}
