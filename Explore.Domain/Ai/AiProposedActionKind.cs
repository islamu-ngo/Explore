// ABOUTME: Defines the allow-listed action kinds the AI assistant may propose.
// ABOUTME: Starts with event draft creation so future mutating tools remain explicitly registered.

namespace Explore.Domain.Ai;

public enum AiProposedActionKind
{
    CreateEventDraft = 1
}
