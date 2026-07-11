// ABOUTME: Describes safe UI presentation hints for AI proposed-action results.
// ABOUTME: Carries labels/card hints only and never raw provider payloads or private content.

namespace Explore.Application.Features.AiAssistant.Tools;

public sealed record AiToolResultPresentationMetadata(
    string CardKind,
    string ProposedTitle,
    string ConfirmedTitle,
    string FailedTitle);
