// ABOUTME: Provider-neutral payment-dispute lifecycle used by refund reservation rules.
// ABOUTME: Treats any unresolved inquiry or formal dispute as open financial exposure.

namespace Explore.Domain.Enums;

public enum PaymentDisputeStatus
{
    Open = 1,
    Won = 2,
    Lost = 3,
    Withdrawn = 4,
    Prevented = 5
}
