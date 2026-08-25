// ABOUTME: Identifies the immutable event decision that created a refund campaign.
// ABOUTME: Separates cancellation from material-change buyer-choice processing.

namespace Explore.Domain.Enums;

public enum RefundCampaignKind
{
    EventCancellation = 1,
    MaterialChange = 2
}
