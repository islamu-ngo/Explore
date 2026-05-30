// ABOUTME: Validates provider proposed actions before they become persisted AI proposals.
// ABOUTME: Enforces allow-listed action kinds and JSON-object payload boundaries for untrusted model output.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Domain.Ai;

namespace Explore.Application.Features.AiAssistant.Prompting;

public sealed class AiStructuredActionParser
{
    private static readonly HashSet<AiProposedActionKind> AllowedKinds = [AiProposedActionKind.CreateEventDraft];

    public AiStructuredActionParseResult Parse(IReadOnlyList<AiProposedActionCandidate> candidates)
    {
        var actions = new List<AiParsedProposedAction>(candidates.Count);

        foreach (var candidate in candidates)
        {
            if (!AllowedKinds.Contains(candidate.Kind))
            {
                return AiStructuredActionParseResult.Failure(
                    "unknown_action_kind",
                    "AI provider returned an unsupported proposed action kind.");
            }

            if (!IsJsonObject(candidate.PayloadJson))
            {
                return AiStructuredActionParseResult.Failure(
                    "invalid_tool_arguments",
                    "AI provider returned invalid action payload JSON.");
            }

            actions.Add(new AiParsedProposedAction(candidate.Kind, candidate.PayloadJson, candidate.Summary));
        }

        return AiStructuredActionParseResult.Success(actions);
    }

    private static bool IsJsonObject(string payloadJson)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public sealed record AiParsedProposedAction(
    AiProposedActionKind Kind,
    string PayloadJson,
    string? Summary);

public sealed record AiStructuredActionParseResult(
    bool Succeeded,
    IReadOnlyList<AiParsedProposedAction> Actions,
    string? FailureCode,
    string? FailureMessage)
{
    public static AiStructuredActionParseResult Success(IReadOnlyList<AiParsedProposedAction> actions)
        => new(true, actions, null, null);

    public static AiStructuredActionParseResult Failure(string failureCode, string failureMessage)
        => new(false, [], failureCode, failureMessage);
}
