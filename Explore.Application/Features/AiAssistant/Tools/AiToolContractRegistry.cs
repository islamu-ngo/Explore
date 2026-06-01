// ABOUTME: Provides the Application-layer registry for AI tool definitions and payload validation.
// ABOUTME: Keeps tool allow-lists centralized before prompts, parsers, confirmations, or adapters use them.

using Explore.Domain.Ai;

namespace Explore.Application.Features.AiAssistant.Tools;

public sealed class AiToolContractRegistry : IAiToolContractRegistry
{
    private readonly IReadOnlyDictionary<AiProposedActionKind, AiToolDefinition> _definitionsByKind;

    public AiToolContractRegistry(IEnumerable<AiToolDefinition> definitions)
    {
        Definitions = definitions.ToList();
        _definitionsByKind = Definitions.ToDictionary(definition => definition.Kind);
    }

    public IReadOnlyList<AiToolDefinition> Definitions { get; }

    public AiToolDefinition? FindDefinition(AiProposedActionKind kind)
        => _definitionsByKind.GetValueOrDefault(kind);

    public AiToolValidationResult ValidatePayload(AiProposedActionKind kind, string payloadJson)
    {
        var definition = FindDefinition(kind);
        if (definition is null)
        {
            return AiToolValidationResult.Failure(
                "unknown_action_kind",
                "AI provider returned an unsupported proposed action kind.");
        }

        return AiToolPayloadGuard.ValidateJsonObject(
            payloadJson,
            definition.AllowedPayloadFields,
            definition.ForbiddenPayloadFields);
    }
}
