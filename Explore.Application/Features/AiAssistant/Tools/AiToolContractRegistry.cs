// ABOUTME: Provides the Application-layer registry for AI tool definitions and payload validation.
// ABOUTME: Keeps tool allow-lists centralized before prompts, parsers, confirmations, or adapters use them.

using Explore.Domain.Ai;

namespace Explore.Application.Features.AiAssistant.Tools;

public sealed class AiToolContractRegistry : IAiToolContractRegistry
{
    private readonly IReadOnlyDictionary<AiProposedActionKind, AiToolDefinition> _definitionsByKind;

    public static AiToolContractRegistry CreateDefault()
        => new([CreateEventDraftAiToolDefinition.Create()]);

    public AiToolContractRegistry(IEnumerable<AiToolDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        var definitionList = definitions.ToArray();
        var duplicateKind = definitionList
            .GroupBy(definition => definition.Kind)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicateKind is not null)
        {
            throw new ArgumentException("AI tool definitions must be unique by proposed-action kind.", nameof(definitions));
        }

        Definitions = definitionList;
        _definitionsByKind = definitionList.ToDictionary(definition => definition.Kind);
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
                "AI provider returned an unsupported proposed action kind.",
                AiToolCorrectionMessages.SchemaExactRetry);
        }

        return AiToolPayloadGuard.ValidateJsonObject(
            payloadJson,
            definition.AllowedPayloadFields,
            definition.ForbiddenPayloadFields,
            definition.JsonSchema);
    }
}
