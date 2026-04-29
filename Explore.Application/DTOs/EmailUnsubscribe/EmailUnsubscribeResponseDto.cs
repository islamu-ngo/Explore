// ABOUTME: Public response contract for email unsubscribe status and one-click POST outcomes.
// ABOUTME: Keeps anonymous unsubscribe responses generic enough to avoid token-oracle leakage.

namespace Explore.Application.DTOs.EmailUnsubscribe;

public sealed record EmailUnsubscribeResponseDto(
    string Status,
    string Message,
    string? Category = null,
    bool? IsSubscribed = null,
    bool RequiresConfirmation = false);
