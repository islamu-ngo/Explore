// ABOUTME: Adapts provider-neutral registration callback verification to the shared incoming-webhook intake service.
// ABOUTME: Produces one stable registration.provider_submission effect identity for durable outbox processing.

using System.Security.Cryptography;
using System.Text.Json;
using Explore.API.Controllers;
using Explore.Application.Contracts.Services.Registration;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.RegistrationSubmissions.Commands;
using Explore.Domain;

namespace Explore.API.Services;

public sealed class RegistrationProviderIncomingWebhookVerifier(
    IRegistrationProviderCallbackBindingResolver bindingResolver,
    IRegistrationProviderCallbackVerifier callbackVerifier,
    IRegistrationProviderCallbackReceiptProtector receiptProtector) : IIncomingWebhookVerifier
{
    public const string IntakeProvider = "registration-provider";

    public string Provider => IntakeProvider;

    public async Task<IncomingWebhookVerificationResult> VerifyAsync(
        IncomingWebhookContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Headers.TryGetValue(RegistrationProviderCallbackController.ProviderHeader, out string? provider) ||
            !context.Headers.TryGetValue(RegistrationProviderCallbackController.BindingHeader, out string? bindingValue) ||
            !Guid.TryParse(bindingValue, out Guid bindingId))
        {
            return IncomingWebhookVerificationResult.Rejected("registration_callback_route_invalid", "The callback route is invalid.");
        }

        RegistrationProviderBinding? binding = await bindingResolver.ResolveForCallbackAsync(provider, bindingId, cancellationToken);
        if (binding is null)
        {
            return IncomingWebhookVerificationResult.Rejected("registration_callback_binding_unknown", "The registration callback could not be verified.");
        }

        RegistrationProviderCallbackVerificationResult verified;
        string providerSubmissionId;
        RegistrationProviderTuple tuple;
        try
        {
            verified = await callbackVerifier.VerifyCallbackAsync(
                new RegistrationProviderCallbackVerificationRequest(binding.TenantId, binding.RegistrationProviderConnectionId,
                    context.RawPayloadBytes, context.Headers), cancellationToken);
            providerSubmissionId = ReadProviderSubmissionId(context.RawPayloadBytes.Span);
            tuple = ResolveTuple(binding, provider);
        }
        catch (Exception exception) when (exception is JsonException or FormatException or InvalidOperationException or ArgumentException)
        {
            return IncomingWebhookVerificationResult.Rejected("registration_callback_format_invalid", "The registration callback could not be verified.");
        }

        if (!verified.IsVerified || string.IsNullOrWhiteSpace(verified.Receipt) || tuple.ProviderCode.Length == 0)
        {
            return IncomingWebhookVerificationResult.Rejected("registration_callback_unverified", "The registration callback could not be verified.");
        }

        string protectedReceipt = receiptProtector.Protect(new RegistrationProviderCallbackReceipt(
            binding.TenantId,
            binding.RegistrationProviderConnectionId,
            binding.Id,
            provider.Trim(),
            tuple.Key,
            ComputePayloadHash(context.RawPayloadBytes.Span),
            providerSubmissionId,
            context.ReceivedAt,
            Guid.CreateVersion7().ToString("N")));

        IncomingWebhookVerificationResult result = IncomingWebhookVerificationResult.VerifiedTenantCredential(
            binding.TenantId,
            $"{binding.Id:N}:{providerSubmissionId}",
            ProcessProviderSubmissionEffectCommandHandler.StableEffectKind,
            $"{binding.Id:N}:{providerSubmissionId}");
        return result with { Receipt = protectedReceipt };
    }

    private static string ReadProviderSubmissionId(ReadOnlySpan<byte> payload)
    {
        using JsonDocument document = JsonDocument.Parse(payload.ToArray());
        string? value = document.RootElement.TryGetProperty("providerSubmissionId", out JsonElement property) &&
            property.ValueKind == JsonValueKind.String
            ? property.GetString()?.Trim()
            : null;
        return !string.IsNullOrWhiteSpace(value) && value.Length <= 200
            ? value
            : throw new JsonException("Provider submission identity is missing or too large.");
    }

    private static RegistrationProviderTuple ResolveTuple(RegistrationProviderBinding binding, string provider)
    {
        RegistrationProviderCapability? capability = binding.Capabilities.FirstOrDefault(capability =>
            !capability.IsDeleted &&
            string.Equals(capability.ProviderCode, provider, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(capability.CapabilityCode, RegistrationProviderCapabilityCodes.CallbackVerification, StringComparison.OrdinalIgnoreCase));
        return capability is null
            ? new RegistrationProviderTuple(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)
            : new RegistrationProviderTuple(capability.ProviderCode, capability.DeploymentKind, capability.ApiVersion,
                capability.AdapterPolicyVersion, capability.ConformanceEvidenceRevision);
    }

    private static string ComputePayloadHash(ReadOnlySpan<byte> bodyBytes) =>
        "sha256:" + Convert.ToHexString(SHA256.HashData(bodyBytes)).ToLowerInvariant();
}
