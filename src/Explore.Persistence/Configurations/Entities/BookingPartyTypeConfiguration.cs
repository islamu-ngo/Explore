// ABOUTME: EF mapping for stable registration booking-party lookup rows.
// ABOUTME: Enforces runtime-seeded IDs, codes, and display metadata.

using Explore.Domain;

namespace Explore.Persistence.Configurations.Entities;

public sealed class BookingPartyTypeConfiguration : LookupConfiguration<BookingPartyType>
{
    protected override string TableName => "booking_party_types";
}
