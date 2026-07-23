// ABOUTME: Returns asynchronous privacy-erasure acceptance state and the once-revealed receipt.
// ABOUTME: Exposes only bounded status metadata and never persisted subject or provider identifiers.

namespace Explore.Application.DTOs.PrivacyErasure;

public sealed record PrivacyErasureStartDto(
    string Status,
    string? Receipt,
    DateTime ReceiptExpiresAtUtc);
