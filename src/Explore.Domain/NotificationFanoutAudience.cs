// ABOUTME: Domain values for deterministic notification fanout audience paging.
// ABOUTME: Uses an immutable timestamp-plus-user cursor so replay can resume without skipping recipients.

namespace Explore.Domain;

public readonly record struct NotificationFanoutAudienceCursor(
    DateTime FirstEligibleRegistrationCreatedAt,
    Guid UserId);

public sealed record NotificationFanoutAudienceMember
{
    public NotificationFanoutAudienceMember()
    {
    }

    public NotificationFanoutAudienceMember(
        Guid userId,
        DateTime firstEligibleRegistrationCreatedAt)
    {
        UserId = userId;
        FirstEligibleRegistrationCreatedAt = firstEligibleRegistrationCreatedAt;
    }

    public Guid UserId { get; init; }
    public DateTime FirstEligibleRegistrationCreatedAt { get; init; }
}
