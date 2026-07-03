// ABOUTME: Verifies signed Svix operational callbacks using the configured webhook signing secret.
// ABOUTME: Reuses the Svix-compatible signature service and secret resolver without depending on outgoing mode.

using Explore.Application.Contracts.Secrets;
using Explore.Application.Contracts.Webhooks;
using Explore.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Explore.API.Services;

public sealed class SvixIncomingWebhookVerifier(
    IOptionsMonitor<WebhookOptions> options,
    ISecretResolver secretResolver,
    IWebhookSignatureService signatureService,
    ILogger<SvixIncomingWebhookVerifier> logger) : IIncomingWebhookVerifier
{
    public string Provider => "svix";

    public async Task<IncomingWebhookVerificationResult> VerifyAsync(
        IncomingWebhookContext context,
        CancellationToken cancellationToken)
    {
        var secretRef = options.CurrentValue.Svix.OperationalWebhookSecretRef?.Trim();
        if (string.IsNullOrWhiteSpace(secretRef))
        {
            logger.LogWarning("Svix operational webhook rejected because Webhooks:Svix:OperationalWebhookSecretRef is not configured.");
            return IncomingWebhookVerificationResult.Rejected(
                "svix_operational_webhook_secret_missing",
                "The Svix operational webhook signing secret is not configured.");
        }

        var resolved = await secretResolver.ResolveAsync(secretRef, tenantId: null, cancellationToken);
        if (resolved is null || string.IsNullOrWhiteSpace(resolved.Value))
        {
            logger.LogWarning("Svix operational webhook rejected because the signing secret could not be resolved.");
            return IncomingWebhookVerificationResult.Rejected(
                "svix_operational_webhook_secret_unresolved",
                "The Svix operational webhook signing secret could not be resolved.");
        }

        var verification = signatureService.Verify(
            context.RawPayload,
            context.Headers,
            new WebhookSecretMaterial(resolved.Value, CurrentSecretVersion: 1));
        if (!verification.IsValid)
        {
            return IncomingWebhookVerificationResult.Rejected(
                $"svix_webhook_{verification.FailureCategory ?? "verification_failed"}",
                "The Svix operational webhook signature could not be verified.");
        }

        var providerMessageId = TryGetHeader(context.Headers, "svix-id", out var svixId)
            ? svixId
            : $"svix:{context.ReceivedAt.ToUnixTimeMilliseconds()}";
        var eventType = TryGetHeader(context.Headers, "svix-event-type", out var svixEventType)
            ? svixEventType
            : null;

        return IncomingWebhookVerificationResult.Verified(providerMessageId, eventType, providerMessageId);
    }

    private static bool TryGetHeader(
        IReadOnlyDictionary<string, string> headers,
        string headerName,
        out string value)
    {
        value = string.Empty;
        return headers.TryGetValue(headerName, out value!) && !string.IsNullOrWhiteSpace(value);
    }
}
