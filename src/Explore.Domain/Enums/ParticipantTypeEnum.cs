// ABOUTME: Enum mirror for stable participant-type lookup identities.
// ABOUTME: Distinguishes adult, represented, employee, guest, and unnamed participants.

namespace Explore.Domain.Enums;

public enum ParticipantTypeEnum
{
    Adult = 1,
    Child = 2,
    Dependent = 3,
    Employee = 4,
    Guest = 5,
    Unnamed = 6
}
