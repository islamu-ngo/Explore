// ABOUTME: Rebuilds authoritative payment disclosures before provider handoff and compares immutable acceptance evidence.
// ABOUTME: Detects schedule, operator, policy, provider, order, and typed-line changes without rewriting accepted history.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Application.Services.Registration;

public interface IPaidOrderAcceptanceFreshnessService
{
    Task<bool> IsCurrentAsync(PaymentAttempt attempt, CancellationToken cancellationToken);
}

public sealed class PaidOrderAcceptanceFreshnessService(
    IRegistrationInventoryRepository orders,
    IPaidOrderAcceptanceService acceptances) : IPaidOrderAcceptanceFreshnessService
{
    public async Task<bool> IsCurrentAsync(PaymentAttempt attempt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        if (attempt.AcceptanceSnapshot is not { } snapshot)
        {
            return false;
        }
        RegistrationOrder? order = await orders.GetOrderWithLinesAsync(
            attempt.RegistrationOrderId, attempt.TenantId, cancellationToken);
        if (order is null)
        {
            return false;
        }
        PaidOrderAcceptanceResult current = await acceptances.DescribeAsync(order, attempt.Id, cancellationToken);
        if (current.Disclosure is not { } disclosure ||
            current.Authority is not { } authority ||
            !Matches(snapshot, disclosure, authority, order, attempt))
        {
            return false;
        }
        try
        {
            return snapshot.MatchesLineFacts(disclosure.Lines.Select(line => PaidOrderAcceptanceLineFact.Create(
                line.OrderLineId, line.Name, line.Quantity, line.UnitAmountMinor,
                line.DiscountAmountMinor, line.LineTotalMinor)));
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool Matches(
        PaidOrderAcceptanceSnapshot snapshot,
        DTOs.RegistrationOrders.PaidOrderAcceptanceDisclosureDto disclosure,
        PaidOrderAcceptanceAuthorityFacts authority,
        RegistrationOrder order,
        PaymentAttempt attempt) =>
        snapshot.InstancePolicyVersionId == authority.InstancePolicyVersionId &&
        snapshot.TenantPolicyVersionId == authority.TenantPolicyVersionId &&
        snapshot.OperatorId == authority.OperatorId &&
        snapshot.CurrencyCode == attempt.CurrencyCode &&
        snapshot.OrganizerAmountMinor == attempt.OrganizerAmountMinor &&
        snapshot.PlatformFeeMinor == attempt.PlatformFeeMinor &&
        snapshot.PlatformContributionMinor == attempt.PlatformContributionMinor &&
        snapshot.TotalMinor == attempt.TotalMinor &&
        snapshot.DisclosureRevision == disclosure.DisclosureRevision &&
        snapshot.CompositionRevision == order.ConcurrencyStamp.ToString("N") &&
        snapshot.MerchantDisclosureText == disclosure.MerchantDisclosureText &&
        snapshot.OperatorDisplayName == disclosure.OperatorDisplayName &&
        snapshot.IsOfficialInstance == disclosure.IsOfficialInstance &&
        snapshot.OfficialOrigin == disclosure.OfficialOrigin &&
        snapshot.OperatorRegionCode == disclosure.OperatorRegionCode &&
        snapshot.OperatorWebsiteUrl == disclosure.OperatorWebsiteUrl &&
        snapshot.OperatorLegalNoticeUrl == disclosure.OperatorLegalNoticeUrl &&
        snapshot.OperatorTermsUrl == disclosure.OperatorTermsUrl &&
        snapshot.OperatorPrivacyUrl == disclosure.OperatorPrivacyUrl &&
        snapshot.ActivationStatus == disclosure.OperatorActivationStatus &&
        snapshot.DeliveryStartsAtUtc == disclosure.DeliveryStartsAtUtc &&
        snapshot.DeliveryEndsAtUtc == disclosure.DeliveryEndsAtUtc &&
        snapshot.EventTimeZoneId == disclosure.EventTimeZoneId &&
        snapshot.CurrencyCode == disclosure.CurrencyCode &&
        snapshot.OrganizerAmountMinor == disclosure.OrganizerAmountMinor &&
        snapshot.PlatformFeeMinor == disclosure.PlatformFeeMinor &&
        snapshot.PlatformContributionMinor == disclosure.PlatformContributionMinor &&
        snapshot.TotalMinor == disclosure.TotalMinor &&
        snapshot.RefundPolicyVersion == disclosure.RefundPolicyVersion &&
        snapshot.RefundPolicyText == disclosure.RefundPolicyText &&
        snapshot.RefundPolicyLanguageTag == disclosure.RefundPolicyLanguageTag &&
        snapshot.SupportContact == disclosure.SupportContact &&
        snapshot.ComplaintContact == disclosure.ComplaintContact &&
        snapshot.ComplaintOwner == disclosure.ComplaintOwner &&
        snapshot.RefundOwner == disclosure.RefundOwner &&
        snapshot.DisputeOwner == disclosure.DisputeOwner &&
        snapshot.ReconciliationOwner == disclosure.ReconciliationOwner &&
        snapshot.ProviderCode == disclosure.ProviderCode &&
        snapshot.ProviderProfileCode == disclosure.ProviderProfileCode &&
        snapshot.ProviderEnvironment == disclosure.ProviderEnvironment &&
        snapshot.ProviderCredentialOwner == disclosure.ProviderCredentialOwner &&
        snapshot.ChargeType == disclosure.ChargeType &&
        snapshot.StatementDescriptor == disclosure.StatementDescriptor;
}
