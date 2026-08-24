// ABOUTME: Builds complete typed paid-acceptance evidence for API payment and scheduler fixtures.
// ABOUTME: Avoids fabricated prose milestones and opaque line JSON in integration setup.

using Explore.Domain;

namespace Event.Api.IntegrationTests.Features;

internal static class PaidAcceptanceTestFacts
{
    internal static PaidOrderAcceptanceSnapshot Create(
        Guid tenantId,
        Guid orderId,
        Guid eventId,
        string compositionRevision,
        Guid instancePolicyVersionId,
        Guid? tenantPolicyVersionId,
        long organizerAmountMinor,
        long platformFeeMinor,
        long platformContributionMinor,
        DateTime acceptedAt) => PaidOrderAcceptanceSnapshot.Create(
            Guid.CreateVersion7(), tenantId, tenantId, orderId, eventId, compositionRevision, "disclosure",
            "Example Organizer, legal merchant for this order",
            PaidCheckoutOperatorDisclosure.Create(
                Guid.CreateVersion7(), "Independent Operator", false, "https://events.example.test", "BE",
                "https://events.example.test", "https://events.example.test/legal", "https://events.example.test/terms",
                "https://events.example.test/privacy", "complaints@example.test", "Trust and Safety",
                "Payments Operations", "Dispute Operations", "Payment Reconciliation", "approved"),
            PaidOrderDeliverySnapshot.Create(
                new DateTimeOffset(acceptedAt.AddDays(10)), new DateTimeOffset(acceptedAt.AddDays(10).AddHours(3)),
                "Europe/Brussels"),
            "EUR", organizerAmountMinor, platformFeeMinor, platformContributionMinor,
            checked(organizerAmountMinor + platformContributionMinor), instancePolicyVersionId, 1,
            "Refund policy", "en-GB", "support@example.test",
            PaidCheckoutProviderDisclosure.Create(
                "stripe", "OrganizerDirect", "direct-charge", "EXAMPLE EVENT", "test", "instance-operator"),
            [PaidOrderAcceptanceLineFact.Create(Guid.CreateVersion7(), "Admission", 1, organizerAmountMinor, 0, organizerAmountMinor)],
            acceptedAt, tenantPolicyVersionId);
}
