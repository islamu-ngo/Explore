// ABOUTME: Stripe.net hosted Checkout create and retrieve adapter for connected-account direct charges.
// ABOUTME: Shapes immutable money requests and maps transport/provider ambiguity into bounded Application results.

using System.Net;
using Explore.Application.Contracts.Payments;
using Explore.Application.Contracts.Secrets;
using Explore.Domain.Secrets;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Payments.Stripe.Checkout;

public sealed class StripeCheckoutAdapter(
    IHttpClientFactory httpClientFactory,
    ISecretResolver secretResolver,
    IOptions<StripePaymentOptions> options) :
    IHostedCheckoutSessionCreator,
    IHostedCheckoutSessionRetriever,
    IPaymentIntentRetriever,
    IPaymentProviderDescriptor
{
    public const string HttpClientName = "Payments.StripeCheckout";
    private const int MaxNetworkRetries = 2;

    public PaymentProviderDescriptor Describe() => new(
        "stripe",
        "OrganizerDirect",
        global::Stripe.StripeConfiguration.ApiVersion);

    public async Task<HostedCheckoutCreateResult> CreateAsync(
        HostedCheckoutCreateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsStripe(request.ProviderCode))
        {
            return HostedCheckoutCreateResult.Failed(ConfigurationFailure(
                "checkout_provider_unsupported",
                HostedCheckoutPreHandoffDisposition.Permanent));
        }

        bool handedOff = false;
        try
        {
            global::Stripe.StripeClient client = await CreateClientAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            handedOff = true;
            global::Stripe.Checkout.Session session = await client.V1.Checkout.Sessions.CreateAsync(
                new global::Stripe.Checkout.SessionCreateOptions
                {
                    Mode = "payment",
                    SuccessUrl = request.SuccessUrl.ToString(),
                    CancelUrl = request.CancelUrl.ToString(),
                    ExpiresAt = request.ExpiresAt,
                    LineItems =
                    [
                        new global::Stripe.Checkout.SessionLineItemOptions
                        {
                            Quantity = 1,
                            PriceData = new global::Stripe.Checkout.SessionLineItemPriceDataOptions
                            {
                                Currency = request.CurrencyCode.ToLowerInvariant(),
                                UnitAmount = request.TotalMinor,
                                ProductData = new global::Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Event registration"
                                }
                            }
                        }
                    ],
                    PaymentIntentData = new global::Stripe.Checkout.SessionPaymentIntentDataOptions
                    {
                        ApplicationFeeAmount = request.ApplicationFeeMinor
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        ["islamu_payment_attempt_id"] = request.PaymentAttemptId.ToString("D"),
                        ["islamu_registration_order_id"] = request.RegistrationOrderId.ToString("D")
                    }
                },
                new global::Stripe.RequestOptions
                {
                    StripeAccount = request.ExternalAccountId,
                    IdempotencyKey = request.ProviderIdempotencyKey
                },
                cancellationToken);

            return MapCreateSession(session);
        }
        catch (OperationCanceledException) when (!handedOff && cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (MapFailure(exception) is { } failure)
        {
            HostedCheckoutFailure mapped = failure with
            {
                ProviderHandoffStarted = handedOff,
                PreHandoffDisposition = handedOff
                    ? HostedCheckoutPreHandoffDisposition.None
                    : failure.PreHandoffDisposition == HostedCheckoutPreHandoffDisposition.None
                        ? HostedCheckoutPreHandoffDisposition.Transient
                        : failure.PreHandoffDisposition
            };
            return IsAmbiguous(mapped)
                ? HostedCheckoutCreateResult.Unknown(mapped)
                : HostedCheckoutCreateResult.Failed(mapped);
        }
    }

    public async Task<HostedCheckoutRetrieveResult> RetrieveAsync(
        HostedCheckoutRetrieveRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsStripe(request.ProviderCode))
        {
            return HostedCheckoutRetrieveResult.Failed(ConfigurationFailure(
                "checkout_provider_unsupported",
                HostedCheckoutPreHandoffDisposition.Permanent));
        }

        try
        {
            global::Stripe.StripeClient client = await CreateClientAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            global::Stripe.Checkout.Session session = await client.V1.Checkout.Sessions.GetAsync(
                request.ProviderCheckoutSessionId,
                options: null,
                new global::Stripe.RequestOptions { StripeAccount = request.ExternalAccountId },
                cancellationToken);
            return MapRetrieveSession(session);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (MapFailure(exception) is { } failure)
        {
            return IsAmbiguous(failure)
                ? HostedCheckoutRetrieveResult.Unknown(failure)
                : HostedCheckoutRetrieveResult.Failed(failure);
        }
    }

    public async Task<PaymentIntentRetrieveResult> RetrievePaymentIntentAsync(
        PaymentIntentRetrieveRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsStripe(request.ProviderCode))
        {
            return PaymentIntentRetrieveResult.Failed(ConfigurationFailure(
                "checkout_provider_unsupported",
                HostedCheckoutPreHandoffDisposition.Permanent));
        }

        try
        {
            global::Stripe.StripeClient client = await CreateClientAsync(cancellationToken);
            global::Stripe.PaymentIntent paymentIntent = await client.V1.PaymentIntents.GetAsync(
                request.PaymentIntentId,
                options: null,
                new global::Stripe.RequestOptions { StripeAccount = request.ExternalAccountId },
                cancellationToken);
            string? requestId = RequestId(paymentIntent.StripeResponse);
            if (paymentIntent.Livemode != options.Value.ExpectsLiveMode ||
                BoundedText(paymentIntent.Id, 200) is not { } paymentIntentId ||
                NormalizeCurrency(paymentIntent.Currency) is not { } currency)
            {
                return PaymentIntentRetrieveResult.Unknown(new HostedCheckoutFailure(
                    "checkout_provider_response_incomplete",
                    HostedCheckoutFailureKind.ProviderDataIncomplete,
                    ProviderRequestId: requestId));
            }

            return PaymentIntentRetrieveResult.Succeeded(new(
                paymentIntentId,
                paymentIntent.Amount,
                currency,
                paymentIntent.ApplicationFeeAmount,
                MapPaymentIntentStatus(paymentIntent.Status)), requestId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (MapFailure(exception) is { } failure)
        {
            return IsAmbiguous(failure)
                ? PaymentIntentRetrieveResult.Unknown(failure)
                : PaymentIntentRetrieveResult.Failed(failure);
        }
    }

    private HostedCheckoutCreateResult MapCreateSession(global::Stripe.Checkout.Session session)
    {
        string? requestId = RequestId(session.StripeResponse);
        if (!MatchesMode(session) || MapSession(session) is not { HostedUrl: not null } mapped)
        {
            return HostedCheckoutCreateResult.Unknown(new HostedCheckoutFailure(
                "checkout_provider_response_incomplete",
                HostedCheckoutFailureKind.ProviderDataIncomplete,
                ProviderRequestId: requestId));
        }

        return HostedCheckoutCreateResult.Succeeded(mapped, requestId);
    }

    private HostedCheckoutRetrieveResult MapRetrieveSession(global::Stripe.Checkout.Session session)
    {
        string? requestId = RequestId(session.StripeResponse);
        if (!MatchesMode(session) ||
            MapSession(session) is not { AmountTotalMinor: not null, CurrencyCode: not null } mapped)
        {
            return HostedCheckoutRetrieveResult.Unknown(new HostedCheckoutFailure(
                "checkout_provider_response_incomplete",
                HostedCheckoutFailureKind.ProviderDataIncomplete,
                ProviderRequestId: requestId));
        }

        return HostedCheckoutRetrieveResult.Succeeded(mapped, requestId);
    }

    private HostedCheckoutSession? MapSession(global::Stripe.Checkout.Session session)
    {
        string? sessionId = BoundedText(session.Id, 200);
        if (sessionId is null)
        {
            return null;
        }

        Uri? hostedUrl = TryMapHostedUrl(session.Url);
        return new(
            sessionId,
            hostedUrl,
            MapStatus(session.Status),
            MapPaymentStatus(session.PaymentStatus),
            BoundedText(session.PaymentIntentId, 200),
            session.ExpiresAt,
            session.AmountTotal,
            NormalizeCurrency(session.Currency));
    }

    private Uri? TryMapHostedUrl(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !uri.IsDefaultPort ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            !string.Equals(uri.Host, uri.IdnHost, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        HashSet<string>? allowedHosts = StripePaymentOptionsValidator.NormalizeAllowedCheckoutHosts(options.Value.AllowedCheckoutHosts);
        return allowedHosts?.Contains(uri.IdnHost.ToLowerInvariant()) == true
            ? uri
            : null;
    }

    private async Task<global::Stripe.StripeClient> CreateClientAsync(CancellationToken cancellationToken)
    {
        ResolvedSecret? secret = await secretResolver.ResolveAsync(
            SecretDefinitionRegistry.Keys.Stripe.PlatformSecretKey,
            tenantId: null,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(secret?.Value))
        {
            throw new CheckoutAdapterFailure(ConfigurationFailure(
                "checkout_provider_secret_unavailable",
                HostedCheckoutPreHandoffDisposition.Transient));
        }

        if (!secret.Value.StartsWith(options.Value.ExpectedSecretKeyPrefix, StringComparison.Ordinal))
        {
            throw new CheckoutAdapterFailure(ConfigurationFailure(
                "checkout_provider_secret_mode_mismatch",
                HostedCheckoutPreHandoffDisposition.Permanent));
        }

        return new global::Stripe.StripeClient(new global::Stripe.StripeClientOptions
        {
            ApiKey = secret.Value,
            HttpClient = new global::Stripe.SystemNetHttpClient(
                httpClientFactory.CreateClient(HttpClientName),
                MaxNetworkRetries,
                appInfo: null,
                enableTelemetry: false)
        });
    }

    private bool MatchesMode(global::Stripe.Checkout.Session session) => session.Livemode == options.Value.ExpectsLiveMode;

    private static HostedCheckoutFailure? MapFailure(Exception exception) => exception switch
    {
        CheckoutAdapterFailure failure => failure.Failure,
        global::Stripe.StripeException stripeException => new HostedCheckoutFailure(
            "checkout_provider_rejected",
            MapFailureKind(stripeException.HttpStatusCode),
            BoundedText(stripeException.StripeError?.Type, 80),
            BoundedText(stripeException.StripeError?.Code, 80),
            (int)stripeException.HttpStatusCode,
            RequestId(stripeException.StripeResponse)),
        OperationCanceledException or HttpRequestException or TimeoutException => new HostedCheckoutFailure(
            "checkout_provider_network_ambiguous",
            HostedCheckoutFailureKind.Network),
        _ => null
    };

    internal static HostedCheckoutFailureKind MapFailureKind(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
            ? HostedCheckoutFailureKind.Configuration
            : (int)statusCode is >= 400 and <= 499 and not (408 or 409 or 429)
            ? HostedCheckoutFailureKind.ProviderRejected
            : HostedCheckoutFailureKind.ProviderUnknown;

    private static bool IsAmbiguous(HostedCheckoutFailure failure) =>
        failure.Kind is HostedCheckoutFailureKind.Network or HostedCheckoutFailureKind.ProviderUnknown or HostedCheckoutFailureKind.ProviderDataIncomplete;

    private static HostedCheckoutFailure ConfigurationFailure(
        string code,
        HostedCheckoutPreHandoffDisposition disposition) =>
        new(
            code,
            HostedCheckoutFailureKind.Configuration,
            ProviderHandoffStarted: false,
            PreHandoffDisposition: disposition);

    private static HostedCheckoutSessionStatus MapStatus(string? value) => value switch
    {
        "open" => HostedCheckoutSessionStatus.Open,
        "complete" => HostedCheckoutSessionStatus.Complete,
        "expired" => HostedCheckoutSessionStatus.Expired,
        _ => HostedCheckoutSessionStatus.Unknown
    };

    private static HostedCheckoutPaymentStatus MapPaymentStatus(string? value) => value switch
    {
        "unpaid" => HostedCheckoutPaymentStatus.Unpaid,
        "paid" => HostedCheckoutPaymentStatus.Paid,
        "no_payment_required" => HostedCheckoutPaymentStatus.NoPaymentRequired,
        _ => HostedCheckoutPaymentStatus.Unknown
    };

    private static PaymentIntentStatus MapPaymentIntentStatus(string? value) => value switch
    {
        "requires_payment_method" => PaymentIntentStatus.RequiresPaymentMethod,
        "requires_confirmation" => PaymentIntentStatus.RequiresConfirmation,
        "requires_action" => PaymentIntentStatus.RequiresAction,
        "processing" => PaymentIntentStatus.Processing,
        "requires_capture" => PaymentIntentStatus.RequiresCapture,
        "canceled" => PaymentIntentStatus.Canceled,
        "succeeded" => PaymentIntentStatus.Succeeded,
        _ => PaymentIntentStatus.Unknown
    };

    private static string? NormalizeCurrency(string? value)
    {
        string normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        return normalized.Length == 3 && normalized.All(character => character is >= 'A' and <= 'Z')
            ? normalized
            : null;
    }

    private static bool IsStripe(string providerCode) => string.Equals(providerCode, "stripe", StringComparison.Ordinal);

    private static string? RequestId(global::Stripe.StripeResponse? response) => BoundedText(response?.RequestId, 120);

    private static string? BoundedText(string? value, int maxLength)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized.Length is 0 || normalized.Length > maxLength || normalized.Any(char.IsControl)
            ? null
            : normalized;
    }

    private sealed class CheckoutAdapterFailure(HostedCheckoutFailure failure) : Exception(failure.Code)
    {
        public HostedCheckoutFailure Failure { get; } = failure;
    }
}
