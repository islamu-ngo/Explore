// ABOUTME: Aggregate-only result for an expired privacy-erasure credential cleanup pass.
// ABOUTME: Excludes intent, subject, receipt, and provider locator identifiers by design.

namespace Explore.Application.Models;

public sealed record PrivacyErasureCredentialCleanupResult(
    int ReceiptHashesEligible,
    int ReceiptHashesCleared,
    int ProviderLocatorsEligible,
    int ProviderLocatorsCleared,
    bool DryRun);
