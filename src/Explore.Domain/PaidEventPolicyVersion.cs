// ABOUTME: Defines one version of instance or tenant paid-event eligibility and currency policy.
// ABOUTME: Keeps paid-event ceilings provider-neutral and immutable across active revisions.

using Explore.Domain.Enums;
using Explore.Domain.Interfaces;
using Explore.Domain.ValueObjects;

namespace Explore.Domain;

public sealed class PaidEventPolicyVersion : IAuditableEntity
{
    private readonly List<ActorTypeEnum> _allowedOrganizerKinds = [];
    private readonly List<string> _allowedCurrencyCodes = [];
    private readonly List<PaidEventRefundProtection> _refundProtections = [];
    private readonly List<PaidEventPolicyCurrencyRiskLimit> _currencyRiskLimits = [];

    private PaidEventPolicyVersion()
    {
    }

    private PaidEventPolicyVersion(
        Guid? tenantId,
        int versionNumber,
        bool isPaymentsEnabled,
        IEnumerable<ActorTypeEnum> allowedOrganizerKinds,
        bool requiresLocalVerification,
        IEnumerable<string> allowedCurrencyCodes,
        string? defaultCurrencyCode,
        IEnumerable<PaidEventRefundProtection> refundProtections,
        IEnumerable<PaidEventPolicyCurrencyRiskLimit> currencyRiskLimits,
        bool requiresFirstPaidEventReview,
        int? farFutureReviewThresholdDays)
    {
        ActorTypeEnum[] normalizedOrganizerKinds = NormalizeOrganizerKinds(allowedOrganizerKinds);
        string[] normalizedCurrencyCodes = NormalizeCurrencyCodes(allowedCurrencyCodes);
        PaidEventRefundProtection[] normalizedRefundProtections = NormalizeRefundProtections(refundProtections);
        PaidEventPolicyCurrencyRiskLimit[] normalizedRiskLimits = NormalizeRiskLimits(currencyRiskLimits, normalizedCurrencyCodes);

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        VersionNumber = versionNumber;
        IsActive = true;
        IsPaymentsEnabled = isPaymentsEnabled;
        RequiresLocalVerification = requiresLocalVerification;
        DefaultCurrencyCode = NormalizeDefaultCurrency(defaultCurrencyCode, normalizedCurrencyCodes);
        RequiresFirstPaidEventReview = requiresFirstPaidEventReview;
        FarFutureReviewThresholdDays = farFutureReviewThresholdDays;
        _allowedOrganizerKinds.AddRange(normalizedOrganizerKinds);
        _allowedCurrencyCodes.AddRange(normalizedCurrencyCodes);
        _refundProtections.AddRange(normalizedRefundProtections);
        _currencyRiskLimits.AddRange(normalizedRiskLimits);
        ValidateFarFutureThreshold(farFutureReviewThresholdDays);
    }

    public Guid Id { get; private set; }

    public Guid? TenantId { get; private set; }

    public int VersionNumber { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsPaymentsEnabled { get; private set; }

    public bool RequiresLocalVerification { get; private set; }

    public IReadOnlyCollection<ActorTypeEnum> AllowedOrganizerKinds => _allowedOrganizerKinds.AsReadOnly();

    public IReadOnlyCollection<string> AllowedCurrencyCodes => _allowedCurrencyCodes.AsReadOnly();

    public IReadOnlyCollection<PaidEventRefundProtection> RefundProtections => _refundProtections.AsReadOnly();

    public IReadOnlyCollection<PaidEventPolicyCurrencyRiskLimit> CurrencyRiskLimits => _currencyRiskLimits.AsReadOnly();

    public string? DefaultCurrencyCode { get; private set; }

    public bool RequiresFirstPaidEventReview { get; private set; }

    public int? FarFutureReviewThresholdDays { get; private set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static PaidEventPolicyVersion CreateDefaultInstance() => new(
        tenantId: null,
        versionNumber: 1,
        isPaymentsEnabled: false,
        allowedOrganizerKinds: [ActorTypeEnum.Organization],
        requiresLocalVerification: false,
        allowedCurrencyCodes: ["EUR", "USD", "MAD", "SAR", "AED"],
        defaultCurrencyCode: null,
        refundProtections: RequiredRefundFloor(),
        currencyRiskLimits: [],
        requiresFirstPaidEventReview: false,
        farFutureReviewThresholdDays: null);

    public static PaidEventPolicyVersion CreateTenant(
        Guid tenantId,
        bool isPaymentsEnabled,
        IEnumerable<ActorTypeEnum> allowedOrganizerKinds,
        bool requiresLocalVerification,
        IEnumerable<string> allowedCurrencyCodes,
        string? defaultCurrencyCode,
        IEnumerable<PaidEventRefundProtection> refundProtections,
        IEnumerable<PaidEventPolicyCurrencyRiskLimit> currencyRiskLimits,
        bool requiresFirstPaidEventReview,
        int? farFutureReviewThresholdDays)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant is required.", nameof(tenantId));
        }

        return new PaidEventPolicyVersion(
            tenantId,
            1,
            isPaymentsEnabled,
            allowedOrganizerKinds,
            requiresLocalVerification,
            allowedCurrencyCodes,
            defaultCurrencyCode,
            refundProtections,
            currencyRiskLimits,
            requiresFirstPaidEventReview,
            farFutureReviewThresholdDays);
    }

