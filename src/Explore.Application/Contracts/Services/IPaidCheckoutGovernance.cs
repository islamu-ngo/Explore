// ABOUTME: Server-owned instance operator identity and operational ownership required for paid Checkout activation.
// ABOUTME: Official and activation status remain startup-governance facts outside tenant and browser mutation surfaces.

namespace Explore.Application.Contracts.Services;

public interface IPaidCheckoutGovernance
{
    Guid OperatorId { get; }
    string OperatorDisplayName { get; }
    bool IsOfficialInstance { get; }
    string OfficialOrigin { get; }
    string OperatorRegionCode { get; }
    string OperatorWebsiteUrl { get; }
    string OperatorLegalNoticeUrl { get; }
    string OperatorTermsUrl { get; }
    string OperatorPrivacyUrl { get; }
    string ComplaintContact { get; }
    string ComplaintOwner { get; }
    string RefundOwner { get; }
    string DisputeOwner { get; }
    string ReconciliationOwner { get; }
    string ActivationStatus { get; }
    string RefundPolicyLanguageTag { get; }
    string StatementDescriptor { get; }
    string ChargeType { get; }
    bool IsConfigured { get; }
    bool IsActivated { get; }
}

public sealed class PaidCheckoutGovernanceOptions : IPaidCheckoutGovernance
{
    public const string SectionName = "Payments:CheckoutGovernance";

    public Guid OperatorId { get; set; }
    public string OperatorDisplayName { get; set; } = string.Empty;
    public bool IsOfficialInstance { get; set; }
    public string OfficialOrigin { get; set; } = string.Empty;
    public string OperatorRegionCode { get; set; } = string.Empty;
    public string OperatorWebsiteUrl { get; set; } = string.Empty;
    public string OperatorLegalNoticeUrl { get; set; } = string.Empty;
    public string OperatorTermsUrl { get; set; } = string.Empty;
    public string OperatorPrivacyUrl { get; set; } = string.Empty;
    public string ComplaintContact { get; set; } = string.Empty;
    public string ComplaintOwner { get; set; } = string.Empty;
    public string RefundOwner { get; set; } = string.Empty;
    public string DisputeOwner { get; set; } = string.Empty;
    public string ReconciliationOwner { get; set; } = string.Empty;
    public string ActivationStatus { get; set; } = "suspended";
    public string RefundPolicyLanguageTag { get; set; } = string.Empty;
    public string StatementDescriptor { get; set; } = string.Empty;
    public string ChargeType { get; set; } = "direct-charge";

    public bool IsConfigured => IsComplete();
    public bool IsActivated => IsComplete() && string.Equals(ActivationStatus, "approved", StringComparison.OrdinalIgnoreCase);

    public bool IsComplete()
    {
        try
        {
            _ = Explore.Domain.PaidCheckoutOperatorDisclosure.Create(
                OperatorId, OperatorDisplayName, IsOfficialInstance, OfficialOrigin, OperatorRegionCode,
                OperatorWebsiteUrl, OperatorLegalNoticeUrl, OperatorTermsUrl, OperatorPrivacyUrl,
                ComplaintContact, ComplaintOwner, RefundOwner, DisputeOwner, ReconciliationOwner, ActivationStatus);
            return !string.IsNullOrWhiteSpace(RefundPolicyLanguageTag) &&
                !string.IsNullOrWhiteSpace(StatementDescriptor) && !string.IsNullOrWhiteSpace(ChargeType);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
