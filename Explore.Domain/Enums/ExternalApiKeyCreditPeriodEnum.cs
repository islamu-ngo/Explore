// ABOUTME: Stable integer identifiers for external API key credit renewal periods.
// ABOUTME: Mapped to ExternalApiKeyCreditPeriod lookup-table rows; None means unlimited (no credit tracking).

namespace Explore.Domain.Enums;

public enum ExternalApiKeyCreditPeriodEnum
{
    None = 1,
    Daily = 2,
    Weekly = 3,
    Monthly = 4,
    Yearly = 5
}
