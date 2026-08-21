// ABOUTME: Browser-local response shape for the opaque registration-payment checkout ticket issue call.
// ABOUTME: Contains only the constant same-origin BFF navigation path and no backend or provider contract data.

namespace Explore.Blazor.Client.Services.Http;

public sealed record BffRegistrationPaymentCheckoutTicketResponseDto(string CheckoutPath);
