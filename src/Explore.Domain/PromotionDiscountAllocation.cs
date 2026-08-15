// ABOUTME: Defines provider-neutral promotion discount allocation inputs and immutable results.
// ABOUTME: Keeps allocation data in minor units so future order snapshots can copy exact values.

namespace Explore.Domain;

public sealed record PromotionDiscountLine(Guid LineId, Guid TicketTypeId, string CurrencyCode, long LineSubtotalMinor);

public sealed record PromotionLineDiscountAllocation(Guid LineId, long PreDiscountLineSubtotalMinor, long DiscountMinor, long PostDiscountLineSubtotalMinor);

public sealed record PromotionDiscountAllocation(long TotalDiscountMinor, long PostDiscountOrganizerTotalMinor, IReadOnlyCollection<PromotionLineDiscountAllocation> LineAllocations);
