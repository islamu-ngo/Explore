// ABOUTME: Validates AI tool payload objects against the safe JSON Schema subset used by registry definitions.
// ABOUTME: Enforces required fields, primitive types, UUID formats, numeric bounds, string lengths, and array items.

using System.Text.Json;

namespace Explore.Application.Features.AiAssistant.Tools;

internal static class AiToolJsonSchemaPayloadValidator
{
    public static AiToolValidationResult Validate(JsonElement payload, string schemaJson)
    {
        try
        {
            using var schemaDocument = JsonDocument.Parse(schemaJson);
            var schema = schemaDocument.RootElement;
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return Failure("invalid_action_schema", "AI tool schema must be a JSON object.");
            }

            var requiredResult = ValidateRequiredFields(payload, schema);
            if (!requiredResult.Succeeded)
            {
                return requiredResult;
            }

            if (!schema.TryGetProperty("properties", out var properties)
                || properties.ValueKind != JsonValueKind.Object)
            {
                return AiToolValidationResult.Success();
            }

            foreach (var property in payload.EnumerateObject())
            {
                if (!properties.TryGetProperty(property.Name, out var propertySchema))
                {
                    continue;
                }

                var result = ValidateValue(property.Value, propertySchema);
                if (!result.Succeeded)
                {
                    return result;
                }
            }

            return AiToolValidationResult.Success();
        }
        catch (JsonException)
        {
            return Failure("invalid_action_schema", "AI tool schema must be valid JSON.");
        }
    }

    private static AiToolValidationResult ValidateRequiredFields(JsonElement payload, JsonElement schema)
    {
        if (!schema.TryGetProperty("required", out var requiredElement)
            || requiredElement.ValueKind != JsonValueKind.Array)
        {
            return AiToolValidationResult.Success();
        }

        var presentFields = payload.EnumerateObject()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var required in requiredElement.EnumerateArray())
        {
            if (required.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var requiredName = required.GetString();
            if (!string.IsNullOrWhiteSpace(requiredName) && !presentFields.Contains(requiredName))
            {
                return Failure("missing_tool_argument", "AI tool payload is missing a required field.");
            }
        }

        return AiToolValidationResult.Success();
    }

    private static AiToolValidationResult ValidateValue(JsonElement value, JsonElement schema)
    {
        var type = GetStringProperty(schema, "type");
        if (string.IsNullOrWhiteSpace(type))
        {
            return AiToolValidationResult.Success();
        }

        return type switch
        {
            "string" => ValidateStringValue(value, schema),
            "integer" => ValidateIntegerValue(value),
            "number" => ValidateNumberValue(value, schema),
            "boolean" => ValidateBooleanValue(value),
            "array" => ValidateArrayValue(value, schema),
            "object" => value.ValueKind == JsonValueKind.Object
                ? AiToolValidationResult.Success()
                : Failure("invalid_tool_argument_type", "AI tool payload contains a value with the wrong type."),
            _ => AiToolValidationResult.Success()
        };
    }

    private static AiToolValidationResult ValidateStringValue(JsonElement value, JsonElement schema)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            return Failure("invalid_tool_argument_type", "AI tool payload contains a value with the wrong type.");
        }

        var text = value.GetString() ?? string.Empty;
        if (TryGetInt32Property(schema, "maxLength", out var maxLength) && text.Length > maxLength)
        {
            return Failure("invalid_tool_argument_value", "AI tool payload contains a value outside the allowed bounds.");
        }

        var format = GetStringProperty(schema, "format");
        if (string.Equals(format, "uuid", StringComparison.OrdinalIgnoreCase) && !Guid.TryParse(text, out _))
        {
            return Failure("invalid_tool_argument_format", "AI tool payload contains a value with an invalid format.");
        }

        return AiToolValidationResult.Success();
    }

    private static AiToolValidationResult ValidateIntegerValue(JsonElement value)
        => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _)
            ? AiToolValidationResult.Success()
            : Failure("invalid_tool_argument_type", "AI tool payload contains a value with the wrong type.");

    private static AiToolValidationResult ValidateNumberValue(JsonElement value, JsonElement schema)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var number))
        {
            return Failure("invalid_tool_argument_type", "AI tool payload contains a value with the wrong type.");
        }

        if (TryGetDecimalProperty(schema, "minimum", out var minimum) && number < minimum)
        {
            return Failure("invalid_tool_argument_value", "AI tool payload contains a value outside the allowed bounds.");
        }

        return AiToolValidationResult.Success();
    }

    private static AiToolValidationResult ValidateBooleanValue(JsonElement value)
        => value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? AiToolValidationResult.Success()
            : Failure("invalid_tool_argument_type", "AI tool payload contains a value with the wrong type.");

    private static AiToolValidationResult ValidateArrayValue(JsonElement value, JsonElement schema)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            return Failure("invalid_tool_argument_type", "AI tool payload contains a value with the wrong type.");
        }

        if (!schema.TryGetProperty("items", out var itemSchema) || itemSchema.ValueKind != JsonValueKind.Object)
        {
            return AiToolValidationResult.Success();
        }

        foreach (var item in value.EnumerateArray())
        {
            var result = ValidateValue(item, itemSchema);
            if (!result.Succeeded)
            {
                return result;
            }
        }

        return AiToolValidationResult.Success();
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static bool TryGetInt32Property(JsonElement element, string propertyName, out int value)
    {
        value = default;
        return element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetInt32(out value);
    }

    private static bool TryGetDecimalProperty(JsonElement element, string propertyName, out decimal value)
    {
        value = default;
        return element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetDecimal(out value);
    }

    private static AiToolValidationResult Failure(string failureCode, string failureMessage)
        => AiToolValidationResult.Failure(failureCode, failureMessage, AiToolCorrectionMessages.SchemaExactRetry);
}
