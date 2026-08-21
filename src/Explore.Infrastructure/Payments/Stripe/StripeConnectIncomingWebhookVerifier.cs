// ABOUTME: Verifies signed Stripe Connect account webhooks and maps them to tenant-owned connections.
// ABOUTME: Uses Stripe.net signature validation while keeping SDK types inside Infrastructure.

using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Secrets;
using Explore.Application.Contracts.Webhooks;
using Explore.Domain.Secrets;
using Explore.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Payments.Stripe;

public sealed class StripeConnectIncomingWebhookVerifier(
    IOptionsMonitor<WebhookOptions> options,
    IOptions<StripePaymentOptions> stripeOptions,
    ISecretResolver secretResolver,
    IOrganizerPaymentProviderConnectionRepository connectionRepository,
    ILogger<StripeConnectIncomingWebhookVerifier> logger) : IIncomingWebhookVerifier
{
    public const string ProviderCode = "stripe-connect";
    private const string OrganizerPaymentProviderCode = "stripe";
    private const int HistoricalMatchLimit = 2;

    public string Provider => ProviderCode;

    public async Task<IncomingWebhookVerificationResult> VerifyAsync(
        IncomingWebhookContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Headers.TryGetValue("Stripe-Signature", out string? signature) || string.IsNullOrWhiteSpace(signature))
        {
            return IncomingWebhookVerificationResult.Rejected("stripe_connect_signature_missing");
        }

        ResolvedSecret? secret = await secretResolver.ResolveAsync(
            options.CurrentValue.Stripe.ConnectWebhookSecretRef ?? SecretDefinitionRegistry.Keys.Stripe.WebhookSecret,
            tenantId: null,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(secret?.Value))
        {
            return IncomingWebhookVerificationResult.Rejected("stripe_connect_secret_unavailable");
        }

        global::Stripe.Event stripeEvent;
        try
        {
            stripeEvent = global::Stripe.EventUtility.ConstructEvent(
                context.RawPayload,
                signature,
                secret.Value,
                tolerance: 300,
                throwOnApiVersionMismatch: true);
        }
        catch (global::Stripe.StripeException)
        {
            logger.LogWarning("Stripe Connect webhook rejected with safe category {FailureCategory}.", "stripe_connect_signature_invalid");
            return IncomingWebhookVerificationResult.Rejected("stripe_connect_signature_invalid");
        }
        catch (JsonException)
        {
            return IncomingWebhookVerificationResult.Rejected("stripe_connect_payload_invalid");
        }

        if (!IsSupportedEvent(stripeEvent.Type))
        {
            return IncomingWebhookVerificationResult.Rejected("stripe_connect_event_unsupported");
        }

        if (!StripeModeEvidence.TryReadEventLivemode(context.RawPayload, out bool eventLivemode))
        {
            return IncomingWebhookVerificationResult.Rejected("stripe_connect_event_mode_missing");
        }

        if (!StripeModeEvidence.Matches(stripeOptions.Value, eventLivemode))
        {
            return IncomingWebhookVerificationResult.Rejected("stripe_connect_event_mode_mismatch");
        }

        if (!TryGetEventId(stripeEvent, out string? eventId))
        {
            return IncomingWebhookVerificationResult.Rejected("stripe_connect_event_id_invalid");
        }

        if (!TryGetAccountId(stripeEvent, out string? accountId))
        {
            return IncomingWebhookVerificationResult.Rejected("stripe_connect_account_missing");
        }

        string verifiedAccountId = accountId!;
        if (string.Equals(stripeEvent.Type, global::Stripe.EventTypes.AccountUpdated, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(stripeEvent.Account)
            && !string.Equals(stripeEvent.Account, verifiedAccountId, StringComparison.Ordinal))
        {
            return IncomingWebhookVerificationResult.Rejected("stripe_connect_account_mismatch");
        }

        if (!TryGetEventCreatedAt(stripeEvent, out _))
        {
            return IncomingWebhookVerificationResult.Rejected("stripe_connect_event_timestamp_invalid");
        }

        IReadOnlyList<Domain.OrganizerPaymentProviderConnection> matches = await connectionRepository.ListHistoricalByExternalAccountAsync(
            OrganizerPaymentProviderCode,
            verifiedAccountId,
            HistoricalMatchLimit,
            cancellationToken);
        if (matches.Count != 1)
        {
            return IncomingWebhookVerificationResult.Rejected("stripe_connect_account_not_unique");
        }

        var connection = matches[0];
        if (IsPaymentEvent(stripeEvent.Type))
        {
            string? objectId = stripeEvent.Data.Object is global::Stripe.Checkout.Session session
                ? StripeConnectAccountAdapter.BoundedText(session.Id, 200)
                : null;
            if (objectId is null)
            {
                return IncomingWebhookVerificationResult.Rejected("stripe_connect_object_id_invalid");
            }

            var envelope = new StripePaymentWebhookEnvelope(
                eventId!,
                stripeEvent.Type,
                objectId,
                verifiedAccountId,
                eventLivemode,
                stripeEvent.ApiVersion,
                stripeEvent.Created);
            return IncomingWebhookVerificationResult.VerifiedTenantCredential(
                connection.TenantId,
                eventId!,
                stripeEvent.Type,
                $"{stripeEvent.Type}:{objectId}",
                envelope.Serialize());
        }

        return IncomingWebhookVerificationResult.VerifiedTenantCredential(
            connection.TenantId,
            eventId!,
            stripeEvent.Type,
            eventId!);
    }

    private static bool TryGetEventId(global::Stripe.Event stripeEvent, out string? eventId)
    {
        eventId = StripeConnectAccountAdapter.BoundedText(stripeEvent.Id, 120);
        return eventId is not null;
    }

    private static bool TryGetAccountId(global::Stripe.Event stripeEvent, out string? accountId)
    {
        accountId = StripeConnectAccountAdapter.BoundedText(stripeEvent.Account, 200);
        if (!string.IsNullOrWhiteSpace(accountId)
            && !string.Equals(stripeEvent.Type, global::Stripe.EventTypes.AccountUpdated, StringComparison.Ordinal))
        {
            return true;
        }

        if (stripeEvent.Data.Object is global::Stripe.Account account && !string.IsNullOrWhiteSpace(account.Id))
        {
            accountId = account.Id;
            return true;
        }

        return !string.IsNullOrWhiteSpace(accountId);
    }

    private static bool IsSupportedEvent(string? eventType) =>
        string.Equals(eventType, global::Stripe.EventTypes.AccountUpdated, StringComparison.Ordinal)
        || string.Equals(eventType, global::Stripe.EventTypes.AccountApplicationDeauthorized, StringComparison.Ordinal)
        || IsPaymentEvent(eventType);

    private static bool IsPaymentEvent(string? eventType) =>
        string.Equals(eventType, global::Stripe.EventTypes.CheckoutSessionCompleted, StringComparison.Ordinal)
        || string.Equals(eventType, global::Stripe.EventTypes.CheckoutSessionAsyncPaymentSucceeded, StringComparison.Ordinal)
        || string.Equals(eventType, global::Stripe.EventTypes.CheckoutSessionAsyncPaymentFailed, StringComparison.Ordinal)
        || string.Equals(eventType, global::Stripe.EventTypes.CheckoutSessionExpired, StringComparison.Ordinal);

    private static bool TryGetEventCreatedAt(global::Stripe.Event stripeEvent, out DateTime createdAt)
    {
        createdAt = stripeEvent.Created;
        return createdAt != default && createdAt.Kind == DateTimeKind.Utc;
    }
}
