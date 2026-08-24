// ABOUTME: Immutable tenant-bound evidence of the exact commercial and operator facts a buyer accepted before payment.
// ABOUTME: Owns normalized acceptance-line rows and never fabricates evidence for historical payment attempts.

using Explore.Domain.Interfaces;
using Explore.Domain.ValueObjects;

namespace Explore.Domain;

public sealed class PaidOrderAcceptanceSnapshot : ITenantEntity, IAuditableEntity
{
    public const int MaxDisplayNameLength = 200;
    public const int MaxDisclosureLength = 2000;
    public const int MaxContactLength = 320;
    private readonly List<PaidOrderAcceptanceLine> _lines = [];

    private PaidOrderAcceptanceSnapshot()
    {
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; set; }
    public Guid RegistrationOrderId { get; private set; }
    public Guid EventId { get; private set; }
    public string CompositionRevision { get; private set; } = string.Empty;
    public string DisclosureRevision { get; private set; } = string.Empty;
    public string MerchantDisclosureText { get; private set; } = string.Empty;

    public Guid OperatorId { get; private set; }
    public string OperatorDisplayName { get; private set; } = string.Empty;
    public bool IsOfficialInstance { get; private set; }
    public string OfficialOrigin { get; private set; } = string.Empty;
    public string OperatorRegionCode { get; private set; } = string.Empty;
    public string OperatorWebsiteUrl { get; private set; } = string.Empty;
    public string OperatorLegalNoticeUrl { get; private set; } = string.Empty;
    public string OperatorTermsUrl { get; private set; } = string.Empty;
    public string OperatorPrivacyUrl { get; private set; } = string.Empty;
    public string ComplaintContact { get; private set; } = string.Empty;
    public string ComplaintOwner { get; private set; } = string.Empty;
    public string RefundOwner { get; private set; } = string.Empty;
    public string DisputeOwner { get; private set; } = string.Empty;
    public string ReconciliationOwner { get; private set; } = string.Empty;
    public string ActivationStatus { get; private set; } = string.Empty;

    public DateTimeOffset DeliveryStartsAtUtc { get; private set; }
    public DateTimeOffset DeliveryEndsAtUtc { get; private set; }
    public string EventTimeZoneId { get; private set; } = string.Empty;

    public string CurrencyCode { get; private set; } = string.Empty;
    public long OrganizerAmountMinor { get; private set; }
    public long PlatformFeeMinor { get; private set; }
    public long PlatformContributionMinor { get; private set; }
    public long TotalMinor { get; private set; }
    public Guid InstancePolicyVersionId { get; private set; }
    public Guid? TenantPolicyVersionId { get; private set; }
    public int RefundPolicyVersion { get; private set; }
    public string RefundPolicyText { get; private set; } = string.Empty;
    public string RefundPolicyLanguageTag { get; private set; } = string.Empty;
    public string SupportContact { get; private set; } = string.Empty;

    public string ProviderCode { get; private set; } = string.Empty;
    public string ProviderProfileCode { get; private set; } = string.Empty;
    public string ChargeType { get; private set; } = string.Empty;
    public string StatementDescriptor { get; private set; } = string.Empty;
    public string ProviderEnvironment { get; private set; } = string.Empty;
    public string ProviderCredentialOwner { get; private set; } = string.Empty;

