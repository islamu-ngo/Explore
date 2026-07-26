// ABOUTME: Enumerates the only evidence categories allowed to consolidate global actors.
// ABOUTME: Excludes names, handles, profile similarity, and other mutable attributes from merge authority.

namespace Explore.Domain.Enums;

public enum ActorMergeProofKind
{
    VerifiedDid = 1,
    ExistingUserOwnership = 2,
    ManualInstanceReview = 3
}
