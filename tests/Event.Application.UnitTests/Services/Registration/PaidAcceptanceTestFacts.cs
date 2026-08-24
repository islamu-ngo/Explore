// ABOUTME: Builds complete typed paid-acceptance evidence for payment application tests.
// ABOUTME: Keeps fixtures explicit about schedule, operator ownership, provider environment, and line money.

using Explore.Domain;
using Explore.Application.DTOs.RegistrationOrders;

namespace Event.Application.UnitTests.Services.Registration;

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

    internal static PaidOrderAcceptanceDisclosureDto ToDisclosure(PaidOrderAcceptanceSnapshot snapshot) => new()
    {
        DisclosureRevision = snapshot.DisclosureRevision,
        MerchantDisclosureText = snapshot.MerchantDisclosureText,
        OperatorDisplayName = snapshot.OperatorDisplayName,
        IsOfficialInstance = snapshot.IsOfficialInstance,
        OfficialOrigin = snapshot.OfficialOrigin,
        OperatorRegionCode = snapshot.OperatorRegionCode,
        OperatorWebsiteUrl = snapshot.OperatorWebsiteUrl,
        OperatorLegalNoticeUrl = snapshot.OperatorLegalNoticeUrl,
        OperatorTermsUrl = snapshot.OperatorTermsUrl,
        OperatorPrivacyUrl = snapshot.OperatorPrivacyUrl,
        OperatorActivationStatus = snapshot.ActivationStatus,
        DeliveryStartsAtUtc = snapshot.DeliveryStartsAtUtc,
        DeliveryEndsAtUtc = snapshot.DeliveryEndsAtUtc,
        EventTimeZoneId = snapshot.EventTimeZoneId,
        CurrencyCode = snapshot.CurrencyCode,
        CurrencyMinorUnitDigits = 2,
        OrganizerAmountMinor = snapshot.OrganizerAmountMinor,
        PlatformFeeMinor = snapshot.PlatformFeeMinor,
        PlatformContributionMinor = snapshot.PlatformContributionMinor,
        TotalMinor = snapshot.TotalMinor,
        RefundPolicyVersion = snapshot.RefundPolicyVersion,
        RefundPolicyText = snapshot.RefundPolicyText,
        RefundPolicyLanguageTag = snapshot.RefundPolicyLanguageTag,
        SupportContact = snapshot.SupportContact,
        ComplaintContact = snapshot.ComplaintContact,
        ComplaintOwner = snapshot.ComplaintOwner,
        RefundOwner = snapshot.RefundOwner,
        DisputeOwner = snapshot.DisputeOwner,
        ReconciliationOwner = snapshot.ReconciliationOwner,
        ProviderCode = snapshot.ProviderCode,
        ProviderProfileCode = snapshot.ProviderProfileCode,
        ProviderEnvironment = snapshot.ProviderEnvironment,
        ProviderCredentialOwner = snapshot.ProviderCredentialOwner,
        ChargeType = snapshot.ChargeType,
        StatementDescriptor = snapshot.StatementDescriptor,
        Lines = snapshot.Lines.Select(line => new PaidOrderAcceptanceLineDto
        {
            OrderLineId = line.OrderLineId,
            Name = line.Name,
            Quantity = line.Quantity,
            UnitAmountMinor = line.UnitAmountMinor,
            DiscountAmountMinor = line.DiscountAmountMinor,
            LineTotalMinor = line.LineTotalMinor
        }).ToArray()
    };
}
