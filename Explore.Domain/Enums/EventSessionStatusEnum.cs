// ABOUTME: Canonical integer identifiers for EventSession lifecycle status lookups.
// ABOUTME: Values must match the EventSessionStatus lookup seed data and database rows.
namespace Explore.Domain.Enums;

public enum EventSessionStatusEnum
{
    Draft = 1,
    Submitted = 2,
    UnderReview = 3,
    Approved = 4,
    Published = 5,
    Rejected = 6,
    Cancelled = 7,
    Archived = 8,
    Completed = 9,
    Moderated = 10
}
