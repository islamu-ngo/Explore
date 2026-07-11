// ABOUTME: API boundary service for validating signed Coop webhook request bodies.
// ABOUTME: Lets controllers stay thin while preserving raw-body HMAC verification.

namespace Explore.API.Services;

public interface ICoopWebhookSignatureValidator
{
    Task<CoopWebhookSignatureValidationResult> ReadAndValidateAsync(
        HttpRequest request,
        CancellationToken cancellationToken);
}
