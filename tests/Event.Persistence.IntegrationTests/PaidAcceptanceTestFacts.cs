// ABOUTME: Builds complete typed buyer-acceptance evidence for payment persistence scenarios.
// ABOUTME: Keeps provider, policy, delivery, operator, and money fixtures explicit before dispatch.

using Explore.Domain;

namespace Event.Persistence.IntegrationTests;

internal static class PaidAcceptanceTestFacts
{
    internal static PaidOrderAcceptanceSnapshot Create(
        Guid tenantId,
        Guid orderId,
        Guid eventId,
        string compositionRevision,
        Guid instancePolicyVersionId,
        long organizerAmountMinor,
        long platformFeeMinor,
        long platformContributionMinor,
        DateTime acceptedAt) => PaidOrderAcceptanceSnapshot.Create(
            Guid.CreateVersion7(),
            tenantId,
            tenantId,
            orderId,
            eventId,
            compositionRevision,
            $"disclosure:{compositionRevision}",
            "Example Organizer is the legal merchant for this order.",
            PaidCheckoutOperatorDisclosure.Create(
                Guid.CreateVersion7(),
                "Independent Example Operator",
                false,
                "https://events.example.test",
                "BE",
                "https://events.example.test",
                "https://events.example.test/legal",
                "https://events.example.test/terms",
                "https://events.example.test/privacy",
                "complaints@example.test",
                "Trust and Safety",
                "Refund Operations",
                "Dispute Operations",
                "Payment Reconciliation",
                "approved"),
            PaidOrderDeliverySnapshot.Create(
                new DateTimeOffset(acceptedAt.AddDays(10)),
                new DateTimeOffset(acceptedAt.AddDays(10).AddHours(3)),
                "Europe/Brussels"),
            "EUR",
            organizerAmountMinor,
            platformFeeMinor,
            platformContributionMinor,
            checked(organizerAmountMinor + platformContributionMinor),
            instancePolicyVersionId,
            1,
            "Refund policy",
            "en-GB",
            "support@example.test",
            PaidCheckoutProviderDisclosure.Create(
                "stripe",
                "OrganizerDirect",
                "direct-charge",
                "EXAMPLE EVENT",
                "test",
                "instance-operator"),
            [
                PaidOrderAcceptanceLineFact.Create(
                    Guid.CreateVersion7(),
                    "Admission",
                    1,
                    organizerAmountMinor,
                    0,
                    organizerAmountMinor)
            ],
            acceptedAt);
}
