// ABOUTME: Provides the configured organizer payment provider identity for publication readiness checks.
// ABOUTME: Keeps paid-publication CQRS from hard-coding deployment-specific Connect platform identifiers.

namespace Explore.Application.Contracts.Services;

public interface IOrganizerPaymentCommerceConfiguration
{
    string ProviderCode { get; }

    string ConnectPlatformId { get; }
}

public sealed class OrganizerPaymentCommerceOptions : IOrganizerPaymentCommerceConfiguration
{
    public const string SectionName = "Payments:OrganizerDirect";

    public string ProviderCode { get; set; } = string.Empty;

    public string ConnectPlatformId { get; set; } = string.Empty;
}
