// ABOUTME: Represents receipt-authorized privacy-erasure progress after login removal.
// ABOUTME: Limits output to bounded phase codes, aggregate provider counts, and timestamps.

namespace Explore.Application.DTOs.PrivacyErasure;

public sealed record PrivacyErasureStatusDto(
    string Status,
    int ProviderWorkCount,
    int CompletedProviderWorkCount,
    DateTime ReceiptExpiresAtUtc,
    DateTime? LocalSettledAtUtc,
    DateTime? CompletedAtUtc);
