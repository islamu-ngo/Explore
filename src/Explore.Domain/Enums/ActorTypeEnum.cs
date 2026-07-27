// ABOUTME: Stable lookup identifiers for the concrete subject kinds owned by Actor.
// ABOUTME: Includes the temporary external-unclassified kind used before verified promotion.

namespace Explore.Domain.Enums;

public enum ActorTypeEnum
{
    User = 1,
    Organization = 2,
    Bot = 3,
    Group = 4,
    System = 5,
    ExternalUnclassified = 6
}
