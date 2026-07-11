// ABOUTME: Defines supported reference target kinds for AI prompt context and conversation audit.
// ABOUTME: Keeps referenced domain objects typed instead of accepting arbitrary provider-supplied strings.

namespace Explore.Domain.Ai;

public enum AiReferenceKind
{
    Event = 1,
    EventSession = 2,
    Actor = 3,
    Organization = 4
}
