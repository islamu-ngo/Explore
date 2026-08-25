// ABOUTME: Provider-neutral stages for payment disputes and pre-dispute inquiries.
// ABOUTME: Keeps inquiry and formal dispute authority explicit without provider SDK types.

namespace Explore.Domain.Enums;

public enum PaymentDisputeStage
{
    Inquiry = 1,
    Formal = 2
}
