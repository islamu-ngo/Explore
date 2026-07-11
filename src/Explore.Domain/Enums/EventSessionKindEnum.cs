// ABOUTME: Enum mirror of the EventSessionKind lookup used to classify program items.
// ABOUTME: Values are stable lookup table ids; add new kinds here and to the seeder together.

namespace Explore.Domain.Enums;

public enum EventSessionKindEnum
{
    Talk = 1,
    Workshop = 2,
    Panel = 3,
    Lecture = 4,
    Class = 5,
    Activity = 6,
    Keynote = 7,
    LightningTalk = 8,
    BOF = 9,
    Demo = 10,
    QAndA = 11,
    Other = 12
}
