// ABOUTME: Shared constants for organization and entity approval statuses.
// ABOUTME: Matches backend ApprovalStatusEnum identifiers.

namespace Explore.Blazor.Client.Constants;

/// <summary>
/// Known Approval Status IDs from the backend (ApprovalStatusEnum).
/// </summary>
public static class ApprovalStatusId
{
    public const int Pending = 1;
    public const int Approved = 2;
    public const int Rejected = 3;
}
