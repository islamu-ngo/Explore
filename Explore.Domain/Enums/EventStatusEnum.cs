// ABOUTME: Canonical integer identifiers for event lifecycle status lookups.
// ABOUTME: Values must match the EventStatus lookup seed data and database rows.

namespace Explore.Domain.Enums;

public enum EventStatusEnum
{
    Draft = 1,
    Published = 2,
    Cancelled = 3,
    Completed = 4,
    Archived = 5,
    Moderated = 6
}
