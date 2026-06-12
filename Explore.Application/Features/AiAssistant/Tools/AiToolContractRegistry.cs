// ABOUTME: Provides the Application-layer registry for AI tool definitions and payload validation.
// ABOUTME: Keeps tool allow-lists and safe provider-payload normalization centralized before downstream use.

using System.Text.Json;
using System.Text.Json.Nodes;
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

    public AiToolValidationResult ValidatePayload(
        AiProposedActionKind kind,
        string payloadJson,
        bool allowProviderNormalization = false)
    {
        var definition = FindDefinition(kind);
        if (definition is null)
        {
            return AiToolValidationResult.Failure(
                "unknown_action_kind",
                "AI provider returned an unsupported proposed action kind.",
                AiToolCorrectionMessages.SchemaExactRetry);
        }

        var validation = AiToolPayloadGuard.ValidateJsonObject(
            payloadJson,
            definition.AllowedPayloadFields,
            definition.ForbiddenPayloadFields,
            definition.JsonSchema);

        if (!allowProviderNormalization
            || validation.Succeeded
            || kind != AiProposedActionKind.CreateEventDraft
            || !ShouldNormalizeOptionalReferenceIds(validation))
        {
            return validation;
        }

        return TryNormalizeCreateEventDraftPayload(payloadJson, definition, out var normalizedPayloadJson)
            ? ValidateNormalizedPayload(normalizedPayloadJson, definition)
            : validation;
    }

    private static bool ShouldNormalizeOptionalReferenceIds(AiToolValidationResult validation)
        => string.Equals(validation.FailureCode, "invalid_tool_argument_format", StringComparison.Ordinal);

    private static AiToolValidationResult ValidateNormalizedPayload(
        string normalizedPayloadJson,
        AiToolDefinition definition)
    {
        var validation = AiToolPayloadGuard.ValidateJsonObject(
            normalizedPayloadJson,
            definition.AllowedPayloadFields,
            definition.ForbiddenPayloadFields,
            definition.JsonSchema);

        return validation.Succeeded
            ? AiToolValidationResult.Success(normalizedPayloadJson)
            : validation;
    }

    private static bool TryNormalizeCreateEventDraftPayload(
        string payloadJson,
        AiToolDefinition definition,
        out string normalizedPayloadJson)
    {
        normalizedPayloadJson = payloadJson;

        JsonObject? payload;
        try
        {
            payload = JsonNode.Parse(payloadJson)?.AsObject();
        }
        catch (JsonException)
        {
            return false;
        }

        if (payload is null)
        {
            return false;
        }

        var changed = false;
        changed |= RemoveInvalidOptionalUuid(payload, "organizationId");
        changed |= RemoveInvalidOptionalUuid(payload, "groupId");
        changed |= RemoveInvalidOptionalUuidArray(payload, "categoryIds");
        changed |= RemoveInvalidOptionalUuidArray(payload, "tagIds");

        if (!changed)
        {
            return false;
        }

        var allowedFieldsOnly = payload
            .Select(property => property.Key)
            .All(definition.AllowedPayloadFields.Contains);
        if (!allowedFieldsOnly)
        {
            return false;
        }

        normalizedPayloadJson = payload.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return true;
    }

    private static bool RemoveInvalidOptionalUuid(JsonObject payload, string propertyName)
    {
        if (!payload.TryGetPropertyValue(propertyName, out var value) || value is null)
        {
            return false;
        }

        if (value.GetValueKind() == JsonValueKind.String
            && Guid.TryParse(value.GetValue<string>(), out _))
        {
            return false;
        }

        return payload.Remove(propertyName);
    }

    private static bool RemoveInvalidOptionalUuidArray(JsonObject payload, string propertyName)
    {
        if (!payload.TryGetPropertyValue(propertyName, out var value) || value is not JsonArray values)
        {
            return false;
        }

        var normalizedValues = new JsonArray();
        var removedInvalidValue = false;
        foreach (var item in values)
        {
            if (item?.GetValueKind() == JsonValueKind.String
                && Guid.TryParse(item.GetValue<string>(), out _))
            {
                normalizedValues.Add(item.DeepClone());
                continue;
            }

            removedInvalidValue = true;
        }

        if (!removedInvalidValue)
        {
            return false;
        }

        if (normalizedValues.Count == 0)
        {
            return payload.Remove(propertyName);
        }

        payload[propertyName] = normalizedValues;
        return true;
    }
}
