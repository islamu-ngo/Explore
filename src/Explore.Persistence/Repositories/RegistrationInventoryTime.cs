// ABOUTME: Validates UTC timestamps entering registration inventory mutations.
// ABOUTME: Keeps one temporal boundary shared by hold and reservation operations.

namespace Explore.Persistence.Repositories;

internal static class RegistrationInventoryTime
{
    public static void RequireUtc(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Expiry time must be UTC.",
                nameof(utcNow));
        }
    }
}
