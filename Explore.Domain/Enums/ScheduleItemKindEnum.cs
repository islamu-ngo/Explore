// ABOUTME: Enum mirror of the ScheduleItemKind lookup used to classify event and session agenda items.
// ABOUTME: Values are stable lookup table ids; add new kinds here and to the seeder together.

namespace Explore.Domain.Enums;

public enum ScheduleItemKindEnum
{
    Intro = 1,
    Talk = 2,
    QAndA = 3,
    Break = 4,
    Prayer = 5,
    Outro = 6,
    Logistics = 7,
    Custom = 8
}
