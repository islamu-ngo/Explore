// ABOUTME: Exposes exact server-owned commercial, schedule, operator, and provider facts acknowledged before paid Checkout.
// ABOUTME: Carries only a revision and explicit acknowledgement back; browser values never author activation facts.

namespace Explore.Application.DTOs.RegistrationOrders;

public sealed record PaidOrderAcceptanceDisclosureDto
{
    public required string DisclosureRevision { get; init; }
    public required string MerchantDisclosureText { get; init; }
    public required string OperatorDisplayName { get; init; }
    public required bool IsOfficialInstance { get; init; }
    public required string OfficialOrigin { get; init; }
    public required string OperatorRegionCode { get; init; }
    public required string OperatorWebsiteUrl { get; init; }
    public required string OperatorLegalNoticeUrl { get; init; }
    public required string OperatorTermsUrl { get; init; }
    public required string OperatorPrivacyUrl { get; init; }
    public required string OperatorActivationStatus { get; init; }
    public required DateTimeOffset DeliveryStartsAtUtc { get; init; }
    public required DateTimeOffset DeliveryEndsAtUtc { get; init; }
    public required string EventTimeZoneId { get; init; }
    public required string CurrencyCode { get; init; }
    public required int CurrencyMinorUnitDigits { get; init; }
    public required long OrganizerAmountMinor { get; init; }
    public required long PlatformFeeMinor { get; init; }
    public required long PlatformContributionMinor { get; init; }
    public required long TotalMinor { get; init; }
    public required int RefundPolicyVersion { get; init; }
    public required string RefundPolicyText { get; init; }
    public required string RefundPolicyLanguageTag { get; init; }
    public required string SupportContact { get; init; }
    public required string ComplaintContact { get; init; }
    public required string ComplaintOwner { get; init; }
    public required string RefundOwner { get; init; }
    public required string DisputeOwner { get; init; }
    public required string ReconciliationOwner { get; init; }
    public required string ProviderCode { get; init; }
    public required string ProviderProfileCode { get; init; }
    public required string ProviderEnvironment { get; init; }
    public required string ProviderCredentialOwner { get; init; }
    public required string ChargeType { get; init; }
    public required string StatementDescriptor { get; init; }
    public required IReadOnlyList<PaidOrderAcceptanceLineDto> Lines { get; init; }
}

public sealed record PaidOrderAcceptanceLineDto
{
    public required Guid OrderLineId { get; init; }
    public required string Name { get; init; }
    public required int Quantity { get; init; }
    public required long UnitAmountMinor { get; init; }
    public required long DiscountAmountMinor { get; init; }
    public required long LineTotalMinor { get; init; }
}

public sealed record PaidOrderAcceptanceAcknowledgementDto
{
    public string? DisclosureRevision { get; init; }
    public required bool Acknowledged { get; init; }
}
