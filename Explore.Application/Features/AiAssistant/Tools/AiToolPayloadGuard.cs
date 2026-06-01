// ABOUTME: Validates untrusted AI tool payload JSON against allow-list and deny-list field policies.
// ABOUTME: Fails closed for malformed JSON, non-object payloads, unknown fields, and forbidden fields.

using System.Text.Json;

namespace Explore.Application.Features.AiAssistant.Tools;

public static class AiToolPayloadGuard
{
    public static AiToolValidationResult ValidateJsonObject(
        string payloadJson,
        IReadOnlySet<string> allowedFields,
        IReadOnlySet<string>? forbiddenFields = null)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return AiToolValidationResult.Failure(
                    "invalid_tool_arguments",
                    "AI tool payload must be a JSON object.");
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (forbiddenFields?.Contains(property.Name) == true)
                {
                    return AiToolValidationResult.Failure(
                        "forbidden_tool_argument",
                        "AI tool payload contains a field that is not allowed.");
                }

                if (!allowedFields.Contains(property.Name))
                {
                    return AiToolValidationResult.Failure(
                        "unsupported_tool_argument",
                        "AI tool payload contains an unsupported field.");
                }
            }

            return AiToolValidationResult.Success();
        }
        catch (JsonException)
        {
            return AiToolValidationResult.Failure(
                "invalid_tool_arguments",
                "AI tool payload must be valid JSON.");
        }
    }
}