    public DateTime AcceptedAt { get; private set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public IReadOnlyCollection<PaidOrderAcceptanceLine> Lines => _lines.OrderBy(line => line.Ordinal).ToArray();

    public PaidCheckoutOperatorDisclosure Operator => PaidCheckoutOperatorDisclosure.Create(
        OperatorId, OperatorDisplayName, IsOfficialInstance, OfficialOrigin, OperatorRegionCode, OperatorWebsiteUrl,
        OperatorLegalNoticeUrl, OperatorTermsUrl, OperatorPrivacyUrl, ComplaintContact, ComplaintOwner, RefundOwner,
        DisputeOwner, ReconciliationOwner, ActivationStatus);

    public PaidOrderDeliverySnapshot Delivery => PaidOrderDeliverySnapshot.Create(
        DeliveryStartsAtUtc, DeliveryEndsAtUtc, EventTimeZoneId);

    public PaidCheckoutProviderDisclosure Provider => PaidCheckoutProviderDisclosure.Create(
        ProviderCode, ProviderProfileCode, ChargeType, StatementDescriptor, ProviderEnvironment, ProviderCredentialOwner);

    public static PaidOrderAcceptanceSnapshot Create(
        Guid id,
        Guid tenantId,
        Guid orderTenantId,
        Guid registrationOrderId,
        Guid eventId,
        string compositionRevision,
        string disclosureRevision,
        string merchantDisclosureText,
        PaidCheckoutOperatorDisclosure operatorDisclosure,
        PaidOrderDeliverySnapshot delivery,
        string currencyCode,
        long organizerAmountMinor,
        long platformFeeMinor,
        long platformContributionMinor,
        long totalMinor,
        Guid instancePolicyVersionId,
        int refundPolicyVersion,
        string refundPolicyText,
        string refundPolicyLanguageTag,
        string supportContact,
        PaidCheckoutProviderDisclosure provider,
        IReadOnlyCollection<PaidOrderAcceptanceLineFact> lines,
        DateTime acceptedAt,
        Guid? tenantPolicyVersionId = null)
    {
        ArgumentNullException.ThrowIfNull(operatorDisclosure);
        ArgumentNullException.ThrowIfNull(delivery);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(lines);
        if (id == Guid.Empty || tenantId == Guid.Empty || orderTenantId != tenantId || registrationOrderId == Guid.Empty ||
            eventId == Guid.Empty || instancePolicyVersionId == Guid.Empty || refundPolicyVersion <= 0)
        {
            throw new ArgumentException("Acceptance identity, policy, and tenant lineage are required.");
        }

        string currency = CurrencyMetadata.Get(currencyCode).Code;
        if (CurrencyMetadata.Get(currency).IsNoCurrency || organizerAmountMinor <= 0 || platformFeeMinor < 0 ||
            platformFeeMinor > organizerAmountMinor || platformContributionMinor < 0 ||
            totalMinor != MinorUnitMath.Add(organizerAmountMinor, platformContributionMinor))
        {
            throw new ArgumentException("Acceptance money facts are invalid.");
        }

        PaidOrderAcceptanceLineFact[] normalizedLines = lines.ToArray();
        if (normalizedLines.Length == 0 || normalizedLines.Select(line => line.OrderLineId).Distinct().Count() != normalizedLines.Length ||
            checked(normalizedLines.Sum(line => line.LineTotalMinor)) != organizerAmountMinor)
        {
            throw new ArgumentException("Acceptance lines must be unique, non-empty, and total to the organizer amount.", nameof(lines));
        }

        DateTime timestamp = OrganizerPaymentProviderConnection.EnsureUtc(acceptedAt, nameof(acceptedAt));
        var snapshot = new PaidOrderAcceptanceSnapshot
        {
            Id = id,
            TenantId = tenantId,
            RegistrationOrderId = registrationOrderId,
            EventId = eventId,
            CompositionRevision = PaidCheckoutDisclosureValidation.Required(compositionRevision, nameof(compositionRevision), 80),
            DisclosureRevision = PaidCheckoutDisclosureValidation.Required(disclosureRevision, nameof(disclosureRevision), 80),
            MerchantDisclosureText = PaidCheckoutDisclosureValidation.Required(merchantDisclosureText, nameof(merchantDisclosureText), MaxDisclosureLength),
            OperatorId = operatorDisclosure.OperatorId,
            OperatorDisplayName = operatorDisclosure.OperatorDisplayName,
            IsOfficialInstance = operatorDisclosure.IsOfficialInstance,
            OfficialOrigin = operatorDisclosure.OfficialOrigin,
            OperatorRegionCode = operatorDisclosure.RegionCode,
            OperatorWebsiteUrl = operatorDisclosure.WebsiteUrl,
            OperatorLegalNoticeUrl = operatorDisclosure.LegalNoticeUrl,
            OperatorTermsUrl = operatorDisclosure.TermsUrl,
            OperatorPrivacyUrl = operatorDisclosure.PrivacyUrl,
            ComplaintContact = operatorDisclosure.ComplaintContact,
            ComplaintOwner = operatorDisclosure.ComplaintOwner,
            RefundOwner = operatorDisclosure.RefundOwner,
            DisputeOwner = operatorDisclosure.DisputeOwner,
            ReconciliationOwner = operatorDisclosure.ReconciliationOwner,
            ActivationStatus = operatorDisclosure.ActivationStatus,
            DeliveryStartsAtUtc = delivery.StartsAtUtc,
            DeliveryEndsAtUtc = delivery.EndsAtUtc,
            EventTimeZoneId = delivery.TimeZoneId,
            CurrencyCode = currency,
            OrganizerAmountMinor = organizerAmountMinor,
            PlatformFeeMinor = platformFeeMinor,
            PlatformContributionMinor = platformContributionMinor,
            TotalMinor = totalMinor,
            InstancePolicyVersionId = instancePolicyVersionId,
            TenantPolicyVersionId = tenantPolicyVersionId,
            RefundPolicyVersion = refundPolicyVersion,
            RefundPolicyText = PaidCheckoutDisclosureValidation.Required(refundPolicyText, nameof(refundPolicyText), MaxDisclosureLength),
            RefundPolicyLanguageTag = PaidCheckoutDisclosureValidation.Required(refundPolicyLanguageTag, nameof(refundPolicyLanguageTag), 35),
            SupportContact = PaidCheckoutDisclosureValidation.Required(supportContact, nameof(supportContact), MaxContactLength),
            ProviderCode = provider.ProviderCode,
            ProviderProfileCode = provider.ProfileCode,
            ChargeType = provider.ChargeType,
            StatementDescriptor = provider.StatementDescriptor,
            ProviderEnvironment = provider.Environment,
            ProviderCredentialOwner = provider.CredentialOwner,
            AcceptedAt = timestamp,
            CreatedAt = timestamp
        };
        snapshot._lines.AddRange(normalizedLines.Select((line, ordinal) => PaidOrderAcceptanceLine.Create(
            tenantId, id, line, ordinal)));
        return snapshot;
    }

    public bool IsCurrent(
        string compositionRevision,
        string disclosureRevision,
        Guid instancePolicyVersionId,
        Guid? tenantPolicyVersionId,
        string providerCode,
        string providerProfileCode,
        string? providerEnvironment = null,
        string? providerCredentialOwner = null) =>
        string.Equals(CompositionRevision, compositionRevision, StringComparison.Ordinal) &&
        string.Equals(DisclosureRevision, disclosureRevision, StringComparison.Ordinal) &&
        InstancePolicyVersionId == instancePolicyVersionId &&
        TenantPolicyVersionId == tenantPolicyVersionId &&
        string.Equals(ProviderCode, providerCode, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(ProviderProfileCode, providerProfileCode, StringComparison.Ordinal) &&
        (providerEnvironment is null || string.Equals(ProviderEnvironment, providerEnvironment, StringComparison.Ordinal)) &&
        (providerCredentialOwner is null || string.Equals(ProviderCredentialOwner, providerCredentialOwner, StringComparison.Ordinal));

    public bool MatchesLineFacts(IEnumerable<PaidOrderAcceptanceLineFact> facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        PaidOrderAcceptanceLineFact[] expected = facts.OrderBy(line => line.OrderLineId).ToArray();
        PaidOrderAcceptanceLine[] actual = _lines.OrderBy(line => line.OrderLineId).ToArray();
        return expected.Length == actual.Length && expected.Zip(actual).All(pair =>
            pair.First.OrderLineId == pair.Second.OrderLineId &&
            string.Equals(pair.First.Name, pair.Second.Name, StringComparison.Ordinal) &&
            pair.First.Quantity == pair.Second.Quantity &&
            pair.First.UnitAmountMinor == pair.Second.UnitAmountMinor &&
            pair.First.DiscountAmountMinor == pair.Second.DiscountAmountMinor &&
            pair.First.LineTotalMinor == pair.Second.LineTotalMinor);
    }
}

public sealed class PaidOrderAcceptanceLine : ITenantEntity
{
    private PaidOrderAcceptanceLine()
    {
    }

    public Guid TenantId { get; set; }
    public Guid PaidOrderAcceptanceSnapshotId { get; private set; }
    public Guid OrderLineId { get; private set; }
    public int Ordinal { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public long UnitAmountMinor { get; private set; }
    public long DiscountAmountMinor { get; private set; }
    public long LineTotalMinor { get; private set; }

    internal static PaidOrderAcceptanceLine Create(
        Guid tenantId,
        Guid acceptanceSnapshotId,
        PaidOrderAcceptanceLineFact fact,
        int ordinal)
    {
        if (tenantId == Guid.Empty || acceptanceSnapshotId == Guid.Empty || ordinal < 0)
        {
            throw new ArgumentException("Acceptance line lineage and ordinal are required.");
        }
        return new()
        {
            TenantId = tenantId,
            PaidOrderAcceptanceSnapshotId = acceptanceSnapshotId,
            OrderLineId = fact.OrderLineId,
            Ordinal = ordinal,
            Name = fact.Name,
            Quantity = fact.Quantity,
            UnitAmountMinor = fact.UnitAmountMinor,
            DiscountAmountMinor = fact.DiscountAmountMinor,
            LineTotalMinor = fact.LineTotalMinor
        };
    }
}
