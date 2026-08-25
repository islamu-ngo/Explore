// ABOUTME: Defines durable refund-campaign scheduling and completion states.
// ABOUTME: Keeps operator intervention distinct from successful fanout completion.

namespace Explore.Domain.Enums;

public enum RefundCampaignStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    RequiresOperator = 4
}