    public PaidEventPolicyVersion CreateRevision(
        bool isPaymentsEnabled,
        IEnumerable<ActorTypeEnum> allowedOrganizerKinds,
        bool requiresLocalVerification,
        IEnumerable<string> allowedCurrencyCodes,
        string? defaultCurrencyCode,
        IEnumerable<PaidEventRefundProtection> refundProtections,
        IEnumerable<PaidEventPolicyCurrencyRiskLimit> currencyRiskLimits,
        bool requiresFirstPaidEventReview,
        int? farFutureReviewThresholdDays)
    {
        EnsureActive();
        PaidEventPolicyVersion revision = new(
            TenantId,
            checked(VersionNumber + 1),
            isPaymentsEnabled,
            allowedOrganizerKinds,
            requiresLocalVerification,
            allowedCurrencyCodes,
            defaultCurrencyCode,
            refundProtections,
            currencyRiskLimits,
            requiresFirstPaidEventReview,
            farFutureReviewThresholdDays);
        IsActive = false;
        return revision;
    }

    public void Retire()
    {
        EnsureActive();
        IsActive = false;
    }

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Only the active paid-event policy version can be revised or retired.");
        }
    }

    private static ActorTypeEnum[] NormalizeOrganizerKinds(IEnumerable<ActorTypeEnum> allowedOrganizerKinds)
    {
        ArgumentNullException.ThrowIfNull(allowedOrganizerKinds);
        ActorTypeEnum[] kinds = allowedOrganizerKinds.Distinct().ToArray();
        if (kinds.Length == 0)
        {
            throw new ArgumentException("At least one organizer kind is required.", nameof(allowedOrganizerKinds));
        }

        if (kinds.Any(static kind => kind is not ActorTypeEnum.Organization and not ActorTypeEnum.Group and not ActorTypeEnum.User))
        {
            throw new ArgumentException("Paid-event organizer kinds are limited to organization, group, or user actors.", nameof(allowedOrganizerKinds));
        }

        return kinds;
    }

    private static string[] NormalizeCurrencyCodes(IEnumerable<string> allowedCurrencyCodes)
    {
        ArgumentNullException.ThrowIfNull(allowedCurrencyCodes);
        string[] currencyCodes = allowedCurrencyCodes.Select(NormalizeMoneyCurrencyCode).Distinct(StringComparer.Ordinal).ToArray();
        if (currencyCodes.Length == 0)
        {
            throw new ArgumentException("At least one currency is required.", nameof(allowedCurrencyCodes));
        }

        return currencyCodes;
    }

    private static PaidEventRefundProtection[] RequiredRefundFloor() =>
    [
        PaidEventRefundProtection.OrganizerCancellationFullRefund,
        PaidEventRefundProtection.MaterialChangeBuyerChoiceOrFullRefund,
        PaidEventRefundProtection.DuplicateOrIncorrectChargeFullRefund,
        PaidEventRefundProtection.SubstantialNonDeliveryRemedy,
        PaidEventRefundProtection.AttendeeBuyerChangeTermsDisclosedSubjectToLaw,
        PaidEventRefundProtection.CardDisputeRightsNotWaived,
        PaidEventRefundProtection.CancelledEventPlatformAmountsRefundedByDefault
    ];

    private static PaidEventRefundProtection[] NormalizeRefundProtections(IEnumerable<PaidEventRefundProtection> refundProtections)
    {
        ArgumentNullException.ThrowIfNull(refundProtections);
        PaidEventRefundProtection[] protections = refundProtections.Distinct().ToArray();
        if (protections.Length == 0 || protections.Any(static protection => !Enum.IsDefined(protection)))
        {
            throw new ArgumentException("Paid-event refund protections must be known and non-empty.", nameof(refundProtections));
        }

        if (RequiredRefundFloor().Any(protection => !protections.Contains(protection)))
        {
            throw new ArgumentException("Paid-event refund protections must include every required refund floor protection.", nameof(refundProtections));
        }

        return protections;
    }

    private static PaidEventPolicyCurrencyRiskLimit[] NormalizeRiskLimits(IEnumerable<PaidEventPolicyCurrencyRiskLimit> currencyRiskLimits, IReadOnlyCollection<string> allowedCurrencyCodes)
    {
        ArgumentNullException.ThrowIfNull(currencyRiskLimits);
        PaidEventPolicyCurrencyRiskLimit[] limits = currencyRiskLimits.ToArray();
        if (limits.Select(static limit => limit.CurrencyCode).Distinct(StringComparer.Ordinal).Count() != limits.Length)
        {
            throw new ArgumentException("Currency risk limit currencies must be unique.", nameof(currencyRiskLimits));
        }

        if (limits.Any(limit => !allowedCurrencyCodes.Contains(limit.CurrencyCode, StringComparer.Ordinal)))
        {
            throw new ArgumentException("Currency risk limits must belong to the allowed currency set.", nameof(currencyRiskLimits));
        }

        return limits;
    }

    private static string? NormalizeDefaultCurrency(string? defaultCurrencyCode, IReadOnlyCollection<string> allowedCurrencyCodes)
    {
        if (string.IsNullOrWhiteSpace(defaultCurrencyCode))
        {
            return null;
        }

        string normalizedDefaultCurrency = NormalizeMoneyCurrencyCode(defaultCurrencyCode);
        if (!allowedCurrencyCodes.Contains(normalizedDefaultCurrency, StringComparer.Ordinal))
        {
            throw new ArgumentException("Default currency must be in the allowed currency set.", nameof(defaultCurrencyCode));
        }

        return normalizedDefaultCurrency;
    }

    private static string NormalizeMoneyCurrencyCode(string currencyCode)
    {
        CurrencyMetadata currency = CurrencyMetadata.Get(currencyCode);
        if (currency.IsNoCurrency)
        {
            throw new ArgumentException("Paid-event policies require a monetary currency.", nameof(currencyCode));
        }

        return currency.Code;
    }

    private static void ValidateFarFutureThreshold(int? farFutureReviewThresholdDays)
    {
        if (farFutureReviewThresholdDays is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(farFutureReviewThresholdDays));
        }
    }
}
