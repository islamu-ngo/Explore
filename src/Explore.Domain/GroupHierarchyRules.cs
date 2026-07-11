// ABOUTME: Central group hierarchy invariants shared by application validation and persistence checks.
// ABOUTME: Keeps max-depth policy in the domain layer so outer layers depend inward.

namespace Explore.Domain;

public static class GroupHierarchyRules
{
    public const int MaxDepth = 8;
}
