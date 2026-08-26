// ABOUTME: Defines the durable operator-controlled availability state for an admission target.
// ABOUTME: Keeps stop and restore decisions explicit without changing immutable check-in facts.

namespace Explore.Domain.Enums;

public enum AdmissionOperationalStatusEnum
{
    Active = 1,
    Stopped = 2
}
